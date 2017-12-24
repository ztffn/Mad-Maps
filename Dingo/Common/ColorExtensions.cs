using UnityEngine;

namespace Dingo.Common
{
    public static class ColorExtensions
    {
        public static string ToHexString(this Color color)
        {
            return string.Format(
                "#{0}{1}{2}{3}",
                ((int)(color.r * 255f)).ToString("X2"),
                ((int)(color.g * 255f)).ToString("X2"),
                ((int)(color.b * 255f)).ToString("X2"),
                ((int)(color.a * 255f)).ToString("X2")
            );
        }

        public static string ToHexString(this Color32 color)
        {
            return ((Color)color).ToHexString();
        }

        public static Color WithAlpha(this Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, Mathf.Clamp01(alpha));
        }

        public static Color32 WithAlpha(this Color32 color, byte alpha)
        {
            return new Color32(color.r, color.g, color.b, alpha);
        }
    }
}