using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using MadMaps.Common;
using MadMaps.Common.Collections;
using MadMaps.Common.GenericEditor;
using MadMaps.Common.Serialization;
using MadMaps.Terrains.Lookups;
using MadMaps.WorldStamps;
using UnityEngine;
using System.Linq;

namespace MadMaps.Terrains
{
    [Name("Terrain Layer")]
    public partial class MMTerrainLayer : LayerBase
    {
        public enum EMMTerrainLayerBlendMode
        {
            Additive = 0,
            Set = 1,
            Stencil = 2,
        }

        public EMMTerrainLayerBlendMode BlendMode = EMMTerrainLayerBlendMode.Set;
        public Serializable2DFloatArray Heights;
        public List<string> ObjectRemovals = new List<string>();
        public List<PrefabObjectData> Objects = new List<PrefabObjectData>();
        public CompressedSplatDataLookup SplatData = new CompressedSplatDataLookup();
        public CompressedDetailDataLookup DetailData = new CompressedDetailDataLookup();
        public List<string> TreeRemovals = new List<string>();
        public List<MadMapsTreeInstance> Trees = new List<MadMapsTreeInstance>();     

        /// <summary>
        /// Go and capture all of the data on a given terrain and store it in this layer.
        /// </summary>
        /// <param name="terrain">The terrain to capture</param>
        public virtual void SnapshotTerrain(Terrain terrain)
        {
            Debug.Log("Snapshotted Terrain " + terrain.name, terrain);
            try
            {
                this.SnapshotHeights(terrain);
            }
            catch(Exception e)
            {
                Debug.LogException(e, terrain);
            }
            try
            {
                this.SnapshotSplats(terrain);
            }
            catch(Exception e)
            {
                Debug.LogException(e, terrain);
            }
            try
            {
                this.SnapshotDetails(terrain);
            }
            catch(Exception e)
            {
                Debug.LogException(e, terrain);
            }
            try
            {
                this.SnapshotTrees(terrain);
            }
            catch(Exception e)
            {
                Debug.LogException(e, terrain);
            }
            try
            {
                this.SnapshotObjects(terrain);
            }
            catch(Exception e)
            {
                Debug.LogException(e, terrain);
            }
            #if VEGETATION_STUDIO
            try
            {
                this.SnapshotVegetationStudioData(terrain);
            }
            catch(Exception e)
            {
                Debug.LogException(e, terrain);
            }
            #endif
        }

        /// <summary>
        /// Write all data to a wrapper's compound data
        /// </summary>
        /// <param name="wrapper"></param>
        public override void WriteToTerrain(TerrainWrapper wrapper)
        {
            var terrain = wrapper.Terrain;
            var bounds = new Bounds(terrain.GetPosition() + terrain.terrainData.size/2, terrain.terrainData.size);
            // TerrainCollider bounds is unreliable as it can be slow to update
            WriteToTerrain(wrapper, bounds);
        }

        public override void PrepareApply(TerrainWrapper terrainWrapper, int index)
        {
            if (index == terrainWrapper.Layers.Count - 1)
            {
                BlendMode = MMTerrainLayer.EMMTerrainLayerBlendMode.Set;
            }
            else
            {
                BlendMode = MMTerrainLayer.EMMTerrainLayerBlendMode.Stencil;
            }

            int bestRes = 0;
            if(Heights != null && Heights.Width > 0 && Heights.Height > 0 && terrainWrapper.Terrain.terrainData.heightmapResolution != Heights.Width)
            {
                if(index == terrainWrapper.Layers.Count - 1)
                {
                    terrainWrapper.Terrain.terrainData.heightmapResolution = Heights.Width;
                }
                bestRes = Heights.Width;
            }
            if(DetailData.Count > 0)
            {
                var firstMap = DetailData.First();
                var detailRes = firstMap.Value.Width;
                if(index == terrainWrapper.Layers.Count - 1)
                {
                    if(detailRes != terrainWrapper.Terrain.terrainData.detailResolution)
                    {
                        terrainWrapper.Terrain.terrainData.SetDetailResolution (detailRes, 
                            terrainWrapper.Terrain.terrainData.GetDetailResolutionPerPatch());
                    }
                }
                if(bestRes == 0)
                {
                    bestRes = detailRes + 1;
                }
            }
            if(SplatData.Count > 0)
            {
                var splatRes = SplatData.First().Value.Width;
                if(index == terrainWrapper.Layers.Count - 1)
                {
                    if(terrainWrapper.Terrain.terrainData.alphamapResolution != splatRes)
                    {
                        terrainWrapper.Terrain.terrainData.alphamapResolution = splatRes;
                    }
                }
                if(bestRes == 0)
                {
                    bestRes = splatRes + 1;
                }
            }
                        
            if(bestRes > 0 && (Stencil == null || Stencil.Width != bestRes || Stencil.Height != bestRes))
            {
                Stencil = new Stencil(bestRes, bestRes);
            }
        }

