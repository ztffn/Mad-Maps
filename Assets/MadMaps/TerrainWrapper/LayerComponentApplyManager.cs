using System;
using System.Collections.Generic;
using System.Linq;
using MadMaps.Common;
using MadMaps.Terrains;
using UnityEngine;
using UnityEngine.Profiling;
using Object = UnityEngine.Object;

namespace MadMaps.Terrains
{
    /// <summary>
    /// This class sets up the order of a collection of layer components, and then executes them.
    /// </summary>
    public static class LayerComponentApplyManager
    {
        public class LayerComponentMapping
        {
            public string LayerName;
            public int LayerIndex = -1;
            public List<LayerComponentBase> Components = new List<LayerComponentBase>();
        }

        public static List<LayerComponentMapping> SortComponents(TerrainWrapper wrapper, string layerFilter)
        {
            Profiler.BeginSample("CollectAndOrganise");

            // Collect all layerComponents that lie within the bounds of the TerrainWrapper and satisfy the layerFilter (if one is specified)
            var allLayerComponents = new List<LayerComponentBase>(Object.FindObjectsOfType<LayerComponentBase>());
            var tBounds = new Bounds(wrapper.Terrain.GetPosition() + wrapper.Terrain.terrainData.size / 2,
                wrapper.Terrain.terrainData.size);

            for (var i = allLayerComponents.Count - 1; i >= 0; i--)
            {
                var layerComponent = allLayerComponents[i];

                if (!String.IsNullOrEmpty(layerFilter) && layerComponent.GetLayerName() != layerFilter)
                {
                    allLayerComponents.RemoveAt(i);
                    continue;
                }

                if (!layerComponent.GetEnabled())
                {
                    allLayerComponents.RemoveAt(i);
                    continue;
                }

                var stampBounds = new ObjectBounds(layerComponent.transform.position, layerComponent.Size / 2,
                    layerComponent.transform.rotation);
                var axisStampBounds = stampBounds.ToAxisBounds();
                if (!tBounds.Intersects(axisStampBounds))
                {
                    allLayerComponents.RemoveAt(i);
                }
            }
            
            // Sort the Components into what layer they each point to
            List<LayerComponentMapping> mappings = new List<LayerComponentMapping>();
            for (int i = 0; i < allLayerComponents.Count; i++)
            {
                var component = allLayerComponents[i];
                LayerComponentMapping mapping = null;
                foreach (var layerStampMapping in mappings)
                {
                    if (layerStampMapping.LayerName == component.GetLayerName())
                    {
                        mapping = layerStampMapping;
                        break;
                    }
                }
                if (mapping == null)
                {
                    mapping = new LayerComponentMapping()
                    {
                        LayerIndex = wrapper.GetLayerIndex(component.GetLayerName()),
                        LayerName = component.GetLayerName()
                    };
                    mappings.Add(mapping);
                }
                mapping.Components.Add(component);
            }

            // Sort this mapping by layer index, then the contents of the mappings by priority
            mappings = mappings.OrderByDescending(mapping => mapping.LayerIndex).ToList();
            for (int i = 0; i < mappings.Count; i++)
            {
                mappings[i].Components = mappings[i].Components.OrderBy(component => component.GetPriority())
                    .ThenBy(component => component.transform.GetSiblingIndex())
                    .ToList();
            }

            // Go through and create new layers as needed
            HashSet<LayerBase> cache = new HashSet<LayerBase>();
            for (int i = mappings.Count - 1; i >= 0; i--)
            {
                var layerStampMapping = mappings[i];
                for(var j = 0; j < layerStampMapping.Components.Count; ++j)
                {
                    var component = layerStampMapping.Components[j];
                    var type = component.GetLayerType();
                    var layer = wrapper.GetLayer(type, layerStampMapping.LayerName, false, true);
                    if(cache.Contains(layer))
                    {
                        continue;
                    }
                    cache.Add(layer);

                    if(layer.Locked)
                    {
                        Debug.LogWarningFormat(layer, "Attempted to write to layer {0} but it was locked!", layer.name);
                        mappings.RemoveAt(i);
                        continue;
                    }

                    layer.Clear(wrapper);
                    layer.PrepareApply(wrapper, wrapper.GetLayerIndex(layer));
                }
                
            }
            Profiler.EndSample();
            return mappings;
        }

