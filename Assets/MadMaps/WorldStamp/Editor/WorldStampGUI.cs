using System.Collections.Generic;
using System.Linq;
using MadMaps.Common;
using MadMaps.Common.Painter;
using MadMaps.Terrains;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

#if UNITY_EDITOR
using Painter = MadMaps.Common.Painter.Painter;
using IGridManager = MadMaps.Common.Painter.IGridManager;
using GridManagerInt = MadMaps.Common.Painter.GridManagerInt;
using IBrush = MadMaps.Common.Painter.IBrush;
using EditorCellHelper = MadMaps.Common.Painter.EditorCellHelper;
#endif

namespace MadMaps.WorldStamps
{
    [CustomEditor(typeof (WorldStamp))]
    [CanEditMultipleObjects]
    public partial class WorldStampGUI : LayerComponentBaseGUI
    {
        BoxBoundsHandle _boxBoundsHandle = new BoxBoundsHandle(-1);
        private SerializedProperty _size;
        private SerializedProperty _snapToTerrain;
        private SerializedProperty _snapToTerrainOffset;
        private SerializedProperty _priority;
        private SerializedProperty _layerName;
        private SerializedProperty _previewEnabled;
        private SerializedProperty _gizmoColor, _gizmosEnabled;
        private SerializedProperty _disableStencil;

        [SerializeField]
        private bool _heightsExpanded, _treesExpanded, _objectsExpanded, _splatsExpanded, _detailsExpanded;
        
        // Heights
        private SerializedProperty _heightsEnabled;
        private SerializedProperty _layerHeightBlendMode;
        private SerializedProperty _heightOffset;

        // Splats
        private SerializedProperty _splatsEnabled;
        private SerializedProperty _stencilSplats;
        private SerializedProperty _splatBlendMode;

        // Trees
        private SerializedProperty _treesEnabled;
        private SerializedProperty _stencilTrees;
        private SerializedProperty _removeTrees;

        // Details
        private SerializedProperty _detailsEnabled;
        private SerializedProperty _detailBlendMode;
        private SerializedProperty _detailsBoost;
        private SerializedProperty _detailsRemoveExisting;

        // Objects
        private SerializedProperty _objectsEnabled;
        private SerializedProperty _stencilObjects;
        private SerializedProperty _removeObjects;
        //private SerializedProperty _overrideRelativeObjectMode;
        private SerializedProperty _relativeObjectMode;

        private bool _editingMask = false;
        private Painter _painter;

        void OnEnable()
        {
            #if VEGETATION_STUDIO
            _vegetationStudioEnabled = serializedObject.FindProperty("VegetationStudioEnabled");
            _stencilVSData = serializedObject.FindProperty("StencilVSData");
            _removeExistingVSData = serializedObject.FindProperty("RemoveExistingVSData");
            #endif

            _size = serializedObject.FindProperty("ExplicitSize");
            _snapToTerrain = serializedObject.FindProperty("SnapToTerrainHeight");
            _snapToTerrainOffset = serializedObject.FindProperty("SnapToTerrainHeightOffset");
            _priority = serializedObject.FindProperty("Priority");
            _layerHeightBlendMode = serializedObject.FindProperty("LayerHeightBlendMode");
            //_heightMin = serializedObject.FindProperty("MinHeight");
            _splatBlendMode = serializedObject.FindProperty("SplatBlendMode");
            _stencilSplats = serializedObject.FindProperty("StencilSplats");
            _removeTrees = serializedObject.FindProperty("RemoveTrees");
            _stencilTrees = serializedObject.FindProperty("StencilTrees");
            _removeObjects = serializedObject.FindProperty("RemoveObjects");
            _stencilObjects = serializedObject.FindProperty("StencilObjects");
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
            _detailsRemoveExisting = serializedObject.FindProperty("RemoveExistingDetails");
            //_overrideRelativeObjectMode = serializedObject.FindProperty("OverrideObjectRelativeMode");
            _relativeObjectMode = serializedObject.FindProperty("RelativeMode");
            _disableStencil = serializedObject.FindProperty("DisableStencil");
        }

