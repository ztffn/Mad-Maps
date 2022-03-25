#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace MadMaps.Common
{
    public static class EditorGUIX
    {
        

        public static void PropertyField(Rect labelRect, Rect propertyRect, SerializedProperty property)
        {
            EditorGUI.LabelField(labelRect, property.displayName);
            EditorGUI.PropertyField(propertyRect, property, GUIContent.none);
        }
    }
}

#endif