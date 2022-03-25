using System.Collections.Generic;
using System.Linq;
using MadMaps.Common;
using MadMaps.Common.GenericEditor;
using UnityEngine;

namespace MadMaps.Terrains
{
    [Name("Trees/Raycast Filter")]
    public class TreeRaycastFilter : ProceduralLayerComponent
    {
        public LayerMask Mask = ~0;
        public float Distance = 1;
        
        public override ApplyTiming Timing
        {
            get { return ApplyTiming.OnFrameAfterPostFinalise; }
        }

        public override string HelpURL
        {
            get { return "http://lrtw.net/madmaps/index.php?title=Tree_Raycast_Filter"; }
        }

        public override void Apply(ProceduralLayer layer, TerrainWrapper wrapper)
        {
            var trees = wrapper.CompoundTerrainData.Trees;
            var tSize = wrapper.Terrain.terrainData.size;
            var terrainY = wrapper.transform.position.y;

            HashSet<string> removed = new HashSet<string>();
            foreach (var treePair in trees)
            {
                var wPos = wrapper.Terrain.TreeToWorldPos(treePair.Value.Position);
                var height = wrapper.GetCompoundHeight(null, wPos) * tSize.y;
                wPos.y = terrainY + treePair.Value.Position.y + height;

                RaycastHit hit;
                if (Physics.Raycast(wPos + Vector3.up * 500, Vector3.down, out hit, 500, Mask) && ((hit.point - wPos).magnitude > Distance))
                {
                    removed.Add(treePair.Key);
                }
            }
            layer.TreeRemovals = removed.ToList();
            foreach (var treeRemoval in layer.TreeRemovals)
            {
                trees.Remove(treeRemoval);
            }

            Debug.Log(string.Format("TreeRaycast deleted {0} trees", layer.TreeRemovals.Count));
            wrapper.FinaliseTrees();
            MiscUtilities.ClearProgressBar();
        }
    }
}