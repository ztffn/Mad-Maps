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

namespace MadMaps.Terrains
{
    public static class MMTerrainLayerUtilities
    {
        public static List<TerrainWrapper> CollectWrappers(ObjectBounds bounds)
        {
            var ret = new List<TerrainWrapper>();
            bounds.Expand(Vector3.up * 5000);
            var allColliders = Physics.OverlapBox(bounds.center, bounds.extents, bounds.Rotation);
            foreach (var allCollider in allColliders)
            {
                if (allCollider is TerrainCollider)
                {
                    ret.Add(allCollider.GetComponent<TerrainWrapper>());
                }
            }
            return ret;
        }

        public static Vector3 GetNormalFromHeightmap(this TerrainWrapper wrapper, Vector2 normalizedPos)
        {
            var tRes = wrapper.Terrain.terrainData.heightmapResolution;
            var tSize = wrapper.Terrain.terrainData.size;

            var x = Mathf.FloorToInt(normalizedPos.x * tRes);
            var z = Mathf.FloorToInt(normalizedPos.y * tRes);

            var p1 = wrapper.Terrain.HeightmapCoordToWorldPos(new Common.Coord(x, z));
            var p2 = wrapper.Terrain.HeightmapCoordToWorldPos(new Common.Coord(x, z));
            var p3 = wrapper.Terrain.HeightmapCoordToWorldPos(new Common.Coord(x + 1, z));
            var p4 = wrapper.Terrain.HeightmapCoordToWorldPos(new Common.Coord(x, z + 1));

            p1.y = wrapper.GetCompoundHeight(null, p1) * tSize.y;
            p2.y = wrapper.GetCompoundHeight(null, p2) * tSize.y;
            p3.y = wrapper.GetCompoundHeight(null, p3) * tSize.y;
            p4.y = wrapper.GetCompoundHeight(null, p4) * tSize.y;

            return Vector3.Cross(p2 - p4, p1 - p3).normalized;
        }

