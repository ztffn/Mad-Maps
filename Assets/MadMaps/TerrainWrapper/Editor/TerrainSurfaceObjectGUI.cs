using MadMaps.Common;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(TerrainSurfaceObject))]
public class TerrainSurfaceObjectGUI : Editor
{
    private bool _editingEnabled;
    private ReorderableList _list;

    public void OnEnable()
    {
        var tso = target as TerrainSurfaceObject;
        _list = new ReorderableList(tso.CastPoints, typeof(TerrainSurfaceObject.CastConfig), false, true, true, true);
        _list.drawElementCallback += DrawElementCallback;
        _list.drawHeaderCallback += DrawHeaderCallback;
    }

    private void DrawHeaderCallback(Rect rect)
    {
        const float labelSize = .75f;
        var labelRect = new Rect(rect.x, rect.y, rect.width * labelSize, rect.height);
        var toggleRect = new Rect(labelRect.xMax, rect.y+1, rect.width * (1 - labelSize), rect.height-4);
        GUI.Label(labelRect, "Cast Points");

        GUI.color = _editingEnabled ? Color.green : Color.white;
        if (GUI.Button(toggleRect, "Edit In Scene", EditorStyles.miniButton))
        {
            _editingEnabled = !_editingEnabled;
            SceneView.RepaintAll();
        }
        GUI.color = Color.white;
    }

    private void DrawElementCallback(Rect rect, int index, bool isActive, bool isFocused)
    {
        var cc = (TerrainSurfaceObject.CastConfig)_list.list[index];
        const float VecSize = .7f;
        const float VecLabelSize = .2f;
        const float DistLabelSize = .4f;

        var vec3Rect = new Rect(rect.x, rect.y, rect.width*VecSize, rect.height);
        var vec3Label = new Rect(vec3Rect.x, vec3Rect.y, vec3Rect.width * VecLabelSize, vec3Rect.height);
        var vec3Field = new Rect(vec3Label.xMax, vec3Rect.y, vec3Rect.width * (1 - VecLabelSize), vec3Rect.height);

        EditorGUI.LabelField(vec3Label, "Position");
        cc.Position = EditorGUI.Vector3Field(vec3Field, GUIContent.none, cc.Position);

        var castDistRect = new Rect(vec3Rect.xMax+4, rect.y, rect.width * (1 - VecSize) -4, rect.height);
        var castDistLabel = new Rect(castDistRect.x, castDistRect.y, castDistRect.width * (DistLabelSize), castDistRect.height);
        var castDistField = new Rect(castDistLabel.xMax, castDistRect.y+2, castDistRect.width * (1 - DistLabelSize), castDistRect.height-4);
        EditorGUI.LabelField(castDistLabel, "Distance");
        cc.AcceptableDistance = EditorGUI.FloatField(castDistField, GUIContent.none, cc.AcceptableDistance);

        _list.list[index] = cc;
    }

    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox("This component can fix floating objects above your terrain, by ensuring they penetrate the ground.", MessageType.Info);
        base.OnInspectorGUI();
        _list.DoLayoutList();
        var tso = target as TerrainSurfaceObject;
        tso.RequireCasts = Mathf.Clamp(tso.RequireCasts, 1, tso.CastPoints.Count);

        if (GUILayout.Button("Recalculate"))
        {
            tso.Recalculate();
        }
    }

    public void OnSceneGUI()
    {
        if (!_editingEnabled)
        {
            return;
        }

        Tools.current = Tool.None;

        var tso = target as TerrainSurfaceObject;
        var rot = tso.transform.rotation;
        var pos = tso.transform.position;
        for (int i = 0; i < tso.CastPoints.Count; i++)
        {
            var castConfig = tso.CastPoints[i];
            var castOffset = castConfig.Position;
            castOffset = Vector3.Scale(castOffset, tso.transform.localScale);
            
            var castPos = pos + rot * castOffset;
            castConfig.Position = Vector3.Scale(Quaternion.Inverse(rot) * (Handles.DoPositionHandle(castPos, rot) - pos), tso.transform.localScale.Inverse());
            Handles.DrawDottedLine(castPos, castPos + Vector3.down * castConfig.AcceptableDistance * tso.transform.lossyScale.y, 1);
            tso.CastPoints[i] = castConfig;
        }
    }
}