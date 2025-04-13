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
            }
            else
            {
                FinalizeConnection(pos);
            }
        }

        public void OnMouseMove(Point pos, MouseEventArgs e)
        {
            if (_isDrawing && _tempPath != null && _startPoint != null)
            {
                _tempPath.Data = GenerateSmartBezier(_startPoint.Value, pos, _startDirection);
            }
        }

        public void OnMouseUp(Point pos, MouseButtonEventArgs e) { }

        private void FinalizeConnection(Point endPoint)
        {
            if (_tempPath == null || _startPoint == null)
                return;

            _canvas.Children.Remove(_tempPath);

            var toNode = GetNodeAt(endPoint);
            if (_fromNode != null && toNode != null)
            {
                // Găsește elementul vizual asociat nodului
                var toElement = _nodes.FirstOrDefault(x => x.Value == toNode).Key;
                if (toElement == null) return;

                // Detectează din ce parte a fost atinsă forma
                var endDirection = DetectDirectionOnShape(endPoint, toElement);

                var bezierGeometry = GenerateSmartBezier(_startPoint.Value, endPoint, _startDirection, endDirection);

                var connection = new BPMNConnection(_fromNode, toNode, bezierGeometry)
                {
                    CreatedAt = DateTime.Now
                };
                connection.SetStroke(_tempPath.Stroke);
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

            _tempPath.Data = GenerateSmartBezier(_startPoint.Value, _startPoint.Value); // inițial
            _canvas.Children.Add(_tempPath);
            _isDrawing = true;
        }

        private PathGeometry GenerateSmartBezier(Point start, Point end, string? startDirection = null, string? endDirection = null)
        {
            Vector delta = end - start;
            double distance = delta.Length;

            Vector startTangent = GetTangentFromDirection(startDirection, delta);
            Vector endTangent = GetTangentFromDirection(endDirection, -delta);

            double curveFactor = Math.Min(80, distance / 2);

            Point control1 = start + startTangent * curveFactor;
            Point control2 = end + endTangent * curveFactor;

            var segment = new BezierSegment(control1, control2, end, true);
            var figure = new PathFigure { StartPoint = start };
            figure.Segments.Add(segment);

            return new PathGeometry(new[] { figure });
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

        public static string DetectDirectionOnShape(Point mousePos, FrameworkElement shape)
        {
            double left = Canvas.GetLeft(shape);
            double top = Canvas.GetTop(shape);
            double width = shape.ActualWidth;
            double height = shape.ActualHeight;

            double centerX = left + width / 2;
            double centerY = top + height / 2;

            double dx = mousePos.X - centerX;
            double dy = mousePos.Y - centerY;

            if (Math.Abs(dx) > Math.Abs(dy))
                return dx < 0 ? "Left" : "Right";
            else
                return dy < 0 ? "Top" : "Bottom";
        }
    }
}
