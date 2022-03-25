#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace MadMaps.Common.GenericEditor
{

    public interface IGenericDrawer
    {
        object DrawGUI(object target, string label = "", Type targetType = null, FieldInfo fieldInfo = null, object context = null);
    }

    public interface ITypedGenericDrawer<T> : IGenericDrawer
    {
    }

    public abstract class GenericDrawer<T> : ITypedGenericDrawer<T>
    {
        public object DrawGUI(object target, string label = "", Type targetType = null, FieldInfo fieldInfo = null, object context = null)
        {
            return DrawGUIInternal((T)target, label, targetType, fieldInfo, context);
        }
        protected abstract T DrawGUIInternal(T target, string label = "", Type targetType = null, FieldInfo fieldInfo = null, object context = null); 
    }

    public class FloatDrawer : GenericDrawer<float>
    {
        protected override float DrawGUIInternal(float target, string label = "", Type targetType = null, FieldInfo fieldInfo = null,
            object context = null)
        {
            if(fieldInfo != null)
            {
                var rangeAttr = fieldInfo.GetAttribute<RangeAttribute>();
                if(rangeAttr != null)
                {
                    return EditorGUILayout.Slider(label, target, rangeAttr.min, rangeAttr.max);
                }
            }
            return EditorGUILayout.FloatField(label, target);
        }
    }

    public class CurveDrawer : GenericDrawer<AnimationCurve>
    {
        protected override AnimationCurve DrawGUIInternal(AnimationCurve target, string label = "", Type targetType = null, FieldInfo fieldInfo = null,
            object context = null)
        {
            if(target == null)
            {
                target = AnimationCurve.Linear(0, 0, 1, 1);
            }
            return EditorGUILayout.CurveField(label, target);
        }
    }

    public class EnumDrawer : GenericDrawer<System.Enum>
    {
        protected override Enum DrawGUIInternal(Enum target, string label = "", Type targetType = null, FieldInfo fieldInfo = null,
            object context = null)
        {
            return EditorGUILayout.EnumPopup(label, target);
        }
    }

    public class IntDrawer : GenericDrawer<int>
    {
        protected override int DrawGUIInternal(int target, string label = "", Type targetType = null, FieldInfo fieldInfo = null,
            object context = null)
        {
            if(fieldInfo != null)
            {
                var rangeAttr = fieldInfo.GetAttribute<RangeAttribute>();
                if(rangeAttr != null)
                {
                    return EditorGUILayout.IntSlider(label, target, Mathf.RoundToInt(rangeAttr.min), Mathf.RoundToInt(rangeAttr.max));
                }
            }
            return EditorGUILayout.IntField(label, target);
        }
    }

    public class ByteDrawer : GenericDrawer<byte>
    {
        protected override byte DrawGUIInternal(byte target, string label = "", Type targetType = null, FieldInfo fieldInfo = null,
            object context = null)
        {
            if(fieldInfo != null)
            {
                var rangeAttr = fieldInfo.GetAttribute<RangeAttribute>();
                if(rangeAttr != null)
                {
                    return (byte)EditorGUILayout.IntSlider(label, target, Mathf.RoundToInt(rangeAttr.min), Mathf.RoundToInt(rangeAttr.max));
                }
            }
            return (byte)EditorGUILayout.IntField(label, target);
        }
    }

    public class DoubleDrawer : GenericDrawer<double>
    {
        protected override double DrawGUIInternal(double target, string label = "", Type targetType = null, FieldInfo fieldInfo = null,
            object context = null)
        {
            if(fieldInfo != null)
            {
                var rangeAttr = fieldInfo.GetAttribute<RangeAttribute>();
                if(rangeAttr != null)
                {
                    return (double)EditorGUILayout.Slider(label, (float)target, rangeAttr.min, rangeAttr.max);
                }
            }
            return EditorGUILayout.DoubleField(label, target);
        }
    }

    public class LongDrawer : GenericDrawer<long>
    {
        protected override long DrawGUIInternal(long target, string label = "", Type targetType = null, FieldInfo fieldInfo = null,
            object context = null)
        {
            return (long)EditorGUILayout.LongField(label, target);
        }
    }

    public class StringDrawer : GenericDrawer<string>
    {
        protected override string DrawGUIInternal(string target, string label = "", Type targetType = null, FieldInfo fieldInfo = null,
            object context = null)
        {
            return EditorGUILayout.TextField(label, target);
        }
    }

    public class BoolDrawer : GenericDrawer<bool>
    {
        protected override bool DrawGUIInternal(bool target, string label = "", Type targetType = null, FieldInfo fieldInfo = null,
            object context = null)
        {
            return EditorGUILayout.Toggle(label, target);
        }
    }

    public class Vector2Drawer : GenericDrawer<Vector2>
    {
        protected override Vector2 DrawGUIInternal(Vector2 target, string label = "", Type targetType = null, FieldInfo fieldInfo = null,
            object context = null)
        {
            return EditorGUILayout.Vector2Field(label, target);
        }
    }

    public class Vector3Drawer : GenericDrawer<Vector3>
    {
        protected override Vector3 DrawGUIInternal(Vector3 target, string label = "", Type targetType = null, FieldInfo fieldInfo = null,
            object context = null)
        {
            return EditorGUILayout.Vector3Field(label, target);
        }
    }

    public class Vector4Drawer : GenericDrawer<Vector4>
    {
        protected override Vector4 DrawGUIInternal(Vector4 target, string label = "", Type targetType = null, FieldInfo fieldInfo = null,
            object context = null)
        {
            return EditorGUILayout.Vector4Field(label, target);
        }
    }

    public class ColorDrawer : GenericDrawer<Color>
    {
        protected override Color DrawGUIInternal(Color target, string label = "", Type targetType = null, FieldInfo fieldInfo = null,
            object context = null)
        {
            return EditorGUILayout.ColorField(label, target);
        }
    }

    public class Vec3MinMaxDrawer : GenericDrawer<Vec3MinMax>
    {
        private const int labelWidth = 70;
        private const int vectorLabelWidth = 30;

        protected override Vec3MinMax DrawGUIInternal(Vec3MinMax target, string label = "", Type targetType = null, FieldInfo fieldInfo = null,
            object context = null)
        {
            var indentLevel = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            EditorGUILayout.BeginHorizontal(/*GUILayout.MaxWidth(maxWidth)*/);
            GUILayout.Space(indentLevel * 16);
            EditorGUILayout.LabelField(label, GUILayout.Width(labelWidth));
            EditorGUILayout.LabelField("Min", EditorStyles.miniLabel, GUILayout.Width(vectorLabelWidth));
            target.Min = EditorGUILayout.Vector3Field(GUIContent.none, target.Min);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal(/*GUILayout.MaxWidth(maxWidth)*/);
            GUILayout.Space(indentLevel * 16);
            EditorGUILayout.LabelField("", GUILayout.Width(labelWidth));
            EditorGUILayout.LabelField("Max", EditorStyles.miniLabel, GUILayout.Width(vectorLabelWidth));
            target.Max = EditorGUILayout.Vector3Field(GUIContent.none, target.Max);
            EditorGUILayout.EndHorizontal();
            EditorGUI.indentLevel = indentLevel;
            return target;
        }
    }

    public class FloatMinMaxDrawer : GenericDrawer<FloatMinMax>
    {
        protected override FloatMinMax DrawGUIInternal(FloatMinMax target, string label = "", Type targetType = null, FieldInfo fieldInfo = null,
            object context = null)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(150));
            EditorGUILayout.LabelField("Min", GUILayout.Width(64));
            target.Min = EditorGUILayout.FloatField(target.Min);
            EditorGUILayout.LabelField("Max", GUILayout.Width(64));
            target.Max = EditorGUILayout.FloatField(target.Max);
            EditorGUILayout.EndHorizontal();
            return target;
        }
    }

    public class LayerMaskDrawer : GenericDrawer<LayerMask>
    {
        protected override LayerMask DrawGUIInternal(LayerMask target, string label = "", Type targetType = null, FieldInfo fieldInfo = null,
            object context = null)
        {
            return LayerMaskFieldUtility.LayerMaskField(label, target, true);
        }
    }

    public class UnityObjectDrawer : GenericDrawer<UnityEngine.Object>
    {
        protected override UnityEngine.Object DrawGUIInternal(UnityEngine.Object target, string label = "", Type targetType = null, FieldInfo fieldInfo = null,
            object context = null)
        {
            var objType = typeof(UnityEngine.Object);
            if (targetType != null)
            {
                objType = targetType;
            }
            else if (fieldInfo != null)
            {
                objType = fieldInfo.FieldType;
            }
            return EditorGUILayout.ObjectField(label, target, objType, true);
        }
    }
    
    public class IListDrawer : GenericDrawer<IList>
    {
        protected override IList DrawGUIInternal(IList target, string label = "", Type targetType = null,
            FieldInfo fieldInfo = null,
            object context = null)
        {
            var expandedKey = fieldInfo != null ? fieldInfo.Name : context + "list";
            var listType = targetType.IsArray ? targetType.GetElementType() : targetType.GetGenericArguments()[0];
            ListGenericUIAttribute listAttribute = fieldInfo != null ? fieldInfo.GetAttribute<ListGenericUIAttribute>() : null;
            if (!GenericEditor.ExpandedFieldCache.ContainsKey(expandedKey))
            {
                GenericEditor.ExpandedFieldCache[expandedKey] = false;
            }
            if (fieldInfo != null)
            {
                GenericEditor.ExpandedFieldCache[expandedKey] =
                    EditorGUILayout.Foldout(GenericEditor.ExpandedFieldCache[expandedKey], fieldInfo.Name, EditorStyles.boldFont);
            }

            if (fieldInfo == null || GenericEditor.ExpandedFieldCache[expandedKey])
            {
                for (var i = 0; i < target.Count; i++)
                {
                    EditorGUI.indentLevel++;
                    var o = target[i];
                    if (!GenericEditor.ExpandedFieldCache.ContainsKey(expandedKey + i))
                    {
                        GenericEditor.ExpandedFieldCache[expandedKey + i] = false;
                    }

                    if (typeof (UnityEngine.Object).IsAssignableFrom(listType))
                    {
                        EditorGUILayout.BeginHorizontal();
                        o = EditorGUILayout.ObjectField((UnityEngine.Object)o, listType, true);
                        if (GUILayout.Button(GenericEditor.DeleteContent, EditorStyles.label, GUILayout.Width(20)))
                        {
                            if (targetType.IsArray)
                            {
                                target = ((Array)target).Remove(i);
                            }
                            else
                            {
                                target.RemoveAt(i);
                            }
                            break;
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    else
                    {
                        EditorGUILayout.BeginHorizontal();

                        string objectLabel = "NULL";
                        if (o != null)
                        {
                            objectLabel = o.ToString();
                        }
                        EditorGUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
                        GenericEditor.ExpandedFieldCache[expandedKey + i] = 
                            EditorGUILayout.Foldout(GenericEditor.ExpandedFieldCache[expandedKey + i], objectLabel);
                        EditorGUILayout.EndHorizontal();

                        if (o != null && typeof(IHelpLinkProvider).IsAssignableFrom(listType))
                        {
                            var helpURL = (o as IHelpLinkProvider).HelpURL;
                            if (!string.IsNullOrEmpty(helpURL))
                            {
                                EditorExtensions.HelpButton(helpURL);
                            }
                        }
                        
                        if (GUILayout.Button(GenericEditor.DeleteContent, EditorStyles.label, GUILayout.Width(20)))
                        {
                            target.RemoveAt(i);
                            break;
                        }

                        var enableToggle = o as IShowEnableToggle;
                        if (enableToggle != null)
                        {
                            var lastIndent = EditorGUI.indentLevel;
                            EditorGUI.indentLevel = 0;
                            enableToggle.Editor_Enabled = EditorGUILayout.Toggle(GUIContent.none, enableToggle.Editor_Enabled, GUILayout.Width(20));
                            EditorGUI.indentLevel = lastIndent;
                        }
                        
                        EditorGUILayout.EndHorizontal();

                        if (GenericEditor.ExpandedFieldCache[expandedKey + i])
                        {
                            if (o != null)
                            {
                                o = GenericEditor.DrawGUI(o, "", o.GetType(), fieldInfo, target);
                            }
                            else
                            {
                                o = GenericEditor.DrawGUI(null, "Null", listType, fieldInfo, target);
                            }
                        }
                    }
                    target[i] = o;
                    EditorGUI.indentLevel--;
                    EditorExtensions.Seperator();
                }
                
                if (!typeof(UnityEngine.Object).IsAssignableFrom(listType) && (listType.IsAbstract || listType.IsInterface || (fieldInfo != null && listAttribute != null && listAttribute.AllowDerived)))
                {
                    EditorGUILayoutX.DerivedTypeSelectButton(listType, (o) => target.Add(o));
                }
                else if(EditorGUILayoutX.IndentedButton("Add " + listType.Name))
                {
                    if (typeof(UnityEngine.Object).IsAssignableFrom(listType))
                    {
                        if (targetType.IsArray)
                        {
                            target = ((Array) target).Add(null);
                        }
                        else
                        {
                            target.Add(null);
                        }
                    }
                    else
                    {
                        var newInstance = Activator.CreateInstance(listType);
                        if (newInstance != null && !newInstance.Equals(null))
                        {
                            if (targetType.IsArray)
                            {
                                target = ((Array)target).Add(newInstance);
                            }
                            else
                            {
                                target.Add(newInstance);
                            }
                        }
                    }
                }
            }
            return target;
        }
    }
}
#endif