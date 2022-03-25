using System;
using System.Collections.Generic;
using MadMaps.Common;
using MadMaps.Terrains;
using UnityEngine;
using Random = UnityEngine.Random;
using MadMaps.WorldStamps.Authoring;

namespace MadMaps.Roads
{
    [AttributeUsage(AttributeTargets.Field)]
    public class SeedAttribute : PropertyAttribute
    {
    }

    [Serializable]
    public class NodeConfiguration
    {
        public enum ESnappingMode
        {
            None,
            Wrapper,
            Terrain,
            Raycast,
        }

        public ESnappingMode SnappingMode = ESnappingMode.Wrapper;
        public float SnapDistance = 800;
        public float SnapOffset = 0;

        public bool IsExplicitControl = false;
        public Vector3 ExplicitControl = new Vector3(1, 0, 0);

        public bool OverrideCurviness = false;
        public float Curviness = 30;
        public LayerMask SnapMask = ~0;

        public NodeConfiguration Clone()
        {
            return new NodeConfiguration()
            {
                SnappingMode = this.SnappingMode,
                SnapDistance = this.SnapDistance,
                SnapOffset = this.SnapOffset,
                IsExplicitControl = this.IsExplicitControl,
                ExplicitControl = this.ExplicitControl,
                OverrideCurviness = this.OverrideCurviness,
                Curviness = this.Curviness,
                SnapMask = this.SnapMask,                
            };
        }
    }

    [SelectionBase]
    [ExecuteInEditMode]
    [HelpURL("http://lrtw.net/madmaps/index.php?title=Road_Network")]
#if HURTWORLDSDK
    [StripComponentOnBuild]
#endif
    public class Node : LayerComponentBase
    {
        public List<NodeConnection> InConnections = new List<NodeConnection>();
        public List<NodeConnection> OutConnections = new List<NodeConnection>();

