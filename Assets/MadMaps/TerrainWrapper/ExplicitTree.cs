using System.Collections.Generic;
using UnityEngine;

namespace MadMaps.Terrains
{
    public class ExplicitTree : MonoBehaviour
    {
        public GameObject Prefab;
        public string LayerName = "ExplicitTrees";

        public static void Reapply()
        {
            var all = FindObjectsOfType<ExplicitTree>();
            var wrappers = new Dictionary<TerrainWrapper, Bounds>();
            {
                var allWrappers = FindObjectsOfType<TerrainWrapper>();
                foreach (var terrainWrapper in allWrappers)
                {
                    wrappers.Add(terrainWrapper, new Bounds(terrainWrapper.transform.position + terrainWrapper.Terrain.terrainData.size / 2, terrainWrapper.Terrain.terrainData.size));
                }
            }

            for (int i = 0; i < all.Length; i++)
            {
                var explicitTree = all[i];
                var wrapper = FindWrapper(explicitTree, wrappers);
                if (wrapper == null)
                {
                    Debug.LogError("Couldn't find wrapper for " + explicitTree.name, explicitTree);
                }
            }
        }

        private static TerrainWrapper FindWrapper(ExplicitTree explicitTree, Dictionary<TerrainWrapper, Bounds> wrappers)
        {
            var pos = explicitTree.transform.position;
            foreach (var wrapper in wrappers)
            {
                if (wrapper.Value.Contains(pos))
                    return wrapper.Key;
            }
            return null;
        }
    }
}