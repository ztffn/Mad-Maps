using MadMaps.Terrains;
using System;
using System.Collections.Generic;
using MadMaps.Common;
using UnityEngine;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MadMaps.WorldStamps.Authoring
{
    [Serializable]
    public class TreeDataCreator : WorldStampCreatorLayer
    {
        [NonSerialized]
        public List<MadMapsTreeInstance> Trees = new List<MadMapsTreeInstance>();

        public List<GameObject> Prototypes = new List<GameObject>();
        public List<GameObject> IgnoredTrees = new List<GameObject>();

        public override GUIContent Label
        {
            get { return new GUIContent(string.Format("Trees ({0})", Trees.Count));}
        }

        protected override bool HasDataPreview
        {
            get { return true; }
        }

        protected override void CaptureInternal(Terrain terrain, Bounds bounds)
        {
            Prototypes.Clear();
            Trees.Clear();
            var trees = terrain.terrainData.treeInstances;
            var prototypes = new List<TreePrototype>(terrain.terrainData.treePrototypes);
            var expandedBounds = bounds;
            expandedBounds.Expand(Vector3.up * 5000);
            for (int i = 0; i < trees.Length; i++)
            {
                var tree = trees[i];
                var worldPos = terrain.TreeToWorldPos(tree.position);
                /*if (IgnoredTrees.Contains(prototypes[tree.prototypeIndex].prefab))
                {
                    continue;
                }*/
                if (!expandedBounds.Contains(worldPos))
                {
                    continue;
                }
                var prototype = prototypes[tree.prototypeIndex].prefab;
                if (!Prototypes.Contains(prototype))
                {
                    Prototypes.Add(prototype);
                }
                var hurtTree = new MadMapsTreeInstance(tree, prototypes);
                var yDelta = worldPos.y - terrain.SampleHeight(worldPos);
                hurtTree.Position = new Vector3((worldPos.x - bounds.min.x)/bounds.size.x, yDelta,
                    (worldPos.z - bounds.min.z)/bounds.size.z);
                Trees.Add(hurtTree);
            }
        }

        public override void PreviewInDataInspector()
        {
            #if UNITY_EDITOR
            Dictionary<object, IDataInspectorProvider> data = new Dictionary<object, IDataInspectorProvider>();
            foreach (var obj in Trees)
            {
                if(IgnoredTrees.Contains(obj.Prototype))
                {
                    continue;
                }
                if(!data.ContainsKey(obj.Prototype))
                {
                    data[obj.Prototype] = new PositionList();
                }
                (data[obj.Prototype] as PositionList).Add(obj.Position);
            }
            DataInspector.SetData(data.Values.ToList(), data.Keys.ToList(), true);
            #endif
        }

        public override void Clear()
        {
            Trees.Clear();
        }

        protected override void CommitInternal(WorldStampData data, WorldStamp stamp)
        {
            data.Trees.Clear();
            for (int i = 0; i < Trees.Count; i++)
            {
                var treeInstance = Trees[i];
                if (IgnoredTrees.Contains(treeInstance.Prototype))
                {
                    continue;
                }
                data.Trees.Add(treeInstance.Clone());
            }
            stamp.RemoveTrees = data.Trees.Count > 0;   
        }

#if UNITY_EDITOR
        protected override void PreviewInSceneInternal(WorldStampCreator parent)
        {
            var bounds = parent.Template.Bounds;
            var terrain = parent.Template.Terrain;
            Handles.color = Color.green;
            foreach (var hurtTreeInstance in Trees)
            {
                var pos = new Vector3(hurtTreeInstance.Position.x * bounds.size.x, 0,
                    hurtTreeInstance.Position.z * bounds.size.z) + bounds.min;
                pos.y += terrain.SampleHeight(pos);
                Handles.DrawDottedLine(pos, pos + Vector3.up * 10 * hurtTreeInstance.Scale.x, 1);
            }
        }

        protected override void OnExpandedGUI(WorldStampCreator parent)
        {
            if (Prototypes.Count == 0)
            {
                EditorGUILayout.HelpBox("No Trees Found", MessageType.Info);
                return;
            }
            for (int i = 0; i < Prototypes.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                Prototypes[i] = (GameObject)EditorGUILayout.ObjectField(Prototypes[i],
                    typeof (GameObject), false);
                GUI.color = Prototypes[i] != null && IgnoredTrees.Contains(Prototypes[i])
                    ? Color.red
                    : Color.white;
                GUI.enabled = Prototypes[i] != null;
                if (GUILayout.Button("Ignore", EditorStyles.miniButton, GUILayout.Width(60)))
                {
                    if (IgnoredTrees.Contains(Prototypes[i]))
                    {
                        IgnoredTrees.Remove(Prototypes[i]);
                    }
                    else
                    {
                        IgnoredTrees.Add(Prototypes[i]);
                    }
                }
                GUI.enabled = true;
                GUI.color = Color.white;
                EditorGUILayout.EndHorizontal();
            }
        }
#endif
    }
}