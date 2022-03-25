using System;
using System.Collections.Generic;
using UnityEngine;

#if VEGETATION_STUDIO
using MadMaps.Integration.VegetationStudio;
#endif

namespace MadMaps.WorldStamps.Authoring
{
    [Serializable]
    public class WorldStampCaptureTemplate
    {
#if UNITY_EDITOR
        public Bounds Bounds;
        public Terrain Terrain;

        public int Layer;
        public DetailDataCreator DetailDataCreator = new DetailDataCreator();
        public HeightmapDataCreator HeightmapDataCreator = new HeightmapDataCreator();
        public MaskDataCreator MaskDataCreator = new MaskDataCreator();
        public ObjectDataCreator ObjectDataCreator = new ObjectDataCreator();
        public RoadDataCreator RoadDataCreator = new RoadDataCreator();
        public SplatDataCreator SplatDataCreator = new SplatDataCreator();
        public TreeDataCreator TreeDataCreator = new TreeDataCreator();
        #if VEGETATION_STUDIO
        public VegetationStudioDataCreator VegetationStudioDataCreator = new VegetationStudioDataCreator();
        #endif

        public List<WorldStampCreatorLayer> Creators
        {
            get
            {
                if (__creators == null)
                {
                    __creators = new List<WorldStampCreatorLayer>
                    {
                        HeightmapDataCreator,
                        SplatDataCreator,
                        DetailDataCreator,
                        TreeDataCreator,
                        ObjectDataCreator,
                        RoadDataCreator,
                        #if VEGETATION_STUDIO
                        VegetationStudioDataCreator,
                        #endif
                        MaskDataCreator,                        
                    };
                }
                return __creators;
            }
        }
        private List<WorldStampCreatorLayer> __creators;
#endif
    }
}