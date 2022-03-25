using System;
using System.Collections.Generic;
using System.Linq;
using MadMaps.Common;
using MadMaps.Common.Collections;
using MadMaps.Terrains;
using UnityEngine;
using UnityEngine.Serialization;
using MadMaps.Common.GenericEditor;

namespace MadMaps.WorldStamps
{
    [ExecuteInEditMode]
    [HelpURL("http://lrtw.net/madmaps/index.php?title=World_Stamps")]
    #if HURTWORLDSDK
    [StripComponentOnBuild()]
    #endif
    public partial class WorldStamp : LayerComponentBase
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

        //======================================
        // General Configuration
        //======================================
        public WorldStampMask Mask;
        public bool HaveHeightsBeenFlipped = false;     // Legacy flag (old heights used to be ZX not XZ)
        
        [FormerlySerializedAs("Size")]
        public Vector3 ExplicitSize;
        public override Vector3 Size { get {
            var copy = ExplicitSize;
            copy.Scale(transform.lossyScale);
            if(copy.IsNan() || copy.magnitude == 0)
            {
                Debug.LogError(string.Format("Stamp {0} size was invalid!", this), this);
                return Vector3.one;
            }
            return copy;
        }}
        public bool SnapPosition;
        public bool SnapRotation;
        public bool SnapToTerrainHeight;
        public float SnapToTerrainHeightOffset;
        public bool DisableStencil;
        public string LayerName = "StampLayer";
        [Common.GenericEditor.Min(1)]
        public int Priority = 1;
        

        //======================================
        // Heights
        //======================================
        public bool WriteHeights = true;
        public EHeightBlendMode LayerHeightBlendMode = EHeightBlendMode.Max;
        public float HeightOffset = 0;

        //======================================
        // Objects
        //======================================
        public bool WriteObjects = true;
        [Tooltip("Should this stamp remove objects?")]
        [FormerlySerializedAs("RemoveBaseObjects")]
        public bool RemoveObjects = true;
        [Tooltip("Remove objects in this stamp if we don't write to the stencil?")]
        public bool StencilObjects = true;
       
        public bool NeedsRelativeModeCheck = true;
        public EObjectRelativeMode RelativeMode = EObjectRelativeMode.RelativeToTerrain;
        
        //======================================
        // Splats
        //======================================
        public bool WriteSplats = true;
        public bool StencilSplats = false;
        [Tooltip("How to blend with existing splats on terrain")]
        public ESplatBlendMode SplatBlendMode = ESplatBlendMode.Set;
        public List<SplatPrototypeWrapper> IgnoredSplats = new List<SplatPrototypeWrapper>();

        //======================================
        // Trees
        //======================================
        public bool WriteTrees = true;
        [Tooltip("Should this stamp remove trees?")]
        [FormerlySerializedAs("RemoveBaseTrees")]
        public bool RemoveTrees = true;
        [Tooltip("Remove trees in this stamp if we don't write to the stencil?")]
        public bool StencilTrees = true;
        public List<GameObject> IgnoredTrees = new List<GameObject>();

        //======================================
        // Details
        //======================================
        public bool WriteDetails = true;
        [Tooltip("Coefficient for detail strength")]
        public float DetailBoost = 1;
        [Tooltip("How to blend with existing details on terrain")]
        public ESplatBlendMode DetailBlendMode = ESplatBlendMode.Set;
        public bool RemoveExistingDetails = false;
        public List<DetailPrototypeWrapper> IgnoredDetails = new List<DetailPrototypeWrapper>();

        //======================================
        // Gizmos
        //======================================
        public bool PreviewEnabled = false;
        public bool GizmosEnabled = true;
        public Color GizmoColor = new Color(1, 1, 1, .3f);

        // Internal
        private WorldStampPreview _preview;
        [SerializeField]
        private Vector3 _lastSnapPosition;


        // So why is this here, you may ask? Well, pretty much entirely to stop Unity running OnBeforeSerialize
        // every frame when you have a stamp selected. So, we keep the data in a subobject.
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

        // If we have an instance mask, return that. If not, return the mask of the prefab.
        public WorldStampMask GetMask()
        {
            if (Mask != null && Mask.Count > 0)
            {
                return Mask;
            }
            return Data.Mask;
        }

