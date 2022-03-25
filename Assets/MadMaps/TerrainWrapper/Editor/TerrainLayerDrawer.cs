using System;
using System.Collections.Generic;
using System.Linq;
using MadMaps.Common;
using MadMaps.Common.Collections;
using MadMaps.Common.GenericEditor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;

namespace MadMaps.Terrains
{
    public class MMTerrainLayerDrawer
    {
        public ReorderableList List;
        private TerrainWrapper _wrapper;

        private static GUIContent _saveContent;

        public MMTerrainLayerDrawer(TerrainWrapper wrapper)
        {
            if (wrapper == null)
            {
                return;
            }
            _wrapper = wrapper;
            List = new ReorderableList(wrapper.Layers, typeof(LayerBase), true, true, true, true);
            List.drawHeaderCallback += DrawLayerHeaderCallback;
            List.drawElementCallback += DrawLayerElementCallback;
            List.drawFooterCallback += DrawLayerFooterCallback;
            List.elementHeightCallback += LayerElementHeightCallback;
            List.onAddCallback += OnLayerAddCallback;
            List.onRemoveCallback += OnLayerRemoveCallback;

            _saveContent = new GUIContent(Resources.Load<Texture2D>("MadMaps/WorldStamp/bttSaveIcon"), "Save Asset");
        }

        private void DrawLayerHeaderCallback(Rect rect)
        {
            EditorGUI.LabelField(rect, "Layers");
        }

        private void DrawLayerFooterCallback(Rect rect)
        {
            float buttonWidth = 96;
            float xMax = rect.xMax;
            float num = xMax - 58f - buttonWidth * 1;
            rect = new Rect(num, rect.y, xMax - num, rect.height);
            Rect rect2 = new Rect(rect.xMax - 50, rect.y - 3f, 25f, 16f);
            Rect position = new Rect(xMax - 29f, rect.y - 3f, 25f, 16f);
            if (Event.current.type == EventType.Repaint)
            {
                GUIStyle footerBackground = "RL Footer";
                footerBackground.Draw(rect, false, false, false, false);
            }
            if (GUI.Button(rect2, EditorGUIUtility.IconContent("Toolbar Plus", "|Add to list"), "RL FooterButton"))
            {
                List.onAddCallback(List);
                if (List.onChangedCallback != null)
                {
                    List.onChangedCallback(List);
                }
            }
            using (new EditorGUI.DisabledScope(List.index < 0 || List.index >= List.count || (List.onCanRemoveCallback != null && !List.onCanRemoveCallback(List))))
            {
                if (GUI.Button(position, EditorGUIUtility.IconContent("Toolbar Minus", "|Remove selection from list"), "RL FooterButton"))
                {
                    List.onRemoveCallback(List);
                    if (List.onChangedCallback != null)
                    {
                        List.onChangedCallback(List);
                    }
                }
            }
            Rect reapplyRect = new Rect(rect.xMax - 50 - buttonWidth, rect.y - 3f, buttonWidth, 16f);
            if(!EditorGUIUtility.isProSkin)
            {
                GUI.color = Color.black;
            }
            if (GUI.Button(reapplyRect, new GUIContent("Reapply All", "Reapply all layers"), "RL FooterButton"))
            {
                _wrapper.ApplyAllLayers();
                EditorGUIUtility.ExitGUI();
                return;
            }
            GUI.color = Color.white;
        }

        private void OnLayerRemoveCallback(ReorderableList list)
        {
            var obj = list.list[list.index] as MMTerrainLayer;
            if (obj != null)
            {                
                bool inLevel = string.IsNullOrEmpty(AssetDatabase.GetAssetPath(obj));
                if (!EditorUtility.DisplayDialog(
                    string.Format("Are you sure you want to delete layer {0}?", obj.name),
                    inLevel
                        ? "This will DESTROY this layer's information!!!"
                        : "This layer is saved as an asset and so won't be destroyed", "Yes, Delete", "No, Go Back"))
                {
                    GUIUtility.ExitGUI();
                    return;
                }
                if (inLevel)
                {
                    UnityEngine.Object.DestroyImmediate(obj);
                }
                obj.Dispose(_wrapper, true);
                GUIUtility.ExitGUI();
            }

            list.list.RemoveAt(list.index);
        }

