using System;
using System.IO;
using MadMaps.Common;
using MadMaps.Common.Collections;
using UnityEngine;

#if UNITY_EDITOR
using Painter = MadMaps.Common.Painter.Painter;
using UnityEditor;

namespace MadMaps.WorldStamp.Authoring
{
    [Serializable]
    public class MaskDataCreator : WorldStampCreatorLayer
    {
        public float GridSize = 5;

        [NonSerialized]
        public Bounds LastBounds;
        [NonSerialized]
        public WorldStampMask Mask = new WorldStampMask();

        private static float MinMaskRes = 4;
        private static float MaskResolution = 128;

        public override bool NeedsRecapture
        {
            get { return false; }
        }

        private Common.Painter.GridManagerInt GridManager
        {
            get
            {
                if (__gridManager == null || __gridManager.GRID_SIZE != GridSize)
                {
                    __gridManager = new Common.Painter.GridManagerInt(GridSize);
                }
                return __gridManager;
            }
        }
        private Common.Painter.GridManagerInt __gridManager;

        private Painter _maskPainter;
        
        private void ResetMask(Bounds bounds)
        {
            GridSize = Math.Max(MinMaskRes, Math.Max(bounds.size.x, bounds.size.z) / MaskResolution);
            Mask.Clear();
            for (var u = GridSize / 2f; u < bounds.size.x; u += GridSize)
            {
                for (var v = GridSize / 2f; v < bounds.size.z; v += GridSize)
                {
                    var cell = GridManager.GetCell(new Vector3(u, 0, v));
                    var cellMax = GridManager.GetCellMax(cell).x0z() + bounds.min;
                    var cellMin = GridManager.GetCellCenter(cell).x0z() + bounds.min;
                    if (!bounds.Contains(cellMax) || !bounds.Contains(cellMin))
                    {
                        continue;
                    }
                    Mask.SetValue(cell, 1);
                }
            }
            LastBounds = bounds;
        }

        private void FillMaskFromMinY(Bounds bounds, Serializable2DFloatArray heights)
        {
            Mask.Clear();
            for (var u = GridSize / 2f; u < bounds.size.x; u += GridSize)
            {
                for (var v = GridSize / 2f; v < bounds.size.z; v += GridSize)
                {
                    var cell = GridManager.GetCell(new Vector3(u, 0, v));
                    var cellMax = GridManager.GetCellMax(cell).x0z() + bounds.min;
                    var cellMin = GridManager.GetCellCenter(cell).x0z() + bounds.min;
                    if (!bounds.Contains(cellMax) || !bounds.Contains(cellMin))
                    {
                        continue;
                    }

                    var h = heights.BilinearSample(new Vector2(u / bounds.size.z, v / bounds.size.x));
                    Mask.SetValue(cell, h > 0 ? 1 : 0);
                }
            }
        }

        public override GUIContent Label
        {
            get { return new GUIContent("Mask"); }
        }

        protected override bool HasDataPreview
        {
            get { return false; }
        }

        public override bool ManuallyRecapturable
        {
            get { return false; }
        }

        protected override void CaptureInternal(Terrain terrain, Bounds bounds)
        {
            if (LastBounds != bounds)
            {
                ResetMask(bounds);
            }
        }

        public override void Dispose()
        {
            if (_maskPainter != null)
            {
                _maskPainter.Destroy();
                _maskPainter = null;
            }
        }