        public void WriteToTerrain(TerrainWrapper wrapper, Bounds bounds)
        {
            if (wrapper.WriteHeights)
            {
                MiscUtilities.ProgressBar("Writing Heights for layer " + name, "", 0);
                WriteHeightsToTerrain(wrapper, bounds);
            }

            if (wrapper.WriteSplats)
            {
                MiscUtilities.ProgressBar("Writing Splats for layer " + name, "", 0);
                WriteSplatsToTerrain(wrapper, bounds);
            }

            if (wrapper.WriteDetails)
            {
                MiscUtilities.ProgressBar("Writing Details for layer " + name, "", 0);
                WriteDetailsToTerrain(wrapper, bounds);
            }

            if (wrapper.WriteTrees)
            {
                MiscUtilities.ProgressBar("Writing Trees for layer " + name, "", 0);
                WriteTreesToTerrain(wrapper, bounds);
            }

            if (wrapper.WriteObjects)
            {
                MiscUtilities.ProgressBar("Writing Objects for layer " + name, "", 0);
                WriteObjectsToTerrain(wrapper, bounds);
            }

            #if VEGETATION_STUDIO

            if (wrapper.WriteVegetationStudio)
            {
                MiscUtilities.ProgressBar("Writing Vegetation Studio for layer " + name, "", 0);
                WriteVegetationStudioToTerrain(wrapper, bounds);
            }

            #endif

            GC.Collect();
        }

        private void WriteObjectsToTerrain(TerrainWrapper wrapper, Bounds bounds)
        {
            bounds.Expand(Vector3.up*5000);
            var tPos = wrapper.transform.position;
            var tSize = wrapper.Terrain.terrainData.size;
            for (var i = 0; i < Objects.Count; i++)
            {
                var prefabObjectData = Objects[i];
                if (prefabObjectData.Prefab == null)
                {
                    continue;
                }

                var worldPos = tPos +
                               new Vector3(prefabObjectData.Position.x*tSize.x, 0, prefabObjectData.Position.z*tSize.z);
                worldPos.y = wrapper.transform.position.y + prefabObjectData.Position.y;
                if (!bounds.Contains(worldPos))
                {
                    DebugHelper.DrawPoint(worldPos, 20, Color.red, 20);
                    continue;
                }
                if (wrapper.CompoundTerrainData.Objects.ContainsKey(prefabObjectData.Guid))
                {
                    Debug.LogWarning("Duplicate object entry found: " + prefabObjectData.Guid);
                    continue;
                }

                wrapper.CompoundTerrainData.Objects.Add(prefabObjectData.Guid,
                    new InstantiatedObjectData(prefabObjectData, this, null));
            }

            for (var i = 0; i < ObjectRemovals.Count; i++)
            {
                wrapper.CompoundTerrainData.Objects.Remove(ObjectRemovals[i]);
            }
        }

        private void WriteHeightsToTerrain(TerrainWrapper wrapper, Bounds bounds)
        {
            var heightmapRes = wrapper.Terrain.terrainData.heightmapResolution;
            if (Heights == null || Heights.Width != heightmapRes || Heights.Height != heightmapRes)
            {
                if (Heights != null && Heights.Width > 0 && Heights.Height > 0)
                {
                    Debug.LogWarning(
                        string.Format(
                            "Failed to write heights for layer '{0}' as it was the wrong resolution. Expected {1}x{1}, got {2}x{2}",
                            name, heightmapRes, Heights.Width), wrapper);
                }
                return;
            }

            var terrain = wrapper.Terrain;
            var existingHeights = wrapper.CompoundTerrainData.Heights;
            if (existingHeights == null || existingHeights.Width != heightmapRes ||
                existingHeights.Height != heightmapRes)
            {
                existingHeights = new Serializable2DFloatArray(heightmapRes, heightmapRes);
                wrapper.CompoundTerrainData.Heights = existingHeights;
            }
            var min = terrain.WorldToHeightmapCoord(bounds.min, TerrainX.RoundType.Floor);
            var max = terrain.WorldToHeightmapCoord(bounds.max, TerrainX.RoundType.Floor);
            min = new Common.Coord(Mathf.Clamp(min.x, 0, heightmapRes), Mathf.Clamp(min.z, 0, heightmapRes));
            max = new Common.Coord(Mathf.Clamp(max.x, 0, heightmapRes), Mathf.Clamp(max.z, 0, heightmapRes));
            
            BlendMMTerrainLayerUtility.BlendArray(ref existingHeights, Heights, IsValidStencil(Stencil) ? Stencil : null,
                BlendMode, min, max, new Common.Coord(heightmapRes, heightmapRes));
        }

