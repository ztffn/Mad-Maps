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
	[GeneratorMenu(menu = "Mad Maps", name = "Mad Maps Grass", disengageable = true, helpLink = "https://gitlab.com/denispahunov/mapmagic/wikis/output_generators/Grass")]
	public class MadMapsGrassOutput : OutputGenerator
	{
		public string LayerName = "MapMagic";

		//layer
		public class Layer
		{
			public Input input = new Input(InoutType.Map);
			public Output output = new Output(InoutType.Map);

			public DetailPrototypeWrapper Wrapper;
			public string name;
			public float density = 0.5f;
			public enum GrassRenderMode { Grass, GrassBillboard, VertexLit, Object };
			public GrassRenderMode renderMode;

			public void OnGUI (Layout layout, bool selected, int num) 
			{
				layout.margin = 20; layout.rightMargin = 20; layout.fieldSize = 1f;
				layout.Par(20);

				input.DrawIcon(layout);
				if (selected) layout.Field(ref name, rect: layout.Inset());
				else layout.Label(name, rect: layout.Inset());
				if (output == null) output = new Output(InoutType.Map); //backwards compatibility
				output.DrawIcon(layout);

				layout.Field(ref Wrapper);
                if (Wrapper == null)
                {
                    return;
                }

				if (selected)
				{
					layout.margin = 5; layout.rightMargin = 10; layout.fieldSize = 0.6f;
					layout.fieldSize = 0.65f;

					//setting render mode
					if (renderMode == GrassRenderMode.Grass && Wrapper.RenderMode != DetailRenderMode.Grass) //loading outdated
					{
						if (Wrapper.RenderMode == DetailRenderMode.GrassBillboard) renderMode = GrassRenderMode.GrassBillboard;
						else renderMode = GrassRenderMode.VertexLit;
					}

					renderMode = layout.Field(renderMode, "Mode");

					if (renderMode == GrassRenderMode.Object || renderMode == GrassRenderMode.VertexLit)
					{
						Wrapper.Prototype = layout.Field(Wrapper.Prototype, "Object");
						//Wrapper.PrototypeTexture = null; //otherwise this texture will be included to build even if not displayed
						//Wrapper.UsePrototypeMesh = true;
					}
					else
					{
						layout.Par(60); //not 65
						layout.Inset((layout.field.width - 60) / 2);
						Wrapper.PrototypeTexture = layout.Field(Wrapper.PrototypeTexture, rect: layout.Inset(60));
						//Wrapper.Prototype = null; //otherwise this object will be included to build even if not displayed
						//Wrapper.UsePrototypeMesh = false;
						layout.Par(2);
					}
					switch (renderMode)
					{
						case GrassRenderMode.Grass: Wrapper.RenderMode = DetailRenderMode.Grass; break;
						case GrassRenderMode.GrassBillboard: Wrapper.RenderMode = DetailRenderMode.GrassBillboard; break;
						case GrassRenderMode.VertexLit: Wrapper.RenderMode = DetailRenderMode.VertexLit; break;
						case GrassRenderMode.Object: Wrapper.RenderMode = DetailRenderMode.Grass; break;
					}

					density = layout.Field(density, "Density", max: 50);
					//Wrapper.bendFactor = layout.Field(Wrapper.bendFactor, "Bend");
					Wrapper.DryColor = layout.Field(Wrapper.DryColor, "Dry");
					Wrapper.HealthyColor = layout.Field(Wrapper.HealthyColor, "Healthy");

					Vector2 temp = new Vector2(Wrapper.MinWidth, Wrapper.MaxWidth);
					layout.Field(ref temp, "Width", max: 10);
					Wrapper.MinWidth = temp.x; Wrapper.MaxWidth = temp.y;

					temp = new Vector2(Wrapper.MinHeight, Wrapper.MaxHeight);
					layout.Field(ref temp, "Height", max: 10);
					Wrapper.MinHeight = temp.x; Wrapper.MaxHeight = temp.y;

					Wrapper.NoiseSpread = layout.Field(Wrapper.NoiseSpread, "Noise", max: 1);
				}
			}

			//public void OnAdd(int n) { name = "Grass"; }
			//public void OnRemove(int n) { input.Link(null, null); }
			//public void OnSwitch(int o, int n) { }
		}
		public Layer[] baseLayers = new Layer[0];
		public int selected;

		public void UnlinkBaseLayer (int p, int n)
		{
			if (baseLayers[0].input.link != null) 
				baseLayers[0].input.Link(null, null);
		}
		public void UnlinkBaseLayer (int n) { UnlinkBaseLayer(0,0); }
		
		public void UnlinkLayer (int num)
		{
			baseLayers[num].input.Link(null,null); //unlink input
			baseLayers[num].output.UnlinkInActiveGens(); //try to unlink output
		}


		//params
		public Input maskIn = new Input(Generator.InoutType.Map);
		public static int patchResolution = 16;
		public bool obscureLayers = false;

		//public class GrassTuple { public int[][,] details; public DetailPrototype[] prototypes; }

		//generator
		public override IEnumerable<Input> Inputs()
		{
			if (maskIn == null) maskIn = new Input(InoutType.Map); //for backwards compatibility, input should not be null
			yield return maskIn;

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


		public override void Generate(MapMagic.CoordRect rect, Chunk.Results results, Chunk.Size terrainSize, int seed, Func<float,bool> stop= null)
		{
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

			//blending layers
			if (obscureLayers) Matrix.BlendLayers(matrices);

			//masking layers
			Matrix mask = (Matrix)maskIn.GetObject(results);
			if (mask != null)
				for (int i = 0; i < matrices.Length; i++) matrices[i].Multiply(mask);

			//saving outputs
			for (int i = 0; i < baseLayers.Length; i++)
			{
				if (stop!=null && stop(0)) return; //do not write object is generating is stopped
				if (baseLayers[i].output == null) baseLayers[i].output = new Output(InoutType.Map); //back compatibility
				baseLayers[i].output.SetObject(results, matrices[i]);
			}
		}

		public static void Process(MapMagic.CoordRect rect, Chunk.Results results, GeneratorsAsset gens, Chunk.Size terrainSize, Func<float,bool> stop = null)
		{
			if (stop!=null && stop(0)) return;

			//values to calculate density
			float pixelSize = terrainSize.pixelSize;
			float pixelSquare = pixelSize * pixelSize;

			//a random needed to convert float to int
			InstanceRandom rnd = new InstanceRandom(terrainSize.Seed(rect));

			//calculating the totoal number of prototypes
			int prototypesNum = 0;
			foreach (MadMapsGrassOutput grassOut in gens.GeneratorsOfType<MadMapsGrassOutput>())
				prototypesNum += grassOut.baseLayers.Length;

			//preparing results
			List<int[,]> detailsList = new List<int[,]>();
			List<DetailPrototypeWrapper> prototypesList = new List<DetailPrototypeWrapper>();

			//filling result
			foreach (MadMapsGrassOutput gen in gens.GeneratorsOfType<MadMapsGrassOutput>(onlyEnabled: true, checkBiomes: true))
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
					if (stop!=null && stop(0)) return;

					//loading objects from input
					//Matrix matrix = (Matrix)gen.baseLayers[i].input.GetObject(chunk);
					//if (matrix == null) continue;

					//reading output directly
					Output output = gen.baseLayers[i].output;
					if (stop!=null && stop(0)) return; //checking stop before reading output
					if (!results.results.ContainsKey(output)) continue;
					Matrix matrix = (Matrix)results.results[output];

					//filling array
					int[,] detail = new int[matrix.rect.size.x, matrix.rect.size.z];
					for (int x = 0; x < matrix.rect.size.x; x++)
						for (int z = 0; z < matrix.rect.size.z; z++)
						{
							float val = matrix[x + matrix.rect.offset.x, z + matrix.rect.offset.z];
							float biomeVal = 1;
							if (gen.biome != null)
							{
								if (biomeMask == null) biomeVal = 0;
								else biomeVal = biomeMask[x + matrix.rect.offset.x, z + matrix.rect.offset.z];
							}
							detail[z, x] = rnd.RandomToInt(val * gen.baseLayers[i].density * pixelSquare * biomeVal);
						}

					//adding to arrays
					detailsList.Add(detail);
					prototypesList.Add(gen.baseLayers[i].Wrapper);
				}
			}

			//pushing to apply
			if (stop!=null && stop(0)) return;

            TupleSet<int[][,], DetailPrototypeWrapper[]> grassTuple = new TupleSet<int[][,], DetailPrototypeWrapper[]>(detailsList.ToArray(), prototypesList.ToArray());

            results.apply.CheckAdd(typeof(MadMapsGrassOutput), grassTuple, replace: true);
        }


        public IEnumerator Apply(MapMagic.CoordRect rect, Terrain terrain, object dataBox, Func<float,bool> stop= null)
		{
			var wrapper = terrain.gameObject.GetOrAddComponent<TerrainWrapper>();
            var MMTerrainLayer = wrapper.GetLayer<MMTerrainLayer>(LayerName, false, true);
            MMTerrainLayer.DetailData.Clear();

            var grassTuple = (TupleSet<int[][,], DetailPrototypeWrapper[]>)dataBox;
			int[][,] details = grassTuple.item1;
			DetailPrototypeWrapper[] prototypes = grassTuple.item2;
            
			for (int i = 0; i < details.Length; i++)
			{
				MMTerrainLayer.SetDetailMap(prototypes[i], 0, 0, details[i].Flip(), global::MapMagic.MapMagic.instance.resolution);
			}

			global::MapMagic.MapMagic.OnApplyCompleted -= MapMagicIntegrationUtilities.MapMagicOnOnApplyCompleted;
            global::MapMagic.MapMagic.OnApplyCompleted += MapMagicIntegrationUtilities.MapMagicOnOnApplyCompleted;

			wrapper.SetDirtyAbove(MMTerrainLayer);

			yield return null;
		}

		public void Purge(MapMagic.CoordRect rect, Terrain terrain)
		{
			//skipping if already purged
			if (terrain.terrainData.detailPrototypes.Length==0) return;

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
            MMTerrainLayer.DetailData.Clear();
            wrapper.Dirty = true;

		}

		public override void OnGUI(GeneratorsAsset gens)
		{
			layout.Par(20); maskIn.DrawIcon(layout, "Mask");
			layout.fieldSize = .6f;
            layout.Field(ref LayerName, "Layer");
            layout.fieldSize = .5f;
			layout.Field(ref patchResolution, "Patch Res", min:4, max:64, fieldSize:0.35f);
			patchResolution = Mathf.ClosestPowerOfTwo(patchResolution);
			layout.Field(ref obscureLayers, "Obscure Layers", fieldSize: 0.35f);
			
			//layer buttons
			layout.Par(3);
			layout.Par();
			layout.Label("Layers:", layout.Inset(0.4f));

			layout.DrawArrayAdd(ref baseLayers, ref selected, rect:layout.Inset(0.15f), reverse:true, createElement:() => new Layer() );
			layout.DrawArrayRemove(ref baseLayers, ref selected, rect:layout.Inset(0.15f), reverse:true, onBeforeRemove:UnlinkLayer);
			layout.DrawArrayDown(ref baseLayers, ref selected, rect:layout.Inset(0.15f), dispUp:true);
			layout.DrawArrayUp(ref baseLayers, ref selected, rect:layout.Inset(0.15f), dispDown:true);

			//layers
			layout.Par(3);
			for (int num=baseLayers.Length-1; num>=0; num--)
				layout.DrawLayer(baseLayers[num].OnGUI, ref selected, num);

			layout.fieldSize = 0.4f; layout.margin = 10; layout.rightMargin = 10;
			layout.Par(5);
		}

	}
}
#endif