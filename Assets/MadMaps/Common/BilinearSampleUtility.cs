using System;
using MadMaps.Common.Collections;
using UnityEngine;

namespace MadMaps.Common
{
    public static class BilinearSampleUtility
    {
        public static void StencilBilinearSample(this Serializable2DFloatArray array, Vector2 normalizedCoord, out float strength, bool ignoreNegativeKeys = true)
        {
            if (normalizedCoord.x < 0 || normalizedCoord.x > 1 || normalizedCoord.y < 0 || normalizedCoord.y > 1)
            {
                strength = 0;
                return;
            }
            normalizedCoord = new Vector2(normalizedCoord.x * array.Width, normalizedCoord.y * array.Height);

            int xMin = Mathf.FloorToInt(normalizedCoord.x);
            int xMax = Mathf.CeilToInt(normalizedCoord.x);
            int yMin = Mathf.FloorToInt(normalizedCoord.y);
            int yMax = Mathf.CeilToInt(normalizedCoord.y);

            xMin = Mathf.Clamp(xMin, 0, array.Width - 1);
            xMax = Mathf.Clamp(xMax, 0, array.Width - 1);
            yMin = Mathf.Clamp(yMin, 0, array.Height - 1);
            yMax = Mathf.Clamp(yMax, 0, array.Height - 1);

            float v1 = array[xMin, yMin];
            float v2 = array[xMin, yMax];
            float v3 = array[xMax, yMin];
            float v4 = array[xMax, yMax];

            int v1Index;
            int v2Index;
            int v3Index;
            int v4Index;

            MiscUtilities.DecompressStencil(v1, out v1Index, out v1);
            MiscUtilities.DecompressStencil(v2, out v2Index, out v2);
            MiscUtilities.DecompressStencil(v3, out v3Index, out v3);
            MiscUtilities.DecompressStencil(v4, out v4Index, out v4);

            if (ignoreNegativeKeys)
            {
                v1 = v1Index > 0 ? v1 : 0;
                v2 = v1Index > 0 ? v2 : 0;
                v3 = v1Index > 0 ? v3 : 0;
                v4 = v1Index > 0 ? v4 : 0;
            }

            if (Math.Abs(v1 + v2 + v3 + v4) < .01f)
            {
                strength = 0;
                return;
            }

            float xFrac = normalizedCoord.x.Frac();
            float yFrac = normalizedCoord.y.Frac();

            v1 *= (1 - xFrac) * (1 - yFrac);
            v2 *= (1 - xFrac) * (/*1 - */yFrac);
            v3 *= (/*1 - */xFrac) * (1 - yFrac);
            v4 *= (/*1 - */xFrac) * (/*1 -*/ yFrac);

            strength = Mathf.Clamp01(v1 + v2 + v3 + v4);
        }

