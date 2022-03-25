using System;
using UnityEngine;
using System.Reflection;

namespace MadMaps.Common
{
    public static class TerrainX
    {
        public static bool ContainsPointXZ(this Terrain terrain, Vector3 worldPos)
        {
            var tBounds = terrain.GetComponent<TerrainCollider>().bounds;
            tBounds.Expand(Vector3.up * 10000);
            return tBounds.Contains(worldPos);
        }

        public enum RoundType
        {
            Round,
            Ceil,
            Floor,
        }

        public static int GetDetailResolutionPerPatch(this TerrainData data)
        {
            // Fuck you Unity
            var prop = typeof(TerrainData).GetProperty("detailResolutionPerPatch", BindingFlags.Instance|BindingFlags.NonPublic);
            var val = prop.GetValue(data, null);
            return (int)val;
        }

        public static Bounds GetBounds(this Terrain terrain)
        {
            var tSize = terrain.terrainData.size;
            return new Bounds(terrain.transform.position + tSize/2, tSize);
        }

        public static Coord WorldToHeightmapCoord(this Terrain terrain, Vector3 worldPos, RoundType rounding)
        {
            var terrainSize = terrain.terrainData.size;
            var resolution = terrain.terrainData.heightmapResolution - 1;

            worldPos -= terrain.GetPosition();
            //worldPos -= new Vector3(0, 0, terrainSize.z / resolution) / 2;
            worldPos = new Vector3(worldPos.x / terrainSize.x, worldPos.y / terrainSize.y, worldPos.z / terrainSize.z);
            worldPos = new Vector3(worldPos.x * resolution, 0, worldPos.z * resolution);
            
            int x = 0;
            int z = 0;
            switch (rounding)
            {
                case RoundType.Round:    
                    x = Mathf.RoundToInt(worldPos.x);
                    z = Mathf.RoundToInt(worldPos.z);
                    break;
                case RoundType.Floor:
                    x = Mathf.FloorToInt(worldPos.x);
                    z = Mathf.FloorToInt(worldPos.z);
                    break;
                case RoundType.Ceil:
                    x = Mathf.CeilToInt(worldPos.x);
                    z = Mathf.CeilToInt(worldPos.z);
                    break;
            }

            x = Math.Max(0, x);
            x = Math.Min(resolution, x);

            z = Math.Max(0, z);
            z = Math.Min(resolution, z);

            return new Coord(x, z);
        }

        public static Vector3 HeightmapCoordToWorldPos(this Terrain terrain, Coord terrainPos)
        {
            var terrainSize = terrain.terrainData.size;
            var resolution = terrain.terrainData.heightmapResolution - 1;
            var terrainSpaceCoordPos = new Vector3(terrainPos.x, 0, terrainPos.z);
            var normalizedPos = new Vector3(terrainSpaceCoordPos.x / (float)resolution, 0,
                (float)terrainSpaceCoordPos.z / resolution);
            var terrainSizePos = new Vector3(normalizedPos.x * terrainSize.x, 0, normalizedPos.z * terrainSize.z);
            terrainSizePos.y = terrain.SampleHeight(terrainSizePos);

            //terrainSizePos += new Vector3(0, 0, terrainSize.z / resolution) / 2;
            terrainSizePos += terrain.GetPosition();

            return terrainSizePos;
        }



        public static Coord WorldToSplatCoord(this Terrain terrain, Vector3 worldPos, RoundType rounding = RoundType.Round)
        {
            var terrainSize = terrain.terrainData.size;
            var resolution = terrain.terrainData.alphamapResolution;

            worldPos -= new Vector3((1f / (float)resolution) * terrainSize.x, 0, (1f / resolution) * terrainSize.z) / 2f;
            worldPos -= terrain.GetPosition();
            worldPos = new Vector3(worldPos.x / terrainSize.x, worldPos.y / terrainSize.y, worldPos.z / terrainSize.z);
            worldPos = new Vector3(worldPos.x * resolution, 0, worldPos.z * resolution);

            switch (rounding)
            {
                case RoundType.Round:    
                    return new Coord(Mathf.RoundToInt(worldPos.x), Mathf.RoundToInt(worldPos.z));
                case RoundType.Floor:
                    return new Coord(Mathf.FloorToInt(worldPos.x), Mathf.FloorToInt(worldPos.z));
                default:
                    return new Coord(Mathf.CeilToInt(worldPos.x), Mathf.CeilToInt(worldPos.z));
            }            
        }

