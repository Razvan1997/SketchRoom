using System.Windows;
using System.Windows.Media;

namespace DrawingStateService.States
{
    public class PanAndZoomService
    {
        private const double MinZoom = 0.3;
        private const double MaxZoom = 3.0;

        public void Zoom(
    ScaleTransform scaleTransform,
    TranslateTransform translateTransform,
    Point position,
    int delta)
        {
            double zoomFactor = delta > 0 ? 1.1 : 0.9;
            double newScale = scaleTransform.ScaleX * zoomFactor;

            if (newScale < 0.3 || newScale > 3.0) // sau MinZoom / MaxZoom dacă vrei constante
                return;

            scaleTransform.ScaleX = newScale;
            scaleTransform.ScaleY = newScale;

            translateTransform.X = (1 - zoomFactor) * position.X + translateTransform.X * zoomFactor;
            translateTransform.Y = (1 - zoomFactor) * position.Y + translateTransform.Y * zoomFactor;
        }

        public Point CalculatePan(Point current, Point last, TranslateTransform translateTransform)
        {
            var delta = current - last;
            translateTransform.X += delta.X;
            translateTransform.Y += delta.Y;
            return current;
        }
    }
}
