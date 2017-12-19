using sMap.Common;
using sMap.Terrains;
using UnityEngine;

namespace sMap.Roads
{
    public class NodeSplatComponent : NodeComponent, IOnBakeCallback
    {
        public SplatPrototypeWrapper PrimaryWrapper;

        public int GetPriority()
        {
            return 0;
        }

        public void OnBake()
        {
            var wrapper = TerrainWrapper.GetWrapper(transform.position);
            var splatPos = wrapper.Terrain.WorldToSplatCoord(transform.position);
            var compoundSplats = wrapper.GetCompoundSplats(RoadNetwork.LevelInstance.GetLayer(wrapper), splatPos.x, splatPos.z, 1, 1, true);
            byte max = 0;
            foreach (var pair in compoundSplats)
            {
                var value = pair.Value[0, 0];
                if (value > max)
                {
                    PrimaryWrapper = pair.Key;
                    max = value;
                }
            }
            if (PrimaryWrapper == null)
            {
                Debug.LogWarning("Failed to find dominant splat!", this);
            }
        }
    }
}