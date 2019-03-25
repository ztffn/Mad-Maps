using System;
using System.Collections.Generic;
using MadMaps.Common.Collections;
using MadMaps.Terrains.Lookups;
using MadMaps.WorldStamps;
using UnityEngine;

namespace MadMaps.Terrains
{
    /// <summary>
    /// Basically the combined data of a series of layers
    /// TODO: we could possibly store a compound for EVERY layer, and then recalc off that
    /// These maps are getting very large very fast, however
    /// We put this data in an in-scene scriptable object
    /// Otherwise we get some very expensive OnBeforeSerialize with the terrainWrapper OnGUI
    /// </summary>
    public class BakedTerrainData : ScriptableObject
    {
        // Baked data
        public ObjectPrefabDataLookup Objects = new ObjectPrefabDataLookup();
        public TreeLookup Trees = new TreeLookup();
#if UNITY_2018_3_OR_NEWER
        public CompressedTerrainLayerSplatDataLookup TerrainLayerSplatData = new CompressedTerrainLayerSplatDataLookup();

        [Obsolete]
#endif
        public CompressedSplatDataLookup SplatData = new CompressedSplatDataLookup();
        public CompressedDetailDataLookup DetailData = new CompressedDetailDataLookup();
        public Serializable2DFloatArray Heights;
        
        #if VEGETATION_STUDIO
        public VegetationStudioLookup VegetationStudio = new VegetationStudioLookup();
        #endif

        public void Clear(TerrainWrapper terrainWrapper)
        {
            if (terrainWrapper.WriteObjects)
            {
                Objects.Clear();
            }
            if (terrainWrapper.WriteTrees)
            {
                Trees.Clear();
            }
            if (terrainWrapper.WriteDetails)
            {
                DetailData.Clear();
            }
            if (terrainWrapper.WriteSplats)
            {
#if UNITY_2018_3_OR_NEWER
                TerrainLayerSplatData.Clear();
#else
                SplatData.Clear();
#endif
            }
            if (terrainWrapper.WriteHeights && Heights != null)
            {
                Heights.Clear();
            }

#if VEGETATION_STUDIO
            if(terrainWrapper.WriteVegetationStudio)
            {
                VegetationStudio.Clear();
            }
#endif
        }
    }

    public class CompoundMMTerrainLayer
    {
        public Serializable2DFloatArray Heights;
        public List<PrefabObjectData> Objects = new List<PrefabObjectData>();
        public Dictionary<DetailPrototypeWrapper, Serializable2DByteArray> DetailData = new Dictionary<DetailPrototypeWrapper, Serializable2DByteArray>();
        public List<MadMapsTreeInstance> Trees = new List<MadMapsTreeInstance>();

#if UNITY_2018_3_OR_NEWER
        public Dictionary<TerrainLayer, Serializable2DByteArray> TerrainLayerSplatData = new Dictionary<TerrainLayer, Serializable2DByteArray>();
        [Obsolete]
#endif
        public Dictionary<SplatPrototypeWrapper, Serializable2DByteArray> SplatData = new Dictionary<SplatPrototypeWrapper, Serializable2DByteArray>();

#if VEGETATION_STUDIO
        public List<VegetationStudioInstance> VegetationStudio = new List<VegetationStudioInstance>();
#endif
    }
}