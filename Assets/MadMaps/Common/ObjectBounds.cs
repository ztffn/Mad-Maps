using UnityEngine;

namespace MadMaps.Common
{
    // A non-axis-aligned bounding box (just does tests in the local rotated space)
    public struct ObjectBounds
    {
        private Bounds _bounds;
        public Quaternion Rotation;

        public Vector3 max
        {
            get { return Rotation * _bounds.max; }
        }

        public Vector3 min
        {
            get { return Rotation * _bounds.min; }
        }

        public Vector3 size
        {
            get { return _bounds.size; }
        }

        public ObjectBounds(Vector3 center, Vector3 extents, Quaternion rotation)
        {
            Rotation = rotation;
            _bounds = new Bounds(Quaternion.Inverse(Rotation) * center, extents * 2);
        }

        public Vector3 center
        {
            get { return Rotation * _bounds.center; }
            set { _bounds.center = Quaternion.Inverse(Rotation) * value; }
        }

        public Vector3 extents
        {
            get { return _bounds.extents; }
            set { _bounds.extents = value; }
        }

        public bool Contains(Vector3 point)
        {
            point = Quaternion.Inverse(Rotation)*point;
            return _bounds.Contains(point);
        }

        public void Encapsulate(Vector3 point)
        {
            point = Quaternion.Inverse(Rotation) * point;
            _bounds.Encapsulate(point);
        }

        public void Expand(Vector3 size)
        {
            _bounds.Expand(size);
        }

        public Bounds ToAxisBounds()
        {
            var b = new Bounds(center, Vector3.zero);

            b.Encapsulate(center + Rotation * new Vector3(extents.x, extents.y, extents.z));
            b.Encapsulate(center + Rotation * new Vector3(-extents.x, extents.y, extents.z));
            b.Encapsulate(center + Rotation * new Vector3(extents.x, extents.y, -extents.z));
            b.Encapsulate(center + Rotation * new Vector3(-extents.x, extents.y, -extents.z));

            b.Encapsulate(center + Rotation * new Vector3(extents.x, -extents.y, extents.z));
            b.Encapsulate(center + Rotation * new Vector3(-extents.x, -extents.y, extents.z));
            b.Encapsulate(center + Rotation * new Vector3(extents.x, -extents.y, -extents.z));
            b.Encapsulate(center + Rotation * new Vector3(-extents.x, -extents.y, -extents.z));

            return b;
        }

        public ObjectBounds Flatten()
        {
            var flatRot = Quaternion.Euler(0, Rotation.eulerAngles.y, 0);

            var b = new ObjectBounds(center, Vector3.zero, flatRot);

            b.Encapsulate(center + Rotation * new Vector3(extents.x, extents.y, extents.z));
            b.Encapsulate(center + Rotation * new Vector3(-extents.x, extents.y, extents.z));
            b.Encapsulate(center + Rotation * new Vector3(extents.x, extents.y, -extents.z));
            b.Encapsulate(center + Rotation * new Vector3(-extents.x, extents.y, -extents.z));

            b.Encapsulate(center + Rotation * new Vector3(extents.x, -extents.y, extents.z));
            b.Encapsulate(center + Rotation * new Vector3(-extents.x, -extents.y, extents.z));
            b.Encapsulate(center + Rotation * new Vector3(extents.x, -extents.y, -extents.z));
            b.Encapsulate(center + Rotation * new Vector3(-extents.x, -extents.y, -extents.z));

            return b;
        }

        public override string ToString()
        {
            return string.Format("Bounds: {0} Rot: {1}", _bounds.ToString(), Rotation.eulerAngles);
        }
    }
}