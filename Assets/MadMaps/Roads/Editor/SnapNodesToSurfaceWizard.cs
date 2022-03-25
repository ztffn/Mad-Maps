using System.Collections;
using System.Collections.Generic;
using MadMaps.Common;
using UnityEditor;
using UnityEngine;

namespace MadMaps.Roads
{
    public class SnapNodesToSurfaceWizard : ScriptableWizard
    {
        private static EditorPersistantVal<int> _savedMask = new EditorPersistantVal<int>("NodeNetworkSnapNodesToSurfaceWizard_Mask", ~0);
        public LayerMask Mask;
        public bool TerrainOnly;
        public bool Closest = true;
        public bool SetNormals = true;

        public void OnOpened()
        {
            Mask = _savedMask.Value;
        }

        private void OnWizardCreate()
        {
            // Snap all
            /*var nn = RoadNetworkWindow.FocusedRoadNetwork;
            if (nn == null)
            {
                Debug.LogError("Failed to find NodeNetwork in scene!");
                return;
            }

            var allNodes = new List<sBehaviour>();
            for (int i = 0; i < nn.Nodes.Count; i++)
            {
                var node = nn.Nodes[i];
                allNodes.Add(node);
            }

            EditorCoroutineManager.StartEditorCoroutine(Snap(allNodes, Mask));*/
        }

        /*private void OnWizardOtherButton()
        {
            // Snap selection
            var nn = NodeNetwork.LevelInstance;
            if (nn == null)
            {
                Debug.LogError("Failed to find NodeNetwork in scene!");
                return;
            }

            EditorCoroutineManager.StartEditorCoroutine(Snap(selection, Mask));
        }*/

        private IEnumerator Snap(List<sBehaviour> selection, LayerMask mask)
        {
            _savedMask.Value = mask.value;
            for (int i = 0; i < selection.Count; i++)
            {
                var node = selection[i] as Node;
                if (node == null)
                {
                    continue;
                }

                var nodeColliders = new HashSet<Collider>(node.GetComponentsInChildren<Collider>());

                Vector3 castPos = node.transform.position;
                var outPos = castPos;
                var outNormal = node.transform.up;
                var hits = Physics.RaycastAll(castPos + Vector3.up*5000, Vector3.down, 10000, mask);

                float minDist = float.MaxValue;
                
                for (int j = 0; j < hits.Length; j++)
                {
                    var hit = hits[j];

                    if (nodeColliders.Contains(hit.collider))

                    if (TerrainOnly && !(hit.collider is TerrainCollider))
                    {
                        continue;
                    }

                    if (Closest)
                    {
                        var dist = (castPos - hit.point).magnitude;
                        if (dist < minDist)
                        {
                            minDist = dist;
                            outPos = hit.point;
                            if (SetNormals)
                            {
                                outNormal = hit.normal;
                            }
                        }
                    }
                    else
                    {
                        // Just get the first
                        outPos = hit.point;
                        if (SetNormals)
                        {
                            outNormal = hit.normal;
                        }
                        break;
                    }
                }
                node.transform.position = outPos;
                node.transform.up = outNormal;
                yield return null;
            }
        }
    }
}