#if VEGETATION_STUDIO
using AwesomeTechnologies.Vegetation.PersistentStorage;
using AwesomeTechnologies.VegetationStudio;
using AwesomeTechnologies;
using AwesomeTechnologies.Common;
using AwesomeTechnologies.Billboards;
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

namespace MadMaps.Terrains
{
    [Serializable]
    public class VegetationStudioInstance
    {
        public string Guid;
        public string VSID;
        public VegetationPackage Package;
        public Vector3 Position;
        public Vector3 Scale = Vector3.one;
        public Vector3 Rotation;

        public VegetationStudioInstance Clone()
        {
            return new VegetationStudioInstance()
            {
                Guid = this.Guid,
                VSID = this.VSID,
                Position = this.Position,
                Scale = this.Scale,
                Rotation = this.Rotation,
                Package = this.Package,
            };
        }
    }

    [Serializable]
    public class VegetationStudioLookup : MadMaps.Common.Collections.CompositionDictionary<string, VegetationStudioInstance>
    {
        private const int Partitioning = 256;

        [Serializable]
        public class SpatialMapping : MadMaps.Common.Collections.CompositionDictionary<Coord, List<string>>{}

        [SerializeField]
        private SpatialMapping _mapping = new SpatialMapping();

        public Coord PositionToCoord(Vector3 pos)
        {
            int x = Mathf.FloorToInt(Mathf.Clamp01(pos.x)*Partitioning);
            int z = Mathf.FloorToInt(Mathf.Clamp01(pos.z)*Partitioning);
            return new Coord(x, z);
        }

        public void AppendPartitionList(Coord coord, List<string> toAppend)
        {
            if (toAppend == null)
            {
                toAppend = new List<string>();
            }
            List<string> partitionList;
            if (_mapping.TryGetValue(coord, out partitionList))
            {
                toAppend.AddRange(partitionList);
            }
        } 

        public override void Add(string key, VegetationStudioInstance value)
        {
            var coord = PositionToCoord(value.Position);
            List<string> partitionList;
            if (!_mapping.TryGetValue(coord, out partitionList))
            {
                partitionList = new List<string>();
                _mapping[coord] = partitionList;
            }
            base.Add(key, value);
            if (partitionList.Contains(value.Guid))
            {
                throw new Exception("Same tree GUID in spatial mapping???");
            }
            partitionList.Add(value.Guid);
        }

        public override bool Remove(string key)
        {
            VegetationStudioInstance value;
            if (TryGetValue(key, out value))
            {
                var coord = PositionToCoord(value.Position);
                List<string> partitionList;
                if (_mapping.TryGetValue(coord, out partitionList))
                {
                    partitionList.Remove(key);
                }
            }
            return base.Remove(key);
        }

        public override void Clear()
        {
            _mapping.Clear();
            base.Clear();
        }
    }

    public static class VegetationStudioUtilities
    {
        public static List<VegetationSystem> GetVegetationSystemsForTerrain(Terrain terrain)
        {
            var result = new List<VegetationSystem>();
            var vegSystems = UnityEngine.Object.FindObjectsOfType<VegetationSystem>();
            foreach(var vegSys in vegSystems)
            {
                if(vegSys.GetTerrain() == terrain)
                {
                    result.Add(vegSys);
                }
            }
            return result;
        }

