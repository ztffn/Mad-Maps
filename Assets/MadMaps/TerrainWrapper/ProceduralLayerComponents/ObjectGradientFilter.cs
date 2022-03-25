using System.Collections.Generic;
using MadMaps.Common;
using MadMaps.Common.GenericEditor;
using UnityEngine;

namespace MadMaps.Terrains
{
    [Name("Objects/Gradient Filter")]
    public class ObjectGradientFilter : ProceduralLayerComponent
    {
        public List<GameObject> Prefabs = new List<GameObject>();
        public bool InvertPrefabMask;
        public float MinY = 0.6f;

        public override ApplyTiming Timing
        {
            get { return ApplyTiming.OnPreFinalise; }
        }

        public override string HelpURL
        {
            get { return "http://lrtw.net/madmaps/index.php?title=Object_Gradient_Filter"; }
        }

        public override void Apply(ProceduralLayer layer, TerrainWrapper wrapper)
        {
            var objects = wrapper.CompoundTerrainData.Objects;
            var tSize = wrapper.Terrain.terrainData.size;
            List<string> toRemove = new List<string>();
            foreach (var tree in objects)
            {
                if (!InvertPrefabMask && Prefabs.Count > 0 && !Prefabs.Contains(tree.Value.Data.Prefab))
                {
                    continue;
                }
                if (InvertPrefabMask && Prefabs.Count > 0 && Prefabs.Contains(tree.Value.Data.Prefab))
                {
                    continue;
                }

                var wPos = wrapper.Terrain.TreeToWorldPos(tree.Value.Data.Position);
                wPos.y = wrapper.GetCompoundHeight(layer, wPos, true) * tSize.y;
                var gradient = wrapper.GetNormalFromHeightmap(tree.Value.Data.Position.xz());

                if (gradient.y < MinY)
                {
                    toRemove.Add(tree.Key);
                    continue;
                }
            }
            foreach (var guid in toRemove)
            {
                objects.Remove(guid);
            }
            Debug.LogFormat("TreeGradientFilter removed {0} trees", toRemove.Count);
        }
    }
}