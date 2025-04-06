using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using WhiteBoard.Core.Services.Interfaces;
using System.Windows.Shapes;
using System.Windows.Controls.Primitives;

namespace WhiteBoard.Core.Tools
{
    public class BpmnTool : IDrawingTool
    {
        public string Name => "BpmnTool";

        private readonly Canvas _canvas;
        private readonly ISnapService _snapService;
        private readonly Canvas _snapCanvas;

        private IInteractiveShape? _selectedShape;
        private IInteractiveShape? _draggingShape;
        private Point _lastMousePos;

        public event Action<IInteractiveShape?>? ShapeSelected;

        public BpmnTool(Canvas canvas, ISnapService snapService, Canvas snapCanvas)
        {
            _canvas = canvas;
            _snapService = snapService;
            _snapCanvas = snapCanvas;
        }

        public void OnMouseDown(Point pos)
        {
            _draggingShape = null;

            foreach (var el in _canvas.Children.OfType<FrameworkElement>().Reverse())
            {
                var bounds = new Rect(
                    Canvas.GetLeft(el),
                    Canvas.GetTop(el),
                    el.ActualWidth,
                    el.ActualHeight);

                if (bounds.Contains(pos) && el is IInteractiveShape interactive)
                {
                    if (_selectedShape != interactive)
                    {
                        DeselectCurrent();
                        _selectedShape = interactive;
                        _selectedShape.Select();
                        ShapeSelected?.Invoke(_selectedShape);
                    }

                    _draggingShape = interactive;
                    _lastMousePos = pos;
                    return;
                }
            }

            DeselectCurrent();
            ShapeSelected?.Invoke(null);
        }

        public void OnMouseMove(Point pos)
        {
            if (_draggingShape == null) return;

            var dx = pos.X - _lastMousePos.X;
            var dy = pos.Y - _lastMousePos.Y;

            if (_draggingShape is FrameworkElement fe)
            {
                double currentLeft = Canvas.GetLeft(fe);
                double currentTop = Canvas.GetTop(fe);

                Point rawNewPos = new Point(currentLeft + dx, currentTop + dy);

                // Elemente la care să se alinieze (excludem shape-ul curent)
                var others = _canvas.Children.OfType<FrameworkElement>()
                             .Where(e => e != fe && IsSnappable(e))
                             .ToList();

                // Obține poziția snap-uită și liniile de ghidaj
                var snappedPos = _snapService.GetSnappedPoint(rawNewPos, others, fe, out List<Line> snapLines);

                // Afișează liniile de snap
                _snapCanvas.Children.Clear();
                foreach (var line in snapLines)
                    _snapCanvas.Children.Add(line);

                // Aplică poziția nouă snap-uită
                Canvas.SetLeft(fe, rawNewPos.X); // noua poziție reală, fără snap
                Canvas.SetTop(fe, rawNewPos.Y);

                _lastMousePos = pos;
            }
        }

        public void OnMouseUp(Point pos)
        {
            _draggingShape = null;
            _snapCanvas.Children.Clear();
        }

        public void DeselectCurrent()
        {
            if (_selectedShape != null)
            {
                _selectedShape.Deselect();
                _selectedShape = null;
            }
        }

        private bool IsSnappable(FrameworkElement element)
        {
            if (element is Thumb || element is Rectangle)
                return false;

            if (element is IInteractiveShape)
                return true;

            // exclude orice altceva ce nu este shape vizibil principal
            return !(element is Line or Ellipse or Path or Border or TextBlock);
        }
    }
}