        public static void StencilBilinearSample(this Serializable2DFloatArray array, 
            Vector2 normalizedCoord, int stencilKey, out float strength, bool ignoreNegativeKeys = true)
        {
            if (normalizedCoord.x < 0 || normalizedCoord.x > 1 || normalizedCoord.y < 0 || normalizedCoord.y > 1)
            {
                strength = 0;
                return;
            }
            normalizedCoord = new Vector2(normalizedCoord.x * array.Width, normalizedCoord.y * array.Height);

            int xMin = Mathf.FloorToInt(normalizedCoord.x);
            int xMax = Mathf.CeilToInt(normalizedCoord.x);
            int yMin = Mathf.FloorToInt(normalizedCoord.y);
            int yMax = Mathf.CeilToInt(normalizedCoord.y);

            xMin = Mathf.Clamp(xMin, 0, array.Width - 1);
            xMax = Mathf.Clamp(xMax, 0, array.Width - 1);
            yMin = Mathf.Clamp(yMin, 0, array.Height - 1);
            yMax = Mathf.Clamp(yMax, 0, array.Height - 1);

            float v1 = array[xMin, yMin];
            float v2 = array[xMin, yMax];
            float v3 = array[xMax, yMin];
            float v4 = array[xMax, yMax];

            int v1Index;
            int v2Index;
            int v3Index;
            int v4Index;

            MiscUtilities.DecompressStencil(v1, out v1Index, out v1);
            MiscUtilities.DecompressStencil(v2, out v2Index, out v2);
            MiscUtilities.DecompressStencil(v3, out v3Index, out v3);
            MiscUtilities.DecompressStencil(v4, out v4Index, out v4);

            if (ignoreNegativeKeys)
            {
                v1 = v1Index > 0 ? v1 : 0;
                v2 = v1Index > 0 ? v2 : 0;
                v3 = v1Index > 0 ? v3 : 0;
                v4 = v1Index > 0 ? v4 : 0;
            }

            v1 = v1Index == stencilKey ? v1 : 0;
            v2 = v2Index == stencilKey ? v2 : 0;
            v3 = v3Index == stencilKey ? v3 : 0;
            v4 = v4Index == stencilKey ? v4 : 0;

            if (Math.Abs(v1 + v2 + v3 + v4) < .01f)
            {
                strength = 0;
                return;
            }

            float xFrac = normalizedCoord.x.Frac();
            float yFrac = normalizedCoord.y.Frac();

            v1 *= (1 - xFrac) * (1 - yFrac);
            v2 *= (1 - xFrac) * (/*1 - */yFrac);
            v3 *= (/*1 - */xFrac) * (1 - yFrac);
            v4 *= (/*1 - */xFrac) * (/*1 -*/ yFrac);

            strength = Mathf.Clamp01(v1 + v2 + v3 + v4);
        }

        public static float BilinearSample(this float[,] array, Vector2 normalizedCoord)
        {
            if (array == null)
            {
                return 0;
            }

            if (normalizedCoord.x < 0 || normalizedCoord.x > 1 || normalizedCoord.y < 0 || normalizedCoord.y > 1)
                return 0;
            normalizedCoord = new Vector2(normalizedCoord.x * array.GetLength(0), normalizedCoord.y * array.GetLength(1));

            int xMin = Mathf.FloorToInt(normalizedCoord.x);
            int xMax = Mathf.CeilToInt(normalizedCoord.x);
            int yMin = Mathf.FloorToInt(normalizedCoord.y);
            int yMax = Mathf.CeilToInt(normalizedCoord.y);

            xMin = Mathf.Clamp(xMin, 0, array.GetLength(0) - 1);
            xMax = Mathf.Clamp(xMax, 0, array.GetLength(0) - 1);
            yMin = Mathf.Clamp(yMin, 0, array.GetLength(1) - 1);
            yMax = Mathf.Clamp(yMax, 0, array.GetLength(1) - 1);

            float v1 = array[xMin, yMin];
            float v2 = array[xMin, yMax];
            float v3 = array[xMax, yMin];
            float v4 = array[xMax, yMax];

            float xFrac = normalizedCoord.x.Frac();
            float yFrac = normalizedCoord.y.Frac();

            v1 *= (1 - xFrac) * (1 - yFrac);
            v2 *= (1 - xFrac) * (/*1 - */yFrac);
            v3 *= (/*1 - */xFrac) * (1 - yFrac);
            v4 *= (/*1 - */xFrac) * (/*1 -*/ yFrac);

            return v1 + v2 + v3 + v4;
        }

