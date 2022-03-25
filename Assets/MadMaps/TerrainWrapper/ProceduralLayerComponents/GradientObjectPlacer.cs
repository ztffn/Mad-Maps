using System;
using System.Collections.Generic;
using MadMaps.Common;
using MadMaps.Common.GenericEditor;
using MadMaps.WorldStamps;
using UnityEngine;
using Random = System.Random;

namespace MadMaps.Terrains
{
    [Name("Objects/Gradient Object Placer")]
    public class GradientObjectPlacer : ProceduralLayerComponent
    {
        public float RequiredY = .9f;
        public float StepDistance = 1;
        public float MinDistance = 10;
        public int MaxCount = 100;

        public List<GameObject> Prefabs = new List<GameObject>();
        public Vec3MinMax Offset = new Vec3MinMax(Vector3.zero, Vector3.zero);
        public Vec3MinMax Scale = new Vec3MinMax(Vector3.one, Vector3.one);
        public Vec3MinMax Rotation = new Vec3MinMax(Vector3.zero, Vector3.zero);

        public override ApplyTiming Timing
        {
            get { return ApplyTiming.Instant; }
        }

        public override string HelpURL
        {
            get { return "http://lrtw.net/madmaps/index.php?title=Gradient_Object_Placer"; }
        }

        public override void Apply(ProceduralLayer layer, TerrainWrapper wrapper)
        {
            const float margin = 0.01f;

            var seed = layer.Seed;
            var rand = new Random(seed);

            var tBounds = wrapper.Terrain.GetBounds();
            var step = StepDistance/tBounds.size.x;

            var cellCount = Mathf.CeilToInt(tBounds.size.x / MinDistance);
            bool[,] proximityCheck = new bool[cellCount, cellCount];

            int counter = 0;
            for (var u = margin; u < 1 - margin; u += step)
            {
                if (counter > MaxCount)
                {
                    break;
                }

                for (var v = margin; v < 1 - margin; v += step)
                {
                    if (counter > MaxCount)
                    {
                        break;
                    }

                    var cellX = Mathf.FloorToInt(u * cellCount);
                    var cellY = Mathf.FloorToInt(v * cellCount);
                    if (proximityCheck[cellX, cellY])
                    {
                        continue;
                    }

                    var normalizedPos = new Vector2(u, v);
                    var normal = wrapper.GetNormalFromHeightmap(normalizedPos);
                    if (normal.y > RequiredY)
                    {
                        continue;
                    }

                    var prefab = Prefabs.Random();
                    var prefabEntry = new PrefabObjectData()
                    {
                        Guid = Guid.NewGuid().ToString(),
                        Prefab = prefab,
                        Position = normalizedPos.x0z() + Offset.GetRand(rand),
                        Rotation = Rotation.GetRand(rand),
                        Scale = Scale.GetRand(rand),
                    };

                    layer.Objects.Add(prefabEntry);
                    counter++;
                    proximityCheck[cellX, cellY] = true;
                }
            }
            Debug.Log(string.Format("GradientObjectPlacer placed {0} objects.", counter));
        }
    }
}