        public NodeConfiguration Configuration = new NodeConfiguration();
        public Vector3 Offset;

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
                if (!__network)
                {
                    __network = transform.GetComponentInAncestors<RoadNetwork>();
                    if (!__network)
                    {
                        var proxy = transform.GetComponentInAncestors<RoadNetworkProxy>();
                        if (proxy)
                        {
                            __network = proxy.Network;
                        }
                    }
                }
                return __network;
            }
        }
        private RoadNetwork __network;

        public int ConnectionCount
        {
            get { return InConnections.Count + OutConnections.Count; }
        }

        [Seed]
        public int Seed = 0;
        public bool Dirty;

        private Vector3 _lastSnapPosition;

        public NodeConnection ConnectTo(Node next, ConnectionConfiguration config)
        {
            if (next == this)
            {
                Debug.LogError("Attempted to connect a RoadNode to itself!", this);
                return null;
            }
            if (IsConnectedTo(next))
            {
                Debug.LogError(string.Format("RoadNode {0} is already connected to RoadNode {1}", name, next.name), this);
                return null;
            }

            var connection = gameObject.AddComponent<NodeConnection>();
            connection.SetData(this, next, config);
            OutConnections.Add(connection);
            next.InConnections.Add(connection);
            return connection;
        }

        public void DisconnectFrom(Node next)
        {
            if (next == this)
            {
                Debug.LogWarning("Attempted to disconnect a RoadNode from itself!", this);
                return;
            }
            if (!IsConnectedTo(next))
            {
                //Debug.LogError(string.Format("RoadNode {0} is already disconnected from RoadNode {1}", name, next.name), this);
                return;
            }

            // Remove from outs
            for (int i = OutConnections.Count-1; i >= 0; i--)
            {
                if (OutConnections[i] == null)
                {
                    OutConnections.RemoveAt(i);
                    continue;
                }
                var nextNode = OutConnections[i].NextNode;
                if (nextNode == next)
                {
                    // Remove from ins
                    for (int j = nextNode.InConnections.Count - 1; j >= 0; j--)
                    {
                        if (nextNode.InConnections[j] == null)
                        {
                            nextNode.InConnections.RemoveAt(j);
                            continue;
                        }
                        if (nextNode.InConnections[j].ThisNode == this)
                        {
                            nextNode.InConnections.RemoveAt(j);
                        }
                    }

                    OutConnections[i].Destroy();
                    DestroyImmediate(OutConnections[i]);
                    OutConnections.RemoveAt(i);
                }
            }
        }

        public bool IsConnectedTo(Node second)
        {
            if (second == this)
            {
                return true;
            }
            for (int i = InConnections.Count-1; i >= 0; i--)
            {
                var nodeConnection = InConnections[i];
                if (nodeConnection == null)
                {
                    InConnections.RemoveAt(i);
                    continue;
                }
                if (nodeConnection.NextNode == second)
                {
                    return true;
                }
            }
            for (int i = OutConnections.Count-1; i >= 0; i--)
            {
                var nodeConnection = OutConnections[i];
                if (nodeConnection == null)
                {
                    OutConnections.RemoveAt(i);
                    continue;
                }
                if (nodeConnection.NextNode == second)
                {
                    return true;
                }
            }
            return false;
        }

        public Vector3 CalculateTangent(Node nextNode)
        {
            var breakAngle = Network != null ? Network.BreakAngle : 90;
            Vector3 normal = Vector3.zero;
            var connectionCount = InConnections.Count + OutConnections.Count;
            if (connectionCount == 0)
            {
                return normal;
            }

            int totalConnectionCounter = 0;
            // Get average dir to node
            for (int i = InConnections.Count-1; i >= 0; i--)
            {
                if (InConnections[i] == null)
                {
                    InConnections.RemoveAt(i);
                    continue;
                }
                var nodeDirection = InConnections[i].GetNodeDirection();
                if (InConnections[i].ThisNode == nextNode)
                {
                    nodeDirection *= -1;
                }
                var angle = Vector3.Angle(normal, -nodeDirection);
                if (totalConnectionCounter >= 2 && angle > breakAngle)
                {
                    if (InConnections[i].ThisNode == nextNode)
                    {
                        // We're breaking on this node, so we don't curve
                        return Vector3.zero;
                    }
                    continue;
                }
                
                //Debug.DrawLine(transform.position + Vector3.up, transform.position + nodeDirection * 10 + Vector3.up, Color.yellow);
                normal += nodeDirection;
                totalConnectionCounter++;
            }
            for (int i = OutConnections.Count-1; i >= 0; i--)
            {
                if (OutConnections[i] == null)
                {
                    OutConnections.RemoveAt(i);
                    continue;
                }
                var nodeDirection = OutConnections[i].GetNodeDirection();
                if (OutConnections[i].NextNode != nextNode)
                {
                    nodeDirection *= -1;
                }
                var angle = Vector3.Angle(normal, nodeDirection);
                if (totalConnectionCounter >= 2 && angle > breakAngle)
                {
                    if (OutConnections[i].NextNode == nextNode)
                    {
                        // We're breaking on this node, so we don't curve
                        return Vector3.zero;
                    }
                    continue;
                }
                //Debug.DrawLine(transform.position + Vector3.up, transform.position + nodeDirection * 10 + Vector3.up, Color.green);
                normal += nodeDirection;
                totalConnectionCounter++;
            }
            // avg
            if (connectionCount > 1)
            {
                normal = (normal / (connectionCount)).normalized;
            }
            
            return normal;
        }
        
        public void Think()
        {
            if (!gameObject || !gameObject.activeInHierarchy)
            {
                return;
            }

            if (RoadNetwork.ThinkCache.Contains(this))
            {
                return;
            }

            if (Dirty)
            {
                Dirty = false;
                _lastSnapPosition = new Vector3(float.NaN, float.NaN, float.NaN);
            }

            if (Seed == 0)
            {
                Seed = Random.Range(1, 99999);
            }

            var allComponents = gameObject.GetComponents<NodeComponent>();
            for (int i = 0; i < allComponents.Length; i++)
            {
                allComponents[i].Think();
            }

            if (!this || !gameObject)
            {
                return;
            }

            var allConnection = GetComponents<NodeConnection>();
            for (int i = 0; i < allConnection.Length; i++)
            {
                var nodeConnection = allConnection[i];
                if (nodeConnection.ThisNode == this && nodeConnection.NextNode != null && !OutConnections.Contains(nodeConnection))
                {
                    Debug.LogWarning("NodeConnection became detached somehow - fixing.", this);
                    OutConnections.Add(nodeConnection);
                }
                nodeConnection.Think();
            }

            Snap();

            RoadNetwork.ThinkCache.Add(this);

            for (int i = OutConnections.Count-1; i >= 0; i--)
            {
                var nodeConnection = OutConnections[i];
                if (nodeConnection == null)
                {
                    OutConnections.RemoveAt(i);
                    continue;
                }
                nodeConnection.NextNode.Think();
            }
        }

        public void Snap()
        {
            if (!Network || Configuration.SnappingMode == NodeConfiguration.ESnappingMode.None || Vector3.Distance(_lastSnapPosition, transform.position) < .01f)
            {
                return; // Do nothing in this case
            }

            if (Configuration.SnappingMode == NodeConfiguration.ESnappingMode.Wrapper)
            {
                var pos = transform.position;
                var wrapper = TerrainWrapper.GetWrapper(pos);
                if (wrapper != null)
                {
                    if (wrapper.Layers.Count > 0)
                    {
                        var newY = wrapper.GetCompoundHeight(Network.GetLayer(wrapper), pos) * wrapper.Terrain.terrainData.size.y
                            + wrapper.Terrain.GetPosition().y + Configuration.SnapOffset;
                        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
                    }
                    else
                    {
                        Debug.LogWarning("Node with a Wrapper snap setting on a terrain, but wrapper has no layers - this won't work.", this);
                    }
                }
                else
                {
                    Debug.LogWarning("Node with a Wrapper snap setting on a terrain without a TerrainWrapper component - this won't work.", this);
                }
                _lastSnapPosition = transform.position;
            }

            if (Configuration.SnappingMode == NodeConfiguration.ESnappingMode.Terrain)
            {
                var terrain = TerrainX.FindEncompassingTerrain(transform.position);
                var newY = terrain.SampleHeight(transform.position) + terrain.GetPosition().y + Configuration.SnapOffset;
                transform.position = new Vector3(transform.position.x, newY, transform.position.z);
                _lastSnapPosition = transform.position;
            }

            if (Configuration.SnappingMode == NodeConfiguration.ESnappingMode.Raycast)
            {
                RaycastHit hitInfo;
                if (Physics.Raycast(
                    new Ray(transform.position + Vector3.up*Configuration.SnapDistance/2, Vector3.down), out hitInfo,
                    Configuration.SnapDistance, Configuration.SnapMask))
                {
                    transform.position = hitInfo.point + Vector3.up*Configuration.SnapOffset;
                }
                _lastSnapPosition = transform.position;
            }

            var exitNode = GetComponent<StampExitNode>();
            if(exitNode)
            {
                exitNode.Update();
            }
        }

        public float GetCurviness()
        {
            if (Configuration.OverrideCurviness)
            {
                return Configuration.Curviness;
            }
            float curve = 0;
            foreach (var nodeConnection in AllConnections())
            {
                if (nodeConnection.Configuration != null)
                {
                    curve += nodeConnection.Configuration.Curviness;
                }
                else
                {
                    curve += ConnectionConfiguration.DefaultCurviness;
                }
            }
            return curve/ConnectionCount;
        }

        public IEnumerable<NodeConnection> AllConnections()
        {
            foreach (var nodeConnection in OutConnections)
            {
                if (nodeConnection == null)
                {
                    continue;
                }
                yield return nodeConnection;
            }
            foreach (var nodeConnection in InConnections)
            {
                if (nodeConnection == null)
                {
                    continue;
                }
                yield return nodeConnection;
            }
        }

        public Vector3 NodePosition
        {
            get
            {
                var scaledOffset = new Vector3(Offset.x * transform.localScale.x, Offset.y * transform.localScale.y,
                    Offset.z * transform.localScale.z);
                return (transform.position + transform.rotation * scaledOffset);
            }
            set
            {
                // minus the offset
                var scaledOffset = new Vector3(Offset.x * transform.localScale.x, Offset.y * transform.localScale.y,
                    Offset.z * transform.localScale.z);
                value -= Quaternion.Inverse(transform.rotation) * scaledOffset;
                transform.position = value;
            }
        }

        public Vector3 GetNodeControl(Node relativeNode)
        {
            if (Configuration.IsExplicitControl)
            {
                return (transform.rotation * Configuration.ExplicitControl);
            }

            var dist = float.MaxValue;
            if (relativeNode != null)
            {
                dist = (relativeNode.NodePosition - NodePosition).magnitude / 2;
            }
            var tangent = Vector3.ClampMagnitude(CalculateTangent(relativeNode) * GetCurviness(), dist);
            if (tangent != Vector3.zero)
                return transform.rotation * tangent;
            if(relativeNode != null)
                return transform.rotation * (relativeNode.NodePosition - NodePosition).normalized * GetCurviness();
            Debug.LogError("We shouldn't really be able to get to this point", this);
            return transform.forward;
        }

        public void Destroy()
        {
            for (int i = 0; i < InConnections.Count; i++)
            {
                var nodeConnection = InConnections[i];
                if (nodeConnection)
                {
                    nodeConnection.Destroy();
                }
                if (nodeConnection)
                {
                    DestroyImmediate(nodeConnection);
                }
            }
            for (int i = 0; i < OutConnections.Count; i++)
            {
                var nodeConnection = OutConnections[i];
                if (nodeConnection)
                {
                    nodeConnection.Destroy();
                }
                if (nodeConnection)
                {
                    DestroyImmediate(nodeConnection);
                }
            }
        }

        public Vector3 GetUpVector()
        {
            // Should we be doing this? probably not...
            return transform.up;
        }
        
        /// <summary>
        /// Clean up after ourselves (need to delete in scene mesh objects)
        /// </summary>
        public void OnDestroy()
        {
#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode || UnityEditor.EditorApplication.isPlaying)
            {
                // Go figure, the first can be false and yet the second true (when stopping playing in editor)
                return;
            }

            var go = gameObject;
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (go)
                {
                    var conn = go.GetComponents<NodeConnection>();
                    for (int i = 0; i < conn.Length; i++)
                    {
                        conn[i].Destroy();
                        DestroyImmediate(conn[i]);
                    }
                }
            };
