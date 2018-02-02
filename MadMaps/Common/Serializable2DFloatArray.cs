using System;
using System.Linq;
using UnityEngine;

namespace MadMaps.Common.Collections
{
    [Serializable]
    public class Serializable2DFloatArray : Serializable2DArray<float>
    {
        public Serializable2DFloatArray(int width, int data) : base(width, data)
        {
        }

        public Serializable2DFloatArray(float[,] data) : base(data)
        {
        }

        public Serializable2DFloatArray Select(int x, int z, int width, int height)
        {
            if (x + width > Width || z + height > Height)
            {
                throw new IndexOutOfRangeException();
            }
            var result = new Serializable2DFloatArray(width, height);
            for (var u = x; u < x + width; ++u)
            {
                for (var v = z; v < z + height; ++v)
                {
                    result[u - x, v - z] = this[u, v];
                }
            }
            return result;
        }

        public Serializable2DByteArray ToBytes()
        {
            var ret = new Serializable2DByteArray(Width, Height);
            for (var u = 0; u < Width; ++u)
            {
                for (var v = 0; v < Height; ++v)
                {
                    var val = this[u, v];
                    ret[u, v] = (byte)(Mathf.Clamp01(val) * 255);
                }
            }
            return ret;
        }

        public Serializable2DFloatArray Select(Coord coord, Coord size)
        {
            return Select(coord.x, coord.z, size.x, size.z);
        }

        public bool IsEmpty()
        {
            if (Height == 0 || Width == 0)
            {
                return true;
            }
            return Data == null || Data.Length == 0;
        }

        public Serializable2DFloatArray Flip()
        {
            var ret = new Serializable2DFloatArray(Height, Width);
            for (var u = 0; u < Width; ++u)
            {
                for (var v = 0; v < Height; ++v)
                {
                    ret[v, u] = this[u, v];
                }
            }
            return ret;
        }
    }
}