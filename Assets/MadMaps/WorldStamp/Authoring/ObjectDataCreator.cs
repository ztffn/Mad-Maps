using MadMaps.Roads;
using System;
using System.Collections.Generic;
using System.Linq;
using MadMaps.Common;
using UnityEngine;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MadMaps.WorldStamps.Authoring
{
    [Serializable]
    public class ObjectDataCreator : WorldStampCreatorLayer
    {
        public LayerMask Mask = ~0;
        public WorldStamp.EObjectRelativeMode RelativeMode = WorldStamp.EObjectRelativeMode.RelativeToTerrain;

        [NonSerialized]
        public Dictionary<PrefabObjectData, Bounds> BoundsMapping = new Dictionary<PrefabObjectData, Bounds>();
        [NonSerialized]
        public List<PrefabObjectData> Objects = new List<PrefabObjectData>();

        protected override void CaptureInternal(Terrain terrain, Bounds bounds)
        {
#if UNITY_EDITOR
            BoundsMapping.Clear();
            Objects.Clear();

            var expandedBounds = bounds;
            expandedBounds.Expand(Vector3.up * 5000);

            var terrainPos = terrain.GetPosition();

            var allTransforms = Object.FindObjectsOfType<Transform>();
            var done = new HashSet<Transform>();
            allTransforms = allTransforms.OrderBy(transform => TransformExtensions.GetHierarchyDepth(transform)).ToArray();
            HashSet<Transform> ignores = new HashSet<Transform>();
            for (int i = 0; i < allTransforms.Length; i++)
            {
                var transform = allTransforms[i];
                if (done.Contains(transform) || ignores.Contains(transform))
                {
                    continue;
                }
                if (transform.GetComponent<TerrainCollider>())
                {
                    continue;
                }
                if (!expandedBounds.Contains(transform.position))
                {
                    continue;
                }
                var ws = transform.GetComponentInAncestors<WorldStamp>();
                if (ws)
                {
                    //Debug.Log(string.Format("WorldStamp Object Capture : Ignored {0} as it contained a WorldStamp. Recursive WorldStamps are currently not supported.", transform), transform);
                    ignores.Add(transform);
                    var children = ws.transform.GetComponentsInChildren<Transform>(true);
                    foreach (var ignore in children)
                    {
                        ignores.Add(ignore);
                    }
                    continue;
                }
                var rn = transform.GetComponentInAncestors<RoadNetwork>();
                if (rn)
                {
                    ignores.Add(rn.transform);
                    var children = rn.transform.GetComponentsInChildren<Transform>(true);
                    foreach (var ignore in children)
                    {
                        ignores.Add(ignore);
                    }
                    continue;
                }
                var template = transform.GetComponentInAncestors<WorldStampTemplate>();
                if (template)
                {
                    ignores.Add(template.transform);
                    var children = template.transform.GetComponentsInChildren<Transform>(true);
                    foreach (var ignore in children)
                    {
                        ignores.Add(ignore);
                    }
                    continue;
                }

                var go = transform.gameObject;
                if (Mask != (Mask | (1 << go.layer)))
                {
                    continue;
                }

                var prefabRoot = PrefabUtility.GetPrefabObject(go);
                if (prefabRoot == null)
                {
                    //DebugHelper.DrawCube(collider.bounds.center, collider.bounds.extents, Quaternion.identity, Color.red, 30);
                    continue;
                }

                done.Add(transform);
                var subTransforms = transform.GetComponentsInChildren<Transform>();
                foreach (var childTransform in subTransforms)
                {
                    done.Add(childTransform);
                }

#if UNITY_2018_2_OR_NEWER
                var prefabAsset = PrefabUtility.FindPrefabRoot(PrefabUtility.GetCorrespondingObjectFromSource(go) as GameObject);
#else
                var prefabAsset = PrefabUtility.FindPrefabRoot(PrefabUtility.GetPrefabParent(go) as GameObject);
#endif
                var root = PrefabUtility.FindPrefabRoot(go);

                var relativePos = root.transform.position - bounds.min;
                relativePos = new Vector3(relativePos.x / bounds.size.x, 0, relativePos.z / bounds.size.z);
                
                if (RelativeMode == WorldStamp.EObjectRelativeMode.RelativeToTerrain)
                {
                    var terrainHeightAtPoint = terrain.SampleHeight(root.transform.position);
                    relativePos.y = (root.transform.position.y - terrainHeightAtPoint);
                }
                else
                {
                    relativePos.y = root.transform.position.y;
                }
                relativePos.y -= terrainPos.y;
                
                var newData = new PrefabObjectData()
                {
                    AbsoluteHeight = RelativeMode == WorldStamp.EObjectRelativeMode.RelativeToStamp,
                    Prefab = prefabAsset,
                    Position = relativePos,
                    Rotation = root.transform.rotation.eulerAngles,
                    Scale = root.transform.lossyScale,
                    Guid = GUID.Generate().ToString(),
                };

                var c = transform.GetComponentInChildren<Collider>();
                if (c)
                {
                    BoundsMapping[newData] = c.bounds;
                }
                else
                {
                    var r = transform.GetComponentInChildren<Renderer>();
                    if (r)
                    {
                        BoundsMapping[newData] = r.bounds;
                    }
                }

                done.Add(root.transform);
                var doneChildren = root.transform.GetComponentsInChildren<Transform>(true);
                foreach (var item in doneChildren)
                {
                    done.Add(item);
                }
                Objects.Add(newData);
            }
#endif
        }

#if UNITY_EDITOR
        protected override void OnExpandedGUI(WorldStampCreator parent)
        {
            EditorGUI.BeginChangeCheck();
            Mask = LayerMaskFieldUtility.LayerMaskField("Layer Mask", Mask, false);
            RelativeMode = (WorldStamp.EObjectRelativeMode) EditorGUILayout.EnumPopup("Relative Mode", RelativeMode);
            if (EditorGUI.EndChangeCheck())
            {
                NeedsRecapture = true;
            }
        }

        protected override void PreviewInSceneInternal(WorldStampCreator parent)
        {
            Handles.color = Color.white.WithAlpha(.5f);
            for (int i = 0; i < Objects.Count; i++)
            {
                var b = Objects[i];
                Bounds objBounds;
                if (BoundsMapping.TryGetValue(b, out objBounds))
                {
                    Handles.DrawWireCube(objBounds.center, objBounds.size);
                }
            }
        }
#endif

        public override void PreviewInDataInspector()
        {
            #if UNITY_EDITOR
            Dictionary<object, IDataInspectorProvider> data = new Dictionary<object, IDataInspectorProvider>();
            foreach (var obj in Objects)
            {
                if(!data.ContainsKey(obj.Prefab))
                {
                    data[obj.Prefab] = new PositionList();
                }
                (data[obj.Prefab] as PositionList).Add(obj.Position);
            }
            DataInspector.SetData(data.Values.ToList(), data.Keys.ToList(), true);
            #endif
        }

        public override void Clear()
        {
            Objects.Clear();
        }

        protected override void CommitInternal(WorldStampData data, WorldStamp stamp)
        {
            data.Objects.Clear();
            foreach (var prefabObjectData in Objects)
            {
                data.Objects.Add(prefabObjectData.JSONClone());
            }
            stamp.RemoveObjects = data.Objects.Count > 0;            
            stamp.RelativeMode = RelativeMode;
            stamp.NeedsRelativeModeCheck = false;
        }

        public override GUIContent Label
        {
            get { return new GUIContent(string.Format("Objects ({0})", Objects.Count));}
        }

        protected override bool HasDataPreview
        {
            get { return true; }
        }
    }
}