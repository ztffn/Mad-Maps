#if UNITY_EDITOR

using System.Linq;
using MadMaps.Common;
using UnityEditor;
using UnityEngine;
using EditorGUILayoutX = MadMaps.Common.EditorGUILayoutX;
using MadMaps.Roads;
using MadMaps.Common.Painter;

namespace MadMaps.WorldStamps.Authoring
{
    public class WorldStampCreator : SceneViewEditorWindow
    {
        [MenuItem("Tools/Mad Maps/World Stamp Creator", false, 6)]
        public static void OpenWindow()
        {
            var w = GetWindow<WorldStampCreator>();
            w.titleContent = new GUIContent("Stamp Creator");
        }

        public bool BoundsLocked;
        public WorldStampCaptureTemplate Template = new WorldStampCaptureTemplate();
        public WorldStampCreatorLayer SceneGUIOwner;

        public WorldStamp TargetInjectionStamp;
        public WorldStampTemplate TargetInjectionTemplate;
        /*
        private GUIContent _createStampTemplateContent = new GUIContent("Create Stamp Template", "Create an in-scene object to preserve stamp capture settings.");
        */

        public static float GetMinGridSize(Bounds bounds, Terrain terrain)
        {
            const float MinMaskRes = 3.34f;
            var hSize = terrain.terrainData.size.x / terrain.terrainData.heightmapResolution;
            while(hSize < MinMaskRes)
            {
                hSize *= 2;
            }
            return hSize;
        }

        void OnSelectionChange()
        {
            if (!Selection.activeGameObject)
            {
                return;
            }
            var t = Selection.activeGameObject.GetComponent<Terrain>();
            if (!t)
            {
                return;
            }
            Template.Terrain = t;
        }

        void OnEnable()
        {
            if (Template.Terrain != null)
            {
                return;
            }
            var currentTerrain = Terrain.activeTerrain;
            if (!currentTerrain)
            {
                return;
            }
            Template.Terrain = currentTerrain;
            Template.Bounds = Template.Terrain.GetBounds();
        }
        
        void OnDisable()
        {
            for (int i = 0; i < Template.Creators.Count; i++)
            {
                Template.Creators[i].Dispose();
            }
        }

        public T GetCreator<T>() where T: WorldStampCreatorLayer
        {
            return Template.Creators.First(layer => layer is T) as T;
        }
        
