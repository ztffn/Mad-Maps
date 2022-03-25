#if UNITY_EDITOR
using UnityEditor;

namespace MadMaps.Common
{
    public abstract class SceneViewEditorWindow : EditorWindow
    {
        void OnFocus()
        {
            SceneView.onSceneGUIDelegate -= this.OnSceneGUI;
            SceneView.onSceneGUIDelegate += this.OnSceneGUI;
            EditorApplication.update -= Update;
            EditorApplication.update += Update;
        }

        void OnDestroy()
        {
            SceneView.onSceneGUIDelegate -= this.OnSceneGUI;
            EditorApplication.update -= Update;
        }

        protected abstract void OnSceneGUI(SceneView sceneView);

        void Update()
        {
            Repaint();
        }
    }
}
#endif