        public static Vector3 SplatCoordToWorldPos(this Terrain terrain, Coord terrainPos)
        {
            var terrainSize = terrain.terrainData.size;
            var resolution = terrain.terrainData.alphamapResolution;

            var terrainSpaceCoordPos = new Vector3(terrainPos.x, 0, terrainPos.z);
            var normalizedPos = new Vector3(terrainSpaceCoordPos.x / (float)resolution, 0, (float)terrainSpaceCoordPos.z / resolution);
            var terrainSizePos = new Vector3(normalizedPos.x * terrainSize.x, 0, normalizedPos.z * terrainSize.z);
            terrainSizePos += terrain.GetPosition();
            terrainSizePos += new Vector3((1f / (float)resolution) * terrainSize.x, 0, (1f / resolution) * terrainSize.z) / 2f;

            return terrainSizePos;
        }

        public static Vector3 WorldToTreePos(this Terrain terrain, Vector3 worldPos)
        {
            var terrainPos = terrain.transform.position;
            var terrainSize = terrain.terrainData.size;
            var terrainSpacePos = worldPos - terrainPos;
            return new Vector3(terrainSpacePos.x / terrainSize.x, terrainSpacePos.y / terrainSize.y, terrainSpacePos.z / terrainSize.z);
        }

        public static Vector3 TreeToWorldPos(this Terrain terrain, Vector3 treePos)
        {
            var terrainPos = terrain.transform.position;
            var terrainSize = terrain.terrainData.size;

            var scaledPos = new Vector3(treePos.x * terrainSize.x, treePos.y * terrainSize.y, treePos.z * terrainSize.z);

            return terrainPos + scaledPos;
        }

        public static Vector3 DetailCoordToWorldPos(this Terrain terrain, Coord terrainPos)
        {
            var terrainSize = terrain.terrainData.size;
            var resolution = terrain.terrainData.detailResolution-1;
            var terrainSpaceCoordPos = new Vector3(terrainPos.x, 0, terrainPos.z);
            var normalizedPos = new Vector3(terrainSpaceCoordPos.x / (float)resolution, 0,
                (float)terrainSpaceCoordPos.z / resolution);
            var terrainSizePos = new Vector3(normalizedPos.x * terrainSize.x, 0, normalizedPos.z * terrainSize.z);
            terrainSizePos.y = terrain.SampleHeight(terrainSizePos);
            terrainSizePos += terrain.GetPosition();

            return terrainSizePos;
        }

        public static Coord WorldToDetailCoord(this Terrain terrain, Vector3 worldPos)
        {
            var terrainSize = terrain.terrainData.size;
            var resolution = terrain.terrainData.detailResolution-1;

            worldPos -= terrain.GetPosition();
            worldPos = new Vector3(worldPos.x / terrainSize.x, worldPos.y / terrainSize.y, worldPos.z / terrainSize.z);
            worldPos = new Vector3(worldPos.x * resolution, 0, worldPos.z * resolution);

            return new Coord(Mathf.RoundToInt(worldPos.x), Mathf.RoundToInt(worldPos.z));
        }

        public static Terrain FindEncompassingTerrain(Vector3 worldPosition)
        {
            for (int index = 0; index < Terrain.activeTerrains.Length; index++)
            {
                var terrain = Terrain.activeTerrains[index];
                var bounds = terrain.GetBounds();
                if (bounds.Contains(worldPosition))
                {
                    return terrain;
                }
            }
            return null;
        }
    }
}