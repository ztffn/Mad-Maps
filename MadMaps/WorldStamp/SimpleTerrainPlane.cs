﻿using System;
using MadMaps.Common;
using MadMaps.Common.Collections;
using MadMaps.Common.GenericEditor;
using MadMaps.Roads;
using MadMaps.Terrains;
using UnityEngine;
using UnityEngine.Serialization;

namespace MadMaps.WorldStamps
{
    [ExecuteInEditMode]
#if HURTWORLDSDK
    [StripComponentOnBuild]
#endif
    public class SimpleTerrainPlane : NodeComponent
    {
        public enum BoundsFalloffMode
        {
            Rectangular,
            Circular,
            Texture,
            None
        }

        public enum HeightBlendMode
        {
            Set,
            Max,
            Min,
            Average
        }

        public HeightBlendMode BlendMode;
        public AnimationCurve Falloff;
        public Texture2D FalloffTexture;
        public BoundsFalloffMode FalloffMode;
        public Vector3 Offset;

        public bool RemoveObjects = true;
        public bool RemoveTrees = true;
        public bool RemoveGrass = true;
        public bool SetSplat = false;
        public SplatPrototypeWrapper Splat;
        [Range(0, 1)]
        public float SplatStrength = 1;
        [FormerlySerializedAs("Size")]
        public Vector2 AreaSize;
        
        public override Vector3 Size
        {
            get
            {
                return GetObjectBounds().size;
            }
        }

        public override void ProcessSplats(TerrainWrapper terrainWrapper, LayerBase baseLayer, int stencilKey)
        {
            var layer = baseLayer as TerrainLayer;
            if(layer == null)
            {
                Debug.LogWarning(string.Format("Attempted to write {0} to incorrect layer type! Expected Layer {1} to be {2}, but it was {3}", name, baseLayer.name, GetLayerType(), baseLayer.GetType()), this);
                return;
            }
            if(!SetSplat || Splat == null || SplatStrength == 0)
            {
                return;
            }
            stencilKey = GetPriority();
            var objectBounds = GetObjectBounds();
            var aRes = terrainWrapper.Terrain.terrainData.alphamapResolution;
            var axisBounds = objectBounds.Flatten().ToAxisBounds();

            var matrixMin = terrainWrapper.Terrain.WorldToSplatCoord(axisBounds.min);
            matrixMin = new Common.Coord(Mathf.Clamp(matrixMin.x, 0, aRes), Mathf.Clamp(matrixMin.z, 0, aRes));

            var matrixMax = terrainWrapper.Terrain.WorldToSplatCoord(axisBounds.max);
            matrixMax = new Common.Coord(Mathf.Clamp(matrixMax.x, 0, aRes), Mathf.Clamp(matrixMax.z, 0, aRes));

            var arraySize = new Common.Coord(matrixMax.x - matrixMin.x, matrixMax.z - matrixMin.z);
            
            var existingSplats = layer.GetSplatMaps(matrixMin.x, matrixMin.z, arraySize.x, arraySize.z, aRes);
            for (var dx = 0; dx < arraySize.x; ++dx)
            {
                var xF = dx/(float) arraySize.x;
                for (var dz = 0; dz < arraySize.z; ++dz)
                {
                    var zF = dz / (float)arraySize.z;
                    var falloff = GetFalloff(new Vector2(xF, zF));
                    
                    float splatStrength = falloff * SplatStrength;
                    if(!existingSplats.ContainsKey(Splat))
                    {
                        existingSplats.Add(Splat, new Serializable2DByteArray(arraySize.x, arraySize.z));
                    }
                    var existingbaseVal = existingSplats[Splat][dx,dz] / 255f;
                    var newBaseVal = Mathf.Max(existingbaseVal, splatStrength);
                    var writeBaseValue = (byte)Mathf.Clamp(newBaseVal * 255, 0, 255);
                    existingSplats[Splat][dx,dz] = writeBaseValue;

                    foreach (var serializable2DByteArray in existingSplats)
                    {
                        if(serializable2DByteArray.Key == Splat)
                        {
                            continue;
                        }

                        var readValue = serializable2DByteArray.Value[dx, dz] / 255f;
                        var newValue = readValue * (1 - newBaseVal);
                        var writeValue = (byte)Mathf.Clamp(newValue * 255, 0, 255);
                        serializable2DByteArray.Value[dx, dz] = writeValue;
                    }
                }
            }
            foreach (var serializable2DByteArray in existingSplats)
            {
                layer.SetSplatmap(serializable2DByteArray.Key, matrixMin.x, matrixMin.z, serializable2DByteArray.Value, aRes);
            }
        }

