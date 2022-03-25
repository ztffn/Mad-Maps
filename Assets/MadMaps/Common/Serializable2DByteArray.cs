using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace MadMaps.Common.Collections
{
    [Serializable]
    public class Serializable2DByteArray : Serializable2DArray<byte>
    {
        public Serializable2DByteArray(int width, int data)
            : base(width, data)
        {
        }

        public Serializable2DByteArray(byte[,] data)
            : base(data)
        {
        }

        public Serializable2DByteArray Select(int x, int z, int width, int height)
        {
            if (x + width > Width || z + height > Height)
            {
                throw new IndexOutOfRangeException();
            }
            var result = new Serializable2DByteArray(width, height);
            for (var u = x; u < x + width; ++u)
            {
                for (var v = z; v < z + height; ++v)
                {
                    result[u - x, v - z] = this[u, v];
                }
            }
            return result;
        }

        public int[,] DeserializeToInt()
        {
            var data = new int[Width, Height];
            for (var u = 0; u < Width; u++)
            {
                for (var v = 0; v < Height; v++)
                {
                    var index = v * Width + u;
                    data[u, v] = Data[index];
                }
            }
            return data;
        }

        public void DeserializeToInt(int[,] data)
        {
            if (data.GetLength(0) != Width || data.GetLength(1) != Height)
            {
                throw new Exception("Data array wrong size!");
            }
            for (var u = 0; u < Width; u++)
            {
                for (var v = 0; v < Height; v++)
                {
                    var index = v * Width + u;
                    data[u, v] = Data[index];
                }
            }
        }

        protected override byte ReadFromStream(BinaryReader br)
        {
            return br.ReadByte();
        }

        public override Texture2D ToTexture2D(bool normalise, Texture2D tex = null)
        {
            byte min = 0;
            byte max = 255;
            if (normalise)
            {
                min = Data.Min();
                max = Data.Max();
            }
            if (tex == null || tex.width != Width || tex.height != Height)
            {
                tex = new Texture2D(Width, Height);
            }
            var colors = new Color32[Width * Height];
            OnBeforeSerialize();
            for (int i = 0; i < Data.Length; i++)
            {
                var val = (Data[i] - min) / (float)max;
                colors[i] = new Color(val, val, val, 1);
            }
            tex.SetPixels32(colors);
            tex.Apply();
            return tex;
        }
    }
}