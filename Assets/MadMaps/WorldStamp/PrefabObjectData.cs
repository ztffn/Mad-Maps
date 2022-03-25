using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace MadMaps.WorldStamps
{
    [Serializable]
    public struct PrefabObjectData
    {
        public Vector3 Position;
        public Vector3 Rotation;
        public Vector3 Scale;
        public GameObject Prefab;
        public string Guid;
        [FormerlySerializedAs("IsRelativeToStamp")]
        public bool AbsoluteHeight;
        public string ContainerMetadata;
    }
}