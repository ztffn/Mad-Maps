using System;
using System.Collections.Generic;
using MadMaps.Common;
using MadMaps.Common.GenericEditor;
using MadMaps.Terrains;
using UnityEngine;

using Random = System.Random;

namespace MadMaps.Roads
{
    public class PlaceTerrainTrees : ConnectionComponent
    {
        [Name("Terrain/Place Trees")]
        public class Config : ConnectionConfigurationBase
        {
            public AnimationCurve ProbablityAlongCurve = new AnimationCurve()
            {
                keys = new Keyframe[]
            {
                new Keyframe(0, 1), 
                new Keyframe(1, 1), 
            }
            };
            public AnimationCurve ProbablityThroughCurve = new AnimationCurve()
            {
                keys = new Keyframe[]
            {
                new Keyframe(0, -1), 
                new Keyframe(0.5f, 0), 
                new Keyframe(1, 1),
            }
            };
            public FloatMinMax Size = new FloatMinMax(1, 1);
            public ColorMinMax Color = new ColorMinMax(UnityEngine.Color.white, UnityEngine.Color.white);
            public List<GameObject> Trees = new List<GameObject>();
            public float ProbabilityMultiplier = 1;
            public FloatMinMax StepDistance = new FloatMinMax(1, 1);
            public float OffsetMultiplier = 5f;
            public float YOffset = 0;

            public override Type GetMonoType()
            {
                return typeof(PlaceTerrainTrees);
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

            if(!Network || Configuration == null)
            {
                return;
            }

            var config = Configuration.GetConfig<Config>();
            if(config == null)
            {
                return;
            }
            
            var spline = NodeConnection.GetSpline();
            var rand = new Random(NodeConnection.ThisNode.Seed);            
            var length = spline.Length;
            var step = config.StepDistance;
            var tSize = wrapper.Terrain.terrainData.size;

            for (var i = 0f; i < length; i += step.GetRand(rand))
            {
                var roll = rand.NextDouble();
                var randroll = (float) rand.NextDouble();
                var eval = config.ProbablityThroughCurve.Evaluate(randroll);
                //Debug.Log(randroll + " " + eval);
                var offsetDist = eval * config.OffsetMultiplier;
                var uniformT = i / length;
                var naturalT = spline.UniformToNaturalTime(uniformT);

                var offset = (spline.GetTangent(naturalT).normalized * offsetDist);
                var probability = config.ProbablityAlongCurve.Evaluate(uniformT) * config.ProbabilityMultiplier;

                if (probability < roll)
                {
                    continue;
                }

                // Place tree
                var randIndex = Mathf.FloorToInt((float)rand.NextDouble() * config.Trees.Count);
                var prefab = config.Trees[randIndex];
                var wPos = spline.GetUniformPointOnSpline(uniformT) + offset;
                //var h -
                //wPos.y = wrapper.GetCompoundHeight(layer, wPos)*tSize.y;
                wPos.y = config.YOffset;

                //Debug.DrawLine(wPos, wPos + Vector3.up *10, Color.red, 10);
                var tPos = wrapper.Terrain.WorldToTreePos(wPos);
                layer.Trees.Add(new MadMapsTreeInstance(tPos, Vector2.one * config.Size.GetRand(rand), prefab, config.Color.GetRand(rand)));
            }
        }
    }
}