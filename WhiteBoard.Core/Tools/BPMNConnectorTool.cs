using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using WhiteBoard.Core.Models;
using WhiteBoard.Core.Services.Interfaces;

namespace WhiteBoard.Core.Tools
{
    public class BpmnConnectorTool : IDrawingTool
    {
        private readonly Canvas _canvas;
        private readonly Dictionary<FrameworkElement, BPMNNode> _nodes;
        private readonly List<BPMNConnection> _connections;
        private readonly List<BPMNConnection> _selectedConnections = new();
        private readonly UIElement _focusTarget;
        private readonly IToolManager _toolManager;

        private List<Point> _pathPoints = new();
        private Polyline? _tempPolyline;
        private BPMNNode? _fromNode;
        private bool _isDrawing = false;
        private DateTime _lastClickTime = DateTime.MinValue;

        private IInteractiveShape? _selectedShape;
        private FrameworkElement? _selectedElement;
        private readonly ISnapService _snapService;
        private readonly List<Line> _activeSnapLines = new();
        public string Name => "Connector";
        public bool IsDrawing => _isDrawing;

        public BpmnConnectorTool(Canvas canvas, List<BPMNConnection> connections, Dictionary<FrameworkElement, BPMNNode> nodes, UIElement focusTarget,
            IToolManager toolManager, ISnapService snapService)
        {
            _canvas = canvas;
            _connections = connections;
            _nodes = nodes;
            _focusTarget = focusTarget;
            _toolManager = toolManager;
            _snapService = snapService;
        }

        public void OnMouseDown(Point pos)
        {
            var now = DateTime.Now;
            if (_isDrawing && (now - _lastClickTime).TotalMilliseconds < 400)
            {
                FinalizeConnection(pos);
                return;
            }

            _lastClickTime = now;

            if (!_isDrawing)
            {
                _isDrawing = true;
                _pathPoints.Clear();

                _fromNode = GetNodeAt(pos);
                _pathPoints.Add(pos);

                _tempPolyline = new Polyline
                {
                    Stroke = Brushes.DodgerBlue,
                    StrokeThickness = 2,
                    Points = new PointCollection { pos }
                };
                _canvas.Children.Add(_tempPolyline);
            }
            else
            {
                var snapped = GetSnappedPoint(pos);
                var toNode = GetNodeAt(snapped);

                if (toNode != null && _fromNode != null && toNode != _fromNode)
                {
                    _pathPoints.Add(snapped);
                    FinalizeConnection(snapped);
                }
                else
                {
                    _pathPoints.Add(pos);
                }
            }
        }

        public void OnMouseMove(Point pos)
        {
            if (_isDrawing && _tempPolyline != null && _pathPoints.Count > 0)
            {
                foreach (var line in _activeSnapLines)
                    _canvas.Children.Remove(line);
                _activeSnapLines.Clear();

                var elements = _nodes.Keys;
                var snapped = _snapService.GetSnappedPoint(pos, elements, _tempPolyline, out var newLines);

                foreach (var line in newLines)
                {
                    _canvas.Children.Add(line);
                    _activeSnapLines.Add(line);
                }

                var preview = new List<Point>(_pathPoints) { snapped };
                _tempPolyline.Points = new PointCollection(preview);
            }
        }

        private void FinalizeConnection(Point endPoint)
        {
            // 1. Curățare linii de snap
            foreach (var line in _activeSnapLines)
                _canvas.Children.Remove(line);
            _activeSnapLines.Clear();

            // 2. Obține punctul snap-uit
            var snapped = _snapService.GetSnappedPoint(endPoint, _nodes.Keys, _tempPolyline, out _);

            // 3. Obține nodul de destinație
            var toNode = GetNodeAt(snapped);

            // 4. Elimină linia temporară
            if (_tempPolyline != null)
            {
                _canvas.Children.Remove(_tempPolyline);
                _tempPolyline = null;
            }

            // 5. Dacă sunt doi noduri valide și diferite, creăm conexiunea
            if (_fromNode != null && toNode != null && _fromNode != toNode)
            {
                if (!_pathPoints.Last().Equals(snapped))
                    _pathPoints.Add(snapped);

                var connection = new BPMNConnection(_fromNode, toNode, _pathPoints);
                connection.Clicked += (s, e) =>
                {
                    bool ctrl = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
                    OnConnectionClicked((BPMNConnection)s, ctrl);
                };

                _connections.Add(connection);
                _canvas.Children.Add(connection.Visual);
            }

            // 6. Resetare stare
            _pathPoints.Clear();
            _isDrawing = false;
            _fromNode = null;
        }

