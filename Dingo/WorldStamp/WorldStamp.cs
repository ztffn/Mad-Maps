﻿using System;
using System.Collections.Generic;
using System.Linq;
using Dingo.Common;
using Dingo.Common.Collections;
using Dingo.Terrains;
using UnityEngine;
using UnityEngine.Profiling;

namespace Dingo.WorldStamp
{
    [ExecuteInEditMode]
    [StripComponentOnBuild()]
    public class WorldStamp : MonoBehaviour
    {
        public enum EHeightBlendMode
        {
            Set,
            Add,
            Max,
            Average,
            Min,
        }

        public enum ESplatBlendMode
        {
            Set,
            Add,
            Max,
            Average,
        }

        public enum EObjectRelativeMode
        {
            RelativeToTerrain,
            RelativeToStamp,
        }

        private class LayerStampMapping
        {
            public string LayerName;
            public int LayerIndex = -1;
            public List<WorldStamp> Stamps = new List<WorldStamp>();
        }

        public WorldStampData Data
        {
            get
            {
                if (_dataContainer == null)
                {
                    _dataContainer = transform.GetComponentInChildren<WorldStampDataContainer>();
                }
                return _dataContainer.GetData();
            }
            set
            {
                if (_dataContainer == null)
                {
                    _dataContainer = transform.GetComponentInChildren<WorldStampDataContainer>();
                    if (_dataContainer == null)
                    {
                        var go = new GameObject("Data Container");
                        go.transform.SetParent(transform);
                        go.transform.localPosition = Vector3.zero;
                        _dataContainer = go.AddComponent<WorldStampDataContainer>();
                    }
                }
                if (_dataContainer.Redirect != null)
                {
                    _dataContainer.Redirect.Data = value;
                }
                else
                {
                    _dataContainer.Data = value;
                }
            }
        }
        [NonSerialized] private WorldStampDataContainer _dataContainer;

        public WorldStampMask Mask;

        public bool HaveHeightsBeenFlipped = false;

        public int Priority = 0;
        public Vector3 Size;
        public bool SnapPosition;
        public bool SnapRotation;
        public bool SnapToTerrainHeight;
        public float SnapToTerrainHeightOffset;

        public string LayerName = "StampLayer";

        [SerializeField]
        private Vector3 _lastSnapPosition;

        public bool WriteStencil = true;

        // Heights
        public bool WriteHeights = true;
        public EHeightBlendMode LayerHeightBlendMode = EHeightBlendMode.Max;
        public float MinHeight = -9999;
        public float HeightOffset = 0;

        public bool WriteObjects = true;
        public bool RemoveBaseObjects = true;
        public bool StencilObjects = true; 
        public bool SnapObjectToGround = true;    // TODO
        public bool ScaleObjects = false;
        public bool RelativeToStamp = false;
        public EObjectRelativeMode ObjectRelativeMode = EObjectRelativeMode.RelativeToTerrain;

        // Splats
        public bool WriteSplats = true;
        public bool StencilSplats = false;
        [Tooltip("How to blend with existing splats on terrain")]
        public ESplatBlendMode SplatBlendMode = ESplatBlendMode.Set;
        public List<SplatPrototypeWrapper> IgnoredSplats = new List<SplatPrototypeWrapper>();

        // Trees
        public bool WriteTrees = true;
        [Tooltip("Remove trees on layers below this stamp?")]
        public bool RemoveBaseTrees = true;
        public bool RemoveSameLayerTrees = true;
        [Tooltip("Remove trees in this stamp if we don't write to the stencil?")]
        public bool StencilTrees = true;   

        // Details
        public bool WriteDetails = true;
        [Tooltip("Coefficient for detail strength")]
        public float DetailBoost = 1;
        [Tooltip("How to blend with existing details on terrain")]
        public ESplatBlendMode DetailBlendMode = ESplatBlendMode.Set;
        public List<DetailPrototypeWrapper> IgnoredDetails = new List<DetailPrototypeWrapper>();

        public bool PreviewEnabled = false;
        public bool GizmosEnabled = true;
        public Color GizmoColor = new Color(1, 1, 1, .3f);
        private WorldStampPreview _preview;

        public WorldStampMask GetMask()
        {
            if (Mask != null && Mask.Count > 0)
            {
                return Mask;
            }
            return Data.Mask;
        }

        public void OnDrawGizmos()
        {
#if UNITY_EDITOR
            if (GizmosEnabled)
            {
                GizmoExtensions.DrawWireCube(transform.position, Size.xz().x0z()/2, transform.rotation, GizmoColor);
            }
#endif
            if (_dataContainer != null && _dataContainer.Redirect == null)
            {
                _dataContainer.LinkToPrefab();
            }

            /*if (!HaveHeightsBeenFlipped)
            {
                HaveHeightsBeenFlipped = true;
                Data.Heights = Data.Heights.Flip();
            }*/

            transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
            if (SnapPosition)
            {
                transform.position = transform.position.Round();
            }
            if (SnapRotation)
            {
                transform.rotation = transform.rotation.SnapToNearest90Degrees();
            }
            if (SnapToTerrainHeight && transform.position != _lastSnapPosition)
            {
                var allWrappers =
                    GetTerrainWrappers()
                        .OrderBy(wrapper => Vector3.Distance(wrapper.transform.position, transform.position))
                        .ToList();
                if (allWrappers.Count > 0)
                {
                    var sample = allWrappers[0].transform.position.y +
                                 allWrappers[0].GetCompoundHeight(allWrappers[0].GetLayer<TerrainLayer>(LayerName),
                                     transform.position) * allWrappers[0].Terrain.terrainData.size.y;
                    transform.position = new Vector3(transform.position.x, sample, transform.position.z) + Vector3.up * SnapToTerrainHeightOffset;
                }
                _lastSnapPosition = transform.position;
            }

            if (!PreviewEnabled && _preview != null)
            {
                _preview.Dispose();
                _preview = null;
            }
            if (PreviewEnabled && _preview == null)
            {
                _preview = new WorldStampPreview();
                _preview.Invalidate(this);
            }
        }

        public void SetData(WorldStampData data)
        {
            Data = data;
            Size = Data.Size;
        }
        
