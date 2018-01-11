﻿using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;
using Dingo.Common;
using Dingo.Common.Painter;

namespace Dingo.Terrains
{
    [CustomEditor(typeof(TerrainWrapper))]
    public class TerrainWrapperGUI : Editor
    {
        public static TerrainLayer StencilLayerDisplay
        {
            get { return __stencilLayerDisplay; }
            set
            {
                __stencilLayerDisplay = value;
                _stencilDisplayDirty = true;
            }
        }
        public static TerrainLayer __stencilLayerDisplay;
        public static bool _stencilDisplayDirty;
        
        public int CurrentTab;
        public bool IsPopout;

        public TerrainWrapper Wrapper;

        private TerrainLayerDrawer _layerDrawer;
        private TerrainSplatsDrawer _splatsDrawer;
        private TerrainDetailsDrawer _detailsDrawer;
        private static GUIContent[] _tabs;

        public void OnEnable()
        {
            if (!Wrapper)
            {
                return;
            }
            _layerDrawer = new TerrainLayerDrawer(Wrapper);
            _splatsDrawer = new TerrainSplatsDrawer(Wrapper);
            _detailsDrawer = new TerrainDetailsDrawer(Wrapper);
        }

        void OnDisable()
        {
            StencilLayerDisplay = null;
        }

        public override void OnInspectorGUI()
        {
            if (Wrapper == null)
            {
                Wrapper = target as TerrainWrapper;
                return;
            }

            if (!IsPopout && GUILayout.Button("Popout Inspector"))
            {
                var w = EditorWindow.GetWindow<TerrainWrapperEditorWindow>();
                w.Wrapper = Wrapper;
                Selection.objects = new Object[0];
                return;
            }

            for (int i = Wrapper.Layers.Count - 1; i >= 0; i--)
            {
                if (!Wrapper.Layers[i])
                {
                    Wrapper.Layers.RemoveAt(i);
                }
            }

            if (_tabs == null)
            {
                _tabs = new[]
                {
                    new GUIContent("Layers") {image = EditorGUIUtility.FindTexture("Terrain Icon")}, 
                    new GUIContent("Splats") {image = EditorGUIUtility.FindTexture("TerrainInspector.TerrainToolSplat")},
                    new GUIContent("Details") {image = EditorGUIUtility.FindTexture("TerrainInspector.TerrainToolPlants")},
                    new GUIContent("Info") {image = EditorGUIUtility.FindTexture("_Help")},
                };
            }

            CurrentTab = GUILayout.Toolbar(CurrentTab, _tabs, GUILayout.Height(20));
            var currentTabTitle = _tabs[CurrentTab].text;
            if (currentTabTitle == "Layers")
            {
                EditorGUILayout.Space();
                TerrainWrapper.ComputeShaders = EditorGUILayout.Toggle("Compute Shaders (Experimental)", TerrainWrapper.ComputeShaders);
                EditorGUILayout.Space();
                _layerDrawer.List.DoLayoutList();
                if (_layerDrawer.List.index >= 0 && Wrapper.Layers.Count > 0 && _layerDrawer.List.index < Wrapper.Layers.Count)
                {
                    var selected = Wrapper.Layers[_layerDrawer.List.index];
                    Wrapper.Layers[_layerDrawer.List.index] = TerrainLayerDrawer.DrawExpandedGUI(Wrapper, selected);
                }
                else
                {
                    EditorGUILayout.HelpBox("Select a Layer to see information about it here.", MessageType.Info);
                }
                EditorGUILayout.Space();
            }
            else if (currentTabTitle == "Splats")
            { 
                _splatsDrawer.List.DoLayoutList();
            }
            else if (currentTabTitle == "Details")
            {
                _detailsDrawer.List.DoLayoutList();
            }
            else if (currentTabTitle == "Info")
            {
                Wrapper.WriteHeights = EditorGUILayout.Toggle("Write Heights", Wrapper.WriteHeights);
                Wrapper.WriteSplats = EditorGUILayout.Toggle("Write Splats", Wrapper.WriteSplats);
                Wrapper.WriteTrees = EditorGUILayout.Toggle("Write Trees", Wrapper.WriteTrees);
                Wrapper.WriteDetails = EditorGUILayout.Toggle("Write Details", Wrapper.WriteDetails);
                Wrapper.WriteObjects = EditorGUILayout.Toggle("Write Objects", Wrapper.WriteObjects);

                EditorExtensions.Seperator();

                var previewContent = EditorGUIUtility.IconContent("ClothInspector.ViewValue");

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Compound Heights", Wrapper.CompoundTerrainData.Heights != null ? string.Format("{0}", string.Format("{0}x{1}", Wrapper.CompoundTerrainData.Heights.Width, Wrapper.CompoundTerrainData.Heights.Height)) : "null");
                if (Wrapper.CompoundTerrainData.SplatData != null && GUILayout.Button(previewContent, EditorStyles.label, GUILayout.Width(20), GUILayout.Height(16)))
                {
                    DataInspector.SetData(Wrapper.CompoundTerrainData.Heights);
                }
                EditorGUILayout.EndHorizontal();
                EditorExtensions.Seperator();
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Compound Splats", Wrapper.CompoundTerrainData.SplatData != null ? string.Format("{0}", Wrapper.CompoundTerrainData.SplatData.Count) : "null");
                if (Wrapper.CompoundTerrainData.SplatData != null && GUILayout.Button(previewContent, EditorStyles.label, GUILayout.Width(20), GUILayout.Height(16)))
                {
                    DataInspector.SetData(Wrapper.CompoundTerrainData.SplatData.GetValues(), Wrapper.CompoundTerrainData.SplatData.GetKeys());
                }
                EditorGUILayout.EndHorizontal();
                EditorExtensions.Seperator();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Compound Details", Wrapper.CompoundTerrainData.DetailData != null ? string.Format("{0}", Wrapper.CompoundTerrainData.DetailData.Count) : "null");
                if (Wrapper.CompoundTerrainData.DetailData != null && GUILayout.Button(previewContent, EditorStyles.label, GUILayout.Width(20), GUILayout.Height(16)))
                {
                    DataInspector.SetData(Wrapper.CompoundTerrainData.DetailData.GetValues(), Wrapper.CompoundTerrainData.DetailData.GetKeys());
                }
                EditorGUILayout.EndHorizontal();
                EditorExtensions.Seperator();

                EditorGUILayout.LabelField("Compound Objects: ", Wrapper.CompoundTerrainData.Objects.Count.ToString());
                EditorExtensions.Seperator();

                EditorGUILayout.LabelField("Compound Trees: ", Wrapper.CompoundTerrainData.Trees.Count.ToString());
                EditorExtensions.Seperator();

                Wrapper.CullYNormal = EditorGUILayout.FloatField("Y Cull", Wrapper.CullYNormal);
            }
        }
        