        public static Dictionary<SplatPrototype, SplatPrototypeWrapper> ResolvePrototypes(SplatPrototype[] prototypes)
        {
            var ret = new Dictionary<SplatPrototype, SplatPrototypeWrapper>(new SplatPrototypeWrapper.SplatPrototypeComparer());
#if UNITY_EDITOR
            var splatWrapperLookup = new Dictionary<SplatPrototype, SplatPrototypeWrapper>(new SplatPrototypeWrapper.SplatPrototypeComparer());
            var allWrapperGUIDs = AssetDatabase.FindAssets("t: SplatPrototypeWrapper");
            foreach (var allWrapperGUID in allWrapperGUIDs)
            {
                var splatWrapper =
                    AssetDatabase.LoadAssetAtPath<SplatPrototypeWrapper>(
                        AssetDatabase.GUIDToAssetPath(allWrapperGUID));
                var unityProto = splatWrapper.GetPrototype();
                if (splatWrapperLookup.ContainsKey(unityProto))
                {
                    Debug.LogWarning(String.Format("Duplicate SplatPrototypeWrappers detected: "));
                    continue;
                }
                splatWrapperLookup.Add(splatWrapper.GetPrototype(), splatWrapper);
            }
            for (var k = 0; k < prototypes.Length; ++k)
            {
                var prototype = prototypes[k];
                SplatPrototypeWrapper wrapper;
                if (!splatWrapperLookup.TryGetValue(prototype, out wrapper))
                {
                    var promptResult = EditorUtility.DisplayDialogComplex(
                            String.Format("Unable to find SplatPrototypeWrapper for {0}", prototype.texture),
                            "What would you like to do?", "Create New Wrapper", "Select Existing Wrapper", "Skip For Now");
                    if (promptResult == 0)                       
                    {
                        var rootAssetName = prototype.texture != null ? prototype.texture.name : "NullTexture";
                        var path = EditorExtensions.SaveFilePanel("Create New SplatPrototype Wrapper",
                            rootAssetName + "Wrapper", "asset");
                        if (!String.IsNullOrEmpty(path))
                        {
                            wrapper = ScriptableObject.CreateInstance<SplatPrototypeWrapper>();
                            wrapper.SetFromPrototype(prototype);
                            AssetDatabase.CreateAsset(wrapper, path);
                            AssetDatabase.Refresh();

                            splatWrapperLookup.Add(wrapper.GetPrototype(), wrapper);
                            ret.Add(wrapper.GetPrototype(), wrapper);
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else if (promptResult == 1)
                    {
                        var wrapperPath = EditorExtensions.OpenFilePanel("Get SplatPrototypeWrapper for " + prototype.texture, "asset");
                        if (!string.IsNullOrEmpty(wrapperPath))
                        {
                            wrapper = AssetDatabase.LoadAssetAtPath<SplatPrototypeWrapper>(wrapperPath);
                            ret.Add(wrapper.GetPrototype(), wrapper);
                        }
                    }
                }
                else
                {
                    ret.Add(wrapper.GetPrototype(), wrapper);
                }
            }
#endif
            return ret;
        }

        public static Dictionary<DetailPrototype, DetailPrototypeWrapper> ResolvePrototypes(DetailPrototype[] prototypes)
        {
            var ret = new Dictionary<DetailPrototype, DetailPrototypeWrapper>(new DetailPrototypeWrapper.DetailPrototypeComparer());
#if UNITY_EDITOR
            var detailWrapperLookup = new Dictionary<DetailPrototype, DetailPrototypeWrapper>(new DetailPrototypeWrapper.DetailPrototypeComparer());
            var allWrapperGUIDs = AssetDatabase.FindAssets("t: DetailPrototypeWrapper");
            foreach (var allWrapperGUID in allWrapperGUIDs)
            {
                var detailWrapper =
                    AssetDatabase.LoadAssetAtPath<DetailPrototypeWrapper>(
                        AssetDatabase.GUIDToAssetPath(allWrapperGUID));
                var unityProto = detailWrapper.GetPrototype();
                if (detailWrapperLookup.ContainsKey(unityProto))
                {
                    Debug.LogWarning(String.Format("Duplicate DetailPrototypeWrappers detected: {0} : {1}", detailWrapper, detailWrapperLookup[unityProto]), detailWrapper);
                    continue;
                }
                detailWrapperLookup.Add(unityProto, detailWrapper);
                DetailPrototypeWrapper wrapper;
                if (!detailWrapperLookup.TryGetValue(unityProto, out wrapper))
                {
                    Debug.LogError("Failed to add prototype");
                }
            }
            for (var k = 0; k < prototypes.Length; ++k)
            {
                var prototype = prototypes[k];
                DetailPrototypeWrapper wrapper;
                if (!detailWrapperLookup.TryGetValue(prototype, out wrapper))
                {
                    var promptResult = EditorUtility.DisplayDialogComplex(
                            String.Format("Unable to find DetailPrototype for {0}", prototype.prototypeTexture),
                            "What would you like to do?", "Create New Wrapper", "Select Existing Wrapper", "Skip For Now");
                    if (promptResult == 0)
                    {
                        var rootAsset = (((UnityEngine.Object)prototype.prototypeTexture ?? prototype.prototype));
                        string rootAssetPath = "NullTexture";
                        if(rootAsset != null)
                        {
                            rootAssetPath = rootAsset.name;
                        }
                        var path = EditorExtensions.SaveFilePanel("Create New DetailPrototype Wrapper", 
                            rootAssetPath + "Wrapper", "asset");
                        if (!String.IsNullOrEmpty(path))
                        {
                            wrapper = ScriptableObject.CreateInstance<DetailPrototypeWrapper>();
                            wrapper.SetFromPrototype(prototype);
                            AssetDatabase.CreateAsset(wrapper, path);
                            AssetDatabase.Refresh();

                            detailWrapperLookup.Add(prototype, wrapper);
                            ret.Add(prototype, wrapper);
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else if (promptResult == 1)
                    {
                        var wrapperPath = EditorExtensions.OpenFilePanel("Get DetailPrototypeWrapper for " + prototype.prototypeTexture, "asset");
                        if (!string.IsNullOrEmpty(wrapperPath))
                        {
                            wrapper = AssetDatabase.LoadAssetAtPath<DetailPrototypeWrapper>(wrapperPath);
                            ret.Add(wrapper.GetPrototype(), wrapper);
                        }
                    }
                }
                else
                {
                    ret.Add(wrapper.GetPrototype(), wrapper);
                }
            }
#endif
            return ret;
        }
        
        public static void SnapshotObjects(this MMTerrainLayer layer, Terrain terrain)
        {
#if UNITY_EDITOR
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
            for (var i = 0; i < allTransforms.Length; i++)
            {
                var transform = allTransforms[i];

                if (done.Contains(transform) || ignores.Contains(transform))
                {
                    continue;
                }

                var ws = transform.GetComponentInAncestors<LayerComponentBase>();
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

#if UNITY_2018_2_OR_NEWER
                var prefabAsset = PrefabUtility.FindPrefabRoot(PrefabUtility.GetCorrespondingObjectFromSource(go) as GameObject);
#else
                var prefabAsset = PrefabUtility.FindPrefabRoot(PrefabUtility.GetPrefabParent(go) as GameObject);
#endif
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
                    Scale = rootInScene.transform.localScale,
                    ContainerMetadata = layer.name,
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

        public static void SnapshotTrees(this MMTerrainLayer layer, Terrain terrain)
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

        public static void SnapshotDetails(this MMTerrainLayer layer, Terrain terrain)
        {
            layer.DetailData.Clear();
            var prototypes = terrain.terrainData.detailPrototypes;
            var dRes = terrain.terrainData.detailResolution;
            var wrapperLookup = ResolvePrototypes(prototypes);
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

        public static void SnapshotSplats(this MMTerrainLayer layer, Terrain terrain)
        {
            var rawSplats = terrain.terrainData.GetAlphamaps(0, 0, terrain.terrainData.alphamapWidth,
                terrain.terrainData.alphamapHeight);
            layer.SplatData.Clear();

            var splatWrapperLookup = ResolvePrototypes(terrain.terrainData.splatPrototypes);

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

        public static void SnapshotHeights(this MMTerrainLayer layer, Terrain terrain)
        {
            layer.Heights =
                new Serializable2DFloatArray(terrain.terrainData.GetHeights(0, 0, terrain.terrainData.heightmapResolution,
                    terrain.terrainData.heightmapResolution).Flip());
        }
    }
}