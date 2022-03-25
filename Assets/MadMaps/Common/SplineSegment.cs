using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MadMaps.Common
{
    [Serializable]
    public class SplineSegment
    {
        public enum ESplineInterpolation
        {
            Natural,
            Uniform,
        }

        public float Resolution = 0.1f;
        public ControlPoint FirstControlPoint;
        public ControlPoint SecondControlPoint;

        // Calculated data
        [HideInInspector]
        public List<SplinePoint> Points;
        [HideInInspector]
        public Bounds Bounds;

        private SplineSnapshot _snapshot;
        
        public SplineSegment()
        {
            FirstControlPoint = new ControlPoint();
            SecondControlPoint = new ControlPoint();
            Points = new List<SplinePoint>();
        }

        public SplineSegment(SplineSegment segment)
        {
            FirstControlPoint = segment.FirstControlPoint.Copy();
            SecondControlPoint = segment.SecondControlPoint.Copy();
            Resolution = segment.Resolution;
            Points = segment.Points.ToList();
            Bounds = segment.Bounds;
            Length = segment.Length;
        }

        public SplineSegment(ControlPoint firstPoint, ControlPoint secondPoint, float resolution)
        {
            FirstControlPoint = firstPoint;
            SecondControlPoint = secondPoint;
            Resolution = resolution;
            Recalculate();
        }

        public float Length { get; private set; }

        public void Recalculate(bool force = false)
        {
            if (!force && !IsDirty())
            {
                return;
            }

            Resolution = Mathf.Max(float.Epsilon, Resolution);
            Points.Clear();

            if (FirstControlPoint.Control == Vector3.zero)
            {
                FirstControlPoint.Control = (SecondControlPoint.Position - FirstControlPoint.Position).normalized;
            }
            if (SecondControlPoint.Control == Vector3.zero)
            {
                SecondControlPoint.Control = (FirstControlPoint.Position - SecondControlPoint.Position).normalized;
            }

            float t = 0;
            var lastPoint = FirstControlPoint.Position;
            var straightDistance = Vector3.Distance(FirstControlPoint.Position, SecondControlPoint.Position);
            var distScaledResolution = Resolution*straightDistance;
            var step = 1/distScaledResolution;

            float accumLength = 0;

            while (t < 1)
            {
                var point = this.GetNaturalPointOnSpline(t);
                accumLength += (lastPoint - point).magnitude;

                Points.Add(new SplinePoint(point, t, accumLength));
                t += step;
                lastPoint = point;
            }

            // Add the last point
            accumLength += (lastPoint - SecondControlPoint.Position).magnitude;
            Points.Add(new SplinePoint(SecondControlPoint.Position, 1, accumLength));
            Length = accumLength;

            for (int i = 0; i < Points.Count; i++)
            {
                var splinePoint = Points[i];
                splinePoint.UniformTime = this.NaturalToUniformTime(splinePoint.NaturalTime);
                if (i == 0)
                {
                    Bounds = new Bounds(splinePoint.Position, Vector3.zero);
                }
                else
                {
                    Bounds.Encapsulate(splinePoint.Position);
                }
                Points[i] = splinePoint;
            }

            UpdateSnapshot();
        }

        void UpdateSnapshot()
        {
            if (_snapshot == null)
            {
                _snapshot = new SplineSnapshot();
            }
            _snapshot.FirstControlPoint = FirstControlPoint.Copy();
            _snapshot.SecondControlPoint = SecondControlPoint.Copy();
            //_snapshot.InterpolationType = Interpolation;
            _snapshot.Resolution = Resolution;
        }

        public bool IsDirty()
        {
            if (_snapshot == null)
            {
                return true;
            }
            if (Points == null || Points.Count == 0)
            {
                return true;
            }
            if (!Equals(_snapshot.FirstControlPoint, FirstControlPoint)
                || !Equals(_snapshot.SecondControlPoint, SecondControlPoint)
                || _snapshot.Resolution != Resolution)
            {
                return true;
            }
            return false;
        }

        [Serializable]
        public struct SplinePoint
        {
            public float AccumLength;
            public Vector3 Position;
            public float NaturalTime;
            public float UniformTime;

            public SplinePoint(Vector3 pos, float naturalTime, float accumLength)
            {
                Position = pos;
                NaturalTime = naturalTime;
                AccumLength = accumLength;
                UniformTime = -1;
            }
        }

        [Serializable]
        public class ControlPoint
        {
            [SerializeField] private Vector3 _upVector = new Vector3(0, 1, 0);
            public Vector3 Control;
            public Vector3 Position;
            public Vector3 Rotation;

            public Vector3 Tangent
            {
                get { return Vector3.Cross(UpVector, Control); }
            }

            public ControlPoint(Vector3 position, Vector3 control, Vector3 rotatation, Vector3 upVector)
            {
                Position = position;
                Control = control;
                Rotation = rotatation;
                UpVector = upVector;
            }
            
            public ControlPoint()
            {
                Position = Vector3.zero;
                Control = Vector3.zero;
                Rotation = Vector3.zero;
                UpVector = Vector3.up;
            }

            public ControlPoint Copy()
            {
                return new ControlPoint(Position, Control, Rotation, UpVector);
            }

            public Vector3 UpVector
            {
                get { return _upVector.normalized; }
                set { _upVector = value.normalized; }
            }

            protected bool Equals(ControlPoint other)
            {
                return _upVector.Equals(other._upVector) && Position.Equals(other.Position) &&
                       Control.Equals(other.Control) && Rotation.Equals(other.Rotation);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = _upVector.GetHashCode();
                    hashCode = (hashCode*397) ^ Position.GetHashCode();
                    hashCode = (hashCode*397) ^ Control.GetHashCode();
                    hashCode = (hashCode*397) ^ Rotation.GetHashCode();
                    return hashCode;
                }
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != GetType()) return false;
                return Equals((ControlPoint) obj);
            }
        }

        /// <summary>
        ///     Store the last configuration the spline was baked with, to detect dirtiness
        /// </summary>
        [Serializable]
        private class SplineSnapshot
        {
            public ControlPoint FirstControlPoint = new ControlPoint();
            public ControlPoint SecondControlPoint = new ControlPoint();
            public float Resolution;
        }
    }
}