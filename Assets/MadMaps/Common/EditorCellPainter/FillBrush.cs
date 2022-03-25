using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;

namespace MadMaps.Common.Painter
{

    public class FillBrush : MadMaps.Common.Painter.BaseBrush, MadMaps.Common.Painter.IBrush
    {
        private double _lastPaint;
        Vector2 Range = new Vector2(0, 1);
        public float Strength = 1;
        Queue<int> _openCells = new Queue<int>();
        HashSet<int> _closedCells = new HashSet<int>();

        public override bool Paint(float dt, MadMaps.Common.Painter.IPaintable canvas, MadMaps.Common.Painter.IGridManager gridManager, MadMaps.Common.Painter.Painter.InputState inputState, float minVal, float maxVal, Rect rect, Matrix4x4 TRS)
        {
            var dirty = false;

            _openCells.Clear();
            _closedCells.Clear();
            _openCells.Enqueue(gridManager.GetCell(inputState.GridPosition));

            while(_openCells.Count > 0)
            {
                var nextCell = _openCells.Dequeue();
                if (_closedCells.Contains(nextCell) || (rect.size.magnitude > 0 && rect.Contains(gridManager.GetCellCenter(nextCell))))
                {
                    _closedCells.Add(nextCell);
                    continue;
                }
                _closedCells.Add(nextCell);
                var value = canvas.GetValue(nextCell);
                if(value < Range.x || value <= Range.y)
                {
                    continue;
                }
                canvas.SetValue(nextCell, Strength);
                _openCells.Enqueue(gridManager.GetCell(inputState.GridPosition + new Vector3(-1, 0, 0)));
                _openCells.Enqueue(gridManager.GetCell(inputState.GridPosition + new Vector3(1, 0, 0)));
                _openCells.Enqueue(gridManager.GetCell(inputState.GridPosition + new Vector3(0, 0, 1)));
                _openCells.Enqueue(gridManager.GetCell(inputState.GridPosition + new Vector3(0, 0, -1)));
            }

            /*var scaledRad = gridManager.GetGridSize();
            for (var i = -scaledRad; i <= scaledRad; i += gridManager.GetGridSize())
            {
                for (var j = -scaledRad; j <= scaledRad; j += gridManager.GetGridSize())
                {
                    var pos = inputState.GridPosition + new Vector3(i, 0, j);
                    var cell = gridManager.GetCell(pos);

                    if (rect.size.magnitude > 0 && !rect.Contains(pos.xz()))
                    {
                        canvas.RemoveCell(cell);
                        continue;
                    }

                    var normalisedDist = 1f;
                    if (BrushShape == EBrushShape.Circle)
                    {
                        var circleDist = Vector2.Distance(inputState.GridPosition.xz(), pos.xz());
                        if (circleDist > scaledRad)
                        {
                            continue;
                        }
                        normalisedDist = circleDist/scaledRad;
                    }
                    else
                    {
                        normalisedDist = Mathf.Abs(inputState.GridPosition.x - pos.x) +
                                         Mathf.Abs(inputState.GridPosition.y - pos.y);
                        normalisedDist /= scaledRad;
                    }

                    var falloff = 1f;
                    if (!float.IsNaN(normalisedDist))
                    {
                        falloff = Falloff.Evaluate(normalisedDist);
                    }
                    var val = Strength*falloff;
                    
                    var existingVal = canvas.GetValue(cell);

                    val = BrushUtilities.BlendValues(val, existingVal, brushBlendMode, dt, Flow);
                    val = Mathf.Clamp(val, minVal, maxVal);

                    canvas.SetValue(cell, val);
                    dirty = true;
                }
            }*/
            return dirty;
        }

        public override void DrawSpecificGUI()
        {
            Strength = Mathf.Max(0, EditorGUILayout.FloatField("Strength", Strength));
            EditorGUILayout.MinMaxSlider("Range", ref Range.x, ref Range.y, 0, 1);
        }

        protected override void DrawSceneGizmos(MadMaps.Common.Painter.IGridManager gridManager, MadMaps.Common.Painter.Painter.InputState inputState, Rect rect, Matrix4x4 TRS)
        {
            /*var translatedPlanePos = TRS.MultiplyPoint(inputState.PlanePosition);
            var translatedGridPos = TRS.MultiplyPoint(inputState.GridPosition);
            var planeUp = TRS.GetRotation()*Vector3.up;
            var planeForward = TRS.GetRotation()*Vector3.forward;
            var planeRot = Quaternion.LookRotation(planeUp, planeForward);

            Handles.color = Color.white*0.5f;
            if (BrushShape == EBrushShape.Circle)
            {
                Handles.CircleHandleCap(-1, translatedPlanePos, planeRot,
                    gridManager.GetGridSize()*Radius, EventType.Repaint);
            }
            else
            {
                Handles.RectangleHandleCap(-1, translatedGridPos, planeRot,
                    gridManager.GetGridSize()*Radius, EventType.Repaint);
            }
            Handles.color = Color.white;
            if (BrushShape == EBrushShape.Circle)
            {
                Handles.CircleHandleCap(-1, translatedGridPos, planeRot,
                    gridManager.GetGridSize()*Mathf.Max(.5f, Radius), EventType.Repaint);
            }
            else
            {
                Handles.RectangleHandleCap(-1, translatedGridPos, planeRot,
                    gridManager.GetGridSize()*Mathf.Max(.5f, Radius), EventType.Repaint);
            }*/
        }
    }
}
#endif