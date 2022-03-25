using MadMaps.Common.Collections;
using System.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MadMaps.Common
{
    public interface IDataInspectorProvider
    {
        Texture2D ToTexture2D(bool normalise, Texture2D tex = null);
        string AuxData { get; }
    }

    public class PositionList : List<Vector3>, IDataInspectorProvider
    {
        public Texture2D ToTexture2D(bool normalise, Texture2D tex = null)
        {
            const int size = 1024;
            /*if(tex == null)
            {*/
            tex = new Texture2D(size, size);
            var c = new Color[size * size];
            ListExtensions.Fill((Array)c, Color.black);
            tex.SetPixels(c);
            /*}
            else if(tex.width != size || tex.height != size)
            {
                tex.Resize(size, size);
            }
            tex.();*/
            
            foreach(var vec3 in this)
            {
                int x = 1 + Mathf.RoundToInt(vec3.x * 1022f);
                int z = 1 + Mathf.RoundToInt(vec3.z * 1022f);

                tex.SetPixel(x, z, Color.white);                
                tex.SetPixel(x - 1, z, Color.white);
                tex.SetPixel(x + 1, z, Color.white);
                tex.SetPixel(x, z - 1, Color.white);
                tex.SetPixel(x, z + 1, Color.white);

                tex.SetPixel(x - 1, z - 1, Color.white);
                tex.SetPixel(x + 1, z - 1, Color.white);
                tex.SetPixel(x - 1, z + 1, Color.white);
                tex.SetPixel(x + 1, z + 1, Color.white);
            }
            return tex;
        }

        public string AuxData {get{return string.Empty;}}
    }

#if UNITY_EDITOR
    
    public class DataInspector : EditorWindow
    {
        public class DataEntry
        {
            public IDataInspectorProvider Data;
            public object Context;
            public Texture2D Texture;
            public string Aux;
        }

        private static List<DataEntry> _entries = new List<DataEntry>(); 
        private static int _index;
        private Vector2 _scroll;

        public static void SetData(IDataInspectorProvider data, object context = null, bool normalise = false)
        {
            Dispose();
            if(data == null)
            {
                Debug.LogWarning("Attempted to view null data in the Data Inspector! " + context);
                return;
            }
            var texture = data.ToTexture2D(normalise);
            _entries.Add(new DataEntry()
            {
                Context = context,
                Data = data,
                Texture = texture,
            });
            GetWindow<DataInspector>();
        }

        public static void SetData(List<IDataInspectorProvider> datas, List<object> contexts, bool normalise = false)
        {
            Dispose();
            _entries.Clear();
            for (int i = 0; i < datas.Count; i++)
            {
                var canInspectInDataInspector = datas[i];
                if (canInspectInDataInspector == null)
                {
                    Debug.LogError("Attempted to preview null Data!");
                    continue;
                }

                var texture = canInspectInDataInspector.ToTexture2D(normalise);
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.Apply();
                var context = contexts[i];

                _entries.Add(new DataEntry()
                {
                    Context = context,
                    Data = canInspectInDataInspector,
                    Texture = texture,
                    Aux = canInspectInDataInspector.AuxData,
                });
            }
            GetWindow<DataInspector>();
        }

        static void Dispose()
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];
                if (entry != null && entry.Texture)
                {
                    DestroyImmediate(entry.Texture);
                }
            }
            _entries.Clear();
            _index = 0;
        }

        void OnDisable()
        {
            Dispose();
        }

        void OnGUI()
        {
            titleContent = new GUIContent("Data Inspector", GUIResources.EyeOpenIcon);
            if (_entries.IsNullOrEmpty())
            {
                EditorGUILayout.HelpBox("No Data", MessageType.Info);
                return;
            }           
            
            var entry = _entries[_index];
            if (entry != null && entry.Texture != null)
            {
                var aspect = entry.Texture.height / (float)entry.Texture.width;
                var windowSize = this.position.size;
                var size = Math.Min(windowSize.x, windowSize.y);
                var scaledSizeFactor = 1f;
                if (entry.Texture.width > entry.Texture.height)
                {
                    scaledSizeFactor = 1 / (size * (1 / aspect) / windowSize.y);
                }
                else
                {
                    scaledSizeFactor = 1 / (size * aspect / windowSize.x);
                }
                size *= scaledSizeFactor;
                _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.Width(size), GUILayout.Height(windowSize.y-100));
                EditorGUILayout.BeginVertical(GUILayout.Width(size), GUILayout.Height(size * aspect));

                GUILayout.Box("", GUILayout.Width(size - 25), GUILayout.Height(size * aspect - 25));
                var lastRect = GUILayoutUtility.GetLastRect();
                lastRect.position += Vector2.one * 4;
                lastRect.size -= Vector2.one * 8;
                GUI.DrawTexture(lastRect, entry.Texture);

                EditorGUILayout.EndVertical();
                EditorGUILayout.EndScrollView();

                EditorGUILayout.LabelField(entry.Aux);
            }
            else
            {
                EditorGUILayout.HelpBox("Failed to generate preview for " + entry.Context, MessageType.Error);
            }
            
            EditorExtensions.Seperator();
            if (_entries.Count > 1)
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("<", GUILayout.Width(30)))
                {
                    _index--;
                }
                GUILayout.Label(string.Format("{0}/{1}", _index+1, _entries.Count), GUILayout.ExpandWidth(true));
                GUILayout.Label(entry.Context != null ? entry.Context.ToString() : "Null");
                if (GUILayout.Button(">", GUILayout.Width(30)))
                {
                    _index++;
                }
                EditorGUILayout.EndHorizontal();
            }
            _index = Mathf.Clamp(_index, 0, _entries.Count - 1);

            if (GUILayout.Button("Normalize Preview"))
            {
                Normalise(entry);
            }

            if (GUILayout.Button("Save as Texture"))
            {
                var path = EditorUtility.SaveFilePanel("Save Data", "", "data", "png");
                if (!string.IsNullOrEmpty(path))
                {
                    File.WriteAllBytes(path, entry.Texture.EncodeToPNG());
                }
            }
        }

        private void Normalise(DataEntry entry)
        {
            entry.Texture = entry.Data.ToTexture2D(true, entry.Texture);
        }
    }
#endif
}