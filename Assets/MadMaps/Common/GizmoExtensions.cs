using UnityEngine;

namespace MadMaps.Common
{
    public static class GizmoExtensions
    {
        public static void Label(Vector3 position, string label)
        {
#if UNITY_EDITOR
            UnityEditor.Handles.Label(position, label);
#endif
        }

        public static void DrawWireCube(Vector3 origin, Vector3 extents, Quaternion rotation, Color color)
        {
            var verts = new[]
            {
            // Top square
            origin + rotation*new Vector3(extents.x, extents.y, extents.z),
            origin + rotation*new Vector3(-extents.x, extents.y, extents.z),
            origin + rotation*new Vector3(extents.x, extents.y, -extents.z),
            origin + rotation*new Vector3(-extents.x, extents.y, -extents.z),

            // Bottom square
            origin + rotation*new Vector3(extents.x, -extents.y, extents.z),
            origin + rotation*new Vector3(-extents.x, -extents.y, extents.z),
            origin + rotation*new Vector3(extents.x, -extents.y, -extents.z),
            origin + rotation*new Vector3(-extents.x, -extents.y, -extents.z)
        };

            Gizmos.color = color;

            // top square
            Gizmos.DrawLine(verts[0], verts[2]);
            Gizmos.DrawLine(verts[1], verts[3]);
            Gizmos.DrawLine(verts[1], verts[0]);
            Gizmos.DrawLine(verts[2], verts[3]);

            // bottom square
            Gizmos.DrawLine(verts[4], verts[6]);
            Gizmos.DrawLine(verts[5], verts[7]);
            Gizmos.DrawLine(verts[5], verts[4]);
            Gizmos.DrawLine(verts[6], verts[7]);

            // connections
            Gizmos.DrawLine(verts[0], verts[4]);
            Gizmos.DrawLine(verts[1], verts[5]);
            Gizmos.DrawLine(verts[2], verts[6]);
            Gizmos.DrawLine(verts[3], verts[7]);

            Gizmos.color = Color.white;
        }
    }
}