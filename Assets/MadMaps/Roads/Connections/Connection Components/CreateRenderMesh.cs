using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using MadMaps.Common;
using MadMaps.Common.GenericEditor;
using MadMaps.Terrains;


namespace MadMaps.Roads.Connections
{
    [Serializable]
    public class CreateRenderMesh : ConnectionComponent
    {
        [Name("Mesh/Create Render Mesh")]
        public class Config : ConnectionConfigurationBase
        {
            public class LodLevel
            {               

                public Mesh SourceMesh;
                public Material[] Materials = new Material[0];
                public float LodDistance = 1f;
                public bool Mirror;
                public int Layer;
                public bool CopyToCollider;
                public float BreakDistance = 50;
                public SplineSegment.ESplineInterpolation SplineInterpolation = SplineSegment.ESplineInterpolation.Natural;
                [Range(0, 1)]
                public float SnapDistance = 1f;
                public MeshTools.Axis Axis;
                public Vector3 Scale = new Vector3(1, 1, 1);
                public Vector3 Offset = new Vector3(0, 0, 0);
                [Tooltip("Rotation around the spline")]
                public Vector3 Rotation = new Vector3(0, 0, 0);
                [Header("Normals")]
                public bool OverrideNormal;
                [ShowIf("OverrideNormal")]
                public Vector3 NormalOverride = Vector3.up;
                [Range(0, 180)]
                public float PhongBreakAngle = 45;

                [Header("Shadows")]
                public ShadowCastingMode ShadowCastingMode = ShadowCastingMode.On;
                
                public override string ToString()
                {
                    return SourceMesh != null ? SourceMesh.name.Truncate(32, "...") : "null";
                }
            }
            
            public List<LodLevel> LodLevels = new List<LodLevel>(); 

            public override Type GetMonoType()
            {
                return typeof(CreateRenderMesh);
            }
        }

        public Vector2 SplineRange = new Vector2(0, 1);

        [Serializable]
        public class BakeResult
        {
            public Mesh Mesh;
            public MeshFilter Filter;
            public MeshRenderer Renderer;
            public MeshCollider MeshCollider;
            public int TargetLodLevel;
            public int ChunkIndex;

            public void Dispose()
            {
                Mesh.TryDestroyImmediate();
                if (Filter)
                {
                    Filter.gameObject.TryDestroyImmediate();
                }
            }
        }

        public List<BakeResult> Results = new List<BakeResult>();
        
        public override void OnPostBake()
        {
            // Setup
            if (!Network || Configuration == null || !NodeConnection)
            {
                return;
            }

            var config = Configuration.GetConfig<Config>();
            if (config == null)
            {
                Debug.LogError("Invalid configuration! Expected RoadMeshObjectConfiguration");
                return;
            }

            foreach (var result in Results)
            {
                result.Dispose();
            }
            Results.Clear();

            var rootGameObject = NodeConnection.GetDataContainer();
            var targetGameObject = rootGameObject;
            for (int i = 0; i < config.LodLevels.Count; i++)
            {
                var lodLevel = config.LodLevels[i];
                var child = new GameObject("LOD" + i).transform;
                child.transform.SetParent(rootGameObject.transform);
                child.transform.localPosition = Vector3.zero;
                child.transform.localRotation = Quaternion.identity;
                child.transform.localScale = Vector3.one;
                targetGameObject = child.gameObject;
                DoLodLevel(targetGameObject, lodLevel, i);
            }

            var lods = new LOD[config.LodLevels.Count];
            foreach (var bakeResult in Results)
            {
                /*var verts = bakeResult.Mesh.vertices;
                var colors = new Color[verts.Length];
                for (int i = 0; i < verts.Length; i++)
                {
                    var vector3 = verts[i];
                    var worldPos = bakeResult.Renderer.transform.TransformPoint(vector3);
                }*/

                if (bakeResult.TargetLodLevel != 0)
                {
                    continue;
                }

                // We've found a root lod level group
                var lodGroup = bakeResult.Renderer.gameObject.GetOrAddComponent<LODGroup>();
                var thisConfig = config.LodLevels[bakeResult.TargetLodLevel];
                lods[0] = new LOD(thisConfig.LodDistance, new []{bakeResult.Renderer});
                foreach (var lowerLOD in Results)
                {
                    if (bakeResult.ChunkIndex != lowerLOD.ChunkIndex)
                    {
                        continue;
                    }
                    var lowerConfig = config.LodLevels[lowerLOD.TargetLodLevel];
                    lods[lowerLOD.TargetLodLevel] = new LOD(lowerConfig.LodDistance, new[] { lowerLOD.Renderer});
                }

                lodGroup.SetLODs(lods);
            }
        }

        private BakeResult CreateNewBakeResult(Config.LodLevel config, GameObject rootTarget, int lodLevelIndex, int chunkCount)
        {
            rootTarget.GetOrAddComponent<ProceduralMeshContainer>().Collider = config.CopyToCollider;
            rootTarget.layer = config.Layer;
            return new BakeResult()
            {
                Mesh = new Mesh(),
                Filter = rootTarget.GetOrAddComponent<MeshFilter>(),
                Renderer = rootTarget.GetOrAddComponent<MeshRenderer>(),
                MeshCollider = config.CopyToCollider ? rootTarget.GetOrAddComponent<MeshCollider>() : null,
                TargetLodLevel = lodLevelIndex,
                ChunkIndex = chunkCount,
            };            
        }

