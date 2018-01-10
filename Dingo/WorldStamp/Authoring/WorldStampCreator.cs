﻿#if UNITY_EDITOR

using System.Linq;
using Dingo.Common;
using UnityEditor;
using UnityEngine;

namespace Dingo.WorldStamp.Authoring
{
    public class WorldStampCreator : SceneViewEditorWindow
    {
        [MenuItem("Tools/Dingo/World Stamp Creator", false, 6)]
        public static void OpenWindow()
        {
            var w = GetWindow<WorldStampCreator>();
            w.titleContent = new GUIContent("World Stamp Creator");
        }
        
        public WorldStampCaptureTemplate Template = new WorldStampCaptureTemplate();
        public WorldStampCreatorLayer SceneGUIOwner;

        private GUIContent _createStampTemplateContent = new GUIContent("Create Stamp Template", "Create an in-scene object to preserve stamp capture settings.");

        void OnEnable()
        {
            if (Template.Terrain != null)
            {
                return;
            }
            var currentTerrain = Terrain.activeTerrain;
            if (!currentTerrain)
            {
                return;
            }
            Template.Terrain = currentTerrain;
            Template.Bounds = Template.Terrain.GetBounds();
        }

        public T GetCreator<T>() where T: WorldStampCreatorLayer
        {
            return Template.Creators.First(layer => layer is T) as T;
        }
        
        protected void OnGUI()
        {
            Template.Terrain = (Terrain)EditorGUILayout.ObjectField("Target Terrain", Template.Terrain, typeof(Terrain), true);
            if (Template.Terrain == null)
            {
                EditorGUILayout.HelpBox("Please Select a Target Terrain", MessageType.Info);
                return;
            }
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Layers", EditorStyles.boldLabel);
            var recaptureContent = new GUIContent("Capture");
            recaptureContent.tooltip = "Recapture this data.";
            GUI.color = Template.Creators.Any(layer => layer.NeedsRecapture) ? Color.Lerp(Color.red, Color.white, .5f) : Color.white;
            if (GUILayout.Button(recaptureContent, EditorStyles.miniButton, GUILayout.Width(60), GUILayout.Height(16)))
            {
                for (int i = 0; i < Template.Creators.Count; i++)
                {
                    var worldStampCreatorLayer = Template.Creators[i];
                    worldStampCreatorLayer.Capture(Template.Terrain, Template.Bounds);
                }
            }
            GUI.color = Color.white;
            EditorGUILayout.EndHorizontal();

            for (int i = 0; i < Template.Creators.Count; i++)
            {
                Template.Creators[i].DrawGUI(this);
            }

            EditorGUILayout.LabelField("", GUILayout.ExpandHeight(true));
            EditorExtensions.Seperator();

            if (GUILayout.Button("Create Stamp Template"))
            {
                var mask = Template.Creators.First(layer => layer is MaskDataCreator) as MaskDataCreator;
                
                mask.Mask.OnBeforeSerialize();
                mask.Mask.OnAfterDeserialize();

                var newTemplate = new GameObject("Stamp Template");
                var temp = newTemplate.AddComponent<WorldStampCaptureTemplateContainer>();
                temp.Template = Template.Clone();
            }
            if (GUILayout.Button("Create New Stamp"))
            {
                for (int i = 0; i < Template.Creators.Count; i++)
                {
                    var layer = Template.Creators[i];
                    if (layer.Enabled && layer.NeedsRecapture && EditorUtility.DisplayDialog(string.Format("Layer {0} Needs Recapture", layer.Label.text), 
                        string.Format("Layer {0} needs to recapture it's data. Do this now?", layer.Label.text), "Yes", "No"))
                    {
                        layer.Capture(Template.Terrain, Template.Bounds);
                    }
                }

                GameObject go = new GameObject("New WorldStamp");
                go.transform.position = Template.Bounds.center.x0z(Template.Bounds.min.y) + Vector3.up * GetCreator<HeightmapDataCreator>().ZeroLevel * Template.Terrain.terrainData.size.y;
                var stamp = go.AddComponent<WorldStamp>();
                var data = new WorldStampData();
                foreach (var worldStampCreatorLayer in Template.Creators)
                {
                    worldStampCreatorLayer.Commit(data, stamp);
                }
                data.Size = Template.Bounds.size;
                stamp.SetData(data);
                stamp.HaveHeightsBeenFlipped = true;
                
                EditorGUIUtility.PingObject(stamp);
            }
            
        }

        protected override void OnSceneGUI(SceneView sceneView)
        {
            if (Template.Terrain == null)
            {
                return;
            }

            if (DoSetArea())
            {
                foreach (var worldStampCreatorLayer in Template.Creators)
                {
                    worldStampCreatorLayer.NeedsRecapture = true;
                }
            }

            if (!Template.Creators.Contains(SceneGUIOwner))
            {
                SceneGUIOwner = null;
            }

            if (SceneGUIOwner != null)
            {
                SceneGUIOwner.PreviewInScene(this);
            }

            Handles.color = Color.white;
            Handles.DrawWireCube(Template.Bounds.center, Template.Bounds.size);
            Handles.color = Color.white.WithAlpha(.5f);
            Handles.DrawWireCube(Template.Terrain.GetPosition() + Template.Terrain.terrainData.size / 2, Template.Terrain.terrainData.size);
        }
        
        private bool DoSetArea()
        {
            var b = Template.Bounds;
            var tb = Template.Terrain.GetBounds();

            b.min = Handles.DoPositionHandle(b.min, Quaternion.identity).Flatten();
            b.max = Handles.DoPositionHandle(b.max.x0z(b.min.y), Quaternion.identity).Flatten();
            b.min = Template.Terrain.HeightmapCoordToWorldPos(Template.Terrain.WorldToHeightmapCoord(b.min, TerrainX.RoundType.Round)).Flatten();
            b.max = Template.Terrain.HeightmapCoordToWorldPos(Template.Terrain.WorldToHeightmapCoord(b.max, TerrainX.RoundType.Round)).Flatten();

            b.Encapsulate(b.center.x0z(tb.max.y));
            b.Encapsulate(b.center.x0z(tb.min.y));
            if (b != Template.Bounds)
            {
                Template.Bounds = b;
                return true;
            }

            return false;
        }
    }
}

#endif