        public static void ApplyAllStamps(TerrainWrapper wrapper, string layerFilter = null)
        {
            Profiler.BeginSample("ApplyAllStamps");
            Profiler.BeginSample("CollectAndOrganise");
            // Collect stamps
            var allStamps = new List<WorldStamp>(FindObjectsOfType<WorldStamp>());
            var tBounds = new Bounds(wrapper.Terrain.GetPosition() + wrapper.Terrain.terrainData.size/2,
                wrapper.Terrain.terrainData.size);
            for (var i = allStamps.Count - 1; i >= 0; i--)
            {
                var worldStamp = allStamps[i];

                if (!string.IsNullOrEmpty(layerFilter) && worldStamp.LayerName != layerFilter)
                {
                    allStamps.RemoveAt(i);
                    continue;
                }

                var stampBounds = new ObjectBounds(worldStamp.transform.position, worldStamp.Size/2,
                    worldStamp.transform.rotation);
                var axisStampBounds = stampBounds.ToAxisBounds();
                if (!tBounds.Intersects(axisStampBounds))
                {
                    allStamps.RemoveAt(i);
                }
            }

            List<LayerStampMapping> mappings = new List<LayerStampMapping>();

            // Setup layers
            foreach (var stamp in allStamps)
            {
                LayerStampMapping mapping = null;
                foreach (var layerStampMapping in mappings)
                {
                    if (layerStampMapping.LayerName == stamp.LayerName)
                    {
                        mapping = layerStampMapping;
                        break;
                    }
                }
                if (mapping == null)
                {
                    mapping = new LayerStampMapping()
                    {
                        LayerIndex = wrapper.GetLayerIndex(stamp.LayerName),
                        LayerName = stamp.LayerName
                    };
                    mappings.Add(mapping);
                }
                mapping.Stamps.Add(stamp);
            }

            mappings = mappings.OrderByDescending(mapping => mapping.LayerIndex).ToList();
            for (int i = 0; i < mappings.Count; i++)
            {
                mappings[i].Stamps = mappings[i].Stamps.OrderBy(stamp => stamp.Priority)
                    .ThenBy(stamp => stamp.transform.GetSiblingIndex())
                    .ToList();
            }

            for (int i =  mappings.Count - 1; i >= 0; i--)
            {
                var layerStampMapping = mappings[i];
                var layer = wrapper.GetLayer<TerrainLayer>(layerStampMapping.LayerName, false, true);
                if (!layer.UserOwned)
                {
                    mappings.RemoveAt(i);
                    continue;
                }
                if (wrapper.GetLayerIndex(layer) == wrapper.Layers.Count-1)
                {
                    layer.BlendMode = TerrainLayer.ETerrainLayerBlendMode.Set;
                }
                else
                {
                    layer.BlendMode = TerrainLayer.ETerrainLayerBlendMode.Stencil;
                }
                layer.Clear(wrapper);
            }
            Profiler.EndSample();

            for (int i = 0; i < mappings.Count; i++)
            {
                var layerStampMapping = mappings[i];
                var layer = wrapper.GetLayer<TerrainLayer>(layerStampMapping.LayerName, false, true);
                wrapper.CopyCompoundToLayer(layer);
                for (int j = 0; j < layerStampMapping.Stamps.Count; j++)
                {
                    MiscUtilities.ProgressBar(string.Format("Applying Heights for Stamp {0} : Layer {1}", layerStampMapping.Stamps[j].name, layer.name), string.Format("{0}/{1}", j, layerStampMapping.Stamps.Count), j / (float)layerStampMapping.Stamps.Count);
                    layerStampMapping.Stamps[j].StampHeights(wrapper, layer);
                }
                //layer.Stencil.Clear();  // To clear out prepass
                for (int j = 0; j < layerStampMapping.Stamps.Count; j++)
                {
                    MiscUtilities.ProgressBar(string.Format("Applying Stencil for Stamp {0} : Layer {1}", layerStampMapping.Stamps[j].name, layer.name), string.Format("{0}/{1}", j, layerStampMapping.Stamps.Count), j / (float)layerStampMapping.Stamps.Count);
                    layerStampMapping.Stamps[j].StampStencil(wrapper, layer, j+1);
                }

               MiscUtilities.ClampStencil(layer.Stencil);
               // MiscUtilities.InvertStencil(layer.Stencil);

                // Copy Splats
                /*var sRes = wrapper.Terrain.terrainData.alphamapResolution;
                var allSplats = wrapper.GetCompoundSplats(layer, 0, 0, sRes, sRes, false);
                foreach (var pair in allSplats)
                {
                    layer.SetSplatmap(pair.Key, 0, 0, pair.Value, sRes);
                }

                var dRes = wrapper.Terrain.terrainData.detailResolution;
                var allDetails = wrapper.GetCompoundDetails(layer, 0, 0, dRes, dRes, false);
                foreach (var pair in allDetails)
                {
                    layer.SetDetailMap(pair.Key, 0, 0, pair.Value, sRes);
                }*/

                for (int j = 0; j < layerStampMapping.Stamps.Count; j++)
                {
                    var worldStamp = layerStampMapping.Stamps[j];
                    var stencilKey = j + 1;

                    MiscUtilities.ProgressBar(string.Format("Applying Splats for Stamp {0} : Layer {1}", layerStampMapping.Stamps[j].name, layer.name), string.Format("{0}/{1}", j, layerStampMapping.Stamps.Count), j / (float)layerStampMapping.Stamps.Count);
                    worldStamp.StampSplats(wrapper, layer, stencilKey);

                    MiscUtilities.ProgressBar(string.Format("Applying Objects for Stamp {0} : Layer {1}", layerStampMapping.Stamps[j].name, layer.name), string.Format("{0}/{1}", j, layerStampMapping.Stamps.Count), j / (float)layerStampMapping.Stamps.Count);
                    worldStamp.StampObjects(wrapper, layer, stencilKey);

                    MiscUtilities.ProgressBar(string.Format("Applying Trees for Stamp {0} : Layer {1}", layerStampMapping.Stamps[j].name, layer.name), string.Format("{0}/{1}", j, layerStampMapping.Stamps.Count), j / (float)layerStampMapping.Stamps.Count);
                    worldStamp.StampTrees(wrapper, layer, stencilKey);
                    
                    MiscUtilities.ProgressBar(string.Format("Applying Details for Stamp {0} : Layer {1}", layerStampMapping.Stamps[j].name, layer.name), string.Format("{0}/{1}", j, layerStampMapping.Stamps.Count), j / (float)layerStampMapping.Stamps.Count);
                    worldStamp.StampDetails(wrapper, layer, stencilKey);
                }

                //MiscUtilities.AbsStencil(layer.Stencil);
                MiscUtilities.ColoriseStencil(layer.Stencil);
                wrapper.ClearCompoundCache(layer);
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(layer);
#endif
            }

            MiscUtilities.ClearProgressBar();

#if UNITY_EDITOR
            if (!UnityEditor.EditorPrefs.GetBool("worldStamp_DirtyOnStamp"))
            {
                return;
            }
#endif
            Profiler.EndSample();
        }
        