        void OnSceneGUI()
        {
            if (StencilLayerDisplay == null)
            {
                return;
            }

            EditorCellHelper.SetAlive();
            if (!_stencilDisplayDirty)
            {
                return;
            }

            var stencil = StencilLayerDisplay.Stencil;
            if (stencil == null)
            {
                return;
            }

            _stencilDisplayDirty = false;

            var wrapper = Wrapper;
            EditorCellHelper.Clear(false);

            int step = wrapper.Terrain.terrainData.heightmapResolution / 256;
            EditorCellHelper.TRS = Matrix4x4.identity;
            EditorCellHelper.CellSize = (wrapper.Terrain.terrainData.size.x/
                                        (float)wrapper.Terrain.terrainData.heightmapResolution) * step;
            //int counter = 0;
            for (var u = 0; u < stencil.Width; u += step)
            {
                for (var v = 0; v < stencil.Height; v += step)
                {
                    var wPos = wrapper.Terrain.HeightmapCoordToWorldPos(new Common.Coord(u, v)).xz().x0z(50);
                    var stencilPos = new Vector2(u/(float) stencil.Width, v/(float) stencil.Height);

                    var stencilKey = Mathf.FloorToInt(stencil.BilinearSample(stencilPos));
                    var strength = StencilLayerDisplay.GetStencilStrength(stencilPos);
                    
                    var stencilColor = ColorUtils.GetIndexColor(stencilKey);
                    if (stencilKey <= 0)
                    {
                        stencilColor = Color.black;
                        strength = 0;
                    }
                    EditorCellHelper.AddCell(wPos, Color.Lerp(Color.black, stencilColor, strength));
                    //counter++;
                }
            }
            EditorCellHelper.Invalidate();
        }

    }
}
