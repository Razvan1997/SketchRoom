using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using WhiteBoard.Core.Services.Interfaces;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Media.Animation;
using System.Windows.Input;

namespace WhiteBoard.Core.Tools
{
    public class RotateTool : IDrawingTool, IToolBehavior
    {
        public string Name => "RotateTool";

        private readonly Canvas _canvas;
        private IInteractiveShape? _target;
        private FrameworkElement? _ghostShape;
        private Border? _rotationOverlay;

        private Point _startMousePos;
        private double _initialAngle;
        private double _overlayBaseY;
        private bool _isOverlayMoved = false;
        private double _lastSnappedAngle;
        public RotateTool(Canvas canvas)
        {
            _canvas = canvas;
        }

        public void StartRotation(IInteractiveShape shape, Point startMousePosition)
        {
            _target = shape;
            _startMousePos = startMousePosition;
            _initialAngle = GetCurrentRotation(shape.Visual);

            // Ghost shape
            _ghostShape = CloneAsGhost(shape.Visual);
            _canvas.Children.Add(_ghostShape);
            Canvas.SetLeft(_ghostShape, Canvas.GetLeft(shape.Visual));
            Canvas.SetTop(_ghostShape, Canvas.GetTop(shape.Visual));

            // Overlay
            _rotationOverlay = CreateRotationOverlay();
            _canvas.Children.Add(_rotationOverlay);

            UpdateOverlay(_initialAngle);
        }

        public void OnMouseDown(Point position, MouseButtonEventArgs e) { }

        public void OnMouseMove(Point position, MouseEventArgs e)
        {
            if (_target == null || _ghostShape == null) return;

            double angle = ComputeRotationAngle(_startMousePos, position, _target);
            _lastSnappedAngle = NormalizeAngle(SnapAngle(angle, 15));
            ApplyRotation(_ghostShape, _lastSnappedAngle);
            UpdateOverlay(_lastSnappedAngle);

            if (_rotationOverlay != null)
            {
                // 🔄 Forțează actualizarea layout-ului
                _rotationOverlay.UpdateLayout();
                _ghostShape.UpdateLayout();

                var overlayBounds = new Rect(
                    Canvas.GetLeft(_rotationOverlay),
                    Canvas.GetTop(_rotationOverlay),
                    _rotationOverlay.ActualWidth,
                    _rotationOverlay.ActualHeight);

                var ghostBounds = new Rect(
                    Canvas.GetLeft(_ghostShape),
                    Canvas.GetTop(_ghostShape),
                    _ghostShape.ActualWidth,
                    _ghostShape.ActualHeight);

                if (overlayBounds.IntersectsWith(ghostBounds) && !_isOverlayMoved)
                {
                    MoveOverlayUp();
                }
                else if (!overlayBounds.IntersectsWith(ghostBounds) && _isOverlayMoved)
                {
                    MoveOverlayBack();
                }
            }
        }

        public void OnMouseUp(Point position, MouseButtonEventArgs e)
        {
            if (_target == null || _ghostShape == null) return;

            double angle = ComputeRotationAngle(_startMousePos, position, _target);
            ApplyRotation(_target.Visual, _lastSnappedAngle);

            _canvas.Children.Remove(_ghostShape);
            _canvas.Children.Remove(_rotationOverlay);

            _ghostShape = null;
            _rotationOverlay = null;
            _target = null;
        }

        private double ComputeRotationAngle(Point start, Point current, IInteractiveShape shape)
        {
            var center = new Point(
                Canvas.GetLeft(shape.Visual) + shape.Visual.RenderSize.Width / 2,
                Canvas.GetTop(shape.Visual) + shape.Visual.RenderSize.Height / 2);

            double angle1 = Math.Atan2(start.Y - center.Y, start.X - center.X);
            double angle2 = Math.Atan2(current.Y - center.Y, current.X - center.X);

            double angle = angle2 - angle1;
            return _initialAngle + angle * 180 / Math.PI;
        }

        private double GetCurrentRotation(UIElement element)
        {
            if (element.RenderTransform is RotateTransform rt)
                return rt.Angle;
            return 0;
        }

        private void ApplyRotation(UIElement element, double angle)
        {
            var center = new Point(element.RenderSize.Width / 2, element.RenderSize.Height / 2);
            element.RenderTransform = new RotateTransform(angle, center.X, center.Y);
        }

        private FrameworkElement CloneAsGhost(UIElement original)
        {
            return new Border
            {
                Width = ((FrameworkElement)original).ActualWidth,
                Height = ((FrameworkElement)original).ActualHeight,
                BorderBrush = Brushes.DodgerBlue,
                BorderThickness = new Thickness(2),
                Background = Brushes.Transparent,
                Opacity = 0.5,
                IsHitTestVisible = false
            };
        }

        private Border CreateRotationOverlay()
        {
            return new Border
            {
                Width = 60,
                Height = 30,
                Background = Brushes.Black,
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Margin = new Thickness(0, -20 , 0 ,0),
                Opacity = 0.85,
                IsHitTestVisible = false,
                Child = new TextBlock
                {
                    Text = "0°",
                    FontSize = 14,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
        }

        private void UpdateOverlay(double angle)
        {
            if (_target == null || _rotationOverlay == null) return;

            if (_rotationOverlay.Child is TextBlock tb)
                tb.Text = $"{Math.Round(NormalizeAngle(angle))}°";

            // Calculează centrul vizual al shape-ului
            var shape = _target.Visual;
            shape.UpdateLayout();

            double centerX = Canvas.GetLeft(shape) + shape.RenderSize.Width / 2;
            double overlayX = centerX - _rotationOverlay.Width / 2;

            // Overlay-ul este mereu deasupra shape-ului, cu spațiu de 10px
            double overlayY = Canvas.GetTop(shape) - _rotationOverlay.Height - 10;

            // Aplicăm X direct (poziția pe orizontală nu se animă)
            Canvas.SetLeft(_rotationOverlay, overlayX);

            // Reținem poziția Y pentru referință
            if (!_isOverlayMoved)
            {
                Canvas.SetTop(_rotationOverlay, overlayY);
            }

            _overlayBaseY = overlayY;
        }

        private void MoveOverlayUp()
        {
            if (_rotationOverlay == null) return;
            _isOverlayMoved = true;

            AnimateCanvasTop(_rotationOverlay, _overlayBaseY - 40, 200);
        }

        private void MoveOverlayBack()
        {
            if (_rotationOverlay == null) return;
            _isOverlayMoved = false;

            AnimateCanvasTop(_rotationOverlay, _overlayBaseY, 250);
        }

        private void AnimateCanvasTop(UIElement element, double to, int durationMs)
        {
            var anim = new DoubleAnimation
            {
                To = to,
                Duration = TimeSpan.FromMilliseconds(durationMs),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            Storyboard.SetTarget(anim, element);
            Storyboard.SetTargetProperty(anim, new PropertyPath("(Canvas.Top)"));

            var storyboard = new Storyboard();
            storyboard.Children.Add(anim);
            storyboard.Begin();
        }

        private double SnapAngle(double angle, double step)
        {
            return Math.Round(angle / step) * step;
        }

        public void OnMouseDown(Point position)
        {
        }

        public void OnMouseMove(Point position)
        {
        }

        public void OnMouseUp(Point position)
        {
        }

        private double NormalizeAngle(double angle)
        {
            angle %= 360;
            if (angle > 180)
                angle -= 360;
            if (angle < -180)
                angle += 360;
            return angle;
        }
    }
}