        private bool ShouldWriteHeights()
        {
            return WriteHeights && Data.Heights != null && Data.Heights.HasData();
        }

        float GetHeightAtPoint(Vector3 wPos, float existingHeight, float stampHeight, float terrainSizeY, out float maskValue)
        {
            float newHeight = 0;
            float sampleHeight = 0;

            var normalisedStampPosition = Quaternion.Inverse(transform.rotation) * (wPos - transform.position);
            normalisedStampPosition = new Vector3(normalisedStampPosition.x / Size.x, normalisedStampPosition.y,
                normalisedStampPosition.z / Size.z);
            normalisedStampPosition =
                new Vector3(normalisedStampPosition.x * Data.Size.x, normalisedStampPosition.y,
                    normalisedStampPosition.z * Data.Size.z) + Data.Size.xz().x0z() / 2;
            maskValue = GetMask().GetBilinear(Data.GridManager, normalisedStampPosition);

            if (ShouldWriteHeights())
            {
                if (maskValue > 0)
                {
                    normalisedStampPosition = new Vector3(normalisedStampPosition.x / Data.Size.x,
                    normalisedStampPosition.y / Data.Size.y, normalisedStampPosition.z / Data.Size.z);
                    normalisedStampPosition = new Vector3(normalisedStampPosition.x, normalisedStampPosition.y,
                        normalisedStampPosition.z);

                    if (!HaveHeightsBeenFlipped)
                    {
                        normalisedStampPosition = new Vector3(normalisedStampPosition.z, normalisedStampPosition.y, normalisedStampPosition.x);
                    }

                    sampleHeight = Data.Heights.BilinearSample(normalisedStampPosition.xz()) * Size.y + HeightOffset;
                    sampleHeight /= terrainSizeY;
                    sampleHeight *= maskValue;
                }
                else if (LayerHeightBlendMode == EHeightBlendMode.Set)
                {
                    sampleHeight = existingHeight;
                    stampHeight = 0;
                }
            }
            else if (LayerHeightBlendMode == EHeightBlendMode.Set)
            {
                sampleHeight = existingHeight;
                stampHeight = 0;
            }
            else
            {
                stampHeight = 0;
                //maskValue = 0;
            }

            switch (LayerHeightBlendMode)
            {
                case EHeightBlendMode.Set:
                    newHeight = sampleHeight + stampHeight;
                    break;
                case EHeightBlendMode.Add:
                    newHeight = existingHeight + sampleHeight;
                    break;
                case EHeightBlendMode.Max:
                    newHeight = Mathf.Max(existingHeight, sampleHeight + stampHeight);
                    break;
                case EHeightBlendMode.Min:
                    newHeight = Mathf.Min(existingHeight, sampleHeight + stampHeight);
                    break;
                case EHeightBlendMode.Average:
                    newHeight = existingHeight + (sampleHeight + stampHeight) / 2;
                    break;
            }
            
            //return newHeight;
            return Mathf.Lerp(existingHeight, newHeight, maskValue);
        }

        private void StampHeights(TerrainWrapper terrainWrapper, TerrainLayer layer)
        {
            if (!ShouldWriteHeights())
            {
                return;
            }

            Profiler.BeginSample("StampHeights");
            Profiler.BeginSample("Setup");

            // Apply heights
            var terrain = terrainWrapper.Terrain;
            var tSize = terrainWrapper.Terrain.terrainData.size;
            var tRes = terrainWrapper.Terrain.terrainData.heightmapResolution;
            var stampBounds = new ObjectBounds(transform.position, Size/2, transform.rotation);
            stampBounds.Expand((tSize/tRes));
            stampBounds.Expand(Vector3.up * 5000);

            var axisStampBounds = stampBounds.ToAxisBounds();
            var targetMinCoord = terrain.WorldToHeightmapCoord(axisStampBounds.min, TerrainX.RoundType.Ceil);
            var targetMaxCoord = terrain.WorldToHeightmapCoord(axisStampBounds.max, TerrainX.RoundType.Ceil);
            var heightArraySize = new Coord(targetMaxCoord.x - targetMinCoord.x, targetMaxCoord.z - targetMinCoord.z);
            
            var stampHeight = (transform.position.y - terrainWrapper.transform.position.y)/
                              terrainWrapper.Terrain.terrainData.size.y;
            var layerHeights = layer.GetHeights(targetMinCoord.x, targetMinCoord.z, heightArraySize.x, heightArraySize.z, tRes) ??
                new Serializable2DFloatArray(heightArraySize.x, heightArraySize.z);
            
            if (layer.Stencil == null)
            {
                layer.Stencil = new Serializable2DFloatArray(tRes, tRes);
            }
            Profiler.EndSample();

            Profiler.BeginSample("MainLoop");
            for (var u = targetMinCoord.x; u < targetMaxCoord.x; ++u)
            {
                var arrayU = u - targetMinCoord.x;
                for (var v = targetMinCoord.z; v < targetMaxCoord.z; ++v)
                {
                    var arrayV = v - targetMinCoord.z;
                    var wPos = terrain.HeightmapCoordToWorldPos(new Common.Coord(u, v));

                    //var baseHeight = baseHeights != null ? baseHeights[arrayU, arrayV] : 0;
                    var layerHeight = layerHeights[arrayU, arrayV];
                    var stencilStrength = layer.Stencil[u, v];

                    float existingStencilStrength;
                    int throwAwayKey;
                    MiscUtilities.DecompressStencil(stencilStrength, out throwAwayKey, out existingStencilStrength, false);
                    existingStencilStrength = existingStencilStrength > 0 ? 1 : 0;
                    //var existingHeight = Mathf.Lerp(baseHeight, layerHeight, existingStencilStrength);

                    float maskValue;
                    var newHeight = GetHeightAtPoint(wPos, layerHeight, stampHeight, tSize.y, out maskValue);
                    layerHeights[arrayU, arrayV] = newHeight;
                    
                    if (maskValue + existingStencilStrength > 0)
                    {
                        // Stencil pre pass
                        var newStencilVal = MiscUtilities.CompressStencil(-1, maskValue + existingStencilStrength);
                        layer.Stencil[u, v] = newStencilVal;
                    }
                }
            }
            layer.SetHeights(targetMinCoord.x, targetMinCoord.z, layerHeights,
                terrainWrapper.Terrain.terrainData.heightmapResolution);

            Profiler.EndSample();
            Profiler.EndSample();
        }

