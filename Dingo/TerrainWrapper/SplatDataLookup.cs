using System;
using System.Collections.Generic;
using Dingo.Common;
using Dingo.Common.Collections;
using Dingo.WorldStamp;
using UnityEngine;

namespace Dingo.Terrains.Lookups
{
    /*[Serializable]
    public class SplatDataLookup : CompositionDictionary<SplatPrototypeWrapper, Serializable2DFloatArray>
    {
    }*/

    [Serializable]
    public class CompressedSplatData
    {
        public SplatPrototypeWrapper Wrapper;
        public Serializable2DByteArray Data;
    }

    [Serializable]
    public class CompressedDetailData
    {
        public DetailPrototypeWrapper Wrapper;
        public Serializable2DByteArray Data;
    }

    [Serializable]
    public class CompressedSplatDataLookup : CompositionDictionary<SplatPrototypeWrapper, Serializable2DByteArray>
    {
        public CompressedSplatDataLookup() : base() { }

        public CompressedSplatDataLookup(Dictionary<SplatPrototypeWrapper, Serializable2DByteArray> data) : base()
        {
            foreach (var pair in data)
            {
                Add(pair.Key, pair.Value);
            }
        }
    }

    /*[Serializable]
    public class DetailDataLookup : CompositionDictionary<DetailPrototypeWrapper, Serializable2DIntArray>
    {
    }*/

    [Serializable]
    public class CompressedDetailDataLookup : CompositionDictionary<DetailPrototypeWrapper, Serializable2DByteArray>
    {
        public CompressedDetailDataLookup() : base() { }

        public CompressedDetailDataLookup(Dictionary<DetailPrototypeWrapper, Serializable2DByteArray> data)
        {
            foreach (var pair in data)
            {
                Add(pair.Key, pair.Value);
            }
        }
    }

    [Serializable]
    public class TreeLookup : CompositionDictionary<string, DingoTreeInstance>
    {
        private const int Partitioning = 256;

        [Serializable]
        public class SpatialMapping : CompositionDictionary<Coord, List<string>>{}

        [SerializeField]
        private SpatialMapping _mapping = new SpatialMapping();

        public Coord PositionToCoord(Vector3 pos)
        {
            int x = Mathf.FloorToInt(Mathf.Clamp01(pos.x)*Partitioning);
            int z = Mathf.FloorToInt(Mathf.Clamp01(pos.z)*Partitioning);
            return new Coord(x, z);
        }

        public void AppendPartitionList(Coord coord, List<string> toAppend)
        {
            if (toAppend == null)
            {
                toAppend = new List<string>();
            }
            List<string> partitionList;
            if (_mapping.TryGetValue(coord, out partitionList))
            {
                toAppend.AddRange(partitionList);
            }
        } 

        public override void Add(string key, DingoTreeInstance value)
        {
            var coord = PositionToCoord(value.Position);
            List<string> partitionList;
            if (!_mapping.TryGetValue(coord, out partitionList))
            {
                partitionList = new List<string>();
                _mapping[coord] = partitionList;
            }
            base.Add(key, value);
            if (partitionList.Contains(value.Guid))
            {
                throw new Exception("Same tree GUID in spatial mapping???");
            }
            partitionList.Add(value.Guid);
        }

        public override bool Remove(string key)
        {
            DingoTreeInstance value;
            if (TryGetValue(key, out value))
            {
                var coord = PositionToCoord(value.Position);
                List<string> partitionList;
                if (_mapping.TryGetValue(coord, out partitionList))
                {
                    partitionList.Remove(key);
                }
            }
            return base.Remove(key);
        }

        public override void Clear()
        {
            _mapping.Clear();
            base.Clear();
        }
    }

    [Serializable]
    public class InstantiatedObjectData
    {
        public GameObject InstantiatedObject;
        public LayerBase Owner;
        public PrefabObjectData Data;

        public InstantiatedObjectData()
        {
        }

        public InstantiatedObjectData(PrefabObjectData data, LayerBase owner, GameObject instantiatedObj)
        {
            Data = data;
            Owner = owner;
            InstantiatedObject = instantiatedObj;
        }
    }
    
    [Serializable]
    public class ObjectPrefabDataLookup : CompositionDictionary<string, InstantiatedObjectData>
    {
    }
}