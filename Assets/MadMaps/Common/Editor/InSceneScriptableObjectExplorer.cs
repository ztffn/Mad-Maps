using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MadMaps.Common
{
    public class InSceneScriptableObjectExplorer : EditorWindow
    {
        public static HashSet<Type> ExcludedTypes = new HashSet<Type>()
        {
            typeof(EditorWindow),
            typeof(Editor),
        }; 
        
        [MenuItem("Tools/Mad Maps/Utilities/In-Scene Scriptable Objects")]
        public static void OpenWindow()
        {
            GetWindow<InSceneScriptableObjectExplorer>();
        }

        List<ScriptableObject> _objects = new List<ScriptableObject>();

        void OnGUI()
        {
            titleContent = new GUIContent("In-Scene ScriptableObjects");
            EditorGUILayout.HelpBox("This tool finds and shows ScriptableObjects that are embedded in the scene. These objects can leak and end up greatly increasing your level size and load times.", MessageType.Info);
            if (GUILayout.Button("Refresh"))
            {
                _objects.Clear();
                _objects.AddRange(Resources.FindObjectsOfTypeAll<ScriptableObject>());
                for (int i = _objects.Count-1; i >= 0; i--)
                {
                    var scriptableObject = _objects[i];
                    if (scriptableObject.hideFlags != HideFlags.None)
                    {
                        _objects.RemoveAt(i);
                        continue;
                    }
                    if (AssetDatabase.Contains(scriptableObject))
                    {
                        _objects.RemoveAt(i);
                        continue;
                    }
                    foreach (var excludedType in ExcludedTypes)
                    {
                        if (excludedType.IsInstanceOfType(scriptableObject))
                        {
                            _objects.RemoveAt(i);
                            break;
                        }
                    }
                }
            }

            for (int i = _objects.Count - 1; i >= 0; i--)
            {
                var scriptableObject = _objects[i];
                if (!scriptableObject)
                {
                    _objects.RemoveAt(i);
                    continue;
                }
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.ObjectField(scriptableObject, typeof (ScriptableObject), true);
                if (GUILayout.Button("Destroy", EditorStyles.miniButton, GUILayout.Width(60)) &&
                    EditorUtility.DisplayDialog(string.Format("Destroy {0} [{1}]?", scriptableObject, scriptableObject.GetType()), "This will permanently destroy this object.", "Yes", "No"))
                {
                    Undo.DestroyObjectImmediate(scriptableObject);
                }
                EditorGUILayout.EndHorizontal();
            }
        }
    }
}