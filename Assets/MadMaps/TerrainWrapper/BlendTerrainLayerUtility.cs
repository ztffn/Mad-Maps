using System;
using MadMaps.Common;
using MadMaps.Common.Collections;
using UnityEngine;

namespace MadMaps.Terrains
{
    public static class BlendMMTerrainLayerUtility
    {
        public static int UseCount = 0;

        public static void BlendArray(ref Serializable2DByteArray baseData,
            Serializable2DByteArray blendingData,
            Serializable2DFloatArray stencil,
            MMTerrainLayer.EMMTerrainLayerBlendMode blendMode,
            Common.Coord offset, Common.Coord size)
        {
            if (blendingData == null)
            {
                return;
            }
            if (baseData == null)
            {
                baseData = blendingData;
                return;
            }

            var width = baseData.Width;
            var height = baseData.Height;
            if (TerrainWrapper.ComputeShaders && ShouldCompute(baseData))
            {
                var blendShader = ComputeShaderPool.GetShader("MadMaps/WorldStamp/ComputeShaders/BlendIntArray");
                int kernelHandle = blendShader.FindKernel("BlendInts");

                var baseAsInt = baseData.Data.ConvertToIntArray();  // TODO hmmmmm
                var blendAsInt = blendingData.Data.ConvertToIntArray();

                //var baseBuffer = new ComputeBuffer(baseData.Width * baseData.Height, sizeof(float));
                var baseBuffer = ComputeShaderPool.GetBuffer(baseData.Width*baseData.Height, sizeof (float));
                baseBuffer.SetData(baseAsInt);
                blendShader.SetBuffer(kernelHandle, "_Base", baseBuffer);
                blendShader.SetVector("_BaseSize", new Vector4(baseData.Width, baseData.Height));

                //var blendBuffer = new ComputeBuffer(blendingData.Width * blendingData.Height, sizeof(float));
                var blendBuffer = ComputeShaderPool.GetBuffer(blendingData.Width * blendingData.Height, sizeof(float));
                blendBuffer.SetData(blendAsInt);
                blendShader.SetBuffer(kernelHandle, "_Blend", blendBuffer);
                blendShader.SetVector("_BlendSize", new Vector4(blendingData.Width, blendingData.Height));

                ComputeBuffer stencilBuffer = null;
                if (stencil != null && stencil.Width > 0 && stencil.Height > 0)
                {
                    //stencilBuffer = new ComputeBuffer(stencil.Width * stencil.Height, sizeof(float));
                    stencilBuffer = ComputeShaderPool.GetBuffer(stencil.Width * stencil.Height, sizeof(float));
                    stencilBuffer.SetData(stencil.Data);
                    blendShader.SetBuffer(kernelHandle, "_Stencil", stencilBuffer);
                    blendShader.SetVector("_StencilSize", new Vector4(stencil.Width, stencil.Height));
                }
                else
                {
                    stencilBuffer = ComputeShaderPool.GetBuffer(1, sizeof(float));
                    blendShader.SetBuffer(kernelHandle, "_Stencil", stencilBuffer);
                    blendShader.SetVector("_StencilSize", new Vector4(0, 0));
                }

                blendShader.SetInt("_BlendMode", (int)blendMode);
                blendShader.SetVector("_MinMax", new Vector4(offset.x, offset.z, 0, 0));

                blendShader.Dispatch(kernelHandle, baseData.Width, baseData.Height, 1);

                baseBuffer.GetData(baseAsInt);
                for (int i = 0; i < baseAsInt.Length; i++)
                {
                    baseData.Data[i] = (byte)baseAsInt[i];
                }

                /*baseBuffer.Release();
                baseBuffer.Dispose();
                blendBuffer.Release();
                blendBuffer.Dispose();*/
                ComputeShaderPool.ReturnBuffer(baseBuffer);
                ComputeShaderPool.ReturnBuffer(blendBuffer);
                if (stencilBuffer != null)
                {
                    ComputeShaderPool.ReturnBuffer(stencilBuffer);
                    //stencilBuffer.Release();
                    //stencilBuffer.Dispose();
                }
                //Resources.UnloadAsset(blendShader);
                UseCount++;
            }
            else
            {
                for (int dx = 0; dx < width; dx++)
                {
                    for (int dz = 0; dz < height; dz++)
                    {
                        var blendingVal = blendingData[dx, dz];

                        switch (blendMode)
                        {
                            case MMTerrainLayer.EMMTerrainLayerBlendMode.Set:
                                baseData[dx, dz] = blendingVal;
                                break;
                            case MMTerrainLayer.EMMTerrainLayerBlendMode.Additive:
                                baseData[dx, dz] += blendingVal;
                                break;
                            case MMTerrainLayer.EMMTerrainLayerBlendMode.Stencil:
                                var xF = (offset.x + dx) / (float)size.x;
                                var zF = (offset.z + dz) / (float)size.z;
                                float strength;
                                stencil.StencilBilinearSample(new Vector2(xF, zF), out strength);
                                if (strength > 0)
                                {
                                    var existingValue = baseData[dx, dz];
                                    var newValue = (byte)Mathf.RoundToInt(Mathf.Clamp(Mathf.Lerp(existingValue, blendingVal, strength), 0, 255));
                                    baseData[dx, dz] = newValue;
                                }
                                break;
                        }
                    }
                }
            }
            GC.Collect(3, GCCollectionMode.Forced);
        }
        
