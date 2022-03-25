using System;
using System.Linq;
using MadMaps.Common;
using MadMaps.Common.Collections;
using UnityEngine;

namespace MadMaps.Terrains
{
    [Serializable]
    public class Stencil : Serializable2DFloatArray 
    {
        public Stencil(int width, int data) : base(width, data)
        {
        }

        public Stencil(float[,] data) : base(data)
        {
        }

        public override Texture2D ToTexture2D(bool normalise, Texture2D tex = null)
        {
            if (tex == null || tex.width != Width || tex.height != Height)
            {
                tex = new Texture2D(Width, Height);
            }
            var colors = new Color32[Width * Height];
            OnBeforeSerialize();
            for (int i = 0; i < Data.Length; i++)
            {
                int stencilKey;
                float value;
                MiscUtilities.DecompressStencil(Data[i], out stencilKey, out value);
                colors[i] = Color.Lerp(Color.black, ColorUtils.GetIndexColor(stencilKey), value);
            }
            tex.SetPixels32(colors);
            tex.Apply();
            return tex;
        }
    }
}