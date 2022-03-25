using System;
using MadMaps.Common;
using MadMaps.Common.Collections;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

namespace MadMaps.WorldStamps.Authoring
{
    [Serializable]
    public class HeightmapDataCreator : WorldStampCreatorLayer
    {
        [NonSerialized]
        public Serializable2DFloatArray Heights;

        public bool AutoZeroLevel = true;

        [NonSerialized]
        [Common.GenericEditor.ShowIf("AutoZeroLevel", false)]
        public float ZeroLevel = 0;

        [NonSerialized]
        [HideInInspector]
        public float HeightSize;

        private WorldStampPreview _preview;

        private bool _dirty;

        public override GUIContent Label
        {
            get { return new GUIContent("Heightmap");}
        }

        protected override bool HasDataPreview
        {
            get { return true; }
        }

        public override void Dispose()
        {
            if(_preview != null)
            {
                _preview.Dispose();
                _preview = null;
            }
        }

        protected override void CaptureInternal(Terrain terrain, Bounds bounds)
        {
            var min = terrain.WorldToHeightmapCoord(bounds.min, TerrainX.RoundType.Floor);
            var max = terrain.WorldToHeightmapCoord(bounds.max, TerrainX.RoundType.Floor);

            int width = max.x - min.x;
            int height = max.z - min.z;

            float avgMarginHeight = 0; // If we want, we can have the stamp try to automatically find a good zero level by averaging the heights around the edge of the stamp
            int marginCount = 0;

            Heights = new Serializable2DFloatArray(width, height);
            float maxHeight = float.MinValue;
            var sampleHeights = terrain.terrainData.GetHeights(min.x, min.z, width, height);
            for (var dx = 0; dx < width; ++dx)
            {
                for (var dz = 0; dz < height; ++dz)
                {
                    var sample = sampleHeights[dz, dx];
                    Heights[dx, dz] = sample;

                    if(sample > maxHeight)
                    {
                        maxHeight = sample;
                    }

                    if (dx == 0 || dx == width-1 || dz == 0 || dz == height-1)
                    {
                        avgMarginHeight += sample;
                        marginCount++;
                    }
                }
            }
            if (AutoZeroLevel)
            {
                ZeroLevel = avgMarginHeight / marginCount;
            }
            for (var dx = 0; dx < width; ++dx)
            {
                for (var dz = 0; dz < height; ++dz)
                {
                    Heights[dx, dz] -= ZeroLevel;
                }
            }
            HeightSize = maxHeight - ZeroLevel;
            _dirty = true;
        }

        public override void Clear()
        {
            if(Heights == null)
            {
                return;
            }
            Heights.Clear();
            
        }

        protected override void CommitInternal(WorldStampData data, WorldStamp stamp)
        {
            data.Heights = Heights.JSONClone();
            data.ZeroLevel = ZeroLevel;
        }

        protected override void OnExpandedGUI(WorldStampCreator parent)
        {
            if(Enabled && (Heights == null || Heights.IsEmpty()))
            {
                NeedsRecapture = true;
            }

            EditorGUI.BeginChangeCheck();
            var tHeight = parent.Template.Terrain.terrainData.size.y;
            AutoZeroLevel = EditorGUILayout.Toggle("Auto Zero Level" + (AutoZeroLevel ? (ZeroLevel*tHeight).ToString() : string.Empty), AutoZeroLevel);
            if (!AutoZeroLevel)
            {
                EditorGUI.indentLevel++;
                
                var newHeightMin = EditorGUILayout.Slider("Zero Level", ZeroLevel * tHeight, 0, tHeight) / tHeight;
                EditorGUI.indentLevel--;
                if (newHeightMin != ZeroLevel)
                {
                    ZeroLevel = newHeightMin;
                    NeedsRecapture = true;
                    SceneView.RepaintAll();
                }
            }
            if (EditorGUI.EndChangeCheck())
            {
                NeedsRecapture = true;
            }
        }

        protected override void PreviewInSceneInternal(WorldStampCreator parent)
        {
            var bounds = parent.Template.Bounds;
            if (_preview == null || _preview.IsDisposed())
            {
                _preview = new WorldStampPreview();
                _dirty = true;
            }
            if (_dirty)
            {
                _preview.Invalidate(
                    Heights, () => bounds.size, () => bounds.center.xz().x0z(bounds.min.y + ZeroLevel * bounds.size.y), () => Vector3.one,
                    () => Quaternion.identity, () => bounds.size, true, null, null,
                    () => parent.SceneGUIOwner == this, 128);
                _dirty = false;
            }            
        }

        public override void PreviewInDataInspector()
        {
            DataInspector.SetData(Heights);
        }
    }
}
#endif