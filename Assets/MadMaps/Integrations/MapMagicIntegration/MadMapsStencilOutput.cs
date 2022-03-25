#if MAPMAGIC
using System;
using System.Collections;
using System.Collections.Generic;
using MadMaps.Common;
using MadMaps.Common.Collections;
using MadMaps.Terrains;
using MapMagic;
using UnityEngine;

namespace MadMaps.Terrains.MapMagicIntegration
{
    [System.Serializable]
    [GeneratorMenu(menu = "Mad Maps", name = "MadMaps Stencil", disengageable = true)]
    public class MadMapsStencilOutput : OutputGenerator
    {
        public string LayerName = "MapMagic";

        public Input input = new Input(InoutType.Map);
        public Output output = new Output(InoutType.Map);

        public override IEnumerable<Input> Inputs() { yield return input; }
        public override IEnumerable<Output> Outputs() { if (output == null) output = new Output(InoutType.Map); yield return output; }

        public override Action<global::MapMagic.CoordRect, Chunk.Results, GeneratorsAsset, Chunk.Size, Func<float, bool>> GetProces() { return Process; }
        public override Func<global::MapMagic.CoordRect, Terrain, object, Func<float, bool>, IEnumerator> GetApply() { return Apply; }
        public override Action<global::MapMagic.CoordRect, Terrain> GetPurge() { return Purge; }

        public void Process(global::MapMagic.CoordRect rect, Chunk.Results results, GeneratorsAsset gens, Chunk.Size terrainSize, Func<float, bool> stop = null)
        {
            if (stop != null && stop(0)) return;

            Matrix result = new Matrix(rect);
            foreach (MadMapsStencilOutput gen in gens.GeneratorsOfType<MadMapsStencilOutput>(onlyEnabled: true, checkBiomes: true))
            {
                Matrix input = (Matrix)gen.input.GetObject(results);
                if (input == null) continue;

                //loading biome matrix
                Matrix biomeMask = null;
                if (gen.biome != null)
                {
                    object biomeMaskObj = gen.biome.mask.GetObject(results);
                    if (biomeMaskObj == null) continue; //adding nothing if biome has no mask
                    biomeMask = (Matrix)biomeMaskObj;
                    if (biomeMask == null) continue;
                    if (biomeMask.IsEmpty()) continue; //optimizing empty biomes
                }

                //adding to final result
                if (gen.biome == null) result.Add(input);
                else if (biomeMask != null) result.Add(input, biomeMask);
            }

            //creating 2d array
            if (stop != null && stop(0)) return;

            int heightSize = terrainSize.resolution;
            var stencil = new Stencil(heightSize, heightSize);
            int key = 1;
            for (int x = 0; x < heightSize - 1; x++)
            {
                for (int z = 0; z < heightSize - 1; z++)
                {
                    float strength;
                    int disposableKey;
                    MiscUtilities.DecompressStencil(stencil[x, z], out disposableKey, out strength);

                    var writeValue = result[x + results.heights.rect.offset.x, z + results.heights.rect.offset.z];

                    stencil[x, z] = MiscUtilities.CompressStencil(key, strength + writeValue);
                }
            }

            //pushing to apply
            if (stop != null && stop(0)) return;
            results.apply.CheckAdd(typeof(MadMapsStencilOutput), stencil, replace: true);
        }

        public void Purge(global::MapMagic.CoordRect rect, Terrain terrain)
        {
            var wrapper = terrain.GetComponent<TerrainWrapper>();
            if (wrapper == null)
            {
                return;
            }
            var MMTerrainLayer = wrapper.GetLayer<MMTerrainLayer>(LayerName);
            if (MMTerrainLayer == null || MMTerrainLayer.Heights == null)
            {
                return;
            }
            MMTerrainLayer.Stencil.Clear();
            wrapper.Dirty = true;
        }

        public IEnumerator Apply(global::MapMagic.CoordRect rect, Terrain terrain, object dataBox, Func<float, bool> stop = null)
        {
            if (terrain == null || terrain.terrainData == null) yield break; //chunk removed during apply

            var stencil = (Stencil)dataBox;

            var wrapper = terrain.gameObject.GetOrAddComponent<TerrainWrapper>();
            var MMTerrainLayer = wrapper.GetLayer<MMTerrainLayer>(LayerName, false, true);

            MMTerrainLayer.Stencil = stencil;
            yield return null;

            global::MapMagic.MapMagic.OnApplyCompleted -= MapMagicIntegrationUtilities.MapMagicOnOnApplyCompleted;
            global::MapMagic.MapMagic.OnApplyCompleted += MapMagicIntegrationUtilities.MapMagicOnOnApplyCompleted;

            wrapper.SetDirtyAbove(MMTerrainLayer);
        }

        public override void OnGUI(GeneratorsAsset gens)
        {
            layout.fieldSize = .6f;
            layout.Field(ref LayerName, "Layer");
            layout.fieldSize = .5f;

            layout.Par(20); input.DrawIcon(layout, "Stencil");
            layout.Par(5);

            if (output == null) output = new Output(InoutType.Map);
        }
    }
}
#endif