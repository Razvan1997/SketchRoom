using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace WhiteBoard.Core.Models
{
    public class FreeDrawStroke : WhiteBoardElement
    {
        private readonly Polyline _polyline;

        public FreeDrawStroke()
        {
            _polyline = new Polyline();
        }

        public override UIElement Visual => _polyline;

        public void AddPoint(Point point) => _polyline.Points.Add(point);

        public IList<Point> Points => _polyline.Points;

        public Brush Color
        {
            get => _polyline.Stroke;
            set => _polyline.Stroke = value;
        }

        public double Thickness
        {
            get => _polyline.StrokeThickness;
            set => _polyline.StrokeThickness = value;
        }

        public override Rect Bounds
        {
            get
            {
                if (_polyline.Points.Count == 0)
                    return Rect.Empty;

                double minX = double.MaxValue;
                double minY = double.MaxValue;
                double maxX = double.MinValue;
                double maxY = double.MinValue;

                foreach (var pt in _polyline.Points)
                {
                    if (pt.X < minX) minX = pt.X;
                    if (pt.Y < minY) minY = pt.Y;
                    if (pt.X > maxX) maxX = pt.X;
                    if (pt.Y > maxY) maxY = pt.Y;
                }

                return new Rect(new Point(minX, minY), new Point(maxX, maxY));
            }
        }
        public FreeDrawStrokeExportModel Export()
        {
            return new FreeDrawStrokeExportModel
            {
                Points = this.Points.ToList(),
                StrokeColorHex = (this.Color as SolidColorBrush)?.Color.ToString(),
                StrokeThickness = this.Thickness
            };
        }

    }
}
