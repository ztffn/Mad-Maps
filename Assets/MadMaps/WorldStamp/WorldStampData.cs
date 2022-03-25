using System;
using System.Collections.Generic;
using MadMaps.Common.Painter;
using MadMaps.Common.Collections;
using MadMaps.Terrains;
using MadMaps.Terrains.Lookups;
using UnityEngine;

#if UNITY_EDITOR
using Painter = MadMaps.Common.Painter.Painter;
using IGridManager = MadMaps.Common.Painter.IGridManager;
using GridManagerInt = MadMaps.Common.Painter.GridManagerInt;
using IBrush = MadMaps.Common.Painter.IBrush;
using EditorCellHelper = MadMaps.Common.Painter.EditorCellHelper;
#endif

#if VEGETATION_STUDIO
using AwesomeTechnologies;
#endif

namespace MadMaps.WorldStamps
{
    [Serializable]
    public class WorldStampData
    {
        public float GridSize = 1;
        
        public Common.Painter.GridManagerInt GridManager
        {
            get
            {
                if (__gridManager == null || __gridManager.GRID_SIZE != GridSize)
                {
                    __gridManager = new Common.Painter.GridManagerInt(GridSize);
                }
                return __gridManager;
            }
        }
        private Common.Painter.GridManagerInt __gridManager;

        public WorldStampMask Mask = new WorldStampMask();

        // The physical size this snapshot was when baked
        public Vector3 Size = new Vector3(1,1,1);

        // HEIGHTS
        public Serializable2DFloatArray Heights;
        public float ZeroLevel;
        
        // TREES
        public List<MadMapsTreeInstance> Trees = new List<MadMapsTreeInstance>();
        public List<GameObject> TreePrototypeCache = new List<GameObject>();

        #if VEGETATION_STUDIO
        public VegetationPackage Package;
        public List<VegetationStudioInstance> VSData = new List<VegetationStudioInstance>();
        #endif

        // OBJECTS
        public List<PrefabObjectData> Objects = new List<PrefabObjectData>();

        // SPLATS
        public List<CompressedSplatData> SplatData = new List<CompressedSplatData>();

        // GRASS
        public List<CompressedDetailData> DetailData = new List<CompressedDetailData>();
        
    }
}