using SketchRoom.Toolkit.Wpf.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using WhiteBoard.Core.Helpers;
using WhiteBoard.Core.Models;
using WhiteBoard.Core.Services.Interfaces;

namespace WhiteBoardModule
{
    public static class HandleSavedElements
    {
        public static void RestoreShapes(
    List<BPMNShapeModelWithPosition> shapes,
    WhiteBoardControl whiteboard,
    IGenericShapeFactory shapeFactory,
    Action<Dictionary<string, BPMNNode>> onAllLoaded)
        {
            var dropService = whiteboard._dropService;
            var nodeMap = new Dictionary<string, BPMNNode>();
            var tempElementMap = new Dictionary<FrameworkElement, string>();
            var visuals = new List<FrameworkElement>();

            foreach (var shape in shapes)
            {
                var shapeControl = shapeFactory.Create(shape.Type.Value);
                var visual = dropService.HandleDropSavedElements(shape, new Point(shape.Left, shape.Top), shapeControl);

                if (visual != null)
                {
                    dropService.PlaceElementOnCanvas(visual, new Point(shape.Left, shape.Top));
                    dropService.RegisterNodeWhenReadyRestore(visual, shape.Id.ToString(), nodeMap);
                    dropService.SetupConnectorButton(visual);
                    visuals.Add(visual);
                }
            }

            // Așteaptă să fie toate Loaded
            int remaining = visuals.Count;

            if (remaining == 0)
            {
                onAllLoaded(nodeMap);
                return;
            }

            foreach (var v in visuals)
            {
                if (v.IsLoaded)
                {
                    remaining--;
                    continue;
                }

                v.Loaded += (_, _) =>
                {
                    if (dropService._nodeMap.TryGetValue(v, out var node) && tempElementMap.TryGetValue(v, out var id))
                    {
                        nodeMap[id] = node;
                    }

                    remaining--;
                    if (remaining == 0)
                        onAllLoaded(nodeMap);
                };
            }

            // Dacă toate erau deja loaded
            if (remaining == 0)
            {
                onAllLoaded(nodeMap);
            }
        }

        public static void RestoreConnections(
            List<BPMNConnectionExportModel> connections,
            WhiteBoardControl whiteboard,
            Dictionary<string, BPMNNode> nodeMap)
        {
            var canvas = whiteboard.DrawingCanvasPublic;
            var drawingPreferences = ContainerLocator.Container.Resolve<IDrawingPreferencesService>();

            foreach (var connModel in connections)
            {
                BPMNNode? from = null;
                BPMNNode? to = null;

                if (connModel.FromId != null)
                    nodeMap.TryGetValue(connModel.FromId, out from);
                if (connModel.ToId != null)
                    nodeMap.TryGetValue(connModel.ToId, out to);

                if (connModel.PathPoints == null || connModel.PathPoints.Count < 2)
                    continue;

                BPMNConnection connection;

                if (connModel.IsCurved && connModel.BezierSegments?.Count > 0)
                {
                    var figure = new PathFigure { StartPoint = connModel.PathPoints[0] };

                    foreach (var bezier in connModel.BezierSegments)
                    {
                        figure.Segments.Add(new BezierSegment(bezier.Point1, bezier.Point2, bezier.Point3, true));
                    }

                    var lastBezier = connModel.BezierSegments.Last();
                    var finalPoint = connModel.PathPoints.Last();

                    bool endsWithExtraLine = finalPoint != lastBezier.Point3;
                    if (endsWithExtraLine)
                    {
                        figure.Segments.Add(new LineSegment(finalPoint, true));
                    }

                    var geometry = new PathGeometry { Figures = { figure } };

                    connection = new BPMNConnection(from, to, geometry)
                    {
                        CreatedAt = connModel.CreatedAt,
                        FromOffset = connModel.FromOffset,
                        ToOffset = connModel.ToOffset,
                        StartDirection = connModel.StartDirection,
                        EndDirection = connModel.EndDirection
                    };

                    connection.From = from;
                    connection.To = to;

                    // 🎯 reconstrucția săgeții corecte
                    if (endsWithExtraLine)
                        connection.SetArrowFromTo(lastBezier.Point3, finalPoint);
                    else
                        connection.SetArrowFromTo(lastBezier.Point2, lastBezier.Point3);
                }
                else
                {
                    // Linie dreaptă
                    connection = new BPMNConnection(from, to, connModel.PathPoints)
                    {
                        CreatedAt = connModel.CreatedAt,
                        FromOffset = connModel.FromOffset,
                        ToOffset = connModel.ToOffset,
                    };

                    connection.From = from;
                    connection.To = to;
                }

                // 🖌️ culoarea
                if (!string.IsNullOrEmpty(connModel.StrokeHex))
                {
                    var color = (Color)ColorConverter.ConvertFromString(connModel.StrokeHex);
                    connection.SetStroke(new SolidColorBrush(color));
                }
                else
                {
                    connection.SetStroke(drawingPreferences.SelectedColor); // fallback
                }

                if (connection.Visual is FrameworkElement fe)
                    fe.Tag = "Connector";

                connection.Clicked += (s, e) =>
                {
                    bool ctrl = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
                    whiteboard.BpmnConnectorToolPublic?.OnConnectionClicked((BPMNConnection)s, ctrl);
                };

                whiteboard._connections.Add(connection);
                canvas.Children.Add(connection.Visual);
            }
        }
    }
}
