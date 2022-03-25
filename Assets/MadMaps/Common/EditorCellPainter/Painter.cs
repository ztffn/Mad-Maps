#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MadMaps.Common.Painter
{
    public delegate Color CellColorDelegate(int cell, Color color);

    public delegate void CellDelegate(int cell);

    public class Painter
    {
        public static List<Rect> UIBlockers = new List<Rect>();
        private IPaintable __canvas;
        private IBrush __currentBrush;
        private InputState _currentInputState;
        private double _lastUpdate;
        // internal variables
        private bool _repaint = true;
        public CellDelegate DoSceneGUIForHoverCell;
        public MadMaps.Common.Painter.IGridManager GridManager;
        public float MaxValue = float.MaxValue;
        public float MinValue = float.MinValue;
        public float Opacity;
        public float PlaneOffset;

        public Gradient Ramp = new Gradient
        {
            colorKeys = new[] {new GradientColorKey(Color.blue, 0), new GradientColorKey(Color.red, 1)}
        };

        public Rect Rect = new Rect(Vector2.zero, Vector2.one*float.MaxValue);
        public CellColorDelegate TransmuteCellColor;
        public Matrix4x4 TRS = Matrix4x4.identity;
        Dictionary<Type, IBrush> _brushCache = null;

        public Painter(MadMaps.Common.Painter.IPaintable canvas, MadMaps.Common.Painter.IGridManager gridManager)
        {
            Canvas = canvas;
            GridManager = gridManager;
            _lastUpdate = EditorApplication.timeSinceStartup;
        }

        public MadMaps.Common.Painter.IPaintable Canvas
        {
            get { return __canvas; }
            set
            {
                if (__canvas != value)
                {
                    __canvas = value;
                    _repaint = true;
                }
            }
        }

        public bool PaintingEnabled { get; set; }

        private MadMaps.Common.Painter.IBrush CurrentBrush
        {
            get
            {
                if (!PaintingEnabled)
                {
                    return null;
                }
                return __currentBrush;
            }
            set
            {
                if (value != __currentBrush)
                {
                    __currentBrush = value;
                    var so = __currentBrush as ScriptableObject;
                    if (so != null)
                    {
                        var guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(so));
                        EditorPrefs.SetString("Painter_LastBrush", guid);
                    }
                }
            }
        }

        /// <summary>
        ///     Entry point for rendering
        /// </summary>
        public void OnSceneGUI()
        {
            if (Event.current.ToString() == "used")
            {
                return;
            }
            if (Event.current.isMouse && Event.current.type == EventType.MouseDrag && Event.current.button == 1)
            {
                return;
            }

            EditorApplication.update -= Update;
            EditorApplication.update += Update;

            var roundedMin = GridManager.GetCellMin(Rect.min.x0z());
            var roundedMax = GridManager.GetCellMin(Rect.max.x0z());
            Rect = Rect.MinMaxRect(roundedMin.x, roundedMin.y, roundedMax.x, roundedMax.y);

            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            Tools.current = Tool.None;

            if (GridManager == null)
            {
                Debug.LogWarning("GridManager was null!");
                return;
            }

            var t = EditorApplication.timeSinceStartup;
            var dt = (float) Mathfx.Clamp(t - _lastUpdate, double.Epsilon, 0.5);
            _lastUpdate = EditorApplication.timeSinceStartup;

            UpdateInputState();
            Paint(dt);
            UpdateVisualisation();

            SceneView.RepaintAll();
        }

        private void Update()
        {
            if (Canvas == null)
            {
                return;
            }
            EditorCellHelper.SetAlive();
        }

        private void Paint(float dt)
        {
            var myEvent = Event.current;
            if (!myEvent.isMouse)
            {
                return;
            }

            if (CurrentBrush == null)
            {
                GetDefaultBrush();
            }
            else if (_currentInputState.MouseDown)
            {
                _repaint = CurrentBrush.Paint(dt, Canvas, GridManager, _currentInputState, MinValue, MaxValue, Rect, TRS);
            }
        }

        private void GetDefaultBrush()
        {
            if (!PaintingEnabled)
            {
                return;
            }

            if(_brushCache == null)
            {
                _brushCache = new Dictionary<Type, IBrush>();
                Type[] types = new Type[]
                {
                    typeof(DefaultBrush),
                    typeof(BlurBrush),
                    typeof(ClusterBrush),
                    typeof(FillBrush),
                };
                foreach(var type in types)
                {
                    var key = "Painter_Brush_" + type.AssemblyQualifiedName;
                    if(EditorPrefs.HasKey(key))
                    {
                        var json = EditorPrefs.GetString(key);
                        var brush = JsonUtility.FromJson(json, type) as MadMaps.Common.Painter.IBrush;
                        _brushCache.Add(type, brush);
                    }
                    else
                    {
                        _brushCache.Add(type, Activator.CreateInstance(type) as MadMaps.Common.Painter.IBrush);
                    }
                }
                
            }

            if (CurrentBrush == null)
            {
                CurrentBrush = _brushCache[typeof(DefaultBrush)];
            }
        }

        private void UpdateInputState()
        {
            // Get mouse point on plane
            var myEvent = Event.current;
            if (!myEvent.isMouse)
            {
                return;
            }

            var blocked = false;
            for (var i = 0; i < UIBlockers.Count; i++)
            {
                if (UIBlockers[i].Contains(myEvent.mousePosition))
                {
                    blocked = true;
                    break;
                }
            }

            var ray = HandleUtility.GUIPointToWorldRay(myEvent.mousePosition);
            var hPlane = new Plane(TRS.GetRotation()*Vector3.up, TRS.MultiplyPoint(new Vector3(0, PlaneOffset, 0)));
            float dist = 0;
            if (!hPlane.Raycast(ray, out dist))
            {
                return;
            }

            var planePos = TRS.inverse.MultiplyPoint((ray.origin + ray.direction*dist));
            var cell = GridManager.GetCell(planePos);
            var quantisedPlanePos = GridManager.GetCellCenter(cell).x0z(planePos.y);

            var lastMouse = _currentInputState.MouseDown;
            var mouseDown = myEvent.button == 0 && myEvent.isMouse &&
                            (myEvent.type == EventType.MouseDown ||
                             myEvent.type == EventType.MouseDrag);

            //planePos = TRS.MultiplyPoint(planePos);
            //quantisedPlanePos = TRS.MultiplyPoint(quantisedPlanePos);

            _currentInputState = new InputState
            {
                PlanePosition = planePos,
                GridPosition = quantisedPlanePos,
                MouseDown = mouseDown && !blocked,
                LastMouseDown = lastMouse,
                MouseBlocked = blocked,
                Shift = myEvent.shift
            };
        }

        private void UpdateVisualisation()
        {
            if (Canvas == null)
            {
                Debug.LogWarning("Trying to draw Painter with null Canvas?");
                return;
            }

            DrawSceneControls();
            for (var i = 0; i < 5; ++i)
            {
                var scale = TRS.GetScale();
                scale = new Vector3(scale.x*Rect.size.x, 0, scale.z*Rect.size.y);
                var rectPos = TRS.MultiplyPoint(Rect.min.x0z(PlaneOffset) + Vector3.up*i + Rect.size.x0z()/2);
                HandleExtensions.DrawWireCube(rectPos, scale/2, TRS.GetRotation(), Color.cyan.WithAlpha(1 - i/5f));
            }
            Handles.color = Color.white;

            if (CurrentBrush != null)
            {
                CurrentBrush.DrawGizmos(GridManager, _currentInputState, Rect, TRS);
            }

            var cellColor = Color.white;
            if (!_currentInputState.MouseBlocked && DoSceneGUIForHoverCell != null)
            {
                DoSceneGUIForHoverCell(GridManager.GetCell(_currentInputState.GridPosition));
            }

            Handles.color = Color.magenta.WithAlpha(.2f);
            Handles.DrawDottedLine(_currentInputState.PlanePosition, _currentInputState.GridPosition, 1);

            _repaint |= Canvas.Dirty;

            EditorCellHelper.SetAlive(); // Tell the cell renderer that it should keep rendering
            EditorCellHelper.CellSize = GridManager.GetGridSize();
            EditorCellHelper.TRS = TRS;
            if (_repaint)
            {
                EditorCellHelper.Clear(false);
                lock (Canvas)
                {
                    Canvas.Dirty = false;
                    var enumerator = Canvas.AllValues();
                    while (enumerator.MoveNext())
                    {
                        cellColor = Ramp.Evaluate((enumerator.Current.Value - MinValue)/MaxValue);
                        cellColor = cellColor.WithAlpha(cellColor.a*Opacity);
                        if (TransmuteCellColor != null)
                        {
                            cellColor = TransmuteCellColor(enumerator.Current.Key, cellColor);
                        }
                        var cellPos = GridManager.GetCellCenter(enumerator.Current.Key).x0z(PlaneOffset);
                        EditorCellHelper.AddCell(cellPos, cellColor);
                    }
                }
                EditorCellHelper.Invalidate();
                _repaint = false;
            }
        }

        
        private void DrawSceneControls()
        {
            if (Event.current.type == EventType.MouseMove)
            {
                return;
            }

            if (Event.current.type == EventType.Repaint)
            {
                UIBlockers.Clear();
            }
            Handles.BeginGUI();
            GUILayout.BeginArea(new Rect(0, 0, 240, Screen.height));

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(EditorCellHelper.UseCPU() ? "Using CPU Painting" : "Using GPU Painting");
            EditorGUILayout.LabelField(string.Format("Grid Size: {0}", GridManager.GetGridSize().ToString("n2")));
            
            var offset = EditorPrefs.GetFloat("Painter_PlaneOffset", 0);
            offset = EditorGUILayout.FloatField("Plane Offset", offset);
            if (!Mathf.Approximately(PlaneOffset, offset))
            {
                PlaneOffset = offset;
                EditorPrefs.SetFloat("Painter_PlaneOffset", offset);
                _repaint = true;
            }

            MinValue = EditorGUILayout.FloatField("Min", MinValue);
            MaxValue = EditorGUILayout.FloatField("Max", MaxValue);
            Ramp = GUIGradientField.GradientField("Ramp", Ramp);

            var opacity = EditorPrefs.GetFloat("Painter_Opacity", 0.5f);
            opacity = Mathf.Clamp01(EditorGUILayout.FloatField("Opacity", opacity));
            if (!Mathf.Approximately(Opacity, opacity))
            {
                Opacity = opacity;
                EditorPrefs.SetFloat("Painter_Opacity", opacity);
                _repaint = true;
            }

            EditorGUILayout.EndVertical();
            if (Event.current.type == EventType.Repaint)
            {
                // Update blocking rect
                var guiRect = GUILayoutUtility.GetLastRect();
                UIBlockers.Add(guiRect);
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();

            if (!PaintingEnabled)
            {
                EditorGUILayout.LabelField("Painting is Disabled");
            }

            GUI.enabled = PaintingEnabled;
            EditorGUILayout.LabelField(CurrentBrush != null ? CurrentBrush.GetType().Name : "Null");
            if (GUILayout.Button("...", EditorStyles.toolbarButton, GUILayout.Width(32)))
            {
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("Paint Brush"), true, () => CurrentBrush = new DefaultBrush());
                menu.AddItem(new GUIContent("Blur Brush"), true, () => CurrentBrush = new BlurBrush());
                menu.AddItem(new GUIContent("Cluster Brush"), true, () => CurrentBrush = new ClusterBrush());
                menu.AddItem(new GUIContent("Fill Brush"), true, () => CurrentBrush = new FillBrush());
                menu.ShowAsContext();
            }

            EditorGUILayout.EndHorizontal();
            if (CurrentBrush != null)
            {
                CurrentBrush.DrawGUI();
            }
            EditorGUILayout.EndVertical();
            if (Event.current.type == EventType.Repaint)
            {
                // Update blocking rect
                var guiRect = GUILayoutUtility.GetLastRect();
                UIBlockers.Add(guiRect);
            }
            GUI.enabled = true;
            GUILayout.EndArea();

            Handles.EndGUI();

            // Save brush
            if (CurrentBrush != null)
            {
                EditorPrefs.SetString("Painter_LastBrushType", CurrentBrush.GetType().AssemblyQualifiedName);
                EditorPrefs.SetString("Painter_Brush_" + CurrentBrush.GetType().AssemblyQualifiedName, JsonUtility.ToJson(CurrentBrush));
            }
        }

        public void Destroy()
        {
            EditorCellHelper.Clear(true);
            EditorApplication.update -= Update;

            //Debug.Log("Destroyed painter");
        }

        public void Repaint()
        {
            _repaint = true;
        }

        public struct InputState
        {
            public Vector3 GridPosition;
            public bool LastMouseDown; // Was the mouse down last frame
            public bool MouseBlocked;
            public bool MouseDown; // Is the mouse down
            public Vector3 PlanePosition;
            public bool Shift;
        }
    }
}
#endif