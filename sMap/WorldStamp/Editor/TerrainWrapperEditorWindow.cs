using UnityEditor;
using UnityEngine;

namespace sMap.Terrains
{
    public class TerrainWrapperEditorWindow : EditorWindow
    {
        public static TerrainWrapperGUI GUI;
        public TerrainWrapper Wrapper;

        void OnGUI()
        {
            if (GUI == null)
            {
                GUI = CreateInstance<TerrainWrapperGUI>();
                GUI.Wrapper = Wrapper ?? (TerrainWrapper)FindObjectOfType(typeof(TerrainWrapper));
                GUI.OnEnable();
            }

            GUI.Wrapper = Wrapper;
            GUI.IsPopout = true;
            titleContent = new GUIContent(Wrapper != null ? Wrapper.name : "none");
            ((Editor) GUI).OnInspectorGUI();
        }
    }
}