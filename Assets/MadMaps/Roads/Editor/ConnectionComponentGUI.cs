using UnityEditor;
using MadMaps.Common;
using System.Collections.Generic;

namespace MadMaps.Roads
{
    [CustomEditor(typeof (ConnectionComponent), true)]
    [CanEditMultipleObjects]
    public class ConnectionComponentGUI : LayerComponentBaseGUI
    {
        private SerializedProperty _overridePriority;
        private SerializedProperty _priority;
        private SerializedProperty _connection;

        List<string> _layers = new List<string>();

        public void OnEnable()
        {
            _overridePriority = serializedObject.FindProperty("OverridePriority");
            _priority = serializedObject.FindProperty("Priority");
            _connection = serializedObject.FindProperty("NodeConnection");
        }

        public override void OnInspectorGUI()
        {
            _layers.Clear();
            foreach (var o in targets)
            {
                var cc = o as ConnectionComponent;
                var layer = cc.GetLayerName();
                if(!_layers.Contains(layer))
                {
                    _layers.Add(layer);
                }
                cc.Think();
            }

            DoGenericUI(_layers, true);

            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_connection);
            EditorGUILayout.PropertyField(_overridePriority);
            if (_overridePriority.boolValue || _overridePriority.hasMultipleDifferentValues)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_priority);
                EditorGUI.indentLevel--;
            }
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
            //base.OnInspectorGUI();
        }
    }
}