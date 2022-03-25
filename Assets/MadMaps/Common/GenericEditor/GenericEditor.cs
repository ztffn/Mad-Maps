
using Object = UnityEngine.Object;
using System.Collections.Generic;
using System.Collections;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace MadMaps.Common.GenericEditor
{
    public interface IHelpLinkProvider
    {
        string HelpURL { get; }
    }

    public interface IShowEnableToggle
    {
        bool Editor_Enabled { get; set; }
    }

    public static class GenericEditor
    {
        public static string GetFriendlyName(Type t)
        {
            var nameAttr = t.GetAttribute<NameAttribute>();
            if (nameAttr != null)
            {
                return nameAttr.Name;
            }
            return t.Name.SplitCamelCase();
        }

        public static string GetFriendlyName(FieldInfo fieldInfo)
        {
            var nameAttr = fieldInfo.GetAttribute<NameAttribute>();
            if (nameAttr != null)
            {
                return nameAttr.Name;
            }
            return fieldInfo.Name.SplitCamelCase();
        }

#if UNITY_EDITOR
        public static Dictionary<string, bool> ExpandedFieldCache = new Dictionary<string, bool>();

        private static Dictionary<Type, IGenericDrawer> _activeDrawers; // Mapping of IGeneriDrawer types to type
        private static Dictionary<Type, IGenericDrawer> _drawerTypeMapping = new Dictionary<Type, IGenericDrawer>();    // Mapping of all types to their drawer
        private static Dictionary<Type, List<FieldInfo>> _typeFieldCache = new Dictionary<Type, List<FieldInfo>>();

        public static GUIContent DeleteContent
        {
            get
            {
                var content = EditorGUIUtility.IconContent("TreeEditor.Trash");
                content.tooltip = "Delete";
                content.text = null;
                return content;
            }
        }

        public static bool HasDrawer(Type t)
        {
            return GetDrawer(t) != null;
        }
        
        private static IGenericDrawer GetDrawer(Type type)
        {
            IGenericDrawer drawer;
            if(_drawerTypeMapping.TryGetValue(type, out drawer) && drawer != null)
            {
                return drawer;
            }

            if(_activeDrawers == null)
            {
                _activeDrawers = new Dictionary<Type, IGenericDrawer>();
                var allTypes = typeof(IGenericDrawer).GetAllTypesImplementingInterface();
                foreach(var t in allTypes)
                {
                    if (t.IsAbstract)
                    {
                        continue;
                    }
                    _activeDrawers.Add(t, null);
                }
            }

            // Interface is type = 0
            // Subclass = distance from actual class
            // Exact match - instant return
            Type bestDrawer = null;
            int bestScore = Int32.MinValue;
            foreach(var mapping in _activeDrawers)
            {
                var targetType = mapping.Key;
                var interfaces = targetType.GetInterfaces();
                foreach(var interfaceType in interfaces)
                {
                    var genArgs = interfaceType.GetGenericArguments();
                    if (genArgs.Length != 1)
                    {
                        continue;
                    }

                    var genericArg = genArgs[0];
                    if (genericArg == type)
                    {
                        bestDrawer = targetType;
                        break;
                    }
                    if (!genericArg.IsAssignableFrom(type))
                    {
                        continue;
                    }
                    var distance = type.GetInheritanceDistance(genericArg);
                    if (distance > bestScore)
                    {
                        bestDrawer = targetType;
                    }
                } 
            }
            if (bestDrawer == null)
            {
                return null;
            }
            if (!_activeDrawers.TryGetValue(bestDrawer, out drawer) || drawer == null)
            {  
                drawer = (IGenericDrawer) Activator.CreateInstance(bestDrawer);
            }
            _drawerTypeMapping[type] = drawer;
            return drawer;
        }

        private static List<FieldInfo> GetFields(Type type)
        {
            List<FieldInfo> result;
            if (!_typeFieldCache.TryGetValue(type, out result) || result == null)
            {
                var allFields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                result = new List<FieldInfo>();
                foreach (var fieldInfo in allFields)
                {
                    var attributes = fieldInfo.GetCustomAttributes(true);
                    if (attributes.Any(o => o is HideInInspector))
                    {
                        continue;
                    }
                    if (attributes.Any(o => o is NonSerializedAttribute))
                    {
                        continue;
                    }
                    if (fieldInfo.IsPrivate && !attributes.Any(o => o is SerializeField))
                    {
                        continue;
                    }
                    result.Add(fieldInfo);
                }
                _typeFieldCache[type] = result;
            }
            return result;
        }
#endif
        
        public static object DrawGUI(object target, string label = "", Type targetType = null, FieldInfo fieldInfo = null, object context = null)
        {
#if UNITY_EDITOR
            if (targetType == null && target == null && fieldInfo == null)
            {
                Debug.LogError("Insufficient information to determine type.");
                return null;
            }

            if (targetType == null)
            {
                targetType = target != null ? target.GetType() : fieldInfo.FieldType;
            }

            var drawer = GetDrawer(targetType);
            if (context != null && drawer != null)
            {
                target = drawer.DrawGUI(target, label, targetType, fieldInfo, context);
            }
            else
            {
                if(target != null)
                {
                    var isInList = context != null && typeof(IList).IsAssignableFrom(context.GetType());
                    var isUnityObject = typeof(UnityEngine.Object).IsAssignableFrom(targetType);
                    if(!isInList && !isUnityObject && !targetType.IsAssignableFrom(typeof(IList)) && !targetType.IsArray)
                    {
                        EditorGUILayout.BeginHorizontal();
                        if(string.IsNullOrEmpty(label) && fieldInfo != null)
                        {
                            label = fieldInfo.Name;
                        }
                        EditorGUILayout.LabelField(string.Format("{0} [{1}]", label, targetType));
                        if (GUILayout.Button(GenericEditor.DeleteContent, EditorStyles.label, GUILayout.Width(20)))
                        {
                            return null;
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    
                    if (fieldInfo != null)
                    {
                        //Debug.Log(string.Format("FieldInfo for {0} wasn't null, so increased IndentLevel from {1} to {2}", fieldInfo.Name, EditorGUI.indentLevel, EditorGUI.indentLevel +1));
                        EditorGUI.indentLevel++;
                    }
                    var fields = GetFields(targetType);
                    foreach (var field in fields)
                    {
                        var subObj = field.GetValue(target);
                        subObj = DrawGUI(subObj, GetFriendlyName(field), subObj != null ? subObj.GetType() : field.FieldType, field, target);
                        field.SetValue(target, subObj);
                    }
                    if (fieldInfo != null)
                    {
                        //Debug.Log(string.Format("FieldInfo for {0} wasn't null, so decreased IndentLevel from {1} to {2}", fieldInfo.Name, EditorGUI.indentLevel, EditorGUI.indentLevel -1));
                        EditorGUI.indentLevel--;
                    }
                }
                else
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(label);
                    if (targetType.IsAbstract || targetType.IsInterface)
                    {
                        EditorGUILayoutX.DerivedTypeSelectButton(targetType, (o) => 
                            {
                            fieldInfo.SetValue(context, o);
                            }
                        );
                    }
                    else if(EditorGUILayoutX.IndentedButton("Add " + targetType.Name))
                    {
                        target = Activator.CreateInstance(targetType);
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }

            var unityObj = target as Object;
            if (!Application.isPlaying && unityObj)
            {
                EditorUtility.SetDirty(unityObj);
                EditorSceneManager.MarkAllScenesDirty();
            }
#endif
            return target;
        }
    }
}