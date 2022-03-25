using MadMaps.WorldStamps.Authoring;
using System.Linq;
using MadMaps.Roads;
using MadMaps.Common;
using System;
using System.Collections.Generic;
using MadMaps.Common.Collections;
using MadMaps.WorldStamps;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/*
namespace MadMaps.Terrains
{
    public static class DeltaLayerUtilities
    {   
        public static void SnapshotObjects(this DeltaLayer layer, TerrainWrapper wrapper)
        {
#if UNITY_EDITOR
            var terrain = wrapper.Terrain;
            var terrainSize = terrain.terrainData.size;
            var terrainPos = terrain.GetPosition();
            var terrainBounds = new Bounds(terrainPos, Vector3.zero);
            terrainBounds.Encapsulate(terrainPos + terrainSize);
            terrainBounds.Expand(Vector3.up * 5000);

            var allTransforms = UnityEngine.Object.FindObjectsOfType<Transform>();
            var done = new HashSet<Transform>();
            allTransforms = allTransforms.OrderBy(transform => TransformExtensions.GetHierarchyDepth(transform)).ToArray();
            HashSet<Transform> ignores = new HashSet<Transform>();

            layer.Objects.Clear();
            layer.ObjectRemovals.Clear();
            for (var i = 0; i < allTransforms.Length; i++)
            {
                var transform = allTransforms[i];

                if (done.Contains(transform) || ignores.Contains(transform))
                {
                    continue;
                }

                var ws = transform.GetComponentInAncestors<WorldStamp.WorldStamp>();
                if (ws)
                {
                    //Debug.Log(string.Format("WorldStamp Object Capture : Ignored {0} as it contained a WorldStamp. Recursive WorldStamps are currently not supported.", transform), transform);
                    ignores.Add(transform);
                    var children = ws.transform.GetComponentsInChildren<Transform>(true);
                    foreach (var ignore in children)
                    {
                        ignores.Add(ignore);
                    }
                    continue;
                }
                var rn = transform.GetComponentInAncestors<RoadNetwork>();
                if (rn)
                {
                    ignores.Add(rn.transform);
                    var children = rn.transform.GetComponentsInChildren<Transform>(true);
                    foreach (var ignore in children)
                    {
                        ignores.Add(ignore);
                    }
                    continue;
                }
                var template = transform.GetComponentInAncestors<WorldStampTemplate>();
                if (template)
                {
                    ignores.Add(template.transform);
                    var children = template.transform.GetComponentsInChildren<Transform>(true);
                    foreach (var ignore in children)
                    {
                        ignores.Add(ignore);
                    }
                    continue;
                }

                var go = transform.gameObject;
                var prefabRoot = PrefabUtility.GetPrefabObject(go);
                if (prefabRoot == null)
                {
                    //Debug.LogError("Unable to collect non-prefab object: " + go.name, go);
                    continue;
                }

                var prefabAsset = PrefabUtility.FindPrefabRoot(PrefabUtility.GetPrefabParent(go) as GameObject);
                var rootInScene = PrefabUtility.FindPrefabRoot(go);
                
                var relativePos = rootInScene.transform.position - terrainPos;
                relativePos = new Vector3(relativePos.x / terrainSize.x,
                    (rootInScene.transform.position.y - terrain.SampleHeight(rootInScene.transform.position)) -
                    terrainPos.y,
                    relativePos.z / terrainSize.z);



                layer.Objects.Add(new PrefabObjectData
                {
                    Guid = Guid.NewGuid().ToString(),
                    Prefab = prefabAsset,
                    Position = relativePos,
                    Rotation = rootInScene.transform.localRotation.eulerAngles,
                    Scale = rootInScene.transform.localScale
                });

                done.Add(rootInScene.transform);
                var doneChildren = rootInScene.transform.GetComponentsInChildren<Transform>(true);
                foreach (var item in doneChildren)
                {
                    done.Add(item);
                }
            }
#endif
        }

        public static void SnapshotTrees(this DeltaLayer layer, TerrainWrapper terrain)
        {
            layer.Trees.Clear();
            var tSize = terrain.terrainData.size;
            var trees = terrain.terrainData.treeInstances;
            var prototypes = new List<TreePrototype>(terrain.terrainData.treePrototypes);
            for (var i = 0; i < trees.Length; i++)
            {
                var wPos = terrain.TreeToWorldPos(trees[i].position);
                var h = terrain.SampleHeight(wPos);
                var newInstance = new MadMapsTreeInstance(trees[i], prototypes);
                newInstance.Position.y = (newInstance.Position.y * tSize.y) - h;
                layer.Trees.Add(newInstance);
            }
        }

        public static void SnapshotDetails(this DeltaLayer layer, TerrainWrapper terrain)
        {
            layer.DetailData.Clear();
            var prototypes = terrain.terrainData.detailPrototypes;
            var dRes = terrain.terrainData.detailResolution;
            var wrapperLookup = MMTerrainLayerUtilities.ResolvePrototypes(prototypes);
            for (var i = 0; i < prototypes.Length; i++)
            {
                DetailPrototypeWrapper wrapper;
                var prototype = prototypes[i];
                if (!wrapperLookup.TryGetValue(prototype, out wrapper))
                {
                    continue;
                }

                var data = new Serializable2DByteArray(dRes, dRes);
                var detailMap = terrain.terrainData.GetDetailLayer(0, 0, terrain.terrainData.detailWidth,
                    terrain.terrainData.detailHeight, i);
                int sum = 0;
                for (var u = 0; u < detailMap.GetLength(0); u++)
                {
                    for (var v = 0; v < detailMap.GetLength(1); v++)
                    {
                        var sample = (byte)detailMap[u, v];
                        data[v, u] = sample;
                        sum += sample;
                    }
                }

                if (sum > 0)
                {
                    layer.DetailData.Add(wrapper, data);
                }
            }
        }

        public static void SnapshotSplats(this DeltaLayer layer, TerrainWrapper terrain)
        {
            var rawSplats = terrain.terrainData.GetAlphamaps(0, 0, terrain.terrainData.alphamapWidth,
                terrain.terrainData.alphamapHeight);
            layer.SplatData.Clear();

            var splatWrapperLookup = MMTerrainLayerUtilities.ResolvePrototypes(terrain.terrainData.splatPrototypes);

            for (var k = 0; k < rawSplats.GetLength(2); ++k)
            {
                var prototype = terrain.terrainData.splatPrototypes[k];
                SplatPrototypeWrapper wrapper;
                if (!splatWrapperLookup.TryGetValue(prototype, out wrapper))
                {
                    continue;
                }
                var data = new Serializable2DByteArray(terrain.terrainData.alphamapResolution,
                    terrain.terrainData.alphamapResolution);
                float sum = 0;
                for (var u = 0; u < rawSplats.GetLength(0); ++u)
                {
                    for (var v = 0; v < rawSplats.GetLength(1); ++v)
                    {
                        var sample = (byte)Mathf.Clamp(rawSplats[u, v, k] * 255f, 0, 255);
                        data[v, u] = sample;
                        sum += sample;
                    }
                }
                if (sum > 0)
                {
                    layer.SplatData[wrapper] = data;
                }
            }
        }

        public static void SnapshotHeights(this DeltaLayer layer, TerrainWrapper terrain)
        {
            var compoundHeights = terrain

            layer.Heights =
                new Serializable2DFloatArray(terrain.terrainData.GetHeights(0, 0, terrain.terrainData.heightmapWidth,
                    terrain.terrainData.heightmapHeight).Flip());
        }
    }
}
*/