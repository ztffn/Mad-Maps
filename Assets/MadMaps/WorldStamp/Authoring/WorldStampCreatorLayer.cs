using MadMaps.Common.GenericEditor;
using MadMaps.Common;
using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MadMaps.WorldStamps.Authoring
{
    [Serializable]
    public abstract class WorldStampCreatorLayer
    {
        [HideInInspector]
        public bool GUIExpanded;
        [HideInInspector]
        public bool Enabled = true;

        public virtual bool NeedsRecapture { get; set; }
        public virtual bool ManuallyRecapturable { get { return true; } }
        public virtual bool CanDisable { get { return true; } }
        public abstract GUIContent Label { get; }
        protected abstract bool HasDataPreview { get; }

        public WorldStampCreatorLayer()
        {
            NeedsRecapture = true;
        }

#if UNITY_EDITOR
        public virtual void DrawGUI(WorldStampCreator parent)
        {
            EditorExtensions.Seperator();

            EditorGUILayout.BeginHorizontal();

            if (CanDisable)
            {
                Enabled = EditorGUILayout.Toggle(new GUIContent(string.Empty, Enabled ? "Enable Capture" : "Disable Capture"), Enabled, GUILayout.Width(18));
            }
            else
            {
                EditorGUILayout.LabelField("", GUILayout.Width(18));
            }
            if (!Enabled || NeedsRecapture)
            {
                if (parent.SceneGUIOwner == this)
                {
                    parent.SceneGUIOwner = null;
                }
            }

            GUIExpanded = EditorGUILayout.Foldout(GUIExpanded, Label);
            //EditorGUILayout.LabelField(NeedsRecapture && Enabled ? "(Needs Recapture)" : string.Empty);

            var previewContent = new GUIContent("Preview");
            previewContent.tooltip = "Preview this data.";
            GUI.color = parent.SceneGUIOwner == this ? Color.green : Color.white;
            GUI.enabled = Enabled && !NeedsRecapture;
            if (GUILayout.Button(previewContent, EditorStyles.miniButton, GUILayout.Width(60), GUILayout.Height(16)))
            {
                if (parent.SceneGUIOwner == this)
                {
                    parent.SceneGUIOwner = null;
                }
                else
                {
                    if (!HasDataPreview)
                    {
                        parent.SceneGUIOwner = parent.SceneGUIOwner == this ? null : this;
                    }
                    else
                    {
                        var menu = new GenericMenu();
                        menu.AddItem(new GUIContent("Preview In Scene"), false, () => parent.SceneGUIOwner = parent.SceneGUIOwner == this ? null : this);
                        menu.AddItem(new GUIContent("Preview In Inspector"), false, PreviewInDataInspector);
                        menu.ShowAsContext();
                    }
                }
            }
            GUI.color = Color.white;

            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            if (GUIExpanded)
            {
                GUI.enabled = Enabled;
                EditorGUI.indentLevel++;
                
                //EditorGUI.BeginChangeCheck();
            
                OnExpandedGUI(parent);

                /*if (EditorGUI.EndChangeCheck())
                {
                    NeedsRecapture = true;
                }*/

                EditorGUI.indentLevel--;
                GUI.enabled = true;
            }
        }

        public void PreviewInScene(WorldStampCreator parent)
        {
            if (parent.Template.Terrain == null || parent.Template.Bounds.size == Vector3.zero || (ManuallyRecapturable && NeedsRecapture))
            {
                return;
            }
            PreviewInSceneInternal(parent);
        }

        protected abstract void PreviewInSceneInternal(WorldStampCreator parent);

        protected virtual void OnExpandedGUI(WorldStampCreator parent)
        {
            GenericEditor.DrawGUI(this);
        }
#endif

        public void Capture(Terrain terrain, Bounds bounds)
        {
            if (!Enabled)
            {
                Clear();
                return;
            }
            if (terrain == null || bounds.size == Vector3.zero)
            {
                return;
            }
            CaptureInternal(terrain, bounds);
            NeedsRecapture = false;
        }

        public void Commit(WorldStampData data, WorldStamp stamp)
        {
            if (!Enabled)
            {
                return;
            }
            CommitInternal(data, stamp);
        }
        
        public abstract void PreviewInDataInspector();
        public abstract void Clear();
        protected abstract void CommitInternal(WorldStampData data, WorldStamp stamp);
        protected abstract void CaptureInternal(Terrain terrain, Bounds bounds);
        public virtual void Dispose()
        {
        }
    }
}