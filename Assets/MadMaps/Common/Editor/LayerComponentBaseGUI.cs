using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using MadMaps.Terrains;
using MadMaps.WorldStamps;

namespace MadMaps.Common
{
    public class LayerComponentBaseGUI : Editor
    {
        protected void DoGenericUI(SerializedProperty layerName, bool enabled)
        {
            EditorGUILayout.BeginVertical();
            if(layerName != null)
            {
                GUI.enabled = enabled && (!layerName.hasMultipleDifferentValues);
                var content = new GUIContent(string.Format("Recalculate Layer '{0}'", layerName.stringValue), "This will recalculate only this layer.");
                if (GUILayout.Button(content))
                {
                    StampAll(layerName.stringValue);
                    GUIUtility.ExitGUI();
                    return;
                }
            }            
            GUI.enabled = enabled;
            var content2 = new GUIContent("Recalculate All Layers", "This will recalculate all layers on this Terrain.");
            if (GUILayout.Button(content2))
            {
                StampAll();
                GUIUtility.ExitGUI();
                return;
            }
            EditorGUILayout.EndVertical();
        }

         protected void DoGenericUI(List<string> layers, bool enabled)
        {
            EditorGUILayout.BeginVertical();
            if(layers != null)
            {
                foreach(var layerName in layers)
                {
                    var content = new GUIContent(string.Format("Recalculate Layer '{0}'", layerName), "This will recalculate only this layer.");
                    if (GUILayout.Button(content))
                    {
                        StampAll(layerName);
                        GUIUtility.ExitGUI();
                        return;
                    }
                }
            }            
            GUI.enabled = enabled;
            var content2 = new GUIContent("Recalculate All Layers", "This will recalculate all layers on this Terrain.");
            if (GUILayout.Button(content2))
            {
                StampAll();
                GUIUtility.ExitGUI();
                return;
            }
            EditorGUILayout.EndVertical();
        }

        private void StampAll(string layerFilter = null)
        {
            HashSet<TerrainWrapper> wrappers = new HashSet<TerrainWrapper>();
            foreach (var obj in targets)
            {
                var layerComponent = obj as LayerComponentBase;
                var relevantWrappers = layerComponent.GetTerrainWrappers();
                if (relevantWrappers.Count == 0)
                {
                    Debug.LogError("Unable to find any TerrainWrappers to write to. Do you need to add the TerrainWrapper component to your terrain?");
                }
                foreach (var relevantWrapper in relevantWrappers)
                {
                    wrappers.Add(relevantWrapper);
                }
            }
            foreach (var terrainWrapper in wrappers)
            {
                LayerComponentApplyManager.ApplyAllLayerComponents(terrainWrapper, layerFilter);
                //terrainWrapper.ApplyAllLayers();
            }
        }
    }
}