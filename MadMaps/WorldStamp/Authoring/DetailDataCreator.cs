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

namespace MadMaps.WorldStamp.Authoring
{
    [Serializable]
    public class DetailDataCreator : WorldStampCreatorLayer
    {
        [NonSerialized]
        public List<CompressedDetailData> DetailData = new List<CompressedDetailData>();

        public List<DetailPrototypeWrapper> IgnoredDetails = new List<DetailPrototypeWrapper>();

        public override GUIContent Label
        {
            get
            {
                var nullCount = DetailData.Count(data => data.Wrapper == null);
                if(nullCount > 0)
                    return new GUIContent(string.Format("Details ({0}) ({1} need resolving)", DetailData.Count, nullCount));
                return new GUIContent(string.Format("Details ({0})", DetailData.Count));
            }
        }

        protected override bool HasDataPreview
        {
            get { return true; }
        }

        protected override void CaptureInternal(Terrain terrain, Bounds bounds)
        {
            DetailData.Clear();

            var min = terrain.WorldToDetailCoord(bounds.min);
            var max = terrain.WorldToDetailCoord(bounds.max);

            int width = max.x - min.x;
            int height = max.z - min.z;

            var prototypes = terrain.terrainData.detailPrototypes;
            var wrappers = TerrainLayerUtilities.ResolvePrototypes(prototypes);
            
            for (var i = 0; i < prototypes.Length; ++i)
            {
                DetailPrototypeWrapper wrapper;
                wrappers.TryGetValue(prototypes[i], out wrapper);
                if (wrapper != null && IgnoredDetails.Contains(wrapper))
                {
                    continue;
                }
                var sample = terrain.terrainData.GetDetailLayer(min.x, min.z, width, height, i);
                var data = new byte[width, height];
                int sum = 0;
                for (var dx = 0; dx < width; ++dx)
                {
                    for (var dz = 0; dz < height; ++dz)
                    {
                        var sampleData = sample[dz, dx];
                        data[dx, dz] = (byte)sampleData;
                        sum += sampleData;
                    }
                }
                if (sum > 0)
                {
                    DetailData.Add(new CompressedDetailData { Wrapper = wrapper, Data = new Serializable2DByteArray(data) });
                }
                else if (wrapper != null)
                {
                    Debug.Log(string.Format("WorldStamp Detail Capture: Ignored detail layer {0} as it appeared to be empty.", wrapper.name));
                }
                else
                {
                    Debug.Log(string.Format("WorldStamp Detail Capture: Unable to resolve prototype at index {0}.", i));
                }
            }
        }

        protected override void PreviewInSceneInternal(WorldStampCreator parent)
        {
            var bounds = parent.Template.Bounds;
            int counter = 0;
            int res = 32;
            foreach (var kvp in DetailData)
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
            if (NeedsRecapture)
            {
                EditorGUILayout.HelpBox("You need to Capture this layer to edit it.", MessageType.Info);
                return;
            }
            foreach (var compressedDetailData in DetailData)
            {
                EditorGUILayout.BeginHorizontal();
                compressedDetailData.Wrapper = (DetailPrototypeWrapper) EditorGUILayout.ObjectField(compressedDetailData.Wrapper,
                    typeof (DetailPrototypeWrapper), false);
                GUI.color = compressedDetailData.Wrapper != null && IgnoredDetails.Contains(compressedDetailData.Wrapper) ? Color.red : Color.white;
                GUI.enabled = compressedDetailData.Wrapper != null;
                if (GUILayout.Button("Ignore", EditorStyles.miniButton, GUILayout.Width(60)))
                {
                    if (IgnoredDetails.Contains(compressedDetailData.Wrapper))
                    {
                        IgnoredDetails.Remove(compressedDetailData.Wrapper);
                    }
                    else
                    {
                        IgnoredDetails.Add(compressedDetailData.Wrapper);
                    }
                }
                GUI.enabled = true;
                GUI.color = Color.white;
                EditorGUILayout.EndHorizontal();
            }
        }

        public override void PreviewInDataInspector()
        {
            DataInspector.SetData(DetailData.Select(x => x.Data).ToList(), DetailData.Select(x => x.Wrapper).ToList());
        }

        public override void Clear()
        {
            DetailData.Clear();
        }

        protected override void CommitInternal(WorldStampData data, WorldStamp stamp)
        {
            data.DetailData.Clear();
            for (int i = 0; i < DetailData.Count; i++)
            {
                var compressedDetailData = DetailData[i];
                data.DetailData.Add(compressedDetailData.JSONClone());
            }
        }
    }
}
#endif