        private void StampStencil(TerrainWrapper terrainWrapper, TerrainLayer layer, int stencilKey)
        {
            if (!WriteStencil)
            {
                return;
            }
            var res = terrainWrapper.Terrain.terrainData.heightmapResolution;
            if (layer.Stencil == null || layer.Stencil.Width != res || layer.Stencil.Height != res)
            {
                layer.Stencil = new Serializable2DFloatArray(res, res);
            }

            var terrain = terrainWrapper.Terrain;
            var tSize = terrainWrapper.Terrain.terrainData.size;

            var scaledSize = (Size / 2);
            scaledSize.Scale(transform.lossyScale);
            var stampBounds = new ObjectBounds(transform.position, scaledSize, transform.rotation);
            stampBounds.Expand((tSize / res));
            stampBounds.Expand(Vector3.up * 5000);
            var axisStampBounds = stampBounds.ToAxisBounds();

            var targetMinCoord = terrain.WorldToHeightmapCoord(axisStampBounds.min, TerrainX.RoundType.Ceil);
            targetMinCoord = targetMinCoord.Clamp(0, terrain.terrainData.heightmapResolution);
            var targetMaxCoord = terrain.WorldToHeightmapCoord(axisStampBounds.max, TerrainX.RoundType.Ceil);
            targetMaxCoord = targetMaxCoord.Clamp(0, terrain.terrainData.heightmapResolution);

            //var heightArraySize = new TerrainCoord(targetMaxCoord.x - targetMinCoord.x, targetMaxCoord.z - targetMinCoord.z);
            //var baseHeights = terrainWrapper.GetCompoundHeights(layer, targetMinCoord.x, targetMinCoord.z, heightArraySize.x, heightArraySize.z, res);

            var stampHeight = (transform.position.y - terrainWrapper.transform.position.y);

            for (var u = targetMinCoord.x; u < targetMaxCoord.x; ++u)
            {
                //var arrayU = u - targetMinCoord.x;
                for (var v = targetMinCoord.z; v < targetMaxCoord.z; ++v)
                {
                    //var arrayV = v - targetMinCoord.z;
                    var wPos = terrain.HeightmapCoordToWorldPos(new Common.Coord(u, v));
                    if (!stampBounds.Contains(wPos))
                    {
                        continue;
                    }

                    var maskPos = Quaternion.Inverse(transform.rotation) * (wPos - transform.position);
                    maskPos = new Vector3(maskPos.x / Size.x, maskPos.y, maskPos.z / Size.z);
                    maskPos = new Vector3(maskPos.x * Data.Size.x, maskPos.y, maskPos.z * Data.Size.z) + (Data.Size / 2);

                    var maskValue = GetMask().GetBilinear(Data.GridManager, maskPos);
                    if (Math.Abs(maskValue) <= 0)
                    {
                        continue;
                    }

                    int existingStencilKey;
                    float existingStencilStrength;
                    var rawStencilValue = layer.Stencil[u, v];
                    MiscUtilities.DecompressStencil(rawStencilValue, out existingStencilKey, out existingStencilStrength, false);
                    if (WriteHeights)
                    {
                        if (Math.Abs(existingStencilStrength) < 0.01f)
                        {
                            continue;
                        }
                        if (LayerHeightBlendMode == EHeightBlendMode.Max || LayerHeightBlendMode == EHeightBlendMode.Min && existingStencilStrength > 0)
                        {
                            var normalisedPos = Quaternion.Inverse(transform.rotation) * (wPos - transform.position) + Size.xz().x0z() / 2;
                            normalisedPos = new Vector3(normalisedPos.x / Size.x, normalisedPos.y / Size.y, normalisedPos.z / Size.z);

                            //var baseHeight = baseHeights != null ? baseHeights[arrayU, arrayV] * tSize.y : 0;
                            var layerHeight = layer.Heights[u, v] * tSize.y;
                            var predictedOutHeight = float.MinValue;
                            if (ShouldWriteHeights())
                            {
                                var heightPos = new Vector2(normalisedPos.x, normalisedPos.z);
                                if (!HaveHeightsBeenFlipped)
                                {
                                    heightPos = new Vector2(normalisedPos.z, normalisedPos.x);
                                }
                                predictedOutHeight = Data.Heights.BilinearSample(heightPos) * Size.y * maskValue;
                                predictedOutHeight += stampHeight;

                                var newStencilVal = MiscUtilities.CompressStencil(stencilKey, maskValue + existingStencilStrength);
                                float tolerance = .2f;
                                if (LayerHeightBlendMode == EHeightBlendMode.Max)
                                {
                                    predictedOutHeight = Mathf.Max(predictedOutHeight, MinHeight);
                                    //predictedOutHeight = Mathf.Max(predictedOutHeight, baseHeight);
                                    if (predictedOutHeight >= layerHeight - tolerance)
                                    {
                                        layer.Stencil[u, v] = newStencilVal;
                                    }
                                }
                                else if (LayerHeightBlendMode == EHeightBlendMode.Min)
                                {
                                    //predictedOutHeight = Mathf.Min(predictedOutHeight, baseHeight);
                                    if (predictedOutHeight <= layerHeight + tolerance)
                                    {
                                        layer.Stencil[u, v] = newStencilVal;
                                    }
                                }
                            }
                            continue;
                        }
                    }
                    float newStencilValue;
                    if (existingStencilStrength > 0 && existingStencilKey > 0 && existingStencilKey != stencilKey)
                    {
                        newStencilValue = MiscUtilities.CompressStencil(stencilKey, maskValue + existingStencilStrength);
                    }
                    else
                    {
                        newStencilValue = MiscUtilities.CompressStencil(stencilKey, maskValue);
                    }
                    layer.Stencil[u, v] = newStencilValue;
                }
            }
        }

