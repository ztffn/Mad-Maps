using System;
using System.Collections.Generic;
using UnityEngine;

namespace MadMaps.Roads
{
    /// <summary>
    /// A class for storing mesh information in Unity Serialization, rather than as an asset.
    /// Primarily used for storing procedural mesh information in prefabs.
    /// </summary>
    [Serializable]
    public class SerializedMesh
    {
        [Serializable]
        public class TriangleMapping
        {
            public int SubmeshIndex;
            public List<int> Triangles = new List<int>();
        }

        public List<Vector3> Vertices = new List<Vector3>();
        public List<Vector3> Normals = new List<Vector3>();
        public List<Vector4> Tangents = new List<Vector4>();
        public List<Vector4> UV1 = new List<Vector4>();
        public List<Color32> Colors = new List<Color32>();
        public List<TriangleMapping> Triangles = new List<TriangleMapping>();
        public bool IsEmpty = true;
        
        public void FromMesh(Mesh mesh)
        {
            mesh.GetVertices(Vertices);
            mesh.GetNormals(Normals);
            mesh.GetTangents(Tangents);
            Triangles.Clear();
            for (var i = 0; i < mesh.subMeshCount; ++i)
            {
                var mapping = new TriangleMapping()
                {
                    SubmeshIndex = i,
                };
                mesh.GetTriangles(mapping.Triangles, i);
                Triangles.Add(mapping);
            }
            mesh.GetUVs(0, UV1);
            IsEmpty = false;
        }

        public Mesh ToMesh(Mesh workingMesh)
        {
            if (workingMesh == null)
            {
                workingMesh = new Mesh();
            }
            else
            {
                workingMesh.Clear();
            }

            workingMesh.SetVertices(Vertices);
            for (int i = 0; i < Triangles.Count; i++)
            {
                var mapping = Triangles[i];
                workingMesh.SetTriangles(mapping.Triangles, mapping.SubmeshIndex, true);
            }
            workingMesh.SetUVs(0, UV1);
            workingMesh.SetNormals(Normals);
            workingMesh.SetColors(Colors);
            workingMesh.SetTangents(Tangents);
            return workingMesh;
        }
    }
}