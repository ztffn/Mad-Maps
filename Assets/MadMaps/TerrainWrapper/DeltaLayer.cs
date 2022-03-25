using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using MadMaps.Common;
using MadMaps.Common.Collections;
using MadMaps.Common.GenericEditor;
using MadMaps.Common.Serialization;
using MadMaps.Terrains.Lookups;
using MadMaps.WorldStamps;
using UnityEngine;
using System.Linq;

/*
namespace MadMaps.Terrains
{
    [Name("Delta Layer")]
    public partial class DeltaLayer : MMTerrainLayer
    {
        public virtual void SnapshotTerrain(Terrain terrain)
        {
            var wrapper = terrain.GetComponent<TerrainWrapper>();
            if(wrapper == null)
            {
                Debug.LogError("Couldn't snapshot Delta Layer - no TerrainWrapper found!");
                return;
            }

            Debug.Log("Snapshotted Terrain " + terrain.name, terrain);
            this.SnapshotHeights(wrapper);
            this.SnapshotSplats(wrapper);
            this.SnapshotDetails(wrapper);
            this.SnapshotTrees(wrapper);
            this.SnapshotObjects(wrapper);

            #if VEGETATION_STUDIO
            this.SnapshotVegetationStudioData(wrapper);
            #endif
        }
    }

}
*/