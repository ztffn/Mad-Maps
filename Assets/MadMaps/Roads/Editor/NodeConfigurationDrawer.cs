using UnityEditor;
using UnityEngine;

namespace MadMaps.Roads
{
    [CustomPropertyDrawer(typeof(NodeConfiguration))]
    public class NodeConfigurationDrawer : PropertyDrawer
    {
        private const float Indent = 16f;
        private const float RowMargin = 2f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            property.serializedObject.Update();
            
            EditorGUI.BeginChangeCheck();
            // Curviness
            var overrideCurviness = property.serializedObject.FindProperty("Configuration.OverrideCurviness");
            var curviness = property.serializedObject.FindProperty("Configuration.Curviness");
            var rect = new Rect(position.x, position.y, position.width, EditorGUI.GetPropertyHeight(overrideCurviness));

            EditorGUI.PropertyField(rect, overrideCurviness);
            if (overrideCurviness.boolValue)
            {
                rect.y += rect.height + RowMargin;
                EditorGUI.indentLevel++;
                rect.height = EditorGUI.GetPropertyHeight(curviness);
                EditorGUI.PropertyField(rect, curviness);
                EditorGUI.indentLevel--;
            }

            // Snapping
            var snapMode = property.serializedObject.FindProperty("Configuration.SnappingMode");
            var snapToGroundDistance = property.serializedObject.FindProperty("Configuration.SnapDistance");
            var snapOffset = property.serializedObject.FindProperty("Configuration.SnapOffset");
            var snapMask = property.serializedObject.FindProperty("Configuration.SnapMask");

            rect.y += rect.height + RowMargin;
            EditorGUI.PropertyField(rect, snapMode);
            if (snapMode.hasMultipleDifferentValues)
            {
                rect.y += rect.height + RowMargin;
                EditorGUI.indentLevel++;
                rect.height = 16;
                EditorGUI.LabelField(rect, "Editing Multiple Snapping Modes");
                EditorGUI.indentLevel--;
            }
            else if(snapMode.enumValueIndex > 0)
            {
                EditorGUI.indentLevel++;
                rect.y += rect.height + RowMargin;
                rect.height = EditorGUI.GetPropertyHeight(snapOffset);
                EditorGUI.PropertyField(rect, snapOffset, new GUIContent("Y Offset"));
                if (snapMode.enumValueIndex == 3)   // Raycast
                {
                    rect.y += rect.height + RowMargin;
                    rect.height = EditorGUI.GetPropertyHeight(snapToGroundDistance);
                    EditorGUI.PropertyField(rect, snapToGroundDistance, new GUIContent("Raycast Distance"));
                    rect.y += rect.height + RowMargin;
                    rect.height = EditorGUI.GetPropertyHeight(snapMask);
                    EditorGUI.PropertyField(rect, snapMask, new GUIContent("Raycast Mask"));
                }
                EditorGUI.indentLevel--;
            }

            // Control
            var explicitControlEnabled = property.serializedObject.FindProperty("Configuration.IsExplicitControl");
            var explicitControl = property.serializedObject.FindProperty("Configuration.ExplicitControl");

            rect.y += rect.height + RowMargin;
            rect.height = EditorGUI.GetPropertyHeight(explicitControlEnabled);
            EditorGUI.PropertyField(rect, explicitControlEnabled);
            if (explicitControlEnabled.hasMultipleDifferentValues || explicitControlEnabled.boolValue)
            {
                EditorGUI.indentLevel++;
                rect.y += rect.height + RowMargin;
                rect.height = EditorGUI.GetPropertyHeight(explicitControl);
                EditorGUI.PropertyField(rect, explicitControl);
                EditorGUI.indentLevel--;
            }

            if (EditorGUI.EndChangeCheck())
            {
                property.serializedObject.FindProperty("Dirty").boolValue = true;
                property.serializedObject.ApplyModifiedProperties();
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            // Curviness
            var overrideCurviness = property.serializedObject.FindProperty("Configuration.OverrideCurviness");
            var curviness = property.serializedObject.FindProperty("Configuration.Curviness");
            var rect = new Rect(0, 0, 0, EditorGUI.GetPropertyHeight(overrideCurviness));

            if (overrideCurviness.boolValue)
            {
                rect.y += rect.height + RowMargin;
                rect.height = EditorGUI.GetPropertyHeight(curviness);
            }

            // Snapping
            var snapMode = property.serializedObject.FindProperty("Configuration.SnappingMode");
            var snapToGroundDistance = property.serializedObject.FindProperty("Configuration.SnapDistance");
            var snapOffset = property.serializedObject.FindProperty("Configuration.SnapOffset");
            var snapMask = property.serializedObject.FindProperty("Configuration.SnapMask");

            rect.y += rect.height + RowMargin;
            if (snapMode.hasMultipleDifferentValues)
            {
                rect.y += rect.height + RowMargin;
                rect.height = 16;
            }
            else if (snapMode.enumValueIndex > 0)
            {
                rect.y += rect.height + RowMargin;
                rect.height = EditorGUI.GetPropertyHeight(snapOffset);
                if (snapMode.enumValueIndex == 3)   // Raycast
                {
                    rect.y += rect.height + RowMargin;
                    rect.height = EditorGUI.GetPropertyHeight(snapToGroundDistance);
                    rect.y += rect.height + RowMargin;
                    rect.height = EditorGUI.GetPropertyHeight(snapMask);
                }
            }

            // Control
            var explicitControlEnabled = property.serializedObject.FindProperty("Configuration.IsExplicitControl");
            var explicitControl = property.serializedObject.FindProperty("Configuration.ExplicitControl");

            rect.y += rect.height + RowMargin;
            rect.height = EditorGUI.GetPropertyHeight(explicitControlEnabled);
            if (explicitControlEnabled.hasMultipleDifferentValues || explicitControlEnabled.boolValue)
            {
                rect.y += rect.height + RowMargin;
                rect.height = EditorGUI.GetPropertyHeight(explicitControl);
            }
            return rect.y + rect.height;
        }
    }
}