        public override void ProcessDetails(TerrainWrapper terrainWrapper, LayerBase baseLayer, int stencilKey)
        {
            var layer = baseLayer as TerrainLayer;
            if(layer == null)
            {
                Debug.LogWarning(string.Format("Attempted to write {0} to incorrect layer type! Expected Layer {1} to be {2}, but it was {3}", name, baseLayer.name, GetLayerType(), baseLayer.GetType()), this);
                return;
            }
            stencilKey = GetPriority();
            var objectBounds = GetObjectBounds();            
            var dRes = terrainWrapper.Terrain.terrainData.detailResolution;
            var axisBounds = objectBounds.Flatten().ToAxisBounds();

            var matrixMin = terrainWrapper.Terrain.WorldToDetailCoord(axisBounds.min);
            matrixMin = new Common.Coord(Mathf.Clamp(matrixMin.x, 0, dRes), Mathf.Clamp(matrixMin.z, 0, dRes));

            var matrixMax = terrainWrapper.Terrain.WorldToDetailCoord(axisBounds.max);
            matrixMax = new Common.Coord(Mathf.Clamp(matrixMax.x, 0, dRes), Mathf.Clamp(matrixMax.z, 0, dRes));

            var arraySize = new Common.Coord(matrixMax.x - matrixMin.x, matrixMax.z - matrixMin.z);

            var details = layer.GetDetailMaps(matrixMin.x, matrixMin.z, arraySize.x, arraySize.z, dRes);
            for (var dx = 0; dx < arraySize.x; ++dx)
            {
                var xF = dx/(float) arraySize.x;
                for (var dz = 0; dz < arraySize.z; ++dz)
                {
                    var zF = dz / (float)arraySize.z;
                    var falloff = GetFalloff(new Vector2(xF, zF));
                    foreach (var serializable2DByteArray in details)
                    {
                        var readValue = serializable2DByteArray.Value[dx, dz] / 255f;
                        var newValue = readValue*(1 - falloff);
                        var writeValue = (byte)Mathf.Clamp(newValue * 255, 0, 255);
                        serializable2DByteArray.Value[dx, dz] = writeValue;
                    }
                }
            }
            foreach (var serializable2DByteArray in details)
            {
                layer.SetDetailMap(serializable2DByteArray.Key, matrixMin.x, matrixMin.z, serializable2DByteArray.Value, dRes);
            }
        }

        float GetFalloff(Vector2 normalizedPos)
        {
            if (normalizedPos.x < 0 || normalizedPos.x > 1 || normalizedPos.y < 0 || normalizedPos.y > 1)
            {
                return 0;
            }

            if (FalloffMode == BoundsFalloffMode.Rectangular)
            {
                var centeredPos = (normalizedPos - Vector2.one*.5f).Abs() * 2;
                return Falloff.Evaluate(centeredPos.x) * Falloff.Evaluate(centeredPos.y);
            }
            if (FalloffMode == BoundsFalloffMode.Circular)
            {
                normalizedPos *= 2;
                normalizedPos -= new Vector2(1, 1);
                return Falloff.Evaluate(normalizedPos.magnitude);
            }
            if (FalloffMode == BoundsFalloffMode.Texture)
            {
                if (FalloffTexture)
                {
                    return FalloffTexture.GetPixelBilinear(normalizedPos.x, normalizedPos.y).grayscale;
                }
            }
            return 1;
        }

