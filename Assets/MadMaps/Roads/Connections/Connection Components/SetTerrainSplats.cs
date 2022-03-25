using System;
using System.Collections.Generic;
using MadMaps.Common;
using MadMaps.Common.Collections;
using MadMaps.Common.GenericEditor;
using MadMaps.Terrains;

using UnityEngine;

namespace MadMaps.Roads.Connections
{
    public class SetTerrainSplats : ConnectionComponent
    {
        [Name("Terrain/Set Splats")]
        public class Config : ConnectionConfigurationBase
        {
            [Serializable]
            [Name("Splat Configuration")]
            public class SplatConfig
            {
                public SplatPrototypeWrapper SplatPrototype;
                public float SplatStrength = 1;
            }

            public float Radius = 10;
            public AnimationCurve SplatFalloff = new AnimationCurve(new Keyframe(0, 1), new Keyframe(0.5f, 1), new Keyframe(1, 0));
            public List<SplatConfig> SplatConfigurations = new List<SplatConfig>();

            public override Type GetMonoType()
            {
                return typeof(SetTerrainSplats);
            }
        }

        public const int SplatOffset = 2;

        public override void ProcessSplats(TerrainWrapper wrapper, LayerBase baseLayer, int stencilKey)
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
            var splatRes = wrapper.Terrain.terrainData.alphamapResolution;
            var mainSpline = NodeConnection.GetSpline();
            var radius = config.Radius;

            // Create bounds to encapsulate spline (axis aligned)
            var bounds = mainSpline.GetApproximateBounds();
            bounds.Expand(radius * 2 * Vector3.one);

            // Create object bounds
            var objectBounds = mainSpline.GetApproximateXZObjectBounds();
            objectBounds.Expand(radius * 2 * Vector3.one);
            objectBounds.Expand(Vector3.up * 10000);

            // Early cull
            var axisBounds = objectBounds.ToAxisBounds();
            var terrainBounds = wrapper.Terrain.GetComponent<Collider>().bounds;
            if (!terrainBounds.Intersects(axisBounds))
            {
                return;
            }

            float planeGive = -(wrapper.Terrain.terrainData.size.x / wrapper.Terrain.terrainData.alphamapResolution) * SplatOffset;
            Plane startPlane, endPlane;
            GenerateSplinePlanes(planeGive, mainSpline, out startPlane, out endPlane);

            // Get matrix space min/max
            var matrixMin = terrain.WorldToSplatCoord(bounds.min, TerrainX.RoundType.Floor);
            var matrixMax = terrain.WorldToSplatCoord(bounds.max, TerrainX.RoundType.Ceil);

            matrixMin = new Common.Coord(Mathf.Clamp(matrixMin.x, 0, terrain.terrainData.alphamapResolution), Mathf.Clamp(matrixMin.z, 0, terrain.terrainData.alphamapResolution));
            matrixMax = new Common.Coord(Mathf.Clamp(matrixMax.x, 0, terrain.terrainData.alphamapResolution), Mathf.Clamp(matrixMax.z, 0, terrain.terrainData.alphamapResolution));

            var floatArraySize = new Common.Coord(matrixMax.x - matrixMin.x, matrixMax.z - matrixMin.z);

            // Get all the existing compound splats
            var currentPrototypes = wrapper.GetCompoundSplatPrototypes(layer, true);
            var baseData = layer.GetSplatMaps(matrixMin.x, matrixMin.z, floatArraySize.x, floatArraySize.z, splatRes);

