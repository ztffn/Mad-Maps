using UnityEditor;
using UnityEngine;

namespace MadMaps.Roads
{
    [CustomEditor(typeof(RoadNetwork))]
    public class RoadNetworkGUI : Editor
    {
        private RoadNetworkWindow _currentWindow;

        [MenuItem("GameObject/Mad Maps Road Network")]
        public static void CreateInstance()
        {
            var go = new GameObject("Road Network");
            go.transform.position = Vector3.zero;
            go.AddComponent<RoadNetwork>();
        }

        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("Open Editor Window"))
            {
                _currentWindow = EditorWindow.GetWindow<RoadNetworkWindow>();
                _currentWindow.name = "Road Network";
                _currentWindow.FocusedRoadNetwork = target as RoadNetwork;
            }
        }
    }
}