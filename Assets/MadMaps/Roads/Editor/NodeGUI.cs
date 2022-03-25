using UnityEditor;
using UnityEngine;
using MadMaps.Common;

namespace MadMaps.Roads
{
    [CustomEditor(typeof (Node))]
    [CanEditMultipleObjects]
    public class NodeGUI : Editor
    {
        private SerializedProperty _configuration,
            _offset,
            _seed;

        private void OnEnable()
        {
            _configuration = serializedObject.FindProperty("Configuration");
            _offset = serializedObject.FindProperty("Offset");
            _seed = serializedObject.FindProperty("Seed");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_configuration, true);
            EditorGUILayout.PropertyField(_offset);
            EditorGUILayout.PropertyField(_seed);

            serializedObject.ApplyModifiedProperties();
            
        }

        public void OnSceneGUI()
        {
            DrawSceneGUI(SceneView.currentDrawingSceneView.camera, target as Node);
        }
        
        public static void DrawSceneGUI(Camera sceneviewCamera, Node node)
        {
            if (node == null)
            {
                return;
            }

            var pos = node.NodePosition;
            const float cullDistance = 500;
            var sqrDist = (sceneviewCamera.transform.position.xz() - pos.xz()).sqrMagnitude;
            if (sqrDist > cullDistance * cullDistance)
            {
                return;
            }

            Handles.color = RoadNetworkWindow.IsSelected(node) ? Color.red : Color.gray;
            if (RoadNetworkWindow.IsHovering(node))
            {
                Handles.color = Color.Lerp(Handles.color, Color.white, 0.5f);
            }

            var rot = node.transform.rotation;
            Handles.CubeHandleCap(-1, pos, rot, RoadNetworkWindow.NodePreviewSize, EventType.Repaint);
            
            if (RoadNetworkWindow.IsSelected(node))
            {
                if (node.Configuration.IsExplicitControl)
                {
                    Handles.color = Color.green;
                    Handles.DrawDottedLine(pos, pos + node.GetNodeControl(null), 3);
                }
                if (node.Configuration.SnappingMode == NodeConfiguration.ESnappingMode.Raycast)
                {
                    Handles.color = Color.gray;
                    Handles.DrawDottedLine(pos + Vector3.up * node.Configuration.SnapDistance / 2, pos - Vector3.up * node.Configuration.SnapDistance / 2, 1);
                }
            }
        }
    }
}