using System;
using UnityEngine;
using Random = System.Random;

namespace MadMaps.Common
{
    [Serializable]
    public struct Vec3MinMax
    {
        public Vector3 Min, Max;

        public Vec3MinMax(Vector3 min, Vector3 max)
        {
            Min = min;
            Max = max;
        }

        public Vector3 GetRand(Random rand = null)
        {
            if (rand == null)
            {
                return Vector3.Lerp(Min, Max, UnityEngine.Random.value);
            }
            return Vector3.Lerp(Min, Max, (float)rand.NextDouble());
        }
    }
}