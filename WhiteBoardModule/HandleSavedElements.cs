using SketchRoom.Models.Enums;
using SketchRoom.Toolkit.Wpf.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using WhiteBoard.Core.Helpers;
using WhiteBoard.Core.Models;
using WhiteBoard.Core.Services;
using WhiteBoard.Core.Services.Interfaces;
using WhiteBoard.Core.Tools;

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
                    tempElementMap[visual] = shape.Id.ToString();
                    dropService.PlaceElementOnCanvas(visual, new Point(shape.Left, shape.Top));
                    dropService.RegisterNodeWhenReadyRestore(visual, shape.Id.ToString(), nodeMap);
                    dropService.SetupConnectorButton(visual);
                    visuals.Add(visual);
                }
            }

            int remaining = visuals.Count;

            if (remaining == 0)
            {
                onAllLoaded(nodeMap);
                return;
            }

            foreach (var v in visuals)
            {
                void TryRegister()
                {
                    if (dropService._nodeMap.TryGetValue(v, out var node) && tempElementMap.TryGetValue(v, out var id))
                    {
                        nodeMap[id] = node;
                    }

                    remaining--;
                    if (remaining == 0)
                        onAllLoaded(nodeMap);
                }

                if (v.IsLoaded)
                {
                    TryRegister();
                }
                else
                {
                    v.Loaded += OnLoaded;

                    void OnLoaded(object sender, RoutedEventArgs e)
                    {
                        v.Loaded -= OnLoaded;
                        TryRegister();
                    }
                }
            }
        }

        public static void RestoreConnections(
            List<BPMNConnectionExportModel> connections,
            WhiteBoardControl whiteboard,
            Dictionary<string, BPMNNode> nodeMap)
        {
            var canvas = whiteboard.DrawingCanvasPublic;
            var drawingPreferences = ContainerLocator.Container.Resolve<IDrawingPreferencesService>();
            var contextMenuService = ContainerLocator.Container.Resolve<IContextMenuService>();
            var selectionService = ContainerLocator.Container.Resolve<IShapeSelectionService>();

            var connectionMap = new Dictionary<string, BPMNConnection>();


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
                    bool addArrow = string.IsNullOrEmpty(connModel.ConnectedToConnectionId);
                    connection = new BPMNConnection(from, to, connModel.PathPoints, addArrow)
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
                {
                    fe.Tag = "Connector";
                    fe.ContextMenuOpening += (s, e) => e.Handled = false;
                }
                connection.Clicked += (s, e) =>
                {
                    bool ctrl = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
                    whiteboard.BpmnConnectorToolPublic?.OnConnectionClicked((BPMNConnection)s, ctrl);
                };
                connection.MouseRightClicked += (s, e) =>
                {
                    if (connection.Visual is FrameworkElement fe)
                    {
                        var clickPos = Mouse.GetPosition(canvas); 
                        var contextInfo = new ConnectionContextMenuInfo(connection, clickPos);

                        fe.ContextMenu = contextMenuService.CreateContextMenu(
                            ShapeContextType.BpmnConnection,
                            contextInfo
                        );

                        fe.ContextMenu.PlacementTarget = fe;
                        fe.ContextMenu.Placement = PlacementMode.MousePoint;
                        fe.ContextMenu.IsOpen = true;
                    }
                };
                whiteboard._connections.Add(connection);
                canvas.Children.Add(connection.Visual);

                if (connection.Visual is FrameworkElement connVisual)
                {
                    var id = connModel.ShapeId;

                    if (string.IsNullOrEmpty(id))
                        id = Guid.NewGuid().ToString();

                    ShapeMetadata.SetShapeId(connVisual, id);
                    connectionMap[id] = connection;
                }

                if (connModel.TextAnnotations != null)
                {
                    foreach (var annotation in connModel.TextAnnotations)
                    {
                        HelpersCore.AddTextAlignedToConnection(
                            connection,
                            annotation.Position,
                            +1,
                            selectionService,
                            annotation 
                        );
                    }
                }
            }

            foreach (var connModel in connections)
            {
                if (!string.IsNullOrEmpty(connModel.ConnectedToConnectionId) &&
                    connModel.ConnectionIntersectionPoint.HasValue)
                {
                    // 🧩 găsește conexiunea target
                    if (!connectionMap.TryGetValue(connModel.ConnectedToConnectionId, out var targetConnection))
                        continue;

                    // 🔎 găsește conexiunea curentă (tot din connectionMap)
                    var currentConnection = connectionMap.Values
                        .FirstOrDefault(c => c.CreatedAt == connModel.CreatedAt);

                    if (currentConnection == null)
                        continue;

                    // 🔗 setează conexiunea și punctul de intersecție
                    currentConnection.ConnectedToConnection = targetConnection;
                    currentConnection.ConnectionIntersectionPoint = connModel.ConnectionIntersectionPoint;

                    // ⚪ DOT vizual
                    var dot = new Ellipse
                    {
                        Width = 10,
                        Height = 10,
                        Fill = new SolidColorBrush(Color.FromRgb(173, 216, 230)),
                        Stroke = new SolidColorBrush(Color.FromRgb(0, 51, 102)),
                        StrokeThickness = 1.5,
                        IsHitTestVisible = false
                    };

                    var dotPos = connModel.ConnectionIntersectionPoint.Value;
                    Canvas.SetLeft(dot, dotPos.X - dot.Width / 2);
                    Canvas.SetTop(dot, dotPos.Y - dot.Height / 2);
                    canvas.Children.Add(dot);
                    currentConnection.ConnectionDot = dot;
                }
            }
        }
    }
}
