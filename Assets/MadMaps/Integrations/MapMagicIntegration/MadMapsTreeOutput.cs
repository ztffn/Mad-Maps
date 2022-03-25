// Taken with some small modifications from Map Magic (https://assetstore.unity.com/packages/tools/terrain/mapmagic-world-generator-56762)
// All rights reserved by the original creator.

#if MAPMAGIC
using System;
using System.Collections;
using System.Collections.Generic;
using MadMaps.Terrains;
using MadMaps.Common;
using MadMaps.Common.Collections;
using MapMagic;
using UnityEngine;
using System.Linq;

namespace MadMaps.Terrains.MapMagicIntegration
{
    [System.Serializable]
	[GeneratorMenu(menu = "Mad Maps", name = "Mad Maps Trees", disengageable = true, helpLink = "https://gitlab.com/denispahunov/mapmagic/wikis/output_generators/Trees")]
	public class MadMapsTreeOutput : OutputGenerator
	{
        public string LayerName = "MapMagic";
		public enum BiomeBlendType { Sharp, AdditiveRandom, NormalizedRandom, Scale }
		public static BiomeBlendType biomeBlendType = BiomeBlendType.AdditiveRandom;

		//layer
		public class Layer
		{
			public Input input = new Input(InoutType.Objects);
			public Output output = new Output(InoutType.Objects);

			public GameObject prefab;
			public bool relativeHeight = true;
			public bool rotate;
			public bool widthScale;
			public bool heightScale;
			public Color color = Color.white;
			public float bendFactor;

			public void OnCollapsedGUI(Layout layout)
			{
				layout.margin = 20; layout.rightMargin = 5; layout.fieldSize = 1f;
				layout.Par(20);
				input.DrawIcon(layout);
				layout.Field(ref prefab, rect: layout.Inset());
			}

			public void OnGUI (Layout layout, bool selected, int num) 
			{
				layout.margin = 20; layout.rightMargin = 5;
				layout.Par(20);

				input.DrawIcon(layout);
				layout.Field(ref prefab, rect: layout.Inset());

				if (selected)
				{
					layout.Par(); layout.Toggle(ref relativeHeight, rect: layout.Inset(20)); layout.Label("Relative Height", rect: layout.Inset(100));
					layout.Par(); layout.Toggle(ref rotate, rect: layout.Inset(20)); layout.Label("Rotate", rect: layout.Inset(45));
					layout.Par(); layout.Toggle(ref widthScale, rect: layout.Inset(20)); layout.Label("Width Scale", rect: layout.Inset(100));
					layout.Par(); layout.Toggle(ref heightScale, rect: layout.Inset(20)); layout.Label("Height Scale", rect: layout.Inset(100));
					layout.fieldSize = 0.37f;
					layout.Field(ref color, "Color");
					layout.Field(ref bendFactor, "Bend Factor");
				}
			}

			//public void OnAdd(int n) { }
			//public void OnRemove(int n) { input.Link(null, null); }
			//public void OnSwitch(int o, int n) { }
		}

		//layers
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

