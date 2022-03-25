using MadMaps.Common.GenericEditor;
using MadMaps.Roads;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MadMaps.WorldStamps.Authoring
{
    [Serializable]
    public class RoadDataCreator : WorldStampCreatorLayer
    {
        [NonSerialized]
        public List<RoadNetwork> RoadNetworks = new List<RoadNetwork>();

        public bool CreateLightNetwork = true;
        public bool StripNetwork = true;
        [ShowIf("StripNetwork", false)]
        public bool DeleteDataContainers = true;

        public override GUIContent Label
        {
            get { return new GUIContent(string.Format("Roads ({0})", RoadNetworks.Count));}
        }

        protected override bool HasDataPreview
        {
            get { return false; }
        }

        public override void PreviewInDataInspector()
        {
            throw new System.NotImplementedException();
        }

        public override void Clear()
        {
        }

        protected override void CommitInternal(WorldStampData data, WorldStamp stamp)
        {
            StampExitNode.IsCommiting = true;
            for (int k = RoadNetworks.Count- 1; k >= 0; k--)
            {
                var roadNetwork = RoadNetworks[k];
                if (!roadNetwork)
                {
                    RoadNetworks.RemoveAt(k);
                    continue;
                }
                roadNetwork.ForceThink();
                var rn = UnityEngine.Object.Instantiate(roadNetwork);
                rn.transform.SetParent(stamp.transform);
                var go = rn.gameObject;
                for (int i = 0; i < rn.Nodes.Count; i++)
                {
                    var node = rn.Nodes[i];
                    for (int j = 0; j < node.OutConnections.Count; j++)
                    {
                        var nodeConnection = node.OutConnections[j];
                        if (!StripNetwork && DeleteDataContainers)
                        {
                            nodeConnection.DestroyAllDataContainers();
                        }
                    }
                }
                if (StripNetwork)
                {
                    rn.Strip();
                }
                else
                {
                    Common.GameObjectExtensions.DestroyEmptyObjectsInHierarchy(rn.transform);
                }
                if (CreateLightNetwork)
                {
                    if (rn)
                    {
                        UnityEngine.Object.Destroy(rn);
                    }
                    go.gameObject.AddComponent<RoadNetworkProxy>();
                }
            }
            StampExitNode.IsCommiting = false;
        }

        protected override void CaptureInternal(Terrain terrain, Bounds bounds)
        {
            RoadNetworks = new List<RoadNetwork>(UnityEngine.Object.FindObjectsOfType<RoadNetwork>().Where((rn) => bounds.Intersects(rn.GetBounds())));
        }

#if UNITY_EDITOR
        /*protected override void OnExpandedGUI(WorldStampCreator parent)
        {
            StripNetwork = EditorGUILayout.Toggle("Strip Network", StripNetwork);
            if (!StripNetwork)
            {
                DeleteDataContainers = EditorGUILayout.Toggle("Delete DataContainers", DeleteDataContainers);
            }
        }*/

        protected override void PreviewInSceneInternal(WorldStampCreator parent)
        {
            Handles.color = Color.white;
            for (int i = RoadNetworks.Count - 1; i >= 0; i--)
            {
                var roadNetwork = RoadNetworks[i];
                if (!roadNetwork)
                {
                    RoadNetworks.RemoveAt(i);
                    continue;
                }

                roadNetwork.ForceThink();
                for (int j = 0; j < roadNetwork.Nodes.Count; j++)
                {
                    var node = roadNetwork.Nodes[j];
                    if (!node)
                    {
                        continue;
                    }
                    for (int k = 0; k < node.OutConnections.Count; k++)
                    {
                        var nodeConnection = node.OutConnections[k];
                        var thisPos = nodeConnection.ThisNode.NodePosition;
                        var nextPos = nodeConnection.NextNode.NodePosition;
                        /*if (!b.Contains(thisPos) && !b.Contains(nextPos))
                        {
                            continue;
                        }*/
                        Handles.DrawLine(thisPos, nextPos);
                    }
                }
            }
        }
#endif
    }
}