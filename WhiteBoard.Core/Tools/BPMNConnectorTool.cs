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
        private IInteractiveShape? _selectedShape;
        private FrameworkElement? _selectedElement;
        private readonly Canvas _canvas;
        private readonly Dictionary<FrameworkElement, BPMNNode> _nodes;
        private BPMNNode? _from;
        private BPMNNode? _to;
        private readonly List<BPMNConnection> _connections;
        private string? _startDirection;
        private bool _isDrawingConnection = false;
        private Point _startPoint;
        private Polyline? _tempPolyline;
        private Line? _tempLine;
        private Point _currentMouse;
        private readonly List<BPMNConnection> _selectedConnections = new();
        private readonly UIElement _focusTarget;
        private readonly IToolManager _toolManager;

        public string Name => "Connector";

        public BpmnConnectorTool(Canvas canvas, List<BPMNConnection> connections, Dictionary<FrameworkElement, BPMNNode> nodes, UIElement focusTarget, IToolManager toolManager)
        {
            _canvas = canvas;
            _connections = connections;
            _nodes = nodes;
            _focusTarget = focusTarget;
            _toolManager = toolManager;
        }

        public void OnMouseDown(Point pos)
        {
            foreach (var el in _canvas.Children.OfType<FrameworkElement>())
            {
                if (_nodes.TryGetValue(el, out var node))
                {
                    var bounds = new Rect(Canvas.GetLeft(el), Canvas.GetTop(el), el.ActualWidth, el.ActualHeight);
                    if (bounds.Contains(pos))
                    {
                        _from = node;
                        _selectedElement = el;

                        if (el is IInteractiveShape interactive)
                        {
                            _selectedShape = interactive;
                            _selectedShape.Select(); // Afișează thumbs + buton
                        }

                        Highlight(el);
                        return;


                    }
                }
            }
        }

        public void OnMouseMove(Point pos)
        {
            if (_isDrawingConnection && _tempPolyline != null)
            {
                var snapped = GetSnappedPoint(pos);

                _tempPolyline.Points.Clear();
                _tempPolyline.Points.Add(_startPoint);

                switch (_startDirection)
                {
                    case "Top":
                    case "Bottom":
                        _tempPolyline.Points.Add(new Point(_startPoint.X, snapped.Y));
                        break;

                    case "Left":
                    case "Right":
                        _tempPolyline.Points.Add(new Point(snapped.X, _startPoint.Y));
                        break;
                }

                _tempPolyline.Points.Add(snapped);
            }
        }
        public void OnMouseUp(Point pos)
        {
            if (!_isDrawingConnection || _from == null || _tempPolyline == null)
                return;

            foreach (var el in _canvas.Children.OfType<FrameworkElement>())
            {
                if (_nodes.TryGetValue(el, out var toNode))
                {
                    var bounds = new Rect(Canvas.GetLeft(el), Canvas.GetTop(el), el.ActualWidth, el.ActualHeight);
                    if (bounds.Contains(pos) && toNode != _from)
                    {
                        var connection = new BPMNConnection(_from, toNode, _tempPolyline.Points);

                        connection.Clicked += (s, e) =>
                        {
                            bool isCtrl = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
                            OnConnectionClicked((BPMNConnection)s, isCtrl);
                        };

                        _canvas.Children.Add(connection.Visual);
                        _connections.Add(connection);
                        break;
                    }
                }
            }

            _canvas.Children.Remove(_tempPolyline);
            _tempPolyline = null;
            _isDrawingConnection = false;
            _from = null;
        }

        private void Highlight(UIElement element)
        {
            if (element is Control control)
                control.BorderBrush = System.Windows.Media.Brushes.Orange;
        }

        private void ClearHighlights()
        {
            foreach (var kvp in _nodes)
            {
                if (kvp.Key is Control control)
                    control.ClearValue(Control.BorderBrushProperty);
            }
        }

        public void DeselectCurrent()
        {
            if (_selectedShape != null)
            {
                _selectedShape.Deselect(); // Ascunde thumbs și buton
                _selectedShape = null;
            }

            if (_selectedElement is Control control)
            {
                control.ClearValue(Control.BorderBrushProperty);
                _selectedElement = null;
            }

            _from = null;
            _to = null;

            if (_tempLine != null)
            {
                _canvas.Children.Remove(_tempLine);
                _tempLine = null;
            }
        }

        public void SetSelected(IInteractiveShape fromShape, string direction)
        {
            var fe = fromShape.Visual as FrameworkElement;
            if (fe == null || !_nodes.TryGetValue(fe, out var node))
                return;

            _from = node;
            _startDirection = direction;

            var start = GetDirectionPoint(fe, direction);
            _startPoint = start;

            _isDrawingConnection = true;

            _tempPolyline = new Polyline
            {
                Stroke = Brushes.DodgerBlue,
                StrokeThickness = 2,
                Points = new PointCollection { start, start } // inițial
            };

            _canvas.Children.Add(_tempPolyline);
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

        private List<(FrameworkElement Element, Point SnapPoint)> GetAllSnapPoints()
        {
            var snapPoints = new List<(FrameworkElement, Point)>();

            foreach (var el in _canvas.Children.OfType<FrameworkElement>())
            {
                if (_nodes.ContainsKey(el))
                {
                    double left = Canvas.GetLeft(el);
                    double top = Canvas.GetTop(el);
                    double width = el.ActualWidth;
                    double height = el.ActualHeight;

                    // Top, Right, Bottom, Left
                    snapPoints.Add((el, new Point(left + width / 2, top)));                  // Top
                    snapPoints.Add((el, new Point(left + width, top + height / 2)));         // Right
                    snapPoints.Add((el, new Point(left + width / 2, top + height)));         // Bottom
                    snapPoints.Add((el, new Point(left, top + height / 2)));                 // Left
                }
            }

            return snapPoints;
        }

        private Point GetSnappedPoint(Point current, double threshold = 15)
        {
            var snapPoints = GetAllSnapPoints();

            var closest = snapPoints
                .Select(sp => new { sp.Element, sp.SnapPoint, Distance = (sp.SnapPoint - current).Length })
                .Where(x => x.Distance < threshold)
                .OrderBy(x => x.Distance)
                .FirstOrDefault();

            return closest?.SnapPoint ?? current;
        }

        public void OnConnectionClicked(BPMNConnection conn, bool isCtrlPressed)
        {
            _toolManager.SetActive("Connector");
            _focusTarget.Focus();

            if (!isCtrlPressed)
                ClearSelectedConnections();

            if (_selectedConnections.Contains(conn))
                _selectedConnections.Remove(conn);
            else
                _selectedConnections.Add(conn);

            conn.IsSelected = true;
        }

        private void ClearSelectedConnections()
        {
            foreach (var conn in _selectedConnections)
                conn.IsSelected = false;

            _selectedConnections.Clear();
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
    }
}