		public static void Process(MapMagic.CoordRect rect, Chunk.Results results, GeneratorsAsset gens, Chunk.Size terrainSize, Func<float,bool> stop = null)
		{
			if (stop!=null && stop(0)) return;

			Noise noise = new Noise(12345, permutationCount:128); //to pick objects based on biome

			List<TreeInstance> instancesList = new List<TreeInstance>();
			List<TreePrototype> prototypesList = new List<TreePrototype>();

			//find all of the biome masks - they will be used to determine object probability
			List<TupleSet<MadMapsTreeOutput,Matrix>> allGensMasks = new List<TupleSet<MadMapsTreeOutput, Matrix>>();
			foreach (MadMapsTreeOutput gen in gens.GeneratorsOfType<MadMapsTreeOutput>(onlyEnabled: true, checkBiomes: true))
			{
				Matrix biomeMask = null;
				if (gen.biome != null)
				{
					object biomeMaskObj = gen.biome.mask.GetObject(results);
					if (biomeMaskObj == null) continue; //adding nothing if biome has no mask
					biomeMask = (Matrix)biomeMaskObj;
					if (biomeMask == null) continue;
					if (biomeMask.IsEmpty()) continue; //optimizing empty biomes
				}

				allGensMasks.Add( new TupleSet<MadMapsTreeOutput, Matrix>(gen,biomeMask) );
			}
			int allGensMasksCount = allGensMasks.Count;

			//biome rect to find array pos faster
			MapMagic.CoordRect biomeRect = new MapMagic.CoordRect();
			for (int g=0; g<allGensMasksCount; g++)
				if (allGensMasks[g].item2 != null) { biomeRect = allGensMasks[g].item2.rect; break; }

			//prepare biome mask values stack to re-use it to find per-coord biome
			float[] biomeVals = new float[allGensMasksCount]; //+1 for not using any object at all

			//iterating all gens
			for (int g=0; g<allGensMasksCount; g++)
			{
				MadMapsTreeOutput gen = allGensMasks[g].item1;

				//iterating in layers
				for (int b = 0; b < gen.baseLayers.Length; b++)
				{
					if (stop!=null && stop(0)) return; //checking stop before reading output
					Layer layer = gen.baseLayers[b];
//					if (layer.prefab == null) continue;

					//loading objects from input
					SpatialHash hash = (SpatialHash)gen.baseLayers[b].input.GetObject(results);
					if (hash == null) continue;

					//adding prototype
//					if (layer.prefab == null) continue;
					TreePrototype prototype = new TreePrototype() { prefab = layer.prefab, bendFactor = layer.bendFactor };
					prototypesList.Add(prototype);
					int prototypeNum = prototypesList.Count - 1;

					//filling instances (no need to check/add key in multidict)
					foreach (SpatialObject obj in hash.AllObjs())
					{
						//blend biomes - calling continue if improper biome
						if (biomeBlendType == BiomeBlendType.Sharp)
						{
							float biomeVal = 1;
							if (allGensMasks[g].item2 != null) biomeVal = allGensMasks[g].item2[obj.pos];
							if (biomeVal < 0.5f) continue;
						}
						else if (biomeBlendType == BiomeBlendType.AdditiveRandom)
						{
							float biomeVal = 1;
							if (allGensMasks[g].item2 != null) biomeVal = allGensMasks[g].item2[obj.pos];

							float rnd = noise.Random((int)obj.pos.x, (int)obj.pos.y);

							if (biomeVal > 0.5f) rnd = 1-rnd;
							
							if (biomeVal < rnd) continue;
						}
						else if (biomeBlendType == BiomeBlendType.NormalizedRandom)
						{
							//filling biome masks values
							int pos = biomeRect.GetPos(obj.pos);

							for (int i=0; i<allGensMasksCount; i++)
							{
								if (allGensMasks[i].item2 != null) biomeVals[i] = allGensMasks[i].item2.array[pos];
								else biomeVals[i] = 1;
							}

							//calculate normalized sum
							float sum = 0;
							for (int i=0; i<biomeVals.Length; i++) sum += biomeVals[i];
							if (sum > 1) //note that if sum is <1 usedBiomeNum can exceed total number of biomes - it means that none object is used here
								for (int i=0; i<biomeVals.Length; i++) biomeVals[i] = biomeVals[i] / sum;
						
							//finding used biome num
							float rnd = noise.Random((int)obj.pos.x, (int)obj.pos.y);
							int usedBiomeNum = biomeVals.Length; //none biome by default
							sum = 0;
							for (int i=0; i<biomeVals.Length; i++)
							{
								sum += biomeVals[i];
								if (sum > rnd) { usedBiomeNum=i; break; }
							}

							//disable object using biome mask
							if (usedBiomeNum != g) continue;
						}
						//scale mode is applied a bit later

						//flooring
						float terrainHeight = 0;
						if (layer.relativeHeight && results.heights != null) //if checbox enabled and heights exist (at least one height generator is in the graph)
							terrainHeight = results.heights.GetInterpolated(obj.pos.x, obj.pos.y);
						if (terrainHeight > 1) terrainHeight = 1;

						TreeInstance tree = new TreeInstance();
						tree.position = new Vector3(
							(obj.pos.x - hash.offset.x) / hash.size,
							obj.height + terrainHeight,
							(obj.pos.y - hash.offset.y) / hash.size);
						tree.rotation = layer.rotate ? obj.rotation % 360 : 0;
						tree.widthScale = layer.widthScale ? obj.size : 1;
						tree.heightScale = layer.heightScale ? obj.size : 1;
						tree.prototypeIndex = prototypeNum;
						tree.color = layer.color;
						tree.lightmapColor = layer.color;

						if (biomeBlendType == BiomeBlendType.Scale)
						{
							float biomeVal = 1;
							if (allGensMasks[g].item2 != null) biomeVal = allGensMasks[g].item2[obj.pos];
							if (biomeVal < 0.001f) continue;
							tree.widthScale *= biomeVal;
							tree.heightScale *= biomeVal;
						}

						instancesList.Add(tree);
					}
				}
			}

			//setting output
			if (stop!=null && stop(0)) return;
			if (instancesList.Count == 0 && prototypesList.Count == 0) return; //empty, process is caused by height change
			TupleSet<TreeInstance[], TreePrototype[]> treesTuple = new TupleSet<TreeInstance[], TreePrototype[]>(instancesList.ToArray(), prototypesList.ToArray());
			results.apply.CheckAdd(typeof(MadMapsTreeOutput), treesTuple, replace: true);
		}

