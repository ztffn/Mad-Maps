using UnityEngine;

namespace MadMaps.Common
{
    public static class ColorUtils
    {
        public static Color GetIndexColor(int index)
        {
            Random.InitState(index);
            return Random.ColorHSV(0f, 1f, 1f, 1f, 1f, 1f);
        }
    }
}