using System.Collections.Generic;
using MadMaps.Common;
using MadMaps.Common.GenericEditor;
using UnityEngine;

namespace MadMaps.Terrains
{
    [Name("Trees/Proximity Filter")]
    public class TreeProximityFilter : ProceduralLayerComponent
    {
        public float Distance = 1;
        List<GameObject> Filter = new List<GameObject>(); 

        public override ApplyTiming Timing
        {
            get { return ApplyTiming.Instant; }
        }

        public override string HelpURL
        {
            get { return "http://lrtw.net/madmaps/index.php?title=Tree_Proximity_Filter"; }
        }

        public override void Apply(ProceduralLayer layer, TerrainWrapper wrapper)
        {
            if (layer == null)
            {
                Debug.LogError("Layer was null!");
                return;
            }

            var trees = wrapper.CompoundTerrainData.Trees;

            HashSet<string> removed = new HashSet<string>();
            List<string> neighbours = new List<string>();
            foreach (var tree in trees)
            {
                if (removed.Contains(tree.Key))
                {
                    continue;
                }
                if (!Filter.IsNullOrEmpty() && !Filter.Contains(tree.Value.Prototype))
                {
                    continue;
                }

                var wPos = wrapper.Terrain.TreeToWorldPos(tree.Value.Position);

                neighbours.Clear();
                var coord = trees.PositionToCoord(tree.Value.Position);
                trees.AppendPartitionList(coord, neighbours);

                for (int i = 0; i < neighbours.Count; i++)
                {
                    var neighbour = trees[neighbours[i]];
                    if (neighbour.Guid == tree.Value.Guid || removed.Contains(neighbour.Guid))
                    {
                        continue;
                    }
                    var neighbourPos = wrapper.Terrain.TreeToWorldPos(trees[neighbours[i]].Position);
                    var distSqr = (wPos.xz() - neighbourPos.xz()).sqrMagnitude;
                    if (distSqr < Distance*Distance)
                    {
                        removed.Add(neighbour.Guid);
                    }
                }
            }

            foreach (var guid in removed)
            {
                if (!layer.TreeRemovals.Contains(guid))
                {
                    layer.TreeRemovals.Add(guid);
                }
                trees.Remove(guid);
            }

            Debug.Log(string.Format("TreeProximity deleted {0} trees", removed.Count));
        }
    }
}