        private void StampSplats(TerrainWrapper terrainWrapper, TerrainLayer layer, int stencilKey)
        {
            if (!WriteSplats || Data.SplatData.Count == 0 || IgnoredSplats.Count == Data.SplatData.Count)
            {
                return;
            }

            Profiler.BeginSample("StampSplats");
            if (SplatBlendMode > ESplatBlendMode.Average)
            {
                Debug.LogWarning("Using old splat blend mode! Set to SET");
            }

            // Find information about where the stamp is relative to the terrain
            var terrain = terrainWrapper.Terrain;
            var tSize = terrainWrapper.Terrain.terrainData.size;
            var sRes = terrainWrapper.Terrain.terrainData.alphamapResolution;
            var stampBounds = new ObjectBounds(transform.position, Size/2, transform.rotation);
            stampBounds.Expand((tSize/sRes));
            var axisBounds = stampBounds.ToAxisBounds();
            var targetMinCoord = terrain.WorldToSplatCoord(axisBounds.min);
            var targetMaxCoord = terrain.WorldToSplatCoord(axisBounds.max);
            targetMinCoord = targetMinCoord.Clamp(0, sRes - 1);
            targetMaxCoord = targetMaxCoord.Clamp(0, sRes - 1);
            var arraySize = new Common.Coord(targetMaxCoord.x - targetMinCoord.x, targetMaxCoord.z - targetMinCoord.z);

            var thisLayerSplatData = layer.GetSplatMaps(targetMinCoord.x, targetMinCoord.z, arraySize.x, arraySize.z, sRes);
            Profiler.BeginSample("MainLoop");
            Serializable2DFloatArray applyStencil = new Serializable2DFloatArray(arraySize.x, arraySize.z);
            for (var u = targetMinCoord.x; u < targetMaxCoord.x; ++u)
            {
                var arrayU = u - targetMinCoord.x;
                for (var v = targetMinCoord.z; v < targetMaxCoord.z; ++v)
                {
                    var arrayV = v - targetMinCoord.z;
                    var wPos = terrain.SplatCoordToWorldPos(new Common.Coord(u, v));
                        
                    // Get the value of the mask
                    var normalisedStampPosition = Quaternion.Inverse(transform.rotation) * (wPos - transform.position) + (Size / 2);
                    var maskPos = new Vector3(normalisedStampPosition.x / Size.x, normalisedStampPosition.y, normalisedStampPosition.z / Size.z);
                    maskPos = new Vector3(maskPos.x * Data.Size.x, maskPos.y, maskPos.z * Data.Size.z);
                    var maskValue = GetMask().GetBilinear(Data.GridManager, maskPos);

                    if (StencilSplats)
                    {
                        var stencilPos = new Vector2(u / (float)(sRes + 1), v / (float)(sRes + 1));
                        maskValue = layer.GetStencilStrength(stencilPos, stencilKey);
                    }

                    applyStencil[arrayU, arrayV] = maskValue > 0 ? 1 : 0;

                    normalisedStampPosition = new Vector3(normalisedStampPosition.x / Size.x, normalisedStampPosition.y / Size.y, normalisedStampPosition.z / Size.z);
                    if (normalisedStampPosition.x < 0 || normalisedStampPosition.x > 1 ||
                        normalisedStampPosition.z < 0 || normalisedStampPosition.z > 1)
                    {
                        continue;
                    }

                    int sum = 0;
                    foreach (var splatPair in Data.SplatData)
                    {
                        if (IgnoredSplats.Contains(splatPair.Wrapper) || splatPair.Wrapper == null)
                        {
                            continue;
                        }

                        var stampValue = splatPair.Data.BilinearSample(normalisedStampPosition.xz()) / 255f;
                        Serializable2DByteArray layerData;
                        if (!thisLayerSplatData.TryGetValue(splatPair.Wrapper, out layerData))
                        {
                            layerData = new Serializable2DByteArray(arraySize.x, arraySize.z);
                            thisLayerSplatData[splatPair.Wrapper] = layerData;
                        }

                        var layerVal = layerData != null ? layerData[arrayU, arrayV] / 255f : 0;
                            
                        float newValue = 0f;
                        switch (SplatBlendMode)
                        {
                            case ESplatBlendMode.Set:
                                newValue = Mathf.Lerp(layerVal, stampValue, maskValue);
                                break;
                            case ESplatBlendMode.Add:
                                stampValue *= maskValue;
                                newValue = layerVal + stampValue;
                                break;
                            case ESplatBlendMode.Max:
                                stampValue *= maskValue;
                                newValue = Mathf.Max(layerVal, stampValue);
                                break;
                        }
                            
                        var byteAmount = (byte)Mathf.Clamp(newValue * 255, 0, 255);
                            
                        sum += byteAmount;
                        layerData[arrayU, arrayV] = byteAmount;
                    }

                    float floatSum = sum/255f;
                    if (floatSum > 0)
                    {
                        foreach (var serializable2DByteArray in thisLayerSplatData)
                        {
                            if (Data.SplatData.Any(data => data.Wrapper == serializable2DByteArray.Key))
                            {
                                continue;
                            }

                            var read = serializable2DByteArray.Value[arrayU, arrayV] / 255f;
                            if (floatSum < 1)
                            {
                                var newCompoundVal = read * (1 - floatSum);
                                var newCompoundByteVal = (byte)Mathf.Clamp(newCompoundVal * 255, 0, 255);
                                serializable2DByteArray.Value[arrayU, arrayV] = newCompoundByteVal;
                            }
                            else
                            {
                                serializable2DByteArray.Value[arrayU, arrayV] = 0;
                            }
                        }
                    }
                }
            }
            
            foreach (var pair in thisLayerSplatData)
            {
                layer.SetSplatmap(pair.Key, targetMinCoord.x, targetMinCoord.z, pair.Value, sRes, applyStencil);
            }

            MiscUtilities.AbsStencil(layer.Stencil, stencilKey);
            Profiler.EndSample();
            Profiler.EndSample();
        }