        private void WriteSplatsToTerrain(TerrainWrapper wrapper, Bounds bounds)
        {
            if (SplatData == null || SplatData.Count == 0)
            {
                return;
            }
            
            if (BlendMode == EMMTerrainLayerBlendMode.Set)
            {
                wrapper.CompoundTerrainData.SplatData.Clear();
            }

            var terrain = wrapper.Terrain;
            var splatRes = terrain.terrainData.alphamapResolution;
            var stencilSize = new Common.Coord(Stencil.Width, Stencil.Height);
            if (BlendMode == EMMTerrainLayerBlendMode.Stencil)
            {
                // Remove Stencil Values
                foreach (var pair in wrapper.CompoundTerrainData.SplatData)
                {
                    var data = pair.Value;
                    BlendMMTerrainLayerUtility.StencilEraseArray(ref data, Stencil, Common.Coord.Zero, new Common.Coord(splatRes, splatRes),
                        stencilSize, false, false);
                }
            }
            
            foreach (var keyValuePair in SplatData)
            {
                var splatPrototypeWrapper = keyValuePair.Key;
                var readData = keyValuePair.Value;

                if (readData == null || readData.Width != splatRes || readData.Height != splatRes)
                {
                    Debug.LogWarning(
                        string.Format(
                            "Failed to write splat layer {0} for layer '{3}' as it was the wrong resolution. Expected {1}x{1}, got {2}x{2}",
                            splatPrototypeWrapper.name, splatRes, readData.Width, name), wrapper);
                    continue;
                }

                Serializable2DByteArray data;
                if (!wrapper.CompoundTerrainData.SplatData.TryGetValue(splatPrototypeWrapper, out data)
                    || data.Width != splatRes || data.Height != splatRes)
                {
                    data = new Serializable2DByteArray(splatRes, splatRes);
                }

                BlendMMTerrainLayerUtility.BlendArray(ref data, readData,
                        IsValidStencil(Stencil) ? Stencil : null,
                        BlendMode, Common.Coord.Zero, stencilSize);

                wrapper.CompoundTerrainData.SplatData[splatPrototypeWrapper] = data;
            }
            GC.Collect();
        }

        public static bool IsValidStencil(Serializable2DFloatArray stencil)
        {
            return stencil != null && stencil.Width > 0 && stencil.Height > 0;
        }

