using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows;

namespace DrawingStateService.States
{
    public class LiveRemoteDrawingService
    {
        private DateTime _lastRemoteDrawTime = DateTime.MinValue;
        private Polyline _remoteLine;
        private Image _cursorImage;

        public Polyline AddLivePoint(Canvas canvas, Point point, Brush color, double thickness = 2)
        {
            var now = DateTime.Now;
            if ((now - _lastRemoteDrawTime).TotalMilliseconds < 16)
                return _remoteLine;

            _lastRemoteDrawTime = now;

            if (_remoteLine == null)
            {
                _remoteLine = new Polyline
                {
                    Stroke = color,
                    StrokeThickness = thickness
                };
                canvas.Children.Add(_remoteLine);
            }

            _remoteLine.Points.Add(point);
            return _remoteLine;
        }

        public void ResetLiveLine(Canvas canvas)
        {
            if (_remoteLine != null)
            {
                canvas.Children.Remove(_remoteLine);
                _remoteLine = null;
            }
        }

        public void MoveCursorImage(Canvas canvas, Point point, BitmapImage image = null)
        {
            if (_cursorImage == null)
            {
                _cursorImage = new Image
                {
                    Width = 20,
                    Height = 20
                };
                canvas.Children.Add(_cursorImage);
            }

            if (image != null)
                _cursorImage.Source = image;

            Canvas.SetLeft(_cursorImage, point.X - 20);
            Canvas.SetTop(_cursorImage, point.Y - 20);
        }
    }
}
