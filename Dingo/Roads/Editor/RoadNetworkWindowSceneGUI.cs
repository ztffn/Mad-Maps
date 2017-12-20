﻿using System.Collections.Generic;
using System.Linq;
using Dingo.Common;
using UnityEditor;
using UnityEngine;

namespace Dingo.Roads
{
    public partial class RoadNetworkWindow
    {
        public const int SplineDrawBudget = 100;
        public static int SplineDrawCalls;
        private static SplineSegment _previewSpline;
        private static Dictionary<int, Node> _nodeHandleMapping = new Dictionary<int, Node>();

        private void Update()
        {
            if (!FocusedRoadNetwork)
            {
                return;
            }
            FocusedRoadNetwork.Update();
        }

        public void OnSceneGUI(SceneView sceneView)
        {
            if (Event.current.control)
            {
                SceneView.RepaintAll();
            }
            SplineDrawCalls = 0;

            wantsMouseMove = true;
            EditorApplication.update -= Update;
            EditorApplication.update += Update;

            if (Event.current.type == EventType.KeyUp && Event.current.keyCode == KeyCode.Tab)
            {
                _nodeIndex++;
                Event.current.Use();
                return;
            }

            if (FocusedRoadNetwork == null)
            {
                return;
            }

            _currentHoverNode = null;

            RaycastHit hit;
            SceneGUICast(out hit);
            DoNodeSelection(ref hit);

            if (_currentTab == 0)
            {
                var myEvent = Event.current;
                var hitPoint = hit.point;
                var hitNormal = hit.normal;
                DoAddNodePreview(myEvent, ref hitPoint);
                DoConnectOrCreateNodes(myEvent, hitPoint, hitNormal);
            }

            // Draw nodes and connections
            const int budget = 50;
            var n =
                FocusedRoadNetwork.Nodes.OrderBy(node => Vector3.SqrMagnitude(node.transform.position - hit.point))
                    .ToList();
            for (var i = 0; i < n.Count && i < budget; i++)
            {
                DrawRoadNodeSceneGUI(sceneView.camera, n[i]);
            }
        }

        private void DoNodeSelection(ref RaycastHit hit)
        {
            _nodeHandleMapping.Clear();
            for (var i = FocusedRoadNetwork.Nodes.Count - 1; i >= 0; i--)
            {
                var node = FocusedRoadNetwork.Nodes[i];
                if (node == null)
                {
                    FocusedRoadNetwork.Nodes.RemoveAt(i);
                    continue;
                }
                var instanceID = node.GetInstanceID();
                _nodeHandleMapping.Add(instanceID, node);
                var dist = HandleUtility.DistanceToCircle(node.NodePosition, FocusedRoadNetwork.NodePreviewSize);
                HandleUtility.AddControl(instanceID, dist);
            }

            var prevHover = _currentHoverNode;
            var handleID = HandleUtility.nearestControl;
            if (_nodeHandleMapping.ContainsKey(handleID))
            {
                _currentHoverNode = _nodeHandleMapping[handleID];
            }
            if (_currentHoverNode != null)
            {
                hit.point = _currentHoverNode.NodePosition;
                hit.normal = _currentHoverNode.transform.up;
            }
            if (prevHover != _currentHoverNode)
            {
                SceneView.RepaintAll();
            }

            var myEvent = Event.current;
            const int selectionMouseNum = 2;
            sBehaviour createdObject = null;
            if (myEvent.type == EventType.MouseDown && myEvent.button == selectionMouseNum && !myEvent.control && _currentHoverNode != null)
            {
                myEvent.Use();

                if (myEvent.shift)
                {
                    var newSelection = GetCurrentlySelectedNodes();
                    newSelection.Add(_currentHoverNode);
                    SetCurrentlySelectedNodes(newSelection);
                }
                else
                {
                    SetCurrentlySelectedNodes(_currentHoverNode);
                }
            }
        }

