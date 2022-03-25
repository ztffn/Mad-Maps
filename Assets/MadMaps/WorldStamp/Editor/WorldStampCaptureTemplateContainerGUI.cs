using System.Linq;
using MadMaps.Common;
using MadMaps.Common.Painter;
using UnityEditor;
using UnityEngine;
using EditorCellHelper = MadMaps.Common.Painter.EditorCellHelper;

namespace MadMaps.WorldStamps.Authoring
{
    [CustomEditor(typeof(WorldStampTemplate))]
    public class WorldStampCaptureTemplateContainerGUI : Editor
    {
        private bool _isPreviewing;
        private bool _hasSet;

        public override void OnInspectorGUI()
        {
            var wsct = target as WorldStampTemplate;
            if (GUILayout.Button("Set as Capture Settings"))
            {
                var w = EditorWindow.GetWindow<WorldStampCreator>();

                w.Template = wsct.Template.JSONClone();
                if (w.Template.Terrain == null)
                {
                    w.Template.Terrain = Terrain.activeTerrain;
                }

                var newBounds = new Bounds(wsct.transform.position, wsct.Size);
                newBounds = WorldStampCreator.ClampBounds(w.Template.Terrain, newBounds);
                
                w.Template.Bounds = newBounds;
                var mask = w.GetCreator<MaskDataCreator>();
                mask.SetMaskFromArray(w, wsct.Mask);
                mask.LastBounds = newBounds;
            }
            GUI.color = _isPreviewing ? Color.green : Color.white;
            if (GUILayout.Button("Preview In Scene"))
            {
                _isPreviewing = !_isPreviewing;
                _hasSet = false;
            }
            GUI.color = Color.white;
            DrawDefaultInspector();
        }

        void OnSceneGUI()
        {
            if (!_isPreviewing)
            {
                return;
            }
            Common.Painter.EditorCellHelper.TRS = Matrix4x4.identity;
            Common.Painter.EditorCellHelper.SetAlive();
            Common.Painter.EditorCellHelper.CellSize = 10;
            if (_hasSet)
            {
                return;
            }
            _hasSet = true;
            Common.Painter.EditorCellHelper.Clear(false);
            var wsct = target as WorldStampTemplate;
            var min = wsct.transform.position - wsct.Size.Flatten()/2;
            for (var u = 0; u < wsct.Mask.Width; u++)
            {
                var uF = u/(float) (wsct.Mask.Width - 1);
                for (var v = 0; v < wsct.Mask.Height; v++)
                {
                    var vF = v / (float)(wsct.Mask.Height - 1);
                    var val = wsct.Mask[u, v];
                    var pos = min + new Vector3(uF * wsct.Size.x, 0, vF * wsct.Size.z);
                    Common.Painter.EditorCellHelper.AddCell(pos, Color.Lerp(Color.black, Color.clear, val));
                }
            }
            Common.Painter.EditorCellHelper.Invalidate(); 
        }
    }
}