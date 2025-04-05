using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows;
using System.Windows.Input;

namespace WhiteBoard.Core.Models
{
    public class BPMNConnection : WhiteBoardElement
    {
        private readonly Path _path;
        private readonly PathGeometry _geometry;
        private readonly PathFigure _figure;

        public BPMNNode From { get; set; }
        public BPMNNode To { get; set; }
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                _path.Stroke = value ? Brushes.Red : Brushes.Black;
                _path.StrokeDashArray = value ? new DoubleCollection { 2, 2 } : null;
            }
        }
        private bool _isSelected;

        public event EventHandler? Clicked;
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

        public BPMNConnection(BPMNNode from, BPMNNode to, IEnumerable<Point> pathPoints)
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
                Cursor = Cursors.Hand
            };

            _path.MouseLeftButtonDown += (s, e) =>
            {
                Clicked?.Invoke(this, EventArgs.Empty);
                e.Handled = true;
            };

            if (pathPoints != null)
                SetCustomPath(pathPoints);
            else
                UpdateLinePosition();
        }

        public void SetCustomPath(IEnumerable<Point> points)
        {
            var pointList = points.ToList();
            if (!pointList.Any()) return;

            _figure.StartPoint = pointList[0];
            _figure.Segments.Clear();

            for (int i = 1; i < pointList.Count; i++)
            {
                _figure.Segments.Add(new LineSegment(pointList[i], true));
            }
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
