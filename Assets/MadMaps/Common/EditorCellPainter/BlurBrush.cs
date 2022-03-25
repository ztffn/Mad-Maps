using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using Painter = MadMaps.Common.Painter.Painter;
using IGridManager = MadMaps.Common.Painter.IGridManager;
using GridManagerInt = MadMaps.Common.Painter.GridManagerInt;
using IBrush = MadMaps.Common.Painter.IBrush;
using EditorCellHelper = MadMaps.Common.Painter.EditorCellHelper;

namespace MadMaps.Common.Painter
{
    public class BlurBrush : BaseBrush, IBrush
    {
        private double _lastPaint;
        public EBrushShape BrushShape;
        public AnimationCurve Falloff = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 1));
        public float Flow = 3;
        public int Radius = 2;
        [Range(0, 1)]
        public float Strength = 1;
        public int Iterations = 1;

        public override bool Paint(float dt, IPaintable canvas, IGridManager gridManager, Painter.InputState inputState, float minVal, float maxVal, Rect rect, Matrix4x4 TRS)
        {
            var dirty = false;
            var scaledRad = gridManager.GetGridSize()*Radius;
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
                    
                    //var existingVal = canvas.GetValue(cell);

                    var sum = 0f;
                    for(var iter = 0; iter < Iterations; ++iter)
                    {
                        for(var u = -1; u <= 1; ++u)
                        {
                            for(var v = -1; v <= 1; ++v)
                            {
                                var neighbourPos = inputState.GridPosition + new Vector3(i + u * gridManager.GetGridSize(), 0, j + v * gridManager.GetGridSize());
                                var neighbourCell = gridManager.GetCell(neighbourPos);
                                sum += canvas.GetValue(neighbourCell);
                            }
                        }
                    }

                    val = Mathf.Lerp(sum / 9, val, Strength * Flow * dt);
                    val = Mathf.Clamp(val, minVal, maxVal);

                    canvas.SetValue(cell, val);
                    dirty = true;
                }
            }
            return dirty;
        }

        public override void DrawSpecificGUI()
        {
            Strength = Mathf.Max(0, EditorGUILayout.FloatField("Strength", Strength));
            Radius = Mathf.Clamp(EditorGUILayout.IntField("Radius", Radius), 0, 32);
            Falloff = EditorGUILayout.CurveField("Falloff", Falloff, Color.white, new Rect(0, 0, 1, 1));
            BrushShape = (EBrushShape) EditorGUILayout.EnumPopup("Shape", BrushShape);
            Flow = Mathf.Max(0, EditorGUILayout.FloatField("Flow", Flow));
        }

        protected override void DrawSceneGizmos(MadMaps.Common.Painter.IGridManager gridManager, MadMaps.Common.Painter.Painter.InputState inputState, Rect rect, Matrix4x4 TRS)
        {
            Radius = Mathf.Clamp(Radius, 0, 32);
            //var scaledRad = gridManager.GetGridSize()*Radius;

            var translatedPlanePos = TRS.MultiplyPoint(inputState.PlanePosition);
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
            }
        }
    }
}
#endif
