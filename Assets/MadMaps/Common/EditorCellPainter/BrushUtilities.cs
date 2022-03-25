using System;
using UnityEngine;

#if UNITY_EDITOR
namespace MadMaps.Common.Painter
{
    public static class BrushUtilities
    {
        public static float BlendValues(float val, float existingVal, EBrushBlendMode brushBlendMode, float dt, float flow)
        {
            switch (brushBlendMode)
            {
                case EBrushBlendMode.Set:
                    return val;
                case EBrushBlendMode.Max:
                    return Mathf.Max(val, existingVal);
                case EBrushBlendMode.Min:
                    return Mathf.Min(val, existingVal);
                case EBrushBlendMode.Average:
                    return (val + existingVal) / 2;
                case EBrushBlendMode.Add:
                    return existingVal + (val * dt * flow);
                case EBrushBlendMode.Subtract:
                    return existingVal - (val * dt * flow);
            }
            throw new NotImplementedException("Unsupported Blend Mode!");
        }
    }
}
#endif