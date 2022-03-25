using UnityEditor;
using UnityEngine;

namespace MadMaps.Roads
{
    [CustomEditor(typeof (NodeConnection))]
    [CanEditMultipleObjects]
    public class NodeConnectionGUI : Editor
    {
        private void OnSceneGUI()
        {
            DrawConnectionSceneGUI(SceneView.currentDrawingSceneView.camera, target as NodeConnection);
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
        }

        public static void DrawConnectionSceneGUI(Camera sceneviewCamera, NodeConnection connection)
        {
            if (!connection)
            {
                return;
            }
            if (connection.ThisNode == null || connection.NextNode == null)
            {
                return;
            }
            DrawSplineSceneGUI(sceneviewCamera, connection);
        }
        
        public static void DrawSplineSceneGUI(Camera sceneviewCamera, NodeConnection connection)
        {
            var startColor = connection.Configuration != null ? connection.Configuration.PreviewColor : Color.gray;
            /*
            float h, s, v;
            Color.RGBToHSV(startColor, out h, out s, out v);
            var endColor = Color.HSVToRGB(h + .5f, s, v);*/

            if (RoadNetworkWindow.IsSelected(connection) || RoadNetworkWindow.IsSelected(connection.NextNode))
            {
                var spline = connection.GetSpline(true);
                var pointCount = Mathf.Min(spline.Points.Count, 30);
                int lastIndex = 0;
                for (int i = 1; i < pointCount; i++)
                {
                    var percentageThrough = i / (float)pointCount;
                    var index = Mathf.FloorToInt(percentageThrough * spline.Points.Count);

                    //Handles.color = Color.Lerp(startColor, endColor, percentageThrough);
                    Handles.color = startColor;
                    Handles.DrawLine(spline.Points[lastIndex].Position, spline.Points[index].Position);

                    lastIndex = index;
                }
                Handles.color = startColor;
                if(pointCount > 0)
                {
                    Handles.DrawLine(spline.Points[lastIndex].Position, spline.Points[spline.Points.Count-1].Position);
                }
            }
            else
            {
                Handles.color = startColor;
                Handles.DrawLine(connection.ThisNode.NodePosition, connection.NextNode.NodePosition);
            }

        }
    }
}