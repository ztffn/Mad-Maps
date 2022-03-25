using UnityEngine;

namespace MadMaps.Roads
{
    public class IntersectionMetadata : MonoBehaviour
    {
        [Header("> One Node Case")]
        public Vector3 OneNodeRotation;
        public Vector3 OneNodeOffset;

        [Header("> Two Node Case")]
        public Vector3 TwoNodeRotation;
        public Vector3 TwoNodeOffset;

        public Quaternion GetRotation(Node insertNode)
        {
            if (insertNode.ConnectionCount == 1)
            {
                return Quaternion.Euler(OneNodeRotation);
            }
            else if (insertNode.ConnectionCount > 1)
            {
                return Quaternion.Euler(TwoNodeRotation);
            }
            else
            {
                return Quaternion.identity;
            }
        }

        public Vector3 GetOffset(Node insertNode)
        {
            if (insertNode.ConnectionCount == 1)
            {
                return OneNodeOffset;
            }
            else if (insertNode.ConnectionCount > 1)
            {
                return TwoNodeOffset;
            }
            else
            {
                return Vector3.zero;
            }
        }

        public void OnInsert(Node insertedNode)
        {
            var insertedNodeConnections = insertedNode.GetAllConnections();
            if (insertedNodeConnections.Count == 0)
            {
                Debug.Log("Simple insertion case on no neighbours.");
                insertedNode.Network.RemoveObject(insertedNode, false);
                return;
            }

            transform.localRotation *= GetRotation(insertedNode);
            transform.localPosition += transform.localRotation * GetOffset(insertedNode);

            var localNodes = GetComponents<Node>();
            if (insertedNodeConnections.Count > localNodes.Length)
            {
                Debug.Log("Complex insertion case (more neighbours than local nodes). Please resolve yourself.");
                return;
            }

            for (int i = insertedNodeConnections.Count-1; i >= 0; i--)
            {
                var connection = insertedNodeConnections[i];
                var neighbour = connection.ThisNode == insertedNode ? connection.NextNode : connection.ThisNode;

                Node closestLocalNode = null;
                float closestLocalNodeDist = float.MaxValue;
                for (int j = 0; j < localNodes.Length; j++)
                {
                    var localNode = localNodes[j];
                    var dist = Vector3.Distance(neighbour.NodePosition, localNode.NodePosition);
                    if (dist < closestLocalNodeDist)
                    {
                        closestLocalNodeDist = dist;
                        closestLocalNode = localNode;
                    }
                }
                if (closestLocalNode != null)
                {
                    insertedNode.Network.ConnectNodes(neighbour, closestLocalNode, connection.Configuration);
                    insertedNodeConnections.RemoveAt(i);
                }
            }
            insertedNode.Destroy();
            DestroyImmediate(insertedNode.gameObject);
        }
    }
}