        public override bool GetEnabled()
        {
            return gameObject.activeInHierarchy && enabled;
        }

        public override void SetEnabled(bool value)
        {
            enabled = value;
        }

        public void OnDrawGizmos()
        {
#if UNITY_EDITOR
            if (GizmosEnabled)
            {
                var rectSize = Size.xz().x0z()/2;
                //rectSize.Scale(transform.lossyScale);
                GizmoExtensions.DrawWireCube(transform.position, rectSize, transform.rotation, GizmoColor);
                Gizmos.color = GizmoColor;
                var size = Mathf.Min(Size.MaxElement(), 10);                
                var yOffset = Vector3.up * Size.y *.3f;
                Gizmos.DrawCube(transform.position + yOffset, size * new Vector3(1, 0.25f, 1));
                Gizmos.DrawCube(transform.position + Vector3.up * size * .5f + yOffset, size * new Vector3(.25f, .75f, .25f));
                Gizmos.DrawSphere(transform.position + Vector3.up * size + yOffset, size * .5f);

                Gizmos.color = Color.white;
            }
#endif
            if (_dataContainer != null && _dataContainer.Redirect == null)
            {
                _dataContainer.LinkToPrefab();
            }

            transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);

            SnapStamp(false);

            if (!PreviewEnabled && _preview != null)
            {
                _preview.Dispose();
                _preview = null;
            }
            if (PreviewEnabled && (_preview == null || _preview.IsDisposed()) && Data != null)
            {
                _preview = new WorldStampPreview();
                _preview.Invalidate(
                    Data.Heights, () => Size, () => transform.position, () => Vector3.one,
                    () => transform.rotation, () => this.Data.Size, HaveHeightsBeenFlipped, GetMask(), Data.GridManager,
                    () => this && gameObject.activeInHierarchy, 32);
            }
        }

        public override Type GetLayerType()
        {
            return typeof(MMTerrainLayer);
        }

        public override void OnPreBake()
        {
            SnapStamp(true);
        }

        public void SnapStamp(bool force)
        {
            if (SnapRotation)
            {
                transform.rotation = transform.rotation.SnapToNearest90Degrees();
            }
            if (SnapToTerrainHeight && (force || transform.position != _lastSnapPosition))
            {
                var allWrappers =
                    GetTerrainWrappers()
                        .OrderBy(wrapper => Vector3.Distance(wrapper.transform.position, transform.position))
                        .ToList();
                if (allWrappers.Count > 0)
                {
                    var sample = allWrappers[0].transform.position.y +
                                 allWrappers[0].GetCompoundHeight(allWrappers[0].GetLayer<MMTerrainLayer>(LayerName),
                                     transform.position) * allWrappers[0].Terrain.terrainData.size.y;
                    transform.position = new Vector3(transform.position.x, sample, transform.position.z) + Vector3.up * SnapToTerrainHeightOffset;
                }
                _lastSnapPosition = transform.position;
            }
        }

        public void SetData(WorldStampData data)
        {
            Data = data;
            ExplicitSize = Data.Size;
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
            
            return Mathf.Lerp(existingHeight, newHeight, maskValue);
        }

        public override void ProcessHeights(TerrainWrapper terrainWrapper, LayerBase baseLayer, int stencilKey)
        {
            var layer = baseLayer as MMTerrainLayer;
            if(layer == null)
            {
                Debug.LogWarning(string.Format("Attempted to write {0} to incorrect layer type! Expected Layer {1} to be {2}, but it was {3}", name, baseLayer.name, GetLayerType(), baseLayer.GetType()), this);
                return;
            }

            if (!ShouldWriteHeights())
            {
                if (WriteHeights)
                {
                    Debug.LogWarning(
                        string.Format(
                            "Stamp {0} is set to write Heights, but is not currently configured correctly to do so. This may be unintended.",
                            name), this);
                }
                return;
            }

            UnityEngine.Profiling.Profiler.BeginSample("StampHeights");
            UnityEngine.Profiling.Profiler.BeginSample("Setup");

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
            var heightArraySize = new Common.Coord(targetMaxCoord.x - targetMinCoord.x, targetMaxCoord.z - targetMinCoord.z);
            
            var stampHeight = (transform.position.y - terrainWrapper.transform.position.y)/
                              terrainWrapper.Terrain.terrainData.size.y;
            var layerHeights = layer.GetHeights(targetMinCoord.x, targetMinCoord.z, heightArraySize.x, heightArraySize.z, tRes) ??
                new Serializable2DFloatArray(heightArraySize.x, heightArraySize.z);
            
            if (layer.Stencil == null)
            {
                layer.Stencil = new Stencil(tRes, tRes);
            }
            UnityEngine.Profiling.Profiler.EndSample();

            UnityEngine.Profiling.Profiler.BeginSample("MainLoop");
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

            UnityEngine.Profiling.Profiler.EndSample();
            UnityEngine.Profiling.Profiler.EndSample();
        }

        public override void ProcessStencil(TerrainWrapper terrainWrapper, LayerBase baseLayer, int stencilKey)
        {
            if(DisableStencil)
            {
                return;
            }

            var layer = baseLayer as MMTerrainLayer;
            if(layer == null)
            {
                Debug.LogWarning(string.Format("Attempted to write {0} to incorrect layer type! Expected Layer {1} to be {2}, but it was {3}", name, baseLayer.name, GetLayerType(), baseLayer.GetType()), this);
                return;
            }

            var res = terrainWrapper.Terrain.terrainData.heightmapResolution;
            if (layer.Stencil == null || layer.Stencil.Width != res || layer.Stencil.Height != res)
            {
                layer.Stencil = new Stencil(res, res);
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
            var stampHeight = (transform.position.y - terrainWrapper.transform.position.y);

            for (var u = targetMinCoord.x; u < targetMaxCoord.x; ++u)
            {
                for (var v = targetMinCoord.z; v < targetMaxCoord.z; ++v)
                {
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
                        if (LayerHeightBlendMode == EHeightBlendMode.Max || LayerHeightBlendMode == EHeightBlendMode.Min && existingStencilStrength > 0)
                        {
                            var normalisedPos = Quaternion.Inverse(transform.rotation) * (wPos - transform.position) + Size.xz().x0z() / 2;
                            normalisedPos = new Vector3(normalisedPos.x / Size.x, normalisedPos.y / Size.y, normalisedPos.z / Size.z);
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
                                    if (predictedOutHeight >= layerHeight - tolerance)
                                    {
                                        layer.Stencil[u, v] = newStencilVal;
                                    }
                                }
                                else if (LayerHeightBlendMode == EHeightBlendMode.Min)
                                {
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

        public override void ProcessSplats(TerrainWrapper terrainWrapper, LayerBase baseLayer, int stencilKey)
        {
            if (!WriteSplats || Data.SplatData.Count == 0 || IgnoredSplats.Count == Data.SplatData.Count)
            {
                return;
            }

            var layer = baseLayer as MMTerrainLayer;
            if(layer == null)
            {
                Debug.LogWarning(string.Format("Attempted to write {0} to incorrect layer type! Expected Layer {1} to be {2}, but it was {3}", name, baseLayer.name, GetLayerType(), baseLayer.GetType()), this);
                return;
            }

            var missingWrapperCount = Data.SplatData.Count(data => data.Wrapper == null);
            if (missingWrapperCount > 0)
            {
                Debug.LogWarning(string.Format("Stamp {0} had {1} missing SplatPrototypes", name, missingWrapperCount), this);
            }

            UnityEngine.Profiling.Profiler.BeginSample("StampSplats");
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
            UnityEngine.Profiling.Profiler.BeginSample("MainLoop");
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
                    //maskPos += new Vector3((1 / (float)Data.Heights.Width) * Data.Size.x, 0, (1 / (float)Data.Heights.Height) * Data.Size.z) * 4;
                    var maskValue = GetMask().GetBilinear(Data.GridManager, maskPos);

                    if (StencilSplats)
                    {
                        var stencilPos = new Vector2(u / (float)(sRes + 1), v / (float)(sRes + 1));
                        maskValue *= layer.GetStencilStrength(stencilPos, stencilKey);
                    }

                    applyStencil[arrayU, arrayV] = maskValue > 0 ? 1 : 0;

                    normalisedStampPosition = new Vector3(normalisedStampPosition.x / Size.x, normalisedStampPosition.y / Size.y, normalisedStampPosition.z / Size.z);
                    if (normalisedStampPosition.x < 0 || normalisedStampPosition.x > 1 ||
                        normalisedStampPosition.z < 0 || normalisedStampPosition.z > 1)
                    {
                        continue;
                    }

                    int delta = 0;
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

                        byte layerValByte =  (byte)(layerData != null ? layerData[arrayU, arrayV] : 0);
                        var layerVal = layerValByte / 255f;
                            
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
                            case ESplatBlendMode.Average:
                                stampValue *= maskValue;
                                newValue = (layerVal + stampValue) / 2;
                                break;
                        }
                            
                        var byteAmount = (byte)Mathf.Clamp(newValue * 255, 0, 255);
                            
                        delta += byteAmount - layerValByte;
                        layerData[arrayU, arrayV] = byteAmount;
                    }
                    
                    if (delta != 0)
                    {
                        float floatSum = delta/255f;
                        foreach (var serializable2DByteArray in thisLayerSplatData)
                        {
                            if (!IgnoredSplats.Contains(serializable2DByteArray.Key) && Data.SplatData.Any(data => data.Wrapper == serializable2DByteArray.Key))
                            {
                                continue;
                            }

                            var readByte = serializable2DByteArray.Value[arrayU, arrayV];
                            if(readByte == 0)
                            {
                                continue;
                            }
                            var read = readByte / 255f;
                            
                            /*if(floatSum < 0)
                            {
                                var newCompoundVal = read * (1 - floatSum * -1);
                                var newCompoundByteVal = (byte)Mathf.Clamp(newCompoundVal * 255, 0, 255);
                                serializable2DByteArray.Value[arrayU, arrayV] = newCompoundByteVal;
                            }
                            else */if (floatSum < 1)
                            {
                                var newCompoundVal = read * (1 - floatSum);
                                var newCompoundByteVal = (byte)Mathf.Clamp(newCompoundVal * 255, 0, 255);
                                serializable2DByteArray.Value[arrayU, arrayV] = newCompoundByteVal;
                            }
                            else
                            {
                                serializable2DByteArray.Value[arrayU, arrayV] = 0;
                            }
                            //Debug.Log(string.Format("Wrapper: {3} \t Delta: {0} \t Read: {1} \t New : {2}", floatSum, read, serializable2DByteArray.Value[arrayU, arrayV] / 255f, serializable2DByteArray.Key));
                        }
                        layer.Stencil[u, v] = MiscUtilities.CompressStencil(stencilKey, 1);
                    }
                }
            }
            
            foreach (var pair in thisLayerSplatData)
            {
                layer.SetSplatmap(pair.Key, targetMinCoord.x, targetMinCoord.z, pair.Value, sRes, applyStencil);
            }

            //MiscUtilities.AbsStencil(layer.Stencil, stencilKey);
            UnityEngine.Profiling.Profiler.EndSample();
            UnityEngine.Profiling.Profiler.EndSample();
        }

        public override void ProcessDetails(TerrainWrapper terrainWrapper, LayerBase baseLayer, int stencilKey)
        {
            if (!WriteDetails&& !RemoveExistingDetails)
            {
                return;
            }

            var layer = baseLayer as MMTerrainLayer;
            if(layer == null)
            {
                Debug.LogWarning(string.Format("Attempted to write {0} to incorrect layer type! Expected Layer {1} to be {2}, but it was {3}", name, baseLayer.name, GetLayerType(), baseLayer.GetType()), this);
                return;
            }

            var missingWrapperCount = Data.DetailData.Count(data => data.Wrapper == null);
            if (missingWrapperCount > 0)
            {
                Debug.LogWarning(string.Format("Stamp {0} had {1} missing DetailPrototypes", name, missingWrapperCount), this);
            }

            UnityEngine.Profiling.Profiler.BeginSample("StampDetails");
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

                    var stencilValue = layer.GetStencilStrength(new Vector2(uF, vF), stencilKey);
                    if (stencilValue <= 0)
                    {
                        continue;
                    }

                    var wPos = terrain.DetailCoordToWorldPos(new Common.Coord(u, v));
                    var normalisedStampPosition = Quaternion.Inverse(transform.rotation) * (wPos - transform.position) + Size.xz().x0z() / 2;
                    normalisedStampPosition = new Vector3(normalisedStampPosition.x / Size.x, normalisedStampPosition.y / Size.y, normalisedStampPosition.z / Size.z);
                    if (normalisedStampPosition.x < 0 || normalisedStampPosition.x > 1 ||
                        normalisedStampPosition.z < 0 || normalisedStampPosition.z > 1)
                    {
                        continue;
                    }

                    if(RemoveExistingDetails)
                    {
                        foreach (var valuePair in allDetails)
                        {
                            float floatStrength = valuePair.Value[arrayU, arrayV];
                            floatStrength *= (1-stencilValue);
                            byte byteStrength = (byte)Mathf.Clamp(Mathf.RoundToInt(floatStrength), 0, 255);
                            valuePair.Value[arrayU, arrayV] = byteStrength;
                        }
                    }                  
                    
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

                    /*foreach (var pair in allDetails)
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
                    }*/
                }
            }
            foreach (var prototypeWrapper in allDetails)
            {
                layer.SetDetailMap(prototypeWrapper.Key, targetMinCoord.x, targetMinCoord.z, prototypeWrapper.Value, dRes);
            }

            UnityEngine.Profiling.Profiler.EndSample();
        }

        public override void ProcessTrees(TerrainWrapper terrainWrapper, LayerBase baseLayer, int stencilKey)
        {
            var layer = baseLayer as MMTerrainLayer;
            if(layer == null)
            {
                Debug.LogWarning(string.Format("Attempted to write {0} to incorrect layer type! Expected Layer {1} to be {2}, but it was {3}", name, baseLayer.name, GetLayerType(), baseLayer.GetType()), this);
                return;
            }

            UnityEngine.Profiling.Profiler.BeginSample("StampTrees");
            // Stamp trees
            var tSize = terrainWrapper.Terrain.terrainData.size;
            var tPos = terrainWrapper.transform.position;

            var wrapperBounds =
                new Bounds(terrainWrapper.Terrain.GetPosition() + terrainWrapper.Terrain.terrainData.size / 2,
                    terrainWrapper.Terrain.terrainData.size);
            wrapperBounds.Expand(Vector3.up * 5000);
            if (RemoveTrees)
            {
                var stampBounds = new ObjectBounds(transform.position, Size / 2, transform.rotation);
                stampBounds.Expand(Vector3.up * 5000);
                List<MadMapsTreeInstance> compoundTrees = terrainWrapper.GetCompoundTrees(layer, true);
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
                UnityEngine.Profiling.Profiler.EndSample();
                return;
            }

            for (var i = 0; i < Data.Trees.Count; i++)
            {
                if(IgnoredTrees.Contains(Data.Trees[i].Prototype))
                {
                    continue;
                }
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

            UnityEngine.Profiling.Profiler.EndSample();
        }

        public override void ProcessObjects(TerrainWrapper terrainWrapper, LayerBase baseLayer, int stencilKey)
        {
            var layer = baseLayer as MMTerrainLayer;
            if(layer == null)
            {
                Debug.LogWarning(string.Format("Attempted to write {0} to incorrect layer type! Expected Layer {1} to be {2}, but it was {3}", name, baseLayer.name, GetLayerType(), baseLayer.GetType()), this);
                return;
            }

            UnityEngine.Profiling.Profiler.BeginSample("StampObjects");
            // Stamp objects
            var t = terrainWrapper.Terrain;
            var tSize = t.terrainData.size;
            var tPos = t.transform.position;

            if (RemoveObjects)
            {
                var stampBounds = new ObjectBounds(transform.position, Size / 2, transform.rotation);
                stampBounds.Expand(Vector3.up * 5000);
                var compoundObjects = terrainWrapper.GetCompoundObjects(layer, true);
                foreach (var prefabObjectData in compoundObjects)
                {                    
                    var pos = prefabObjectData.Position;
                    //pos = Quaternion.Inverse(transform.rotation) * (pos - transform.position);
                    pos = new Vector3(pos.x * tSize.x, pos.y, pos.z * tSize.z) + t.GetPosition();
                    
                    var wPos = pos;
                    if (stampBounds.Contains(wPos))
                    {
                        var stencilPos = wPos - tPos;
                        stencilPos = new Vector2(stencilPos.x / tSize.x, stencilPos.z / tSize.z);
                        var stencilAmount = layer.GetStencilStrength(stencilPos, stencilKey);
                        if (stencilAmount > 0.5f && !prefabObjectData.Prefab.GetComponent<WorldStamp>())
                        {
                            layer.ObjectRemovals.Add(prefabObjectData.Guid);
                            //Debug.DrawLine(wPos, wPos + Vector3.up * stencilAmount * 20, Color.red, 30);
                        }
                    }
                }
            }

            if (!WriteObjects)
            {
                UnityEngine.Profiling.Profiler.EndSample();
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

                //if (OverrideObjectRelativeMode)
                {
                    float heightVal;
                    if (HaveHeightsBeenFlipped)
                    {
                        //prefabObjectData.Position.y += Data.Heights.BilinearSample(new Vector2(oldPos.x, oldPos.z)) * Data.Size.y;
                        heightVal = Data.Heights.BilinearSample(new Vector2(oldPos.x, oldPos.z)) * Size.y;
                    }
                    else
                    {
                        //prefabObjectData.Position.y += Data.Heights.BilinearSample(new Vector2(oldPos.z, oldPos.x)) * Data.Size.y;
                        heightVal = Data.Heights.BilinearSample(new Vector2(oldPos.z, oldPos.x)) * Size.y;
                    }

                    if (RelativeMode == EObjectRelativeMode.RelativeToStamp && !prefabObjectData.AbsoluteHeight)
                    {
                        prefabObjectData.Position.y += heightVal;
                        prefabObjectData.Position.y += Data.ZeroLevel * Size.y;
                    }
                    if (RelativeMode == EObjectRelativeMode.RelativeToTerrain && prefabObjectData.AbsoluteHeight)
                    {
                        prefabObjectData.Position.y -= heightVal;
                        prefabObjectData.Position.y -= Data.ZeroLevel * Size.y;
                    }

                    prefabObjectData.AbsoluteHeight = RelativeMode == EObjectRelativeMode.RelativeToStamp;
                }

                if (prefabObjectData.AbsoluteHeight)
                {
                    //if(!OverrideObjectRelativeMode)
                    prefabObjectData.Position.y -= Data.ZeroLevel * Size.y;
                    prefabObjectData.Position.y += transform.position.y;
                    prefabObjectData.Position.y -= tPos.y;
                }

                if (prefabObjectData.Scale.x < 0 || prefabObjectData.Scale.y < 0 || prefabObjectData.Scale.z < 0)
                {
                    Debug.LogWarning(string.Format("Stamp {0} has created an object ({1}) with negative scale. This can cause performance issues if you do this lots! Select the stamp prefab to resolve this.", name, prefabObjectData.Prefab.name), this);
                }

                prefabObjectData.ContainerMetadata = string.Format("{0}/{1}", layer.name, name);

                layer.Objects.Add(prefabObjectData);
            }
            UnityEngine.Profiling.Profiler.EndSample();
        }
        
        

        public void OnDestroy()
        {
            if (_preview != null)
            {
                _preview.Dispose();
                _preview = null;
            }
        }

        public override int GetPriority()
        {
            return Priority;
        }

        public override string GetLayerName()
        {
            return LayerName;
        }

        public override void SetPriority(int priority)
        {
            Priority = priority;
        }

        public void Validate()
        {
            if (Data.Heights == null || Data.Heights.IsEmpty())
            {
                WriteHeights = false;
            }
            if (Data.Trees.Count == 0)
            {
                WriteTrees = false;
            }
            if (Data.Objects.Count == 0)
            {
                WriteObjects = false;
            }
            if (Data.SplatData.Count == 0)
            {
                WriteSplats = false;
            }
            if (Data.DetailData.Count == 0)
            {
                WriteDetails = false;
            }
            #if VEGETATION_STUDIO
            if (Data.VSData.Count == 0)
            {
                VegetationStudioEnabled = false;
            }
            #endif
        }
    }
}