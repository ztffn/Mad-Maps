﻿using System.Collections.Generic;
using System.Linq;
using MadMaps.Common;
using MadMaps.Common.Painter;
using MadMaps.Terrains;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace MadMaps.WorldStamp
{
    [CustomEditor(typeof (WorldStamp))]
    [CanEditMultipleObjects]
    public class WorldStampGUI : Editor
    {
        BoxBoundsHandle _boxBoundsHandle = new BoxBoundsHandle(-1);
        private SerializedProperty _size;
        private SerializedProperty _snapRotation;
        private SerializedProperty _snapPosition;
        private SerializedProperty _snapToTerrain;
        private SerializedProperty _snapToTerrainOffset;
        private SerializedProperty _priority;
        private SerializedProperty _layerName;
        private SerializedProperty _previewEnabled;
        private SerializedProperty _writeStencil;
        private SerializedProperty _gizmoColor, _gizmosEnabled;

        [SerializeField]
        private bool _heightsExpanded, _treesExpanded, _objectsExpanded, _splatsExpanded, _detailsExpanded;
        
        // Heights
        private SerializedProperty _heightsEnabled;
        private SerializedProperty _layerHeightBlendMode;
        private SerializedProperty _heightMin;
        private SerializedProperty _heightOffset;
        //private SerializedProperty _baseHeightBlendMode;

        // Splats
        private SerializedProperty _splatsEnabled;
        //private SerializedProperty _copyBaseSplats;
        private SerializedProperty _stencilSplats;
        private SerializedProperty _splatBlendMode;

        // Trees
        private SerializedProperty _treesEnabled;
        private SerializedProperty _removeTreesWithStencil;
        private SerializedProperty _removeBaseTrees;
        private SerializedProperty _removeSameLayerTrees;

        // Details
        private SerializedProperty _detailsEnabled;
        private SerializedProperty _detailBlendMode;
        private SerializedProperty _detailsBoost;

        // Objects
        private SerializedProperty _objectsEnabled;
        private SerializedProperty _removeObjectsWithStencil;
        private SerializedProperty _removeBaseObjects;
        private SerializedProperty _overrideRelativeObjectMode;
        private SerializedProperty _relativeObjectMode;

        private bool _editingMask = false;
        private Painter _painter;

        [MenuItem("Tools/Compress World Stamps")]
        public static void CompressAll()
        {
            var allStamps = sFinder.FindComponentInPrefabs.FindComponentsInPrefab<WorldStamp>();
            foreach (var ws in allStamps)
            {
                ws.Data.Heights.ForceDirty();
                foreach (var compressedSplatData in ws.Data.SplatData)
                {
                    compressedSplatData.Data.ForceDirty();
                }
                foreach (var compressedDetailData in ws.Data.DetailData)
                {
                    compressedDetailData.Data.ForceDirty();
                }
                EditorUtility.SetDirty(ws);
                EditorUtility.SetDirty(ws.gameObject);
            }
            Debug.LogFormat("Compressed {0} stamps", allStamps.Count);
        }

        void OnEnable()
        {
            _size = serializedObject.FindProperty("Size");
            _writeStencil = serializedObject.FindProperty("WriteStencil");
            _snapRotation = serializedObject.FindProperty("SnapRotation");
            _snapPosition = serializedObject.FindProperty("SnapPosition");
            _snapToTerrain = serializedObject.FindProperty("SnapToTerrainHeight");
            _snapToTerrainOffset = serializedObject.FindProperty("SnapToTerrainHeightOffset");
            _priority = serializedObject.FindProperty("Priority");
            _layerHeightBlendMode = serializedObject.FindProperty("LayerHeightBlendMode");
            _heightMin = serializedObject.FindProperty("MinHeight");
            _splatBlendMode = serializedObject.FindProperty("SplatBlendMode");
            _stencilSplats = serializedObject.FindProperty("StencilSplats");
            _removeBaseTrees = serializedObject.FindProperty("RemoveBaseTrees");
            _removeSameLayerTrees = serializedObject.FindProperty("RemoveSameLayerTrees");
            _removeTreesWithStencil = serializedObject.FindProperty("StencilTrees");
            _removeBaseObjects = serializedObject.FindProperty("RemoveBaseObjects");
            _removeObjectsWithStencil = serializedObject.FindProperty("StencilObjects");
            _heightsEnabled = serializedObject.FindProperty("WriteHeights");
            _heightOffset = serializedObject.FindProperty("HeightOffset");
            _treesEnabled = serializedObject.FindProperty("WriteTrees");
            _objectsEnabled = serializedObject.FindProperty("WriteObjects");
            _splatsEnabled = serializedObject.FindProperty("WriteSplats");
            _previewEnabled = serializedObject.FindProperty("PreviewEnabled");
            _layerName = serializedObject.FindProperty("LayerName");
            _gizmosEnabled = serializedObject.FindProperty("GizmosEnabled");
            _gizmoColor = serializedObject.FindProperty("GizmoColor");
            _detailsEnabled = serializedObject.FindProperty("WriteDetails");
            _detailBlendMode = serializedObject.FindProperty("DetailBlendMode");
            _detailsBoost = serializedObject.FindProperty("DetailBoost");
            _overrideRelativeObjectMode = serializedObject.FindProperty("OverrideObjectRelativeMode");
            _relativeObjectMode = serializedObject.FindProperty("RelativeMode");
        }

        void DoHeader(string text, ref bool expanded, SerializedProperty enabled, bool canEnable)
        {
            EditorGUILayout.BeginHorizontal();
            expanded = EditorGUILayout.Foldout(expanded, text);
            GUI.enabled = canEnable;
            EditorGUILayout.PropertyField(enabled, GUIContent.none, GUILayout.Width(20));
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }

        public override void OnInspectorGUI()
        {
            if (targets.Length == 1)
            {
                DoSingleInstanceInfo();
            }
            else
            {
                _editingMask = false;
            }

            foreach (var o in targets)
            {
                var ws = o as WorldStamp;
                ws.Validate();
            }

            if (_editingMask)
            {
                GUI.enabled = false;
                _previewEnabled.boolValue = false;
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical();
            GUI.enabled = !_editingMask && (!_layerName.hasMultipleDifferentValues);
            if (GUILayout.Button(string.Format("Stamp Layer '{0}'", _layerName.stringValue)))
            {
                StampAll(_layerName.stringValue);
            }
            GUI.enabled = !_editingMask;
            if (GUILayout.Button("Stamp All"))
            {
                StampAll();
            }
            EditorGUILayout.EndVertical();

            serializedObject.Update();

            EditorGUILayout.BeginVertical(GUILayout.Width(100));
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Recalculate Terrain", GUILayout.Width(150));
            EditorPrefs.SetBool("worldStamp_DirtyOnStamp", EditorGUILayout.Toggle(EditorPrefs.GetBool("worldStamp_DirtyOnStamp", true), GUILayout.Width(30)));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Gizmos Enabled", GUILayout.Width(150));
            EditorGUILayout.PropertyField(_gizmosEnabled, GUIContent.none, GUILayout.Width(30));
            if (GUILayout.Button("All Off", EditorStyles.toolbarButton, GUILayout.Width(64)))
            {
                var all = FindObjectsOfType<WorldStamp>();
                foreach (var worldStamp in all)
                {
                    worldStamp.GizmosEnabled = false;
                    EditorUtility.SetDirty(worldStamp);
                }
                SceneView.RepaintAll();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(_gizmoColor, GUILayout.Width(180));

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            
            // Scale
            EditorGUILayout.BeginHorizontal();
            _size = serializedObject.FindProperty("Size");
            EditorGUILayout.PropertyField(_size);
            GUI.enabled = !_editingMask && (_size.hasMultipleDifferentValues ? true : _size.vector3Value != (target as WorldStamp).Data.Size);
            if (GUILayout.Button("Reset", GUILayout.Width(70)))
            {
                foreach (var obj in targets)
                {
                    var worldStamp = obj as WorldStamp;
                    worldStamp.Size = worldStamp.Data.Size;
                }
                _size = serializedObject.FindProperty("Size");
                SceneView.RepaintAll();
            }
            GUI.enabled = !_editingMask;
            EditorGUILayout.EndHorizontal();

            /*EditorGUILayout.PropertyField(_snapPosition);
            EditorGUILayout.PropertyField(_snapRotation, new GUIContent("Snap Rotation (90\u00B0)"));*/
            EditorGUILayout.PropertyField(_snapToTerrain);
            EditorGUILayout.PropertyField(_snapToTerrainOffset);

            EditorGUILayout.PropertyField(_layerName);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(_previewEnabled);
            if (GUILayout.Button("All Off", EditorStyles.toolbarButton, GUILayout.Width(64)))
            {
                var all = FindObjectsOfType<WorldStamp>();
                foreach (var worldStamp in all)
                {
                    worldStamp.PreviewEnabled = false;
                    EditorUtility.SetDirty(worldStamp);
                }
                SceneView.RepaintAll();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(_priority, new GUIContent("Priority (Lower = Earlier)"));
            var priorityEditContent = EditorGUIUtility.IconContent("editicon.sml");
            priorityEditContent.tooltip = "Open Priority Editor Window";
            if (GUILayout.Button(priorityEditContent, EditorStyles.toolbarButton, GUILayout.Height(20), GUILayout.Width(20)))
            {
                EditorWindow.GetWindow<WorldStampPriorityEditorWindow>().titleContent = new GUIContent("World Stamp Priority Editor");
            }
            EditorGUILayout.EndHorizontal();

            EditorExtensions.Seperator();

            EditorGUILayout.PropertyField(_writeStencil);

            DoHeightsSection();

            DoTreesSection();

            DoObjectsSection();

            DoSplatsSection();

            DoDetailsSection();

            serializedObject.ApplyModifiedProperties();
            GUI.enabled = true;
        }

        private void DoDetailsSection()
        {
            WorldStamp singleInstance = targets.Length == 1 ? target as WorldStamp : null;
            bool canWrite = !singleInstance ||
                            (singleInstance.Data.DetailData != null && singleInstance.Data.DetailData.Count > 0);
            DoHeader("Details", ref _detailsExpanded, _detailsEnabled, canWrite);
            if (_detailsExpanded)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(_detailBlendMode);
                EditorGUILayout.PropertyField(_detailsBoost);

                if (targets.Length > 1)
                {
                    EditorGUILayout.HelpBox("Can't Edit Multiple Splat Ignores", MessageType.Info);
                }
                else
                {
                    var stamp = target as WorldStamp;
                    foreach (var keyValuePair in stamp.Data.DetailData)
                    {
                        var detailPrototypeWrapper = keyValuePair.Wrapper;
                        var ignored = stamp.IgnoredDetails.Contains(detailPrototypeWrapper);
                        GUILayout.BeginHorizontal();

                        GUI.color = ignored ? Color.red : Color.green;
                        var newPicked = (DetailPrototypeWrapper)EditorGUILayout.ObjectField(detailPrototypeWrapper, typeof(DetailPrototypeWrapper), false);
                        if (newPicked != null && newPicked != detailPrototypeWrapper)
                        {
                            keyValuePair.Wrapper = newPicked;
                            EditorUtility.SetDirty(stamp);
                            var prefab = PrefabUtility.GetPrefabObject(stamp);
                            if (prefab)
                            {
                                EditorUtility.SetDirty(prefab);
                            }
                        }
                        if (EditorGUILayoutX.IndentedButton(ignored ? "Unmute" : "Mute"))
                        {
                            if (stamp.IgnoredDetails.Contains(detailPrototypeWrapper))
                            {
                                stamp.IgnoredDetails.Remove(detailPrototypeWrapper);
                            }
                            else
                            {
                                stamp.IgnoredDetails.Add(detailPrototypeWrapper);
                            }
                        }
                        GUILayout.EndHorizontal();
                    }
                    GUI.color = Color.white;
                }
                EditorGUI.indentLevel--;
            }
        }

        private void DoSplatsSection()
        {
            WorldStamp singleInstance = targets.Length == 1 ? target as WorldStamp : null;
            bool canWrite = !singleInstance ||
                            (singleInstance.Data.SplatData != null && singleInstance.Data.SplatData.Count > 0);
            DoHeader("Splats", ref _splatsExpanded, _splatsEnabled, canWrite);
            if (_splatsExpanded)
            {
                EditorGUI.indentLevel++;

                GUI.enabled = canWrite;
                EditorGUILayout.PropertyField(_stencilSplats);
                EditorGUILayout.PropertyField(_splatBlendMode);

                if (targets.Length > 1)
                {
                    EditorGUILayout.HelpBox("Can't Edit Multiple Splat Ignores", MessageType.Info);
                }
                else
                {
                    var stamp = target as WorldStamp;
                    foreach (var keyValuePair in stamp.Data.SplatData)
                    {
                        var splatWrapper = keyValuePair.Wrapper;
                        var ignored = stamp.IgnoredSplats.Contains(splatWrapper);
                        GUILayout.BeginHorizontal();
                        GUI.color = ignored ? Color.red : Color.green;
                        var newPicked = (SplatPrototypeWrapper)EditorGUILayout.ObjectField(splatWrapper, typeof(SplatPrototypeWrapper), false);
                        if (newPicked != null && newPicked != splatWrapper)
                        {
                            keyValuePair.Wrapper = newPicked;
                            EditorUtility.SetDirty(stamp);
                            var prefab = PrefabUtility.GetPrefabObject(stamp);
                            if (prefab)
                            {
                                EditorUtility.SetDirty(prefab);
                            }
                        }
                        if (EditorGUILayoutX.IndentedButton(ignored ? "Unmute" : "Mute"))
                        {
                            if (stamp.IgnoredSplats.Contains(splatWrapper))
                            {
                                stamp.IgnoredSplats.Remove(splatWrapper);
                            }
                            else
                            {
                                stamp.IgnoredSplats.Add(splatWrapper);
                            }
                        }
                        GUILayout.EndHorizontal();
                    }
                    GUI.color = Color.white;
                }
                GUI.enabled = true;
                EditorGUI.indentLevel--;
            }
        }

        private void DoObjectsSection()
        {
            WorldStamp singleInstance = targets.Length == 1 ? target as WorldStamp : null;
            bool canWrite = !singleInstance || singleInstance.Data.Objects.Count > 0;
            DoHeader("Objects", ref _objectsExpanded, _objectsEnabled, canWrite);
            if (_objectsExpanded)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_removeBaseObjects);
                GUI.enabled = canWrite;
                EditorGUILayout.PropertyField(_removeObjectsWithStencil);
                EditorGUILayout.PropertyField(_overrideRelativeObjectMode);
                if (_overrideRelativeObjectMode.boolValue)
                {
                    EditorGUILayout.PropertyField(_relativeObjectMode);
                }
                GUI.enabled = true;
                EditorGUI.indentLevel--;
            }
        }

        private void DoTreesSection()
        {
            WorldStamp singleInstance = targets.Length == 1 ? target as WorldStamp : null;
            bool canWrite = !singleInstance || singleInstance.Data.Trees.Count > 0;
            DoHeader("Trees", ref _treesExpanded, _treesEnabled, canWrite);
            if (_treesExpanded)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_removeBaseTrees);
                EditorGUILayout.PropertyField(_removeSameLayerTrees);
                GUI.enabled = canWrite;
                EditorGUILayout.PropertyField(_removeTreesWithStencil);
                GUI.enabled = true;
                EditorGUI.indentLevel--;
            }
        }

        private void DoHeightsSection()
        {
            WorldStamp singleInstance = targets.Length == 1 ? target as WorldStamp : null;
            bool canEnable = !singleInstance ||
                             (singleInstance.Data.Heights != null && !singleInstance.Data.Heights.IsEmpty());
            DoHeader("Heights", ref _heightsExpanded, _heightsEnabled, canEnable);
            if (_heightsExpanded)
            {
                EditorGUI.indentLevel++;
                GUI.enabled = canEnable;
                EditorGUILayout.PropertyField(_layerHeightBlendMode, new GUIContent("Blend Mode"));
                EditorGUILayout.PropertyField(_heightOffset);
                if (_layerHeightBlendMode.enumValueIndex == 2)
                {
                    EditorGUILayout.PropertyField(_heightMin);
                }
                GUI.enabled = true;
                EditorGUI.indentLevel--;
            }
        }

        private void StampAll(string layerFilter = null)
        {
            HashSet<TerrainWrapper> wrappers = new HashSet<TerrainWrapper>();
            foreach (var obj in targets)
            {
                var worldStamp = obj as WorldStamp;
                var relevantWrappers = worldStamp.GetTerrainWrappers();
                if (relevantWrappers.Count == 0)
                {
                    Debug.LogError("Unable to find any TerrainWrappers to write to. Do you need to add the TerrainWrapper component to your terrain?");
                }
                foreach (var relevantWrapper in relevantWrappers)
                {
                    wrappers.Add(relevantWrapper);
                }
            }
            foreach (var terrainWrapper in wrappers)
            {
                WorldStampApplyManager.ApplyAllStamps(terrainWrapper, layerFilter);
                terrainWrapper.ApplyAllLayers();
            }
        }

        private void DoSingleInstanceInfo()
        {
            var stamp = target as WorldStamp;

            //FIX ISSUES
            if (stamp.Data.Objects.Exists(data => data.Prefab == null))
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Stamp contains objects with null prefabs!");
                if (GUILayout.Button("Fix"))
                {
                    var prevCount = stamp.Data.Objects.Count;
                    stamp.Data.Objects.RemoveAll(data => data.Prefab == null);
                    Debug.Log(string.Format("Removed {0} missing prefabs from stamp {1}", (prevCount - stamp.Data.Objects.Count), stamp.name), stamp);
                    EditorUtility.SetDirty(stamp);
                    var prefab = PrefabUtility.GetPrefabParent(stamp);
                    if (prefab != null)
                    {
                        EditorUtility.SetDirty(prefab);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            if (stamp.Data.Objects.Exists(data => data.Scale.x < 0 || data.Scale.y < 0 || data.Scale.z < 0))
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Stamp contains objects with negative scales!");
                if (GUILayout.Button("Fix"))
                {
                    for (int i = 0; i < stamp.Data.Objects.Count; i++)
                    {
                        var obj = stamp.Data.Objects[i];
                        obj.Scale = new Vector3(Mathf.Abs(stamp.Data.Objects[i].Scale.x), Mathf.Abs(stamp.Data.Objects[i].Scale.y), Mathf.Abs(stamp.Data.Objects[i].Scale.z));
                        stamp.Data.Objects[i] = obj;
                        EditorUtility.SetDirty(stamp);
                        var prefab = PrefabUtility.GetPrefabParent(stamp);
                        if (prefab != null)
                        {
                            EditorUtility.SetDirty(prefab);
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            if (stamp.Data.Objects.Exists(data => PrefabUtility.FindPrefabRoot(data.Prefab) != data.Prefab))
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Stamp contains references to prefab subobjects!");
                if (GUILayout.Button("Fix"))
                {
                    for (int i = 0; i < stamp.Data.Objects.Count; i++)
                    {
                        var obj = stamp.Data.Objects[i];
                        obj.Prefab = PrefabUtility.FindPrefabRoot(obj.Prefab);
                        stamp.Data.Objects[i] = obj;
                        EditorUtility.SetDirty(stamp);
                        var prefab = PrefabUtility.GetPrefabParent(stamp);
                        if (prefab != null)
                        {
                            EditorUtility.SetDirty(prefab);
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            // END FIX ISSUES

            EditorGUILayout.BeginVertical("Box");
            var previewContent = EditorGUIUtility.IconContent("ClothInspector.ViewValue");
            
            if (stamp == null)
            {
                return;
            }

            bool isPrefab = PrefabUtility.GetPrefabType(target) == PrefabType.Prefab;
            //var dc = stamp.GetComponentInChildren<WorldStampDataContainer>();
            //bool isProxy = !isPrefab && dc.Redirect != null;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Heights", stamp.Data.Heights != null ? string.Format("{0}x{1}", stamp.Data.Heights.Width, stamp.Data.Heights.Height) : "null");
            if (GUILayout.Button(previewContent, EditorStyles.label, GUILayout.Width(20), GUILayout.Height(16)))
            {
                DataInspector.SetData(stamp.Data.Heights);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Splats", stamp.Data.SplatData.Count.ToString());
            if (GUILayout.Button(previewContent, EditorStyles.label, GUILayout.Width(20), GUILayout.Height(16)))
            {
                DataInspector.SetData(stamp.Data.SplatData.Select(x => x.Data).ToList(), stamp.Data.SplatData.Select(x => x.Wrapper).ToList());
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Details", stamp.Data.DetailData.Count.ToString());
            if (GUILayout.Button(previewContent, EditorStyles.label, GUILayout.Width(20), GUILayout.Height(16)))
            {
                DataInspector.SetData(stamp.Data.DetailData.Select(x => x.Data).ToList(), stamp.Data.DetailData.Select(x => x.Wrapper).ToList());
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField("Trees", stamp.Data.Trees.Count.ToString());
            EditorGUILayout.LabelField("Objects", stamp.Data.Objects.Count.ToString());
            
            GUI.enabled = !isPrefab;
            bool alreadyHasMaskInstance = stamp.Mask != null && stamp.Mask.Count > 0;
            if (GUILayout.Button(_editingMask ? "Finish Editing" : alreadyHasMaskInstance ? "Edit Mask" : "Edit Mask (Create Instance)"))
            {
                if (_editingMask)
                {
                    EditorUtility.SetDirty(stamp);
                }
                else if (stamp.Mask == null || stamp.Mask.Count == 0)
                {
                    stamp.Data.Mask.OnBeforeSerialize();
                    stamp.Mask = JsonUtility.FromJson<WorldStampMask>(JsonUtility.ToJson(stamp.Data.Mask));
                }
                _editingMask = !_editingMask;
            }
            EditorGUILayout.BeginHorizontal();
            if (alreadyHasMaskInstance && GUILayout.Button("Revert Mask"))
            {
                if (EditorUtility.DisplayDialog("Really Revert Mask?",
                    "You will lose all mask data you changed on this instance!", "Yes", "No"))
                {
                    stamp.Mask = null;
                    _editingMask = false;
                }
            }
            if (alreadyHasMaskInstance && !_editingMask && stamp.Mask != null)
            {
                if (GUILayout.Button("Write Mask To Prefab"))
                {
                    stamp.Data.Mask = JsonUtility.FromJson<WorldStampMask>(JsonUtility.ToJson(stamp.Mask));
                    var prefab = PrefabUtility.GetPrefabParent(stamp);
                    if (prefab != null)
                    {
                        Debug.Log("Wrote mask back to prefab");
                        EditorUtility.SetDirty(prefab);
                    }
                }
            }

            EditorGUILayout.EndHorizontal();
            GUI.enabled = true;
            
            /*if (isPrefab && GUILayout.Button("Instantiate Proxy In Scene"))
            {
                var stampInstance = PrefabUtility.InstantiatePrefab(target) as WorldStamp;
                if (stampInstance)
                {

                    var dcis = stampInstance.GetComponentInChildren<WorldStampDataContainer>();
                    dcis.Data = null;
                    dcis.Redirect = stamp.GetComponentInChildren<WorldStampDataContainer>();
                }
                else
                {
                    Debug.LogError("Failed to instantiate prefab " + stamp, stamp);
                }
            }*/

            EditorGUILayout.EndVertical();
        }
        
        void OnSceneGUI()
        {
            var worldStamp = target as WorldStamp;
            var rotatedMatrix = Handles.matrix * Matrix4x4.TRS(worldStamp.transform.position, worldStamp.transform.rotation, worldStamp.transform.lossyScale);
            _boxBoundsHandle.center = Vector3.up * (worldStamp.Size.y / 2);
            _boxBoundsHandle.size = worldStamp.Size;
            using (new Handles.DrawingScope(rotatedMatrix))
            {
                _boxBoundsHandle.DrawHandle();
            }
            if (worldStamp.Size != _boxBoundsHandle.size)
            {
                worldStamp.Size = _boxBoundsHandle.size;
                SceneView.RepaintAll();
            }

            if (!_editingMask)
            {
                _painter = null;
                return;
            }

            if (_painter == null)
            {
                _painter = new Common.Painter.Painter(worldStamp.Mask, worldStamp.Data.GridManager);
                _painter.Ramp = new Gradient()
                {
                    colorKeys = new[] { new GradientColorKey(Color.red, 0),new GradientColorKey(Color.black, 0.001f), new GradientColorKey(Color.black, 1), },
                    alphaKeys = new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0, 1), }
                };
            }

            _painter.Canvas = worldStamp.Mask;
            
            _painter.MaxValue = 1;
            _painter.MinValue = 0;
            _painter.Rect = new Rect(0, 0, worldStamp.Data.Size.x, worldStamp.Data.Size.z);
            _painter.GridManager = worldStamp.Data.GridManager;
            _painter.TRS = Matrix4x4.TRS(worldStamp.transform.position - worldStamp.transform.rotation * worldStamp.Size.xz().x0z()/2,
                worldStamp.transform.rotation, 
                new Vector3(
                    worldStamp.transform.lossyScale.x * (worldStamp.Size.x / worldStamp.Data.Size.x),
                    worldStamp.transform.lossyScale.y * (worldStamp.Size.y / worldStamp.Data.Size.y),
                    worldStamp.transform.lossyScale.z * (worldStamp.Size.z / worldStamp.Data.Size.z))
                    );

            _painter.PaintingEnabled = true;
            _painter.OnSceneGUI();
        }
    }
}
