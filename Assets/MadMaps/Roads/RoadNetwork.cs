using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MadMaps.Common;
using MadMaps.Common.Collections;
using MadMaps.Terrains;
using UnityEngine;

namespace MadMaps.Roads
{
#if HURTWORLDSDK
    [StripComponentOnBuild]
#endif
    [ExecuteInEditMode]
    [HelpURL("http://lrtw.net/madmaps/index.php?title=Road_Network")]
    public class RoadNetwork : MonoBehaviour
    {
        public const string LAYER_NAME = "Road Network";
        public static HashSet<Node> ThinkCache = new HashSet<Node>();

        [Serializable]
        public class RoadLayerMapping : MadMaps.Common.Collections.CompositionDictionary<TerrainWrapper, MMTerrainLayer> { }
        
        public float NodePreviewSize = 5;
        public float SplineResolution = 1; 
        //public float Curviness = 60;
        public float BreakAngle = 90;
        public List<Node> Nodes = new List<Node>();
        public List<string> IgnoredTypes = new List<string>();
        public List<GameObject> IntersectionHistory = new List<GameObject>();
        public List<ConnectionConfiguration> RoadConfigHistory = new List<ConnectionConfiguration>();
        public NodeConfiguration CurrentNodeConfiguration = new NodeConfiguration();
        public bool SceneViewEnabled = true;
        public RoadLayerMapping LayerMapping = new RoadLayerMapping();
        private IEnumerator _slowThink;

        public Bounds GetBounds()
        {
            Bounds? b = null;
            if(Nodes.IsNullOrEmpty())
            {
                return new Bounds(transform.position, Vector3.zero);
            }
            for(var i = 0; i < Nodes.Count - 1; i++)
            {
                var n = Nodes[i];
                for(var j = 0; j < n.OutConnections.Count - 1; j++)
                {
                    var sb = n.OutConnections[j].GetSplineBounds();
                    if(b == null)
                    {
                        b = sb;
                    }
                    else
                    {
                        b.Value.Encapsulate(sb);
                    }
                }
            }
            return b.Value;
        }
        
        public Node CreateNewNode(Vector3 position, Vector3 normal, Node previous, ConnectionConfiguration connectionConfig, NodeConfiguration nodeConfig)
        {
            var newNodeGo = new GameObject(string.Format("RoadNode_{0}", Nodes.Count));
            var newNode = newNodeGo.AddComponent<Node>();
            newNodeGo.transform.position = position;
            newNodeGo.transform.rotation = Quaternion.LookRotation(Vector3.forward, normal);
            newNodeGo.transform.SetParent(transform);

            if (previous != null)
            {
                previous.ConnectTo(newNode, connectionConfig);
            }

            newNode.Configuration = nodeConfig.Clone() ?? new NodeConfiguration();
            Nodes.Add(newNode);
            return newNode;
        }

        public void RemoveObject(IEnumerable<sBehaviour> sNodeBehaviours, bool tryReconnect)
        {
            foreach (var obj in sNodeBehaviours)
            {
                RemoveObject(obj, tryReconnect);
            }
            while (Think().MoveNext()) { }
        }

        public void RemoveObject(sBehaviour obj, bool tryReconnect)
        {
            var node = obj as Node;
            if (node != null)
            {
                if (!Nodes.Remove(node))
                {
                    Debug.LogWarning("Removed node but was not in list: " + node.name, node);
                    return;
                }
                
                if (tryReconnect && node.InConnections.Count == 1 && node.OutConnections.Count == 1)
                {
                    // In a simple case of <x>--<y>--<z> where we are removing <y>, we can connect <x> and <z>
                    ConnectNodes(node.InConnections[0].ThisNode, node.OutConnections[0].NextNode, node.InConnections[0].Configuration);
                }

                node.Destroy();
                var go = node.gameObject;
                DestroyImmediate(node);
                if (go.transform.childCount == 0 && go.GetComponents<Component>().Length == 1)
                {
                    DestroyImmediate(go);
                }
            }
            var connection = obj as NodeConnection;
            if (connection != null)
            {
                DisconnectNodes(connection.ThisNode, connection.NextNode);
            }
            //Think();
        }

        public Node GetCloseNeighbour(Vector3 position, float acceptableDistance = 20)
        {
            for (int i = 0; i < Nodes.Count; i++)
            {
                var roadNodePos = Nodes[i].transform.position;
                var dist = Vector3.SqrMagnitude(position - roadNodePos);
                if (dist < acceptableDistance*acceptableDistance)
                {
                    return Nodes[i];
                }
            }
            return null;
        }
        
        public NodeConnection ConnectNodes(Node first, Node second, ConnectionConfiguration config)
        {
            if (first.IsConnectedTo(second) || second.IsConnectedTo(first))
            {
                return null;
            }
            return first.ConnectTo(second, config);
        }

        public void DisconnectNodes(Node first, Node second)
        {
            first.DisconnectFrom(second);
            second.DisconnectFrom(first);
        }
        
        public void ForceThink()
        {
            CollectAllNodes();
            foreach (var node in Nodes)
            {
                node.Snap();
            }
            var enumerator = Think(true);
            enumerator.MoveNext();
        }

        private IEnumerator ThinkLoop()
        {
            while (true)
            {
                yield return null;
                var enumerator = Think();
                while (enumerator.MoveNext())
                {
                    yield return null;
                }
            }
        }
        
