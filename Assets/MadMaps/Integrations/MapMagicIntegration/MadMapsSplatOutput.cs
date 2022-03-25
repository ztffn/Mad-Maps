// Taken with some small modifications from Map Magic (https://assetstore.unity.com/packages/tools/terrain/mapmagic-world-generator-56762)
// All rights reserved by the original creator.

#if MAPMAGIC
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using MapMagic;
using MadMaps.Common;
using MadMaps.Terrains;

#if UN_MapMagic
using uNature.Core.Extensions.MapMagicIntegration;
using uNature.Core.FoliageClasses;
#endif

namespace MadMaps.Terrains.MapMagicIntegration
{
	[System.Serializable]
	[GeneratorMenu(menu = "Mad Maps", name = "Mad Maps Textures", disengageable = true, priority = -1, helpLink = "https://gitlab.com/denispahunov/mapmagic/wikis/output_generators/Textures")]
	public class MadMapsSplatOutput : OutputGenerator
	{
		public string LayerName = "MapMagic";

		//layer
		public class Layer
		{
			public Input input = new Input(InoutType.Map);
			public Output output = new Output(InoutType.Map);
			public string name = "Layer";
			public float opacity = 1;
			public SplatPrototypeWrapper Wrapper;

			public void OnGUI (Layout layout, bool selected, int num) 
			{
				layout.margin = 20; layout.rightMargin = 20;
				layout.Par(20);

				if (num != 0) input.DrawIcon(layout);
				if (selected) layout.Field(ref name, rect: layout.Inset());
				else layout.Label(name, rect: layout.Inset());
				output.DrawIcon(layout);

				layout.Field(ref Wrapper);

				if (Wrapper && selected)
				{
					layout.Par(2);
					layout.Par(60); //not 65
					Wrapper.Texture = layout.Field(Wrapper.Texture, rect: layout.Inset(60));
					Wrapper.NormalMap = layout.Field(Wrapper.NormalMap, rect: layout.Inset(60));
					layout.Par(2);

					layout.margin = 5; layout.rightMargin = 5; layout.fieldSize = 0.6f;
					//layout.SmartField(ref downscale, "Downscale", min:1, max:8); downscale = Mathf.ClosestPowerOfTwo(downscale);
					opacity = layout.Field(opacity, "Opacity", min: 0);
					Wrapper.TileSize = layout.Field(Wrapper.TileSize, "Size");
					Wrapper.TileOffset = layout.Field(Wrapper.TileOffset, "Offset");
					Wrapper.SpecularColor = layout.Field(Wrapper.SpecularColor, "Specular");
					Wrapper.Smoothness = layout.Field(Wrapper.Smoothness, "Smooth", max: 1);
					Wrapper.Metallic = layout.Field(Wrapper.Metallic, "Metallic", max: 1);
				}
			}
		}

		//layer
		public Layer[] baseLayers = new Layer[] { new Layer() { name = "Background" } };
		public int selected;
		
		public void UnlinkBaseLayer (int p, int n)
		{
			if (baseLayers.Length == 0) return; //no base layer
			if (baseLayers[0].input.link != null) 
				baseLayers[0].input.Link(null, null);
		}
		public void UnlinkBaseLayer (int n) { UnlinkBaseLayer(0,0); }
		
		public void UnlinkLayer (int num)
		{
			baseLayers[num].input.Link(null,null); //unlink input
			baseLayers[num].output.UnlinkInActiveGens(); //try to unlink output
		}


		public static Texture2D _defaultTex;
		public static Texture2D defaultTex { get { if (_defaultTex == null) _defaultTex = Extensions.ColorTexture(2, 2, new Color(0.75f, 0.75f, 0.75f, 0f)); return _defaultTex; } }

		//public class SplatsTuple { public float[,,] array; public SplatPrototype[] prototypes; }

