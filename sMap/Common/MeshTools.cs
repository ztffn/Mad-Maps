﻿using System;
using System.Collections.Generic;
using UnityEngine;

namespace sMap.Common
{
    public static class MeshTools
    {
        public enum Axis
        {
            XPos,
            XNeg,
            YPos,
            YNeg,
            ZPos,
            ZNeg
        }

        public static void ProcessRemovedVerts(HashSet<int> removedVerts, List<int> tris, List<Vector4> uvs)
        {
            if (removedVerts.Count == 0)
            {
                return;
            }
            for (var i = tris.Count - 3; i >= 0; i -= 3)
            {
                if (removedVerts.Contains(tris[i]) || removedVerts.Contains(tris[i + 1]) ||
                    removedVerts.Contains(tris[i + 2]))
                {
                    tris.RemoveAt(i + 2);
                    tris.RemoveAt(i + 1);
                    tris.RemoveAt(i);
                }
            }
        }

        public static Mesh FlipNormals(this Mesh mesh)
        {
            var n = new List<Vector3>(mesh.normals);
            for (int i = 0; i < n.Count; i++)
            {
                n[i] *= -1;
            }
            mesh.SetNormals(n);
            return mesh;
        }

        public static Mesh FlipWindingOrder(this Mesh mesh)
        {
            var t = new List<int>(mesh.GetTriangles(0));
            for (var i = 0; i < t.Count; i += 3)
            {
                var t0 = t[i];
                var t1 = t[i + 1];
                var t2 = t[i + 2];

                t[i] = t2;
                t[i + 1] = t1;
                t[i + 2] = t0;
            }
            mesh.SetTriangles(t, 0);
            return mesh;
        }

        // Distorts a mesh along a spline
        public static Mesh DistortAlongSpline(
            this Mesh sourceMesh,
            SplineSegment spline,
            Matrix4x4 meshTransform,
            float splineStartTime = 0,
            float splineEndTime = 1,
            float tSnap = 0)
        {
            //spline.Recalculate();

            Mesh result = new Mesh();
            var verts = new List<Vector3>(sourceMesh.vertices);

            var meshBounds = new Bounds();
            for (int i = 0; i < verts.Count; i++)
            {
                verts[i] = meshTransform.MultiplyPoint3x4(verts[i]);
                if (i == 0)
                {
                    meshBounds.center = verts[i];
                }
                else
                {
                    meshBounds.Encapsulate(verts[i]);
                }
            }
            //DebugHelper.DrawCube(meshBounds.center, meshBounds.extents, Quaternion.identity, Color.white, 20);

            var colors = new Color[verts.Count];
            // Transform vert from mesh space to spline-axis space
            for (var i = verts.Count - 1; i >= 0; i--)
            {
                var vert = verts[i];
                
                // First is finding the vert on the spline axis
                var normalizedVert = vert - meshBounds.min; // Normalized vert tells us where it falls on the splines 't' axis
                normalizedVert = new Vector3(
                    normalizedVert.x / Mathf.Max(float.Epsilon, meshBounds.size.x),
                    normalizedVert.y/ Mathf.Max(float.Epsilon, meshBounds.size.y),
                    normalizedVert.z / Mathf.Max(float.Epsilon, meshBounds.size.z));
                var t = Mathf.Lerp(splineStartTime, splineEndTime, normalizedVert.x);
                t = Mathf.Clamp01(t);

                var scaledTSnap = tSnap;
                if (t < scaledTSnap)
                {
                    t = 0;
                }
                if (t > 1 - scaledTSnap)
                {
                    t = 1;
                }

                var natT = spline.UniformToNaturalTime(t);

                //var rotation = spline.GetRotation(natT);
                var tangent = spline.GetTangent(natT);
                var splineForward = Quaternion.LookRotation(tangent, spline.GetUpVector(natT));
                //var axisRotation = GetAxisRotation(axis);

                //var uniformT = spline.NaturalToUniformTime(t);
                Vector3 pointAlongSpline = /*transform.worldToLocalMatrix**/spline.GetUniformPointOnSpline(t);

                //var vectorFromAxis = GetVectorFromAxis(vert, axis);
                //var splineAdjustedVert = vert - vectorFromAxis;

                verts[i] = pointAlongSpline + splineForward * new Vector3(0, vert.y, vert.z);
                colors[i] = Color.Lerp(Color.black, Color.white, t);
            }

            result.SetVertices(verts);
            result.colors = colors;
            result.SetTriangles(sourceMesh.GetTriangles(0), 0);
            result.SetUVs(0, new List<Vector2>(sourceMesh.uv));
            return result;
        }

        public static float GetScalarFromAxis(Vector3 vector, Axis axis)
        {
            switch (axis)
            {
                case Axis.XPos:
                    return vector.x;
                case Axis.XNeg:
                    return -vector.x;
                case Axis.YPos:
                    return vector.y;
                case Axis.YNeg:
                    return -vector.y;
                case Axis.ZPos:
                    return vector.z;
                case Axis.ZNeg:
                    return -vector.z;
            }
            throw new Exception("Unknown axis!");
        }

        public static Vector3 GetVectorFromAxis(Vector3 vector, Axis axis)
        {
            switch (axis)
            {
                case Axis.XPos:
                    return new Vector3(vector.x, 0, 0);
                case Axis.XNeg:
                    return new Vector3(-vector.x, 0, 0);
                case Axis.YPos:
                    return new Vector3(0, vector.y, 0);
                case Axis.YNeg:
                    return new Vector3(0, -vector.y, 0);
                case Axis.ZPos:
                    return new Vector3(0, 0, vector.z);
                case Axis.ZNeg:
                    return new Vector3(0, 0, -vector.z);
            }
            throw new Exception("Unknown axis!");
        }

        public static Quaternion GetAxisRotation(Axis axis)
        {
            switch (axis)
            {
                case Axis.XNeg:
                case Axis.XPos:
                    return Quaternion.identity;
                case Axis.YPos:
                    return Quaternion.Euler(new Vector3(0, 90, 0));
                case Axis.YNeg:
                    return Quaternion.Euler(new Vector3(0, -90, 0));
                case Axis.ZPos:
                    return Quaternion.Euler(new Vector3(0, 0, 90));
                case Axis.ZNeg:
                    return Quaternion.Euler(new Vector3(0, 0, -90));
            }
            throw new Exception("Unknown axis!");
        }

        public static void CopyMesh(this Mesh baseMesh, Mesh copyMesh)
        {
            baseMesh.Clear();

            // verts
            baseMesh.SetVertices(new List<Vector3>(copyMesh.vertices));

            // tris
            for (var i = 0; i < copyMesh.subMeshCount; ++i)
            {
                baseMesh.SetTriangles(copyMesh.GetTriangles(i), i);
            }

            // uvs (only first for now)
            var copyUVs = new List<Vector4>();
            copyMesh.GetUVs(0, copyUVs);
            baseMesh.SetUVs(0, copyUVs);

            // normals
            baseMesh.SetNormals(new List<Vector3>(copyMesh.normals));

            baseMesh.SetTangents(new List<Vector4>(copyMesh.tangents));
        }
    }
}

