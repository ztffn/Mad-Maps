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
	public static class MapMagicIntegrationUtilities
	{
		public static void MapMagicOnOnApplyCompleted(Terrain terrain)
        {
            global::MapMagic.MapMagic.OnApplyCompleted -= MapMagicOnOnApplyCompleted;
            var wrapper = terrain.gameObject.GetOrAddComponent<TerrainWrapper>();
			#if UNITY_EDITOR
			UnityEditor.EditorApplication.update -= wrapper.Update;
			UnityEditor.EditorApplication.update += wrapper.Update;
			#endif
            wrapper.Dirty = true;
        }
	}

	[System.Serializable]
	[GeneratorMenu(menu = "Mad Maps", name = "Mad Maps Height", disengageable = true, priority = -2, helpLink = "https://gitlab.com/denispahunov/mapmagic/wikis/output_generators/Height")]
	public class MadMapsHeightOutput : OutputGenerator
	{
		public string LayerName = "MapMagic";
		public Input input = new Input(InoutType.Map);
		public Output output = new Output(InoutType.Map);
		public override IEnumerable<Input> Inputs() { yield return input; }
		public override IEnumerable<Output> Outputs() { if (output == null) output = new Output(InoutType.Map); yield return output; }

		public float layer { get; set; }

		//get static actions using instance
		public override MapMagic.Action<MapMagic.CoordRect, Chunk.Results, GeneratorsAsset, Chunk.Size, Func<float,bool>> GetProces () { return Process; }
		public override Func<MapMagic.CoordRect, Terrain, object, Func<float,bool>, IEnumerator> GetApply () { return Apply; }
		public override Action<MapMagic.CoordRect, Terrain> GetPurge () { return Purge; }

		static HashSet<MapMagic.CoordRect> _pendingWrappers = new HashSet<MapMagic.CoordRect>();
		
		public static int scale = 1;

		public static void Process(MapMagic.CoordRect rect, Chunk.Results results, GeneratorsAsset gens, Chunk.Size terrainSize, Func<float,bool> stop = null)
		{
			if (stop!=null && stop(0)) return;

			//reading height outputs
			if (results.heights == null || results.heights.rect.size.x != rect.size.x) results.heights = new Matrix(rect);
			results.heights.rect.offset = rect.offset;
			results.heights.Clear();

			_pendingWrappers.Add(rect);

			//processing main height
			foreach (MadMapsHeightOutput gen in gens.GeneratorsOfType<MadMapsHeightOutput>(onlyEnabled: true, checkBiomes: true))
			{
				//if (stop!=null && stop(0)) return; //do not break while results.heights is empty!

				//loading inputs
				Matrix heights = (Matrix)gen.input.GetObject(results);
				if (heights == null) continue;

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
				if (gen.biome == null) results.heights.Add(heights);
				else if (biomeMask != null) results.heights.Add(heights, biomeMask);
			}

			//creating 2d array
			if (stop!=null && stop(0)) return;
			int heightSize = terrainSize.resolution * scale + 1;
			float[,] heights2D = new float[heightSize, heightSize];
			for (int x = 0; x < heightSize - 1; x++)
				for (int z = 0; z < heightSize - 1; z++)
				{
					if (scale == 1) heights2D[z, x] += results.heights[x + results.heights.rect.offset.x, z + results.heights.rect.offset.z];
					else
					{
						float fx = 1f * x / scale; float fz = 1f * z / scale;
						heights2D[z, x] = results.heights.GetInterpolated(fx + results.heights.rect.offset.x, fz + results.heights.rect.offset.z);
					}
				}

			//blur only original base verts
			if (scale == 2)
			{
				float blurVal = 0.2f;
				
				for (int z=0; z<heightSize-1; z+=2)
					for (int x=2; x<heightSize-1; x+=2)
						heights2D[x,z] = (heights2D[x-1,z] + heights2D[x+1,z])/2 * blurVal  +  heights2D[x,z] * (1-blurVal);

				for (int x=0; x<heightSize-1; x+=2)
					for (int z=2; z<heightSize-1; z+=2)
						heights2D[x,z] = (heights2D[x,z-1] + heights2D[x,z+1])/2 * blurVal  +  heights2D[x,z] * (1-blurVal);
			}

			//blur high scale values
			if (scale == 4)
			{
				int blurIterations = 2;

				for (int i=0; i<blurIterations; i++)
				{
					float prev = 0;
					float curr = 0;
					float next = 0;

					for (int x=0; x<heightSize; x++)
					{
						prev = heights2D[x,0]; curr = prev;
						for (int z=1; z<heightSize-2; z++)
						{
							next = heights2D[x,z+1];
							curr = (next+prev)/2;// * blurVal + curr*(1-blurVal);

							heights2D[x,z] = curr;
							prev = curr;
							curr = next;
						}

					}

					for (int z=0; z<heightSize; z++)
					{
						prev = heights2D[0,z]; curr = prev;
						for (int x=1; x<heightSize-2; x++)
						{
							next = heights2D[x+1,z];
							curr = (next+prev)/2;// * blurVal + curr*(1-blurVal);

							heights2D[x,z] = curr;
							prev = curr;
							curr = next;
						}
					}
				}
			}

			//processing sides
			for (int x = 0; x < heightSize; x++)
			{
				float prevVal = heights2D[heightSize - 3, x]; //size-2
				float currVal = heights2D[heightSize - 2, x]; //size-1, point on border
				float nextVal = currVal - (prevVal - currVal);
				heights2D[heightSize - 1, x] = nextVal;
			}
			for (int z = 0; z < heightSize; z++)
			{
				float prevVal = heights2D[z, heightSize - 3]; //size-2
				float currVal = heights2D[z, heightSize - 2]; //size-1, point on border
				float nextVal = currVal - (prevVal - currVal);
				heights2D[z, heightSize - 1] = nextVal;
			}
			heights2D[heightSize - 1, heightSize - 1] = heights2D[heightSize - 1, heightSize - 2];

			for (int x = 0; x < heightSize - 1; x++)
			{
				for (int z = 0; z < heightSize - 1; z++)
				{
					heights2D[z, x] = Mathf.Clamp01(heights2D[z, x]);
				}
			}

			//pushing to apply
			if (stop!=null && stop(0)) return;

            #if UN_MapMagic
            if (FoliageCore_MainManager.instance != null)
            {
                float resolutionDifferences = (float)MapMagic.instance.terrainSize / terrainSize.resolution;

                uNatureHeightTuple heightTuple = new uNatureHeightTuple(heights2D, new Vector3(rect.Min.x * resolutionDifferences, 0, rect.Min.z * resolutionDifferences)); // transform coords
                results.apply.CheckAdd(typeof(MadMapsHeightOutput), heightTuple, replace: true);
            }
            else
            {
                //Debug.LogError("uNature_MapMagic extension is enabled but no foliage manager exists on the scene.");
                //return;
				results.apply.CheckAdd(typeof(MadMapsHeightOutput), heights2D, replace: true);
            }

            #else
			results.apply.CheckAdd(typeof(MadMapsHeightOutput), heights2D, replace: true);
            #endif
        }

		public void Purge(MapMagic.CoordRect rect, Terrain terrain)
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
            MMTerrainLayer.Heights.Clear();
            wrapper.Dirty = true;

			Debug.Log("Heights Purged");

		}

		public IEnumerator Apply(MapMagic.CoordRect rect, Terrain terrain, object dataBox, Func<float,bool> stop= null)
		{


            //init heights
            #if UN_MapMagic

			#if WDEBUG
			Profiler.BeginSample("UNature");
			#endif

            uNatureHeightTuple heightTuple;
            float[,] heights2D;

            if (FoliageCore_MainManager.instance != null)
            {
                heightTuple = (uNatureHeightTuple)dataBox; // get data
                heights2D = heightTuple.normalizedHeights;
                UNMapMagic_Manager.ApplyHeightOutput(heightTuple, terrain);
            }
            else
            {
                //Debug.LogError("uNature_MapMagic extension is enabled but no foliage manager exists on the scene.");
                //yield break;
				heights2D = (float[,])dataBox;
            }         

			#if WDEBUG
			Profiler.EndSample();
			#endif

			#else
			float[,] heights2D = (float[,])dataBox;
			#endif

			heights2D = heights2D.Flip();

			//quick lod apply
			/*if (chunk.lod)
			{
				//if (chunk.lodTerrain == null) { chunk.lodTerrain = (MapMagic.instance.transform.AddChild("Terrain " + chunk.coord.x + "," + chunk.coord.z + " LOD")).gameObject.AddComponent<Terrain>(); chunk.lodTerrain.terrainData = new TerrainData(); }
				if (chunk.lodTerrain.terrainData==null) chunk.lodTerrain.terrainData = new TerrainData();

				chunk.lodTerrain.Resize(heights2D.GetLength(0), new Vector3(MapMagic.instance.terrainSize, MapMagic.instance.terrainHeight, MapMagic.instance.terrainSize));
				chunk.lodTerrain.terrainData.SetHeightsDelayLOD(0,0,heights2D);
				
				yield break;
			}*/

			//determining data
			if (terrain==null || terrain.terrainData==null) yield break; //chunk removed during apply
			TerrainData data = terrain.terrainData;

			//resizing terrain (standard terrain resize is extremely slow. Even when creating a new terrain)
			Vector3 terrainSize = terrain.terrainData.size; //new Vector3(MapMagic.instance.terrainSize, MapMagic.instance.terrainHeight, MapMagic.instance.terrainSize);
			int terrainResolution = heights2D.GetLength(0); //heights2D[0].GetLength(0);
			if ((data.size - terrainSize).sqrMagnitude > 0.01f || data.heightmapResolution != terrainResolution)
			{
				if (terrainResolution <= 64) //brute force
				{
					data.heightmapResolution = terrainResolution;
					data.size = new Vector3(terrainSize.x, terrainSize.y, terrainSize.z);
				}

				else //setting res 64, re-scaling to 1/64, and then changing res
				{
					data.heightmapResolution = 65;
					terrain.Flush(); //otherwise unity crushes without an error
					int resFactor = (terrainResolution - 1) / 64;
					data.size = new Vector3(terrainSize.x / resFactor, terrainSize.y, terrainSize.z / resFactor);
					data.heightmapResolution = terrainResolution;
				}
			}
			yield return null;

			var heightSize = heights2D.GetLength(0);
			for (int x = 0; x < heightSize - 1; x++)
			{
				for (int z = 0; z < heightSize - 1; z++)
				{
					heights2D[z, x] = Mathf.Clamp01(heights2D[z, x]);
				}
			}
			
			var wrapper = terrain.gameObject.GetOrAddComponent<TerrainWrapper>();
            var MMTerrainLayer = wrapper.GetLayer<MMTerrainLayer>(LayerName, false, true);            
            MMTerrainLayer.SetHeights(0, 0, heights2D, MapMagic.MapMagic.instance.resolution+1);
			_pendingWrappers.Remove(rect);

			//welding
			if (MapMagic.MapMagic.instance != null && MapMagic.MapMagic.instance.heightWeldMargins!=0)
			{
				MapMagic.Coord coord = MapMagic.Coord.PickCell(rect.offset, MapMagic.MapMagic.instance.resolution);
				Chunk chunk = MapMagic.MapMagic.instance.chunks[coord.x, coord.z];
				
				Chunk neigPrevX = MapMagic.MapMagic.instance.chunks[coord.x-1, coord.z];
				if (neigPrevX!=null && !_pendingWrappers.Contains(neigPrevX.rect) && neigPrevX.terrain && neigPrevX.terrain.terrainData.heightmapResolution==terrainResolution)
				{
					var neighbourWrapper = neigPrevX.terrain.GetComponent<TerrainWrapper>();
					if (neigPrevX.worker.ready && neighbourWrapper) 
						WeldTerrains.WeldToPrevZ(ref heights2D, neighbourWrapper, MapMagic.MapMagic.instance.heightWeldMargins);
						//WeldTerrains.WeldToPrevX(ref heights2D, neighbourWrapper, MapMagic.MapMagic.instance.heightWeldMargins);
					Chunk.SetNeigsX(neigPrevX, chunk);
				}

				Chunk neigNextX = MapMagic.MapMagic.instance.chunks[coord.x+1, coord.z];
				if (neigNextX!=null && !_pendingWrappers.Contains(neigNextX.rect) && neigNextX.terrain.terrainData.heightmapResolution==terrainResolution)
				{
					var neighbourWrapper = neigNextX.terrain.GetComponent<TerrainWrapper>();
					if (neigNextX.worker.ready && neighbourWrapper) 
						WeldTerrains.WeldToNextZ(ref heights2D, neighbourWrapper, MapMagic.MapMagic.instance.heightWeldMargins);
						//WeldTerrains.WeldToNextX(ref heights2D, neighbourWrapper, MapMagic.MapMagic.instance.heightWeldMargins);
					Chunk.SetNeigsX(chunk, neigNextX);
				}

				Chunk neigPrevZ = MapMagic.MapMagic.instance.chunks[coord.x, coord.z-1];
				if (neigPrevZ!=null  &&!_pendingWrappers.Contains(neigPrevZ.rect) && neigPrevZ.terrain.terrainData.heightmapResolution==terrainResolution)
				{
					var neighbourWrapper = neigPrevZ.terrain.GetComponent<TerrainWrapper>();
					if (neigPrevZ.worker.ready && neighbourWrapper) 
						//WeldTerrains.WeldToNextX(ref heights2D, neighbourWrapper, MapMagic.MapMagic.instance.heightWeldMargins);						
						WeldTerrains.WeldToPrevX(ref heights2D, neighbourWrapper, MapMagic.MapMagic.instance.heightWeldMargins);
					Chunk.SetNeigsZ(neigPrevZ, chunk);
				}

				Chunk neigNextZ = MapMagic.MapMagic.instance.chunks[coord.x, coord.z+1];
				if (neigNextZ!=null  && !_pendingWrappers.Contains(neigNextZ.rect) && neigNextZ.terrain.terrainData.heightmapResolution==terrainResolution)
				{
					var neighbourWrapper = neigNextZ.terrain.GetComponent<TerrainWrapper>();
					if (neigNextZ.worker.ready && neighbourWrapper) 
						//WeldTerrains.WeldToPrevX(ref heights2D, neighbourWrapper, MapMagic.MapMagic.instance.heightWeldMargins);
						WeldTerrains.WeldToNextX(ref heights2D, neighbourWrapper, MapMagic.MapMagic.instance.heightWeldMargins);
					Chunk.SetNeigsZ(chunk, neigNextZ);
				}
			}
			yield return null;

			MMTerrainLayer.SetHeights(0, 0, heights2D, MapMagic.MapMagic.instance.resolution+1);
			global::MapMagic.MapMagic.OnApplyCompleted -= MapMagicIntegrationUtilities.MapMagicOnOnApplyCompleted;
            global::MapMagic.MapMagic.OnApplyCompleted += MapMagicIntegrationUtilities.MapMagicOnOnApplyCompleted;

			wrapper.SetDirtyAbove(MMTerrainLayer);

			yield return null;
		}

		

		public override void OnGUI(GeneratorsAsset gens)
		{
			layout.fieldSize = .6f;
            layout.Field(ref LayerName, "Layer");
            layout.fieldSize = .5f;

			layout.Par(20); input.DrawIcon(layout, "Height");
			layout.Par(5);

			if (output == null) output = new Output(InoutType.Map);

			layout.Field(ref scale, "Scale", min:1, max:4f);
			scale = Mathf.NextPowerOfTwo(scale);
		}
	}
}
#endif