        public static void StencilEraseArray(
            ref Serializable2DByteArray baseData, 
            Serializable2DFloatArray stencil,
            Common.Coord min, Common.Coord max, Common.Coord size,
            bool invert, bool absolute)
        {
            if (baseData == null || stencil == null)
            {
                return;
            }

            var width = baseData.Width;
            var height = baseData.Height;
            if (TerrainWrapper.ComputeShaders && ShouldCompute(baseData))
            {
                var blendShader = ComputeShaderPool.GetShader("MadMaps/WorldStamp/ComputeShaders/StencilIntArray");
                int kernelHandle = blendShader.FindKernel("StencilInts");

                var dataAsInt = baseData.Data.ConvertToIntArray();
                
                //var baseBuffer = new ComputeBuffer(baseData.Width * baseData.Height, sizeof(float));
                var baseBuffer = ComputeShaderPool.GetBuffer(baseData.Width * baseData.Height, sizeof(float));
                baseBuffer.SetData(dataAsInt);
                blendShader.SetBuffer(kernelHandle, "_Base", baseBuffer);
                blendShader.SetVector("_BaseSize", new Vector4(baseData.Width, baseData.Height));
                blendShader.SetVector("_MinMax", new Vector4(min.x, min.z, max.x, max.z));
                blendShader.SetVector("_TotalSize", new Vector4(size.x, size.z));

                //ComputeBuffer stencilBuffer= new ComputeBuffer(stencil.Width * stencil.Height, sizeof(float));
                var stencilBuffer = ComputeShaderPool.GetBuffer(stencil.Width * stencil.Height, sizeof(float));
                stencilBuffer.SetData(stencil.Data);
                blendShader.SetBuffer(kernelHandle, "_Stencil", stencilBuffer);
                blendShader.SetVector("_StencilSize", new Vector4(stencil.Width, stencil.Height));
                blendShader.SetBool("_Invert", invert);
                blendShader.SetBool("_Absolute", absolute);
                
                blendShader.Dispatch(kernelHandle, baseData.Width, baseData.Height, 1);

                baseBuffer.GetData(dataAsInt);

                for (int i = 0; i < dataAsInt.Length; i++)
                {
                    var val = dataAsInt[i];
                    baseData.Data[i] = (byte)Mathf.Clamp(val, 0, 255);
                }

                /*baseBuffer.Release();
                baseBuffer.Dispose();
                stencilBuffer.Release();
                stencilBuffer.Dispose();
                Resources.UnloadAsset(blendShader);*/

                ComputeShaderPool.ReturnBuffer(baseBuffer);
                ComputeShaderPool.ReturnBuffer(stencilBuffer);
                UseCount++;
            }
            else
            {
                for (var u = 0; u < width; ++u)
                {
                    var uF = (u + min.x) / (float)size.x;
                    for (var v = 0; v < height; ++v)
                    {
                        var vF = (v + min.z) / (float)size.z;

                        float value;
                        stencil.StencilBilinearSample(new Vector2(uF, vF), out value);
                        var existingValue = baseData[u, v];
                        var newValue = existingValue;
                        if (absolute)
                        {
                            newValue = (byte)(value > 0 ? existingValue : 0);
                        }
                        else
                        {
                            newValue = (byte)Mathf.Clamp(existingValue * (1 - value), 0, 255);
                            if (invert)
                            {
                                newValue = (byte)Mathf.Clamp(existingValue * value, 0, 255);
                            }
                        }
                        baseData[u, v] = newValue;
                    }
                }
            }
            GC.Collect(3, GCCollectionMode.Forced);
        }

