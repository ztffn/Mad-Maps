#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.Rendering;

namespace MadMaps.Common.Painter
{
    public static class EditorCellHelper
    {
        private const string ShaderName = "Hidden/EditorCellShader";

        private const int MAX_VERTS = 65534;
        private const string ForceCPUKey = "MadMaps_PainteForceGPU";
        public static bool ForceCPU {
            get
            {
                return EditorPrefs.GetBool(ForceCPUKey, false);
            }
            set
            {
                EditorPrefs.SetBool(ForceCPUKey, value);
            }
        }
        public static bool UseCPU()
        {
            if(EditorCellHelper.ForceCPU)
            {
                return true;
            }
            if(SystemInfo.graphicsShaderLevel < 45)
            {
                return true;
            }
            return SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLCore;
        }
        public static float CellSize
        {
            get { return __cellSize; }
            set
            {
                if (value != __cellSize)
                {
                    __cellSize = value;
                    Invalidate();
                }
            }
        }
        
        private static float __cellSize = 1;
        private static Material _material;

        private static List<EditorCellRenderer> _rendererList = new List<EditorCellRenderer>();

        public struct Cell
        {
            public Vector3 Center;
            public Color Color;
        }

        private static List<Cell> _cells = new List<Cell>();

        private static double _lastAliveTime;
        public const double AutoClearTime = .1;
        private static bool _dirty;
        public static Matrix4x4 TRS = Matrix4x4.identity;

        static EditorCellHelper()
        {
            SceneView.onSceneGUIDelegate -= OnSceneGUIDelegate;
            SceneView.onSceneGUIDelegate += OnSceneGUIDelegate;
        }

        private static void OnSceneGUIDelegate(SceneView sceneview)
        {
            var t = EditorApplication.timeSinceStartup;
            
            if (_dirty)
            {
                Invalidate();
            }
            else if (_lastAliveTime + AutoClearTime < t)
            {
                //Debug.Log("Auto cleared");
                Clear(true);
            }
        }

        public static void SetAlive()
        {
            _lastAliveTime = EditorApplication.timeSinceStartup;
            if (_material)
            {
                _material.SetFloat("_Size", CellSize);
            }
            for(var i = 0; i < _rendererList.Count; ++i)
            {
                var renderer = _rendererList[i];
                renderer.SetAlive();
            }
            if(_rendererList.Count == 0 && _cells.Count > 0)
            {
                _dirty = true;
            }
            SceneView.onSceneGUIDelegate -= OnSceneGUIDelegate;
            SceneView.onSceneGUIDelegate += OnSceneGUIDelegate;
        }

        public static void AddCell(Vector3 center, Color color)
        {
            _cells.Add(new Cell()
            {
                Center = center,
                Color = color,
            });
            _dirty = true;
            SceneView.onSceneGUIDelegate -= OnSceneGUIDelegate;
            SceneView.onSceneGUIDelegate += OnSceneGUIDelegate;
        }

        public static void Register(EditorCellRenderer r)
        {
            if (!_rendererList.Contains(r))
            {
                _rendererList.Add(r);
            }
            SceneView.onSceneGUIDelegate -= OnSceneGUIDelegate;
            SceneView.onSceneGUIDelegate += OnSceneGUIDelegate;
        }

        // Change to invalidation
        public static void Invalidate()
        {
            if (_cells.Count == 0)
            {
                for (int i = 0; i < _rendererList.Count; i++)
                {
                    if(_rendererList[i])
                    {
                        Object.DestroyImmediate(_rendererList[i].gameObject);
                    }                    
                }
                _rendererList.Clear();
                return;
            }

            if (_material == null)
            {
                var shader = Shader.Find(ShaderName);
                if (shader == null)
                {
                    Debug.LogError("Failed to find shader " + ShaderName);
                    return;
                }
                _material = new Material(shader) { hideFlags = HideFlags.DontSave };
            }
            _material.SetFloat("_Size", CellSize);

            for(var i = _rendererList.Count - 1; i >= 0; i--)
            {
                if(!_rendererList[i])
                {
                    _rendererList.RemoveAt(i);
                }
            }

            int requiredRendererCount = Mathf.CeilToInt(_cells.Count / (float)(UseCPU() ? (MAX_VERTS-4) / 4 : MAX_VERTS));

            //Debug.Log(string.Format("{0} = {1}", _cells.Count, requiredRendererCount));
            for (int i = 0; i < requiredRendererCount; i++)
            {
                if (_rendererList.Count <= i)
                {
                    _rendererList.Add(GetNewRenderer());
                }
                if (_rendererList[i] == null)
                {
                    _rendererList[i] = GetNewRenderer();
                }

                EditorCellRenderer renderer = _rendererList[i];
                renderer.Clear();
                renderer.SetData(TRS, _material);                
            }
            for (int i = _rendererList.Count - 1; i >= requiredRendererCount; i--)
            {
                Object.DestroyImmediate(_rendererList[i].gameObject);
                _rendererList.RemoveAt(i);
            }

            if(requiredRendererCount == 0)
            {
                return;
            }

            
            var activeRenderer = _rendererList[0];
            int index = 0;
            for (int i = 0; i < _cells.Count; i++)
            {
                var cell = _cells[i];
                activeRenderer.QueueCell(cell);
                if (activeRenderer.VertCount > (UseCPU() ? MAX_VERTS - 4 : MAX_VERTS))
                {
                    index++;
                    activeRenderer = _rendererList[index];
                }
            }

            for (int i = 0; i < _rendererList.Count; i++)
            {
                _rendererList[i].Finalise();
            }

            _lastAliveTime = EditorApplication.timeSinceStartup;
            _dirty = false;

            SceneView.onSceneGUIDelegate -= OnSceneGUIDelegate;
            SceneView.onSceneGUIDelegate += OnSceneGUIDelegate;
            SceneView.RepaintAll();
        }



        private static EditorCellRenderer GetNewRenderer()
        {
            var newGo = new GameObject("grid_temp_6217354124");
            var r = newGo.AddComponent<EditorCellRenderer>();
            newGo.hideFlags = HideFlags.DontSave;
            newGo.transform.ApplyTRSMatrix(TRS);

            return r;
        }

        public static void Clear(bool invalidate)
        {
            _cells.Clear();
            if (invalidate)
            {
                Invalidate();
            }
        }
    }
}

#endif