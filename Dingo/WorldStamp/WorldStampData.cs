﻿using System;
using System.Collections.Generic;
using Dingo.Common.Collections;
using Dingo.Terrains;
using Dingo.Terrains.Lookups;
using Dingo.Common;
using Dingo.Common.Painter;
using UnityEngine;

namespace Dingo.WorldStamp
{
    [Serializable]
    public class WorldStampData
    {
        public float GridSize = 1;
        public GridManagerInt GridManager
        {
            get
            {
                if (__gridManager == null || __gridManager.GRID_SIZE != GridSize)
                {
                    __gridManager = new GridManagerInt(GridSize);
                }
                return __gridManager;
            }
        }
        private GridManagerInt __gridManager;

        public WorldStampMask Mask = new WorldStampMask();
        // The physical size this snapshot was when baked
        public Vector3 Size = new Vector3(1,1,1);

        // HEIGHTS
        public Serializable2DFloatArray Heights;
        
        // TREES
        public List<DingoTreeInstance> Trees = new List<DingoTreeInstance>();

        // OBJECTS
        public List<PrefabObjectData> Objects = new List<PrefabObjectData>();

        // SPLATS
        public List<CompressedSplatData> SplatData = new List<CompressedSplatData>();

        // GRASS
        public List<CompressedDetailData> DetailData = new List<CompressedDetailData>();

    }
}