        public static void BlendArray(ref Serializable2DFloatArray baseData,
            Serializable2DFloatArray blendingData,
            Serializable2DFloatArray stencil,
            MMTerrainLayer.EMMTerrainLayerBlendMode blendMode,
            Common.Coord offset, Common.Coord max, Common.Coord originalSize)
        {
            if (blendingData == null)
            {
                return;
            }
            if (baseData == null)
            {
                baseData = blendingData;
                return;
            }

            var width = baseData.Width;
            var height = baseData.Height;
            if (TerrainWrapper.ComputeShaders && ShouldCompute(baseData))
            {
                var blendShader = ComputeShaderPool.GetShader("MadMaps/WorldStamp/ComputeShaders/BlendFloatArray");
                int kernelHandle = blendShader.FindKernel("BlendFloats");

                //var baseBuffer = new ComputeBuffer(baseData.Width * baseData.Height, sizeof(float));
                var baseBuffer = ComputeShaderPool.GetBuffer(baseData.Width * baseData.Height, sizeof(float));
                baseBuffer.SetData(baseData.Data);
                blendShader.SetBuffer(kernelHandle, "_Base", baseBuffer);
                blendShader.SetVector("_BaseSize", new Vector4(baseData.Width, baseData.Height));

                //var blendBuffer = new ComputeBuffer(blendingData.Width * blendingData.Height, sizeof(float));
                var blendBuffer = ComputeShaderPool.GetBuffer(blendingData.Width * blendingData.Height, sizeof(float));
                blendBuffer.SetData(blendingData.Data);
                blendShader.SetBuffer(kernelHandle, "_Blend", blendBuffer);
                blendShader.SetVector("_BlendSize", new Vector4(blendingData.Width, blendingData.Height));

                ComputeBuffer stencilBuffer = null;
                if (stencil != null && stencil.Width > 0 && stencil.Height > 0)
                {
                    //stencilBuffer = new ComputeBuffer(stencil.Width * stencil.Height, sizeof(float));
                    stencilBuffer = ComputeShaderPool.GetBuffer(stencil.Width * stencil.Height, sizeof(float));
                    stencilBuffer.SetData(stencil.Data);
                    blendShader.SetBuffer(kernelHandle, "_Stencil", stencilBuffer);
                    blendShader.SetVector("_StencilSize", new Vector4(stencil.Width, stencil.Height));
                }
                else
                {
                    stencilBuffer = ComputeShaderPool.GetBuffer(1, sizeof(float));
                    blendShader.SetBuffer(kernelHandle, "_Stencil", stencilBuffer);
                    blendShader.SetVector("_StencilSize", new Vector4(0, 0));
                }

                blendShader.SetInt("_BlendMode", (int)blendMode);
                blendShader.SetVector("_MinMax", new Vector4(offset.x, offset.z, max.x, max.z));
                blendShader.SetVector("_TotalSize", new Vector4(originalSize.x, originalSize.z, 0, 0));

                blendShader.Dispatch(kernelHandle, baseData.Width, baseData.Height, 1);

                baseBuffer.GetData(baseData.Data);

                /*baseBuffer.Release();
                baseBuffer.Dispose();
                blendBuffer.Release();
                blendBuffer.Dispose();*/
                ComputeShaderPool.ReturnBuffer(baseBuffer);
                ComputeShaderPool.ReturnBuffer(blendBuffer);
                if (stencilBuffer != null)
                {
                    /*stencilBuffer.Release();
                    stencilBuffer.Dispose();*/
                    ComputeShaderPool.ReturnBuffer(stencilBuffer);
                }
                //Resources.UnloadAsset(blendShader);
                UseCount++;
            }
            else
            {
                for (int dx = 0; dx < width; dx++)
                {
                    for (int dz = 0; dz < height; dz++)
                    {
                        var blendingVal = blendingData[dx, dz];
                        var baseValue = baseData[dx, dz];
                        switch (blendMode)
                        {
                            case MMTerrainLayer.EMMTerrainLayerBlendMode.Set:
                                baseData[dx, dz] = blendingVal;
                                break;
                            case MMTerrainLayer.EMMTerrainLayerBlendMode.Additive:
                                baseData[dx, dz] += blendingVal;
                                break;
                            case MMTerrainLayer.EMMTerrainLayerBlendMode.Stencil:
                                var xF = (dx + offset.x) / (float)originalSize.x;
                                var zF = (dz + offset.z) / (float)originalSize.z;
                                float strength;
                                stencil.StencilBilinearSample(new Vector2(xF, zF), out strength);
                                //strength  = 1;
                                if (strength > 0)
                                {
                                    baseData[dx, dz] = Mathf.Lerp(baseValue, blendingVal, strength);
                                }
                                break;
                        }
                    }
                }
            }
            GC.Collect(3, GCCollectionMode.Forced);
        }

        public static bool ShouldCompute<T>(Serializable2DArray<T> array) where T:struct
        {
            if(SystemInfo.graphicsShaderLevel < 45)
            {
                return false;
            }
            return array.Width * array.Height >= 8 * 8;
        }

        public static bool ShouldCompute()
        {
            if(SystemInfo.graphicsShaderLevel < 45)
            {
                return false;
            }
            return true;
        }
    }
}