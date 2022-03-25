using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using MadMaps.Common;
using MadMaps.Common.Collections;
using MadMaps.WorldStamps;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace MadMaps.Terrains
{
#if HURTWORLDSDK
    [StripComponentOnBuild]
#endif
    [ExecuteInEditMode]
    [HelpURL("http://lrtw.net/madmaps/index.php?title=Terrain_Wrapper")]
    public partial class TerrainWrapper : MonoBehaviour
#if HURTWORLDSDK
        , ILevelPreBuildStepCallback
#endif
    {
        
        public Action<TerrainWrapper> OnPreRecalculate;
        public Action<TerrainWrapper> OnPreFinalise;
        public Action<TerrainWrapper> OnPostFinalise;
        public Action<TerrainWrapper> OnFrameAfterPostFinalise;
        private bool _needsPostRecalcInvokation;

        public bool Locked = false;
        public bool WriteHeights = true;
        public bool WriteSplats = true;
        public bool WriteTrees = true;
        public bool WriteDetails = true;
        public bool WriteObjects = true;

        public List<LayerBase> Layers = new List<LayerBase>();

        public List<SplatPrototypeWrapper> SplatPrototypes = new List<SplatPrototypeWrapper>();
            // Canon list of splat prototypes (can contain more than in compound data, e.g. user added)

        public List<DetailPrototypeWrapper> DetailPrototypes = new List<DetailPrototypeWrapper>();
            // Same, but for detail prototypes

        public GameObject ObjectContainer; // The container for prefab objects

        private Dictionary<LayerBase, CompoundMMTerrainLayer> _compoundDataCache =
            new Dictionary<LayerBase, CompoundMMTerrainLayer>();

        public BakedTerrainData CompoundTerrainData
        {
            get
            {
                if (_compoundTerrainData == null)
                {
                    _compoundTerrainData = ScriptableObject.CreateInstance<BakedTerrainData>();
                    _compoundTerrainData.name = name + "CompoundLayer";
                }
                return _compoundTerrainData;
            }
        } // The final data that is written to the terrain
        [SerializeField] private BakedTerrainData _compoundTerrainData;

        public Terrain Terrain
        {
            get { return GetComponent<Terrain>(); }
        }

        public bool Dirty { get; set; } // If true, the terrain wrapper will update in the next frame (in editor)

#if UNITY_EDITOR
#if HURTWORLDSDK
        [UnityEditor.MenuItem("CONTEXT/Terrain/Setup for Hurtworld")]
        public static void SetupOnTerrain(UnityEditor.MenuCommand command)
        {
            var t = command.context as Terrain;
            t.gameObject.GetOrAddComponent<TerrainWrapper>();
            t.gameObject.GetOrAddComponent<TerrainShaderManager>();
            t.gameObject.GetOrAddComponent<TerrainSettingsManager>();
        }
#endif
        public static bool ComputeShaders
        {
            get { return UnityEditor.EditorPrefs.GetBool("TerrainWrapper_ComputeShaders", true); }
            set { UnityEditor.EditorPrefs.SetBool("TerrainWrapper_ComputeShaders", value); }
        }
#else
        public static bool ComputeShaders{get;set;}
#endif

        public void Update()
        {
            if (_needsPostRecalcInvokation)
            {
                _needsPostRecalcInvokation = false;
                OnFrameAfterPostFinalise.SafeInvoke(this);
            }

            if (Dirty)
            {
                Dirty = false;
                ApplyAllLayers();
            }
        }

        public void ApplyAllLayers()
        {
            if (Layers.Count == 0)
            {
                return;
            }

            if (!WriteHeights && !WriteDetails && !WriteObjects && !WriteSplats && !WriteTrees)
            {
                Debug.LogWarning("Nothing for wrapper to do! Maybe you disabled all the flags in TerrainWrapper > Info?");
                return;
            }

            var sw = new Stopwatch();
            sw.Start();

            var size = Terrain.terrainData.size;

            if(!PrepareApply())
            {
                return;
            }

            OnPreRecalculate.SafeInvoke(this);
            
            for (var i = Layers.Count - 1; i >= 0; i--)
            {
                if (!Layers[i].Enabled)
                {
                    Layers[i].Dispose(this, true);
                    continue;
                }
                Layers[i].WriteToTerrain(this);
            }

            Terrain.terrainData.size = size;

            OnPreFinalise.SafeInvoke(this);

            UnityEngine.Profiling.Profiler.BeginSample("FinaliseApply");
            FinaliseApply();
            UnityEngine.Profiling.Profiler.EndSample();
            GC.Collect(3, GCCollectionMode.Forced);

            OnPostFinalise.SafeInvoke(this);

            ComputeShaderPool.ClearPool();

            Debug.Log(string.Format("Applied all layers at {0} (took {1}) (Compute Shaders {2})",
                DateTime.Now.ToShortTimeString(), sw.Elapsed, (ComputeShaders && BlendMMTerrainLayerUtility.ShouldCompute()) ? "ON" : "OFF"));

            _needsPostRecalcInvokation = true;
        }

        public bool PrepareApply()
        {
            if (Layers.Count == 0)
            {
                return false;
            }

            if(!Terrain || !Terrain.terrainData)
            {
                Debug.LogError(string.Format("No terrain found for Terrain Wrapper {0}! Aborting Apply.", this), this);
                return false;
            }

            CompoundTerrainData.Clear(this);

            /*var lastSetLayer = Layers.Last((layer) => 
            {
                if(!(layer is MMTerrainLayer))
                {
                    return false;
                }
                return (layer as MMTerrainLayer).BlendMode == MMTerrainLayer.EMMTerrainLayerBlendMode.Set;
            });
            Debug.Log(lastSetLayer);
            if(lastSetLayer.Heights != null)
            {
                Terrain.Layer
            }*/

            for (var i = 0; i < Layers.Count; ++i)
            {
                if(Layers[i])
                {
                    Layers[i].PrepareApply(this, i);
                }
            }

#if VEGETATION_STUDIO
            PrepareVegetationStudio();
#endif
            return true;
        }

        public void FinaliseApply()
        {
            FinaliseHeights();

            FinaliseSplats();

            FinaliseDetails();

            FinaliseTrees();

            FinaliseObjects();

#if VEGETATION_STUDIO
            FinaliseVegetationStudio();
#endif
            Terrain.Flush();
#if UNITY_EDITOR            
            if(!Application.isPlaying)
            {
                UnityEditor.EditorUtility.SetDirty(this);
                UnityEditor.EditorUtility.SetDirty(CompoundTerrainData);
                UnityEditor.EditorUtility.ClearProgressBar();
                UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
            }            
#endif
        }

        /*public void CullTrees()
        {
            var gradient = MiscUtilities.GetNormalsFromHeightmap(CompoundTerrainData.Heights, Terrain.terrainData.size.y);
            var trees = new List<TreeInstance>(Terrain.terrainData.treeInstances);
            var counter = 0;
            for (var i = trees.Count - 1; i >= 0; i--)
            {
                var tree = trees[i];
                var wPos = Terrain.TreeToWorldPos(tree.position);
                var norm = gradient.BilinearSample(new Vector2(tree.position.z, tree.position.x));
                if (norm.y < CullYNormal)
                {
                    //Debug.DrawLine(wPos, wPos + Vector3.up*200, Color.green, 20);
                    trees.RemoveAt(i);
                    counter++;
                    continue;
                }
                RaycastHit hit;
                if (!Physics.Raycast(wPos + Vector3.up*200, Vector3.down, out hit, 300))
                {
                    //Debug.DrawLine(wPos, wPos + Vector3.up*200, Color.magenta, 20);
                    continue;
                }
                if (Math.Abs(hit.distance - 200f) > 1)
                {
                    //Debug.DrawLine(wPos, wPos + Vector3.up*200, Color.red, 20);
                    trees.RemoveAt(i);
                    counter++;
                }
            }
            Terrain.terrainData.treeInstances = trees.ToArray();
            Terrain.Flush();
            Debug.Log(string.Format("Culled {0} trees", counter));
        }*/

        private Dictionary<GameObject, Queue<GameObject>> GenerateObjectContainerCacheRecursvive(Transform root, Dictionary<GameObject, Queue<GameObject>> cache = null)
        {
            if(cache == null) 
            {
                cache = new Dictionary<GameObject, Queue<GameObject>>();
            }
            foreach(Transform instantiatedObject in root)
            {
                //var instantiatedObject = CompoundTerrainData.OwnedInstantiatedObjects[i];
                if (!instantiatedObject)
                {
                    continue;
                }
#if UNITY_EDITOR
#if UNITY_2018_2_OR_NEWER
                var prefab = UnityEditor.PrefabUtility.FindPrefabRoot(UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(instantiatedObject.gameObject) as GameObject);
#else
                var prefab = UnityEditor.PrefabUtility.FindPrefabRoot(UnityEditor.PrefabUtility.GetPrefabParent(instantiatedObject.gameObject) as GameObject);
#endif
                UnityEditor.PrefabUtility.RevertPrefabInstance(instantiatedObject.gameObject);
                
#else
                GameObject prefab = null;
#endif
                if (prefab != null)
                {
                    Queue<GameObject> queue;
                    if (!cache.TryGetValue(prefab, out queue))
                    {
                        queue = new Queue<GameObject>();
                        cache[prefab] = queue;
                    }
                    queue.Enqueue(instantiatedObject.gameObject);
                    //Debug.Log(instantiatedObject, instantiatedObject);
                }
                else
                {
                    cache = GenerateObjectContainerCacheRecursvive(instantiatedObject, cache);
                }                
            }
            return cache;
        }

        /// <summary>
        ///     Instantiate the prefab objects in CompoundData
        /// </summary>
        private void FinaliseObjects()
        {
            if (!WriteObjects)
            {
                /*if (ObjectContainer)
                {
                    UnityEngine.Object.DestroyImmediate(ObjectContainer);
                }*/
                return;
            }

            if (CompoundTerrainData.Objects.Count == 0)
            {
                if(ObjectContainer)
                {
                    UnityEngine.Object.DestroyImmediate(ObjectContainer);
                }                
                return;
            }

            if (ObjectContainer == null)
            {
                ObjectContainer = new GameObject(string.Format("{0}_ObjectContainer", name));
            }

            var tPos = transform.position;
            var tSize = Terrain.terrainData.size;
            // Objects
            MiscUtilities.ProgressBar("Applying Final Objects", "", 0.8f);
            var cache = GenerateObjectContainerCacheRecursvive(ObjectContainer.transform);
            //Debug.Log(cache.Count);
            int missingCount = 0;
            //CompoundTerrainData.OwnedInstantiatedObjects.Clear();
            foreach (var keyValuePair in CompoundTerrainData.Objects)
            {
                var instantiatedObjectData = keyValuePair.Value;
                var prefabObjectData = instantiatedObjectData.Data;
                if (prefabObjectData.Prefab == null)
                {
                    missingCount++;
                    continue;
                }

#if UNITY_EDITOR
                var root = UnityEditor.PrefabUtility.FindPrefabRoot(prefabObjectData.Prefab);
                if (root != prefabObjectData.Prefab)
                {
                    Debug.LogError("Layer {0} references a prefab sub object! This is not supported.");
                    continue;
                }
#endif

                var worldPos = tPos + new Vector3(prefabObjectData.Position.x*tSize.x,
                                   prefabObjectData.Position.y,
                                   prefabObjectData.Position.z*tSize.z);
                if (!prefabObjectData.AbsoluteHeight)
                {
                    worldPos.y += Terrain.SampleHeight(worldPos);
                }

                Queue<GameObject> cacheList;
                GameObject newObj = null;
                if (cache.TryGetValue(prefabObjectData.Prefab, out cacheList) && cacheList.Count > 0)
                {
                    newObj = cacheList.Dequeue();
                }
#if UNITY_EDITOR
                else
                {
                    newObj = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefabObjectData.Prefab);
                }
#endif
                if (newObj == null)
                {
                    Debug.LogError("Failed to instantiate prefab " + prefabObjectData.Prefab);
                    continue;
                }

                newObj.transform.position = worldPos;
                newObj.transform.localScale = prefabObjectData.Scale;
                newObj.transform.rotation = Quaternion.Euler(prefabObjectData.Rotation);

                var parent = ObjectContainer.transform;
                if(!string.IsNullOrEmpty(prefabObjectData.ContainerMetadata))
                {
                    parent = ObjectContainer.transform.Find(prefabObjectData.ContainerMetadata);
                    if(!parent)
                    {
                        Transform currentTraversal = ObjectContainer.transform;
                        var split = prefabObjectData.ContainerMetadata.Split('/');
                        for(var i = 0; i < split.Length; ++i)
                        {
                            var child = currentTraversal.Find(split[i]);
                            if(!child)
                            {
                                child = new GameObject(split[i]).transform;
                                child.SetParent(currentTraversal);
                            }
                            currentTraversal = child;
                        }
                        parent = currentTraversal;
                    }
                }

                newObj.transform.SetParent(parent);
                instantiatedObjectData.InstantiatedObject = newObj;
                //CompoundTerrainData.OwnedInstantiatedObjects.Add(newObj);
            }

            if (missingCount > 0)
            {
                Debug.LogWarning(string.Format("Tried to instantiate {0} null objects in FinaliseObjects!", missingCount));
            }

            var destroyList = new List<GameObject>();
            foreach (var keyValuePair in cache)
            {
                foreach (var item in keyValuePair.Value)
                {
                    destroyList.Add(item);
                }
            }
            for (var i = 0; i < destroyList.Count; i++)
            {
                DestroyImmediate(destroyList[i]);
            }

            GameObjectExtensions.DestroyEmptyObjectsInHierarchy(ObjectContainer.transform);
        }

        /// <summary>
        ///     Instantiate the trees in CompoundData
        /// </summary>
        public void FinaliseTrees()
        {
            if (!WriteTrees)
            {
                return;
            }

            // Trees
            MiscUtilities.ProgressBar("Applying Final Trees", "", 0.6f);

            var tSize = Terrain.terrainData.size;
            var treeInstances = new List<TreeInstance>();
            var treePrototypes = new List<TreePrototype>();
            foreach (var compoundTree in CompoundTerrainData.Trees)
            {
                if (compoundTree.Value.Prototype == null)
                {
                    continue;
                }

                var treeClone = compoundTree.Value.Clone();
                var wPos = Terrain.TreeToWorldPos(treeClone.Position);
                var height = GetCompoundHeight(null, wPos);

                treeClone.Position.y += height * tSize.y;
                treeClone.Position.y /= tSize.y;
                treeInstances.Add(treeClone.ToUnityTreeInstance(treePrototypes));

                //DebugHelper.DrawPoint(Terrain.TreeToWorldPos(compoundTree.Value.Position), 0.1f, Color.green, 10);
            }
            Terrain.terrainData.treePrototypes = treePrototypes.ToArray();
            Terrain.terrainData.treeInstances = treeInstances.ToArray();
        }

        /// <summary>
        ///     Refresh details and apply to terrain data
        /// </summary>
        private void FinaliseDetails()
        {
            if (!WriteDetails)
            {
                return;
            }

            // Details
            MiscUtilities.ProgressBar("Applying Final Details", "", 0.4f);
            foreach (var details in CompoundTerrainData.DetailData)
            {
                if (!DetailPrototypes.Contains(details.Key))
                {
                    DetailPrototypes.Add(details.Key);
                }
            }
            DetailPrototypes = RefreshDetails();
            if (CompoundTerrainData.DetailData.Count > 0 && DetailPrototypes.Count > 0)
            {
                foreach (var details in CompoundTerrainData.DetailData)
                {
                    var index = DetailPrototypes.IndexOf(details.Key);
                    if (index < 0 || index >= Terrain.terrainData.detailPrototypes.Length)
                    {
                        Debug.LogError("Failed to add DetailPrototypeWrapper " + details.Key);
                    }
                    var data = details.Value.DeserializeToInt().Flip();
                    Terrain.terrainData.SetDetailLayer(0, 0, index, data);
                }
            }
        }

        /// <summary>
        ///     Refresh heights and apply to terrain data
        /// </summary>
        private void FinaliseHeights()
        {
            if (!WriteHeights || CompoundTerrainData.Heights == null)
            {
                return;
            }

            MiscUtilities.ProgressBar("Applying Final Height", "", 0.2f);

            // Heights
            var hRes = CompoundTerrainData.Heights.Width;
            Terrain.terrainData.heightmapResolution = hRes;

            var flipHeight = CompoundTerrainData.Heights != null
                ? CompoundTerrainData.Heights.Deserialize()
                : new float[hRes, hRes];
            flipHeight = flipHeight.Flip();

            Terrain.terrainData.SetHeights(0, 0, flipHeight);
            Terrain.ApplyDelayedHeightmapModification();
        }

        /// <summary>
        ///     Refresh splats and apply to terrain data
        /// </summary>
        private void FinaliseSplats()
        {
            if(!WriteSplats)
            {
                return;
            }
            MiscUtilities.ProgressBar("Applying Final Splats", "", 0.3f);
            // Splats
            foreach (var compoundSplat in CompoundTerrainData.SplatData)
            {
                if (!SplatPrototypes.Contains(compoundSplat.Key))
                {
                    SplatPrototypes.Add(compoundSplat.Key);
                }
            }

            var sw = new Stopwatch();
            sw.Start();

            SplatPrototypes = RefreshSplats();
            if (CompoundTerrainData.SplatData.Count > 0 && SplatPrototypes.Count > 0)
            {
                var aRes = CompoundTerrainData.SplatData.GetValues().First().Width;
                if (ComputeShaders && BlendMMTerrainLayerUtility.ShouldCompute())
                {
                    const int maxChunkSize = 2048*2048*4;
                    var subdivisions = Mathf.CeilToInt((aRes*aRes*SplatPrototypes.Count)/(float) maxChunkSize);
                    var subRes = aRes/subdivisions;

                    var splats = new float[subRes, subRes, SplatPrototypes.Count];

                    var combineShader = Resources.Load<ComputeShader>("MadMaps/WorldStamp/ComputeShaders/CombineSplats");
                    var combineShaderKernel = combineShader.FindKernel("CombineSplats");

                    var normalizeShader = Resources.Load<ComputeShader>("MadMaps/WorldStamp/ComputeShaders/NormalizeSplats");
                    var normalizeShaderKernel = normalizeShader.FindKernel("Normalize");

                    var dataBuffer = new ComputeBuffer(subRes*subRes*SplatPrototypes.Count, sizeof (float));
                    var splatLayerData = new ComputeBuffer(aRes*aRes, sizeof (int));
                    var intData = new int[aRes*aRes];

                    for (var u = 0; u < subdivisions; u++)
                    {
                        for (var v = 0; v < subdivisions; v++)
                        {
                            foreach (var compoundSplat in CompoundTerrainData.SplatData)
                            {
                                var index = SplatPrototypes.IndexOf(compoundSplat.Key);
                                if (index < 0)
                                {
                                    throw new Exception("Failed to add SplatPrototypeWrapper?");
                                }
                                var data = compoundSplat.Value;
                                data.Data.ConvertToIntArray(ref intData);

                                dataBuffer.SetData(splats);
                                splatLayerData.SetData(intData);

                                combineShader.SetBuffer(combineShaderKernel, "_WriteData", dataBuffer);
                                combineShader.SetVector("_WriteDataSize",
                                    new Vector4(subRes, subRes, SplatPrototypes.Count));

                                combineShader.SetBuffer(combineShaderKernel, "_ReadData", splatLayerData);
                                combineShader.SetVector("_ReadDataSize", new Vector4(aRes, aRes, SplatPrototypes.Count));

                                combineShader.SetVector("_Offset", new Vector2(u*subRes, v*subRes));
                                combineShader.SetFloat("_Index", index);

                                combineShader.Dispatch(combineShaderKernel, data.Width, data.Height, 1);
                                dataBuffer.GetData(splats);
                            }

                            dataBuffer.SetData(splats);

                            normalizeShader.SetBuffer(combineShaderKernel, "_WriteData", dataBuffer);
                            normalizeShader.SetVector("_Size", new Vector4(subRes, subRes, SplatPrototypes.Count));
                            normalizeShader.Dispatch(normalizeShaderKernel, subRes, subRes, SplatPrototypes.Count);
                            dataBuffer.GetData(splats);
                            //splats.Normalize();

                            Terrain.terrainData.SetAlphamaps(u*subRes, v*subRes, splats);

                            /*for (int index00 = 0; index00 < splats.GetLength(0); index00++)
                                for (int index01 = 0; index01 < splats.GetLength(1); index01++)
                                    for (int index02 = 0; index02 < splats.GetLength(2); index02++)
                                    {
                                        splats[index00, index01, index02] = 0;
                                    }*/
                            //break;
                        }
                        //break;
                    }
                    dataBuffer.Dispose();
                    splatLayerData.Dispose();
                }
                else
                {
                    var splats = new float[aRes, aRes, SplatPrototypes.Count];
                    foreach (var compoundSplat in CompoundTerrainData.SplatData)
                    {
                        var index = SplatPrototypes.IndexOf(compoundSplat.Key);
                        if (index < 0)
                        {
                            throw new Exception("Failed to add SplatPrototypeWrapper?");
                        }
                        var boost = compoundSplat.Key.Multiplier;
                        var data = compoundSplat.Value;
                        for (var u = 0; u < data.Width; ++u)
                        {
                            for (var v = 0; v < data.Height; ++v)
                            {
                                var sample = data[u, v];
                                var sampleF = sample/255f;
                                sampleF *= boost;
                                splats[v, u, index] = sampleF;
                            }
                        }
                    }
                    splats.Normalize();
                    Terrain.terrainData.SetAlphamaps(0, 0, splats);
                }
            }
        }

        public T GetLayer<T>(string layerName, bool warnIfMissing = false, bool createIfMissing = false) where T:LayerBase
        {
            return GetLayer(typeof(T), layerName, warnIfMissing, createIfMissing) as T;
        }

        public LayerBase GetLayer(Type t, string layerName, bool warnIfMissing = false, bool createIfMissing = false)
        {
            foreach (var layer in Layers)
            {
                if (layer != null && layer.name == layerName && layer.GetType().IsAssignableFrom(t))
                {
                    return layer;
                }
            }
            if (createIfMissing)
            {
                var layer = ScriptableObject.CreateInstance(t) as LayerBase;
                layer.name = layerName;
                Layers.Insert(0, layer);
                return layer;
            }
            if (warnIfMissing)
            {
                Debug.LogWarning(string.Format("Missing layer {0} on TerrainDataSnapshot {1}", layerName, name), this);
            }
            return null;
        }

        public static TerrainWrapper GetWrapper(Vector3 pos)
        {
            var allT = FindObjectsOfType<Terrain>();
            for (var i = 0; i < allT.Length; i++)
            {
                var terrain = allT[i];
                var b = terrain.GetComponent<TerrainCollider>().bounds;
                b.Expand(Vector3.up*float.MaxValue);
                if (b.Contains(pos))
                {
                    return terrain.GetComponent<TerrainWrapper>();
                }
            }
            //Debug.LogError("Failed to find terrain!");
            return null;
        }

        public static void WeldHeights(Serializable2DFloatArray heights, MMTerrainLayer prevX, MMTerrainLayer nextZ,
            MMTerrainLayer nextX, MMTerrainLayer prevZ, int margin)
        {
            if (heights == null)
            {
                return;
            }

            var heightSize = heights.Width;
            if (margin == 0) return;

            //creating delta heights
            var deltaHeights = new float[heightSize, heightSize];

            //prev x
            if (prevX != null)
            {
                var nStrip = prevX.GetHeights(0, heightSize - 1, heightSize, 1, heightSize);
                if (nStrip != null)
                {
                    for (var z = 0; z < heightSize; z++)
                    {
                        var delta = nStrip[z, 0] - (heights[z, 0] + deltaHeights[z, 0]);

                        //float percentFromSide = Mathf.Min( Mathf.Clamp01(1f*z/margin),  Mathf.Clamp01(1 - 1f*(z-(heightSize-1-margin))/margin) );
                        //float invPercentFromSide = 2000000000; if (percentFromSide > 0.0001f) invPercentFromSide = 1f/percentFromSide;

                        deltaHeights[z, 0] = delta;
                        //deltaHeights[z,1] = heights[z,0] + delta + vector - heights[z,1];

                        for (var x = 1; x < margin; x++)
                        {
                            var percent = 1 - 1f*x/margin;
                            //if (percentFromSide < 0.999f) percent = Mathf.Pow(percent, invPercentFromSide);
                            percent = 3*percent*percent - 2*percent*percent*percent;

                            deltaHeights[z, x] += delta*percent;
                        }
                    }
                }
            }

            //next x
            if (nextX != null)
            {
                var nStrip = nextX.GetHeights(0, 0, heightSize, 1, heightSize);
                if (nStrip != null)
                {
                    for (var z = 0; z < heightSize; z++)
                    {
                        var delta = nStrip[z, 0] - heights[z, heightSize - 1] - deltaHeights[z, heightSize - 1];

                        //float percentFromSide = Mathf.Min( Mathf.Clamp01(1f*z/margin),  Mathf.Clamp01(1 - 1f*(z-(heightSize-1-margin))/margin) );
                        //float invPercentFromSide = 2000000000; if (percentFromSide > 0.0001f) invPercentFromSide = 1f/percentFromSide;

                        for (var x = heightSize - margin; x < heightSize; x++)
                        {
                            var percent = 1 - 1f*(heightSize - x - 1)/margin;
                            //if (percentFromSide < 0.999f) percent = Mathf.Pow(percent, invPercentFromSide);
                            percent = 3*percent*percent - 2*percent*percent*percent;

                            deltaHeights[z, x] += delta*percent;
                        }
                    }
                }
            }

            //prev z
            if (prevZ != null)
            {
                var nStrip = prevZ.GetHeights(heightSize - 1, 0, 1, heightSize, heightSize);
                if (nStrip != null)
                {
                    for (var x = 0; x < heightSize; x++)
                    {
                        var delta = nStrip[0, x] - heights[0, x] - deltaHeights[0, x];

                        var percentFromSide = Mathf.Min(Mathf.Clamp01(1f*x/margin),
                            Mathf.Clamp01(1 - 1f*(x - (heightSize - 1 - margin))/margin));
                        float invPercentFromSide = 2000000000;
                        if (percentFromSide > 0.0001f) invPercentFromSide = 1f/percentFromSide;

                        for (var z = 0; z < margin; z++)
                        {
                            var percent = 1 - 1f*z/margin;
                            if (percentFromSide < 0.999f) percent = Mathf.Pow(percent, invPercentFromSide);
                            percent = 3*percent*percent - 2*percent*percent*percent;

                            deltaHeights[z, x] += delta*percent;
                        }
                    }
                }
            }

            //next z
            if (nextZ != null)
            {
                var nStrip = nextZ.GetHeights(0, 0, 1, heightSize, heightSize);

                if (nStrip != null)
                {
                    for (var x = 0; x < heightSize; x++)
                    {
                        var delta = nStrip[0, x] - heights[heightSize - 1, x] - deltaHeights[heightSize - 1, x];

                        var percentFromSide = Mathf.Min(Mathf.Clamp01(1f*x/margin),
                            Mathf.Clamp01(1 - 1f*(x - (heightSize - 1 - margin))/margin));
                        float invPercentFromSide = 2000000000;
                        if (percentFromSide > 0.0001f) invPercentFromSide = 1f/percentFromSide;

                        for (var z = heightSize - margin; z < heightSize; z++)
                        {
                            var percent = 1 - 1f*(heightSize - z - 1)/margin;
                            if (percentFromSide < 0.999f) percent = Mathf.Pow(percent, invPercentFromSide);
                            percent = 3*percent*percent - 2*percent*percent*percent;

                            deltaHeights[z, x] += delta*percent;
                        }
                    }
                }
            }

            //saving delta heights
            for (var z = 0; z < heightSize; z++)
                for (var x = 0; x < heightSize; x++)
                    heights[z, x] += deltaHeights[z, x];
        }

        public void Clear()
        {
            for (var i = Layers.Count - 1; i >= 0; i--)
            {
                Layers[i].Clear(this);
            }
        }

        public Serializable2DFloatArray GetCompoundHeights(LayerBase terminatingLayer, int x, int z, int width,
            int height, int heightRes)
        {
            if (terminatingLayer != null && _compoundDataCache != null && _compoundDataCache.ContainsKey(terminatingLayer))
            {
                var compoundData = _compoundDataCache[terminatingLayer];
                if (compoundData.Heights != null)
                {
                    return compoundData.Heights.Select(x, z, width, height);
                }
            }

            UnityEngine.Profiling.Profiler.BeginSample("GetCompoundHeights");
            Serializable2DFloatArray result = null;
            for (var i = Layers.Count - 1; i >= 0; i--)
            {
                var layer = Layers[i];
                if (layer == null)
                {
                    Layers.RemoveAt(i);
                    continue;
                }
                if (!layer.Enabled)
                {
                    continue;
                }
                if (layer == terminatingLayer)
                {
                    break;
                }
                result = layer.BlendHeights(x, z, width, height, heightRes, result);
            }
            UnityEngine.Profiling.Profiler.EndSample();
            return result;
        }

        public Serializable2DByteArray GetCompoundSplat(LayerBase terminatingLayer, SplatPrototypeWrapper splat,
            int x, int z, int width, int height, bool includeTerminatingLayer)
        {
            var aRes = Terrain.terrainData.alphamapResolution;
            if (terminatingLayer != null && _compoundDataCache.ContainsKey(terminatingLayer))
            {
                var data = _compoundDataCache[terminatingLayer].SplatData;
                Serializable2DByteArray splatLayer;
                if (data.TryGetValue(splat, out splatLayer))
                {
                    var layerCopy = data[splat].Select(x, z, width, height);
                    if (!includeTerminatingLayer)
                    {
                        return layerCopy;
                    }
                    return terminatingLayer.BlendSplats(splat, x, z, width, height, aRes, layerCopy);
                }
                return null;
            }

            UnityEngine.Profiling.Profiler.BeginSample("GetCompoundSplat");
            Serializable2DByteArray result = null;
            
            for (var i = Layers.Count - 1; i >= 0; i--)
            {
                var layer = Layers[i];
                if (!layer.Enabled)
                {
                    continue;
                }
                if (!includeTerminatingLayer && layer == terminatingLayer)
                {
                    break;
                }

                result = layer.BlendSplats(splat, x, z, width, height, aRes, result);

                if (layer == terminatingLayer)
                {
                    break;
                }
            }
            UnityEngine.Profiling.Profiler.EndSample();
            return result;
        }

        public Dictionary<SplatPrototypeWrapper, Serializable2DByteArray> GetCompoundSplats(
            LayerBase terminatingLayer, int x, int z, int width, int height, bool includeTerminatingLayer)
        {
            UnityEngine.Profiling.Profiler.BeginSample("GetCompoundSplats");
            var result = new Dictionary<SplatPrototypeWrapper, Serializable2DByteArray>();
            var allPrototypes = GetCompoundSplatPrototypes(terminatingLayer, includeTerminatingLayer);
            foreach (var splatPrototypeWrapper in allPrototypes)
            {
                var data = GetCompoundSplat(terminatingLayer, splatPrototypeWrapper, x, z, width,
                    height, includeTerminatingLayer);
                if (data != null)
                {
                    result[splatPrototypeWrapper] = data;
                }
            }
            UnityEngine.Profiling.Profiler.EndSample();
            return result;
        }

        public List<SplatPrototypeWrapper> GetCompoundSplatPrototypes(LayerBase terminatingLayer,
            bool includeTerminatingLayer)
        {
            var result = new List<SplatPrototypeWrapper>();
            for (var i = Layers.Count - 1; i >= 0; i--)
            {
                if (!includeTerminatingLayer && Layers[i] == terminatingLayer)
                {
                    break;
                }
                var wrappers = Layers[i].GetSplatPrototypeWrappers();
                if (wrappers != null)
                {
                    for (int j = 0; j < wrappers.Count; j++)
                    {
                        var splatPrototypeWrapper = wrappers[j];
                        if (!result.Contains(splatPrototypeWrapper))
                        {
                            result.Add(splatPrototypeWrapper);
                        }
                    }
                }
                if (Layers[i] == terminatingLayer)
                {
                    break;
                }
            }
            return result;
        }

        public List<DetailPrototypeWrapper> GetCompoundDetailPrototypes(LayerBase terminatingLayer,
            bool includeTerminatingLayer)
        {
            var result = new List<DetailPrototypeWrapper>();
            for (var i = Layers.Count - 1; i >= 0; i--)
            {
                if (!includeTerminatingLayer && Layers[i] == terminatingLayer)
                {
                    break;
                }
                var wrappers = Layers[i].GetDetailPrototypeWrappers();
                if (wrappers != null)
                {
                    for (int j = 0; j < wrappers.Count; j++)
                    {
                        var detailPrototypeWrapper = wrappers[j];
                        if (!result.Contains(detailPrototypeWrapper))
                        {
                            result.Add(detailPrototypeWrapper);
                        }
                    }
                }
                if (Layers[i] == terminatingLayer)
                {
                    break;
                }
            }
            return result;
        }

        public Serializable2DByteArray GetCompoundDetail(LayerBase terminatingLayer,
            DetailPrototypeWrapper detailWrapper, int x, int z, int width, int height, bool includeTerminatingLayer)
        {
            /*Serializable2DByteArray result = null;

            if (_compoundDataCache.ContainsKey(terminatingLayer))
            {
                var data = _compoundDataCache[terminatingLayer].DetailData;
                if (data.ContainsKey(detailWrapper))
                {
                    return data[detailWrapper].Select(x, z, width, height);
                }
                return null;
            }*/

            var dRes = Terrain.terrainData.detailResolution;
            if (terminatingLayer != null && _compoundDataCache.ContainsKey(terminatingLayer))
            {
                var data = _compoundDataCache[terminatingLayer].DetailData;
                Serializable2DByteArray detailLayer;
                if (data.TryGetValue(detailWrapper, out detailLayer))
                {
                    var layerCopy = data[detailWrapper].Select(x, z, width, height);
                    if (!includeTerminatingLayer)
                    {
                        return layerCopy;
                    }
                    return terminatingLayer.BlendDetails(detailWrapper, x, z, width, height, dRes, layerCopy);
                }
                return null;
            }

            Serializable2DByteArray result = null;
            for (var i = Layers.Count - 1; i >= 0; i--)
            {
                var layer = Layers[i];
                if (!layer.Enabled)
                {
                    continue;
                }
                if (!includeTerminatingLayer && layer == terminatingLayer)
                {
                    break;
                }

                result = layer.BlendDetails(detailWrapper, x, z, width, height, dRes, result);

                if (layer == terminatingLayer)
                {
                    break;
                }
            }
            return result;
        }

        public Dictionary<DetailPrototypeWrapper, Serializable2DByteArray> GetCompoundDetails(
            LayerBase terminatingLayer, int x, int z, int width, int height, bool includeTerminatingLayer)
        {
            UnityEngine.Profiling.Profiler.BeginSample("GetCompoundDetails");
            var result = new Dictionary<DetailPrototypeWrapper, Serializable2DByteArray>();
            var allPrototypes = GetCompoundDetailPrototypes(terminatingLayer, includeTerminatingLayer);
            foreach (var detailPrototypeWrapper in allPrototypes)
            {
                var data = GetCompoundDetail(terminatingLayer, detailPrototypeWrapper, x, z, width, height,
                    includeTerminatingLayer);
                if (data != null)
                {
                    result[detailPrototypeWrapper] = data;
                }
            }
            UnityEngine.Profiling.Profiler.EndSample();
            return result;
        }

        public float GetCompoundHeight(LayerBase terminatingLayer, Vector3 worldPos, bool includeTerminatingLayer = false)
        {
            float heightSum = 0;
            for (var i = Layers.Count - 1; i >= 0; i--)
            {
                var layer = Layers[i];
                if (layer == null || !layer.Enabled)
                {
                    continue;
                }
                if (!includeTerminatingLayer && layer == terminatingLayer)
                {
                    break;
                }

                heightSum = layer.BlendHeight(heightSum, worldPos, this);

                if (layer == terminatingLayer)
                {
                    break;
                }
            }
            return heightSum;
        }

        public List<MadMapsTreeInstance> GetCompoundTrees(LayerBase terminatingLayer, bool includeTerminatingLayer = false, Bounds? bounds = null)
        {
            if (terminatingLayer != null && !includeTerminatingLayer && _compoundDataCache.ContainsKey(terminatingLayer))
            {
                var data = _compoundDataCache[terminatingLayer].Trees;
                return data; 
            }

            UnityEngine.Profiling.Profiler.BeginSample("GetCompoundTrees");
            var result = new Dictionary<string, MadMapsTreeInstance>();
            for (var i = Layers.Count - 1; i >= 0; i--)
            {
                var layer = Layers[i];
                if (!includeTerminatingLayer && layer == terminatingLayer)
                {
                    break;
                }
                List<MadMapsTreeInstance> trees = layer.GetTrees();
                if (trees != null)
                {
                    for (int j = 0; j < trees.Count; j++)
                    {
                        var hurtTreeInstance = trees[j];
                        if (bounds.HasValue && !bounds.Value.Contains(Terrain.TreeToWorldPos(hurtTreeInstance.Position)))
                        {
                            continue;
                        }
                        result.Add(hurtTreeInstance.Guid, hurtTreeInstance);
                    }
                }
                List<string> removals = layer.GetTreeRemovals();
                if (removals != null)
                {
                    for (int j = 0; j < removals.Count; j++)
                    {
                        var treeRemoval = removals[j];
                        result.Remove(treeRemoval);
                    }
                }
                if (layer == terminatingLayer)
                {
                    break;
                }
            }
            UnityEngine.Profiling.Profiler.EndSample();
            return result.Values.ToList();
        }

        public List<PrefabObjectData> GetCompoundObjects(LayerBase terminatingLayer, bool includeTerminatingLayer = false)
        {
            if (_compoundDataCache.ContainsKey(terminatingLayer))
            {
                return _compoundDataCache[terminatingLayer].Objects;
            }

            var result = new Dictionary<string, PrefabObjectData>();
            for (var i = Layers.Count - 1; i >= 0; i--)
            {
                var layer = Layers[i];
                if (!includeTerminatingLayer && layer == terminatingLayer)
                {
                    break;
                }
                var obj = layer.GetObjects();
                if (obj != null)
                {
                    foreach (var value in obj)
                    {
                        if (result.ContainsKey(value.Guid))
                        {
                            Debug.LogError("Object with duplicate GUID detected!");
                            continue;
                        }
                        result.Add(value.Guid, value);
                    }
                }
                var objRemovals = layer.GetObjectRemovals();
                if (objRemovals != null)
                {
                    foreach (var removal in objRemovals)
                    {
                        result.Remove(removal);
                    }
                }
                if (layer == terminatingLayer)
                {
                    break;
                }
            }
            return result.Values.ToList();
        }

        public List<SplatPrototypeWrapper> RefreshSplats()
        {
            if(!WriteSplats)
            {
                Debug.LogWarning("Refreshing splats, but Write Splats is disabled in the Info tab. This will have no effect.", this);
                return new List<SplatPrototypeWrapper>();
            }
            var notNullSplats = SplatPrototypes.Where(wrapper => wrapper != null && wrapper.Texture != null).ToList();
            var sp = new SplatPrototype[notNullSplats.Count];
            for (var i = 0; i < notNullSplats.Count; i++)
            {
                sp[i] = notNullSplats[i].GetPrototype();
            }

            Terrain.terrainData.splatPrototypes = sp;
            Terrain.Flush();
            return notNullSplats;
        }

        public List<DetailPrototypeWrapper> RefreshDetails()
        {
            if(!WriteDetails)
            {
                Debug.LogWarning("Refreshing details, but Write Details is disabled in the Info tab. This will have no effect.", this);
                return new List<DetailPrototypeWrapper>();
            }
            var notNullDetails = DetailPrototypes.Where(wrapper => wrapper != null ).ToList();
            var sp = new DetailPrototype[notNullDetails.Count];
            for (var i = 0; i < notNullDetails.Count; i++)
            {
                sp[i] = notNullDetails[i].GetPrototype();
            }
            Terrain.terrainData.detailPrototypes = sp;
            Terrain.Flush();
            return notNullDetails;
        }

        public int GetLayerIndex(string layerName)
        {
            for (var i = 0; i < Layers.Count; i++)
            {
                if (Layers[i].name == layerName)
                {
                    return i;
                }
            }
            return -1;
        }

        public int GetLayerIndex(LayerBase layer)
        {
            for (var i = 0; i < Layers.Count; i++)
            {
                if (Layers[i] == layer)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// If you know you're going to smash a 
        /// </summary>
        /// <param name="layer"></param>
        public void CreateCompoundCache(LayerBase layer)
        {
            _compoundDataCache.Remove(layer);
            CompoundMMTerrainLayer result = new CompoundMMTerrainLayer();

            var hRes = Terrain.terrainData.heightmapResolution;
            var aRes = Terrain.terrainData.alphamapResolution;
            var dRes = Terrain.terrainData.detailResolution;
            result.Heights = GetCompoundHeights(layer, 0, 0, hRes, hRes, hRes);
            result.SplatData = GetCompoundSplats(layer, 0, 0, aRes, aRes, false);
            result.DetailData = GetCompoundDetails(layer, 0, 0, dRes, dRes, false);
            result.Objects = GetCompoundObjects(layer);
            result.Trees = GetCompoundTrees(layer);

            _compoundDataCache[layer] = result;
        }

        public void ClearCompoundCache(LayerBase layer)
        {
            _compoundDataCache.Remove(layer);
        }

        public void CopyCompoundToLayer(MMTerrainLayer layer)
        {
            var hRes = Terrain.terrainData.heightmapResolution;
            layer.SetHeights(0, 0, GetCompoundHeights(layer, 0, 0, hRes, hRes, hRes), hRes);

            var dRes = Terrain.terrainData.detailResolution;
            var details = GetCompoundDetails(layer, 0, 0, dRes, dRes, false);
            foreach (var pair in details)
            {
                layer.SetDetailMap(pair.Key, 0, 0, pair.Value, dRes);
            }

            var sRes = Terrain.terrainData.alphamapResolution;
            var splats = GetCompoundSplats(layer, 0, 0, sRes, sRes, false);
            foreach (var pair in splats)
            {
                layer.SetSplatmap(pair.Key, 0, 0, pair.Value, sRes);
            }
        }

        public void OnLevelPreBuildStep()
        {
            if (_compoundTerrainData == null)
            {
                return;
            }
#if UNITY_EDITOR
            for (int i = 0; i < Layers.Count; i++)
            {
                if (!UnityEditor.AssetDatabase.Contains(Layers[i]))
                {
                    DestroyImmediate(Layers[i]);
                    return;
                }
            }
#endif
            DestroyImmediate(_compoundTerrainData);
            _compoundTerrainData = null;
        }

        public void SetDirtyAbove(LayerBase layer)
        {
            int layerIndex = GetLayerIndex(layer) - 1;
            for(var j = layerIndex; j >= 0; --j)
            {
                var dirtyingLayer = Layers[j];
                if(dirtyingLayer)
                {
                    dirtyingLayer.Dirty = true;
                }
            }
        }
    }
}