        private void StampDetails(TerrainWrapper terrainWrapper, TerrainLayer layer, int stencilKey)
        {
            if (!WriteDetails)
            {
                return;
            }

            Profiler.BeginSample("StampDetails");
            // Apply details
            var terrain = terrainWrapper.Terrain;
            var tSize = terrainWrapper.Terrain.terrainData.size;
            var dRes = terrainWrapper.Terrain.terrainData.detailResolution;
            var stampBounds = new ObjectBounds(transform.position, Size, transform.rotation);
            stampBounds.Expand((tSize / dRes));

            var axisBounds = stampBounds.ToAxisBounds();

            var targetMinCoord = terrain.WorldToDetailCoord(axisBounds.min);
            var targetMaxCoord = terrain.WorldToDetailCoord(axisBounds.max);
            targetMinCoord = targetMinCoord.Clamp(0, dRes);
            targetMaxCoord = targetMaxCoord.Clamp(0, dRes);

            var arraySize = new Common.Coord(targetMaxCoord.x - targetMinCoord.x, targetMaxCoord.z - targetMinCoord.z);
            var allDetails = layer.GetDetailMaps(targetMinCoord.x, targetMinCoord.z, arraySize.x, arraySize.z, dRes);
            
            for (var u = targetMinCoord.x; u < targetMaxCoord.x; ++u)
            {
                var arrayU = u - targetMinCoord.x;
                var uF = u/(float) dRes;
                for (var v = targetMinCoord.z; v < targetMaxCoord.z; ++v)
                {
                    var arrayV = v - targetMinCoord.z;
                    var vF = v/(float) dRes;

                    int sum = 0;
                    foreach (var valuePair in Data.DetailData)
                    {
                        if (valuePair.Wrapper == null || IgnoredDetails.Contains(valuePair.Wrapper))
                        {
                            continue;
                        }

                        Serializable2DByteArray data;
                        if (!allDetails.TryGetValue(valuePair.Wrapper, out data))
                        {
                            data = new Serializable2DByteArray(arraySize.x, arraySize.z);
                            allDetails[valuePair.Wrapper] = data;
                        }

                        var wPos = terrain.DetailCoordToWorldPos(new Common.Coord(u, v));
                        var normalisedStampPosition = Quaternion.Inverse(transform.rotation) * (wPos - transform.position) + Size.xz().x0z() / 2;
                        normalisedStampPosition = new Vector3(normalisedStampPosition.x / Size.x, normalisedStampPosition.y / Size.y, normalisedStampPosition.z / Size.z);
                        if (normalisedStampPosition.x < 0 || normalisedStampPosition.x > 1 ||
                            normalisedStampPosition.z < 0 || normalisedStampPosition.z > 1)
                        {
                            continue;
                        }

                        //var maskPosition = new Vector3(normalisedStampPosition.x * Data.Size.x, normalisedStampPosition.y * Data.Size.y, normalisedStampPosition.z * Data.Size.z);
                        var stencilValue = layer.GetStencilStrength(new Vector2(uF, vF), stencilKey);
                        if (stencilValue <= 0)
                        {
                            continue;
                        }

                        var sampleDetail = valuePair.Data.BilinearSample(normalisedStampPosition.xz());
                        var layerValue = data[arrayU, arrayV];

                        float newValueF = layerValue;
                        switch (SplatBlendMode)
                        {
                            case ESplatBlendMode.Set:
                                newValueF = Mathf.Lerp(layerValue, sampleDetail, stencilValue);
                                break;
                            case ESplatBlendMode.Add:
                                newValueF += sampleDetail * stencilValue;
                                break;
                            case ESplatBlendMode.Max:
                                newValueF = Mathf.Max(layerValue, sampleDetail * stencilValue);
                                break;
                        }
                        newValueF *= DetailBoost;
                        var newValByte = (byte)Mathf.RoundToInt(Mathf.Clamp(newValueF, 0, 16));
                        data[arrayU, arrayV] = newValByte;
                        sum += newValByte;
                    }

                    foreach (var pair in allDetails)
                    {
                        if (Data.DetailData.Any(data => data.Wrapper == pair.Key))
                        {
                            continue;
                        }
                        if (pair.Value == null)
                        {
                            Debug.LogError("Splat output was null for " + pair.Key);
                            continue;
                        }
                        pair.Value[arrayU, arrayV] = (byte) Mathf.Clamp(pair.Value[arrayU, arrayV] - sum, 0, 255);
                    }
                }
            }

            /*foreach (var valuePair in Data.DetailData)
            {
                var detailWrapper = valuePair.Wrapper;
                if (detailWrapper == null || IgnoredDetails.Contains(detailWrapper))
                {
                    continue;
                }

                int sum = 0;
                //var baseDetails = terrainWrapper.GetCompoundDetails(layer, detailWrapper, targetMinCoord.x, targetMinCoord.z, arraySize.x, arraySize.z);
                var layerDetails = layer.GetDetailMap(detailWrapper, targetMinCoord.x, targetMinCoord.z, arraySize.x, arraySize.z, detailResolution) ?? new Serializable2DByteArray(arraySize.x, arraySize.z);
                

                        if (DetailBlendMode == ESplatBlendMode.Set && layer.GetStencilStrength(new Vector2(uF, vF), stencilKey) < 0.01f)
                        {
                            continue;
                        }

                        var wPos = terrain.DetailCoordToWorldPos(new TerrainCoord(u, v));
                        var normalisedStampPosition = Quaternion.Inverse(transform.rotation) * (wPos - transform.position) + Size.xz().x0z() / 2;
                        normalisedStampPosition = new Vector3(normalisedStampPosition.x / Size.x, normalisedStampPosition.y / Size.y, normalisedStampPosition.z / Size.z);
                        if (normalisedStampPosition.x < 0 || normalisedStampPosition.x > 1 ||
                            normalisedStampPosition.z < 0 || normalisedStampPosition.z > 1)
                        {
                            continue;
                        }

                        var maskPosition = new Vector3(normalisedStampPosition.x * Data.Size.x, normalisedStampPosition.y * Data.Size.y, normalisedStampPosition.z * Data.Size.z);
                        var maskValue = GetMask().GetBilinear(Data.GridManager, maskPosition);

                        var sampleDetail = valuePair.Data.BilinearSample(normalisedStampPosition.xz()) * maskValue;
                        //var baseValue = baseDetails != null ? baseDetails[arrayU, arrayV] : 0;
                        var layerValue = layerDetails[arrayU, arrayV];

                        float newValueF = layerValue;
                        switch (SplatBlendMode)
                        {
                            case ESplatBlendMode.Set:
                                newValueF = sampleDetail;
                                break;
                            case ESplatBlendMode.Add:
                                newValueF += sampleDetail;
                                break;
                            case ESplatBlendMode.Max:
                                newValueF = Mathf.Max(layerValue, sampleDetail);
                                break;
                        }
                        newValueF *= DetailBoost * maskValue;
                        var newValByte = (byte)Mathf.RoundToInt(Mathf.Clamp(newValueF, 0, 16));
                        sum += newValByte;
                        layerDetails[arrayU, arrayV] = newValByte;
                    }
                }
                if (sum == 0)
                {
                    if (detailResolution == 0)
                    {
                        Debug.Log(string.Format("Stamp {0} didn't write any values in detail channel {1} - as detail resolution is 0!!", name, detailWrapper), this);
                    }
                    else
                    {
                        Debug.Log(string.Format("Stamp {0} didn't write any values in detail channel {1}", name, detailWrapper), this);
                    }
                    
                    continue;
                }

                layer.SetDetailMap(detailWrapper, targetMinCoord.x, targetMinCoord.z, layerDetails,
                terrain.terrainData.detailResolution);
            }*/

            foreach (var prototypeWrapper in allDetails)
            {
                layer.SetDetailMap(prototypeWrapper.Key, targetMinCoord.x, targetMinCoord.z, prototypeWrapper.Value, dRes);
            }

            Profiler.EndSample();
        }

