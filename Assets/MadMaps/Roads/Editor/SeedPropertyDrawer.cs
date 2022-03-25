using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

namespace MadMaps.Roads
{
    [CustomPropertyDrawer(typeof(SeedAttribute))]
    public class SeedPropertyDrawer : PropertyDrawer
    {
        private const float Size = 20;
        private const float Margin = 2;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var propRect = position;
            propRect.width -= Size + Margin;
            EditorGUI.PropertyField(propRect, property);
            var buttonRect = new Rect(propRect.xMax + Margin, position.y, Size, position.height);
            var buttonContent = EditorGUIUtility.IconContent("TreeEditor.Refresh");
            buttonContent.tooltip = "Recalculate a random seed";
            if (GUI.Button(buttonRect, buttonContent, EditorStyles.boldLabel))
            {
                foreach (var o in property.serializedObject.targetObjects)
                {
                    fieldInfo.SetValue(o, Random.Range(int.MinValue, int.MaxValue));
                }
            }
        }
    }
}