#endif
        }

        public void ResetSnapPosition()
        {
            _lastSnapPosition = Vector3.zero;
        }

        public List<NodeConnection> GetAllConnections()
        {
            var ret = new List<NodeConnection>();
            foreach (var nodeConnection in AllConnections())
            {
                if (!ret.Contains(nodeConnection))
                {
                    ret.Add(nodeConnection);
                }
                if (!ret.Contains(nodeConnection))
                {
                    ret.Add(nodeConnection);
                }
            }
            return ret;
        }

        public void Strip()
        {
            var nodeComponents = GetComponents<NodeComponent>();
            for (int i = 0; i < nodeComponents.Length; i++)
            {
                nodeComponents[i].Strip();
            }
            for (int i = 0; i < OutConnections.Count; i++)
            {
                OutConnections[i].Strip();
            }
            DestroyImmediate(this);
        }

        public override int GetPriority()
        {
            return -1;
        }

        public override void SetPriority(int priority)
        {
            Debug.LogWarning("This is not supported for Node Objects", this);
        }

        public override string GetLayerName()
        {
            return RoadNetwork.LAYER_NAME;
        }

        public override void OnPreBake()
        {
            Snap();
        }

        public override Vector3 Size
        {
            get{
                return new Vector3(0.1f, 1000000, 0.1f);
            }
        }

        public override Type GetLayerType(){
            return typeof(MMTerrainLayer);
        }
    }

    
}