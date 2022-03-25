using System;
using MadMaps.Common;
using MadMaps.Common.GenericEditor;
using MadMaps.Terrains;
using UnityEngine;

namespace MadMaps.Roads
{
    [Serializable]
    public class ComponentConfigurationRef
    {
        public ConnectionConfiguration Configuration;
        public string ComponentGUID;

        public ComponentConfigurationRef()
        {
        }

        public ComponentConfigurationRef(ConnectionConfiguration configuration, string guid)
        {
            Configuration = configuration;
            ComponentGUID = guid;
        }

        public T GetConfig<T>() where T : ConnectionConfigurationBase
        {
            return GetConfig() as T;
        }

        public ConnectionConfigurationBase GetConfig()
        {
            if (Configuration == null)
            {
                return null;
            }
            return Configuration.GetComponentWithGUID(ComponentGUID);
        }
    }

    public abstract class ConnectionComponent : LayerComponentBase
    {
        public bool OverridePriority;
        
        public ComponentConfigurationRef Configuration;
        public NodeConnection NodeConnection;
        [Common.GenericEditor.Min(1)]
        public int Priority = 1;

        public override bool GetEnabled()
        {
            if(!gameObject.activeInHierarchy || !enabled || Configuration == null)
            {
                return false;
            }
            var config = Configuration.GetConfig();
            return config != null && config.Enabled;
        }

        public override void SetEnabled(bool value)
        {
            enabled = value;
        }

        public override Vector3 Size
        {
            get
            {
                if(NodeConnection == null)
                {
                    return Vector3.zero;
                }
                return NodeConnection.GetSplineBounds().size;
            }
        }

        public override string GetLayerName()
        {
            return RoadNetwork.LAYER_NAME;
        }


        public RoadNetwork Network
        {
            get
            {
                if (NodeConnection == null)
                {
                    return null;
                }
                return NodeConnection.Network;
            }
        }

        public override Type GetLayerType()
        {
            return typeof(MMTerrainLayer);
        }

        public virtual void Think()
        {
            if (NodeConnection == null || NodeConnection.Equals(null))
            {
                Destroy();
                DestroyImmediate(this);
            }
        }

        public virtual void CopyTo(ConnectionComponent otherRo)
        {
            otherRo.Configuration = Configuration.JSONClone();
        }

        public virtual void SetData(NodeConnection nodeConnection, ComponentConfigurationRef config)
        {
            Configuration = config;
            NodeConnection = nodeConnection;
        }
        
        public virtual void Destroy()
        {
        }

        /// <summary>
        /// If a connection component wants to enforce that it should be done early or late
        /// </summary>
        /// <returns></returns>
        public override int GetPriority()
        {
            if (OverridePriority)
            {
                return Priority;
            }
            return Configuration != null && Configuration.GetConfig() != null ? Configuration.GetConfig().Priority : 1;
        }

        public override void SetPriority(int newPriority)
        {
            if (!OverridePriority)
            {
                Debug.LogWarning(string.Format("Setting Explicit Priority on {0} to {1}", name, newPriority));
                OverridePriority = true;
            }
            Priority = newPriority;
        }

        public void Strip()
        {
            DestroyImmediate(this);
        }
    }
}