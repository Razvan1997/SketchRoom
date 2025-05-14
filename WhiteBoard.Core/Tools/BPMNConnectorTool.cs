using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Xml.Linq;
using WhiteBoard.Core.Helpers;
using WhiteBoard.Core.Models;
using WhiteBoard.Core.Services;
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
        private readonly UndoRedoService _undoRedoService;
        private List<Point> _pathPoints = new();
        private Polyline? _tempPolyline;
        private BPMNNode? _fromNode;
        private DateTime _lastClickTime = DateTime.MinValue;

        private IInteractiveShape? _selectedShape;
        private IDrawingPreferencesService _drawingPreferences;
        private FrameworkElement? _selectedElement;
        private readonly ISnapService _snapService;
        private readonly List<Line> _activeSnapLines = new();
        public IReadOnlyList<BPMNConnection> SelectedConnections => _selectedConnections;
        public string Name => "Connector";

        private bool _isDrawing = false;
        public bool IsDrawing => _isDrawing;

        public BpmnConnectorTool(Canvas canvas, List<BPMNConnection> connections, Dictionary<FrameworkElement, BPMNNode> nodes, UIElement focusTarget,
            IToolManager toolManager, ISnapService snapService, UndoRedoService undoRedoService, IDrawingPreferencesService drawingPreferencesService)
        {
            _canvas = canvas;
            _connections = connections;
            _nodes = nodes;
            _focusTarget = focusTarget;
            _toolManager = toolManager;
            _snapService = snapService;
            _undoRedoService = undoRedoService;
            _drawingPreferences = drawingPreferencesService;
        }

        public void OnMouseDown(Point pos, MouseButtonEventArgs e)
        {
            var element = _canvas.InputHitTest(pos) as FrameworkElement;
            var connection = GetConnectionAt(pos);

            if (connection != null)
            {
                if (_isDrawing)
                {
                    // 🎯 Finalizează conexiunea către linie și adaugă DOT-ul
                    _pathPoints.Add(pos);
                    FinalizeConnectionToConnection(pos, connection);
                    return;
                }
                else
                {
                    // 🖱️ Nu desenăm => doar selecție normală
                    bool ctrl = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
                    OnConnectionClicked(connection, ctrl);
                    return;
                }
            }

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
                var elements = _nodes.Keys.Where(e => IsSnappable(e)).ToList();
                var snappedStart = _snapService.GetSnappedPointCursor(pos, elements, _canvas, out var _, true, true);
                _pathPoints.Add(snappedStart);

                _tempPolyline = new Polyline
                {
                    Stroke = Brushes.DodgerBlue,
                    StrokeThickness = 2,
                    Points = new PointCollection { snappedStart } 
                };
                _canvas.Children.Add(_tempPolyline);
            }
            else
            {
                var elements = _nodes.Keys.Where(e => IsSnappable(e)).ToList();
                var snapped = _snapService.GetSnappedPointCursor(pos, elements, _tempPolyline!, out var _, true, true);

                var toNode = GetNodeAt(snapped);
                var toConnection = GetConnectionAt(snapped);

                if (toConnection != null)
                {
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
                    _pathPoints.Add(snapped);
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
                connection.SetStroke(_drawingPreferences.SelectedColor);
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
                if (connection.Visual is FrameworkElement fe)
                    fe.Tag = "Connector";
                _canvas.Children.Add(connection.Visual);
            }

            _pathPoints.Clear();
            _isDrawing = true;
            _fromNode = null;
            _toolManager.SetNone();
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

        public void OnMouseMove(Point pos, MouseEventArgs e)
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

            var elements = _nodes.Keys.Where(e => IsSnappable(e)).ToList();
            var snapped = _snapService.GetSnappedPointCursor(endPoint, elements, _tempPolyline!, out _, true, true);

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

                Point? fromOffset = null;
                Point? toOffset = null;

                if(_fromNode.Visual is FrameworkElement fromFe)
                {
                    var fromLeft = Canvas.GetLeft(fromFe);
                    var fromTop = Canvas.GetTop(fromFe);
                    var firstPoint = _pathPoints.First();
                    fromOffset = new Point(firstPoint.X - fromLeft, firstPoint.Y - fromTop);
                }

                if (toNode?.Visual is FrameworkElement toFe)
                {
                    var toLeft = Canvas.GetLeft(toFe);
                    var toTop = Canvas.GetTop(toFe);
                    var lastPoint = _pathPoints.Last();
                    toOffset = new Point(lastPoint.X - toLeft, lastPoint.Y - toTop);
                }

                var connection = new BPMNConnection(_fromNode, toNode, _pathPoints)
                {
                    FromOffset = fromOffset,
                    ToOffset = toOffset,
                    CreatedAt = DateTime.Now
                };

                connection.SetStroke(_drawingPreferences.SelectedColor);
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
                if (connection.Visual is FrameworkElement fe)
                    fe.Tag = "Connector";
                _canvas.Children.Add(connection.Visual);
            }

            _pathPoints.Clear();
            _isDrawing = false;
            _fromNode = null;
            _toolManager.SetNone();
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

        public void OnMouseUp(Point pos, MouseButtonEventArgs e) { }

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
            //_isDrawing = true;
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
                //if (conn.From?.Visual is FrameworkElement fromEl)
                //    _nodes.Remove(fromEl);
                //if (conn.To?.Visual is FrameworkElement toEl)
                //    _nodes.Remove(toEl);
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
            if (_isDrawing)
            {
                _isDrawing = false;
                return;
            }
            _focusTarget.Focus();

            if (!ctrl)
                DeselectAllConnections();

            if (_selectedConnections.Contains(conn))
                _selectedConnections.Remove(conn); 
            else
                _selectedConnections.Add(conn);

            foreach (var c in _connections)
                c.IsSelected = _selectedConnections.Contains(c);

            if (_toolManager.ActiveTool?.Name == "Connector")
            {
                _toolManager.SetNone();
            }
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

        public void OnMouseDown(Point position)
        {
            throw new NotImplementedException();
        }
    }
}
