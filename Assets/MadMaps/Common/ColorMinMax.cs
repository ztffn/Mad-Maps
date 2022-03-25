using System;
using UnityEngine;
using Random = System.Random;

namespace MadMaps.Common
{
    [Serializable]
    public struct ColorMinMax
    {
        public Color Min, Max;

        public ColorMinMax(Color min, Color max)
        {
            Min = min;
            Max = max;
        }

        public Color GetRand(Random rand)
        {
            return Color.Lerp(Min, Max, (float)rand.NextDouble());
        }
    }
}