using System;
using System.IO;
using UnityEngine;
/*
namespace MadMaps.Common.Collections
{
    [Serializable]
    public class Serializable2DIntArray : Serializable2DArray<int>
    {
        public Serializable2DIntArray(int width, int data)
            : base(width, data)
        {
        }

        public Serializable2DIntArray(int[,] data)
            : base(data)
        {
        }

        [Pure]
        public Serializable2DIntArray Select(int x, int z, int width, int height)
        {
            if (x + width > Width || z + height > Height)
            {
                throw new IndexOutOfRangeException();
            }
            var result = new Serializable2DIntArray(width, height);
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
                    var val = Mathf.Clamp(this[u, v], 0, 255);
                    ret[u, v] = (byte)(val);
                }
            }
            return ret;
        }

        protected override int ReadFromStream(BinaryReader br)
        {
            return br.ReadInt32();
        }

        protected override Color32 ToColor(int val)
        {
            return 
        }
    }
}*/