		//generator
		public override IEnumerable<Input> Inputs()
		{
			if (baseLayers == null) baseLayers = new Layer[0];
			for (int i = 0; i < baseLayers.Length; i++)
				if (baseLayers[i].input != null)
					yield return baseLayers[i].input;
		}
		public override IEnumerable<Output> Outputs()
		{
			if (baseLayers == null) baseLayers = new Layer[0];
			for (int i = 0; i < baseLayers.Length; i++)
				if (baseLayers[i].output != null)
					yield return baseLayers[i].output;
		}

		//get static actions using instance
		public override MapMagic.Action<MapMagic.CoordRect, Chunk.Results, GeneratorsAsset, Chunk.Size, Func<float,bool>> GetProces () { return Process; }
		public override Func<MapMagic.CoordRect, Terrain, object, Func<float,bool>, IEnumerator> GetApply () { return Apply; }
		public override Action<MapMagic.CoordRect, Terrain> GetPurge () { return Purge; }


		public override void Generate (MapMagic.CoordRect rect, Chunk.Results results, Chunk.Size terrainSize, int seed, Func<float,bool> stop = null)
		{
			if ((stop!=null && stop(0)) || !enabled) return;

			//loading inputs
			Matrix[] matrices = new Matrix[baseLayers.Length];
			for (int i = 0; i < baseLayers.Length; i++)
			{
				if (baseLayers[i].input != null)
				{
					matrices[i] = (Matrix)baseLayers[i].input.GetObject(results);
					if (matrices[i] != null) matrices[i] = matrices[i].Copy(null);
				}
				if (matrices[i] == null) matrices[i] = new Matrix(rect);
			}

			//background matrix
			//matrices[0] = terrain.defaultMatrix; //already created
			matrices[0].Fill(1);

			//populating opacity array
			float[] opacities = new float[matrices.Length];
			for (int i = 0; i < baseLayers.Length; i++)
				opacities[i] = baseLayers[i].opacity;
			opacities[0] = 1;

			//blending layers
			Matrix.BlendLayers(matrices, opacities);

			//saving changed matrix results
			for (int i = 0; i < baseLayers.Length; i++)
			{
				if (stop!=null && stop(0)) return; //do not write object is generating is stopped
				baseLayers[i].output.SetObject(results, matrices[i]);
			}
		}

		public static void Process (MapMagic.CoordRect rect, Chunk.Results results, GeneratorsAsset gens, Chunk.Size terrainSize, Func<float,bool> stop = null)
		{
			if (stop!=null && stop(0)) return;

			//gathering prototypes and matrices lists
			List<SplatPrototypeWrapper> prototypesList = new List<SplatPrototypeWrapper>();
			List<float> opacities = new List<float>();
			List<Matrix> matrices = new List<Matrix>();
			List<Matrix> biomeMasks = new List<Matrix>();

			foreach (MadMapsSplatOutput gen in gens.GeneratorsOfType<MadMapsSplatOutput>(onlyEnabled: true, checkBiomes: true))
			{
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

				for (int i = 0; i < gen.baseLayers.Length; i++)
				{
				    if(!gen.baseLayers[i].Wrapper)
					{
						continue;
					}

					//reading output directly
					Output output = gen.baseLayers[i].output;
					if (stop!=null && stop(0)) return; //checking stop before reading output
					if (!results.results.ContainsKey(output)) continue;
					Matrix matrix = (Matrix)results.results[output];
					matrix.Clamp01();

					//adding to lists
					matrices.Add(matrix);
					biomeMasks.Add(gen.biome == null ? null : biomeMask);
					prototypesList.Add(gen.baseLayers[i].Wrapper);
					opacities.Add(gen.baseLayers[i].opacity);
				}
			}

			//optimizing matrices list if they are not used
//			for (int i = matrices.Count - 1; i >= 0; i--)
//				if (opacities[i] < 0.001f || matrices[i].IsEmpty() || (biomeMasks[i] != null && biomeMasks[i].IsEmpty()))
//				{ prototypesList.RemoveAt(i); opacities.RemoveAt(i); matrices.RemoveAt(i); biomeMasks.RemoveAt(i); }

			//creating array
			float[,,] splats3D = new float[terrainSize.resolution, terrainSize.resolution, prototypesList.Count];
			if (matrices.Count == 0) { results.apply.CheckAdd(typeof(MadMapsSplatOutput), new TupleSet<float[,,], SplatPrototypeWrapper[]>(splats3D, new SplatPrototypeWrapper[0]), replace: true); return; }

			//filling array
			if (stop!=null && stop(0)) return;

			int numLayers = matrices.Count;
			int maxX = splats3D.GetLength(0); int maxZ = splats3D.GetLength(1); //MapMagic.instance.resolution should not be used because of possible lods
																				//MapMagic.CoordRect rect =  matrices[0].rect;

			float[] values = new float[numLayers]; //row, to avoid reading/writing 3d array (it is too slow)

			for (int x = 0; x < maxX; x++)
				for (int z = 0; z < maxZ; z++)
				{
					int pos = rect.GetPos(x + rect.offset.x, z + rect.offset.z);
					float sum = 0;

					//getting values
					for (int i = 0; i < numLayers; i++)
					{
						float val = matrices[i].array[pos];
						if (biomeMasks[i] != null) val *= biomeMasks[i].array[pos]; //if mask is not assigned biome was ignored, so only main outs with mask==null left here
						if (val < 0) val = 0; if (val > 1) val = 1;
						sum += val; //normalizing: calculating sum
						values[i] = val;
					}

					//setting color
					for (int i = 0; i < numLayers; i++) splats3D[z, x, i] = values[i] / sum;
				}

			//pushing to apply
			if (stop!=null && stop(0)) return;
			TupleSet<float[,,], SplatPrototypeWrapper[]> splatsTuple = new TupleSet<float[,,], SplatPrototypeWrapper[]>(splats3D, prototypesList.ToArray());
			results.apply.CheckAdd(typeof(MadMapsSplatOutput), splatsTuple, replace: true);
		}