        private IEnumerator Think(bool synchronous = false) 
        {
            ThinkCache.Clear();
            // Balance ins and outs
            for (int i = Nodes.Count - 1; i >= 0; i--)
            {
                if (i >= Nodes.Count)
                {
                    continue;
                }
                var roadNode = Nodes[i];
                if (roadNode == null)
                {
                    Nodes.RemoveAt(i);
                    continue;
                }

                roadNode.Think();
                if (!synchronous)
                {
                    yield return null;
                }
            }

            foreach (Transform child in transform)
            {
                var nodes = child.GetComponents<Node>();
                for (int i = 0; i < nodes.Length; i++)
                {
                    var node = nodes[i];
                    if (!Nodes.Contains(node))
                    {
                        Nodes.Add(node);
                    }
                }
            }
        }

        public void SwapConnection(NodeConnection nodeConnection)
        {
            var first = nodeConnection.ThisNode;
            var second = nodeConnection.NextNode;
            var config = nodeConnection.Configuration;
            DisconnectNodes(first, second);
            // WARNING nodeConnection is null at this point
            ConnectNodes(second, first, config);
        }

        public List<T> Collect<T>() where T : class
        {
            var list = new List<T>(gameObject.GetComponentsByInterfaceInChildren<T>());
            var allProxies = FindObjectsOfType<RoadNetworkProxy>().Where(proxy => proxy.Network == this);
            foreach (var roadNetworkProxy in allProxies)
            {
                list.AddRange(roadNetworkProxy.gameObject.GetComponentsByInterfaceInChildren<T>());
            }
            return list;
        } 

        public void RebakeAllNodes()
        {
            ForceThink();
            var terrains = FindObjectsOfType<Terrain>();
            foreach (var terrain in terrains)
            {
                if(!terrain)
                {
                    continue;
                }
                var wrapper = terrain.gameObject.GetOrAddComponent<TerrainWrapper>();
                if(!wrapper)
                {
                    continue;
                }
                LayerComponentApplyManager.ApplyAllLayerComponents(wrapper, LAYER_NAME);
            }                      
        }


        public void CollectAllNodes()
        {
            Nodes.Clear();
            Nodes.AddRange(GetComponentsInChildren<Node>());
            var allProxies = FindObjectsOfType<RoadNetworkProxy>().Where(proxy => proxy.Network == this);
            foreach (var roadNetworkProxy in allProxies)
            {
                Nodes.AddRange(roadNetworkProxy.GetComponentsInChildren<Node>());
            }
        }

        public void MergeNodes(Node baseNode, Node nodeToMerge)
        {
            baseNode.NodePosition = Vector3.Lerp(baseNode.NodePosition, nodeToMerge.NodePosition, 0.5f);

            for (int i = nodeToMerge.InConnections.Count - 1; i >= 0; i--)
            {
                var nodeConnection = nodeToMerge.InConnections[i];
                ConnectNodes(baseNode, nodeConnection.ThisNode, nodeConnection.Configuration);
                DisconnectNodes(nodeToMerge, nodeConnection.NextNode);
            }

            for (int i = nodeToMerge.OutConnections.Count - 1; i >= 0; i--)
            {
                var nodeConnection = nodeToMerge.OutConnections[i];
                ConnectNodes(baseNode, nodeConnection.NextNode, nodeConnection.Configuration);
                DisconnectNodes(nodeToMerge, nodeConnection.ThisNode);
            }

            nodeToMerge.Destroy();

            var nodeGO = nodeToMerge.gameObject;
            DestroyImmediate(nodeToMerge);
            if (nodeGO.GetComponents<Node>().Length == 0)
            {
                DestroyImmediate(nodeGO);
            }
        }
        
        public void Update()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.update -= EditorCoroutineManager.UpdateCoroutines;
            UnityEditor.EditorApplication.update += EditorCoroutineManager.UpdateCoroutines;
#endif
            if (_slowThink == null)
            {
                _slowThink = ThinkLoop();
            }
            _slowThink.MoveNext();
        }

        public MMTerrainLayer GetLayer(TerrainWrapper terrainWrapper, bool createIfMissing = false)
        {
            MMTerrainLayer snapshot;
            if ((!LayerMapping.TryGetValue(terrainWrapper, out snapshot) || snapshot == null) && createIfMissing)
            {
                snapshot = ScriptableObject.CreateInstance<MMTerrainLayer>();
                snapshot.name = "Road Network";
                LayerMapping[terrainWrapper] = snapshot;
                terrainWrapper.Layers.Insert(0, snapshot);
                snapshot.BlendMode = MMTerrainLayer.EMMTerrainLayerBlendMode.Stencil;
            }
            return snapshot;
        }

        public void InsertIntersection(Node targetNode, GameObject intersection, Vector3 extraRotation)
        {
            if (intersection == null || targetNode == null)
            {
                return;
            }
            intersection.transform.position = targetNode.transform.position;
            intersection.transform.rotation = Quaternion.LookRotation(targetNode.CalculateTangent(null).Flatten()) * Quaternion.Euler(extraRotation);
            intersection.transform.SetParent(transform);

            var meta = intersection.GetComponent<IntersectionMetadata>();
            if (meta)
            {
                meta.OnInsert(targetNode);
            }
        }

        public void Strip()
        {
            for (int i = 0; i < Nodes.Count; i++)
            {
                Nodes[i].Strip();
            }
            Common.GameObjectExtensions.OptimizeAndFlattenHierarchy(transform);
            DestroyImmediate(this);
        }
    }
}