        public static Vector3 BilinearSample(this Vector3[,] array, Vector2 normalizedCoord)
        {
            if (array == null)
            {
                return Vector3.zero;
            }

            if (normalizedCoord.x < 0 || normalizedCoord.x > 1 || normalizedCoord.y < 0 || normalizedCoord.y > 1)
                return Vector3.zero;
            normalizedCoord = new Vector2(normalizedCoord.x * array.GetLength(0), normalizedCoord.y * array.GetLength(1));

            int xMin = Mathf.FloorToInt(normalizedCoord.x);
            int xMax = Mathf.CeilToInt(normalizedCoord.x);
            int yMin = Mathf.FloorToInt(normalizedCoord.y);
            int yMax = Mathf.CeilToInt(normalizedCoord.y);

            xMin = Mathf.Clamp(xMin, 0, array.GetLength(0) - 1);
            xMax = Mathf.Clamp(xMax, 0, array.GetLength(0) - 1);
            yMin = Mathf.Clamp(yMin, 0, array.GetLength(1) - 1);
            yMax = Mathf.Clamp(yMax, 0, array.GetLength(1) - 1);

            var v1 = array[xMin, yMin];
            var v2 = array[xMin, yMax];
            var v3 = array[xMax, yMin];
            var v4 = array[xMax, yMax];

            float xFrac = normalizedCoord.x.Frac();
            float yFrac = normalizedCoord.y.Frac();

            v1 *= (1 - xFrac) * (1 - yFrac);
            v2 *= (1 - xFrac) * (/*1 - */yFrac);
            v3 *= (/*1 - */xFrac) * (1 - yFrac);
            v4 *= (/*1 - */xFrac) * (/*1 -*/ yFrac);

            return v1 + v2 + v3 + v4;
        }

        public static float BilinearSample(this byte[,] array, Vector2 normalizedCoord)
        {
            if (array == null)
            {
                return 0;
            }

            if (normalizedCoord.x < 0 || normalizedCoord.x > 1 || normalizedCoord.y < 0 || normalizedCoord.y > 1)
                return 0;
            normalizedCoord = new Vector2(normalizedCoord.x * array.GetLength(0), normalizedCoord.y * array.GetLength(1));

            int xMin = Mathf.FloorToInt(normalizedCoord.x);
            int xMax = Mathf.CeilToInt(normalizedCoord.x);
            int yMin = Mathf.FloorToInt(normalizedCoord.y);
            int yMax = Mathf.CeilToInt(normalizedCoord.y);

            xMin = Mathf.Clamp(xMin, 0, array.GetLength(0) - 1);
            xMax = Mathf.Clamp(xMax, 0, array.GetLength(0) - 1);
            yMin = Mathf.Clamp(yMin, 0, array.GetLength(1) - 1);
            yMax = Mathf.Clamp(yMax, 0, array.GetLength(1) - 1);

            float v1 = array[xMin, yMin];
            float v2 = array[xMin, yMax];
            float v3 = array[xMax, yMin];
            float v4 = array[xMax, yMax];

            float xFrac = normalizedCoord.x.Frac();
            float yFrac = normalizedCoord.y.Frac();

            v1 *= (1 - xFrac) * (1 - yFrac);
            v2 *= (1 - xFrac) * (/*1 - */yFrac);
            v3 *= (/*1 - */xFrac) * (1 - yFrac);
            v4 *= (/*1 - */xFrac) * (/*1 -*/ yFrac);

            return v1 + v2 + v3 + v4;
        }

        /*
        public static float BilinearSample(this Serializable2DIntArray array, Vector2 normalizedCoord)
        {
            if (array == null)
            {
                return 0;
            }

            if (normalizedCoord.x < 0 || normalizedCoord.x > 1 || normalizedCoord.y < 0 || normalizedCoord.y > 1)
                return 0;
            normalizedCoord = new Vector2(normalizedCoord.x * array.Width, normalizedCoord.y * array.Height);

            int xMin = Mathf.FloorToInt(normalizedCoord.x);
            int xMax = Mathf.CeilToInt(normalizedCoord.x);
            int yMin = Mathf.FloorToInt(normalizedCoord.y);
            int yMax = Mathf.CeilToInt(normalizedCoord.y);

            xMin = Mathf.Clamp(xMin, 0, array.Width - 1);
            xMax = Mathf.Clamp(xMax, 0, array.Width - 1);
            yMin = Mathf.Clamp(yMin, 0, array.Height - 1);
            yMax = Mathf.Clamp(yMax, 0, array.Height - 1);

            float v1 = array[xMin, yMin];
            float v2 = array[xMin, yMax];
            float v3 = array[xMax, yMin];
            float v4 = array[xMax, yMax];

            if (Math.Abs(v1 + v2 + v3 + v4) < Single.Epsilon)
            {
                return 0;
            }

            float xFrac = normalizedCoord.x.Frac();
            float yFrac = normalizedCoord.y.Frac();

            v1 *= (1 - xFrac) * (1 - yFrac);
            v2 *= (1 - xFrac) * (yFrac);
            v3 *= (xFrac) * (1 - yFrac);
            v4 *= (xFrac) * (yFrac);

            return v1 + v2 + v3 + v4;
        }
        */

