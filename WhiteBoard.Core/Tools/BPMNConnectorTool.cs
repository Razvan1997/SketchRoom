using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using WhiteBoard.Core.Helpers;
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
                // DETECTĂM DACĂ AM DAT CLICK PE ALTĂ LINIE
                var snapped = GetSnappedPoint(pos);
                var toNode = GetNodeAt(snapped);
                var toConnection = GetConnectionAt(snapped);

                if (toConnection != null)
                {
                    // STOP TRASARE + CREAȚIE BULINĂ
                    _pathPoints.Add(snapped);
                    FinalizeConnectionToConnection(snapped, toConnection);
                    return;
                }

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

        private void FinalizeConnectionToConnection(Point snapped, BPMNConnection toConnection)
        {
            foreach (var line in _activeSnapLines)
                _canvas.Children.Remove(line);
            _activeSnapLines.Clear();

            if (_tempPolyline != null)
            {
                _canvas.Children.Remove(_tempPolyline);
                _tempPolyline = null;
            }

            if (_fromNode != null)
            {
                var connection = new BPMNConnection(_fromNode, null, _pathPoints, addArrow: false)
                {
                    ConnectedToConnection = toConnection,
                    ConnectionIntersectionPoint = snapped,
                    CreatedAt = DateTime.Now
                };

                // Adaugă bulina
                var dot = CreateConnectionDot(snapped);
                _canvas.Children.Add(dot);
                connection.ConnectionDot = dot;

                connection.Clicked += (s, e) =>
                {
                    bool ctrl = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
                    OnConnectionClicked((BPMNConnection)s, ctrl);
                };

                _connections.Add(connection);
                _canvas.Children.Add(connection.Visual);
            }

            _pathPoints.Clear();
            _isDrawing = false;
            _fromNode = null;
        }

        private Ellipse CreateConnectionDot(Point pos)
        {
            var dot = new Ellipse
            {
                Width = 10,
                Height = 10,
                Fill = new SolidColorBrush(Color.FromRgb(173, 216, 230)), // light blue
                Stroke = new SolidColorBrush(Color.FromRgb(0, 51, 102)),   // dark blue
                StrokeThickness = 1.5,
                IsHitTestVisible = false
            };

            Canvas.SetLeft(dot, pos.X - dot.Width / 2);
            Canvas.SetTop(dot, pos.Y - dot.Height / 2);
            return dot;
        }

        public void OnMouseMove(Point pos)
        {
            if (_isDrawing && _tempPolyline != null && _pathPoints.Count > 0)
            {
                // Șterge liniile anterioare
                foreach (var line in _activeSnapLines)
                    _canvas.Children.Remove(line);
                _activeSnapLines.Clear();

                var elements = _nodes.Keys
                                .Where(e => IsSnappable(e))
                                .ToList();

                // 👇 Aici forțăm snapping pe ambele axe
                var snapped = _snapService.GetSnappedPointCursor(
                    pos,
                    elements,
                    _tempPolyline,
                    out var newLines,
                    snapX: true,
                    snapY: true);

                foreach (var line in newLines)
                {
                    _canvas.Children.Add(line);
                    _activeSnapLines.Add(line);
                }

                // Adaugă punctul de previzualizare
                var preview = new List<Point>(_pathPoints) { snapped };
                _tempPolyline.Points = new PointCollection(preview);
            }
        }

        private void FinalizeConnection(Point endPoint)
        {
            foreach (var line in _activeSnapLines)
                _canvas.Children.Remove(line);
            _activeSnapLines.Clear();

            var snapped = _snapService.GetSnappedPoint(endPoint, _nodes.Keys, _tempPolyline!, out _);

            var toNode = GetNodeAt(snapped);
            var toConnection = GetConnectionAt(snapped);

            if (_tempPolyline != null)
            {
                _canvas.Children.Remove(_tempPolyline);
                _tempPolyline = null;
            }

            if (_fromNode != null)
            {
                if (!_pathPoints.Last().Equals(snapped))
                    _pathPoints.Add(snapped);

                var connection = new BPMNConnection(_fromNode, toNode, _pathPoints)
                {
                    CreatedAt = DateTime.Now
                };

                if (toConnection != null)
                {
                    connection.ConnectedToConnection = toConnection;
                    connection.ConnectionIntersectionPoint = snapped;
                }

                connection.Clicked += (s, e) =>
                {
                    bool ctrl = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
                    OnConnectionClicked((BPMNConnection)s, ctrl);
                };

                _connections.Add(connection);
                _canvas.Children.Add(connection.Visual);
            }

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
            var ordered = _selectedConnections.OrderByDescending(c => c.CreatedAt).ToList();

            foreach (var conn in ordered)
            {
                _canvas.Children.Remove(conn.Visual);

                if (conn.ConnectionDot != null)
                {
                    _canvas.Children.Remove(conn.ConnectionDot);
                    conn.ConnectionDot = null;
                }

                _connections.Remove(conn);

                // 👇 Eliminăm nodurile din _nodes care au fost legate de conexiune (doar dacă sunt incluse)
                if (conn.From?.Visual is FrameworkElement fromEl)
                    _nodes.Remove(fromEl);
                if (conn.To?.Visual is FrameworkElement toEl)
                    _nodes.Remove(toEl);
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

        public void AddToSelection(BPMNConnection conn)
        {
            if (!_selectedConnections.Contains(conn))
            {
                conn.IsSelected = true;
                _selectedConnections.Add(conn);
            }
        }

        public IEnumerable<BPMNConnection> GetAllConnections() => _connections;

        private bool IsSnappable(UIElement element)
        {
            if (element is FrameworkElement fe && fe.Tag?.ToString() == "NoSnap")
                return false;

            if (element is Thumb || element is Line)
                return false;

            return true;
        }
    }
}