            stencilKey = GetStencilKey();
            Serializable2DFloatArray thisPatchStencil = new Serializable2DFloatArray(floatArraySize.x,
                floatArraySize.z);
            foreach (var splatConfiguration in config.SplatConfigurations)
            {
                var splatPrototypeWrapper = splatConfiguration.SplatPrototype;
                Serializable2DByteArray baseLayerSplat;
                if (!baseData.TryGetValue(splatPrototypeWrapper, out baseLayerSplat))
                {
                    baseLayerSplat = new Serializable2DByteArray(floatArraySize.x, floatArraySize.z);
                    baseData[splatPrototypeWrapper] = baseLayerSplat;
                }

                for (var dz = 0; dz < floatArraySize.z; ++dz)
                {
                    for (var dx = 0; dx < floatArraySize.x; ++dx)
                    {
                        var coordX = matrixMin.x + dx;
                        var coordZ = matrixMin.z + dz;
                        var worldPos = terrain.SplatCoordToWorldPos(new Common.Coord(coordX, coordZ));
                        worldPos = new Vector3(worldPos.x, objectBounds.center.y, worldPos.z);

                        if (terrain.ContainsPointXZ(worldPos) &&
                            objectBounds.Contains(worldPos) &&
                            GeometryExtensions.BetweenPlanes(worldPos, startPlane, endPlane))
                        {
                            var uniformT = mainSpline.GetClosestUniformTimeOnSplineXZ(worldPos.xz()); // Expensive!
                            var closestOnSpline = mainSpline.GetUniformPointOnSpline(uniformT);
                            var normalizedFlatDistToSpline = (worldPos - closestOnSpline).xz().magnitude / (config.Radius);
                            if(normalizedFlatDistToSpline != Mathf.Clamp01(normalizedFlatDistToSpline))
                            {
                                continue;
                            }
                            var maskValue = config.SplatFalloff.Evaluate(normalizedFlatDistToSpline);
                            var writeFloatValue = splatConfiguration.SplatStrength * maskValue;
                            var writeValue = (byte)Mathf.RoundToInt(Mathf.Clamp(writeFloatValue * 255f, 0, 255));
                            var mainRead = baseLayerSplat[dx, dz];
                            var newVal = (byte)Mathf.Clamp(Mathf.Max(writeValue, mainRead), 0, 255);
                            var delta = newVal - mainRead;

                            if(delta < 1/255f)
                            {
                                continue;
                            }

                            foreach (var currentPrototype in currentPrototypes)
                            {
                                if (!baseData.ContainsKey(currentPrototype))
                                {
                                    continue;
                                }
                                if(currentPrototype == splatPrototypeWrapper)
                                {
                                    continue;
                                }
                                var otherSplatFloatValue = baseData[currentPrototype][dx, dz] / 255f;
                                var otherSplatFloatWriteVal = (otherSplatFloatValue * (1 - (delta / 255f)));
                                var write = (byte)Mathf.Clamp(Mathf.RoundToInt(otherSplatFloatWriteVal * 255), 0, 255);
                                baseData[currentPrototype][dx, dz] = write;
                            }
                            //DebugHelper.DrawPoint(worldPos, 1, Color.red, 10);
                            baseLayerSplat[dx, dz] = newVal;
                            layer.Stencil[coordX, coordZ] = MiscUtilities.CompressStencil(stencilKey, 1);
                            thisPatchStencil[dx, dz] = 1;
                        }
                        else
                        {
                            thisPatchStencil[dx, dz] = 0;
                        }
                    }
                }
            }

            foreach (var existingSplatPrototype in baseData)
            {
                var splat = existingSplatPrototype.Key;
                var data = existingSplatPrototype.Value;
                layer.SetSplatmap(splat, matrixMin.x, matrixMin.z, data, wrapper.Terrain.terrainData.alphamapResolution, thisPatchStencil);
            }
        }

        private int GetStencilKey()
        {
            return GetPriority();
            //return Network.Nodes.IndexOf(NodeConnection.ThisNode) + 1;
        }

        private void GenerateSplinePlanes(float planeGive, SplineSegment mainSpline, out Plane startPlane, out Plane endPlane)
        {
            var startNodePos = mainSpline.FirstControlPoint.Position;
            var startNodeControl = NodeConnection.ThisNode.GetNodeControl(NodeConnection.NextNode).Flatten().normalized;
            startPlane = new Plane(startNodeControl, startNodePos + startNodeControl * planeGive);
            var endNodePos = mainSpline.SecondControlPoint.Position;
            var endNodeControl = NodeConnection.NextNode.GetNodeControl(NodeConnection.ThisNode).Flatten().normalized;
            endPlane = new Plane(endNodeControl, endNodePos + endNodeControl * planeGive);
        }

    }
}