        private void OnLayerAddCallback(ReorderableList list)
        {
            var menu = Common.EditorGUILayoutX.GetTypeSelectionMenu(typeof (LayerBase), type =>
            {
                var newLayer = ScriptableObject.CreateInstance(type);
                int counter = 1;
                while (_wrapper.GetLayer<LayerBase>(string.Format("New Layer {0}", counter)) != null)
                {
                    counter++;
                }
                newLayer.name = string.Format("New Layer {0}", counter);
                list.list.Insert(0, newLayer);

                /*var dLayer = newLayer as DeltaLayer;
                if(dLayer)
                {
                    dLayer.BlendMode = MMTerrainLayer.EMMTerrainLayerBlendMode.Additive;
                }*/
            });
            menu.ShowAsContext();
        }

        private float LayerElementHeightCallback(int index)
        {
            return 24;
        }

        private void DrawLayerElementCallback(Rect rect, int index, bool isActive, bool isFocused)
        {
            rect.y += 2;
            rect.height -= 2;
            var layer = _wrapper.Layers[index];

            float headerHeight = 18;
            var layerNumberRect = new Rect(rect.x, rect.y, 20, headerHeight);
            EditorGUI.LabelField(layerNumberRect, (List.list.Count - index - 1).ToString());

            var nameRect = new Rect(layerNumberRect.xMax + 4, rect.y, rect.width - 120, headerHeight);
            layer.name = EditorGUI.TextField(nameRect, layer.name);

            var lockedRect = new Rect(nameRect.xMax + 2, rect.y - 3, headerHeight + 7, headerHeight + 7);
            if (GUI.Button(lockedRect, new GUIContent(layer.Locked ? GUIResources.LockedIcon : GUIResources.UnlockedIcon, "Lock Layer"), EditorStyles.label))
            {
                layer.Locked = !layer.Locked;
            }
            GUI.enabled = !layer.Locked;
            var reapplyRect = new Rect(lockedRect.xMax + 1, rect.y, headerHeight, headerHeight); 
            if (GUI.Button(reapplyRect, EditorGUIUtility.IconContent("TreeEditor.Refresh", "Reapply all components on this layer."), "RL FooterButton"))
            {
                LayerComponentApplyManager.ApplyAllLayerComponents(_wrapper, layer.name);
                GUIUtility.ExitGUI();
                return;
            }
            GUI.enabled = true;
            var dirtyRect = new Rect(reapplyRect.xMax+ 3, rect.y - 2, headerHeight + 4, headerHeight + 4);
            GUI.color = layer.Dirty ? Color.white : Color.gray.WithAlpha(.1f);
            if(GUI.Button(dirtyRect, new GUIContent(GUIResources.WarningIcon, layer.Dirty ? "This layer is dirty. Click to manually reset this." : "This layer is not dirty."), EditorStyles.label))
            {
                layer.Dirty = false;
            }
            GUI.color = Color.white;
            
            var enabledRect = new Rect(rect.xMax - 20, rect.y, 20, rect.height);
            layer.Enabled = GUI.Toggle(enabledRect, layer.Enabled, GUIContent.none);
        }


        public static LayerBase DrawExpandedGUI(TerrainWrapper wrapper, LayerBase layer)
        {
            if (layer == null)
            {
                return null;
            }

            //var deltaLayer = layer as DeltaLayer;
            var MMTerrainLayer = layer as MMTerrainLayer;
            var procLayer = layer as ProceduralLayer;
            EditorGUILayout.BeginVertical("Box");
            EditorGUI.indentLevel++;
            if (MMTerrainLayer != null)
            {
                layer = DrawExpandedGUI(wrapper, MMTerrainLayer);
            }            
            else if (procLayer != null)
            {
                GenericEditor.DrawGUI(procLayer.Components, "Components", 
                    typeof(List<ProceduralLayerComponent>), typeof(ProceduralLayer).GetField("Components"), procLayer);
            }
            else
            {
                EditorGUILayout.HelpBox(string.Format("Attempting to draw GUI for {0}, but type {1} is not implemented", 
                    layer.name, layer.GetType()), MessageType.Info);
            }
            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();
            
            return layer;
        }