		public IEnumerator Apply(MapMagic.CoordRect rect, Terrain terrain, object dataBox, Func<float,bool> stop= null)
		{
			TupleSet<float[,,], SplatPrototypeWrapper[]> splatsTuple = (TupleSet<float[,,], SplatPrototypeWrapper[]>)dataBox;
			float[,,] splats3D = splatsTuple.item1;
			SplatPrototypeWrapper[] prototypes = splatsTuple.item2;

			if (splats3D.GetLength(2) == 0) { Purge(rect,terrain); yield break; }

			//TerrainData data = terrain.terrainData;

			//setting resolution
			//int size = splats3D.GetLength(0);
			//if (data.alphamapResolution != size) data.alphamapResolution = size;

			//checking prototypes texture
			for (int i = 0; i < prototypes.Length; i++)
				if (prototypes[i].Texture == null) prototypes[i].Texture = defaultTex;
			yield return null;

			//welding
			if (MapMagic.MapMagic.instance != null && MapMagic.MapMagic.instance.splatsWeldMargins!=0)
			{
				MapMagic.Coord coord = MapMagic.Coord.PickCell(rect.offset, MapMagic.MapMagic.instance.resolution);
				//Chunk chunk = MapMagic.MapMagic.instance.chunks[coord.x, coord.z];
				
				Chunk neigPrevX = MapMagic.MapMagic.instance.chunks[coord.x-1, coord.z];
				if (neigPrevX!=null && neigPrevX.worker.ready) WeldTerrains.WeldSplatToPrevX(ref splats3D, neigPrevX.terrain, MapMagic.MapMagic.instance.splatsWeldMargins);

				Chunk neigNextX = MapMagic.MapMagic.instance.chunks[coord.x+1, coord.z];
				if (neigNextX!=null && neigNextX.worker.ready) WeldTerrains.WeldSplatToNextX(ref splats3D, neigNextX.terrain, MapMagic.MapMagic.instance.splatsWeldMargins);

				Chunk neigPrevZ = MapMagic.MapMagic.instance.chunks[coord.x, coord.z-1];
				if (neigPrevZ!=null && neigPrevZ.worker.ready) WeldTerrains.WeldSplatToPrevZ(ref splats3D, neigPrevZ.terrain, MapMagic.MapMagic.instance.splatsWeldMargins);

				Chunk neigNextZ = MapMagic.MapMagic.instance.chunks[coord.x, coord.z+1];
				if (neigNextZ!=null && neigNextZ.worker.ready) WeldTerrains.WeldSplatToNextZ(ref splats3D, neigNextZ.terrain, MapMagic.MapMagic.instance.splatsWeldMargins);
			}
			yield return null;

			terrain.terrainData.splatPrototypes = new[] {new SplatPrototype(){texture = defaultTex}};    // To stop MapMagic purging what we're doing here alter on...

            var wrapper = terrain.gameObject.GetOrAddComponent<TerrainWrapper>();
            var MMTerrainLayer = wrapper.GetLayer<MMTerrainLayer>(LayerName, false, true);
            MMTerrainLayer.SplatData.Clear();

            var splatWidth = splats3D.GetLength(0);
            var splatHeight = splats3D.GetLength(1);

            if (terrain.terrainData.alphamapResolution != splatWidth)
            {
                Debug.Log("Set alphamapResolution to " + splatWidth);
                terrain.terrainData.alphamapResolution = splatWidth;
            }

            var data = new float[splatWidth, splatHeight];
            for (int i = 0; i < prototypes.Length; i++)
            {
                var splatPrototypeWrapper = prototypes[i];
                for (var u = 0; u < splatWidth; ++u)
                {
                    for (var v = 0; v < splatHeight; ++v)
                    {
                        data[v, u] = splats3D[u, v, i];
                    }
                }
                MMTerrainLayer.SetSplatmap(splatPrototypeWrapper, 0, 0, data, splatWidth);
            }

			global::MapMagic.MapMagic.OnApplyCompleted -= MapMagicIntegrationUtilities.MapMagicOnOnApplyCompleted;
            global::MapMagic.MapMagic.OnApplyCompleted += MapMagicIntegrationUtilities.MapMagicOnOnApplyCompleted;

			wrapper.SetDirtyAbove(MMTerrainLayer);
			yield return null;
		}