        protected override void PreviewInSceneInternal(WorldStampCreator parent)
        {
            var bounds = parent.Template.Bounds;
            if (_maskPainter == null)
            {
                _maskPainter = new Painter(Mask, GridManager);
                _maskPainter.Ramp = new Gradient()
                {
                    colorKeys = new[] { new GradientColorKey(Color.red, 0), new GradientColorKey(Color.black, 0.001f), new GradientColorKey(Color.black, 1), },
                    alphaKeys = new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0, 1), }
                };
                _maskPainter.Rect = new Rect(bounds.min.xz(), bounds.size.xz());
            }
            else
            {
                GridSize = Math.Max(MinMaskRes, Math.Max(bounds.size.x, bounds.size.z) / MaskResolution);
                _maskPainter.GridManager = GridManager;
                _maskPainter.Canvas = Mask;
                _maskPainter.MaxValue = 1;
                _maskPainter.MinValue = 0;
                _maskPainter.Rect = new Rect(Vector2.zero, bounds.size.xz());
                _maskPainter.TRS = Matrix4x4.TRS(bounds.min, Quaternion.identity, Vector3.one);
                //_maskPainter.Repaint();
                _maskPainter.PaintingEnabled = true;
                _maskPainter.OnSceneGUI();
            }
        }

        public override void DrawGUI(WorldStampCreator parent)
        {
            if (parent.SceneGUIOwner != this && _maskPainter != null)
            {
                _maskPainter.Canvas = null;
            }
            if (_maskPainter != null && _maskPainter.Canvas != null &&_maskPainter.Canvas != Mask)
            {
                _maskPainter.Canvas = null;
                parent.SceneGUIOwner = null;
            }
            if (Mask.Count == 0 || LastBounds != parent.Template.Bounds)
            {
                ResetMask(parent.Template.Bounds);
            }

            EditorExtensions.Seperator();
            EditorGUILayout.BeginHorizontal();

            GUIExpanded = EditorGUILayout.Foldout(GUIExpanded, Label);
            var previewContent = new GUIContent("Edit");
            previewContent.tooltip = "Edit the mask for this stamp.";
            GUI.color = parent.SceneGUIOwner == this ? Color.green : Color.white;
            if (GUILayout.Button(previewContent, EditorStyles.miniButton, GUILayout.Width(60), GUILayout.Height(16)))
            {
                if (parent.SceneGUIOwner == this && _maskPainter != null)
                {
                    _maskPainter.Destroy();
                    _maskPainter = null;
                }
                parent.SceneGUIOwner = parent.SceneGUIOwner == this ? null : this;
                if ((parent.Template.Bounds.size - LastBounds.size).magnitude > 1)
                {
                    ResetMask(parent.Template.Bounds);
                }
                GUIUtility.ExitGUI();
                return;
            }
            GUI.color = Color.white;
            EditorGUILayout.EndHorizontal();

            if (GUIExpanded)
            {
                GUI.enabled = Enabled;
                EditorGUI.indentLevel++;
                EditorGUI.BeginChangeCheck();
                OnExpandedGUI(parent);
                if (EditorGUI.EndChangeCheck())
                {
                    NeedsRecapture = true;
                }
                EditorGUI.indentLevel--;
                GUI.enabled = true;
            }
        }

        protected override void OnExpandedGUI(WorldStampCreator parent)
        {
            if (EditorGUILayoutX.IndentedButton("Reset"))
            {
                ResetMask(parent.Template.Bounds);
            }
            if (EditorGUILayoutX.IndentedButton("Fill From Zero Level"))
            {
                FillMaskFromMinY(parent.Template.Bounds, parent.GetCreator<HeightmapDataCreator>().Heights);
            }
            if (EditorGUILayoutX.IndentedButton("Load From Texture"))
            {
                LoadFromTexture(parent);
            }
        }

        public Texture2D GetTextureFromMask(WorldStampCreator parent)
        {
            GridSize = Math.Max(MinMaskRes, Math.Max(parent.Template.Bounds.size.x, parent.Template.Bounds.size.z) / MaskResolution);
            var bounds = parent.Template.Bounds;
            var width = Mathf.CeilToInt(bounds.size.x/GridSize);
            var height = Mathf.CeilToInt(bounds.size.z/GridSize);
            var tex = new Texture2D(width, height);
            for (var u = 0; u < width; u++)
            {
                for (var v = 0; v < height; v++)
                {
                    var pos = new Vector3((u / (float)width) * bounds.size.x, bounds.size.y/2, (v / (float)height) * bounds.size.z);
                    var cell = GridManager.GetCell(pos);
                    var cellMax = GridManager.GetCellMax(cell).x0z() + bounds.min;
                    var cellMin = GridManager.GetCellCenter(cell).x0z() + bounds.min;
                    if (!bounds.Contains(cellMax) || !bounds.Contains(cellMin))
                    {
                        continue;
                    }

                    var val = Mask.GetValue(cell);
                    tex.SetPixel(u, v, Color.Lerp(Color.black, Color.white, val));
                }
            }
            tex.Apply();
            return tex;
        }

        public Serializable2DFloatArray GetArrayFromMask(WorldStampCreator parent)
        {
            GridSize = Math.Max(MinMaskRes, Math.Max(parent.Template.Bounds.size.x, parent.Template.Bounds.size.z) / MaskResolution);
            var bounds = parent.Template.Bounds;
            var width = Mathf.CeilToInt(bounds.size.x / GridSize);
            var height = Mathf.CeilToInt(bounds.size.z / GridSize);
            var array = new Serializable2DFloatArray(width, height);
            for (var u = 0; u < width; u++)
            {
                for (var v = 0; v < height; v++)
                {
                    var pos = new Vector3((u / (float)width) * bounds.size.x, bounds.size.y / 2, (v / (float)height) * bounds.size.z);
                    var cell = GridManager.GetCell(pos);
                    var cellMax = GridManager.GetCellMax(cell).x0z() + bounds.min;
                    var cellMin = GridManager.GetCellCenter(cell).x0z() + bounds.min;
                    if (!bounds.Contains(cellMax) || !bounds.Contains(cellMin))
                    {
                        continue;
                    }

                    var val = Mask.GetValue(cell);
                    array[u, v] = val;
                }
            }
            return array;
        }

        public void SetMaskFromArray(WorldStampCreator parent, Serializable2DFloatArray mask)
        {
            GridSize = Math.Max(MinMaskRes, Math.Max(parent.Template.Bounds.size.x, parent.Template.Bounds.size.z) / MaskResolution);
            Mask.Clear();
            for (var u = GridSize/2f; u < parent.Template.Bounds.size.x; u += GridSize)
            {
                for (var v = GridSize / 2f; v < parent.Template.Bounds.size.z; v += GridSize)
                {
                    var cell = GridManager.GetCell(new Vector3(u, 0, v));
                    var cellMax = GridManager.GetCellMax(cell).x0z() + parent.Template.Bounds.min;
                    var cellMin = GridManager.GetCellCenter(cell).x0z() + parent.Template.Bounds.min;
                    if (!parent.Template.Bounds.Contains(cellMax) || !parent.Template.Bounds.Contains(cellMin))
                    {
                        continue;
                    }
                    var val = mask.BilinearSample(new Vector2(u / parent.Template.Bounds.size.x, v / parent.Template.Bounds.size.z));
                    Mask.SetValue(cell, val);
                }
            }
        }

        public void SetMaskFromTexture(WorldStampCreator parent, Texture2D tex)
        {
            GridSize = Math.Max(MinMaskRes, Math.Max(parent.Template.Bounds.size.x, parent.Template.Bounds.size.z) / MaskResolution);
            Mask.Clear();
            for (var u = 0f; u < parent.Template.Bounds.size.x; u += GridSize)
            {
                for (var v = 0f; v < parent.Template.Bounds.size.z; v += GridSize)
                {
                    var cell = GridManager.GetCell(new Vector3(u, 0, v));
                    var cellMax = GridManager.GetCellMax(cell).x0z() + parent.Template.Bounds.min;
                    var cellMin = GridManager.GetCellCenter(cell).x0z() + parent.Template.Bounds.min;
                    if (!parent.Template.Bounds.Contains(cellMax) || !parent.Template.Bounds.Contains(cellMin))
                    {
                        continue;
                    }
                    var val = tex.GetPixelBilinear(u / parent.Template.Bounds.size.x, v / parent.Template.Bounds.size.z).grayscale;
                    Mask.SetValue(cell, val);
                }
            }
        }

        private void LoadFromTexture(WorldStampCreator parent)
        {
            var path = EditorUtility.OpenFilePanel("Load Texture Into Mask", "Assets", "png");
            if (!string.IsNullOrEmpty(path))
            {
                var tex = new Texture2D(0, 0);
                tex.LoadImage(File.ReadAllBytes(path));
                SetMaskFromTexture(parent, tex);
                UnityEngine.Object.DestroyImmediate(tex);
            }
        }

        public override void PreviewInDataInspector()
        {
            throw new NotImplementedException();
        }

        public override void Clear()
        {
            Mask.Clear();
        }

        protected override void CommitInternal(WorldStampData data, WorldStamp stamp)
        {
            Mask.OnBeforeSerialize();
            data.Mask = Mask.JSONClone();
            data.GridSize = GridSize;
        }

        public override bool CanDisable
        {
            get { return false; }
        }
    }
}
#endif
