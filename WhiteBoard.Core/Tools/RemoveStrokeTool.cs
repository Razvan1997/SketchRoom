using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using WhiteBoard.Core.Services.Interfaces;

namespace WhiteBoard.Core.Tools
{
    public class RemoveStrokeTool : IDrawingTool
    {
        private Point? _lastPos = null;
        public string Name => "RemoveStroke";

        private readonly Canvas _canvas;
        private readonly IDrawingService _drawingService;

        private bool _isErasing = false;
        private Ellipse? _eraserVisual;

        public bool IsDrawing => _isErasing;

        public event Action<List<Point>>? StrokeCompleted;
        public event Action<Point>? PointDrawn;
        public event Action<Point>? PointerMoved;
        private DateTime _lastEraseTime = DateTime.MinValue;
        private readonly TimeSpan _eraseThrottle = TimeSpan.FromMilliseconds(10);
        private readonly IDrawingPreferencesService _preferencesService;
        public RemoveStrokeTool(IDrawingService drawingService, Canvas canvas, IDrawingPreferencesService drawingPreferencesService)
        {
            _drawingService = drawingService;
            _canvas = canvas;
            _preferencesService = drawingPreferencesService;
        }

        public void OnMouseDown(Point pos, MouseButtonEventArgs e)
        {
            _isErasing = true;
            ShowEraserVisual(pos);
            TryEraseAt(pos);
        }

        public void OnMouseMove(Point pos, MouseEventArgs e)
        {
            if (!_isErasing) return;

            MoveEraserVisual(pos);

            if ((DateTime.Now - _lastEraseTime) > _eraseThrottle)
            {
                TryEraseAt(pos);
                _lastEraseTime = DateTime.Now;
            }

            PointerMoved?.Invoke(pos);
        }

        public void OnMouseUp(Point pos, MouseButtonEventArgs e)
        {
            _isErasing = false;
            _lastPos = null;
            RemoveEraserVisual();
        }

        private void TryEraseAt(Point pos)
        {
            var radius = _preferencesService.EraseRadius;
            _drawingService.ErasePointsNear(pos, radius);

            if (_lastPos is not null)
            {
                foreach (var p in InterpolatePoints(_lastPos.Value, pos, radius / 2))
                {
                    _drawingService.ErasePointsNear(pos, radius);
                }
            }

            _lastPos = pos;
            PointDrawn?.Invoke(pos);
        }

        private void ShowEraserVisual(Point center)
        {
            var radius = _preferencesService.EraseRadius;

            _eraserVisual = new Ellipse
            {
                Width = radius * 2,
                Height = radius * 2,
                Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#404040")) { Opacity = 0.4 },
                IsHitTestVisible = false
            };

            Canvas.SetLeft(_eraserVisual, center.X - radius);
            Canvas.SetTop(_eraserVisual, center.Y - radius);
            _canvas.Children.Add(_eraserVisual);
        }

        private void MoveEraserVisual(Point center)
        {
            var radius = _preferencesService.EraseRadius;

            if (_eraserVisual != null)
            {
                Canvas.SetLeft(_eraserVisual, center.X - radius);
                Canvas.SetTop(_eraserVisual, center.Y - radius);
            }
        }

        private void RemoveEraserVisual()
        {
            if (_eraserVisual != null)
            {
                _canvas.Children.Remove(_eraserVisual);
                _eraserVisual = null;
            }
        }

        public void OnMouseDown(Point position)
        {
            throw new NotImplementedException();
        }

        private IEnumerable<Point> InterpolatePoints(Point from, Point to, double step)
        {
            double distance = Math.Sqrt(Math.Pow(to.X - from.X, 2) + Math.Pow(to.Y - from.Y, 2));
            int steps = (int)(distance / step);

            for (int i = 0; i <= steps; i++)
            {
                double t = (double)i / steps;
                yield return new Point(
                    from.X + (to.X - from.X) * t,
                    from.Y + (to.Y - from.Y) * t
                );
            }
        }
    }
}