        private void WriteDetailsToTerrain(TerrainWrapper wrapper, Bounds bounds)
        {
            if (DetailData == null || DetailData.Count == 0)
            {
                return;
            }

            var terrain = wrapper.Terrain;
            var detailResolution = terrain.terrainData.detailResolution;

            var detailCoordMin = terrain.WorldToDetailCoord(bounds.min);
            var detailCoordMax = terrain.WorldToDetailCoord(bounds.max);

            detailCoordMin = new Common.Coord(Mathf.Clamp(detailCoordMin.x, 0, detailResolution - 1),
                Mathf.Clamp(detailCoordMin.z, 0, detailResolution - 1));
            detailCoordMax = new Common.Coord(Mathf.Clamp(detailCoordMax.x, 0, detailResolution - 1),
                Mathf.Clamp(detailCoordMax.z, 0, detailResolution - 1));

            if (BlendMode == EMMTerrainLayerBlendMode.Set)
            {
                wrapper.CompoundTerrainData.DetailData.Clear();
            }

            foreach (var keyValuePair in DetailData)
            {
                var detailPrototypeWrapper = keyValuePair.Key;
                var readData = keyValuePair.Value;

                if (readData == null || readData.Width != detailResolution || readData.Height != detailResolution)
                {
                    Debug.LogWarning(
                        string.Format(
                            "Failed to write detail layer {0} for layer '{3}' as it was the wrong resolution. Expected {1}x{1}, got {2}x{2}",
                            detailPrototypeWrapper.name, detailResolution, readData.Width, name), wrapper);
                    continue;
                }

                Serializable2DByteArray data;
                if (!wrapper.CompoundTerrainData.DetailData.TryGetValue(detailPrototypeWrapper, out data)
                    || data.Width != detailResolution || data.Height != detailResolution)
                {
                    data = new Serializable2DByteArray(detailResolution, detailResolution);
                }

                for (var u = detailCoordMin.x; u < detailCoordMax.x; ++u)
                {
                    var uF = u / (float)detailResolution;
                    //var arrayU = u - detailCoordMin.x;
                    for (var v = detailCoordMin.z; v < detailCoordMax.z; ++v)
                    {
                        var vF = v / (float)detailResolution;
                        //var arrayV = v - detailCoordMin.x;

                        if (BlendMode == EMMTerrainLayerBlendMode.Set)
                        {
                            data[u, v] = readData[u, v];
                        }
                        else if (BlendMode == EMMTerrainLayerBlendMode.Additive)
                        {
                            data[u, v] += readData[u, v];
                        }
                        else if (BlendMode == EMMTerrainLayerBlendMode.Stencil)
                        {
                            var stencil = this.GetStencilStrength(new Vector2(uF, vF));
                            if (stencil > 0)
                            {
                                var dataVal = data[u, v];
                                var readVal = readData[u, v];
                                var newVal = (byte)
                                    Mathf.RoundToInt(Mathf.Clamp(Mathf.Lerp(dataVal, readVal, stencil), 0, 255));
                                data[u, v] = newVal;
                            }
                        }
                    }
                }

                /*BlendArrayUtility.BlendArray(ref data, readData,
                        Stencil != null ? Stencil.Select(0, 0, detailResolution, detailResolution) : null,
                        BlendMode, TerrainCoord.Zero, new TerrainCoord(detailResolution - 1, detailResolution - 1));*/
                wrapper.CompoundTerrainData.DetailData[detailPrototypeWrapper] = data;
            }
            GC.Collect();
        }

        private void WriteTreesToTerrain(TerrainWrapper wrapper, Bounds bounds)
        {
            var existingTrees = wrapper.CompoundTerrainData.Trees;
            if (BlendMode == EMMTerrainLayerBlendMode.Set)
            {
                existingTrees.Clear();
            }
            for (var i = 0; i < Trees.Count; i++)
            {
                if (Trees[i].Prototype == null)
                {
                    continue;
                }
                existingTrees.Add(Trees[i].Guid, Trees[i]);
            }
            foreach (var treeRemoval in TreeRemovals)
            {
                existingTrees.Remove(treeRemoval);
            }
        }

        public override Serializable2DFloatArray GetHeights(int x, int z, int xSize, int zSize, int hRes)
        {
            if (Heights == null || Heights.Width != hRes || Heights.Height != hRes)
            {
                return null;
            }
            var h = new Serializable2DFloatArray(xSize, zSize);
            for (var u = x; u < x + xSize; ++u)
            {
                for (var v = z; v < z + zSize; ++v)
                {
                    var hx = u - x;
                    var hz = v - z;
                    try
                    {
                        var baseHeight = Heights[u, v];
                        h[hx, hz] = baseHeight;
                    }
                    catch (IndexOutOfRangeException e)
                    {
                        Debug.LogError(string.Format("x {0} y {1}", hx, hz));
                        throw e;
                    }
                }
            }
            return h;
        }

