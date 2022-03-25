using UnityEngine;

#if UNITY_EDITOR
namespace MadMaps.Common.Painter
{
    public interface IBrush
    {
        void DrawGizmos(MadMaps.Common.Painter.IGridManager gridManager, MadMaps.Common.Painter.Painter.InputState inputState, Rect rect, Matrix4x4 TRS);
        void DrawGUI();
        bool Paint(float dt, MadMaps.Common.Painter.IPaintable canvas, MadMaps.Common.Painter.IGridManager gridManager, MadMaps.Common.Painter.Painter.InputState currentInputState, float minValue, float maxValue, Rect rect, Matrix4x4 TRS);
    }
}
#endif