        private BPMNNode? GetNodeAt(Point pos)
        {
            foreach (var el in _canvas.Children.OfType<FrameworkElement>())
            {
                if (_nodes.TryGetValue(el, out var node))
                {
                    var rect = new Rect(Canvas.GetLeft(el), Canvas.GetTop(el), el.ActualWidth, el.ActualHeight);
                    if (rect.Contains(pos)) return node;
                }
            }
            return null;
        }

        private Point GetSnappedPoint(Point pos, double threshold = 15)
        {
            var allSnapPoints = _nodes.SelectMany(kvp => new[]
            {
                new Point(Canvas.GetLeft(kvp.Key) + kvp.Key.ActualWidth / 2, Canvas.GetTop(kvp.Key)),
                new Point(Canvas.GetLeft(kvp.Key) + kvp.Key.ActualWidth, Canvas.GetTop(kvp.Key) + kvp.Key.ActualHeight / 2),
                new Point(Canvas.GetLeft(kvp.Key) + kvp.Key.ActualWidth / 2, Canvas.GetTop(kvp.Key) + kvp.Key.ActualHeight),
                new Point(Canvas.GetLeft(kvp.Key), Canvas.GetTop(kvp.Key) + kvp.Key.ActualHeight / 2)
            });

            return allSnapPoints
                .Select(p => new { Point = p, Distance = (p - pos).Length })
                .Where(x => x.Distance < threshold)
                .OrderBy(x => x.Distance)
                .Select(x => x.Point)
                .FirstOrDefault(pos);
        }

        public void OnMouseUp(Point pos) { }

        public void SetSelected(IInteractiveShape fromShape, string direction)
        {
            var fe = fromShape.Visual as FrameworkElement;
            if (fe == null || !_nodes.TryGetValue(fe, out var node))
                return;

            _fromNode = node;

            var start = GetDirectionPoint(fe, direction);
            _pathPoints.Clear();
            _pathPoints.Add(start);

            _tempPolyline = new Polyline
            {
                Stroke = Brushes.DodgerBlue,
                StrokeThickness = 2,
                Points = new PointCollection { start, start }
            };

            _canvas.Children.Add(_tempPolyline);
            _isDrawing = true;
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

        public void DeselectCurrent()
        {
            if (_selectedShape != null)
            {
                _selectedShape.Deselect();
                _selectedShape = null;
            }

            if (_selectedElement is Control control)
            {
                control.ClearValue(Control.BorderBrushProperty);
                _selectedElement = null;
            }
        }

        public void DeleteSelectedConnections()
        {
            foreach (var conn in _selectedConnections)
            {
                _canvas.Children.Remove(conn.Visual);
                _connections.Remove(conn);
            }
            _selectedConnections.Clear();
        }

        public void DeselectAllConnections()
        {
            foreach (var conn in _selectedConnections)
                conn.IsSelected = false;
            _selectedConnections.Clear();
        }

        public void OnConnectionClicked(BPMNConnection conn, bool ctrl)
        {
            _toolManager.SetActive("Connector");
            _focusTarget.Focus();

            if (!ctrl)
                DeselectAllConnections();

            if (_selectedConnections.Contains(conn))
                _selectedConnections.Remove(conn);
            else
                _selectedConnections.Add(conn);

            foreach (var c in _connections)
                c.IsSelected = _selectedConnections.Contains(c);
        }
    }
}
