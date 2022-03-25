using System;
using System.Text.RegularExpressions;
using MadMaps.Common;
using MadMaps.Common.GenericEditor;
using MadMaps.Terrains;

using UnityEngine;

namespace MadMaps.Roads
{
    public class RemoveTerrainObjects : ConnectionComponent
    {
        [Name("Terrain/Remove Objects")]
        public class Config : ConnectionConfigurationBase
        {
            //[Header("Trees")]
            //public float TreeRemoveDistance = 1;

            [Header("Objects")]
            public LayerMask ObjectRemovalMask = 1 << 21;
            public float ObjectRemoveDistance = 1;
            public string RegexMatch;

            public override Type GetMonoType()
            {
                return typeof(RemoveTerrainObjects);
            }
        }

        public bool ShowDebug;

        public override void ProcessObjects(TerrainWrapper wrapper, LayerBase baseLayer, int stencilKey)
        {
            var layer = baseLayer as MMTerrainLayer;
            if(layer == null)
            {
                Debug.LogWarning(string.Format("Attempted to write {0} to incorrect layer type! Expected Layer {1} to be {2}, but it was {3}", name, baseLayer.name, GetLayerType(), baseLayer.GetType()), this);
                return;
            }
            if (!Network)
            {
                return;
            }
            if (Configuration == null)
            {
                return;
            }
            var config = Configuration.GetConfig<Config>();
            if (config == null)
            {
                Debug.LogError("Invalid configuration! Expected ConnectionTerrainHeightConfiguration");
                return;
            }

            var spline = NodeConnection.GetSpline();
            var bounds = spline.GetApproximateXZObjectBounds();
            bounds.Expand(new Vector3(config.ObjectRemoveDistance, 1000, config.ObjectRemoveDistance));

            var tPos = wrapper.transform.position;
            var tSize = wrapper.Terrain.terrainData.size;

            var regex = new Regex(config.RegexMatch ?? string.Empty);

            var objects = wrapper.GetCompoundObjects(layer, true);
            foreach (var data in objects)
            {
                if (data.Prefab == null || !regex.IsMatch(data.Prefab.name))
                {
                    continue;
                }

                var wPos = tPos +
                           new Vector3(data.Position.x * tSize.x, data.Position.y, data.Position.z * tSize.z);
                if (!bounds.Contains(wPos))
                {
                    //Debug.DrawLine(wPos, wPos + Vector3.up * 10, Color.cyan, 20);
                    continue;
                }

                var ut = spline.GetClosestUniformTimeOnSplineXZ(wPos.xz());
                var pointOnSpline = spline.GetUniformPointOnSpline(ut);

                var dist = (pointOnSpline.xz() - wPos.xz()).magnitude;
                if (dist < config.ObjectRemoveDistance)
                {
                    if (!layer.ObjectRemovals.Contains(data.Guid))
                    {
                        layer.ObjectRemovals.Add(data.Guid);
                    }
                    if (ShowDebug)
                        Debug.DrawLine(pointOnSpline, wPos, Color.red, 20);
                }
                else
                {
                    if (ShowDebug)
                        Debug.DrawLine(pointOnSpline, wPos, Color.green, 20);
                }
            }
        }
    }
}