        private void StampTrees(TerrainWrapper terrainWrapper, TerrainLayer layer, int stencilKey)
        {
            Profiler.BeginSample("StampTrees");
            // Stamp trees
            var tSize = terrainWrapper.Terrain.terrainData.size;
            var tPos = terrainWrapper.transform.position;

            var wrapperBounds =
                new Bounds(terrainWrapper.Terrain.GetPosition() + terrainWrapper.Terrain.terrainData.size / 2,
                    terrainWrapper.Terrain.terrainData.size);
            wrapperBounds.Expand(Vector3.up * 5000);
            if (RemoveBaseTrees)
            {
                var stampBounds = new ObjectBounds(transform.position, Size / 2, transform.rotation);
                stampBounds.Expand(Vector3.up * 5000);
                List<DingoTreeInstance> compoundTrees = terrainWrapper.GetCompoundTrees(layer, RemoveSameLayerTrees);
                foreach (var hurtTreeInstance in compoundTrees)
                {
                    if (layer.TreeRemovals.Contains(hurtTreeInstance.Guid))
                    {
                        continue;
                    }

                    var wPos = hurtTreeInstance.Position;
                    wPos = new Vector3(wPos.x * tSize.x, wPos.y * tSize.y, wPos.z * tSize.z);
                    wPos += tPos;

                    if (stampBounds.Contains(wPos))
                    {
                        var stencilPos = wPos - tPos;
                        stencilPos = new Vector2(stencilPos.x / tSize.x, stencilPos.z / tSize.z);
                        var stencilAmount = layer.GetStencilStrength(stencilPos, stencilKey);
                        if (stencilAmount > 0.5f)
                        {
                            layer.TreeRemovals.Add(hurtTreeInstance.Guid);
                            //Debug.DrawLine(wPos, wPos + Vector3.up * stencilAmount * 20, Color.red, 30);
                        }
                    }
                }
            }

            if (!WriteTrees)
            {
                Profiler.EndSample();
                return;
            }

            for (var i = 0; i < Data.Trees.Count; i++)
            {
                var tree = Data.Trees[i].Clone();

                var maskPos = new Vector3(tree.Position.x*Data.Size.x, 0, tree.Position.z*Data.Size.z)/* + (Data.Size/2)*/;
                var maskValue = GetMask().GetBilinear(Data.GridManager, maskPos);
                if (maskValue <= 0.25f)
                {
                    continue;
                }

                var wPos = transform.position + transform.rotation * (new Vector3(tree.Position.x * Size.x, tree.Position.y, tree.Position.z * Size.z) - (Size.xz().x0z() / 2));
                if (!wrapperBounds.Contains(wPos))
                {
                    continue;
                }

                if (StencilTrees)
                {
                    var stencilPos = new Vector2((wPos.x - tPos.x) / tSize.x, (wPos.z - tPos.z) / tSize.z);
                    var stencilVal = layer.GetStencilStrength(stencilPos, stencilKey);
                    if (stencilVal <= 0.25f)
                    {
                        continue;
                    }
                }

                tree.Guid = Guid.NewGuid().ToString();
                tree.Position = wPos - terrainWrapper.transform.position;
                tree.Position = new Vector3(tree.Position.x / tSize.x, tree.Position.y / tSize.y - .5f, tree.Position.z / tSize.z);

                layer.Trees.Add(tree);
            }

            /*foreach (var hurtTreeInstance in layer.TreeRemovals)
            {
                if (layer.TreeRemovals.Contains(hurtTreeInstance.Guid))
                {
                    throw new Exception("Removing tree on own layer!");
                }
            }*/
            Profiler.EndSample();
        }