        public static void SnapshotVegetationStudioData(this MMTerrainLayer layer, Terrain terrain)
        {
            layer.VSRemovals.Clear();
            layer.VSInstances.Clear();

            var vegStorages = UnityEngine.Object.FindObjectsOfType<PersistentVegetationStorage>();
            foreach(var vegStorage in vegStorages)
            {
                var vegSys = vegStorage.GetComponent<VegetationSystem>();
                if(vegSys.GetTerrain() != terrain)
                {
                    continue;
                }
                if(vegSys.CurrentVegetationPackage == null)
                {
                    Debug.LogWarning(string.Format("Couldn't capture from system {0} as it doesn't have a Vegetation Package assigned.", vegSys), vegSys);
                    continue;
                }
                if(vegStorage == null)
                {
                    Debug.LogError(string.Format("Unable to find Vegetation Storage from system {0}!", vegSys), vegSys);
                    continue;
                }
                if(vegStorage.PersistentVegetationStoragePackage == null)
                {
                    Debug.LogError(string.Format("Unable to find PersistentVegetation Storage from system {0}!", vegStorage), vegStorage);
                    continue;
                }
                foreach(var cell in vegStorage.PersistentVegetationStoragePackage.PersistentVegetationCellList)
                {
                    foreach(var info in cell.PersistentVegetationInfoList)
                    {
                        foreach(var item in info.VegetationItemList)
                        {
                            var wPos = terrain.GetPosition() + item.Position;
                            var pos = terrain.WorldToTreePos(wPos);
                            pos.y = wPos.y - terrain.SampleHeight(wPos) - terrain.GetPosition().y;
                            layer.VSInstances.Add(new VegetationStudioInstance()
                            {
                                Guid = System.Guid.NewGuid().ToString(),
                                Position = pos,
                                Rotation = item.Rotation.eulerAngles,
                                Scale = item.Scale,
                                VSID = info.VegetationItemID,
                                Package = vegSys.CurrentVegetationPackage,
                            });
                        }
                    }
                }
            }
        }

        public static void SetupTerrain(Terrain terrain, ref VegetationPackage package, out VegetationSystem system, out PersistentVegetationStorage storage)
        {
            var vetSystems = GetVegetationSystemsForTerrain(terrain);
            VegetationSystem vetSys = null;
            for(var i = 0; i < vetSystems.Count; ++i)
            {
                var candidate = vetSystems[i];
                if(candidate.CurrentVegetationPackage == package)
                {
                    vetSys = candidate;
                    break;
                }
            }
            
			if (vetSys == null) 
			{
				vetSys = VegetationStudioManager.AddVegetationSystemToTerrain(terrain, package, createPersistentVegetationStoragePackage:true);
			}
            if(package != null)
            {
                if (vetSys.VegetationPackageList.Count == 0) 
                {
                    vetSys.VegetationPackageList.Add(package);
                }
                if (vetSys.VegetationPackageList.Count == 1 && vetSys.VegetationPackageList[0] == null) 
                {
                    vetSys.VegetationPackageList[0] = package;
                }
                if (!vetSys.VegetationPackageList.Contains(package)) 
                {
                    vetSys.VegetationPackageList.Add(package);
                }
            }			
			if (!vetSys.InitDone) 
			{
				vetSys.SetupVegetationSystem();
				vetSys.RefreshVegetationPackage();
			}
            vetSys.SetSleepMode(false);
            package = vetSys.CurrentVegetationPackage;
            system = vetSys;
            storage = vetSys.GetComponent<PersistentVegetationStorage>();
        }
    }
  
    public partial class MMTerrainLayer : LayerBase
    {
        
        public List<VegetationStudioInstance> VSInstances = new List<VegetationStudioInstance>();
        
        public List<string> VSRemovals = new List<string>();
        public override List<VegetationPackage> GetPackages()
        {
            List<VegetationPackage> result = new List<VegetationPackage>();
            foreach(var instance in VSInstances)
            {
                if(instance.Package != null && !result.Contains(instance.Package))
                {
                    result.Add(instance.Package);
                }
            }
            return result;
        }

        private void WriteVegetationStudioToTerrain(TerrainWrapper wrapper, Bounds bounds)
        {
            var existingVSData = wrapper.CompoundTerrainData.VegetationStudio;
            if (BlendMode == EMMTerrainLayerBlendMode.Set)
            {
                existingVSData.Clear();
            }
            for (var i = 0; i < VSInstances.Count; i++)
            {
                existingVSData.Add(VSInstances[i].Guid, VSInstances[i]);
            }
            foreach (var treeRemoval in VSRemovals)
            {
                if(!existingVSData.Remove(treeRemoval))
                {
                    //Debug.LogWarning(string.Format("Layer {0}: Unable to remove tree {1}", this, treeRemoval));
                }
            }
        }

        public override List<VegetationStudioInstance> GetVegetationStudioData()
        {
            return VSInstances;
        }

        public override List<string> GetVegetationStudioRemovals()
        {
            return VSRemovals;
        }

    }
}
#endif