        public override void ProcessTrees(TerrainWrapper terrainWrapper, LayerBase baseLayer, int stencilKey)
        {
            var layer = baseLayer as TerrainLayer;
            if(layer == null)
            {
                Debug.LogWarning(string.Format("Attempted to write {0} to incorrect layer type! Expected Layer {1} to be {2}, but it was {3}", name, baseLayer.name, GetLayerType(), baseLayer.GetType()), this);
                return;
            }
            var objectBounds = GetObjectBounds();
            stencilKey = GetPriority();
            var trees = terrainWrapper.GetCompoundTrees(layer, true);
            objectBounds = new ObjectBounds(objectBounds.center,
                new Vector3(objectBounds.extents.x, 5000,
                    objectBounds.extents.z), objectBounds.Rotation);
            foreach (var hurtTreeInstance in trees)
            {
                var wPos = terrainWrapper.Terrain.TreeToWorldPos(hurtTreeInstance.Position);
                if (!objectBounds.Contains(wPos))
                {
                    continue;
                }
                Vector2 localPos = transform.worldToLocalMatrix.MultiplyPoint3x4(wPos).xz() + AreaSize/2 - Offset.xz();
                localPos = new Vector2(localPos.x / Size.x, localPos.y / Size.y);
                var falloff = GetFalloff(localPos);
                if (falloff > .5f)
                {
                    layer.TreeRemovals.Add(hurtTreeInstance.Guid);
                }
            }
        }

        public override void ProcessObjects(TerrainWrapper terrainWrapper, LayerBase baseLayer, int stencilKey)
        {
            var layer = baseLayer as TerrainLayer;
            if(layer == null)
            {
                Debug.LogWarning(string.Format("Attempted to write {0} to incorrect layer type! Expected Layer {1} to be {2}, but it was {3}", name, baseLayer.name, GetLayerType(), baseLayer.GetType()), this);
                return;
            }

            var objectBounds = GetObjectBounds();
            stencilKey = GetPriority();
            var objects = terrainWrapper.GetCompoundObjects(layer);
            objectBounds = new ObjectBounds(objectBounds.center, new Vector3(objectBounds.extents.x, 5000, objectBounds.extents.z), objectBounds.Rotation);
            foreach (var prefabObjectData in objects)
            {
                var wPos = terrainWrapper.Terrain.TreeToWorldPos(prefabObjectData.Position);
                if (!objectBounds.Contains(wPos))
                {
                    continue;
                }
                Vector2 localPos = transform.worldToLocalMatrix.MultiplyPoint3x4(wPos).xz() + AreaSize / 2 - Offset.xz();
                localPos = new Vector2(localPos.x / Size.x, localPos.y / Size.y);
                var falloff = GetFalloff(localPos);
                if (falloff > .5f)
                {
                    layer.ObjectRemovals.Add(prefabObjectData.Guid);
                }
            }
        }

