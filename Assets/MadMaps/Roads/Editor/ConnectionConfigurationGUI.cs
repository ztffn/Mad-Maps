using UnityEditor;
using MadMaps.Common.GenericEditor;

namespace MadMaps.Roads
{
    [CustomEditor(typeof(ConnectionConfiguration))]
    public class ConnectionConfigurationGUI : Editor
    {
        public override void OnInspectorGUI()
        {
            GenericEditor.DrawGUI(target);
        }
    }
}