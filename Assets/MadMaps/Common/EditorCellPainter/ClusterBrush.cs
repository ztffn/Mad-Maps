using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

namespace MadMaps.Common.Painter
{
    public class ClusterBrush : BaseBrush, IBrush
    {
        public int Radius = 1;
        public float Strength = 1;
        public float Flow = 3;
        public int Iterations = 1;
        public AnimationCurve Falloff = new AnimationCurve(new[] { new Keyframe(0, 1), new Keyframe(1, 1) });

        public override void DrawSpecificGUI()
        {
            Strength = Mathf.Max(0, EditorGUILayout.Slider("Strength", Strength, 0, 1));
            Flow = Mathf.Max(0, EditorGUILayout.FloatField("Flow", Flow));
            Radius = Mathf.Max(0, EditorGUILayoutX.IntSlider("Radius", Radius, 0, 10));
        }

        protected override void DrawSceneGizmos(IGridManager gridManager, Painter.InputState inputState, Rect rect, Matrix4x4 TRS)
        {
            var gridSize = gridManager.GetGridSize();
            Handles.color = Color.white * 0.5f;
            Handles.CircleHandleCap(-1, inputState.PlanePosition, Quaternion.LookRotation(Vector3.up), gridSize * Radius, EventType.Repaint);
            var scaledRad = gridSize * Radius;
            for (var i = -scaledRad; i <= scaledRad; i += gridSize)
            {
                for (var j = -scaledRad; j <= scaledRad; j += gridSize)
                {
                    var pos = inputState.GridPosition + new Vector3(i, 0, j);
                    var circleDist = Vector2.Distance(inputState.GridPosition.xz(), pos.xz());
                    if (circleDist > scaledRad)
                    {
                        continue;
                    }
                    Handles.RectangleHandleCap(-1, pos, Quaternion.LookRotation(Vector3.up), gridSize / 2, EventType.Repaint);
                }
            }
            Handles.color = Color.white;
            Handles.CircleHandleCap(-1, inputState.GridPosition, Quaternion.LookRotation(Vector3.up), gridSize * Radius, EventType.Repaint);
        }

        public override bool Paint(float dt, IPaintable canvas, IGridManager gridManager, Painter.InputState inputState, float minVal, float maxVal, Rect rect, Matrix4x4 TRS)
        {
            bool dirty = false;
            var pos = inputState.GridPosition;
            var baseCell = gridManager.GetCell(pos);
            int stealCells = 2;

            float stolenValue = 0;
            for (var i = -stealCells; i <= stealCells; i += 1)
            {
                for (var j = -stealCells; j <= stealCells; j += 1)
                {
                    if (i == 0 && j == 0)
                    {
                        continue;
                    }

                    var offset = gridManager.GetOffset(i, j);
                    var value = canvas.GetValue(baseCell + offset);

                    if (value <= 0)
                    {
                        continue;
                    }

                    var stealAmount = Mathf.Min(value, Strength);

                    stolenValue += stealAmount;
                    canvas.SetValue(baseCell + offset, value - stealAmount);
                    dirty = true;
                }
            }

            canvas.SetValue(baseCell,canvas.GetValue(baseCell) + stolenValue);
            return dirty;
        }
    }
}
#endif