        public void SetHeights(int x, int z, float[,] heights, int heightRes, Serializable2DFloatArray stencil = null)
        {
            if (heights == null)
            {
                return;
            }
            if (Heights == null || Heights.Width != heightRes || Heights.Height != heightRes)
            {
                Heights = new Serializable2DFloatArray(heightRes, heightRes);
            }
            var width = heights.GetLength(0);
            var height = heights.GetLength(1);
            for (var u = x; u < x + width; ++u)
            {
                for (var v = z; v < z + height; ++v)
                {
                    var hx = u - x;
                    var hz = v - z;
                    try
                    {
                        var heightsSample = heights[hx, hz];                        
                        if (stencil == null)
                        {
                            Heights[u, v] = heightsSample;
                        }
                        else
                        {
                            float val;
                            int key;
                            MiscUtilities.DecompressStencil(stencil[u, v], out key, out val);
                            Heights[u, v] = Mathf.Lerp(Heights[u, v], heightsSample, val);
                        }
                    }
                    catch (IndexOutOfRangeException e)
                    {
                        Debug.LogError(string.Format("x {0} y {1}", hx, hz));
                        throw e;
                    }
                }
            }

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        public void SetHeights(int x, int z, Serializable2DFloatArray heights, int heightRes, Serializable2DFloatArray stencil = null)
        {
            if (heights == null)
            {
                return;
            }
            if (Heights == null || Heights.Width != heightRes || Heights.Height != heightRes)
            {
                Heights = new Serializable2DFloatArray(heightRes, heightRes);
            }
            var width = heights.Width;
            var height = heights.Height;
            for (var u = x; u < x + width; ++u)
            {
                for (var v = z; v < z + height; ++v)
                {
                    var hx = u - x;
                    var hz = v - z;
                    try
                    {
                        var heightsSample = heights[hx, hz];
                        heightsSample = Mathf.Clamp01(heightsSample);
                        if (stencil == null)
                        {
                            Heights[u, v] = heightsSample;
                        }
                        else
                        {
                            float val;
                            int key;
                            MiscUtilities.DecompressStencil(stencil[u, v], out key, out val);
                            Heights[u, v] = Mathf.Lerp(Heights[u, v], heightsSample, val);
                        }
                    }
                    catch (IndexOutOfRangeException e)
                    {
                        Debug.LogError(string.Format("x {0} y {1}", hx, hz));
                        throw e;
                    }
                }
            }

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        public override void Clear(TerrainWrapper wrapper)
        {
            Dispose(wrapper, false);

            Trees.Clear();
            TreeRemovals.Clear();

            if (Heights != null)
            {
                var tRes = wrapper.Terrain.terrainData.heightmapResolution;
                if (Heights.Width == tRes && Heights.Height == tRes)
                {
                    Heights.Clear();
                }
                else
                {
                    Heights = new Serializable2DFloatArray(tRes, tRes);
                }
            }
            SplatData.Clear();

            Objects.Clear();
            ObjectRemovals.Clear();
            if (DetailData != null)
            {
                DetailData.Clear();
            }


            if (Stencil != null)
            {
                var tRes = wrapper.Terrain.terrainData.heightmapResolution;
                if (Stencil.Width == tRes && Stencil.Height == tRes)
                {
                    Stencil.Clear();
                }
                else
                {
                    Stencil = new Stencil(tRes, tRes);
                }
            }

#if VEGETATION_STUDIO
            VSInstances.Clear();
            VSRemovals.Clear();
#endif

            GC.Collect();
            
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        public void SetSplatmap(SplatPrototypeWrapper prototype, int x, int z, 
            Serializable2DByteArray splats, int splatRes, Serializable2DFloatArray stencil = null)
        {
            if (splats == null || prototype == null)
            {
                return;
            }
            if (SplatData == null)
            {
                SplatData = new CompressedSplatDataLookup();
            }

            Serializable2DByteArray existingSplats;
            if (!SplatData.TryGetValue(prototype, out existingSplats) || existingSplats.Width != splatRes ||
                existingSplats.Height != splatRes)
            {
                existingSplats = new Serializable2DByteArray(splatRes, splatRes);
                SplatData[prototype] = existingSplats;
            }

            var width = splats.Width;
            var height = splats.Height;
            for (var u = x; u < x + width; ++u)
            {
                if (u < 0 || u >= splatRes)
                {
                    continue;
                }
                for (var v = z; v < z + height; ++v)
                {
                    if (v < 0 || v >= splatRes)
                    {
                        continue;
                    }

                    var hx = u - x;
                    var hz = v - z;

                    try
                    {
                        var splatSample = splats[hx, hz];

                        if (stencil != null)
                        {
                            var stencilVal = stencil[hx, hz];
                            splatSample = (byte)Mathf.Clamp(Mathf.Lerp(existingSplats[u, v], splatSample, stencilVal), 0, 255);
                        }

                        existingSplats[u, v] = (byte)Mathf.Clamp(splatSample, 0, 255);
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }
            }


#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        public void SetSplatmap(SplatPrototypeWrapper prototype, int x, int z,
            float[,] splats, int splatRes, Serializable2DFloatArray stencil = null)
        {
            if (splats == null || prototype == null)
            {
                return;
            }
            if (SplatData == null)
            {
                SplatData = new CompressedSplatDataLookup();
            }

            Serializable2DByteArray existingSplats;
            if (!SplatData.TryGetValue(prototype, out existingSplats) || existingSplats.Width != splatRes ||
                existingSplats.Height != splatRes)
            {
                existingSplats = new Serializable2DByteArray(splatRes, splatRes);
                SplatData[prototype] = existingSplats;
            }

            var width = splats.GetLength(0);
            var height = splats.GetLength(1);
            for (var u = x; u < x + width; ++u)
            {
                if (u < 0 || u >= splatRes)
                {
                    continue;
                }
                for (var v = z; v < z + height; ++v)
                {
                    if (v < 0 || v >= splatRes)
                    {
                        continue;
                    }

                    var hx = u - x;
                    var hz = v - z;

                    try
                    {
                        var splatSample = splats[hx, hz] * 255f;

                        if (stencil != null)
                        {
                            var stencilVal = stencil[hx, hz];
                            splatSample = (byte)Mathf.Clamp(Mathf.Lerp(existingSplats[u, v], splatSample, stencilVal), 0, 255);
                        }

                        existingSplats[u, v] = (byte)Mathf.Clamp(splatSample, 0, 255);
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }
            }


#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }


        public override Serializable2DByteArray GetSplatmap(SplatPrototypeWrapper prototype, int x, int z, int width, int height, int splatResolution)
        {
            if (SplatData == null)
            {
                return null;
            }

            Serializable2DByteArray data;
            if (!SplatData.TryGetValue(prototype, out data))
            {
                return null;
            }

            if (data.Width != splatResolution || data.Height != splatResolution)
            {
                SplatData.Remove(prototype);
                return null;
            }

            var splats = new Serializable2DByteArray(width, height);
            for (var u = x; u < x + width; ++u)
            {
                for (var v = z; v < z + height; ++v)
                {
                    var hx = u - x;
                    var hz = v - z;
                    try
                    {
                        var splatSample = data[u, v];
                        splats[hx, hz] = splatSample;
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }
            }
            return splats;
        }

        public void SetDetailMap(DetailPrototypeWrapper prototype, int x, int z, int[,] details, int dRes, Serializable2DFloatArray stencil = null)
        {
            if (DetailData == null)
            {
                DetailData = new CompressedDetailDataLookup();
            }

            Serializable2DByteArray existingDetails;
            if (!DetailData.TryGetValue(prototype, out existingDetails) || existingDetails.Width != dRes ||
                existingDetails.Height != dRes)
            {
                existingDetails = new Serializable2DByteArray(dRes, dRes);
                DetailData[prototype] = existingDetails;
            }

            var width = details.GetLength(0);
            var height = details.GetLength(1);
            for (var u = x; u < x + width; ++u)
            {
                for (var v = z; v < z + height; ++v)
                {
                    var hx = u - x;
                    var hz = v - z;

                    try
                    {
                        var splatSample = details[hx, hz];
                        if (stencil != null)
                        {
                            existingDetails[u, v] = (byte)Mathf.Clamp(Mathf.Lerp(existingDetails[u, v], splatSample, stencil[hx, hz]), 0, 255);
                        }
                        else
                        {
                            existingDetails[u, v] = (byte)splatSample;
                        }
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }
            }


#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
        
        public void SetDetailMap(DetailPrototypeWrapper prototype, int x, int z, Serializable2DByteArray details, int dRes, Serializable2DFloatArray stencil = null)
        {
            if (DetailData == null)
            {
                DetailData = new CompressedDetailDataLookup();
            }

            Serializable2DByteArray existingDetails;
            if (!DetailData.TryGetValue(prototype, out existingDetails) || existingDetails.Width != dRes ||
                existingDetails.Height != dRes)
            {
                existingDetails = new Serializable2DByteArray(dRes, dRes);
                DetailData[prototype] = existingDetails;
            }

            var width = details.Width;
            var height = details.Height;
            for (var u = x; u < x + width; ++u)
            {
                for (var v = z; v < z + height; ++v)
                {
                    var hx = u - x;
                    var hz = v - z;

                    try
                    {
                        var splatSample = details[hx, hz];
                        if (stencil != null)
                        {
                            existingDetails[u, v] = (byte)Mathf.Clamp(Mathf.Lerp(existingDetails[u, v], splatSample, stencil[hx, hz]), 0, 255);
                        }
                        else
                        {
                            existingDetails[u, v] = (byte)splatSample;
                        }
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }
            }


#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        public override Serializable2DByteArray GetDetailMap(DetailPrototypeWrapper detailWrapper, int x, int z, int width, int height, int detailResolution)
        {
            if (DetailData == null)
            {
                return null;
            }

            Serializable2DByteArray data;
            if (!DetailData.TryGetValue(detailWrapper, out data))
            {
                return null;
            }

            if (data.Width != detailResolution || data.Height != detailResolution)
            {
                DetailData.Remove(detailWrapper);
                return null;
            }

            var detailMap = new Serializable2DByteArray(width, height);
            for (var u = x; u < x + width; ++u)
            {
                for (var v = z; v < z + height; ++v)
                {
                    var hx = u - x;
                    var hz = v - z;
                    try
                    {
                        var detail = data[u, v];
                        detailMap[hx, hz] = detail;
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }
            }
            return detailMap;
        }
        
        public void SetTrees(List<TreeInstance> trees, List<TreePrototype> prototypeList)
        {
            Trees.Clear();
            foreach (var tree in trees)
            {
                var newTree = new MadMapsTreeInstance(tree, prototypeList);
                Trees.Add(newTree);
            }

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        public override void Dispose(TerrainWrapper terrainWrapper, bool destroyObjects)
        {
            foreach (var prefabObjectData in Objects)
            {
                InstantiatedObjectData data;
                if (!terrainWrapper.CompoundTerrainData.Objects.TryGetValue(prefabObjectData.Guid, out data) || data == null)
                {
                    continue;
                }
                if (!string.IsNullOrEmpty(prefabObjectData.Guid))
                {
                    terrainWrapper.CompoundTerrainData.Objects.Remove(prefabObjectData.Guid);
                }
                if (destroyObjects && data.InstantiatedObject != null)
                {
                    DestroyImmediate(data.InstantiatedObject);
                }
            }
            for (var i = 0; i < Trees.Count; i++)
            {
                var guid = Trees[i].Guid;
                terrainWrapper.CompoundTerrainData.Trees.Remove(guid);
                foreach (var layer in terrainWrapper.Layers)
                {
                    var MMTerrainLayer = layer as MMTerrainLayer;
                    if (MMTerrainLayer != null)
                    {
                        MMTerrainLayer.TreeRemovals.Remove(guid);
                    }
                }
            }
        }

        public override float SampleHeight(TerrainWrapper wrapper, Vector3 worldPos)
        {
            if (Heights == null || Heights.Width == 0 || Heights.Height == 0)
            {
                return 0;
            }

            var tSize = wrapper.Terrain.terrainData.size;
            var normalizedPos = worldPos - wrapper.transform.position;
            var step = wrapper.Terrain.terrainData.size.x / wrapper.Terrain.terrainData.heightmapResolution;
            normalizedPos = new Vector3(normalizedPos.x / (tSize.x + step), normalizedPos.y / tSize.y, normalizedPos.z / (tSize.z + step));
            if (normalizedPos.x < 0 || normalizedPos.z < 0 || normalizedPos.x > 1 || normalizedPos.z > 1)
            {
                return 0;
            }

            var h = Heights.BilinearSample(new Vector2(normalizedPos.x, normalizedPos.z));
            //var tHeight = wrapper.Terrain.terrainData.size.y;
            //return h * tHeight;
            return h;
        }

        public override float SampleHeightNormalized(Vector2 normalizedPos)
        {
            if (Heights == null)
            {
                return 0;
            }

            if (normalizedPos.x < 0 || normalizedPos.y < 0 || normalizedPos.x > 1 || normalizedPos.y > 1)
            {
                return 0;
            }

            return Heights.BilinearSample(new Vector2(normalizedPos.y, normalizedPos.x));
        }

        public override List<string> GetTreeRemovals()
        {
            return TreeRemovals;
        }

        public override List<MadMapsTreeInstance> GetTrees()
        {
            return Trees;
        }

        public override List<SplatPrototypeWrapper> GetSplatPrototypeWrappers()
        {
            return SplatData.GetKeys();
        }

        public override List<DetailPrototypeWrapper> GetDetailPrototypeWrappers()
        {
            return DetailData.GetKeys();
        }

        public override float BlendHeight(float heightSum, Vector3 worldPos, TerrainWrapper wrapper)
        {
            if (BlendMode == EMMTerrainLayerBlendMode.Set)
            {
                heightSum = SampleHeight(wrapper, worldPos);
            }
            else if (BlendMode == EMMTerrainLayerBlendMode.Additive)
            {
                heightSum += SampleHeight(wrapper, worldPos);
            }
            else if (BlendMode == EMMTerrainLayerBlendMode.Stencil)
            {
                var tSize = wrapper.Terrain.terrainData.size;
                var normalizedPos = worldPos - wrapper.transform.position;
                var step = tSize.x / wrapper.Terrain.terrainData.heightmapResolution;
                normalizedPos = new Vector3(normalizedPos.x / (tSize.x + step), 0, normalizedPos.z / (tSize.z + step));
                var stencil = this.GetStencilStrength(normalizedPos.xz());
                heightSum = Mathf.Lerp(heightSum, SampleHeight(wrapper, worldPos), stencil);
            }
            return heightSum;
        }

        public override Serializable2DFloatArray BlendHeights(int x, int z, int width, int height, int heightRes, Serializable2DFloatArray result)
        {
            var layerHeights = GetHeights(x, z, width, height, heightRes);
            var stencil = IsValidStencil(Stencil) ? Stencil : null;
            BlendMMTerrainLayerUtility.BlendArray(ref result,
                layerHeights,
                stencil,
                BlendMode,
                new Common.Coord(x, z),
                new Common.Coord(x + width, z + height),
                new Common.Coord(heightRes, heightRes));
            return result;
        }

        public override float GetStencilStrength(Vector2 vector2, bool ignoreNegativeKeys = true)
        {
            /*if (BlendMode != EMMTerrainLayerBlendMode.Stencil || Stencil == null)
            {
                return 1;
            }*/
            float strength;
            Stencil.StencilBilinearSample(vector2, out strength, ignoreNegativeKeys);
            return strength;
        }

        public override float GetStencilStrength(Vector2 vector2, int stencilKey)
        {
            /*if (BlendMode != EMMTerrainLayerBlendMode.Stencil || Stencil == null)
            {
                return 1;
            }*/
            float strength;
            Stencil.StencilBilinearSample(vector2, stencilKey, out strength, stencilKey > 0);
            return strength;
        }

        public override Serializable2DByteArray BlendSplats(SplatPrototypeWrapper splat, int x, int z, int width, int height, int aRes,
            Serializable2DByteArray result)
        {
            var layerSplats = GetSplatmap(splat, x, z, width, height, aRes);
            if (layerSplats == null)
            {
                return result;
            }

            var stencil = BlendMode == EMMTerrainLayerBlendMode.Stencil && IsValidStencil(Stencil) ? Stencil : null;
            BlendMMTerrainLayerUtility.BlendArray(ref result,
                layerSplats,
                stencil,
                BlendMode,
                new Common.Coord(x, z),
                new Common.Coord(aRes, aRes));
            return result;
        }

        public override Serializable2DByteArray BlendDetails(DetailPrototypeWrapper detailWrapper, int x, int z, int width, int height, int dRes,
            Serializable2DByteArray result)
        {
            var layerDetails = GetDetailMap(detailWrapper, x, z, width, height, dRes);
            if (layerDetails == null)
            {
                return result;
            }

            var stencil = IsValidStencil(Stencil)
                ? Stencil
                : null;

            BlendMMTerrainLayerUtility.BlendArray(ref result, layerDetails, stencil, BlendMode,
                Common.Coord.Zero, new Common.Coord(dRes, dRes));
            return result;
        }

        public override List<string> GetObjectRemovals()
        {
            return ObjectRemovals;
        }

        public override List<PrefabObjectData> GetObjects()
        {
            return Objects;
        }

        public override void ForceDirty()
        {
            base.ForceDirty();
            Heights.ForceDirty();
            foreach (var pair in SplatData)
            {
                pair.Value.ForceDirty();
            }
            foreach (var pair in DetailData)
            {
                pair.Value.ForceDirty();
            }
        }
    }
}