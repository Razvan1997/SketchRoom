using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using WhiteBoard.Core.Services.Interfaces;

namespace WhiteBoard.Core.Services
{
    public class ZoomPanService : IZoomPanService
    {
        private const double MinZoom = 0.3;
        private const double MaxZoom = 3.0;

        public void Zoom(ScaleTransform scale, TranslateTransform translate, Point position, int delta)
        {
            double zoomFactor = delta > 0 ? 1.1 : 0.9;
            double newScale = scale.ScaleX * zoomFactor;

            if (newScale < MinZoom || newScale > MaxZoom)
                return;

            scale.ScaleX = newScale;
            scale.ScaleY = newScale;

            translate.X = (1 - zoomFactor) * position.X + translate.X * zoomFactor;
            translate.Y = (1 - zoomFactor) * position.Y + translate.Y * zoomFactor;
        }

        public Point Pan(Point current, Point last, TranslateTransform translate)
        {
            var delta = current - last;
            translate.X += delta.X;
            translate.Y += delta.Y;
            return current;
        }
    }
}
