#if VEGETATION_STUDIO
using System.Linq;
using System.Collections.Generic;
using MadMaps.Common;
using MadMaps.Common.Collections;
using MadMaps.WorldStamps;
using UnityEngine;
using Debug = UnityEngine.Debug;
using AwesomeTechnologies.Vegetation.PersistentStorage;
using AwesomeTechnologies.VegetationStudio;
using AwesomeTechnologies;
using AwesomeTechnologies.Common;
using AwesomeTechnologies.Billboards;

namespace MadMaps.Terrains
{
    public partial class TerrainWrapper 
    {
        public const byte VegetationStudio_ID = 123;

        public bool WriteVegetationStudio = true;
        
        struct VegetationSystemData
        {
            public VegetationSystem System;
            public PersistentVegetationStorage Storage;
        }

        private void PrepareVegetationStudio()
        {
        }

        private void FinaliseVegetationStudio()
        {
            Dictionary<VegetationPackage, VegetationSystemData> lookup = new Dictionary<VegetationPackage, VegetationSystemData>();
            foreach(var layer in Layers)
            {
                var packages = layer.GetPackages();
                if(packages == null)
                {
                    continue;
                }
                for(var i = 0; i < packages.Count; ++i)
                {
                    var package = packages[i];
                    if(lookup.ContainsKey(package))
                    {
                        continue;
                    }

                    VegetationSystem system;
                    PersistentVegetationStorage storage;
                    
                    VegetationStudioUtilities.SetupTerrain(Terrain, ref package, out system, out storage);                    
                    foreach(var info in package.VegetationInfoList)
                    {
                        storage.RemoveVegetationItemInstances(info.VegetationItemID, VegetationStudio_ID);
                    }

                    lookup[package] = new VegetationSystemData(){
                        System = system, Storage = storage, };                    
                }
            }
            
            var tSize = Terrain.terrainData.size;
            foreach(var kvp in CompoundTerrainData.VegetationStudio)
            {
                var instance = kvp.Value;
                VegetationSystemData data;
                if(instance.Package == null)
                {
                    Debug.LogError(string.Format("Vegetation instance (ID: {0}) with missing VegetationPackage!", instance.VSID), this);
                    continue;
                }
                if(!lookup.TryGetValue(instance.Package, out data) || data.System == null || data.Storage == null)
                {
                    Debug.LogError("Failed to find matching system for package " + instance.Package);
                    continue;
                }
                var worldPos = Terrain.TreeToWorldPos(instance.Position);
                var height = GetCompoundHeight(null, worldPos);
                worldPos.y = instance.Position.y + height * tSize.y;
                //DebugHelper.DrawPoint(worldPos, .2f, Color.red, 10);
                data.Storage.AddVegetationItemInstance(instance.VSID, worldPos, instance.Scale, 
                    Quaternion.Euler(instance.Rotation), true, VegetationStudio_ID);
            }

            foreach(var kvp in lookup)
            {
                var system = kvp.Value.System;
                for (int j = 0; j < system.VegetationCellList.Count; j++)
                {
                    system.VegetationCellList[j].ClearCache();
                }
                system.UnityTerrainData = new UnityTerrainData(Terrain, false, false);
                var billboardSystem = system.GetComponent<BillboardSystem>(); 
                billboardSystem.RefreshBillboards();
            }
        }

        public List<VegetationStudioInstance> GetCompoundVegetationStudioData(LayerBase terminatingLayer, bool includeTerminatingLayer = false, Bounds? bounds = null)
        {
            if (!includeTerminatingLayer && _compoundDataCache.ContainsKey(terminatingLayer))
            {
                var data = _compoundDataCache[terminatingLayer].VegetationStudio;
                return data; 
            }

            var result = new Dictionary<string, VegetationStudioInstance>();
            for (var i = Layers.Count - 1; i >= 0; i--)
            {
                var layer = Layers[i];
                if (!includeTerminatingLayer && layer == terminatingLayer)
                {
                    break;
                }
                List<VegetationStudioInstance> vsData = layer.GetVegetationStudioData();
                if (vsData != null)
                {
                    for (int j = 0; j < vsData.Count; j++)
                    {
                        var vsdataInstance = vsData[j];
                        if (bounds.HasValue && !bounds.Value.Contains(Terrain.TreeToWorldPos(vsdataInstance.Position)))
                        {
                            continue;
                        }
                        result.Add(vsdataInstance.Guid, vsdataInstance);
                    }
                }
                List<string> removals = layer.GetVegetationStudioRemovals();
                if (removals != null)
                {
                    for (int j = 0; j < removals.Count; j++)
                    {
                        var treeRemoval = removals[j];
                        if(!result.Remove(treeRemoval))
                        {
                            //Debug.LogWarning(string.Format("Layer {0} was unable to remove Vegetation Studio instance with GUID {1}", layer, treeRemoval));
                        }
                    }
                }
                if (layer == terminatingLayer)
                {
                    break;
                }
            }
            return result.Values.ToList();
        }
    }
}
#endif