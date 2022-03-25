// Taken with some small modifications from Map Magic (https://assetstore.unity.com/packages/tools/terrain/mapmagic-world-generator-56762)
// All rights reserved by the original creator.

#if MAPMAGIC
using System;
using System.Collections;
using System.Collections.Generic;
using MadMaps.Common;
using MadMaps.Common.Collections;
using MapMagic;
using UnityEngine;
using System.Linq;
using MadMaps.WorldStamps;
using MadMaps.Terrains;

namespace MadMaps.Terrains.MapMagicIntegration
{
    [System.Serializable]
	[GeneratorMenu(menu = "Mad Maps", name = "Mad Maps Objects", disengageable = true, helpLink = "https://gitlab.com/denispahunov/mapmagic/wikis/output_generators/Objects")]
	public class MadMapsObjectOutput : OutputGenerator
	{
		public string LayerName = "MapMagic";
		public bool ApplyStamps = true;

		//layer
		public class Layer
		{
			public Input input = new Input(InoutType.Objects);

			public Transform prefab;

			//public bool relativeHeight = true;
			
			//public bool regardRotation = false;
			public bool rotate = true;
			public bool takeTerrainNormal = false;

			//public bool regardScale = false;
			public bool scale = true;
			public bool scaleY;

			//public bool usePool = true;
			public bool parentToRoot = false;

			//public bool processChildren = false;
			//public bool floorChildren;
			//public Vector2 rotateChildren;
			//public Vector2 scaleChildren;
			//public float removeChildren = 0;

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
					//layout.Toggle(ref relativeHeight, "Relative Height");
					layout.Toggle(ref rotate, "Rotate");
					layout.Toggle(ref takeTerrainNormal, "Incline by Terrain");
					layout.Par(); layout.Toggle(ref scale, "Scale", rect: layout.Inset(60));
					layout.disabled = !scale;
					layout.Toggle(ref scaleY, rect: layout.Inset(18)); layout.Label("Y only", rect: layout.Inset(45)); //if (layout.lastChange) scaleU = false;
					layout.disabled = false;
				}
			}