        public static MMTerrainLayer DrawExpandedGUI(TerrainWrapper wrapper, MMTerrainLayer layer)
        {
            bool isInScene = layer != null && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(layer));
            EditorGUILayout.BeginHorizontal();
            layer = (MMTerrainLayer)EditorGUILayout.ObjectField(isInScene ? "Asset (In-Scene)" : "Asset", layer, typeof(MMTerrainLayer), true);
            if (layer != null && isInScene && GUILayout.Button(_saveContent, EditorStyles.label, GUILayout.Width(20), GUILayout.Height(16)))
            {
                var path = EditorUtility.SaveFilePanel("Save Terrain Layer", "Assets", layer.name, "asset");
                if (!string.IsNullOrEmpty(path))
                {
                    path = path.Substring(path.LastIndexOf("Assets/", StringComparison.Ordinal));
                    AssetDatabase.CreateAsset(layer, path);
                }
            }
            EditorGUILayout.EndHorizontal();
            if (layer == null)
            {
                EditorGUILayout.HelpBox("Snapshot is Null!", MessageType.Error);
                if (GUILayout.Button("Create Snapshot", EditorStyles.toolbarButton))
                {
                    layer = ScriptableObject.CreateInstance<MMTerrainLayer>();
                    GUIUtility.ExitGUI();
                    return layer;
                }
                EditorGUILayout.EndVertical();
                return layer;
            }

