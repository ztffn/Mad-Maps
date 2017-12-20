﻿using Dingo.Common;
using Dingo.Common.Collections;
using Dingo.Roads;
using Dingo.Terrains;
using UnityEngine;

namespace Dingo.WorldStamp
{
    [ExecuteInEditMode]
#if HURTWORLDSDK
    [StripComponentOnBuild]
#endif
    public class SimpleTerrainPlane : NodeComponent, IOnBakeCallback
    {
        public enum BoundsFalloffMode
        {
            Rectangular,
            Circular,
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
        public BoundsFalloffMode FalloffMode;
        public string LayerName = "sRoad";
        public Vector3 Offset;

        public bool RemoveObjects = true;
        public bool RemoveTrees = true;
        public bool RemoveGrass = true;
        public Vector2 Size;
        public int Priority;

        public void OnBake()
        {
            var objectBounds = GetObjectBounds();
            var terrainWrappers = TerrainLayerUtilities.CollectWrappers(objectBounds);
            var stencilKey = GetPriority();

            foreach (var terrainWrapper in terrainWrappers)
            {
                ProcessHeight(objectBounds, stencilKey, terrainWrapper);
                if (RemoveTrees)
                {
                    ProcessTrees(objectBounds, terrainWrapper);
                }
                if (RemoveObjects)
                {
                    ProcessObjects(objectBounds, terrainWrapper);
                }
                if (RemoveGrass)
                {
                    ProcessGrass(objectBounds, terrainWrapper);
                }
            }
        }

        private void ProcessGrass(ObjectBounds objectBounds, TerrainWrapper terrainWrapper)
        {
            var layer = terrainWrapper.GetLayer<TerrainLayer>(LayerName, true, true);
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
                Debug.LogError("BoundsFalloffMode.Circular not currently implemented");
                //var falloff = 1;
                /*var theta = Vector3.Angle(Vector3.forward, localPos.Flatten()) * Mathf.Deg2Rad;
                var objRadius = objectBounds.size / 2;
                var sin = Mathf.Sin(theta);
                var cos = Mathf.Cos(theta);
                var circRad = Mathf.Abs((objRadius.x * objRadius.x) /
                                        Mathf.Sqrt(objRadius.x * objRadius.x * cos * cos +
                                                   objRadius.z * objRadius.z * sin * sin));
                var circDist = Mathf.Clamp01(localPos.magnitude / circRad);
                falloff = Falloff.Evaluate(circDist) * Falloff.Evaluate(circDist);*/
            }
            return 1;
        }

        private void ProcessTrees(ObjectBounds objectBounds, TerrainWrapper terrainWrapper)
        {
            var layer = terrainWrapper.GetLayer<TerrainLayer>(LayerName, true, true);
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
                Vector2 localPos = transform.worldToLocalMatrix.MultiplyPoint3x4(wPos).xz() + Size/2 - Offset.xz();
                localPos = new Vector2(localPos.x / Size.x, localPos.y / Size.y);
                var falloff = GetFalloff(localPos);
                if (falloff > .5f)
                {
                    layer.TreeRemovals.Add(hurtTreeInstance.Guid);
                }
            }
        }

        private void ProcessObjects(ObjectBounds objectBounds, TerrainWrapper terrainWrapper)
        {
            var layer = terrainWrapper.GetLayer<TerrainLayer>(LayerName, true, true);
            var objects = terrainWrapper.GetCompoundObjects(layer);
            objectBounds = new ObjectBounds(objectBounds.center, new Vector3(objectBounds.extents.x, 5000, objectBounds.extents.z), objectBounds.Rotation);
            foreach (var prefabObjectData in objects)
            {
                var wPos = terrainWrapper.Terrain.TreeToWorldPos(prefabObjectData.Position);
                if (!objectBounds.Contains(wPos))
                {
                    continue;
                }
                Vector2 localPos = transform.worldToLocalMatrix.MultiplyPoint3x4(wPos).xz() + Size / 2 - Offset.xz();
                localPos = new Vector2(localPos.x / Size.x, localPos.y / Size.y);
                var falloff = GetFalloff(localPos);
                if (falloff > .5f)
                {
                    layer.ObjectRemovals.Add(prefabObjectData.Guid);
                }
            }
        }

        private void ProcessHeight(ObjectBounds objectBounds, int stencilKey, TerrainWrapper terrainWrapper)
        {
            var flatObjBounds = objectBounds.Flatten();
            var flatBounds = flatObjBounds.ToAxisBounds();
            var matrixMin = terrainWrapper.Terrain.WorldToHeightmapCoord(flatBounds.min,
                TerrainX.RoundType.Floor);
            var matrixMax = terrainWrapper.Terrain.WorldToHeightmapCoord(flatBounds.max,
                TerrainX.RoundType.Ceil);
            var floatArraySize = new Common.Coord(matrixMax.x - matrixMin.x, matrixMax.z - matrixMin.z);
            var terrainSize = terrainWrapper.Terrain.terrainData.size;
            var hRes = terrainWrapper.Terrain.terrainData.heightmapResolution;
            var layer = terrainWrapper.GetLayer<TerrainLayer>(LayerName, true, true);

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

                    var planeRay = new Ray(worldPos, Vector3.up);
                    float dist;

                    objectBoundsPlane.Raycast(planeRay, out dist);

                    var heightAtPoint = (planeRay.GetPoint(dist) - terrainWrapper.transform.position).y/terrainSize.y;
                    var blendedHeight = heightAtPoint;

                    var existingHeight = layerHeights[dx, dz];

                    switch (BlendMode)
                    {
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
                        MiscUtilities.CompressStencil(falloff > existingStencilVal ? stencilKey : existingStencilKey,
                            falloff + existingStencilVal);
                    layerHeights[dx, dz] = blendedHeight;
                }
            }

            layer.SetHeights(matrixMin.x, matrixMin.z, layerHeights, hRes);
        }

        public int GetPriority()
        {
            return Priority;
        }

        private Vector3 GetScaledSize()
        {
            return new Vector3(transform.lossyScale.x*Size.x, 0, transform.lossyScale.z*Size.y);
        }

        public void OnDrawGizmosSelected()
        {
#if UNITY_EDITOR && HURTWORLDSDK
            var pos = transform.position;
            var rot = transform.rotation;
            GizmoExtensions.DrawWireCube(pos + rot*Offset, GetScaledSize(), rot, Color.white);
#endif
        }

        private ObjectBounds GetObjectBounds()
        {
            return new ObjectBounds(transform.position + transform.rotation*Offset,
                GetScaledSize(), transform.rotation);
        }
    }
}