			//public void OnAdd(int n) { }
			//public void OnRemove(int n) { input.Link(null, null); }
			//public void OnSwitch(int o, int n) { }
		}

		//layer
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
			//baseLayers[num].output.UnlinkInActiveGens(); //try to unlink output
		}


		public enum BiomeBlendType { Sharp, AdditiveRandom, NormalizedRandom, Scale }
		public static BiomeBlendType biomeBlendType = BiomeBlendType.AdditiveRandom;

		//public class ObjectsTuple { public List<TransformPool.InstanceDraft[]> instances; public List<Layer> layers; }

		//generator
		public override IEnumerable<Input> Inputs()
		{
			if (baseLayers == null) baseLayers = new Layer[0];
			for (int i = 0; i < baseLayers.Length; i++)
				if (baseLayers[i].input != null)
					yield return baseLayers[i].input;
		}

		//get static actions using instance
		public override MapMagic.Action<MapMagic.CoordRect, Chunk.Results, GeneratorsAsset, Chunk.Size, Func<float,bool>> GetProces () { return Process; }
		public override Func<MapMagic.CoordRect, Terrain, object, Func<float,bool>, IEnumerator> GetApply () { return Apply; }
		public override Action<MapMagic.CoordRect, Terrain> GetPurge () { return Purge; }



		public static void Process(MapMagic.CoordRect rect, Chunk.Results results, GeneratorsAsset gens, Chunk.Size terrainSize, Func<float,bool> stop = null)
		{
			if (stop!=null && stop(0)) return;

			Noise noise = new Noise(12345, permutationCount:128); //to pick objects based on biome

			//preparing output
			Dictionary<Transform, List<ObjectPool.Transition>> transitions = new Dictionary<Transform, List<ObjectPool.Transition>>();

			//find all of the biome masks - they will be used to determine object probability
			List<TupleSet<MadMapsObjectOutput,Matrix>> allGensMasks = new List<TupleSet<MadMapsObjectOutput, Matrix>>();
			foreach (MadMapsObjectOutput gen in gens.GeneratorsOfType<MadMapsObjectOutput>(onlyEnabled: true, checkBiomes: true))
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

				allGensMasks.Add( new TupleSet<MadMapsObjectOutput, Matrix>(gen,biomeMask) );
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
				MadMapsObjectOutput gen = allGensMasks[g].item1;

				//iterating in layers
				for (int b = 0; b < gen.baseLayers.Length; b++)
				{
					if (stop!=null && stop(0)) return; //checking stop before reading output
					Layer layer = gen.baseLayers[b];
					if (!layer.prefab) continue;

					//loading objects from input
					SpatialHash hash = (SpatialHash)gen.baseLayers[b].input.GetObject(results);
					if (hash == null) continue;

					//finding/creating proper transitions list
					List<ObjectPool.Transition> transitionsList;
					if (!transitions.ContainsKey(layer.prefab)) { transitionsList = new List<ObjectPool.Transition>(); transitions.Add(layer.prefab, transitionsList); }
					else transitionsList = transitions[layer.prefab];

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
						//if (layer.relativeHeight && results.heights != null) //if checbox enabled and heights exist (at least one height generator is in the graph)
						//	terrainHeight = results.heights.GetInterpolated(obj.pos.x, obj.pos.y);
						//if (terrainHeight > 1) terrainHeight = 1;


						//world-space object position
						Vector3 position = new Vector3(
							(obj.pos.x) / hash.size * terrainSize.dimensions,  // relative (0-1) position * terrain dimension
							(obj.height + terrainHeight) * terrainSize.height, 
							(obj.pos.y) / hash.size * terrainSize.dimensions);

						//rotation + taking terrain normal
						Quaternion rotation;
						float objRotation = layer.rotate ? obj.rotation % 360 : 0;
						if (layer.takeTerrainNormal)
						{
							Vector3 terrainNormal = GetTerrainNormal(obj.pos.x, obj.pos.y, results.heights, terrainSize.height, terrainSize.pixelSize);
							Vector3 sideVector = new Vector3( Mathf.Sin((obj.rotation+90)*Mathf.Deg2Rad), 0, Mathf.Cos((obj.rotation+90)*Mathf.Deg2Rad) );
							Vector3 frontVector = Vector3.Cross(sideVector, terrainNormal);
							rotation = Quaternion.LookRotation(frontVector, terrainNormal);
						}
						else rotation = objRotation.EulerToQuat();

						//scale + biome scale mode
						Vector3 scale = layer.scale ? new Vector3(layer.scaleY ? 1 : obj.size, obj.size, layer.scaleY ? 1 : obj.size) : Vector3.one;

						if (biomeBlendType == BiomeBlendType.Scale)
						{
							float biomeVal = 1;
							if (allGensMasks[g].item2 != null) biomeVal = allGensMasks[g].item2[obj.pos];
							if (biomeVal < 0.001f) continue;
							scale *= biomeVal;
						}
						
						transitionsList.Add(new ObjectPool.Transition() {pos=position, rotation=rotation, scale=scale });
					}
				}
			}

			//queue apply
			if (stop!=null && stop(0)) return;
			results.apply.CheckAdd(typeof(MadMapsObjectOutput), transitions, replace: true);
		}

		public static Vector3 GetTerrainNormal (float fx, float fz, Matrix heightmap, float heightFactor, float pixelSize)
		{
			//copy of rect's GetPos to process negative terrains properly
			int x = (int)(fx + 0.5f); 
			if (fx < 0) x--;
			if (x >= heightmap.rect.offset.x+heightmap.rect.size.x) x--;

			int z = (int)(fz + 0.5f); 
			if (fz < 0) z--;
			if (z >= heightmap.rect.offset.z+heightmap.rect.size.z) z--;
			
			int pos = (z-heightmap.rect.offset.z)*heightmap.rect.size.x + x - heightmap.rect.offset.x; 

			float curHeight = heightmap.array[pos];
						
			float prevXHeight = curHeight;
			if (x>=heightmap.rect.offset.x+1) prevXHeight = heightmap.array[pos-1];

			float nextXHeight = curHeight;
			if (x<heightmap.rect.offset.x+heightmap.rect.size.x-1) nextXHeight = heightmap.array[pos+1];
									
			float prevZHeight = curHeight;
			if (z>=heightmap.rect.offset.z+1) prevZHeight = heightmap.array[pos-heightmap.rect.size.x];

			float nextZHeight = curHeight;
			if (z<heightmap.rect.offset.z+heightmap.rect.size.z-1) nextZHeight = heightmap.array[pos+heightmap.rect.size.z];

			return new Vector3((prevXHeight-nextXHeight)*heightFactor, pixelSize*2, (prevZHeight-nextZHeight)*heightFactor).normalized;
		}

		public IEnumerator Apply(MapMagic.CoordRect rect, Terrain terrain, object dataBox, Func<float,bool> stop= null)
		{
			Dictionary<Transform, List<ObjectPool.Transition>> transitions = (Dictionary<Transform, List<ObjectPool.Transition>>)dataBox;
var wrapper = terrain.gameObject.GetOrAddComponent<TerrainWrapper>();
            var MMTerrainLayer = wrapper.GetLayer<MMTerrainLayer>(LayerName, false, true);
            MMTerrainLayer.Objects.Clear();
            
            //float pixelSize = 1f * global::MapMagic.MapMagic.instance.terrainSize / global::MapMagic.MapMagic.instance.resolution;
            //Rect terrainRect = new Rect(rect.offset.x * pixelSize, rect.offset.z * pixelSize, rect.size.x * pixelSize, rect.size.z * pixelSize);
            var terrainSize = terrain.terrainData.size;

            //adding
            foreach (KeyValuePair<Transform, List<ObjectPool.Transition>> kvp in transitions)
            {
                Transform prefab = kvp.Key;
                List<ObjectPool.Transition> transitionsList = kvp.Value;
                foreach (var transition in transitionsList)
                {
                    var terrainSpacePos = transition.pos - terrain.transform.localPosition /*- terrainSize/2*/;
                    var normalisedPos = new Vector3(terrainSpacePos.x / terrainSize.x, transition.pos.y,
                        terrainSpacePos.z / terrainSize.z);

                    var prefabObj = new PrefabObjectData()
                    {
                        AbsoluteHeight = false,
                        Guid = System.Guid.NewGuid().ToString(),
                        Prefab = prefab.gameObject,
                        Rotation = transition.rotation.eulerAngles,
                        Position = normalisedPos,
                        Scale = transition.scale, 
						ContainerMetadata = LayerName,
                    };
                    MMTerrainLayer.Objects.Add(prefabObj);
                }
            }
            global::MapMagic.MapMagic.OnApplyCompleted -= MapMagicIntegrationUtilities.MapMagicOnOnApplyCompleted;
            global::MapMagic.MapMagic.OnApplyCompleted += MapMagicIntegrationUtilities.MapMagicOnOnApplyCompleted;

			wrapper.OnPostFinalise -= ApplyStampsToTerrain;
			if(ApplyStamps)
			{				
            	wrapper.OnPostFinalise += ApplyStampsToTerrain;
			}

			wrapper.SetDirtyAbove(MMTerrainLayer);
            yield break;
        }

		public void ApplyStampsToTerrain(TerrainWrapper wrapper)
		{
			wrapper.OnPostFinalise -= ApplyStampsToTerrain;
			if(!ApplyStamps)
			{
				return;
			}
			//Debug.Log("ApplyStampsToTerrain");
			if(!wrapper.ObjectContainer)
			{
				return;
			}
			var ws = wrapper.ObjectContainer.transform.FindTransformInChildren(LayerName);
			if(!ws)
			{
				return;
			}
			var stamps = ws.GetComponentsInChildren<WorldStamp>();
			List<string> layers = new List<string>();
			foreach(var s in stamps)
			{
				if(layers.Contains(s.LayerName))
				{
					continue;
				}
				layers.Add(s.LayerName);
			}
			layers = layers.OrderBy((x) => wrapper.Layers.FindIndex(0, (y) => x == y.name)).ToList();
			for(var i = 0; i < layers.Count; ++i)
			{
				LayerComponentApplyManager.ApplyAllLayerComponents(wrapper, layers[i]);
			}
		}

		public void Purge(global::MapMagic.CoordRect rect, Terrain terrain)
        {
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
            MMTerrainLayer.Objects.Clear();
            wrapper.Dirty = true;
        }

		public override void OnGUI(GeneratorsAsset gens)
		{
			layout.fieldSize = .6f;
            layout.Field(ref LayerName, "Layer");
            layout.fieldSize = .5f;

			layout.Field(ref ApplyStamps, "Apply Stamps");

			//layer buttons
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