using System.Collections.Generic;
using System.Linq;
using Dingo.Common;
using ParadoxNotion;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Dingo.Roads
{
    public partial class RoadNetworkWindow : EditorWindow
    {
        public static EditorPersistantVal<bool> DrawConnections = new EditorPersistantVal<bool>("NodeNetworkGUI_DrawConnections", true);
        private static EditorPersistantVal<int> _currentTab = new EditorPersistantVal<int>("NodeNetworkGUI_CurrentTab", 0);
        private static EditorPersistantVal<bool> _configurationIgnoredTypesExpanded = new EditorPersistantVal<bool>("NodeNetworkGUI_IgnoredTypesExpanded", false);

        public RoadNetwork FocusedRoadNetwork;

        private static Vector2 _scroll;
        private static Node _currentHoverNode;
        private GameObject _currentIntersection;
        private ConnectionConfiguration _currentConfiguration;
        private static int _nodeIndex = 0;
        private static Editor _intersectionPreviewEditor;
        private Vector3 _extraRotation;
        private Vector2 _intersectionScroll;
        public static float NodePreviewSize = 5;

        private GUIContent[] _tabs;

        public static List<Node> GetCurrentlySelectedNodes(List<Node> result = null)
        {
            if (result == null)
            {
                result = new List<Node>();
            }
            for (int i = 0; i < Selection.objects.Length; i++)
            {
                var obj = Selection.objects[i];
                var firstGo = obj as GameObject;
                if (firstGo != null)
                {
                    var goN = firstGo.GetComponents<Node>();
                    if (goN.Length > 0)
                    {
                        result.Add(goN[_nodeIndex%goN.Length]);
                    }
                }
            }

            return result;
        }

        public static void SetCurrentlySelectedNodes(List<Node> selection)
        {
            var gameObjects = new List<Object>();
            for (int i = 0; i < selection.Count; i++)
            {
                var node = selection[i];
                if (!gameObjects.Contains(node.gameObject))
                {
                    gameObjects.Add(node.gameObject);
                }
            }
            Selection.objects = gameObjects.ToArray();
        }

        public static void SetCurrentlySelectedNodes(Node selection)
        {
            Selection.activeGameObject = selection.gameObject;
        }

        public static NodeConnection CurrentlySelectedConnection
        {
            get
            {
                var firstGo = Selection.objects.FirstOrDefault() as GameObject;
                if (firstGo != null)
                {
                    var goN = firstGo.GetComponent<NodeConnection>();
                    if (goN)
                    {
                        return goN;
                    }
                }
                return null;
            }
            set
            {
                if (value == null)
                {
                    Selection.objects = null;
                }
                Selection.objects = new[] { value.gameObject };
            }
        }

        public void OnSelectionChange()
        {
            var activeGO = Selection.activeGameObject;
            if (!activeGO)
            {
                return;
            }
            var rn = activeGO.GetComponent<RoadNetwork>();
            if (rn)
            {
                FocusedRoadNetwork = rn;
            }
        }

        [MenuItem("Tools/Dingo/Road Network Editor")]
        public static void OpenWindow()
        {
            var w = GetWindow<RoadNetworkWindow>();
            w.titleContent = new GUIContent("Road Network", GUIResources.RoadNetworkIcon);
        }

        public void OnGUI()
        {
            if (FocusedRoadNetwork == null)
            {
                return;
            }

            NodePreviewSize = FocusedRoadNetwork.NodePreviewSize;

            _currentTab.Value = GUILayout.Toolbar(_currentTab, _tabs, GUILayout.Height(20));
            EditorExtensions.Seperator();
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            switch (_currentTab)
            {
                case 0: // Connections
                    DoConnectionsUI();
                    break;
                case 1: // Intersections
                    DoIntersectionsUI();
                    break;
                case 2: // Configuration
                    DoConfigUI();
                    break;
            }
            
            EditorGUILayout.LabelField("", GUILayout.ExpandHeight(true));
            DoCommands();
            EditorGUILayout.EndScrollView();
        }

        private Vector2 _roadConfigScroll;
        private void DoConnectionsUI()
        {
            EditorGUILayout.HelpBox("Press [Middle Mouse Button] to select a node and [Shift] to select multiple.\n\nHold [CTRL] and press [Middle Mouse Button] to place a new node, or connect two existing nodes.", MessageType.Info);

            // Connection Configuration history for easy selection
            FocusedRoadNetwork.RoadConfigHistory =
                FocusedRoadNetwork.RoadConfigHistory.Where(configuration => configuration).ToList();    // Remove null entries
            if (FocusedRoadNetwork.RoadConfigHistory.Count > 0)
            {
                _roadConfigScroll = EditorGUILayout.BeginScrollView(_roadConfigScroll, GUILayout.Height(80));
                EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
                for (int i = FocusedRoadNetwork.RoadConfigHistory.Count - 1; i >= 0; i--)
                {
                    var config = FocusedRoadNetwork.RoadConfigHistory[i];
                    if (config == null)
                    {
                        FocusedRoadNetwork.RoadConfigHistory.RemoveAt(i);
                        continue;
                    }

                    var textStyle = new GUIStyle
                    {
                        richText = true,
                        wordWrap = true,
                        alignment = TextAnchor.MiddleCenter,
                        fontStyle = FontStyle.Bold,
                    };

                    if (config == _currentConfiguration)
                    {
                        GUILayout.Label(GUIResources.RoadConfigurationIcon, "Box", GUILayout.Width(64), GUILayout.Height(64));
                    }
                    else
                    {
                        GUILayout.Label(GUIResources.RoadConfigurationIcon, GUILayout.Width(64), GUILayout.Height(64));
                    }
                    var buttonRect = GUILayoutUtility.GetLastRect();

                    var configName = string.Format("<color={1}>{0}</color>", config.name.SplitCamelCase(), Color.black.ToHexString());
                    GUI.Label(buttonRect, configName, textStyle);
                    configName = string.Format("<color={1}>{0}</color>", config.name.SplitCamelCase(), config.Color.ToHexString());
                    buttonRect.y += 2;
                    if (GUI.Button(buttonRect, configName, textStyle))
                    {
                        _currentConfiguration = config;
                    }
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndScrollView();
            }
            if(!FocusedRoadNetwork.RoadConfigHistory.Contains(_currentConfiguration))
            {
                FocusedRoadNetwork.RoadConfigHistory.Add(_currentConfiguration);
            }

            _currentConfiguration = (ConnectionConfiguration) EditorGUILayout.ObjectField("Configuration", _currentConfiguration,
                typeof (ConnectionConfiguration), false);

            if (GUILayout.Button("Apply Configuration to Selected"))
            {
                var selected = GetCurrentlySelectedNodes();
                foreach (var node in selected)
                {
                    foreach (var nodeConnection in node.AllConnections())
                    {
                        nodeConnection.Configuration = _currentConfiguration;
                        EditorUtility.SetDirty(nodeConnection);
                    }
                }
            }

            EditorExtensions.Seperator();

            // Config
            FocusedRoadNetwork.CurrentNodeConfiguration.SnappingMode = (NodeConfiguration.ESnappingMode) EditorGUILayout.EnumPopup("Snapping Mode",
                FocusedRoadNetwork.CurrentNodeConfiguration.SnappingMode);
            FocusedRoadNetwork.CurrentNodeConfiguration.SnapMask = LayerMaskFieldUtility.LayerMaskField("Snapping Mask",
                FocusedRoadNetwork.CurrentNodeConfiguration.SnapMask, false);
            FocusedRoadNetwork.CurrentNodeConfiguration.SnapOffset = EditorGUILayout.FloatField("Snapping Offset",
                FocusedRoadNetwork.CurrentNodeConfiguration.SnapOffset);
        }

        private void DoIntersectionsUI()
        {
            if (FocusedRoadNetwork.IntersectionHistory.Count > 0)
            {
                _intersectionScroll = EditorGUILayout.BeginScrollView(_intersectionScroll, GUILayout.Height(100));
                EditorGUILayout.BeginHorizontal("Box", GUILayout.ExpandWidth(true));
                for (int i = FocusedRoadNetwork.IntersectionHistory.Count - 1; i >= 0; i--)
                {
                    var gameObject = FocusedRoadNetwork.IntersectionHistory[i];
                    if (gameObject == null)
                    {
                        FocusedRoadNetwork.IntersectionHistory.RemoveAt(i);
                        continue;
                    }
                    var guiContent = new GUIContent(AssetPreview.GetAssetPreview(gameObject));
                    EditorGUILayout.BeginVertical(GUILayout.Width(70));
                    if (GUILayout.Button(guiContent, GUIStyle.none, GUILayout.Width(64), GUILayout.Height(64)) || GUILayout.Button(gameObject.name, EditorStyles.miniLabel, GUILayout.Width(64)))
                    {
                        _currentIntersection = gameObject;
                    }
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndScrollView();
            }

            _currentIntersection = (GameObject) EditorGUILayout.ObjectField("Intersection", _currentIntersection,
                typeof(GameObject), false);
            if (_currentIntersection == null)
            {
                return;
            }

            var nodes = _currentIntersection.GetComponents<Node>();
            if (nodes.Length == 0)
            {
                EditorGUILayout.HelpBox("No Node Components found on prefab!", MessageType.Error);
                return;
            }

            if (!FocusedRoadNetwork.IntersectionHistory.Contains(_currentIntersection))
            {
                FocusedRoadNetwork.IntersectionHistory.Add(_currentIntersection);
            }

            if (_intersectionPreviewEditor == null)
            {
                _intersectionPreviewEditor = Editor.CreateEditor(_currentIntersection);
            }

            _extraRotation = EditorGUILayout.Vector3Field("Rotation", _extraRotation);

            var currentSelectedNodes = GetCurrentlySelectedNodes();
            GUI.enabled = currentSelectedNodes.IsNullOrEmpty();
            if (GUILayout.Button("Insert Intersection Into Node"))
            {
                for (int i = 0; i < currentSelectedNodes.Count; i++)
                {
                    FocusedRoadNetwork.InsertIntersection(currentSelectedNodes[i],
                        (GameObject) PrefabUtility.InstantiatePrefab(_currentIntersection.gameObject), _extraRotation);
                }
            }
            GUI.enabled = true;
        }

        private void DoConfigUI()
        {
            FocusedRoadNetwork.SplineResolution = EditorGUILayout.FloatField("Spline Resolution", FocusedRoadNetwork.SplineResolution);
            FocusedRoadNetwork.Curviness = EditorGUILayout.FloatField("Curviness", FocusedRoadNetwork.Curviness);
            FocusedRoadNetwork.BreakAngle = EditorGUILayout.Slider("Break Angle", FocusedRoadNetwork.BreakAngle, 0, 90);
            FocusedRoadNetwork.NodePreviewSize = EditorGUILayout.Slider("Node Preview Size", FocusedRoadNetwork.NodePreviewSize, 1, 10);
            FocusedRoadNetwork.RecalculateTerrain = EditorGUILayout.Toggle("Recalculate Terrain", FocusedRoadNetwork.RecalculateTerrain);
            DrawConnections.Value = EditorGUILayout.Toggle("Draw Connections", DrawConnections);

            EditorGUILayout.BeginHorizontal();
            _configurationIgnoredTypesExpanded.Value = EditorGUILayout.Foldout(_configurationIgnoredTypesExpanded,
                "Ignored Connection Component Types");
            EditorGUILayout.EndHorizontal();
            if (_configurationIgnoredTypesExpanded)
            {
                EditorGUI.indentLevel++;
                var t = FocusedRoadNetwork.IgnoredTypes;
                var allT = typeof (ConnectionComponent).GetAllChildTypes();
                foreach (var type in allT)
                {
                    EditorGUILayout.BeginHorizontal();
                    var contained = t.Contains(type.AssemblyQualifiedName);
                    var newVal = EditorGUILayout.Toggle(contained, GUILayout.Width(30));
                    if (contained != newVal)
                    {
                        if (t.Contains(type.AssemblyQualifiedName))
                        {
                            t.Remove(type.AssemblyQualifiedName);
                        }
                        else
                        {
                            t.Add(type.AssemblyQualifiedName);
                        }
                    }
                    EditorGUILayout.LabelField(type.Name, GUILayout.ExpandWidth(true));
                    EditorGUILayout.EndHorizontal();
                }
                FocusedRoadNetwork.IgnoredTypes = t;
                EditorGUI.indentLevel--;
            }

            EditorExtensions.Seperator();
            EditorGUILayout.LabelField("Tools", EditorStyles.boldLabel);
            if (GUILayout.Button("Snap To Surface"))
            {
                var wiz = ScriptableWizard.DisplayWizard<SnapNodesToSurfaceWizard>("Snap Nodes To Surface", "Snap All", "Snap Selection");
                wiz.OnOpened();
            }
            if (GUILayout.Button("Force Refresh"))
            {
                FocusedRoadNetwork.ForceThink();
            }
            if (GUILayout.Button("Delete all DataContainer Objects") && 
                EditorUtility.DisplayDialog("Really delete all DataContainers on all connections?", "You won't be able to undo this action!", "Yes", "No"))
            {
                foreach (var node in FocusedRoadNetwork.Nodes)
                {
                    foreach (var nodeConnection in node.AllConnections())
                    {
                        nodeConnection.DestroyAllDataContainers();
                    }
                }
            }
            if (GUILayout.Button("Create Stripped Copy"))
            {
                var newObj = Instantiate(FocusedRoadNetwork);
                newObj.name = newObj.name.Replace("Clone", "Stripped");
                var rn = newObj.GetComponent<RoadNetwork>();
                rn.Strip();
            }
        }

        private void DoCommands()
        {
            EditorExtensions.Seperator();
            if (GUILayout.Button("Rebake"))
            {
                FocusedRoadNetwork.RebakeAllNodes();
            }
        }

        public void OnEnable()
        {
            _tabs = new GUIContent[]
            {
                new GUIContent("  Connections", GUIResources.NodeIcon, "Paint connections and nodes"), 
                new GUIContent("  Intersections", GUIResources.IntersectionIcon, "Insert intersection prefabs into the network"), 
                new GUIContent("  Configuration", GUIResources.RoadNetworkIcon, "Settings and tools"), 
            };

            SceneView.onSceneGUIDelegate -= OnSceneGUI;
            SceneView.onSceneGUIDelegate += OnSceneGUI;
        }

        public void OnDisable()
        {
            SceneView.onSceneGUIDelegate -= OnSceneGUI;
        }
    }
}