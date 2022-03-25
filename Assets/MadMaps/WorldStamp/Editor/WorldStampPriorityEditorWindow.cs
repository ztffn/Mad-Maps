using System.Collections.Generic;
using System.Linq;
using MadMaps.Common;
using MadMaps.Terrains;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace MadMaps.WorldStamps
{
    public class WorldStampPriorityEditorWindow : EditorWindow
    {
        private List<LayerComponentApplyManager.LayerComponentMapping> _list;
        private Vector2 _scroll;

        public static bool NeedsResort;

        public TerrainWrapper Context;
        public string Filter = "";

        void OnEnable()
        {
            var wrappers = FindObjectsOfType<TerrainWrapper>();
            if(wrappers.Length > 0)
            {
                Context = wrappers[0];
            }
            Sort();
        }
        
        void Sort()
        {
            if(Context == null)
            {
                if(_list != null)
                {
                    _list.Clear();
                }                
                return;
            }
            _list = LayerComponentApplyManager.SortComponents(Context, Filter);
        }

        void OnSelectionChange()
        {
            if(!Selection.activeGameObject)
            {
                return;
            }
            var wrapper = Selection.activeGameObject.GetComponent<TerrainWrapper>();
            if(wrapper)
            {
                Context = wrapper;
                NeedsResort = true;
            }
        }

        void OnGUI()
        {
            titleContent = new GUIContent("WS Priorities");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical();
            Context = (TerrainWrapper)EditorGUILayout.ObjectField("Terrain Wrapper", Context, typeof(TerrainWrapper), true);
            Filter = EditorGUILayout.TextField("Layer Filter", Filter);
            EditorGUILayout.EndVertical();
            GUI.enabled = Context;
            if(GUILayout.Button(EditorGUIUtility.IconContent("TreeEditor.Refresh"), GUILayout.Width(32), GUILayout.Height(32)))
            {
                NeedsResort = true;
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            if(Context == null)
            {
                EditorGUILayout.HelpBox("Please Select a Terrain Wrapper", MessageType.Info);
                return;
            }

            if (NeedsResort)
            {
                NeedsResort = false;
                Sort();
            }

            if ((_list == null || _list.Count == 0))
            {
                EditorGUILayout.HelpBox("No Eligible Layer Components Found In Scene", MessageType.Info);
                return;
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            foreach(var mapping in _list)
            {
                EditorExtensions.Seperator();
                EditorGUILayout.LabelField(string.Format("[{0}]  Layer '{1}'", mapping.LayerIndex, mapping.LayerName));
                
                foreach(var stamp in mapping.Components)
                {
                    EditorGUILayout.BeginHorizontal();
                    if(GUILayout.Button(string.Format("    {0}", stamp.name), EditorStyles.boldLabel))
                    {
                        Selection.activeGameObject = stamp.gameObject;
                    }
                    EditorGUILayout.LabelField("Priority", GUILayout.Width(50));
                    var currentPriority = stamp.GetPriority();
                    currentPriority = EditorGUILayout.DelayedIntField(currentPriority, GUILayout.Width(100));
                    if(currentPriority != stamp.GetPriority())
                    {
                        stamp.SetPriority(currentPriority);
                        NeedsResort = true;
                    }
                    EditorGUILayout.EndHorizontal();                    
                }
                EditorGUILayout.Space();
            }

            EditorGUILayout.EndScrollView();
        }

    }
}