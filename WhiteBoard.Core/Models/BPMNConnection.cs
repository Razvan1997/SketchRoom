using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows;

namespace WhiteBoard.Core.Models
{
    public class BPMNConnection : WhiteBoardElement
    {
        private readonly Path _path;
        private readonly PathGeometry _geometry;
        private readonly PathFigure _figure;

        public BPMNNode From { get; set; }
        public BPMNNode To { get; set; }

        public BPMNConnection(BPMNNode from, BPMNNode to)
        {
            From = from;
            To = to;

            _figure = new PathFigure();
            _geometry = new PathGeometry();
            _geometry.Figures.Add(_figure);

            _path = new Path
            {
                Stroke = Brushes.Black,
                StrokeThickness = 2,
                Data = _geometry,
                StrokeEndLineCap = PenLineCap.Triangle,
                IsHitTestVisible = false
            };

            UpdateLinePosition();
        }

        public override UIElement Visual => _path;

        public override Rect Bounds => _geometry.Bounds;

        public void UpdateLinePosition()
        {
            var fromCenter = From.Center;
            var toCenter = To.Center;

            _figure.StartPoint = fromCenter;
            _figure.Segments.Clear();
            _figure.Segments.Add(new LineSegment(toCenter, true));
        }
    }
}
