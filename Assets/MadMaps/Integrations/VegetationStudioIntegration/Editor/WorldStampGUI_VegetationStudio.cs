#if VEGETATION_STUDIO
using System.Collections.Generic;
using System.Linq;
using AwesomeTechnologies;
using MadMaps.Common;
using MadMaps.Common.Painter;
using MadMaps.Terrains;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using MadMaps.Common.GenericEditor;
using MadMaps.Roads;
using MadMaps.Integration.VegetationStudio;

namespace MadMaps.WorldStamps
{
    public partial class WorldStampGUI
    {        
        private bool _vegetationStudioExpanded;
        private SerializedProperty _vegetationStudioEnabled;
        private SerializedProperty _stencilVSData;
        private SerializedProperty _removeExistingVSData;     

        VegetationStudioPrototypePickerDrawer _vsIgnoredPrototypesDrawer;

        private void DoVegetationStudioSection()
        {
            WorldStamp singleInstance = targets.Length == 1 ? target as WorldStamp : null;
            bool canWrite = !(singleInstance && singleInstance.Data.VSData.Count == 0);
            DoHeader("Vegetation Studio", ref _vegetationStudioExpanded, _vegetationStudioEnabled, canWrite);
            if (_vegetationStudioExpanded)
            {
                EditorGUI.indentLevel++;     

                if (!canWrite)
                {
                    if(singleInstance)
                    {
                        singleInstance.VegetationStudioEnabled = false;
                    }
                    EditorGUILayout.HelpBox("No Vegetation Studio instances in Stamp", MessageType.Info);
                }

                EditorGUILayout.PropertyField(_removeExistingVSData);
                GUI.enabled = canWrite;
                EditorGUILayout.PropertyField(_stencilVSData);
                GUI.enabled = true;

                if(singleInstance)
                {
                    if(_vsIgnoredPrototypesDrawer == null)
                    {
                        _vsIgnoredPrototypesDrawer = new VegetationStudioPrototypePickerDrawer();
                    }
                    singleInstance.IgnoredVSPrototypes = (VegetationStudioPrototypePicker)_vsIgnoredPrototypesDrawer.DrawGUI(
                        singleInstance.IgnoredVSPrototypes, "Ignored Protoypes", typeof(VegetationStudioPrototypePicker), null, singleInstance);
                    if(singleInstance.Data.VSData.Any((x) => !x.Package))
                    {
                        EditorGUILayout.HelpBox("Vegetation Instances are missing their VegetationStudioPackage! Select one now?", MessageType.Info);
                        if(GUILayout.Button("Select Package"))
                        {
                            var path = EditorExtensions.OpenFilePanel("Select Vegetation Package", "asset");
                            if(!string.IsNullOrEmpty(path))
                            {
                                var package = AssetDatabase.LoadAssetAtPath<VegetationPackage>(path);
                                if(!package)
                                {
                                    Debug.LogError("Failed to load Vegetation Package at " + path);
                                }
                                else
                                {
                                    for(var i = 0; i < singleInstance.Data.VSData.Count; ++i)
                                    {
                                        if(!singleInstance.Data.VSData[i].Package)
                                        {
                                            singleInstance.Data.VSData[i].Package = package;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    
                }

                EditorGUI.indentLevel--;
            }
        }
   
    }
}
#endif