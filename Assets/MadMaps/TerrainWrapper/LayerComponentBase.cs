using System;
using System.Collections.Generic;
using System.Linq;
using MadMaps.Common;
using MadMaps.Common.Collections;
using MadMaps.Terrains;
using UnityEngine;
using UnityEngine.Serialization;
using MadMaps.Common.GenericEditor;

namespace MadMaps.Terrains
{
    [Serializable]
    public class ChannelFilter
    {
        public bool HeightMap = true;
        public bool Splats = true;
        public bool Details = true;
        public bool Trees = true;
        public bool Objects = true;
        public bool Stencil = true;
        #if VEGETATION_STUDIO
        public bool VegetationStudio = true;
        #endif
    }

    public abstract class LayerComponentBase : sBehaviour
    {
        public abstract int GetPriority();
        public abstract void SetPriority(int priority);
        public abstract string GetLayerName();
        public abstract Vector3 Size { get; }
        public abstract Type GetLayerType();
        public abstract bool GetEnabled();
        public abstract void SetEnabled(bool value);
        public virtual void OnPreBake(){}
        public virtual void ProcessHeights(TerrainWrapper wrapper, LayerBase layer, int stencilKey){}
        public virtual void ProcessStencil(TerrainWrapper wrapper, LayerBase layer, int stencilKey){}
        public virtual void ProcessSplats(TerrainWrapper wrapper, LayerBase layer, int stencilKey){}
        public virtual void ProcessObjects(TerrainWrapper wrapper, LayerBase layer, int stencilKey){}
        public virtual void ProcessTrees(TerrainWrapper wrapper, LayerBase layer, int stencilKey){}
        public virtual void ProcessDetails(TerrainWrapper wrapper, LayerBase layer, int stencilKey){}
        #if VEGETATION_STUDIO
        public virtual void ProcessVegetationStudio(TerrainWrapper wrapper, LayerBase layer, int stencilKey){}
        #endif
        public virtual void OnPostBake(){}

        public List<TerrainWrapper> GetTerrainWrappers()
        {
            var result = new List<TerrainWrapper>();
            var allT = FindObjectsOfType<TerrainWrapper>();
            var stampBounds =
                new ObjectBounds(transform.position + Vector3.up*(Size.y/2), Size/2, transform.rotation).ToAxisBounds();
            foreach (var terrainWrapper in allT)
            {
                var b = terrainWrapper.GetComponent<TerrainCollider>().bounds;
                b.Expand(Vector3.up*9999999);
                if (b.Intersects(stampBounds))
                {
                    result.Add(terrainWrapper);
                }
            }
            return result;
        }
    }
}