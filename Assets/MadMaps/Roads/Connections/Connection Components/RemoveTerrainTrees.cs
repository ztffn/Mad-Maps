using System;
using MadMaps.Common;
using MadMaps.Common.GenericEditor;
using MadMaps.Terrains;
using UnityEngine;

namespace MadMaps.Roads.Connections
{
    public class RemoveTerrainTrees : ConnectionComponent
    {
        [Name("Terrain/Remove Trees")]
        public class Config : ConnectionConfigurationBase
        {
            public float TreeRemoveDistance = 1;
            public string RegexMatch;

            public override Type GetMonoType()
            {
                return typeof(RemoveTerrainTrees);
            }
        }

        public override void ProcessTrees(TerrainWrapper wrapper, LayerBase baseLayer, int stencilKey)
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

            var mainSpline = NodeConnection.GetSpline();
            var radius = config.TreeRemoveDistance;

            // Create bounds to encapsulate spline (axis aligned)
            var bounds = mainSpline.GetApproximateBounds();
            bounds.Expand(radius * 2 * Vector3.one);

            // Create object bounds
            var objectBounds = mainSpline.GetApproximateXZObjectBounds();
            objectBounds.Expand(radius * 2 * Vector3.one);
            objectBounds.Expand(Vector3.up * 10000);

            var flatCullbounds = objectBounds.ToAxisBounds();
            //DebugHelper.DrawCube(flatCullbounds.center, flatCullbounds.extents, Quaternion.identity, Color.yellow, 20);
            //DebugHelper.DrawCube(objectBounds.center, objectBounds.extents, objectBounds.Rotation, Color.cyan, 20);

            Plane startPlane, endPlane;
            GenerateSplinePlanes(0, mainSpline, out startPlane, out endPlane);

            var trees = wrapper.GetCompoundTrees(layer, true, flatCullbounds);
            int removeCount = 0;
            for (int i = trees.Count - 1; i >= 0; i--)
            {
                var treeInstance = trees[i];
                var wPos = wrapper.Terrain.TreeToWorldPos(treeInstance.Position);
                if (!objectBounds.Contains(wPos) || !GeometryExtensions.BetweenPlanes(wPos, startPlane, endPlane))
                {
                    continue;
                }

                var ut = mainSpline.GetClosestUniformTimeOnSplineXZ(wPos.xz());
                var splinePos = mainSpline.GetUniformPointOnSpline(ut);
                var d = splinePos.xz() - wPos.xz();

                if (d.sqrMagnitude < config.TreeRemoveDistance * config.TreeRemoveDistance && !layer.TreeRemovals.Contains(treeInstance.Guid))
                {
                    layer.TreeRemovals.Add(treeInstance.Guid);
                    removeCount++;
                }
            }
            //Debug.Log(string.Format("{0} removed {1} trees.", this, removeCount), this);
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