		public static void Purge(MapMagic.CoordRect rect, Terrain terrain)
		{
			//skipping if already purged
			if (terrain.terrainData.alphamapResolution<=16) return; //using 8 will return resolution to 16

			SplatPrototype[] prototypes = new SplatPrototype[1];
			if (prototypes[0] == null) prototypes[0] = new SplatPrototype();
			if (prototypes[0].texture == null) prototypes[0].texture = defaultTex;
			terrain.terrainData.splatPrototypes = prototypes;

			float[,,] emptySplats = new float[16, 16, 1];
			for (int x = 0; x < 16; x++)
				for (int z = 0; z < 16; z++)
					emptySplats[z, x, 0] = 1;

			terrain.terrainData.alphamapResolution = 16;
			terrain.terrainData.SetAlphamaps(0, 0, emptySplats);
		}

		public override void OnGUI(GeneratorsAsset gens)
		{
			layout.fieldSize = .6f;
            layout.Field(ref LayerName, "Layer");
            layout.fieldSize = .5f;
			//Layer buttons
			layout.Par();
			layout.Label("Layers:", layout.Inset(0.4f));

			layout.DrawArrayAdd(ref baseLayers, ref selected, rect:layout.Inset(0.15f), reverse:true, createElement:() => new Layer(), onAdded:UnlinkBaseLayer );
			layout.DrawArrayRemove(ref baseLayers, ref selected, rect:layout.Inset(0.15f), reverse:true, onBeforeRemove:UnlinkLayer, onRemoved:UnlinkBaseLayer);
			layout.DrawArrayDown(ref baseLayers, ref selected, rect:layout.Inset(0.15f), dispUp:true, onSwitch:UnlinkBaseLayer);
			layout.DrawArrayUp(ref baseLayers, ref selected, rect:layout.Inset(0.15f), dispDown:true, onSwitch:UnlinkBaseLayer);

			//layers
			layout.Par(3);
			for (int num=baseLayers.Length-1; num>=0; num--)
				layout.DrawLayer(baseLayers[num].OnGUI, ref selected, num);
		}
	}
}
#endif