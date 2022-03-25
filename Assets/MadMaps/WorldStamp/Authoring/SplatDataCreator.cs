using System;
using System.Collections.Generic;
using System.Linq;
using MadMaps.Common;
using MadMaps.Common.Collections;
using MadMaps.Terrains;
using MadMaps.Terrains.Lookups;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

namespace MadMaps.WorldStamps.Authoring
{
    [Serializable]
    public class SplatDataCreator : WorldStampCreatorLayer
    {
        [NonSerialized]
        public List<CompressedSplatData> SplatData = new List<CompressedSplatData>();

        public List<SplatPrototypeWrapper> IgnoredSplats = new List<SplatPrototypeWrapper>();

        public override GUIContent Label
        {
            get
            {
                var nullCount = SplatData.Count(data => data.Wrapper == null);
                if (nullCount > 0)
                    return new GUIContent(string.Format("Splats ({0}) ({1} need resolving)", SplatData.Count, nullCount));
                return new GUIContent(string.Format("Splats ({0})", SplatData.Count));
            }
        }

        protected override bool HasDataPreview
        {
            get { return true; }
        }

        protected override void CaptureInternal(Terrain terrain, Bounds bounds)
        {
            SplatData.Clear();
            var min = terrain.WorldToSplatCoord(bounds.min);
            var max = terrain.WorldToSplatCoord(bounds.max);

            int width = max.x - min.x;
            int height = max.z - min.z;

            var prototypes = terrain.terrainData.splatPrototypes;
            var wrappers = MMTerrainLayerUtilities.ResolvePrototypes(prototypes);

            var sampleSplats = terrain.terrainData.GetAlphamaps(min.x, min.z, width, height);

            for (var i = 0; i < prototypes.Length; ++i)
            {
                SplatPrototypeWrapper wrapper;
                wrappers.TryGetValue(prototypes[i], out wrapper);
                /*if (IgnoredSplats.Contains(wrapper))
                {
                    continue;
                }*/
                var data = new byte[width, height];
                float sum = 0;
                for (var dx = 0; dx < width; ++dx)
                {
                    for (var dz = 0; dz < height; ++dz)
                    {
                        var val = sampleSplats[dz, dx, i];
                        data[dx, dz] = (byte)Mathf.Clamp(val * 255f, 0, 255);
                        sum += val;
                    }
                }
                if (sum < 0.01f)
                {
                    Debug.Log(string.Format("WorldStamp Splat Capture: Ignored splat layer {0} as it appeared to be empty.", wrapper!=null ? wrapper.name : "Unresolved Splat"));
                    continue;
                }

                SplatData.Add(new CompressedSplatData { Wrapper = wrapper, Data = new Serializable2DByteArray(data) });
            }
        }

        protected override void PreviewInSceneInternal(WorldStampCreator parent)
        {
            var bounds = parent.Template.Bounds;
            int counter = 0;
            int res = 32;
            foreach (var kvp in SplatData)
            {
                int xStep = kvp.Data.Width / res;
                int zStep = kvp.Data.Height / res;
                Vector2 cellSize = new Vector2((xStep / (float)kvp.Data.Width) * bounds.size.x, (zStep / (float)kvp.Data.Height) * bounds.size.z);
                for (var u = 0; u < kvp.Data.Width; u += xStep)
                {
                    var fU = u / (float)kvp.Data.Width;
                    var wU = bounds.min.x + fU * bounds.size.x;
                    for (var v = 0; v < kvp.Data.Height; v += zStep)
                    {
                        var fV = v / (float)kvp.Data.Height;
                        var wV = bounds.min.z + fV * bounds.size.z;

                        var val = kvp.Data[u, v] / 255f;
                        HandleExtensions.DrawXZCell(new Vector3(wU, counter, wV), cellSize,
                            Quaternion.identity, ColorUtils.GetIndexColor(counter).WithAlpha(val));
                    }
                }
                counter++;
            }
        }

        protected override void OnExpandedGUI(WorldStampCreator parent)
        {
            if (SplatData.Count == 0)
            {
                EditorGUILayout.HelpBox("No Splats Found", MessageType.Info);
                return;
            }
            foreach (var compressedDetailData in SplatData)
            {
                EditorGUILayout.BeginHorizontal();
                compressedDetailData.Wrapper = (SplatPrototypeWrapper)EditorGUILayout.ObjectField(compressedDetailData.Wrapper,
                    typeof(SplatPrototypeWrapper), false);
                GUI.color = compressedDetailData.Wrapper != null && IgnoredSplats.Contains(compressedDetailData.Wrapper) ? Color.red : Color.white;
                GUI.enabled = compressedDetailData.Wrapper != null;
                if (GUILayout.Button("Ignore", EditorStyles.miniButton, GUILayout.Width(60)))
                {
                    if (IgnoredSplats.Contains(compressedDetailData.Wrapper))
                    {
                        IgnoredSplats.Remove(compressedDetailData.Wrapper);
                    }
                    else
                    {
                        IgnoredSplats.Add(compressedDetailData.Wrapper);
                    }
                }
                GUI.enabled = true;
                GUI.color = Color.white;
                EditorGUILayout.EndHorizontal();
            }
        }

        public override void PreviewInDataInspector()
        {
            DataInspector.SetData(SplatData.Select(x => (IDataInspectorProvider)x.Data).ToList(), SplatData.Select(x => (object)x.Wrapper).ToList());
        }

        public override void Clear()
        {
            SplatData.Clear();
        }

        protected override void CommitInternal(WorldStampData data, WorldStamp stamp)
        {
            data.SplatData.Clear();
            for (int i = 0; i < SplatData.Count; i++)
            {
                var compressedSplatData = SplatData[i];
                if (IgnoredSplats.Contains(compressedSplatData.Wrapper))
                {
                    continue;
                }
                data.SplatData.Add(compressedSplatData.JSONClone());
            }
        }
    }
}
#endif