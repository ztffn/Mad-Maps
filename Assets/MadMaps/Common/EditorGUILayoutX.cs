#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using MadMaps.Common.GenericEditor;
using UnityEditor;
using UnityEngine;

namespace MadMaps.Common
{
    public delegate T ReturnAction<T>(T obj);
    public delegate bool FilterAction<T>(T obj);
    public static class EditorGUILayoutX
    {
        public static int IntSlider(string label, int value, int min, int max)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label);
            value = EditorGUILayout.IntSlider(value, min, max);
            EditorGUILayout.EndHorizontal();
            return value;
        }

        public static T Indent<T>(Func<T> function, int indent = 16)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(indent);
            var ret = (T)function.DynamicInvoke();
            EditorGUILayout.EndHorizontal();
            return ret;
        }

        public static void DrawList<T>(List<T> list,
            List<bool> expanded, ReturnAction<T> action,
            FilterAction<T> filterAction,
            bool scrollable = false,
            bool oneLine = false)
        {
            if (list == null)
            {
                EditorGUILayout.HelpBox("List was null!", MessageType.Info);
                return;
            }

            if (scrollable)
            {
                var scroll = new Vector2(0, PlayerPrefs.GetFloat("shigivar__attachmentpointscroll", 0));
                scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.MaxHeight(600), GUILayout.MinHeight(60));
                PlayerPrefs.SetFloat("shigivar__attachmentpointscroll", scroll.y);
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(16 * EditorGUI.indentLevel);
            EditorGUILayout.BeginVertical("Box"/*, GUILayout.MinHeight(64)*/);

            int? removal = null;
            for (int i = 0; i < list.Count; i++)
            {
                var item = list[i];

                if (expanded != null)
                {
                    if (expanded.Count >= i)
                    {
                        expanded.Add(false);
                    }
                }

                if ((filterAction != null && !filterAction(item)))
                {
                    continue;
                }

                EditorGUILayout.BeginHorizontal();
                if (expanded != null)
                {
                    expanded[i] = EditorGUILayout.Foldout(expanded[i], String.Format("{0}: {1}", i, item.ToString()));
                }
                if (GUILayout.Button("C", GUILayout.Width(20)))
                {
                    list.Add(SerializableCopy(list[list.Count - 1]));
                    continue;
                }
                if (GUILayout.Button("-", GUILayout.Width(20)))
                {
                    removal = i;
                    continue;
                }
                if (!oneLine)
                {
                    EditorGUILayout.EndHorizontal();
                }
                if (expanded == null || expanded[i])
                {
                    var indent = EditorGUI.indentLevel;
                    EditorGUI.indentLevel = 0;
                    list[i] = action(item);
                    EditorGUI.indentLevel = indent;
                }
                if (oneLine)
                {
                    EditorGUILayout.EndHorizontal();
                }
            }
            if (GUILayout.Button("+"))
            {
                if (list.Count == 0 || typeof(UnityEngine.Object).IsAssignableFrom(typeof(T)))
                {
                    list.Add(default(T));
                }
                else
                {
                    list.Add(SerializableCopy(list[list.Count - 1]));
                }
            }
            if (removal.HasValue)
            {
                list.RemoveAt(removal.Value);
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
            if (scrollable)
            {
                EditorGUILayout.EndScrollView();
            }
        }

        private static T SerializableCopy<T>(T p)
        {
            return JsonUtility.FromJson<T>(JsonUtility.ToJson(p));
        }

        public static int Toolbar(int currentValue, GUIContent[] toolbarItems, float width, float height)
        {
            var currentWidthCounter = 0f;
            EditorGUILayout.BeginHorizontal();
            int columnCounter = 0;

            for (int i = 0; i < toolbarItems.Length; i++)
            {
                var style = EditorStyles.miniButtonMid;
                if (columnCounter == 0)
                {
                    style = EditorStyles.miniButtonLeft;
                }
                style.stretchWidth = true;
                var contentWidth = style.CalcSize(toolbarItems[i]).x;
                currentWidthCounter += contentWidth;

                if (currentWidthCounter > width)
                {
                    currentWidthCounter = contentWidth;
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    columnCounter = 0;
                }
                
                if (i == currentValue)
                {
                    GUI.enabled = false;
                    GUILayout.Button(toolbarItems[i], style, GUILayout.ExpandWidth(true), GUILayout.Height(height));
                    GUI.enabled = true;
                }
                else
                {
                    if (GUILayout.Button(toolbarItems[i], style, GUILayout.ExpandWidth(true), GUILayout.Height(height)))
                    {
                        return i;
                    }
                }
                columnCounter++;
            }
            EditorGUILayout.EndHorizontal();
            return currentValue;
        }


        public static void DerivedTypeSelectButton<T>(Action<T> callback)
        {
            DerivedTypeSelectButton(typeof(T), (t) => { callback((T)t); });
        }

        //Shows a button that when clicked, pops a context menu with a list of tasks deriving the base type specified. When something is selected the callback is called
        //On top of that it also shows a search field for Tasks
        public static void DerivedTypeSelectButton(Type baseType, Action<object> callback)
        {
            Action<Type> TaskTypeSelected = (t) =>
            {
                var newTask = Activator.CreateInstance(t);
                callback(newTask);
            };

            Func<GenericMenu> GetMenu = () =>
            {
                var menu = GetTypeSelectionMenu(baseType, TaskTypeSelected);
                return menu;
            };
            
            GUILayout.BeginHorizontal();
            if (IndentedButton("Add " + baseType.Name.SplitCamelCase()))
            {
                GetMenu().ShowAsContext();
                Event.current.Use();
            }
            GUILayout.EndHorizontal();
            
            GUI.backgroundColor = Color.white;
        }

        //a cool label :-P (for headers)
        public static bool IndentedButton(string text)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(EditorGUI.indentLevel * 16 + 6);
            var result = GUILayout.Button(text);
            GUILayout.EndHorizontal();
            return result;
        }

        public static bool IndentedButton(Texture2D text, params GUILayoutOption[] options )
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(EditorGUI.indentLevel * 16 + 6);
            var result = GUILayout.Button(text, options);
            GUILayout.EndHorizontal();
            return result;
        }

        ///Get a selection menu of types deriving base type
        public static GenericMenu GetTypeSelectionMenu(Type baseType, Action<Type> callback)
        {
            var menu = new GenericMenu();

            var types = baseType.GetAllChildTypes().Where(t => !t.IsAbstract);
            foreach (var type in types)
            {
                if (type.HasAttribute<InternalManagedType>())
                {
                    continue;
                }
                menu.AddItem(new GUIContent(GenericEditor.GenericEditor.GetFriendlyName(type)), false, () => callback(type));
            }

            return menu;
        }        
    }
}

#endif