        private void DoLodLevel(GameObject rootTarget, Config.LodLevel config, int lodLevelIndex)
        {
            // Get a version of the spline in local space
            var spline = new SplineSegment(NodeConnection.GetSpline());
            spline.ApplyMatrix(rootTarget.transform.worldToLocalMatrix);

            if(config.SourceMesh == null)
            {
                Debug.LogError("No mesh defined for CreateRenderMesh component!");
                return;
            }

            List<CombineInstance> workingMeshes = new List<CombineInstance>(); // All the meshes that will be combined at the end
            var totalSplineLength = spline.Length;
            var scale = config.SourceMesh.bounds.size;
            scale.Scale(config.Scale);
            var sourceMeshAxisLength = MeshTools.GetScalarFromAxis(scale, config.Axis);

            var perMeshLength = Math.Min(totalSplineLength, sourceMeshAxisLength);
            var perMeshPercentage = perMeshLength / totalSplineLength;

            var snapDistance = config.SnapDistance;

            var matrix = Matrix4x4.TRS(
                config.Offset,
                Quaternion.Euler(config.Rotation),
                config.Scale);
            var mirrorMatrix = Matrix4x4.TRS(
                new Vector3(config.Offset.x, config.Offset.y, -config.Offset.z),
                Quaternion.Euler(config.Rotation),
                new Vector3(config.Scale.x, config.Scale.y, -config.Scale.z));
            var meshCombineMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one);

            int chunkCount = 0;
            var currentResult = CreateNewBakeResult(config, rootTarget, lodLevelIndex, chunkCount);
            chunkCount++;
            float distanceAccumulator = 0;
            
            for (var i = SplineRange.x; i < 1; i += perMeshPercentage)
            {
                var startTime = i;
                var endTime = Mathf.Clamp01(i + perMeshPercentage);

                if (Math.Abs(startTime - endTime) < .001f)
                {
                    break;
                }

                if (config.SplineInterpolation == SplineSegment.ESplineInterpolation.Uniform)
                {
                    startTime = spline.NaturalToUniformTime(startTime);
                    endTime = spline.NaturalToUniformTime(endTime);
                }

                var distanceLeft = totalSplineLength - (endTime * totalSplineLength);
                if (endTime < 1 && distanceLeft <= snapDistance)
                {
                    endTime = 1;
                    i = 1;
                }

                if (currentResult == null)
                {
                    var newChunkObject = new GameObject("Chunk" + chunkCount);
                    newChunkObject.transform.SetParent(rootTarget.transform);
                    newChunkObject.transform.localPosition = Vector3.zero;
                    newChunkObject.transform.localRotation = Quaternion.identity;
                    newChunkObject.transform.localScale = Vector3.one;
                    currentResult = CreateNewBakeResult(config, newChunkObject, lodLevelIndex, chunkCount);
                    chunkCount++;
                }

                workingMeshes.Add(new CombineInstance()
                {
                    mesh = config.SourceMesh.DistortAlongSpline(spline, matrix, startTime, endTime, snapDistance, config.SplineInterpolation),
                    transform = meshCombineMatrix,
                });

                if (config.Mirror)
                {
                    workingMeshes.Add(new CombineInstance()
                    {
                        mesh = config.SourceMesh.DistortAlongSpline(spline, mirrorMatrix, startTime, endTime, snapDistance, config.SplineInterpolation)
                            .FlipWindingOrder(),
                        transform = Matrix4x4.identity,
                    });
                }

                distanceAccumulator += perMeshPercentage*totalSplineLength;
                if ((distanceAccumulator > config.BreakDistance && totalSplineLength - distanceAccumulator > config.BreakDistance) || Mathf.Approximately(endTime, 1))
                {
                    distanceAccumulator = 0;
                    if (workingMeshes.Count > 0)
                    {
                        CombineInstance[] combineInstances = workingMeshes
                            .Where((instance => instance.mesh != null && instance.mesh.bounds.size.magnitude > 0)).ToArray();
                        currentResult.Mesh.CombineMeshes(combineInstances);
                    }
                    for (int j = 0; j < workingMeshes.Count; j++)
                    {
                        DestroyImmediate(workingMeshes[j].mesh);
                    }

                    if (config.OverrideNormal)
                    {
                        var n = new List<Vector3>();
                        n.Fill(Vector3.up, currentResult.Mesh.vertexCount);
                        currentResult.Mesh.SetNormals(n);
                    }
                    else
                    {
                        currentResult.Mesh.RecalculateNormals();
                    }

                    if (Mathf.Approximately(currentResult.Mesh.bounds.size.x, 0) ||
                        Mathf.Approximately(currentResult.Mesh.bounds.size.y, 0) ||
                        Mathf.Approximately(currentResult.Mesh.bounds.size.z, 0))
                    {
                        continue;
                    }

                    currentResult.Mesh.RecalculateTangents();
                    currentResult.Mesh.RecalculateBounds();
                    if (config.CopyToCollider)
                    {
                        currentResult.MeshCollider.enabled = true;
                        currentResult.MeshCollider.sharedMesh = currentResult.Mesh;
                    }

                    currentResult.Renderer.shadowCastingMode = config.ShadowCastingMode;
                    currentResult.Filter.sharedMesh = currentResult.Mesh;
                    currentResult.Renderer.sharedMaterials = config.Materials.ToArray();
                    Results.Add(currentResult);
                    currentResult = null;
                }
            }
        }

        public override void Destroy()
        {
            for (int i = 0; i < Results.Count; i++)
            {
                var bakeResult = Results[i];
                bakeResult.Dispose();
            }
            base.Destroy();
        }
    }
}