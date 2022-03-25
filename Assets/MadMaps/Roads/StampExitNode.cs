using MadMaps.Common;
using MadMaps.Roads;
using UnityEngine;

namespace MadMaps.WorldStamps.Authoring
{
    [ExecuteInEditMode]
#if HURTWORLDSDK
    [StripComponentOnBuild]
#endif
    public class StampExitNode : NodeComponent
#if HURTWORLDSDK
        , ILevelPreBuildStepCallback
#endif
    {
        public static Texture2D Icon
        {
            get
            {
                if (__icon == null)
                {
                    __icon = Resources.Load<Texture2D>("StampExitNodeIcon");
                }
                return __icon;
            }
        }
        private static Texture2D __icon;

        public static bool IsCommiting;

        [HideInInspector]
        public bool IsDummy = false;

        public override Vector3 Size 
        {
            get
            {
                return new Vector3(0.1f, 1000000, 0.1f);
            }
        }

        [SerializeField] [HideInInspector] private Vector3 _nodeControl;

        public void Update()
        {
            if(!Node)
                return;

            if(Node.ConnectionCount > 1)
            {
                Debug.LogWarning(string.Format("Exit Node {0} has multiple connections! This is not supported.", name), this);
            }

            _nodeControl = Node.GetNodeControl(null);
        }

        public override void Strip()
        {
            if(IsDummy)
            {
                return;
            }
            
            var n = gameObject.AddComponent<Node>();
            n.Configuration.ExplicitControl = _nodeControl;
            n.Configuration.IsExplicitControl = true;
            n.Configuration.SnappingMode = NodeConfiguration.ESnappingMode.None;
            IsDummy = true;
        }

#if UNITY_EDITOR
        public void OnDrawGizmos()
        {
            EditorExtensions.DrawArrow(transform.position, transform.position + _nodeControl, Color.green, 1);
        }
#endif

#if HURTWORLDSDK
        public void OnLevelPreBuildStep()
        {
            var allMeshColliders = transform.GetComponentsInChildren<MeshCollider>();
            for (int i = 0; i < allMeshColliders.Length; i++)
            {
                allMeshColliders[i].enabled = true;
            }
        }
#endif
    }
}