		public IEnumerator Apply(MapMagic.CoordRect rect, Terrain terrain, object dataBox, Func<float,bool> stop= null)
		{
			var wrapper = terrain.gameObject.GetOrAddComponent<TerrainWrapper>();
            var MMTerrainLayer = wrapper.GetLayer<MMTerrainLayer>(LayerName, false, true);
            MMTerrainLayer.Trees.Clear();
            
            TupleSet<TreeInstance[], TreePrototype[]> treesTuple = (TupleSet<TreeInstance[], TreePrototype[]>)dataBox;
            var prototypeList = treesTuple.item2.ToList();
            for (int i = 0; i < treesTuple.item1.Length; i++)
            {
                var treeInstance = treesTuple.item1[i];
                var newTree = new MadMapsTreeInstance(treeInstance, prototypeList);
                MMTerrainLayer.Trees.Add(newTree);
            }
            global::MapMagic.MapMagic.OnApplyCompleted -= MapMagicIntegrationUtilities.MapMagicOnOnApplyCompleted;
            global::MapMagic.MapMagic.OnApplyCompleted += MapMagicIntegrationUtilities.MapMagicOnOnApplyCompleted;
			wrapper.SetDirtyAbove(MMTerrainLayer);
			yield return null;
		}

		public void Purge(MapMagic.CoordRect rect, Terrain terrain)
		{
			//skipping if already purged
			if (terrain.terrainData.treeInstances.Length==0) return;
			
			var wrapper = terrain.GetComponent<TerrainWrapper>();
            if (wrapper == null)
            {
                return;
            }
            var MMTerrainLayer = wrapper.GetLayer<MMTerrainLayer>(LayerName);
            if (MMTerrainLayer == null || MMTerrainLayer.Trees == null)
            {
                return;
            }
            MMTerrainLayer.Trees.Clear();
            wrapper.Dirty = true;

		}


		public override void OnGUI(GeneratorsAsset gens)
		{
            layout.fieldSize = .6f;
            layout.Field(ref LayerName, "Layer");
            layout.fieldSize = .5f;
			layout.Field(ref biomeBlendType, "Biome Blend", fieldSize:0.47f); 
			
			//layer buttons
			layout.Par(5);
			layout.Par();
			layout.Label("Layers:", layout.Inset(0.4f));

			layout.DrawArrayAdd(ref baseLayers, ref selected, rect:layout.Inset(0.15f), createElement:() => new Layer() );
			layout.DrawArrayRemove(ref baseLayers, ref selected, rect:layout.Inset(0.15f), onBeforeRemove:UnlinkLayer);
			layout.DrawArrayUp(ref baseLayers, ref selected, rect:layout.Inset(0.15f));
			layout.DrawArrayDown(ref baseLayers, ref selected, rect:layout.Inset(0.15f));

			//layers
			layout.Par(3);
			for (int num=0; num<baseLayers.Length; num++)
				layout.DrawLayer(baseLayers[num].OnGUI, ref selected, num);
		}

	}
}
#endif