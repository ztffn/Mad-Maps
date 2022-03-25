using System;
using System.Collections.Generic;
using MadMaps.Common;
using MadMaps.Common.GenericEditor;
using MadMaps.Common.Serialization;
using MadMaps.Roads;
using MadMaps.Terrains.Lookups;
using MadMaps.WorldStamps;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace MadMaps.Terrains
{
    public abstract class ProceduralLayerComponent : IHelpLinkProvider, IShowEnableToggle
    {
        public enum ApplyTiming
        {
            Instant,
            OnPreFinalise,
            OnPreRecalculate,
            OnPostFinalise,
            OnFrameAfterPostFinalise,
        }

        public abstract ApplyTiming Timing { get; }
        [HideInInspector]
        public bool Enabled = true;
        public abstract void Apply(ProceduralLayer layer, TerrainWrapper wrapper);

        public virtual string HelpURL
        {
            get { return null; }
        }

        public bool Editor_Enabled
        {
            get { return Enabled; }
            set { Enabled = value; }
        }

        public override string ToString()
        {
            return GetType().Name.SplitCamelCase();
        }
    }

    [Name("Procedural Layer")]
    public class ProceduralLayer : LayerBase, ISerializationCallbackReceiver
    {
        public List<ProceduralLayerComponent> Components = new List<ProceduralLayerComponent>();

        [HideInInspector]
        public List<Common.Serialization.DerivedComponentJsonDataRow> ComponentsJSON = new List<Common.Serialization.DerivedComponentJsonDataRow>();

        [HideInInspector]
        public List<string> TreeRemovals = new List<string>();
        public List<PrefabObjectData> Objects = new List<PrefabObjectData>();

        [Seed]
        public int Seed;

        public override void PrepareApply(TerrainWrapper terrainWrapper, int index)
        {
            if (Seed == 0)
            {
                Seed = Random.Range(int.MinValue, int.MaxValue);
            }

            terrainWrapper.OnPreRecalculate -= OnPreRecalculate;
            terrainWrapper.OnPreFinalise -= OnPreFinalise;
            terrainWrapper.OnPostFinalise -= OnPostFinalise;
            terrainWrapper.OnFrameAfterPostFinalise -= OnFrameAfterPostFinalise;

            if (Enabled)
            {
                terrainWrapper.OnPreRecalculate += OnPreRecalculate;
                terrainWrapper.OnPreFinalise += OnPreFinalise;
                terrainWrapper.OnPostFinalise += OnPostFinalise;
                terrainWrapper.OnFrameAfterPostFinalise += OnFrameAfterPostFinalise;
            }
        }

        private void OnFrameAfterPostFinalise(TerrainWrapper wrapper)
        {
            if (!Enabled)
            {
                return;
            }
            for (int i = 0; i < Components.Count; i++)
            {
                var proceduralLayerComponent = Components[i];
                if (!proceduralLayerComponent.Enabled)
                {
                    continue;
                }
                if (proceduralLayerComponent.Timing == ProceduralLayerComponent.ApplyTiming.OnFrameAfterPostFinalise)
                {
                    MiscUtilities.ProgressBar(
                        string.Format("Procedural Layer {0}: Processing {1}", name, proceduralLayerComponent.GetType()),
                        string.Format("{0}/{1}", i + 1, Components.Count), i / (float)Components.Count);
                    proceduralLayerComponent.Apply(this, wrapper);
                }
            }
            MiscUtilities.ClearProgressBar();
        }

        private void OnPostFinalise(TerrainWrapper wrapper)
        {
            if (!Enabled)
            {
                return;
            }
            for (int i = 0; i < Components.Count; i++)
            {
                var proceduralLayerComponent = Components[i];
                if (!proceduralLayerComponent.Enabled)
                {
                    continue;
                }
                if (proceduralLayerComponent.Timing == ProceduralLayerComponent.ApplyTiming.OnPostFinalise)
                {
                    MiscUtilities.ProgressBar(
                        string.Format("Procedural Layer {0}: Processing {1}", name, proceduralLayerComponent.GetType()),
                        string.Format("{0}/{1}", i + 1, Components.Count), i/(float) Components.Count);
                    proceduralLayerComponent.Apply(this, wrapper);
                }
            }
        }

        private void OnPreFinalise(TerrainWrapper wrapper)
        {
            if (!Enabled)
            {
                return;
            }
            for (int i = 0; i < Components.Count; i++)
            {
                var proceduralLayerComponent = Components[i];
                if (!proceduralLayerComponent.Enabled)
                {
                    continue;
                }
                if (proceduralLayerComponent.Timing == ProceduralLayerComponent.ApplyTiming.OnPreFinalise)
                {
                    proceduralLayerComponent.Apply(this, wrapper);
                }
            }
        }

        private void OnPreRecalculate(TerrainWrapper wrapper)
        {
            if (!Enabled)
            {
                return;
            }
            Objects.Clear();
            TreeRemovals.Clear();
            for (int i = 0; i < Components.Count; i++)
            {
                var proceduralLayerComponent = Components[i];
                if (!proceduralLayerComponent.Enabled)
                {
                    continue;
                }
                if (proceduralLayerComponent.Timing == ProceduralLayerComponent.ApplyTiming.OnPreRecalculate)
                {
                    MiscUtilities.ProgressBar(
                        string.Format("Procedural Layer {0}: Processing {1}", name, proceduralLayerComponent.GetType()),
                        string.Format("{0}/{1}", i + 1, Components.Count), i / (float)Components.Count);
                    proceduralLayerComponent.Apply(this, wrapper);
                }
            }
        }

        public override void WriteToTerrain(TerrainWrapper wrapper)
        {
            if (!Enabled)
            {
                return;
            }
            for (int i = 0; i < Components.Count; i++)
            {
                var proceduralLayerComponent = Components[i];
                if (!proceduralLayerComponent.Enabled)
                {
                    continue;
                }
                if (proceduralLayerComponent.Timing == ProceduralLayerComponent.ApplyTiming.Instant)
                {
                    MiscUtilities.ProgressBar(
                        string.Format("Procedural Layer {0}: Processing {1}", name, proceduralLayerComponent.GetType()),
                        string.Format("{0}/{1}", i + 1, Components.Count), i / (float)Components.Count);
                    proceduralLayerComponent.Apply(this, wrapper);
                }
            }

            foreach (var prefabObjectData in Objects)
            {
                wrapper.CompoundTerrainData.Objects.Add(prefabObjectData.Guid,
                    new InstantiatedObjectData(prefabObjectData, this, null));
            }
            
        }

        public override List<string> GetTreeRemovals()
        {
            return TreeRemovals;
        }

        public void OnBeforeSerialize()
        {
            ComponentsJSON.Clear();
            foreach (var proceduralLayerComponent in Components)
            {
                var type = proceduralLayerComponent.GetType();
                var jsonRow = new Common.Serialization.DerivedComponentJsonDataRow()
                {
                    AssemblyQualifiedName = type.AssemblyQualifiedName,
                    SerializedObjects = new List<Object>(),
                };
                jsonRow.JsonText = JSONSerializer.Serialize(type, proceduralLayerComponent, false,
                    jsonRow.SerializedObjects);
                ComponentsJSON.Add(jsonRow);
            }
        }

        public void OnAfterDeserialize()
        {
            Components.Clear();
            foreach (var component in ComponentsJSON)
            {
                Components.Add(
                (ProceduralLayerComponent)
                    JSONSerializer.Deserialize(Type.GetType(component.AssemblyQualifiedName), component.JsonText,
                        component.SerializedObjects));
            }
        }

        public override float BlendHeight(float sum, Vector3 worldPos, TerrainWrapper wrapper)
        {
            return sum;
        }

        public override List<PrefabObjectData> GetObjects()
        {
            return Objects;
        }
    }
}