        private sBehaviour DoConnectOrCreateNodes(Event myEvent, Vector3 hitPoint, Vector3 hitNormal)
        {
            var currentSelection = GetCurrentlySelectedNodes();
            Node currentlySelectedNode = null;
            if (!currentSelection.IsNullOrEmpty() && currentSelection.Count == 1)
            {
                currentlySelectedNode = currentSelection[0];
            }

            const int selectionMouseNum = 2;
            sBehaviour createdObject = null;
            if (myEvent.type == EventType.MouseDown && myEvent.button == selectionMouseNum && myEvent.control)
            {
                if (_currentHoverNode == null)
                {
                    // Create a new node and set the selection to it
                    createdObject = FocusedRoadNetwork.CreateNewNode(hitPoint, Vector3.up, currentlySelectedNode, _currentConfiguration, FocusedRoadNetwork.CurrentNodeConfiguration);
                    SetCurrentlySelectedNodes((Node)createdObject);
                    myEvent.Use();
                    FocusedRoadNetwork.ForceThink();
                }
                else if (_currentHoverNode != null)
                {
                    // Connect two nodes
                    var currentSelectedNode = currentlySelectedNode;
                    if (currentSelectedNode != null)
                    {
                        if (myEvent.button == selectionMouseNum && myEvent.control)
                        {
                            createdObject = FocusedRoadNetwork.ConnectNodes(currentSelectedNode, _currentHoverNode,
                                _currentConfiguration);
                        }
                    }
                    myEvent.Use();
                    SetCurrentlySelectedNodes(_currentHoverNode);
                    FocusedRoadNetwork.ForceThink();
                }
                
            }
            return createdObject;
        }

        private static void DoAddNodePreview(Event myEvent, ref Vector3 hitPoint)
        {
            if (myEvent.control)
            {
                var currentSelection = GetCurrentlySelectedNodes();
                if (currentSelection.Count != 1)
                {
                    return;
                }

                Handles.SphereCap(-1, hitPoint, Quaternion.identity, NodePreviewSize);
                // Draw the spline preview
                var currentNode = currentSelection[0];
                if (currentNode != null)
                {
                    if (_previewSpline == null)
                    {
                        _previewSpline = new SplineSegment();
                    }
                    _previewSpline.FirstControlPoint.Position = currentNode.NodePosition;
                    _previewSpline.SecondControlPoint.Position = hitPoint;
                    _previewSpline.Recalculate();

                    Handles.DrawLine(_previewSpline.FirstControlPoint.Position,
                        _previewSpline.SecondControlPoint.Position);
                    //NodeConnectionGUI.DrawSplineSceneGUI(SceneView.currentDrawingSceneView.camera, _previewSpline, Color.white, Color.white);
                }
            }
        }

        private static void SceneGUICast(out RaycastHit hit)
        {
            var myEvent = Event.current;
            var ray = HandleUtility.GUIPointToWorldRay(myEvent.mousePosition);
            if (!Physics.Raycast(ray, out hit, 1000, ~0))
            {
                var hPlane = new Plane(Vector3.up, new Vector3(0, 0, 0));
                float dist = 0;
                if (!hPlane.Raycast(ray, out dist))
                {
                    return;
                }
                var planePos = ray.origin + ray.direction*dist;
                hit.point = planePos;
            }
        }

        public static bool IsSelected(sBehaviour obj)
        {
            return Selection.objects.Contains(obj) || Selection.objects.Contains(obj.gameObject);
        }

        public static bool IsHovering(sBehaviour obj)
        {
            return obj == _currentHoverNode;
        }

        private static void DrawRoadNodeSceneGUI(Camera currentSceneviewCamera, Node node)
        {
            if (!node || !node.gameObject.activeInHierarchy)
            {
                return;
            }
            NodeGUI.DrawSceneGUI(currentSceneviewCamera, node);
            foreach (var nodeConnection in node.AllConnections())
            {
                NodeConnectionGUI.DrawConnectionSceneGUI(currentSceneviewCamera, nodeConnection);
            }
        }
    }
}