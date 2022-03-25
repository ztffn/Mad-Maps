using UnityEngine;

#if UNITY_EDITOR
namespace MadMaps.Common.Painter
{
    public abstract class BaseBrush : IBrush
    {
        public abstract bool Paint(float dt, IPaintable canvas, IGridManager gridManager, Painter.InputState inputState, float maxValue, float maxVal, Rect rect, Matrix4x4 TRS);

        public void DrawGizmos(MadMaps.Common.Painter.IGridManager gridManager, MadMaps.Common.Painter.Painter.InputState inputState, Rect rect, Matrix4x4 TRS)
        {
            if (!inputState.MouseBlocked)
            {
                DrawSceneGizmos(gridManager, inputState, rect, TRS);
            }
        }

        protected abstract void DrawSceneGizmos(MadMaps.Common.Painter.IGridManager gridManager, MadMaps.Common.Painter.Painter.InputState inputState, Rect rect, Matrix4x4 TRS);

        public void DrawGUI()
        {
            DrawSpecificGUI();
        }

        public abstract void DrawSpecificGUI();
    }
}
#endif