        void OnDisable()
        {
            if(_painter != null)
            {
                _painter.Destroy();
                _painter = null;
            }
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
            DoGenericUI(_layerName, !_editingMask);

            serializedObject.Update();

            EditorGUILayout.BeginVertical(GUILayout.Width(100));

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

            EditorGUILayout.PropertyField(_gizmoColor);

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            
            // Scale
            EditorGUILayout.BeginHorizontal();
            _size = serializedObject.FindProperty("ExplicitSize");
            EditorGUILayout.PropertyField(_size);
            GUI.enabled = !_editingMask && (_size.hasMultipleDifferentValues ? true : _size.vector3Value != (target as WorldStamp).Data.Size);
            if (GUILayout.Button("Reset", GUILayout.Width(70)))
            {
                foreach (var obj in targets)
                {
                    var worldStamp = obj as WorldStamp;
                    worldStamp.ExplicitSize = worldStamp.Data.Size;
                }
                _size = serializedObject.FindProperty("Size");
                SceneView.RepaintAll();
            }
            GUI.enabled = !_editingMask;
            EditorGUILayout.EndHorizontal();

            /*EditorGUILayout.PropertyField(_snapPosition);
            EditorGUILayout.PropertyField(_snapRotation, new GUIContent("Snap Rotation (90\u00B0)"));*/
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_snapToTerrain);
            EditorGUILayout.PropertyField(_snapToTerrainOffset);
            if(EditorGUI.EndChangeCheck())
            {
                foreach(var obj in targets)
                {
                    (obj as WorldStamp).SnapStamp(true);
                }
            }

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_layerName);
            if(EditorGUI.EndChangeCheck())
            {
                WorldStampPriorityEditorWindow.NeedsResort = true;
            }

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
                EditorWindow.GetWindow<WorldStampPriorityEditorWindow>();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(_disableStencil);

            EditorExtensions.Seperator();

            DoHeightsSection();

            DoTreesSection();

            DoObjectsSection();

            DoSplatsSection();

            DoDetailsSection();

            #if VEGETATION_STUDIO
            DoVegetationStudioSection();
            #endif



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
                if (!canWrite)
                {
                    EditorGUILayout.HelpBox("No Details in Stamp", MessageType.Info);
                }

                EditorGUILayout.PropertyField(_detailBlendMode);
                EditorGUILayout.PropertyField(_detailsBoost);
                EditorGUILayout.PropertyField(_detailsRemoveExisting);                

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
                if (!canWrite)
                {
                    EditorGUILayout.HelpBox("No Splats in Stamp", MessageType.Info);
                }
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
            #region KILL ME ASAP
            foreach(var t in targets)
            {
                var stamp = t as WorldStamp;
                if(stamp && stamp.NeedsRelativeModeCheck && stamp.Data != null && stamp.Data.Objects.Count > 0)
                {
                    
                    stamp.NeedsRelativeModeCheck = false;
                    //RelativeHeight = RelativeMode == WorldStamp.EObjectRelativeMode.RelativeToStamp,
                    stamp.RelativeMode = stamp.Data.Objects[0].AbsoluteHeight ? WorldStamp.EObjectRelativeMode.RelativeToStamp 
                        : WorldStamp.EObjectRelativeMode.RelativeToTerrain;
                    Debug.Log(string.Format("Updated stamp object relative mode of '{0}' to v0.1.2c. This set the Object Relative Mode to {1}. This should be a one time process.", stamp.name, stamp.RelativeMode), stamp);
                    EditorUtility.SetDirty(stamp);
                }
            }
            #endregion

