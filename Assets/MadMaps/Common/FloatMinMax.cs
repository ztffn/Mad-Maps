using System;
using UnityEngine;
using Random = System.Random;

namespace MadMaps.Common
{
    [Serializable]
    public struct FloatMinMax
    {
        public float Min, Max;

        public FloatMinMax(float min, float max)
        {
            Min = min;
            Max = max;
        }

        public float GetRand(Random rand)
        {
            var randomVal = (float)rand.NextDouble();
            var val = Mathf.Lerp(Min, Max, randomVal);
            return val;
        }
    }
}