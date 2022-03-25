using System.Text;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace MadMaps.Terrains
{
    public class TerrainDetailsDrawer
    {
        public ReorderableList List;
        private TerrainWrapper _wrapper;

        public TerrainDetailsDrawer(TerrainWrapper wrapper)
        {
            _wrapper = wrapper;
            List = new ReorderableList(wrapper.DetailPrototypes, typeof(DetailPrototypeWrapper), false, false, true, false);
            List.drawHeaderCallback += DrawLayerHeaderCallback;
            List.drawElementCallback += DrawLayerElementCallback;
            List.elementHeightCallback += LayerElementHeightCallback;
            List.onAddCallback += OnLayerAddCallback;
            List.onRemoveCallback += OnLayerRemoveCallback;
            List.drawFooterCallback += DrawLayerFooterCallback;
            List.onChangedCallback += RefreshDetails;
        }

        private void RefreshDetails(ReorderableList list)
        {
            _wrapper.RefreshDetails();
        }
        
        private void OnLayerRemoveCallback(ReorderableList list)
        {
            _wrapper.DetailPrototypes.RemoveAt(list.index);
            //_wrapper.Dirty = true;
        }

        private void OnLayerAddCallback(ReorderableList list)
        {
            _wrapper.DetailPrototypes.Add(null);
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
            float objFieldWidth = rect.width - previewTexSize - previewTexSize / 2;

            var layerNumberRect = new Rect(rect.x, rect.y, 10, headerHeight);
            EditorGUI.LabelField(layerNumberRect, index.ToString());

            var nameRect = new Rect(layerNumberRect.xMax + 4, rect.y, objFieldWidth, headerHeight);
            var newKey = (DetailPrototypeWrapper)EditorGUI.ObjectField(nameRect, _wrapper.DetailPrototypes[index], typeof(DetailPrototypeWrapper), false);
            if (newKey != _wrapper.DetailPrototypes[index])
            {
                _wrapper.DetailPrototypes[index] = newKey;
                _wrapper.RefreshDetails();
            }

            if (_wrapper.DetailPrototypes[index] == null)
            {
                return;
            }

            var previewRect = new Rect(layerNumberRect.xMax + 4 + objFieldWidth + 4, rect.y, previewTexSize, previewTexSize);
            var tex = _wrapper.DetailPrototypes[index] != null ? _wrapper.DetailPrototypes[index].PrototypeTexture : null;
            GUI.DrawTexture(previewRect, tex);

           
            var infoRect = new Rect(layerNumberRect.xMax + 4, rect.y + headerHeight, objFieldWidth, rect.height - headerHeight);
            bool any = false;
            StringBuilder sb = new StringBuilder("Written to by: ");
            foreach (var MMTerrainLayer in _wrapper.Layers)
            {
                var wrappers = MMTerrainLayer.GetDetailPrototypeWrappers();
                if (wrappers != null && wrappers.Contains(_wrapper.DetailPrototypes[index]))
                {
                    sb.Append(MMTerrainLayer.name);
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
            EditorGUI.LabelField(rect, "Details");
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
                _wrapper.RefreshDetails();
            }
            List.list = _wrapper.DetailPrototypes;
        }

    }
}