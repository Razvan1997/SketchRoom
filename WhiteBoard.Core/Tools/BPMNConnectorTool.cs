using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using WhiteBoard.Core.Models;
using WhiteBoard.Core.Services.Interfaces;
using System.Xml.Linq;
using System.Windows.Shapes;
using System.Windows.Media;

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

        private Line? _tempLine;
        private Point _currentMouse;

        public string Name => "Connector";

        public BpmnConnectorTool(Canvas canvas, List<BPMNConnection> connections, Dictionary<FrameworkElement, BPMNNode> nodes)
        {
            _canvas = canvas;
            _connections = connections;
            _nodes = nodes;
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

            if (_tempLine != null)
            {
                _tempLine.X2 = pos.X;
                _tempLine.Y2 = pos.Y;
            }
        }
        public void OnMouseUp(Point pos)
        {
            if (_from == null || _tempLine == null)
                return;

            foreach (var el in _canvas.Children.OfType<FrameworkElement>())
            {
                if (_nodes.TryGetValue(el, out var toNode))
                {
                    var bounds = new Rect(Canvas.GetLeft(el), Canvas.GetTop(el), el.ActualWidth, el.ActualHeight);
                    if (bounds.Contains(pos) && toNode != _from)
                    {
                        var connection = new BPMNConnection(_from, toNode);
                        _canvas.Children.Add(connection.Visual);
                        _connections.Add(connection);
                        break;
                    }
                }
            }

            _canvas.Children.Remove(_tempLine);
            _tempLine = null;
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

        public void SetSelected(IInteractiveShape shape, string direction)
        {
            _selectedShape = shape;
            _selectedShape.Select();

            if (_selectedShape is FrameworkElement fe && _nodes.TryGetValue(fe, out var node))
            {
                _from = node;
                _selectedElement = fe;
                Highlight(fe);

                // Poziție pentru pornire linie temporară
                var start = GetConnectionPoint(fe, direction);
                _tempLine = new Line
                {
                    X1 = start.X,
                    Y1 = start.Y,
                    X2 = start.X,
                    Y2 = start.Y,
                    Stroke = Brushes.Black,
                    StrokeThickness = 2,
                    StrokeDashArray = new DoubleCollection { 2, 2 }
                };
                _canvas.Children.Add(_tempLine);
            }
        }

        private Point GetConnectionPoint(FrameworkElement element, string direction)
        {
            var left = Canvas.GetLeft(element);
            var top = Canvas.GetTop(element);

            return direction switch
            {
                "Top" => new Point(left + element.ActualWidth / 2, top),
                "Right" => new Point(left + element.ActualWidth, top + element.ActualHeight / 2),
                "Bottom" => new Point(left + element.ActualWidth / 2, top + element.ActualHeight),
                "Left" => new Point(left, top + element.ActualHeight / 2),
                _ => new Point(left, top)
            };
        }
    }
}
