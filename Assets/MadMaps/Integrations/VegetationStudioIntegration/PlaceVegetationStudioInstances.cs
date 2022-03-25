#if VEGETATION_STUDIO
using AwesomeTechnologies;
using AwesomeTechnologies.VegetationStudio;
using System;
using System.Collections.Generic;
using MadMaps.Common;
using MadMaps.Terrains;
using UnityEngine;
using Random = System.Random;
using AwesomeTechnologies.Vegetation.PersistentStorage;
using System.Reflection;
using MadMaps.Common.GenericEditor;
using MadMaps.Roads;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MadMaps.Integration.VegetationStudio
{
#if UNITY_EDITOR
    public class VegetationStudioPrototypePickerDrawer : GenericDrawer<VegetationStudioPrototypePicker>
    {
        protected override VegetationStudioPrototypePicker DrawGUIInternal(VegetationStudioPrototypePicker target, string label = "", Type targetType = null, FieldInfo fieldInfo = null,
            object context = null)
        {
            EditorGUILayout.LabelField(label);
            EditorGUI.indentLevel++;
            target.Package = (VegetationPackage)EditorGUILayout.ObjectField("Package", target.Package, typeof(VegetationPackage), false);
            if(target.Package == null)
            {
                EditorGUI.indentLevel--;
                return target;
            }
            for(var i = 0; i < target.Package.VegetationInfoList.Count; ++i)
            {
                var info = target.Package.VegetationInfoList[i];
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("", GUILayout.Width(EditorGUI.indentLevel * 16 - 4));
                GUI.color = target.SelectedIDs.Contains(info.VegetationItemID) ? Color.red : Color.white;
                EditorGUILayout.BeginHorizontal("Box");
                GUI.color = Color.white;
                Texture2D previewTex = UnityEditor.AssetPreview.GetAssetPreview(info.PrefabType == VegetationPrefabType.Mesh ?
                    (UnityEngine.Object)info.VegetationPrefab : (UnityEngine.Object)info.VegetationTexture);
                
                if(GUILayout.Button(previewTex, GUILayout.Width(30), GUILayout.Height(30)) || GUILayout.Button(info.Name, EditorStyles.boldLabel, 
                    GUILayout.Height(30)))
                {
                    if(target.SelectedIDs.Contains(info.VegetationItemID))
                    {
                        target.SelectedIDs.Remove(info.VegetationItemID);
                    }
                    else
                    {
                        target.SelectedIDs.Add(info.VegetationItemID);
                    }
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndHorizontal();
            }            
            EditorGUI.indentLevel--;
            return target;
        }
    }
#endif
    [Serializable]
    public class VegetationStudioPrototypePicker
    {
        public VegetationPackage Package;
        public List<string> SelectedIDs = new List<string>();

        public bool Contains(string id)
        {
            return SelectedIDs.Contains(id);
        }
    }

    public class PlaceVegetationStudioInstances : ConnectionComponent
    {
        [Name("Vegetation Studio/Place Vegetation")]
        public class Config : ConnectionConfigurationBase
        {
            public AnimationCurve ProbablityAlongCurve = new AnimationCurve()
            {
                keys = new Keyframe[]
            {
                new Keyframe(0, 1), 
                new Keyframe(1, 1), 
            }
            };
            public AnimationCurve ProbablityThroughCurve = new AnimationCurve()
            {
                keys = new Keyframe[]
            {
                new Keyframe(0, -1), 
                new Keyframe(0.5f, 0), 
                new Keyframe(1, 1),
            }
            };
            public VegetationStudioPrototypePicker Prototypes = new VegetationStudioPrototypePicker();
            public Vec3MinMax Rotation = new Vec3MinMax(new Vector3(0, 0, 0), new Vector3(0, 360, 0));
            public FloatMinMax Size = new FloatMinMax(1, 1);
            public ColorMinMax Color = new ColorMinMax(UnityEngine.Color.white, UnityEngine.Color.white);
            public float ProbabilityMultiplier = 1;
            public FloatMinMax StepDistance = new FloatMinMax(1, 1);
            public float OffsetMultiplier = 5f;
            public float YOffset = 0;

            public override Type GetMonoType()
            {
                return typeof(PlaceVegetationStudioInstances);
            }
        }


        public override void ProcessVegetationStudio(TerrainWrapper wrapper, LayerBase baseLayer, int stencilKey)
        {
            var layer = baseLayer as MMTerrainLayer;
            if(layer == null)
            {
                Debug.LogWarning(string.Format("Attempted to write {0} to incorrect layer type! Expected Layer {1} to be {2}, but it was {3}", name, baseLayer.name, GetLayerType(), baseLayer.GetType()), this);
                return;
            }

            var config = Configuration.GetConfig<Config>();
            var spline = NodeConnection.GetSpline();
            var rand = new Random(NodeConnection.ThisNode.Seed);
            if(config.Prototypes.SelectedIDs.Count == 0 || config.Prototypes.Package == null)
            {
                return;
            }
            
            var length = spline.Length;
            var step = config.StepDistance;
            var tSize = wrapper.Terrain.terrainData.size;

            for (var i = 0f; i < length; i += step.GetRand(rand))
            {
                var roll = rand.NextDouble();
                var randroll = (float) rand.NextDouble();
                var eval = config.ProbablityThroughCurve.Evaluate(randroll);
                //Debug.Log(randroll + " " + eval);
                var offsetDist = eval * config.OffsetMultiplier;
                var uniformT = i / length;
                var naturalT = spline.UniformToNaturalTime(uniformT);

                var offset = (spline.GetTangent(naturalT).normalized * offsetDist);
                var probability = config.ProbablityAlongCurve.Evaluate(uniformT) * config.ProbabilityMultiplier;

                if (probability < roll)
                {
                    continue;
                }

                // Place tree
                var randIndex = Mathf.FloorToInt((float)rand.NextDouble() * config.Prototypes.SelectedIDs.Count);
                var id = config.Prototypes.SelectedIDs[randIndex];
                var wPos = spline.GetUniformPointOnSpline(uniformT) + offset;
                
                //Debug.DrawLine(wPos, wPos + Vector3.up *10, Color.red, 10);
                var tPos = wrapper.Terrain.WorldToTreePos(wPos);
                tPos.y = config.YOffset;
                var size = Vector3.one * config.Size.GetRand(rand);
                var newInstance = new VegetationStudioInstance()
                {
                    Guid = System.Guid.NewGuid().ToString(),
                    VSID = id,
                    Position = tPos,
                    Scale = size,
                    Rotation = config.Rotation.GetRand(rand),
                    Package = config.Prototypes.Package,
                };
                layer.VSInstances.Add(newInstance);
            }
        }
    }
}
#endif