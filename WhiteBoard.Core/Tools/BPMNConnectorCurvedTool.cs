using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;
using WhiteBoard.Core.Models;
using WhiteBoard.Core.Services.Interfaces;
using System.Windows.Shapes;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace WhiteBoard.Core.Tools
{
    public class BpmnConnectorCurvedTool : IDrawingTool
    {
        private readonly Canvas _canvas;
        private readonly List<BPMNConnection> _connections;
        private readonly Dictionary<FrameworkElement, BPMNNode> _nodes;
        private readonly ISnapService _snapService;
        private readonly IToolManager _toolManager;

        private BPMNNode? _fromNode;
        private Path? _tempPath;
        private Point? _startPoint;
        private bool _isDrawing = false;
        private string? _startDirection;

        public bool IsDrawing => _isDrawing;
        public string Name => "ConnectorCurved";

        public BpmnConnectorCurvedTool(
            Canvas canvas,
            List<BPMNConnection> connections,
            Dictionary<FrameworkElement, BPMNNode> nodes,
            UIElement focusTarget,
            IToolManager toolManager,
            ISnapService snapService)
        {
            _canvas = canvas;
            _connections = connections;
            _nodes = nodes;
            _snapService = snapService;
            _toolManager = toolManager;
        }
        private BPMNConnection? GetConnectionAt(Point pos, double threshold = 8)
        {
            foreach (var conn in _connections)
            {
                var geometry = conn.Geometry.GetWidenedPathGeometry(new Pen(Brushes.Black, threshold));
                if (geometry.FillContains(pos))
                    return conn;
            }
            return null;
        }
        public void OnMouseDown(Point pos, MouseButtonEventArgs e)
        {
            if (GetConnectionAt(pos) != null)
                return;

            if (!_isDrawing)
            {
                _fromNode = GetNodeAt(pos);
                if (_fromNode == null) return;

                _startPoint = pos;

                _tempPath = new Path
                {
                    Stroke = Brushes.MediumSlateBlue,
                    StrokeThickness = 2,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round,
                    IsHitTestVisible = false
                };

                _canvas.Children.Add(_tempPath);
                _isDrawing = true;
                return;
            }

            if (_isDrawing)
            {
                var toNode = GetNodeAt(pos);
                if (toNode != null)
                {
                    FinalizeConnection(pos);
                    _isDrawing = false;
                    return;
                }
            }
        }

        public void OnMouseMove(Point pos, MouseEventArgs e)
        {
            if (_isDrawing && _tempPath != null && _startPoint != null)
            {
                _tempPath.Data = GenerateSmartBezierWithInfo(_startPoint.Value, pos, _startDirection).Geometry;
            }
        }

        public void OnMouseUp(Point pos, MouseButtonEventArgs e) { }

        private void FinalizeConnection(Point endPoint, string? endDirectionOverride = null)
        {
            if (_tempPath == null || _startPoint == null)
                return;

            _canvas.Children.Remove(_tempPath);

            PathGeometry geometry;
            string? endDirection = null;
            Point end = endPoint;

            if (_fromNode != null)
            {
                var toNode = GetNodeAt(endPoint);
                if (toNode != null)
                {
                    var toElement = _nodes.FirstOrDefault(x => x.Value == toNode).Key;
                    if (toElement != null)
                    {
                        end = endPoint;
                        endDirection = endDirectionOverride ?? DetectDirectionOnShape(endPoint, toElement);
                    }

                    var resultGeometry = GenerateSmartBezierWithInfo(
                        _startPoint.Value,
                        end,
                        _startDirection,
                        endDirection
                    );

                    var connection = new BPMNConnection(_fromNode, toNode, resultGeometry.Geometry)
                    {
                        CreatedAt = DateTime.Now
                    };
                    connection.SetStroke(_tempPath.Stroke);
                    connection.SetArrowFromTo(resultGeometry.LastLineStart, resultGeometry.LastLineEnd);
                    _connections.Add(connection);
                    _canvas.Children.Add(connection.Visual);
                }
            }

            _isDrawing = false;
            _startPoint = null;
            _tempPath = null;
            _fromNode = null;
            _startDirection = null;
        }

        private void FinalizeConnectionMagnetismToDestination(Point endPoint)
        {
            if (_tempPath == null || _startPoint == null)
                return;

            _canvas.Children.Remove(_tempPath);

            var toNode = GetNodeAt(endPoint);
            if (_fromNode != null && toNode != null)
            {
                var toElement = _nodes.FirstOrDefault(x => x.Value == toNode).Key;
                if (toElement == null) return;

                var endDirection = DetectDirectionOnShape(endPoint, toElement);

                // 📌 corectăm endpointul să fie exact pe marginea formei
                var adjustedEndPoint = GetDirectionPoint(toElement, endDirection);

                var result = GenerateSmartBezierWithInfo(
                    _startPoint.Value,
                    adjustedEndPoint,
                    _startDirection,
                    endDirection
                );

                var connection = new BPMNConnection(_fromNode, toNode, result.Geometry)
                {
                    CreatedAt = DateTime.Now
                };
                connection.SetStroke(_tempPath.Stroke);
                connection.SetArrowFromTo(result.LastLineStart, result.LastLineEnd);
                _connections.Add(connection);
                _canvas.Children.Add(connection.Visual);
            }

            _isDrawing = false;
            _startPoint = null;
            _tempPath = null;
            _fromNode = null;
            _startDirection = null;
        }

        public void SetSelected(IInteractiveShape fromShape, string direction)
        {
            if (fromShape.Visual is not FrameworkElement fe || !_nodes.TryGetValue(fe, out var node))
                return;

            _fromNode = node;
            _startDirection = direction;
            _startPoint = GetDirectionPoint(fe, direction);

            _tempPath = new Path
            {
                Stroke = Brushes.MediumSlateBlue,
                StrokeThickness = 2,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                IsHitTestVisible = false
            };

            _tempPath.Data = GenerateSmartBezierWithInfo(_startPoint.Value, _startPoint.Value).Geometry; // ✅ corect
            _canvas.Children.Add(_tempPath);
            _isDrawing = true;
        }

        public void SetSelected(IInteractiveShape fromShape, string direction, UIElement? sourceElement = null, Point? startPosOverride = null)
        {
            if (fromShape.Visual is not FrameworkElement fe || !_nodes.TryGetValue(fe, out var node))
                return;

            _fromNode = node;
            _startDirection = direction;

            _startPoint = startPosOverride ??
                          (sourceElement != null
                              ? GetCenterOfElement(sourceElement)
                              : GetDirectionPoint(fe, direction));

            _tempPath = new Path
            {
                Stroke = Brushes.MediumSlateBlue,
                StrokeThickness = 2,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                IsHitTestVisible = false
            };

            _tempPath.Data = GenerateSmartBezierWithInfo(_startPoint.Value, _startPoint.Value).Geometry;
            _canvas.Children.Add(_tempPath);
            _isDrawing = true;
        }

        private Point GetCenterOfElement(UIElement element)
        {
            var transform = element.TransformToAncestor(_canvas);
            var topLeft = transform.Transform(new Point(0, 0));

            if (element is FrameworkElement fe)
            {
                return new Point(
                    topLeft.X + fe.ActualWidth / 2,
                    topLeft.Y + fe.ActualHeight / 2
                );
            }

            return topLeft;
        }

        private CurvedPathResult GenerateSmartBezierWithInfo(Point start, Point end, string? startDirection = null, string? endDirection = null)
        {
            Vector delta = end - start;
            double totalDistance = delta.Length;

            Vector startTangent = GetTangentFromDirection(startDirection, delta);
            Vector endTangent = GetTangentFromDirection(endDirection, -delta);

            double straightLength = Math.Min(25, totalDistance / 5);
            double curveFactor = Math.Min(70, totalDistance / 2);

            Point p1 = start + startTangent * straightLength;
            Point p4 = end + endTangent * straightLength;

            Point control1 = p1 + startTangent * curveFactor;
            Point control2 = p4 + endTangent * curveFactor;

            var figure = new PathFigure { StartPoint = start };
            figure.Segments.Add(new LineSegment(p1, true));
            figure.Segments.Add(new BezierSegment(control1, control2, p4, true));
            figure.Segments.Add(new LineSegment(end, true));

            return new CurvedPathResult
            {
                Geometry = new PathGeometry(new[] { figure }),
                LastLineStart = p4,
                LastLineEnd = end
            };
        }

        private Vector GetTangentFromDirection(string? direction, Vector fallback)
        {
            return direction switch
            {
                "Top" => new Vector(0, -1),
                "Right" => new Vector(1, 0),
                "Bottom" => new Vector(0, 1),
                "Left" => new Vector(-1, 0),
                _ => fallback.Length > 0 ? fallback / fallback.Length : new Vector(1, 0)
            };
        }

        private BPMNNode? GetNodeAt(Point pos)
        {
            foreach (var (el, node) in _nodes)
            {
                var rect = new Rect(Canvas.GetLeft(el), Canvas.GetTop(el), el.ActualWidth, el.ActualHeight);
                if (rect.Contains(pos))
                    return node;
            }
            return null;
        }

        private Point GetDirectionPoint(FrameworkElement el, string direction)
        {
            double left = Canvas.GetLeft(el);
            double top = Canvas.GetTop(el);
            double width = el.ActualWidth;
            double height = el.ActualHeight;

            return direction switch
            {
                "Top" => new Point(left + width / 2, top),
                "Right" => new Point(left + width, top + height / 2),
                "Bottom" => new Point(left + width / 2, top + height),
                "Left" => new Point(left, top + height / 2),
                _ => new Point(left + width / 2, top + height / 2)
            };
        }

        public static string DetectDirectionOnShape(Point mousePos, FrameworkElement shape, double margin = 15)
        {
            double left = Canvas.GetLeft(shape);
            double top = Canvas.GetTop(shape);
            double width = shape.ActualWidth;
            double height = shape.ActualHeight;

            double right = left + width;
            double bottom = top + height;

            double centerX = left + width / 2;
            double centerY = top + height / 2;

            // 👉 Detectăm dacă suntem aproape de margine
            if (mousePos.X >= left && mousePos.X <= left + margin)
                return "Left";
            if (mousePos.X <= right && mousePos.X >= right - margin)
                return "Right";
            if (mousePos.Y >= top && mousePos.Y <= top + margin)
                return "Top";
            if (mousePos.Y <= bottom && mousePos.Y >= bottom - margin)
                return "Bottom";

            // Dacă nu e aproape de margine, decidem pe baza unghiului față de centru
            double dx = mousePos.X - centerX;
            double dy = mousePos.Y - centerY;

            return Math.Abs(dx) > Math.Abs(dy)
                ? (dx < 0 ? "Left" : "Right")
                : (dy < 0 ? "Top" : "Bottom");
        }

        public void InvokePrivateFinalizeConnection(Point endPoint, string? direction = null)
        {
            FinalizeConnection(endPoint, direction);
        }

        public void OnMouseDown(Point position)
        {
            throw new NotImplementedException();
        }
    }
}
