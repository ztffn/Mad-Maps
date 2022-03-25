using System;
using MadMaps.Common;
using MadMaps.Common.Collections;
using MadMaps.Common.GenericEditor;
using MadMaps.Terrains;
using UnityEngine;

namespace MadMaps.Roads.Connections
{
    public class SetTerrainHeight : ConnectionComponent
    {
        [Name("Terrain/Set Height")]
        public class Config : ConnectionConfigurationBase
        {
            public AnimationCurve Falloff = new AnimationCurve(new Keyframe(0, 1), new Keyframe(0.5f, 1), new Keyframe(1, 0));
            public AnimationCurve Height = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 0));
            public float Radius = 10;

            public override Type GetMonoType()
            {
                return typeof(SetTerrainHeight);
            }
        }

        private int GetStencilKey()
        {
            return Mathf.Max(1, GetPriority());
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

        public override void ProcessHeights(TerrainWrapper wrapper, LayerBase baseLayer, int stencilKey)
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
            var terrainPos = wrapper.Terrain.GetPosition();
            var terrainSize = wrapper.Terrain.terrainData.size;
            var heightRes = terrain.terrainData.heightmapResolution;

            var mainSpline = NodeConnection.GetSpline();
            var radius = config.Radius;
            var falloffCurve = config.Falloff;
            var heightCurve = config.Height;

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
            terrainBounds.Expand(Vector3.up * 10000);
            if (!terrainBounds.Intersects(axisBounds))
            {
                return;
            }

            // Get matrix space min/max
            var matrixMin = terrain.WorldToHeightmapCoord(bounds.min, TerrainX.RoundType.Floor) - Coord.One;
            matrixMin = matrixMin.Clamp(0, heightRes);
            var matrixMax = terrain.WorldToHeightmapCoord(bounds.max, TerrainX.RoundType.Ceil) + Coord.One;
            matrixMax = matrixMax.Clamp(0, heightRes);

            var xDelta = matrixMax.x - matrixMin.x;
            var zDelta = matrixMax.z - matrixMin.z;

            var floatArraySize = new Common.Coord(
                Mathf.Min(xDelta, terrain.terrainData.heightmapResolution - matrixMin.x),
                Mathf.Min(zDelta, terrain.terrainData.heightmapResolution - matrixMin.z));

            float planeGive = (wrapper.Terrain.terrainData.size.x / wrapper.Terrain.terrainData.heightmapResolution) * 0;
            Plane startPlane, endPlane;
            GenerateSplinePlanes(planeGive, mainSpline, out startPlane, out endPlane);

            var layerHeights = layer.GetHeights(matrixMin.x, matrixMin.z, floatArraySize.x, floatArraySize.z, heightRes) ??
                               new Serializable2DFloatArray(floatArraySize.x, floatArraySize.z);

            stencilKey = GetStencilKey();
            if (layer.Stencil == null || layer.Stencil.Width != heightRes || layer.Stencil.Height != heightRes)
            {
                layer.Stencil = new Stencil(heightRes, heightRes);
            }

            for (var dz = 0; dz < floatArraySize.z; ++dz)
            {
                for (var dx = 0; dx < floatArraySize.x; ++dx)
                {
                    var coordX = matrixMin.x + dx;
                    var coordZ = matrixMin.z + dz;

                    var worldPos = terrain.HeightmapCoordToWorldPos(new Common.Coord(coordX, coordZ));
                    worldPos = new Vector3(worldPos.x, objectBounds.center.y, worldPos.z);
                    if (!terrain.ContainsPointXZ(worldPos) ||
                        !objectBounds.Contains(worldPos) ||
                        !GeometryExtensions.BetweenPlanes(worldPos, startPlane, endPlane))
                    {
                        // Cull if we're outside of the approx bounds
                        continue;
                    }

                    var uniformT = mainSpline.GetClosestUniformTimeOnSplineXZ(worldPos.xz()); // Expensive!
                    var closestOnSpline = mainSpline.GetUniformPointOnSpline(uniformT);
                    var normalizedFlatDistToSpline = (worldPos - closestOnSpline).xz().magnitude / (radius);
                    if (normalizedFlatDistToSpline >= 1)
                    {
                        continue;
                    }

                    var maskValue = Mathf.Clamp01(falloffCurve.Evaluate(normalizedFlatDistToSpline));
                    var heightDelta = heightCurve.Evaluate(normalizedFlatDistToSpline);

                    float existingStencilStrength;
                    int existingStencilKey;
                    MiscUtilities.DecompressStencil(layer.Stencil[coordX, coordZ], out existingStencilKey, out existingStencilStrength);

                    if (existingStencilKey != stencilKey &&
                        existingStencilKey > stencilKey &&
                        !(existingStencilStrength < maskValue && maskValue > 0))
                    {
                        continue;
                    }

                    // Refine our worldposition to be on the same XZ plane as the spline point
                    worldPos = new Vector3(worldPos.x, closestOnSpline.y, worldPos.z);

                    // Find the point on the spline closest to this given point
                    var naturalT = mainSpline.UniformToNaturalTime(uniformT);

                    // Get the upvec from the natural time
                    var up = mainSpline.GetUpVector(naturalT).normalized;

                    // Create a plane and cast against it
                    var plane = new Plane(up, closestOnSpline);

                    float dist = 0;
                    var castRay = new Ray(worldPos, Vector3.down);
                    plane.Raycast(castRay, out dist);
                    var castPoint = castRay.GetPoint(dist);
                    var heightAtPoint = (castPoint.y + heightDelta);

                    heightAtPoint -= terrainPos.y;
                    heightAtPoint /= terrainSize.y;
                    heightAtPoint = MiscUtilities.FloorToUshort(heightAtPoint);

                    var existingHeight = layerHeights[dx, dz];
                    var newHeight = Mathf.Lerp(existingHeight, heightAtPoint, Mathf.Clamp01(maskValue));

                    layerHeights[dx, dz] = newHeight;

                    var key = maskValue > existingStencilStrength ? stencilKey : existingStencilKey;
                    var newRawStencilValue = MiscUtilities.CompressStencil(key, /*stencilKey == existingStencilKey ?*/ Mathf.Max(maskValue, existingStencilStrength)/* : maskValue + existingStencilStrength*/);
                    //newRawStencilValue = MiscUtilities.CompressStencil(key, 1);

                    layer.Stencil[coordX, coordZ] = newRawStencilValue;
                }
            }

            layer.SetHeights(matrixMin.x, matrixMin.z,
                layerHeights, wrapper.Terrain.terrainData.heightmapResolution);
        }
    }
}