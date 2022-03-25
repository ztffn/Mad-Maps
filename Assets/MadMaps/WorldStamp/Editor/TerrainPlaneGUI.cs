using System.Collections;
using System.Collections.Generic;
using MadMaps.WorldStamps;
using MadMaps.Common;
using UnityEditor;
using UnityEngine;

namespace MadMaps.WorldStamps
{
    [CustomEditor(typeof(TerrainPlane))]
    [CanEditMultipleObjects]
    public class TerrainPlaneGUI : LayerComponentBaseGUI
    {
        private SerializedProperty _setHeights,
            _blendMode,
            _falloffMode,
            _falloffCurve,
            _falloffTexture,
            _layerName,
            _offset,
            _size,
            _objectsEnabled,
            _regex,
            _treesEnabled,
            _ignoredTrees,
            _grassEnabled,
            _ignoredDetails,
            _setSplat,
            _splat,
            _splatStrength,
            _priority;

        public void OnEnable()
        {
            _setHeights = serializedObject.FindProperty("SetHeights");
            _blendMode = serializedObject.FindProperty("BlendMode");
            _falloffMode = serializedObject.FindProperty("FalloffMode");
            _falloffCurve = serializedObject.FindProperty("Falloff");
            _falloffTexture = serializedObject.FindProperty("FalloffTexture");
            _layerName = serializedObject.FindProperty("LayerName");
            _offset = serializedObject.FindProperty("Offset");
            _size = serializedObject.FindProperty("AreaSize");

            _objectsEnabled = serializedObject.FindProperty("RemoveObjects");
            _regex = serializedObject.FindProperty("IgnoredObjectsRegex");

            _treesEnabled = serializedObject.FindProperty("RemoveTrees");
            _ignoredTrees = serializedObject.FindProperty("IgnoredTrees");

            _grassEnabled = serializedObject.FindProperty("RemoveGrass");
            _ignoredDetails = serializedObject.FindProperty("IgnoredDetails");

            _setSplat = serializedObject.FindProperty("SetSplat");
            _splat = serializedObject.FindProperty("Splat");
            _splatStrength = serializedObject.FindProperty("SplatStrength");
            _priority = serializedObject.FindProperty("Priority");
        }

        public override void OnInspectorGUI()
        {
            DoGenericUI(_layerName, true);
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(_layerName);
            EditorGUILayout.PropertyField(_priority);
            EditorGUILayout.PropertyField(_offset);
            EditorGUILayout.PropertyField(_size);
            EditorGUILayout.PropertyField(_falloffMode);
            EditorGUI.indentLevel++;
            if (_falloffMode.hasMultipleDifferentValues || _falloffMode.enumValueIndex == 0 ||
                _falloffMode.enumValueIndex == 1)
            {
                EditorGUILayout.PropertyField(_falloffCurve);
            }
            if (_falloffMode.hasMultipleDifferentValues || _falloffMode.enumValueIndex == 2)
            {
                EditorGUILayout.PropertyField(_falloffTexture);
            }
            EditorGUI.indentLevel--;

            EditorExtensions.Seperator();
            EditorGUILayout.PropertyField(_setHeights);
            if(_setHeights.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_blendMode);
                EditorGUI.indentLevel--;
            }
            
            EditorExtensions.Seperator();
            EditorGUILayout.PropertyField(_objectsEnabled);
            if(_objectsEnabled.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_regex);
                EditorGUI.indentLevel--;
            }

            EditorExtensions.Seperator();
            EditorGUILayout.PropertyField(_treesEnabled);
            if(_treesEnabled.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_ignoredTrees, true);
                EditorGUI.indentLevel--;
            }

            EditorExtensions.Seperator();
            EditorGUILayout.PropertyField(_grassEnabled);
            if(_grassEnabled.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_ignoredDetails, true);
                EditorGUI.indentLevel--;
            }

            EditorExtensions.Seperator();
            EditorGUILayout.PropertyField(_setSplat);
            if(_setSplat.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_splat);
                EditorGUILayout.PropertyField(_splatStrength);
                EditorGUI.indentLevel--;
            }

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