        public static float BilinearSample(this Serializable2DByteArray array, Vector2 normalizedCoord)
        {
            if (array == null)
            {
                return 0;
            }

            if (normalizedCoord.x < 0 || normalizedCoord.x > 1 || normalizedCoord.y < 0 || normalizedCoord.y > 1)
                return 0;
            normalizedCoord = new Vector2(normalizedCoord.x * array.Width, normalizedCoord.y * array.Height);

            int xMin = Mathf.FloorToInt(normalizedCoord.x);
            int xMax = Mathf.CeilToInt(normalizedCoord.x);
            int yMin = Mathf.FloorToInt(normalizedCoord.y);
            int yMax = Mathf.CeilToInt(normalizedCoord.y);

            xMin = Mathf.Clamp(xMin, 0, array.Width - 1);
            xMax = Mathf.Clamp(xMax, 0, array.Width - 1);
            yMin = Mathf.Clamp(yMin, 0, array.Height - 1);
            yMax = Mathf.Clamp(yMax, 0, array.Height - 1);

            float v1 = array[xMin, yMin];
            float v2 = array[xMin, yMax];
            float v3 = array[xMax, yMin];
            float v4 = array[xMax, yMax];

            if (Math.Abs(v1 + v2 + v3 + v4) < Single.Epsilon)
            {
                return 0;
            }

            float xFrac = normalizedCoord.x.Frac();
            float yFrac = normalizedCoord.y.Frac();

            v1 *= (1 - xFrac) * (1 - yFrac);
            v2 *= (1 - xFrac) * (/*1 - */yFrac);
            v3 *= (/*1 - */xFrac) * (1 - yFrac);
            v4 *= (/*1 - */xFrac) * (/*1 -*/ yFrac);

            return v1 + v2 + v3 + v4;
        }

        public static float BilinearSample(this Serializable2DFloatArray array, 
            Vector2 normalizedCoord)
        {
            if (array == null)
            {
                return 0;
            }

            if (array.Width == 0 && array.Height == 0)
            {
                return 0;
            }

            if (normalizedCoord.x < 0 || normalizedCoord.x > 1 || normalizedCoord.y < 0 || normalizedCoord.y > 1)
                return 0;
            normalizedCoord = new Vector2(normalizedCoord.x * array.Width, normalizedCoord.y * array.Height);

            int xMin = Mathf.FloorToInt(normalizedCoord.x);
            int xMax = Mathf.CeilToInt(normalizedCoord.x);
            int yMin = Mathf.FloorToInt(normalizedCoord.y);
            int yMax = Mathf.CeilToInt(normalizedCoord.y);

            xMin = Mathf.Clamp(xMin, 0, array.Width - 1);
            xMax = Mathf.Clamp(xMax, 0, array.Width - 1);
            yMin = Mathf.Clamp(yMin, 0, array.Height - 1);
            yMax = Mathf.Clamp(yMax, 0, array.Height - 1);

            float v1 = array[xMin, yMin];
            float v2 = array[xMin, yMax];
            float v3 = array[xMax, yMin];
            float v4 = array[xMax, yMax];

            if (Math.Abs(v1 + v2 + v3 + v4) < Single.Epsilon)
            {
                return 0;
            }

            float xFrac = normalizedCoord.x.Frac();
            float yFrac = normalizedCoord.y.Frac();

            v1 *= (1 - xFrac) * (1 - yFrac);
            v2 *= (1 - xFrac) * (/*1 - */yFrac);
            v3 *= (/*1 - */xFrac) * (1 - yFrac);
            v4 *= (/*1 - */xFrac) * (/*1 -*/ yFrac);

            return v1 + v2 + v3 + v4;
        }

    }
}