        public override void ProcessHeights(TerrainWrapper terrainWrapper, LayerBase baseLayer, int stencilKey)
        {
            var layer = baseLayer as TerrainLayer;
            if(layer == null)
            {
                Debug.LogWarning(string.Format("Attempted to write {0} to incorrect layer type! Expected Layer {1} to be {2}, but it was {3}", name, baseLayer.name, GetLayerType(), baseLayer.GetType()), this);
                return;
            }
            stencilKey = GetPriority();
            var objectBounds = GetObjectBounds();
            var flatObjBounds = objectBounds.Flatten();
            var flatBounds = flatObjBounds.ToAxisBounds();
            var terrainSize = terrainWrapper.Terrain.terrainData.size;
            var hRes = terrainWrapper.Terrain.terrainData.heightmapResolution;

            var matrixMin = terrainWrapper.Terrain.WorldToHeightmapCoord(flatBounds.min,
                TerrainX.RoundType.Floor);
            var matrixMax = terrainWrapper.Terrain.WorldToHeightmapCoord(flatBounds.max,
                TerrainX.RoundType.Ceil);
            matrixMin = new Common.Coord(Mathf.Clamp(matrixMin.x, 0, hRes), Mathf.Clamp(matrixMin.z, 0, hRes));
            matrixMax = new Common.Coord(Mathf.Clamp(matrixMax.x, 0, hRes), Mathf.Clamp(matrixMax.z, 0, hRes));

            var floatArraySize = new Common.Coord(matrixMax.x - matrixMin.x, matrixMax.z - matrixMin.z);
            
            
            layer.BlendMode = TerrainLayer.ETerrainLayerBlendMode.Stencil;
            
            if (layer.Stencil == null || layer.Stencil.Width != hRes || layer.Stencil.Height != hRes)
            {
                layer.Stencil = new Stencil(hRes, hRes);
            }
            var layerHeights = layer.GetHeights(matrixMin.x, matrixMin.z, floatArraySize.x, floatArraySize.z, hRes) ??
                               new Serializable2DFloatArray(floatArraySize.x, floatArraySize.z);

            var objectBoundsPlane = new Plane((objectBounds.Rotation*Vector3.up).normalized, objectBounds.center);
            for (var dz = 0; dz < floatArraySize.z; ++dz)
            {
                for (var dx = 0; dx < floatArraySize.x; ++dx)
                {
                    var coordX = matrixMin.x + dx;
                    var coordZ = matrixMin.z + dz;

                    int existingStencilKey;
                    float existingStencilVal;
                    MiscUtilities.DecompressStencil(layer.Stencil[coordX, coordZ], out existingStencilKey,
                        out existingStencilVal);

                    var worldPos = terrainWrapper.Terrain.HeightmapCoordToWorldPos(new Common.Coord(coordX, coordZ));
                    worldPos = new Vector3(worldPos.x, objectBounds.center.y, worldPos.z);
                    if (!flatObjBounds.Contains(new Vector3(worldPos.x, flatObjBounds.center.y, worldPos.z)))
                    {
                        continue;
                    }
                    
                    var localPos = Quaternion.Inverse(objectBounds.Rotation)*(worldPos - objectBounds.min);
                    var xDist = localPos.x/objectBounds.size.x;
                    var zDist = localPos.z/objectBounds.size.z;

                    float falloff = GetFalloff(new Vector2(xDist, zDist));
                    if (Mathf.Approximately(falloff, 0))
                    {
                        continue;
                    }

                    var planeRay = new Ray(worldPos, Vector3.up);
                    float dist;

                    objectBoundsPlane.Raycast(planeRay, out dist);

                    var heightAtPoint = (planeRay.GetPoint(dist) - terrainWrapper.transform.position).y/terrainSize.y;
                    var blendedHeight = heightAtPoint;

                    var existingHeight = layerHeights[dx, dz];

                    switch (BlendMode)
                    {
                        case HeightBlendMode.Set:
                            blendedHeight = Mathf.Lerp(existingHeight, blendedHeight, falloff);
                            break;
                        case HeightBlendMode.Max:
                            blendedHeight = Mathf.Max(existingHeight, blendedHeight);
                            break;
                        case HeightBlendMode.Min:
                            blendedHeight = Mathf.Min(existingHeight, blendedHeight);
                            break;
                        case HeightBlendMode.Average:
                            blendedHeight = (existingHeight + blendedHeight)/2;
                            break;
                    }

                    layer.Stencil[matrixMin.x + dx, matrixMin.z + dz] =
                        MiscUtilities.CompressStencil(stencilKey, 1);
                    layerHeights[dx, dz] = blendedHeight;
                }
            }

            layer.SetHeights(matrixMin.x, matrixMin.z, layerHeights, hRes);
        }

        private Vector3 GetScaledSize()
        {
            try{
                return new Vector3(transform.lossyScale.x*AreaSize.x, 0, transform.lossyScale.z*AreaSize.y);
            }
            catch(Exception e)
            {
                Debug.LogException(e);
            }
            return Vector3.one;
        }

        public void OnDrawGizmosSelected()
        {
            var pos = transform.position;
            var rot = transform.rotation;
            GizmoExtensions.DrawWireCube(pos + rot*Offset, GetScaledSize(), rot, Color.white);
        }

        private ObjectBounds GetObjectBounds()
        {
            return new ObjectBounds(transform.position + transform.rotation*Offset,
                GetScaledSize(), transform.rotation);
        }
    }
}