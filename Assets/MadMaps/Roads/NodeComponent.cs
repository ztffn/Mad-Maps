using MadMaps.Common;
using MadMaps.Terrains;
using UnityEngine;
using System;

namespace MadMaps.Roads
{
    public abstract class NodeComponent : LayerComponentBase
    {
        [HideInInspector]
        public Node Node;
        public int Priority = 1;
        public string LayerName = "New Layer";

        public override bool GetEnabled()
        {
            return gameObject.activeInHierarchy && enabled;
        }

        public override void SetEnabled(bool value)
        {
            enabled = value;
        }

        public RoadNetwork Network
        {
            get
            {
                if (!Node)
                {
                    Node = GetComponent<Node>();
                }
                return Node.Network;
            }
        }

        public virtual void Think()
        {
        }

        public virtual void Strip()
        {
            DestroyImmediate(this);
        }

        public void Reset()
        {
            Node = GetComponent<Node>();
        }

        public override int GetPriority() { return Priority; }
        public override void SetPriority(int priority) {Priority = priority;}
        public override string GetLayerName() {return LayerName;}        
        public override Type GetLayerType() {return typeof(MMTerrainLayer);}
    }
}