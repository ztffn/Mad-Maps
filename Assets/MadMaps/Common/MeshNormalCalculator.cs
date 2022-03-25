using System.Collections.Generic;
using UnityEngine;

namespace MadMaps.Common
{

    public static class MeshNormalCalculator
    {
        /// <summary>
        /// A moderately expensive normal calculation algorithm that can take a phong break angle and merge across verts
        /// </summary>
        /// <param name="baseMesh"></param>
        /// <param name="phongBreakAngle"></param>
        /// <param name="mergeVerts"></param>
        public static void RecalculateNormals(this Mesh baseMesh, 
            float phongBreakAngle, 
            bool mergeVerts = true)
        {
            if (baseMesh.subMeshCount > 1)
            {
                Debug.LogWarning("Multiple submeshes probably won't work yet with this function!");
            }

            var triangles = baseMesh.triangles;

            var verts = baseMesh.vertices;
            /*for (int i = 0; i < verts.Length; i++)
            {
                verts[i] /= mergeDistance;
                verts[i].Round();
                verts[i] *= mergeDistance;
            }*/

            var normals = new List<Vector3>(baseMesh.normals);
            if (normals.Count == 0)
            {
                // For dynamic meshes
                normals.Fill(Vector3.zero, verts.Length);
            }

            // Key is vertex position, then a list of the results from the loop below
            Dictionary<Vector3, List<VertIndexNormalTuple>> normalCache = new Dictionary<Vector3, List<VertIndexNormalTuple>>();
            for (int triangleIndex = 0; triangleIndex < triangles.Length; triangleIndex += 3)
            {
                var t1 = triangles[triangleIndex];
                var t2 = triangles[triangleIndex + 1];
                var t3 = triangles[triangleIndex + 2];

                var v1 = verts[t1];
                var v2 = verts[t2];
                var v3 = verts[t3];

                var a = v1 - v2;
                var b = v1 - v3;

                var norm = Vector3.Cross(a, b);

                RecalcNormalsInternal_SetNormCache(normalCache, t1, v1, norm);
                RecalcNormalsInternal_SetNormCache(normalCache, t2, v2, norm);
                RecalcNormalsInternal_SetNormCache(normalCache, t3, v3, norm);
            }

            for (int i = 0; i < verts.Length; i++)
            {
                var vert = verts[i];
                List<VertIndexNormalTuple> normList;
                if (!normalCache.TryGetValue(vert, out normList))
                {
                    // Likely a floating vert
                    // Throw a warning?
                    continue;
                }

                Vector3 norm = Vector3.zero;
                Vector3 baseNormal = Vector3.zero;

                bool baseNormFlag = false;

                for (int j = 0; j < normList.Count; j++)
                {
                    if (i == normList[j].Index)
                    {
                        if (!baseNormFlag)
                        {
                            baseNormFlag = true;
                            norm += normList[j].Position;
                            baseNormal = norm;
                        }
                        else if (Vector3.Angle(baseNormal, normList[j].Position) < phongBreakAngle)
                        {
                            norm += normList[j].Position;
                        }
                    }
                }
                if (mergeVerts)
                {
                    for (int j = 0; j < normList.Count; j++)
                    {
                        if (i != normList[j].Index && Vector3.Angle(baseNormal, normList[j].Position) < phongBreakAngle)
                        {
                            norm += normList[j].Position;
                        }
                    }
                }

                normals[i] = norm.normalized;
            }

            baseMesh.SetNormals(normals);

            for (int i = 0; i < verts.Length; i++)
            {
                var vector3 = verts[i];
                var norm = normals[i];
                Debug.DrawLine(vector3, vector3 + norm *0.1f, Color.blue.WithAlpha(0.5f));
            }
        }

        private static void RecalcNormalsInternal_SetNormCache(Dictionary<Vector3, List<VertIndexNormalTuple>> normalCache,
            int t1, Vector3 v1, Vector3 norm)
        {
            List<VertIndexNormalTuple> normList;
            if (!normalCache.TryGetValue(v1, out normList))
            {
                normList = new List<VertIndexNormalTuple>();
                normalCache[v1] = normList;
            }
            normList.Add(new VertIndexNormalTuple(t1, norm));

            Debug.DrawLine(v1, v1 + norm.normalized*0.15f, Color.yellow);
        }

        private class VertIndexNormalTuple
        {
            public int Index;
            public Vector3 Position;

            public VertIndexNormalTuple(int index, Vector3 pos)
            {
                Index = index;
                Position = pos;
            }
        }
    }
}