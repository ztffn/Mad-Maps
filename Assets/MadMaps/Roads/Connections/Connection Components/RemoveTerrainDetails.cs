using System;
using MadMaps.Common;
using MadMaps.Common.Collections;
using MadMaps.Common.GenericEditor;
using MadMaps.Terrains;

using UnityEngine;

namespace MadMaps.Roads.Connections
{
    public class RemoveTerrainDetails : ConnectionComponent
    {
        [Name("Terrain/Remove Details")]
        public class Config : ConnectionConfigurationBase
        {
            public float Radius = 10;
            public AnimationCurve GrassFalloff = new AnimationCurve(new Keyframe(0, 1), new Keyframe(0.5f, 1), new Keyframe(1, 0));

            public override Type GetMonoType()
            {
                return typeof(RemoveTerrainDetails);
            }
        }

        public override void ProcessDetails(TerrainWrapper wrapper, LayerBase baseLayer, int stencilKey)
        {
            var layer = baseLayer as MMTerrainLayer;
            if(layer == null)
            {
                Debug.LogWarning(string.Format("Attempted to write {0} to incorrect layer type! Expected Layer {1} to be {2}, but it was {3}", name, baseLayer.name, GetLayerType(), baseLayer.GetType()), this);
                return;
            }
            if (!Network || Configuration == null)
            {
                return;
            }
            var config = Configuration.GetConfig<Config>();
            if (config == null)
            {
                Debug.LogError("Invalid configuration! Expected ConnectionTerrainHeightConfiguration");
                return;
            }
            
            var terrain = wrapper.Terrain;
            var dRes = terrain.terrainData.detailResolution;
            var mainSpline = NodeConnection.GetSpline();
            var radius = config.Radius;
            var falloffCurve = config.GrassFalloff;

            // Create bounds to encapsulate spline (axis aligned)
            var bounds = mainSpline.GetApproximateBounds();
            bounds.Expand(radius * 2 * Vector3.one);

            // Create object bounds
            var objectBounds = mainSpline.GetApproximateXZObjectBounds();
            objectBounds.Expand(radius * 2 * Vector3.one);
            objectBounds.Expand(Vector3.up * 10000);

            // Early cull
            var axisBounds = objectBounds.ToAxisBounds();
            var terrainBounds = terrain.GetComponent<Collider>().bounds;
            if (!terrainBounds.Intersects(axisBounds))
            {
                return;
            }

            // Get matrix space min/max
            var matrixMin = terrain.WorldToDetailCoord(bounds.min);
            matrixMin = new Common.Coord(Mathf.Clamp(matrixMin.x, 0, dRes), Mathf.Clamp(matrixMin.z, 0, dRes));
            var matrixMax = terrain.WorldToDetailCoord(bounds.max);
            matrixMax = new Common.Coord(Mathf.Clamp(matrixMax.x, 0, dRes), Mathf.Clamp(matrixMax.z, 0, dRes));

            var floatArraySize = new Common.Coord(matrixMax.x - matrixMin.x, matrixMax.z - matrixMin.z);
            var layerDetails = layer.GetDetailMaps(matrixMin.x, matrixMin.z, floatArraySize.x, floatArraySize.z, dRes);

            var writeStencil = new Serializable2DFloatArray(floatArraySize.x, floatArraySize.z);
            for (var dx = 0; dx < floatArraySize.x; ++dx)
            {
                for (var dz = 0; dz < floatArraySize.z; ++dz)
                {
                    var coordX = matrixMin.x + dx;
                    var coordZ = matrixMin.z + dz;

                    var worldPos = terrain.DetailCoordToWorldPos(new Common.Coord(coordX, coordZ));
                    worldPos = new Vector3(worldPos.x, objectBounds.center.y, worldPos.z);

                    if (!terrain.ContainsPointXZ(worldPos) || !objectBounds.Contains(worldPos))
                    {
                        continue;
                    }

                    var uniformT = mainSpline.GetClosestUniformTimeOnSplineXZ(worldPos.xz()); // Expensive!
                    var closestOnSpline = mainSpline.GetUniformPointOnSpline(uniformT);
                    
                    var normalizedFlatDistToSpline = (worldPos - closestOnSpline).xz().magnitude / radius;
                    var maskValue = Mathf.Clamp01(falloffCurve.Evaluate(normalizedFlatDistToSpline));
                    if (maskValue <= 0 || normalizedFlatDistToSpline < 0 || normalizedFlatDistToSpline > 1)
                    {
                        //DebugHelper.DrawPoint(worldPos, 1, Color.yellow, 20);
                        continue;
                    }

                    //DebugHelper.DrawPoint(worldPos, .2f, Color.green, 20);
                    //Debug.DrawLine(worldPos, worldPos + Vector3.up * maskValue, Color.green, 20);
                    writeStencil[dx, dz] = 1;
                    layer.Stencil[coordX, coordZ] = MiscUtilities.CompressStencil(GetPriority(), 1);
                    foreach (var data in layerDetails)
                    {
                        float readValue = data.Value[dx, dz];
                        readValue /= 16;

                        var writeValue = readValue * (1 - maskValue);
                        var writeByteValue = (byte)Mathf.Clamp(writeValue * 16, 0, 16);
                        data.Value[dx, dz] = writeByteValue;
                    }
                }
            }

            foreach (var serializable2DByteArray in layerDetails)
            {
                layer.SetDetailMap(serializable2DByteArray.Key, matrixMin.x, matrixMin.z, serializable2DByteArray.Value, dRes, writeStencil);
            }
        }
        
    }
}