        /// <summary>
        /// TODO: This is a very complicated method. Is there a way to move towards componentisation?
        /// TODO: Layer filter method should be able to include/exclude arbitrary numbers of layers (currently it is all or one)
        /// Generally it doesn't make sense to apply only one component for a given layer. While partial recalculation
        /// might be possible, it doesn't currently exist. So when we recalcualte one component on a layer, we also
        /// recalculate all other Components on that layer.
        /// </summary>
        /// <param name="wrapper">The wrapper to recalculate the Components for</param>
        /// <param name="layerFilter">A name filter for the layers (for recalculating a single layer)</param>
        public static void ApplyAllLayerComponents(TerrainWrapper wrapper, string layerFilter = null)
        {
            Profiler.BeginSample("ApplyallLayerComponents");
            
            var mappings = SortComponents(wrapper, layerFilter);

            for (int i = 0; i < mappings.Count; i++)
            {
                var layerStampMapping = mappings[i];
                for (int j = 0; j < layerStampMapping.Components.Count; j++)
                {
                    MiscUtilities.ProgressBar(String.Format("PreBake for Component {0}", layerStampMapping.Components[j].name), String.Format("{0}/{1}", j, layerStampMapping.Components.Count), j / (float)layerStampMapping.Components.Count);
                    layerStampMapping.Components[j].OnPreBake();
                }
            }

            mappings = SortComponents(wrapper, layerFilter);
            if(wrapper.Locked)
            {   
                Debug.LogWarning(string.Format("Attempted to write to Terrain Wrapper {0} but it was locked.", wrapper.name), wrapper);
            }
            else
            {      
                bool anyLayerWrittenTo = false;
                // Okay, we've organised our Components in such a way so we can iterate through and execute them.
                // Iterating the mappings here is equivalent to iterating through the TerrainWrapper layers and executing all the relevant Components for each layer.
                for (int i = 0; i < mappings.Count; i++)
                {
                    var layerStampMapping = mappings[i];
                    var layer = wrapper.GetLayer<MMTerrainLayer>(layerStampMapping.LayerName, false, true);   // Is this redundant or just cautious? See line 100
                    
                    // Here, we copy the flattened information of all layers below this layer to the current layer, to easily blend new data with old data. 
                    // A somewhat naive solution, but the best I have found after trying several out.
                    wrapper.CopyCompoundToLayer(layer);

                    anyLayerWrittenTo = true;

                    if(wrapper.WriteHeights)
                    {
                        // Heights are a special type of layer, as layerComponents have the ability to compete with each other and determine who gets control of a point
                        // on the map through their height values. For this reason heights needed to be evaluated first, so winners can be determined.
                        for (int j = 0; j < layerStampMapping.Components.Count; j++)
                        {
                            MiscUtilities.ProgressBar(String.Format("Applying Heights for Component {0} : Layer {1}", layerStampMapping.Components[j].name, layer.name), String.Format("{0}/{1}", j, layerStampMapping.Components.Count), j / (float)layerStampMapping.Components.Count);
                            layerStampMapping.Components[j].ProcessHeights(wrapper, layer, j + 1);
                        }
                    }                    

                    // The Stencil is a critical component of componenting. Fundament It holds a dual purpose - blending 
                    for (int j = 0; j < layerStampMapping.Components.Count; j++)
                    {
                        var stamp = layerStampMapping.Components[j];                    
                        MiscUtilities.ProgressBar(String.Format("Applying Stencil for Component {0} : Layer {1}", stamp.name, layer.name), String.Format("{0}/{1}", j, layerStampMapping.Components.Count), j / (float)layerStampMapping.Components.Count);
                        stamp.ProcessStencil(wrapper, layer, j + 1);
                    }

                    MiscUtilities.ClampStencil(layer.Stencil);

                    for (int j = 0; j < layerStampMapping.Components.Count; j++)
                    {
                        var layerComponent = layerStampMapping.Components[j];
                        var stencilKey = j + 1;

                        if(wrapper.WriteSplats)
                        {
                            MiscUtilities.ProgressBar(String.Format("Applying Splats for Component {0} : Layer {1}", layerStampMapping.Components[j].name, layer.name), String.Format("{0}/{1}", j, layerStampMapping.Components.Count), j / (float)layerStampMapping.Components.Count);
                            layerComponent.ProcessSplats(wrapper, layer, stencilKey);
                        }
                        
                        if(wrapper.WriteObjects)
                        {
                            MiscUtilities.ProgressBar(String.Format("Applying Objects for Component {0} : Layer {1}", layerStampMapping.Components[j].name, layer.name), String.Format("{0}/{1}", j, layerStampMapping.Components.Count), j / (float)layerStampMapping.Components.Count);
                            layerComponent.ProcessObjects(wrapper, layer, stencilKey);
                        }

                        if(wrapper.WriteTrees)
                        {
                            MiscUtilities.ProgressBar(String.Format("Applying Trees for Component {0} : Layer {1}", layerStampMapping.Components[j].name, layer.name), String.Format("{0}/{1}", j, layerStampMapping.Components.Count), j / (float)layerStampMapping.Components.Count);
                            layerComponent.ProcessTrees(wrapper, layer, stencilKey);
                        }

                        if(wrapper.WriteDetails)
                        {
                            MiscUtilities.ProgressBar(String.Format("Applying Details for Component {0} : Layer {1}", layerStampMapping.Components[j].name, layer.name), String.Format("{0}/{1}", j, layerStampMapping.Components.Count), j / (float)layerStampMapping.Components.Count);
                            layerComponent.ProcessDetails(wrapper, layer, stencilKey);
                        }

                        #if VEGETATION_STUDIO
                        if(wrapper.WriteVegetationStudio)
                        {
                            MiscUtilities.ProgressBar(String.Format("Applying Details for Component {0} : Layer {1}", layerStampMapping.Components[j].name, layer.name), String.Format("{0}/{1}", j, layerStampMapping.Components.Count), j / (float)layerStampMapping.Components.Count);
                            layerComponent.ProcessVegetationStudio(wrapper, layer, stencilKey);
                        }
                        #endif

                        layer.Dirty = false;
                    }

                    if(anyLayerWrittenTo)
                    {
                        wrapper.SetDirtyAbove(layer);
                        MiscUtilities.ColoriseStencil(layer.Stencil);
                        wrapper.ClearCompoundCache(layer);                
        #if UNITY_EDITOR
                        UnityEditor.EditorUtility.SetDirty(layer);
        #endif
                        wrapper.Dirty = true;
                    }
                }
            }
            for (int i = 0; i < mappings.Count; i++)
            {
                var layerStampMapping = mappings[i];
                for (int j = 0; j < layerStampMapping.Components.Count; j++)
                {
                    layerStampMapping.Components[j].OnPostBake();
                }
            }
            MiscUtilities.ClearProgressBar();
            Profiler.EndSample();
        }
    }
}