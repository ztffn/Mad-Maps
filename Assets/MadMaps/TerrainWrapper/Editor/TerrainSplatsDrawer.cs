using System.Text;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace MadMaps.Terrains
{
    public class TerrainSplatsDrawer
    {
        public ReorderableList List;
        private TerrainWrapper _wrapper;

        public TerrainSplatsDrawer(TerrainWrapper wrapper)
        {
            _wrapper = wrapper;
            List = new ReorderableList(wrapper.SplatPrototypes, typeof(SplatPrototypeWrapper), false, false, true, false);
            List.drawHeaderCallback += DrawLayerHeaderCallback;
            List.drawElementCallback += DrawLayerElementCallback;
            List.elementHeightCallback += LayerElementHeightCallback;
            List.onAddCallback += OnLayerAddCallback;
            List.onRemoveCallback += OnLayerRemoveCallback;
            List.drawFooterCallback += DrawLayerFooterCallback;
            //List.onCanRemoveCallback += OnCanRemoveCallback;
            List.onChangedCallback += RefreshSplats;
        }

        private void RefreshSplats(ReorderableList list)
        {
            _wrapper.RefreshSplats();
        }

        /*private bool OnCanRemoveCallback(ReorderableList list)
        {
            var splat = _wrapper.SplatPrototypes[list.index];
            if (splat == null)
            {
                return true;
            }
            foreach (var MMTerrainLayer in _wrapper.Layers)
            {
                if (MMTerrainLayer.SplatData.ContainsKey(splat))
                {
                    return false;
                }
            }
            return true;
        }*/

        private void OnLayerRemoveCallback(ReorderableList list)
        {
            _wrapper.SplatPrototypes.RemoveAt(list.index);
            //_wrapper.Dirty = true;
        }

        private void OnLayerAddCallback(ReorderableList list)
        {
            _wrapper.SplatPrototypes.Add(null);
            //_wrapper.Dirty = true;
        }

        private float LayerElementHeightCallback(int index)
        {
            return 64;
        }

        private void DrawLayerElementCallback(Rect rect, int index, bool isActive, bool isFocused)
        {
            rect.y += 2;
            rect.height -= 2;

            float headerHeight = 18;
            float previewTexSize = 57;
            float objFieldWidth = rect.width - previewTexSize - previewTexSize/2;

            var layerNumberRect = new Rect(rect.x, rect.y, 10, headerHeight);
            EditorGUI.LabelField(layerNumberRect, index.ToString());

            var nameRect = new Rect(layerNumberRect.xMax + 4, rect.y, objFieldWidth, headerHeight);
            var newKey = (SplatPrototypeWrapper)EditorGUI.ObjectField(nameRect, _wrapper.SplatPrototypes[index], typeof(SplatPrototypeWrapper), false);
            if (newKey != _wrapper.SplatPrototypes[index])
            {
                _wrapper.SplatPrototypes[index] = newKey;
                _wrapper.RefreshSplats();
                //_wrapper.Dirty = true;
            }

            if (_wrapper.SplatPrototypes[index] == null)
            {
                return;
            }
            
            var previewRect = new Rect(layerNumberRect.xMax + 4 + objFieldWidth + 4, rect.y, previewTexSize, previewTexSize);
            var tex = _wrapper.SplatPrototypes[index] != null ? _wrapper.SplatPrototypes[index].Texture : null;
            GUI.DrawTexture(previewRect, tex);

            var infoRect = new Rect(layerNumberRect.xMax + 4, rect.y + headerHeight, objFieldWidth, rect.height - headerHeight);
            StringBuilder sb = new StringBuilder("Written to by: ");
            bool any = false;
            foreach (var layer in _wrapper.Layers)
            {
                var wrappers = layer.GetSplatPrototypeWrappers();
                if (wrappers != null && wrappers.Contains(_wrapper.SplatPrototypes[index]))
                {
                    sb.Append(layer.name);
                    sb.Append("    ");
                    any = true;
                }
            }
            if (any)
            {
                var style = EditorStyles.label;
                style.wordWrap = true;
                EditorGUI.LabelField(infoRect, sb.ToString(), style);
            }
        }

        private void DrawLayerHeaderCallback(Rect rect)
        {
            EditorGUI.LabelField(rect, "Splats");
        }

        private void DrawLayerFooterCallback(Rect rect)
        {
            float buttonWidth = 25;
            float xMax = rect.xMax;
            float num = xMax - 58f - buttonWidth * 1;
            rect = new Rect(num, rect.y, xMax - num, rect.height);
            Rect rect2 = new Rect(rect.xMax - 50, rect.y - 3f, 25f, 16f);
            Rect position = new Rect(xMax - 29f, rect.y - 3f, 25f, 16f);
            if (Event.current.type == EventType.Repaint)
            {
                GUIStyle footerBackground = "RL Footer";
                footerBackground.Draw(rect, false, false, false, false);
            }
            if (GUI.Button(rect2, EditorGUIUtility.IconContent("Toolbar Plus", "|Add to list"), "RL FooterButton"))
            {
                List.onAddCallback(List);
                if (List.onChangedCallback != null)
                {
                    List.onChangedCallback(List);
                }
            }
            using (new EditorGUI.DisabledScope(List.index < 0 || List.index >= List.count || (List.onCanRemoveCallback != null && !List.onCanRemoveCallback(List))))
            {
                if (GUI.Button(position, EditorGUIUtility.IconContent("Toolbar Minus", "|Remove selection from list"), "RL FooterButton"))
                {
                    List.onRemoveCallback(List);
                    if (List.onChangedCallback != null)
                    {
                        List.onChangedCallback(List);
                    }
                }
            }
            Rect reapplyRect = new Rect(rect.xMax - 50 - buttonWidth, rect.y - 3f, buttonWidth, 16f);
            if (GUI.Button(reapplyRect, EditorGUIUtility.IconContent("TreeEditor.Refresh", "Refresh All Layers"), "RL FooterButton"))
            {
                _wrapper.RefreshSplats();
            }
            List.list = _wrapper.SplatPrototypes;
        }
        
    }
}