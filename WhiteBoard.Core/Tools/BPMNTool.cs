using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using WhiteBoard.Core.Services.Interfaces;
using System.Windows.Shapes;

namespace WhiteBoard.Core.Tools
{
    public class BpmnTool : IDrawingTool
    {
        public string Name => "BpmnTool";

        private readonly Canvas _canvas;
        private readonly ISnapService _snapService;
        private IInteractiveShape? _selectedShape;
        private IInteractiveShape? _draggingShape;
        private Point _lastMousePos;

        public event Action<IInteractiveShape?>? ShapeSelected;
        private readonly Canvas _snapCanvas;
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
                var currentLeft = Canvas.GetLeft(fe);
                var currentTop = Canvas.GetTop(fe);
                var newPos = new Point(currentLeft + dx, currentTop + dy);

                var others = _canvas.Children.OfType<FrameworkElement>()
                        .Where(e => e != fe).ToList();

                // Obține doar liniile de snap (nu poziția ajustată!)
                _snapService.GetSnappedPoint(newPos, others, fe, out List<Line> snapLines);

                // Afișează ghidajele (snap lines)
                _snapCanvas.Children.Clear();
                foreach (var line in snapLines)
                    _snapCanvas.Children.Add(line);

                // Mutare reală (fără snap)
                Canvas.SetLeft(fe, newPos.X);
                Canvas.SetTop(fe, newPos.Y);

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
    }
}
