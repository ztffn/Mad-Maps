using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

namespace MadMaps.Common
{
    public static class EditorExtensions
    {
        public static void HelpButton(string url, float size = 20)
        {
            var content = EditorGUIUtility.IconContent("_Help");
            content.text = null;
            content.tooltip = "Open Documentation";
            if (GUILayout.Button(content, EditorStyles.label, GUILayout.Width(size), GUILayout.Height(size)))
            {
                Application.OpenURL(url);
            }
        }

        [MenuItem("CONTEXT/MeshCollider/Dump Info")]
        public static void DumpMeshColliderInfo(MenuCommand command)
        {
            var mc = command.context as MeshCollider;
            Debug.Log(mc.bounds);
        }

        [MenuItem("GameObject/Hierarchy/Alphabeticise")]
        public static void Alphabeticise()
        {
            var s = Selection.gameObjects;
            s = s.OrderBy(o => o.name).ToArray();
            foreach (var gameObject in s)
            {
                gameObject.transform.SetAsLastSibling();
            }
        }

        public static void DrawArrow(Vector3 start, Vector3 end, Color color, float size)
        {
            Gizmos.color = color;

            var delta = end - start;
            var up = Vector3.up * size;
            var p1 = start + delta * .75f;
            
            Gizmos.DrawLine(start + up * .5f, start - up * .5f);
            Gizmos.DrawLine(start + up * .5f, p1 + up * .5f);
            Gizmos.DrawLine(start - up * .5f, p1 - up * .5f);
            Gizmos.DrawLine(p1 + up * .5f, p1 + up);
            Gizmos.DrawLine(p1 - up * .5f, p1 - up);

            Gizmos.DrawLine(p1 + up, end);
            Gizmos.DrawLine(p1 - up, end);
        }

        private static GUIStyle _seperator = new GUIStyle("box")
        {
            border = new RectOffset(0, 0, 1, 0),
            margin = new RectOffset(0, 0, 0, 1),
            padding = new RectOffset(0, 0, 0, 1)
        };

        public static string OpenFilePanel(string title, string extension, string directory = null, bool assetPath = true)
        {
            bool persistantString = String.IsNullOrEmpty(directory);
            if (persistantString)
            {
                directory = EditorPrefs.GetString("sMap_EditorExtensions_OpenFilePanel");
                if (!directory.Contains(Application.dataPath))
                {
                    directory = Application.dataPath + "/" + directory;
                }
            }
            //Debug.Log("Directory was " + directory);
            var path = EditorUtility.OpenFilePanel(title, directory, extension);
            if (String.IsNullOrEmpty(path))
            {
                return path;
            }
            if (assetPath)
            {
                path = path.Substring(path.IndexOf("Assets/", StringComparison.Ordinal));
            }
            if (!persistantString)
            {
                return path;
            }
            directory = path.Substring(0, path.LastIndexOf("/", StringComparison.Ordinal));
            if (assetPath)
            {
                directory = directory.Replace(Application.dataPath, String.Empty);
            }
            EditorPrefs.SetString("sMap_EditorExtensions_OpenFilePanel", directory);
            //Debug.Log("Set persistent OpenFilePanel to " + directory);
            return path;
        }

        public static string SaveFilePanel(string title, string defaultName, string extension, string directory = null, bool assetPath = true)
        {
            bool persistantString = String.IsNullOrEmpty(directory);
            if (persistantString)
            {
                directory = EditorPrefs.GetString("sMap_EditorExtensions_OpenFilePanel");
                if (!directory.Contains(Application.dataPath))
                {
                    directory = Application.dataPath + "/" + directory;
                }
            }
            //Debug.Log("Directory was " + directory);
            var path = EditorUtility.SaveFilePanel(title, directory, defaultName, extension);
            if (String.IsNullOrEmpty(path))
            {
                return path;
            }
            if (assetPath)
            {
                path = path.Substring(path.IndexOf("Assets/", StringComparison.Ordinal));
            }
            if (!persistantString)
            {
                return path;
            }
            directory = path.Substring(0, path.LastIndexOf("/", StringComparison.Ordinal));
            if (assetPath)
            {
                directory = directory.Replace(Application.dataPath, String.Empty);
            }
            EditorPrefs.SetString("sMap_EditorExtensions_OpenFilePanel", directory);
            //Debug.Log("Set persistent OpenFilePanel to " + directory);
            return path;
        }

        public static void Seperator()
        {
            _seperator.margin.left = EditorGUI.indentLevel * 16 + 6;
            GUILayout.Box(GUIContent.none, _seperator, GUILayout.Height(1), GUILayout.ExpandWidth(true));
        }
        

        public static object GetParent(this SerializedProperty prop)
        {
            var path = prop.propertyPath.Replace(".Array.data[", "[");
            object obj = prop.serializedObject.targetObject;
            var elements = path.Split('.');
            foreach (var element in elements.Take(elements.Length - 1))
            {
                if (element.Contains("["))
                {
                    var elementName = element.Substring(0, element.IndexOf("["));
                    var index = Convert.ToInt32(element.Substring(element.IndexOf("[")).Replace("[", "").Replace("]", ""));
                    obj = GetValue(obj, elementName, index);
                }
                else
                {
                    obj = GetValue(obj, element);
                }
            }
            return obj;
        }

        private static object GetValue(object source, string name)
        {
            if (source == null)
                return null;
            var type = source.GetType();
            var f = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            if (f == null)
            {
                var p = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (p == null)
                    return null;
                return p.GetValue(source, null);
            }
            return f.GetValue(source);
        }

        private static object GetValue(object source, string name, int index)
        {
            var enumerable = GetValue(source, name) as IEnumerable;
            var enm = enumerable.GetEnumerator();
            while (index-- >= 0)
                enm.MoveNext();
            return enm.Current;
        }
    }
}
#endif