        protected void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            Template.Terrain = (Terrain)EditorGUILayout.ObjectField("Target Terrain", Template.Terrain, typeof(Terrain), true);
            EditorExtensions.HelpButton("http://lrtw.net/madmaps/index.php?title=World_Stamp_Creator");
            EditorGUILayout.EndHorizontal();
            if (Template.Terrain == null)
            {
                EditorGUILayout.HelpBox("Please Select a Target Terrain", MessageType.Info);
                return;
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Capture Area:", Template.Bounds.size.xz().ToString());
            if (GUILayout.Button(new GUIContent(BoundsLocked ? GUIResources.LockedIcon : GUIResources.UnlockedIcon, "Lock Bounds"), EditorStyles.label,
                GUILayout.Width(24), GUILayout.Height(24)))
            {
                BoundsLocked = !BoundsLocked;
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Layers", EditorStyles.boldLabel);
            var recaptureContent = new GUIContent("Capture");
            recaptureContent.tooltip = "Recapture this data.";
            GUI.color = Template.Creators.Any(layer => layer.NeedsRecapture && layer.Enabled) ? Color.Lerp(Color.red, Color.white, .5f) : Color.white;
            if (GUILayout.Button(recaptureContent, EditorStyles.miniButton, GUILayout.Height(16)))
            {
                for (int i = 0; i < Template.Creators.Count; i++)
                {
                    var worldStampCreatorLayer = Template.Creators[i];
                    worldStampCreatorLayer.Capture(Template.Terrain, Template.Bounds);
                }
                GUIUtility.ExitGUI();
                return;
            }
            GUI.color = Color.white;
            EditorGUILayout.EndHorizontal();
            
            for (int i = 0; i < Template.Creators.Count; i++)
            {
                Template.Creators[i].DrawGUI(this);
            }

            EditorGUILayout.LabelField("", GUILayout.ExpandHeight(true));
            EditorExtensions.Seperator();

            if (GUILayout.Button("Create Stamp Template"))
            {
                CreateTemplate();
                GUIUtility.ExitGUI();
                return;
            }

            GUILayout.BeginHorizontal();
            TargetInjectionTemplate = (WorldStampTemplate) EditorGUILayout.ObjectField(TargetInjectionTemplate,
                typeof (WorldStampTemplate), true);
            GUI.enabled = TargetInjectionTemplate;
            if (GUILayout.Button("Replace Existing Stamp Template", GUILayout.Width(220)))
            {
                CreateTemplate(TargetInjectionTemplate);
                GUIUtility.ExitGUI();
                return;
            }

            GUI.enabled = true;
            GUILayout.EndHorizontal();

            EditorExtensions.Seperator();

            Template.Layer = EditorGUILayout.LayerField("Create Stamp On Layer:", Template.Layer);
            if (GUILayout.Button("Create New Stamp"))
            {
                CreateStamp();
                GUIUtility.ExitGUI();
                return;
            }
            EditorGUILayout.BeginHorizontal();
            TargetInjectionStamp = (WorldStamp) EditorGUILayout.ObjectField(TargetInjectionStamp, typeof (WorldStamp), true);
            GUI.enabled = TargetInjectionStamp;
            if (GUILayout.Button("Replace Existing Stamp", GUILayout.Width(220)))
            {
                CreateStamp(TargetInjectionStamp);
                GUIUtility.ExitGUI();
                return;
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }

        public WorldStamp CreateStamp(WorldStamp stampToReplace = null)
        {
            if (Template.Creators.Any(layer => layer.Enabled && layer.NeedsRecapture) && EditorUtility.DisplayDialog("Layer {0} Needs Recapture",
                    string.Format("We need to recapture the terrain. Do this now?"), "Yes", "No"))
            {
                for (int i = 0; i < Template.Creators.Count; i++)
                {
                    var layer = Template.Creators[i];
                    if (layer.Enabled && layer.NeedsRecapture)
                    {
                        layer.Capture(Template.Terrain, Template.Bounds);
                    }
                }
            }

            bool isWritingToPrefab = false;
            GameObject go  = null;
            if(stampToReplace != null)
            {
                var prefabType = PrefabUtility.GetPrefabType(stampToReplace);
                if(prefabType == PrefabType.Prefab)
                {
                    go = (GameObject)PrefabUtility.InstantiatePrefab(stampToReplace.gameObject);
                    go.DestroyChildren();
                }
                else
                {
                    go = stampToReplace.gameObject;
                }
                isWritingToPrefab = true;
            }
            else
            {
                go = new GameObject("New WorldStamp");
            }
            
            go.transform.position = Template.Bounds.center.xz().x0z(Template.Bounds.min.y) + Vector3.up * GetCreator<HeightmapDataCreator>().ZeroLevel * Template.Terrain.terrainData.size.y;
            var stamp = go.GetOrAddComponent<WorldStamp>();
            var data = new WorldStampData();
            foreach (var layer in Template.Creators)
            {
                if (layer.Enabled)
                {
                    layer.Commit(data, stamp);
                }
            }
            data.Size = Template.Bounds.size;
            
            stamp.SetData(data);
            stamp.HaveHeightsBeenFlipped = true;
            //go.transform.SetLayerRecursive(Template.Layer);

            if(isWritingToPrefab)
            {
                stamp = PrefabUtility.ReplacePrefab(stamp.gameObject, stampToReplace.gameObject).GetComponent<WorldStamp>();
                DestroyImmediate(go);
            }
            
            EditorGUIUtility.PingObject(stamp);
            EditorUtility.SetDirty(stamp);
            return stamp;
        }

        public WorldStampTemplate CreateTemplate(WorldStampTemplate templateToReplace = null)
        {
            if(templateToReplace == null)
            {
                var newTemplate = new GameObject("Stamp Template");
                templateToReplace = newTemplate.AddComponent<WorldStampTemplate>();
            }
                        
            templateToReplace.transform.position = Template.Bounds.center.xz().x0z(Template.Bounds.min.y);
            var mask = GetCreator<MaskDataCreator>();
            templateToReplace.Mask = mask.GetArrayFromMask(this);
            templateToReplace.Template = Template.JSONClone();
            templateToReplace.Size = Template.Bounds.size;
            return templateToReplace;
        }

        protected override void OnSceneGUI(SceneView sceneView)
        {
            if (Template.Terrain == null)
            {
                return;
            }

            if (!BoundsLocked && DoSetArea())
            {
                foreach (var worldStampCreatorLayer in Template.Creators)
                {
                    worldStampCreatorLayer.NeedsRecapture = true;
                }
            }

            if (!Template.Creators.Contains(SceneGUIOwner))
            {
                SceneGUIOwner = null;
            }

            if (SceneGUIOwner != null)
            {
                SceneGUIOwner.PreviewInScene(this);
            }

            var heightmap = GetCreator<HeightmapDataCreator>();
            if(heightmap != null && heightmap.Enabled)
            {
                HandleExtensions.DrawWireCube(Template.Bounds.center.xz().x0z(Template.Bounds.min.y + heightmap.ZeroLevel * Template.Bounds.size.y), 
                    Template.Bounds.size.xz().x0z() / 2, Quaternion.identity, Color.cyan);
            }

            Handles.color = Color.white;
            Handles.DrawWireCube(Template.Bounds.center, Template.Bounds.size);
            Handles.color = Color.white.WithAlpha(.5f);
            Handles.DrawWireCube(Template.Terrain.GetPosition() + Template.Terrain.terrainData.size / 2, Template.Terrain.terrainData.size);
        }

        public static Bounds ClampBounds(Terrain terrain, Bounds b)
        {
            var m = new GridManagerInt(GetMinGridSize(b, terrain));
            var tb = terrain.GetBounds();
            
            b.min = new Vector3(Mathf.Max(tb.min.x, m.GetCellMin(m.GetCell(b.min)).x), 0, Mathf.Max(tb.min.z, m.GetCellMin(m.GetCell(b.min)).y));
            b.max = new Vector3(Mathf.Min(tb.max.x, m.GetCellMin(m.GetCell(b.max)).x), 0, Mathf.Min(tb.max.z, m.GetCellMin(m.GetCell(b.max)).y));

            b.SetMinMax(b.min, b.max);
            
            var newb = new Bounds(b.min.xz().x0z(tb.min.y), Vector3.zero);
            newb.Encapsulate(b.max.xz().x0z(tb.max.y));
            return newb;
        }
        
        private bool DoSetArea()
        {
            var b = Template.Bounds;
            b.min = Handles.DoPositionHandle(b.min, Quaternion.identity);
            b.max = Handles.DoPositionHandle(b.max.xz().x0z(b.min.y), Quaternion.identity);
            
            b = ClampBounds(Template.Terrain, b);
            if (b != Template.Bounds)
            {
                Template.Bounds = b;
                return true;
            }

            return false;
        }
    }
}

#endif