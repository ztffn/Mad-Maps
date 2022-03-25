using MadMaps.Terrains;
using System;
using System.Collections.Generic;
using MadMaps.Common;
using MadMaps.Roads;
using UnityEngine;
using System.Linq;
using MadMaps.WorldStamps.Authoring;
using MadMaps.WorldStamps;

#if VEGETATION_STUDIO

using AwesomeTechnologies.Vegetation.PersistentStorage;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MadMaps.Integration.VegetationStudio
{
    [Serializable]
    public class VegetationStudioDataCreator : WorldStampCreatorLayer
    {
        [NonSerialized]
        public List<VegetationStudioInstance> VSData = new List<VegetationStudioInstance>();
        public VegetationStudioPrototypePicker IgnoredPrototypes = new VegetationStudioPrototypePicker();
        //public List<GameObject> Prototypes = new List<GameObject>();
        //public List<GameObject> IgnoredTrees = new List<GameObject>();

        public override GUIContent Label
        {
            get { return new GUIContent(string.Format("Vegetation Studio ({0})", VSData.Count));}
        }

        protected override bool HasDataPreview
        {
            get { return true; }
        }

        protected override void CaptureInternal(Terrain terrain, Bounds bounds)
        {
            //Prototypes.Clear();
            VSData.Clear();

            var vegSystems = VegetationStudioUtilities.GetVegetationSystemsForTerrain(terrain);

            var expandedBounds = bounds;
            expandedBounds.Expand(Vector3.up * 5000);

            foreach(var vegSys in vegSystems)
            {
                if(vegSys.GetTerrain() != terrain)
                {
                    continue;
                }
                var vegStorage = vegSys.GetComponent<PersistentVegetationStorage>();
                if(!vegStorage)
                {
                    continue;
                }
                IgnoredPrototypes.Package = vegSys.CurrentVegetationPackage;
                foreach(var cell in vegStorage.PersistentVegetationStoragePackage.PersistentVegetationCellList)
                {
                    foreach(var info in cell.PersistentVegetationInfoList)
                    {
                        foreach(var item in info.VegetationItemList)
                        {
                            var worldPos = terrain.transform.position + item.Position;                
                            if (!expandedBounds.Contains(worldPos))
                            {
                                continue;
                            }
                            var stampRelativePos = worldPos - bounds.min;
                            stampRelativePos = new Vector3(stampRelativePos.x / bounds.size.x, 
                                stampRelativePos.y - terrain.SampleHeight(worldPos), stampRelativePos.z / bounds.size.z);

                            VSData.Add(new VegetationStudioInstance()
                            {
                                Guid = System.Guid.NewGuid().ToString(),
                                Position = stampRelativePos,
                                Rotation = item.Rotation.eulerAngles,
                                Scale = item.Scale,
                                VSID = info.VegetationItemID,
                                Package = vegSys.CurrentVegetationPackage,
                            });
                        }
                    }
                }
            }
        }

        public override void PreviewInDataInspector()
        {
            #if UNITY_EDITOR
            Dictionary<object, IDataInspectorProvider> data = new Dictionary<object, IDataInspectorProvider>();
            foreach (var obj in VSData)
            {
                if(IgnoredPrototypes.Contains(obj.VSID))
                {
                    continue;
                }
                if(!data.ContainsKey(obj.VSID))
                {
                    data[obj.VSID] = new PositionList();
                }
                (data[obj.VSID] as PositionList).Add(obj.Position);
            }
            DataInspector.SetData(data.Values.ToList(), data.Keys.ToList(), true);
            #endif
        }

        public override void Clear()
        {
            VSData.Clear();
        }

        protected override void CommitInternal(WorldStampData data, WorldStamp stamp)
        {
            data.VSData.Clear();
            for (int i = 0; i < VSData.Count; i++)
            {
                var instance = VSData[i];
                if (IgnoredPrototypes.Contains(instance.VSID))
                {
                    continue;
                }
                data.VSData.Add(instance.Clone());
            }
            stamp.RemoveExistingVSData = data.VSData.Count > 0;
            stamp.IgnoredVSPrototypes = IgnoredPrototypes.JSONClone();
        }

#if UNITY_EDITOR
        protected override void PreviewInSceneInternal(WorldStampCreator parent)
        {
            var bounds = parent.Template.Bounds;
            var terrain = parent.Template.Terrain;
            var tSize = terrain.terrainData.size;
            Handles.color = Color.green;
            foreach (var vsInstance in VSData)
            {
                var pos = new Vector3(vsInstance.Position.x * bounds.size.x, 0,
                    vsInstance.Position.z * bounds.size.z) + bounds.min;   
                pos.y += terrain.SampleHeight(pos);    
                Handles.DrawDottedLine(pos, pos + Vector3.up * 10 * vsInstance.Scale.x, 1);
            }
        }

        VegetationStudioPrototypePickerDrawer _protoypePicker;
        
        protected override void OnExpandedGUI(WorldStampCreator parent)
        {
            if (VSData.Count == 0)
            {
                EditorGUILayout.HelpBox("No VegetationStudio Data Found", MessageType.Info);
                return;
            }

            if(_protoypePicker == null)
            {
                _protoypePicker = new VegetationStudioPrototypePickerDrawer();
            }
            
            IgnoredPrototypes = (VegetationStudioPrototypePicker)_protoypePicker.DrawGUI(IgnoredPrototypes, 
                "Ignored Prototypes", typeof(VegetationStudioPrototypePicker), null, this);

            /*for (int i = 0; i < Prototypes.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                Prototypes[i] = (GameObject)EditorGUILayout.ObjectField(Prototypes[i],
                    typeof (GameObject), false);
                GUI.color = Prototypes[i] != null && IgnoredTrees.Contains(Prototypes[i])
                    ? Color.red
                    : Color.white;
                GUI.enabled = Prototypes[i] != null;
                if (GUILayout.Button("Ignore", EditorStyles.miniButton, GUILayout.Width(60)))
                {
                    if (IgnoredTrees.Contains(Prototypes[i]))
                    {
                        IgnoredTrees.Remove(Prototypes[i]);
                    }
                    else
                    {
                        IgnoredTrees.Add(Prototypes[i]);
                    }
                }
                GUI.enabled = true;
                GUI.color = Color.white;
                EditorGUILayout.EndHorizontal();
            }*/
        }
#endif
    }
}
#endif