            EditorGUILayout.BeginHorizontal();
            var previewContent = new GUIContent(GUIResources.EyeOpenIcon, "Preview");
            EditorGUILayout.LabelField("Height:", layer.Heights != null ? string.Format("{0}x{1}", layer.Heights.Width, layer.Heights.Height) : "null");
            if (layer.Heights != null && GUILayout.Button(previewContent, EditorStyles.label, GUILayout.Width(20), GUILayout.Height(16)))
            {
                DataInspector.SetData(layer.Heights);
            }
            if (GUILayout.Button(GenericEditor.DeleteContent, EditorStyles.label, GUILayout.Width(20)) && EditorUtility.DisplayDialog("Really Clear?", "", "Yes", "No"))
            {
                layer.Heights.Clear();
                EditorUtility.SetDirty(layer);
                EditorGUIUtility.ExitGUI();
                return layer;
            }
            EditorGUILayout.EndHorizontal();
            EditorExtensions.Seperator();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Splats", layer.SplatData != null ? string.Format("{0}", layer.SplatData.Count) : "null");
            if (layer.SplatData != null && GUILayout.Button(previewContent, EditorStyles.label, GUILayout.Width(20), GUILayout.Height(16)))
            {
                List<IDataInspectorProvider> data = new List<IDataInspectorProvider>();
                List<object> context = new List<object>();
                foreach (var keyValuePair in layer.SplatData)
                {
                    data.Add(keyValuePair.Value);
                    context.Add(keyValuePair.Key);
                }
                DataInspector.SetData(data, context);
            }
            if (GUILayout.Button(GenericEditor.DeleteContent, EditorStyles.label, GUILayout.Width(20)) && EditorUtility.DisplayDialog("Really Clear?", "", "Yes", "No"))
            {
                layer.SplatData.Clear();
                EditorUtility.SetDirty(layer);
                EditorGUIUtility.ExitGUI();
                return layer;
            }
            EditorGUILayout.EndHorizontal();
            EditorExtensions.Seperator();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Details", layer.DetailData != null ? string.Format("{0}", layer.DetailData.Count) : "null");
            if (layer.DetailData != null && GUILayout.Button(previewContent, EditorStyles.label, GUILayout.Width(20), GUILayout.Height(16)))
            {
                List<IDataInspectorProvider> data = new List<IDataInspectorProvider>();
                List<object> context = new List<object>();
                foreach (var keyValuePair in layer.DetailData)
                {
                    data.Add(keyValuePair.Value);
                    context.Add(keyValuePair.Key);
                }
                DataInspector.SetData(data, context, true);
            }
            if (GUILayout.Button(GenericEditor.DeleteContent, EditorStyles.label, GUILayout.Width(20)) && EditorUtility.DisplayDialog("Really Clear?", "", "Yes", "No"))
            {
                layer.DetailData.Clear();
                EditorUtility.SetDirty(layer);
                EditorGUIUtility.ExitGUI();
                return layer;
            }
            EditorGUILayout.EndHorizontal();
            EditorExtensions.Seperator();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Trees", layer.Trees != null ? string.Format("{0}", layer.Trees.Count) : "null");
            if (layer.Trees != null && GUILayout.Button(previewContent, EditorStyles.label, GUILayout.Width(20), GUILayout.Height(16)))
            {
                Dictionary<object, IDataInspectorProvider> data = new Dictionary<object, IDataInspectorProvider>();
                foreach (var tree in layer.Trees)
                {
                    if(!data.ContainsKey(tree.Prototype))
                    {
                        data[tree.Prototype] = new PositionList();
                    }
                    (data[tree.Prototype] as PositionList).Add(tree.Position);
                }
                DataInspector.SetData(data.Values.ToList(), data.Keys.ToList(), true);
            }
            if (GUILayout.Button(GenericEditor.DeleteContent, EditorStyles.label, GUILayout.Width(20)) && EditorUtility.DisplayDialog("Really Clear?", "", "Yes", "No"))
            {
                layer.Trees.Clear();
                EditorUtility.SetDirty(layer);
                EditorGUIUtility.ExitGUI();
                return layer;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("➥Removals", layer.TreeRemovals != null ? string.Format("{0}", layer.TreeRemovals.Count) : "null");
            if (layer.TreeRemovals != null && GUILayout.Button(previewContent, EditorStyles.label, GUILayout.Width(20), GUILayout.Height(16)))
            {
                Dictionary<object, IDataInspectorProvider> data = new Dictionary<object, IDataInspectorProvider>();
                var compoundTrees = wrapper.GetCompoundTrees(layer, false);
                compoundTrees.AddRange(layer.Trees);
                foreach (var guid in layer.TreeRemovals)
                {
                    MadMapsTreeInstance tree = null;
                    foreach(var t in compoundTrees)
                    {
                        if(t.Guid == guid)
                        {
                            tree = t;
                            break;
                        }
                    }
                    if(tree == null)
                    {
                        Debug.LogError("Couldn't find tree removal " + guid, layer);
                        continue;
                    }
                    if(!data.ContainsKey(tree.Prototype))
                    {
                        data[tree.Prototype] = new PositionList();
                    }
                    (data[tree.Prototype] as PositionList).Add(tree.Position);
                }
                DataInspector.SetData(data.Values.ToList(), data.Keys.ToList(), true);
            }
            if (GUILayout.Button(GenericEditor.DeleteContent, EditorStyles.label, GUILayout.Width(20)) && EditorUtility.DisplayDialog("Really Clear?", "", "Yes", "No"))
            {
                layer.TreeRemovals.Clear();
                EditorUtility.SetDirty(layer);
                EditorGUIUtility.ExitGUI();
                return layer;
            }
            EditorGUILayout.EndHorizontal();
            EditorExtensions.Seperator();

            #if VEGETATION_STUDIO
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Vegetation Studio", layer.VSInstances != null ? string.Format("{0}", layer.VSInstances.Count) : "null");
            if (layer.VSInstances != null && GUILayout.Button(previewContent, EditorStyles.label, GUILayout.Width(20), GUILayout.Height(16)))
            {
                Dictionary<object, IDataInspectorProvider> data = new Dictionary<object, IDataInspectorProvider>();
                foreach (var tree in layer.VSInstances)
                {
                    if(!data.ContainsKey(tree.VSID))
                    {
                        data[tree.VSID] = new PositionList();
                    }
                    (data[tree.VSID] as PositionList).Add(tree.Position);
                }
                DataInspector.SetData(data.Values.ToList(), data.Keys.ToList(), true);
            }
            if (GUILayout.Button(GenericEditor.DeleteContent, EditorStyles.label, GUILayout.Width(20)) && EditorUtility.DisplayDialog("Really Clear?", "", "Yes", "No"))
            {
                layer.VSInstances.Clear();
                EditorUtility.SetDirty(layer);
                EditorGUIUtility.ExitGUI();
                return layer;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("➥Removals", layer.VSRemovals != null ? string.Format("{0}", layer.VSRemovals.Count) : "null");
            if (layer.VSRemovals != null && GUILayout.Button(previewContent, EditorStyles.label, GUILayout.Width(20), GUILayout.Height(16)))
            {
                Dictionary<object, IDataInspectorProvider> data = new Dictionary<object, IDataInspectorProvider>();
                var compoundVS = wrapper.GetCompoundVegetationStudioData(layer, false);
                compoundVS.AddRange(layer.VSInstances);
                foreach (var guid in layer.VSRemovals)
                {
                    VegetationStudioInstance vsInstance = null;
                    foreach(var t in compoundVS)
                    {
                        if(t.Guid == guid)
                        {
                            vsInstance = t;
                            break;
                        }
                    }
                    if(vsInstance == null)
                    {
                        Debug.LogError("Couldn't find VS removal " + guid, layer);
                        continue;
                    }
                    if(!data.ContainsKey(vsInstance.VSID))
                    {
                        data[vsInstance.VSID] = new PositionList();
                    }
                    (data[vsInstance.VSID] as PositionList).Add(vsInstance.Position);
                }
                DataInspector.SetData(data.Values.ToList(), data.Keys.ToList(), true);
            }
            if (GUILayout.Button(GenericEditor.DeleteContent, EditorStyles.label, GUILayout.Width(20)) && EditorUtility.DisplayDialog("Really Clear?", "", "Yes", "No"))
            {
                layer.VSRemovals.Clear();
                EditorUtility.SetDirty(layer);
                EditorGUIUtility.ExitGUI();
                return layer;
            }
            EditorGUILayout.EndHorizontal();
            EditorExtensions.Seperator();
            #endif

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Objects", layer.Objects != null ? string.Format("{0}", layer.Objects.Count) : "null");
            if (layer.Objects != null && GUILayout.Button(previewContent, EditorStyles.label, GUILayout.Width(20), GUILayout.Height(16)))
            {
                Dictionary<object, IDataInspectorProvider> data = new Dictionary<object, IDataInspectorProvider>();
                foreach (var obj in layer.Objects)
                {
                    if(!data.ContainsKey(obj.Prefab))
                    {
                        data[obj.Prefab] = new PositionList();
                    }
                    (data[obj.Prefab] as PositionList).Add(obj.Position);
                }
                DataInspector.SetData(data.Values.ToList(), data.Keys.ToList(), true);
            }
            if (GUILayout.Button(GenericEditor.DeleteContent, EditorStyles.label, GUILayout.Width(20)) && EditorUtility.DisplayDialog("Really Clear?", "", "Yes", "No"))
            {
                layer.Objects.Clear();
                EditorUtility.SetDirty(layer);
                EditorGUIUtility.ExitGUI();
                return layer;
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("➥Removals", layer.ObjectRemovals != null ? string.Format("{0}", layer.ObjectRemovals.Count) : "null");
            if (layer.ObjectRemovals != null && GUILayout.Button(previewContent, EditorStyles.label, GUILayout.Width(20), GUILayout.Height(16)))
            {
                Dictionary<object, IDataInspectorProvider> data = new Dictionary<object, IDataInspectorProvider>();
                var compoundObjs = wrapper.GetCompoundObjects(layer, false);
                compoundObjs.AddRange(layer.Objects);
                foreach (var guid in layer.ObjectRemovals)
                {
                    WorldStamps.PrefabObjectData? obj = null;
                    foreach(var t in compoundObjs)
                    {
                        if(t.Guid == guid)
                        {
                            obj = t;
                            break;
                        }
                    }
                    if(obj == null)
                    {
                        Debug.LogError("Couldn't find object removal " + guid, layer);
                        continue;
                    }
                    if(!data.ContainsKey(obj.Value.Prefab))
                    {
                        data[obj.Value.Prefab] = new PositionList();
                    }
                    (data[obj.Value.Prefab] as PositionList).Add(obj.Value.Position);
                }
                DataInspector.SetData(data.Values.ToList(), data.Keys.ToList(), true);
            }
            if (GUILayout.Button(GenericEditor.DeleteContent, EditorStyles.label, GUILayout.Width(20)) && EditorUtility.DisplayDialog("Really Clear?", "", "Yes", "No"))
            {
                layer.ObjectRemovals.Clear();
                EditorUtility.SetDirty(layer);
                EditorGUIUtility.ExitGUI();
                return layer;
            }
            EditorGUILayout.EndHorizontal();
            EditorExtensions.Seperator();

            EditorGUILayout.BeginHorizontal();
            bool hasStencil = layer.Stencil != null && layer.Stencil.Width > 0 && layer.Stencil.Height > 0;;
            EditorGUILayout.LabelField("Stencil" + (hasStencil ? "" : " (null)"), layer.Stencil != null ? string.Format("{0}", string.Format("{0}x{1}", layer.Stencil.Width, layer.Stencil.Height)) : "null");
            
            GUI.enabled = hasStencil;
            if (GUILayout.Button(EditorGUIUtility.IconContent("Terrain Icon"), EditorStyles.label, GUILayout.Width(20), GUILayout.Height(16)))
            {
                TerrainWrapperGUI.StencilLayerDisplay = layer;
            }
            if (GUILayout.Button(previewContent, EditorStyles.label, GUILayout.Width(20), GUILayout.Height(16)))
            {
                DataInspector.SetData(layer.Stencil);
            }
            GUI.enabled = true;
            if (GUILayout.Button(GenericEditor.DeleteContent, EditorStyles.label, GUILayout.Width(20)) && EditorUtility.DisplayDialog("Really Clear?", "", "Yes", "No"))
            {
                layer.Stencil.Clear();
                EditorUtility.SetDirty(layer);
                EditorGUIUtility.ExitGUI();
                return layer;
            }
            EditorGUILayout.EndHorizontal();

            EditorExtensions.Seperator();

            EditorGUILayout.LabelField("Layer Commands", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Capture From Terrain"))
            {
                if (EditorUtility.DisplayDialog(string.Format("Capture Layer {0} from current Terrain?", layer.name), "This will clear any existing data!",
                    "Yes", "No"))
                {
                    layer.SnapshotTerrain(wrapper.Terrain);
                    EditorUtility.SetDirty(layer);
                    EditorUtility.SetDirty(wrapper.Terrain);
                    EditorSceneManager.MarkAllScenesDirty();
                    AssetDatabase.SaveAssets();
                    EditorUtility.SetDirty(layer);
                    EditorGUIUtility.ExitGUI();
                    return layer;
                }
            }
            EditorGUILayout.Space();
            if (GUILayout.Button("Apply To Terrain"))
            {
                if(wrapper.PrepareApply())
                {
                    layer.WriteToTerrain(wrapper);
                    wrapper.FinaliseApply();

                    EditorUtility.SetDirty(layer);
                    EditorSceneManager.MarkAllScenesDirty();

                    EditorGUIUtility.ExitGUI();
                    return layer;
                }                
            }
            EditorGUILayout.Space();
            if (GUILayout.Button("Clear Layer"))
            {
                if (EditorUtility.DisplayDialog(string.Format("Clear Layer {0}?", layer.name), "This will clear any existing data!",
                    "Yes", "No"))
                {
                    layer.Clear(wrapper);
                    EditorUtility.SetDirty(wrapper.Terrain);
                    EditorUtility.SetDirty(layer);
                    EditorSceneManager.MarkAllScenesDirty();
                    EditorGUIUtility.ExitGUI();
                    return layer;
                }
                
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            
            return layer;
        }
    }
}

