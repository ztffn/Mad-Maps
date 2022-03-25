using System;
using MadMaps.Common;
using MadMaps.Common.GenericEditor;
using UnityEngine;
using Random = System.Random;

namespace MadMaps.Roads.Connections
{
    public class SplineDeformer : ConnectionComponent, ISplineModifier
    {
        [Name("Spline Deformer")]
        public class Config : ConnectionConfigurationBase
        {
            public AnimationCurve Amplitude = new AnimationCurve()
            {
                keys = new Keyframe[]
            {
                new Keyframe(0, 0), 
                new Keyframe(0.2f, 1), 
                new Keyframe(0.8f, 1), 
                new Keyframe(1, 0), 
            }
            };
            public float AmplitudeCoefficient = 1;
            public float Frequency = .1f;

            public override Type GetMonoType()
            {
                return typeof(SplineDeformer);
            }
        }

        public SplineSegment ProcessSpline(SplineSegment spline)
        {
            spline.Recalculate(true);
            var config = Configuration.GetConfig<Config>();
            if (config == null)
            {
                return spline;
            }

            var rand = new Random(NodeConnection.ThisNode.Seed);

            float accum = (float)rand.NextDouble() * (rand.Flip() ? -1 : 1);
            for (var i = 0; i < spline.Points.Count; ++i)
            {
                var p = spline.Points[i];
                var tangent = spline.GetTangent(p.NaturalTime).normalized;
                var offset = tangent * accum * config.AmplitudeCoefficient;
                p.Position = Vector3.Lerp(p.Position, p.Position + offset, config.Amplitude.Evaluate(p.UniformTime));

                spline.Points[i] = p;

                var flip = rand.NextDouble() > 0.5 ? 1 : -1;
                accum += config.Frequency*(float) rand.NextDouble() * flip;
            }
            return spline;
        }
    }
}