            WorldStamp singleInstance = targets.Length == 1 ? target as WorldStamp : null;
            bool canWrite = !singleInstance || singleInstance.Data.Objects.Count > 0;
            DoHeader("Objects", ref _objectsExpanded, _objectsEnabled, canWrite);
            if (_objectsExpanded)
            {
                EditorGUI.indentLevel++;
                if (!canWrite)
                {
                    EditorGUILayout.HelpBox("No Objects in Stamp", MessageType.Info);
                }
                EditorGUILayout.PropertyField(_removeObjects, new GUIContent("Remove Existing Objects"));
                GUI.enabled = canWrite;
                EditorGUILayout.PropertyField(_stencilObjects);
                EditorGUILayout.PropertyField(_relativeObjectMode);
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
                if (!canWrite)
                {
                    EditorGUILayout.HelpBox("No Trees in Stamp", MessageType.Info);
                }
                if (targets.Length > 1)
                {
                    EditorGUILayout.HelpBox("Can't Edit Multiple Tree Ignores", MessageType.Info);
                }
                else
                {
                    var stamp = target as WorldStamp;

                    if(stamp.Data.Trees.Count > 0 && (stamp.Data.TreePrototypeCache == null || stamp.Data.TreePrototypeCache.Count ==0))
                    {
                        stamp.Data.TreePrototypeCache = new List<GameObject>();
                        foreach (var tree in stamp.Data.Trees)
                        {
                            if(!stamp.Data.TreePrototypeCache.Contains(tree.Prototype))
                            {
                                stamp.Data.TreePrototypeCache.Add(tree.Prototype);
                            }
                        }
                    }

                    for (var i = 0; i < stamp.Data.TreePrototypeCache.Count; ++i)
                    {
                        var prototype = stamp.Data.TreePrototypeCache[i];
                        if(prototype == null)
                        {
                            continue;
                        }

                        var ignored = stamp.IgnoredTrees.Contains(prototype);
                        GUILayout.BeginHorizontal();
                        GUI.color = ignored ? Color.red : Color.green;
                        var newPicked = (GameObject)EditorGUILayout.ObjectField(prototype, typeof(GameObject), false);
                        if (newPicked != null && newPicked != prototype)
                        {
                            prototype = newPicked;
                            EditorUtility.SetDirty(stamp);
                            var prefab = PrefabUtility.GetPrefabObject(stamp);
                            if (prefab)
                            {
                                EditorUtility.SetDirty(prefab);
                            }
                        }
                        if (EditorGUILayoutX.IndentedButton(ignored ? "Unmute" : "Mute"))
                        {
                            if (stamp.IgnoredTrees.Contains(prototype))
                            {
                                stamp.IgnoredTrees.Remove(prototype);
                            }
                            else
                            {
                                stamp.IgnoredTrees.Add(prototype);
                            }
                        }
                        GUILayout.EndHorizontal();
                    }
                    GUI.color = Color.white;
                }
                EditorGUILayout.PropertyField(_removeTrees, new GUIContent("Remove Existing Trees"));
                GUI.enabled = canWrite;
                EditorGUILayout.PropertyField(_stencilTrees);
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
                if (!canEnable)
                {
                    EditorGUILayout.HelpBox("No Heights in Stamp", MessageType.Info);
                }
                EditorGUILayout.PropertyField(_layerHeightBlendMode, new GUIContent("Blend Mode"));
                EditorGUILayout.PropertyField(_heightOffset);
                GUI.enabled = true;
                EditorGUI.indentLevel--;
            }
        }

        UnityEngine.Object GetPrefab(UnityEngine.Object source)
        {
#if UNITY_2018_2_OR_NEWER
            return PrefabUtility.FindPrefabRoot(PrefabUtility.GetCorrespondingObjectFromSource(source) as GameObject);
#else
            return PrefabUtility.FindPrefabRoot(PrefabUtility.GetPrefabParent(source) as GameObject);
#endif
        }
      
        private void DoSingleInstanceInfo()
        {
            var stamp = target as WorldStamp;
            EditorGUILayout.BeginVertical("Box");

            //FIX ISSUES
            if (stamp.Data.Objects.Exists(data => data.Prefab == null))
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Stamp contains objects with null prefabs");
                if (GUILayout.Button("Fix"))
                {
                    var prevCount = stamp.Data.Objects.Count;
                    stamp.Data.Objects.RemoveAll(data => data.Prefab == null);
                    Debug.Log(string.Format("Removed {0} missing prefabs from stamp {1}", (prevCount - stamp.Data.Objects.Count), stamp.name), stamp);
                    EditorUtility.SetDirty(stamp);
                    var prefab = GetPrefab(stamp);
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
                EditorGUILayout.LabelField("Stamp contains objects with negative scales");
                if (GUILayout.Button("Fix"))
                {
                    for (int i = 0; i < stamp.Data.Objects.Count; i++)
                    {
                        var obj = stamp.Data.Objects[i];
                        obj.Scale = new Vector3(Mathf.Abs(stamp.Data.Objects[i].Scale.x), Mathf.Abs(stamp.Data.Objects[i].Scale.y), Mathf.Abs(stamp.Data.Objects[i].Scale.z));
                        stamp.Data.Objects[i] = obj;
                        EditorUtility.SetDirty(stamp);
                        var prefab = GetPrefab(stamp);
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
                EditorGUILayout.LabelField("Stamp contains references to prefab subobjects");
                if (GUILayout.Button("Fix"))
                {
                    for (int i = 0; i < stamp.Data.Objects.Count; i++)
                    {
                        var obj = stamp.Data.Objects[i];
                        obj.Prefab = PrefabUtility.FindPrefabRoot(obj.Prefab);
                        stamp.Data.Objects[i] = obj;
                        EditorUtility.SetDirty(stamp);
                        var prefab = GetPrefab(stamp);
                        if (prefab != null)
                        {
                            EditorUtility.SetDirty(prefab);
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            #if VEGETATION_STUDIO
            if (stamp.Data.VSData.Exists(data => data.Package == null))
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Stamp contains Vegetation Studio with no package!");
                if (GUILayout.Button("Remove"))
                {
                    var prevCount = stamp.Data.VSData.Count;
                    stamp.Data.VSData.RemoveAll(data => data.Package == null);
                    Debug.Log(string.Format("Removed {0} invalid instances from stamp {1}", (prevCount - stamp.Data.VSData.Count), stamp.name), stamp);
                    EditorUtility.SetDirty(stamp);
                    var prefab = PrefabUtility.GetPrefabParent(stamp);
                    if (prefab != null)
                    {
                        EditorUtility.SetDirty(prefab);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            #endif
            // END FIX ISSUES

            var previewContent = new GUIContent(GUIResources.EyeOpenIcon, "Preview");
            
            if (stamp == null)
            {
                return;
            }

            bool isPrefab = PrefabUtility.GetPrefabType(target) == PrefabType.Prefab;
            bool hasPrefab = PrefabUtility.GetPrefabObject(target);
            //var dc = stamp.GetComponentInChildren<WorldStampDataContainer>();
            //bool isProxy = !isPrefab && dc.Redirect != null;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Heights", stamp.Data.Heights != null ? string.Format("{0}x{1}", stamp.Data.Heights.Width, stamp.Data.Heights.Height) : "null");
            if (GUILayout.Button(previewContent, EditorStyles.label, GUILayout.Width(20), GUILayout.Height(16)))
            {
                DataInspector.SetData(stamp.Data.Heights, null, true);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Splats", stamp.Data.SplatData.Count.ToString());
            if (GUILayout.Button(previewContent, EditorStyles.label, GUILayout.Width(20), GUILayout.Height(16)))
            {
                List<IDataInspectorProvider> data = new List<IDataInspectorProvider>();
                List<object> context = new List<object>();
                foreach (var splatData in stamp.Data.SplatData)
                {
                    data.Add(splatData.Data);
                    context.Add(splatData.Wrapper);
                }
                DataInspector.SetData(data, context);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Details", stamp.Data.DetailData.Count.ToString());
            if (GUILayout.Button(previewContent, EditorStyles.label, GUILayout.Width(20), GUILayout.Height(16)))
            {
                List<IDataInspectorProvider> data = new List<IDataInspectorProvider>();
                List<object> context = new List<object>();
                foreach (var detailData in stamp.Data.DetailData)
                {
                    data.Add(detailData.Data);
                    context.Add(detailData.Wrapper);
                }
                DataInspector.SetData(data, context, true);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Trees", stamp.Data.Trees.Count.ToString());
            if (GUILayout.Button(previewContent, EditorStyles.label, GUILayout.Width(20), GUILayout.Height(16)))
            {
                Dictionary<object, IDataInspectorProvider> data = new Dictionary<object, IDataInspectorProvider>();
                foreach (var tree in stamp.Data.Trees)
                {
                    if(!data.ContainsKey(tree.Prototype))
                    {
                        data[tree.Prototype] = new PositionList();
                    }
                    (data[tree.Prototype] as PositionList).Add(tree.Position);
                }
                DataInspector.SetData(data.Values.ToList(), data.Keys.ToList(), true);
            }
            EditorGUILayout.EndHorizontal();

            #if VEGETATION_STUDIO
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Vegetation Studio Instances", stamp.Data.VSData.Count.ToString());
            if (GUILayout.Button(previewContent, EditorStyles.label, GUILayout.Width(20), GUILayout.Height(16)))
            {
                Dictionary<object, IDataInspectorProvider> data = new Dictionary<object, IDataInspectorProvider>();
                foreach (var tree in stamp.Data.VSData)
                {
                    if(!data.ContainsKey(tree.VSID))
                    {
                        data[tree.VSID] = new PositionList();
                    }
                    (data[tree.VSID] as PositionList).Add(tree.Position);
                }
                DataInspector.SetData(data.Values.ToList(), data.Keys.ToList(), true);
            }
            EditorGUILayout.EndHorizontal();
            #endif

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Objects", stamp.Data.Objects.Count.ToString());
            if (GUILayout.Button(previewContent, EditorStyles.label, GUILayout.Width(20), GUILayout.Height(16)))
            {
                Dictionary<object, IDataInspectorProvider> data = new Dictionary<object, IDataInspectorProvider>();
                foreach (var obj in stamp.Data.Objects)
                {
                    if(!data.ContainsKey(obj.Prefab))
                    {
                        data[obj.Prefab] = new PositionList();
                    }
                    (data[obj.Prefab] as PositionList).Add(obj.Position);
                }
                DataInspector.SetData(data.Values.ToList(), data.Keys.ToList(), true);
            }
            EditorGUILayout.EndHorizontal();
            
            GUI.enabled = !isPrefab;
            bool alreadyHasMaskInstance = !hasPrefab || (stamp.Mask != null && stamp.Mask.Count > 0);

            if (GUILayout.Button(_editingMask ? "Finish Editing" : alreadyHasMaskInstance ? "Edit Mask" : "Edit Mask (Create Instance)"))
            {
                if (_editingMask)
                {
                    EditorUtility.SetDirty(stamp);
                    if(_painter != null)
                    {
                        _painter.Destroy();
                        _painter = null;
                    }
                }
                else if (stamp.Mask == null || stamp.Mask.Count == 0)
                {
                    stamp.Data.Mask.OnBeforeSerialize();
                    stamp.Mask = JsonUtility.FromJson<WorldStampMask>(JsonUtility.ToJson(stamp.Data.Mask));
                }
                _editingMask = !_editingMask;
                GUIUtility.ExitGUI();
                return;
            }
            GUI.enabled = !_editingMask && hasPrefab && alreadyHasMaskInstance;
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Revert Mask"))
            {
                if (EditorUtility.DisplayDialog("Really Revert Mask?",
                    "You will lose all mask data you changed on this instance!", "Yes", "No"))
                {
                    stamp.Mask = null;
                    _editingMask = false;
                    if(_painter != null)
                    {
                        _painter.Destroy();
                        _painter = null;
                    }
                }
                GUIUtility.ExitGUI();
                return;
            }
            if (GUILayout.Button("Write Mask To Prefab") && stamp.Mask != null)
            {
                stamp.Mask.OnBeforeSerialize();
                stamp.Data.Mask = JsonUtility.FromJson<WorldStampMask>(JsonUtility.ToJson(stamp.Mask));
                var prefab = GetPrefab(stamp);
                if (prefab != null)
                {
                    Debug.Log("Wrote mask back to prefab");
                    EditorUtility.SetDirty(prefab);
                }              
                stamp.Mask = null;
                _editingMask = false;  
                if(_painter != null)
                {
                    _painter.Destroy();
                    _painter = null;
                }
                GUIUtility.ExitGUI();
                return;
            }

            EditorGUILayout.EndHorizontal();
            GUI.enabled = true;

            EditorGUILayout.EndVertical();
        }
        
        void OnSceneGUI()
        {
            var worldStamp = target as WorldStamp;
            if (!_editingMask)
            {
                var rotatedMatrix = Handles.matrix * Matrix4x4.TRS(worldStamp.transform.position, worldStamp.transform.rotation, worldStamp.transform.lossyScale);
                _boxBoundsHandle.center = Vector3.up * (worldStamp.Size.y / 2);
                _boxBoundsHandle.size = worldStamp.ExplicitSize;
                using (new Handles.DrawingScope(rotatedMatrix))
                {
                    _boxBoundsHandle.DrawHandle();
                }
                if (worldStamp.Size != _boxBoundsHandle.size)
                {
                    //var scaled = _boxBoundsHandle.size;
                    //scaled.Scale(worldStamp.transform.lossyScale.Inverse());
                    worldStamp.ExplicitSize = _boxBoundsHandle.size;
                    SceneView.RepaintAll();
                }

                if(_painter != null)
                {
                    _painter.Destroy();
                    _painter = null;
                }
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
