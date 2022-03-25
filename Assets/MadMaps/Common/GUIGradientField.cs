#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace MadMaps.Common
{
    public static class GUIGradientField
    {

        #region Initial Setup

        private static MethodInfo s_miGradientField1;
        //private static MethodInfo s_miGradientField2;

        static GUIGradientField()
        {
            // Get our grubby hands on hidden "GradientField" :)
            Type tyEditorGUILayout = typeof(EditorGUILayout);
            s_miGradientField1 = tyEditorGUILayout.GetMethod("GradientField", BindingFlags.NonPublic | BindingFlags.Static, null, new Type[] { typeof(string), typeof(Gradient), typeof(GUILayoutOption[]) }, null);
            //s_miGradientField2 = tyEditorGUILayout.GetMethod("GradientField", BindingFlags.NonPublic | BindingFlags.Static, null, new Type[] { typeof(Gradient), typeof(GUILayoutOption[]) }, null);
        }

        #endregion

        public static Gradient GradientField(string label, Gradient gradient, params GUILayoutOption[] options)
        {
            if (gradient == null)
                gradient = new Gradient();

            gradient = (Gradient)s_miGradientField1.Invoke(null, new object[] { label, gradient, options });

            return gradient;
        }

        public static Gradient GradientField(Gradient gradient, params GUILayoutOption[] options)
        {
            if (gradient == null)
                gradient = new Gradient();

            gradient = (Gradient)s_miGradientField1.Invoke(null, new object[] { gradient, options });

            return gradient;
        }

    }
}
#endif