        private void StampObjects(TerrainWrapper terrainWrapper, TerrainLayer layer, int stencilKey)
        {
            Profiler.BeginSample("StampObjects");
            // Stamp objects
            var t = terrainWrapper.Terrain;
            var tSize = t.terrainData.size;
            var tPos = t.transform.position;

            if (RemoveBaseObjects)
            {
                var stampBounds = new ObjectBounds(transform.position, Size / 2, transform.rotation);
                stampBounds.Expand(Vector3.up * 5000);
                var compoundObjects = terrainWrapper.GetCompoundObjects(layer);
                foreach (var prefabObjectData in compoundObjects)
                {
                    var pos = prefabObjectData.Position;
                    pos = new Vector3(pos.x * tSize.x, pos.y, pos.z * tSize.z) + t.GetPosition();
                    pos = Quaternion.Inverse(transform.rotation) * (pos - transform.position);
                    var wPos = pos;
                    //pos = new Vector3(pos.x / Size.x, pos.y, pos.z / Size.z);
                    //pos = new Vector3(pos.x * Data.Size.x, pos.y, pos.z * Data.Size.z) + Data.Size / 2;

                    if (stampBounds.Contains(wPos))
                    {
                        var stencilPos = wPos - tPos;
                        stencilPos = new Vector2(stencilPos.x / tSize.x, stencilPos.z / tSize.z);
                        var stencilAmount = layer.GetStencilStrength(stencilPos, stencilKey);
                        if (stencilAmount > 0.5f)
                        {
                            layer.ObjectRemovals.Add(prefabObjectData.Guid);
                            //Debug.DrawLine(wPos, wPos + Vector3.up * stencilAmount * 20, Color.red, 30);
                        }
                    }

                    /*var maskPos = new Vector3(prefabObjectData.Position.x*Data.Size.x, 0, prefabObjectData.Position.z*Data.Size.z);
                    var mask = GetMask().GetBilinear(Data.GridManager, maskPos);
                    if (mask > 0.5f)
                    {
                        DebugHelper.DrawPoint(wPos, 1, Color.blue, 20);
                        layer.ObjectRemovals.Add(prefabObjectData.Guid);
                    }*/
                }
            }

            if (!WriteObjects)
            {
                Profiler.EndSample();
                return;
            }

            var tBounds = new Bounds(t.GetPosition() + tSize / 2, tSize);
            tBounds.Expand(Vector3.up * 5000);
            for (var i = 0; i < Data.Objects.Count; i++)
            {
                var prefabObjectData = Data.Objects[i]; // PrefabObjectData is a struct so this is a copy

                if (!prefabObjectData.Prefab)
                {
                    continue;
                }

                if (prefabObjectData.Prefab.GetComponent<WorldStamp>())
                {
                    Debug.Log("Stamp had a Worldstamp prefab in it: " + name, this);
                    continue;
                }

#if UNITY_EDITOR
                if (UnityEditor.PrefabUtility.FindPrefabRoot(prefabObjectData.Prefab) != prefabObjectData.Prefab)
                {
                    Debug.LogWarning("Referencing inner prefab object somehow!", this);
                    continue;
                }
#endif

                prefabObjectData.Guid = Guid.NewGuid().ToString();  // So multiple stamps don't conflict
                var oldPos = prefabObjectData.Position;
                var maskAmount =
                    GetMask().GetBilinear(Data.GridManager,
                        new Vector3(prefabObjectData.Position.x * Data.Size.x, 0, prefabObjectData.Position.z * Data.Size.z));
                if (maskAmount <= 0)
                {
                    continue;
                }

                var worldPos = transform.position + transform.rotation *
                               (new Vector3(prefabObjectData.Position.x * Size.x, prefabObjectData.Position.y,
                                   prefabObjectData.Position.z * Size.z) - (Size.xz().x0z() / 2));

                if (!tBounds.Contains(worldPos))
                {
                    continue;
                }

                worldPos -= t.GetPosition();
                worldPos = new Vector3(worldPos.x / tSize.x, prefabObjectData.Position.y, worldPos.z / tSize.z);
                prefabObjectData.Position = worldPos;

                if (StencilObjects) // Possinle early return if we're trying to place an object outside the stencil
                {
                    var stencilValue =
                    layer.GetStencilStrength(new Vector2(prefabObjectData.Position.x, prefabObjectData.Position.z), stencilKey);
                    if (stencilValue <= 0)
                    {
                        continue;
                    }
                }

                prefabObjectData.Rotation = (transform.rotation * Quaternion.Euler(prefabObjectData.Rotation)).eulerAngles;

                if (ObjectRelativeMode == EObjectRelativeMode.RelativeToStamp)
                {
                    prefabObjectData.AbsoluteHeight = true;
                    if (HaveHeightsBeenFlipped)
                    {
                        prefabObjectData.Position.y += transform.position.y - tPos.y
                        + Data.Heights.BilinearSample(new Vector2(oldPos.x, oldPos.z)) * Data.Size.y;
                    }
                    else
                    {
                        prefabObjectData.Position.y += transform.position.y - tPos.y
                        + Data.Heights.BilinearSample(new Vector2(oldPos.z, oldPos.x)) * Data.Size.y;
                    }
                }

                if (prefabObjectData.Scale.x < 0 || prefabObjectData.Scale.y < 0 || prefabObjectData.Scale.z < 0)
                {
                    Debug.LogWarning(string.Format("Stamp {0} has created an object ({1}) with negative scale. This can cause performance issues if you do this lots! Select the stamp prefab to resolve this.", name, prefabObjectData.Prefab.name), this);
                }

                layer.Objects.Add(prefabObjectData);
            }
            Profiler.EndSample();
        }
        
        public List<TerrainWrapper> GetTerrainWrappers()
        {
            var result = new List<TerrainWrapper>();
            var allT = FindObjectsOfType<TerrainWrapper>();
            var stampBounds =
                new ObjectBounds(transform.position + Vector3.up*(Size.y/2), Size/2, transform.rotation).ToAxisBounds();
            foreach (var terrainWrapper in allT)
            {
                var b = terrainWrapper.GetComponent<TerrainCollider>().bounds;
                b.Expand(Vector3.up*9999999);
                if (b.Intersects(stampBounds))
                {
                    result.Add(terrainWrapper);
                }
            }
            return result;
        }

        public void OnDestroy()
        {
            if (_preview != null)
            {
                _preview.Dispose();
                _preview = null;
            }
        }
    }
}