using System;
using MadMaps.Common;
using MadMaps.Common.Painter;
using MadMaps.Common.Collections;
using UnityEngine;

namespace MadMaps.WorldStamps
{
    public class WorldStampPreview
    {
        private Mesh _mesh;
        private Material _material;
        private int _lastFrame;
        //private Vector3 _lastPosition;
        //private Quaternion _lastRotation;
        private bool _isDisposed;

        private Func<bool> _existenceHook;
        private Func<Vector3> _displaySize;
        private Func<Vector3> _dataSize;
        private Func<Vector3> _position;
        private Func<Vector3> _scale;
        private Func<Quaternion> _rotation;

        public void Invalidate(Serializable2DFloatArray heights, 
            Func<Vector3> displaySizeGetter, 
            Func<Vector3> positionGetter, 
            Func<Vector3> scaleGetter,
            Func<Quaternion> rotationGetter, 
            Func<Vector3> dataSizeGetter, bool flipHeights,
            WorldStampMask mask, Common.Painter.GridManagerInt gridManager, 
            Func<bool> existenceHook, int res)
        {
            _dataSize = dataSizeGetter;
            _displaySize = displaySizeGetter;
            _existenceHook = existenceHook;
            _position = positionGetter;
            _scale = scaleGetter;
            _rotation = rotationGetter;

            if (_mesh == null)
            {
                _mesh = new Mesh();
            }
            if (_material == null)
            {
                _material = Resources.Load<Material>("MadMaps/WorldStamp/WorldStampPreviewMaterial");
            }
            var verts = new Vector3[(res+1) * (res+1)];
            var uv = new Vector2[verts.Length];
            var colors = new Color[verts.Length];
            int counter = 0;
            var dataSize = dataSizeGetter();
            for (int u = 0; u <= res; u++)
            {
                var uF = u/(float) res;
                for (int v = 0; v <= res; v++)
                {
                    var vF = v / (float)res;
                    var samplePoint = flipHeights ? new Vector2(uF, vF) : new Vector2(vF, uF);
                    var height = heights.BilinearSample(samplePoint);

                    var pos = new Vector3(uF * dataSize.x, height * dataSize.y, vF * dataSize.z) - dataSize.xz().x0z() / 2;

                    float val = 1;
                    if (mask != null && gridManager != null)
                    {
                        val = mask.GetBilinear(gridManager, new Vector3(pos.x, 0, pos.z) + dataSize.xz().x0z() / 2);
                        pos.y *= val;
                    }
                    
                    verts[counter] = pos;
                    uv[counter] = new Vector2(uF, vF);
                    colors[counter] = Color.Lerp(Color.clear, Color.white, val);
                    counter++;
                }
            }
            
            _mesh.vertices = verts;
            _mesh.uv = uv;
            _mesh.colors = colors;

            var tris = new int[((res+1) * (res)) * 6];
            for (var i = 0; i < tris.Length; i += 6)
            {
                var vIndex = i/6;

                var t1 = vIndex + 0;
                var t2 = vIndex + 1;
                var t3 = vIndex + 2 + (res-1);

                var t1r = t1 / (res + 1);
                var t2r = t2 / (res + 1);
                var t3r = (t3 / (res + 1)) - 1;

                if (t1r == t2r && t2r == t3r && t3r == t1r)
                {
                    tris[i + 0] = t1;
                    tris[i + 1] = t2;
                    tris[i + 2] = t3;
                }

                var t4 = vIndex + 2 + (res - 1);
                var t5 = vIndex + 1 + (res - 1);
                var t6 = vIndex + 0;

                var t4r = (t4 / (res + 1)) - 1;
                var t5r = (t5 / (res + 1)) - 1;
                var t6r = t6 / (res + 1);

                if (t4r == t5r && t5r == t6r && t6r == t4r)
                {
                    tris[i + 3] = t4;
                    tris[i + 4] = t5;
                    tris[i + 5] = t6;
                }
            }

            _mesh.triangles = tris;
            _mesh.RecalculateNormals(0);
            _mesh.RecalculateBounds();

#if UNITY_EDITOR
            UnityEditor.SceneView.onSceneGUIDelegate -= OnSceneGUIDelegate;
            UnityEditor.SceneView.onSceneGUIDelegate += OnSceneGUIDelegate;
#endif
        }

#if UNITY_EDITOR
        private void OnSceneGUIDelegate(UnityEditor.SceneView sceneView)
        {
            //Debug.Log(Time.renderedFrameCount);
            if (!_existenceHook())
            {
                UnityEditor.SceneView.onSceneGUIDelegate -= OnSceneGUIDelegate;
                Dispose();
                return;
            }

            if (_lastFrame >= Time.renderedFrameCount)
            {
                return;
            }

            _lastFrame = Time.renderedFrameCount; 
            var position = _position();
            var rotation = _rotation();

            var dSize = _dataSize();
            dSize = new Vector3(Mathf.Max(dSize.x, float.Epsilon), Mathf.Max(dSize.y, float.Epsilon), Mathf.Max(dSize.z, float.Epsilon));
            var displaySize = _displaySize();

            var displaySizeScale = new Vector3(displaySize.x/dSize.x, displaySize.y/dSize.y,displaySize.z/dSize.z);
            var scale = _scale();
            scale = new Vector3(scale.x * displaySizeScale.x, scale.y * displaySizeScale.y, scale.z * displaySizeScale.z);
            
            var mat = Matrix4x4.TRS(position, rotation, scale);
            Graphics.DrawMesh(_mesh, mat, _material, 0);
        }
#endif

        public void Dispose()
        {
#if UNITY_EDITOR
            UnityEditor.SceneView.onSceneGUIDelegate -= OnSceneGUIDelegate;
#endif
            UnityEngine.Object.DestroyImmediate(_mesh);
            _isDisposed = true;
        }

        public bool IsDisposed()
        {
            return _isDisposed;
        }
    }
}