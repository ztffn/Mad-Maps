#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace MadMaps.Roads
{
#if UNITY_EDITOR
    [CustomEditor(typeof(RoadNetworkProxy))]
    [CanEditMultipleObjects]

    public class RoadNetworkProxyGUI : Editor
    {
        private SerializedProperty _network;

        public void OnEnable()
        {
            _network = serializedObject.FindProperty("Network");
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            serializedObject.Update();
            EditorGUILayout.PropertyField(_network);
            serializedObject.ApplyModifiedProperties();
            if (EditorGUI.EndChangeCheck())
            {
                foreach (var o in targets)
                {
                    var proxy = o as RoadNetworkProxy;
                    if (proxy && proxy.Network)
                    {
                        proxy.Network.ForceThink();
                    }
                }
            }
            /*var rnp = target as RoadNetworkProxy;
            var newTarget = EditorGUILayout.ObjectField("Network", rnp.Network, typeof (RoadNetwork), true) as RoadNetwork;
            if (rnp.Network != newTarget)
            {
                rnp.Network = newTarget;
                if (newTarget)
                {
                    newTarget.ForceThink();
                }
            }*/
        }
    }
#endif

#if HURTWORLDSDK
    [StripComponentOnBuild]
#endif
    public class RoadNetworkProxy : MonoBehaviour
    {
        public RoadNetwork Network;
    }
}