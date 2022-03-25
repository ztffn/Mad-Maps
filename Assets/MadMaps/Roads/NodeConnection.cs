using System;
using System.Collections.Generic;
using System.Linq;
using MadMaps.Common.Collections;
using MadMaps.Common;
using MadMaps.Terrains;
using UnityEngine;

namespace MadMaps.Roads
{
    public interface ISplineModifier
    {
        SplineSegment ProcessSpline(SplineSegment s);
    }

    /// <summary>
    /// Represents a connection from this node to another node
    /// </summary>
#if HURTWORLDSDK
    [StripComponentOnBuild]
    [ExecuteInEditMode]
#endif
    public class NodeConnection : LayerComponentBase
    {
        public Node ThisNode;
        public Node NextNode;
        public ConnectionConfiguration Configuration;
        public List<ConnectionComponent> Components = new List<ConnectionComponent>();

        private bool _isStripping;

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
                if (ThisNode == null)
                {
                    return null;
                }
                return ThisNode.Network;
            }
        }

        [Serializable]
        public class IntGameObjectMapping : MadMaps.Common.Collections.CompositionDictionary<int, GameObject> { }

        [SerializeField]
        [HideInInspector]
        private SplineSegment _spline;

        /// <summary>
        /// This is a persistent mapping between an int and a gameObject childed to the owner node, 
        /// which is useful for sharing gameobjects between components.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public GameObject GetDataContainer(int index = 0)
        {
            if (!this)
            {
                Debug.LogError("Attempting to access DataContainer for destroyed object!");
                return null;
            }
            GameObject result;
            if (!__dataContainers.TryGetValue(index, out result) || !result)
            {
                result = new GameObject("DataContainer" + index);
                result.transform.SetParent(transform);
                result.gameObject.layer = gameObject.layer;
                result.transform.localPosition = Vector3.zero;
                result.transform.localRotation = Quaternion.identity;
                result.transform.localScale = Vector3.one;
                __dataContainers[index] = result;
            }
            return result;
        }

        [SerializeField]
        [HideInInspector]
        private IntGameObjectMapping __dataContainers = new IntGameObjectMapping();
        

        public SplineSegment GetSpline(bool recalculate = false, bool applyModifiers = true)
        {
            if (recalculate)
            {
                RecalculateSpline();
            }
            if (applyModifiers)
            {
                foreach (var connectionComponent in Components)
                {
                    var splineMod = connectionComponent as ISplineModifier;
                    if (splineMod != null)
                    {
                        _spline = splineMod.ProcessSpline(_spline);
                    }
                }
            }
            return _spline;
        }

        public void SetData(Node thisNode, Node nextNode, ConnectionConfiguration config)
        {
            ThisNode = thisNode;
            NextNode = nextNode;
            _spline = new SplineSegment();
            Configuration = config;
        }

        public void Think()
        {
            if (ThisNode == null || NextNode == null)
            {
                Destroy();
                DestroyImmediate(this);
                return;
            }

            var allComponents = GetComponentsInChildren<ConnectionComponent>();

            // Recollect
            Components.Clear();
            Components.AddRange(allComponents.Where(component => component.NodeConnection == this || component.NodeConnection == null || component.NodeConnection.Equals(null)));

            RecalculateSpline();
            for (int i = 0; i < Components.Count; i++)
            {
                var connectionComponent = Components[i];
                connectionComponent.Think();
            }
        }

        private void RecalculateSpline()
        {
            var instance = Network;
            if (!instance)
            {
                return;
            }
            _spline.Resolution = instance.SplineResolution;

            _spline.FirstControlPoint.Position = ThisNode.NodePosition;
            _spline.SecondControlPoint.Position = NextNode.NodePosition;

            _spline.FirstControlPoint.Control = ThisNode.GetNodeControl(NextNode);
            _spline.SecondControlPoint.Control = NextNode.GetNodeControl(ThisNode);
            _spline.FirstControlPoint.UpVector = ThisNode.GetUpVector();
            _spline.SecondControlPoint.UpVector = NextNode.GetUpVector();

            _spline.Recalculate();
        }

        public SplineSegment GetSplineCopy()
        {
            if(_spline.IsDirty())
            {
                RecalculateSpline();
            }
            return _spline.JSONClone();
        }

        public Bounds GetSplineBounds()
        {
            if(_spline.IsDirty())
            {
                RecalculateSpline();
            }
            return _spline.GetApproximateBounds();
        }

        public bool IsValid()
        {
            return ThisNode != null && NextNode != null;
        }

        public Vector3 GetNodeDirection()
        {
            if (NextNode == null || NextNode.Equals(null) || ThisNode.Equals(null) || ThisNode == null)
            {
                return Vector3.forward;
            }
            return (NextNode.NodePosition - ThisNode.NodePosition).normalized;
        }

        public void OnDestroy()
        {
#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode || UnityEditor.EditorApplication.isPlaying || _isStripping)
            {
                // Go figure, the first can be false and yet the second true (when stopping playing in editor)
                return;
            }
            Destroy();
#endif
        }

        public void Destroy()
        {
            for (int i = Components.Count - 1; i >= 0; i--)
            {
                if (!Components[i] || Components[i].Equals(null)) // Don't double destroy
                    continue;
                Components[i].Destroy();
                if (Components[i].gameObject != gameObject)
                {
                    DestroyImmediate(Components[i].gameObject);
                }
            }
            Components.Clear();
#if UNITY_EDITOR && HURTWORLDSDK
            if (!LevelBuilder.IsBuilding)
#endif
            {
                DestroyAllDataContainers();
            }
        }

        public void DestroyAllDataContainers()
        {
            var gameObjects = __dataContainers.GetValues();
            for (int i = 0; i < gameObjects.Count; i++)
            {
                DestroyImmediate(gameObjects[i]);
            }
            __dataContainers.Clear();
        }

        public override int GetPriority()
        {
            return 1;
        }

        public override void SetPriority(int priority)
        {
            Debug.LogWarning("This is not supported for Node Objects", this);
        }

        public override string GetLayerName()
        {
            return RoadNetwork.LAYER_NAME;
        }

        public override Type GetLayerType(){
            return typeof(MMTerrainLayer);
        }

        public override Vector3 Size
        {
            get
            {
                return GetSplineBounds().size;
            }
        }

        public override void OnPreBake()
        {
            if (Configuration == null)
            {
                return;
            }

            var existingComponents = new List<ConnectionComponent>(Components);
            Components.Clear();
            for (int i = 0; i < Configuration.Components.Count; i++)
            {
                var componentConfiguration = Configuration.Components[i];
                ConnectionComponent existingComponent = null;
                for (int j = 0; j < existingComponents.Count; j++)
                {
                    var config = existingComponents[j];
                    if (config.Configuration.Configuration == Configuration &&
                        config.Configuration.ComponentGUID == componentConfiguration.GUID)
                    {
                        existingComponent = config;
                        existingComponents.Remove(config);
                        break;
                    }
                }
                if (existingComponent == null)
                {
                    existingComponent = gameObject.AddComponent(componentConfiguration.GetMonoType()) as ConnectionComponent;
                    existingComponent.SetData(this, 
                        new ComponentConfigurationRef(Configuration, componentConfiguration.GUID));
                }
                Components.Add(existingComponent);
            }

            for (int i = 0; i < existingComponents.Count; i++)
            {
                var connectionComponent = existingComponents[i];
                connectionComponent.Destroy();
                DestroyImmediate(connectionComponent);
            }
        }

        public void Strip()
        {
            _isStripping = true;
            for (int i = 0; i < Components.Count; i++)
            {
                 Components[i].Strip();
            }
            DestroyImmediate(this);
        }
    }
}

