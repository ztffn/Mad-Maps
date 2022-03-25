using System.Collections.Generic;
using MadMaps.Common;
using MadMaps.Common.GenericEditor;
using UnityEngine;

namespace MadMaps.Terrains
{
    [Name("Trees/Gradient Filter")]
    public class TreeGradientFilter : ProceduralLayerComponent
    {
        public List<GameObject> Prefabs = new List<GameObject>();
        public bool InvertPrefabMask;
        public AnimationCurve Offset = new AnimationCurve();
        public float MinY = 0.6f;

        public override ApplyTiming Timing
        {
            get { return ApplyTiming.OnPreFinalise; }
        }

        public override string HelpURL
        {
            get { return "http://lrtw.net/madmaps/index.php?title=Tree_Gradient_Filter"; }
        }
        
        public override void Apply(ProceduralLayer layer, TerrainWrapper wrapper)
        {
            var trees = wrapper.CompoundTerrainData.Trees;
            var tSize = wrapper.Terrain.terrainData.size;
            List<string> toRemove = new List<string>();
            foreach (var tree in trees)
            {
                if (!InvertPrefabMask && Prefabs.Count > 0 && !Prefabs.Contains(tree.Value.Prototype))
                {
                    continue;
                }
                if (InvertPrefabMask && Prefabs.Count > 0 && Prefabs.Contains(tree.Value.Prototype))
                {
                    continue;
                }

                var wPos = wrapper.Terrain.TreeToWorldPos(tree.Value.Position);
                wPos.y = wrapper.GetCompoundHeight(layer, wPos, true) * tSize.y;
                var gradient = wrapper.GetNormalFromHeightmap(tree.Value.Position.xz());

                if (gradient.y < MinY)
                {
                    //Debug.DrawLine(wPos, wPos + gradient, Color.red, 10);
                    toRemove.Add(tree.Key);
                    continue;
                }

                //Debug.DrawLine(wPos, wPos + gradient, Color.green, 10);
                var yPos = Offset.Evaluate(gradient.y);
                tree.Value.Position.y = Mathf.Min(yPos, tree.Value.Position.y);

                /*Debug.DrawLine(wPos, wPos + Vector3.up * yPos, Color.yellow, 10);
                DebugHelper.DrawPoint(wPos, 0.1f, Color.yellow, 10);
                DebugHelper.DrawPoint(wPos + Vector3.up * yPos, 0.1f, Color.blue, 10);*/
            }
            foreach (var guid in toRemove)
            {
                trees.Remove(guid);
            }
            Debug.LogFormat("TreeGradientFilter removed {0} trees", toRemove.Count);
        }
    }
}