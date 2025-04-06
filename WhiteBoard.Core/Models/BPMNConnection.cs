using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace WhiteBoard.Core.Models
{
    public class BPMNConnection : WhiteBoardElement
    {
        private readonly Path _path;
        private readonly PathGeometry _geometry;
        private readonly PathFigure _figure;
        private readonly Canvas _containerCanvas;
        private Polygon? _arrowHead;
        public BPMNNode? From { get; set; }
        public BPMNNode? To { get; set; }

        public BPMNConnection? ConnectedToConnection { get; set; }
        public Point? ConnectionIntersectionPoint { get; set; }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                _path.Stroke = value ? Brushes.Red : Brushes.Black;
                _path.StrokeDashArray = value ? new DoubleCollection { 2, 2 } : null;
                if (_arrowHead != null)
                    _arrowHead.Fill = _path.Stroke;
            }
        }

        public event EventHandler? Clicked;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public PathGeometry Geometry => _geometry;

        public Ellipse? ConnectionDot { get; set; }

        public BPMNConnection(BPMNNode? from, BPMNNode? to)
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
                Cursor = Cursors.Hand
            };

            _path.MouseLeftButtonDown += (s, e) =>
            {
                Clicked?.Invoke(this, EventArgs.Empty);
                e.Handled = true;
            };

            _containerCanvas = new Canvas();
            _containerCanvas.Children.Add(_path);
        }

        public BPMNConnection(BPMNNode from, BPMNNode? to, IEnumerable<Point> pathPoints, bool addArrow = true)
    : this(from, to)
        {
            if (pathPoints != null)
                SetCustomPath(pathPoints, addArrow);
        }

        public void SetCustomPath(IEnumerable<Point> points, bool addArrow = true)
        {
            var pointList = points.ToList();
            if (pointList.Count < 2) return;

            _figure.StartPoint = pointList[0];
            _figure.Segments.Clear();

            for (int i = 1; i < pointList.Count; i++)
                _figure.Segments.Add(new LineSegment(pointList[i], true));

            if (addArrow)
                AddArrowHead(pointList[pointList.Count - 2], pointList[pointList.Count - 1]);
        }

        private void AddArrowHead(Point from, Point to)
        {
            if (_arrowHead != null)
                _containerCanvas.Children.Remove(_arrowHead);

            Vector direction = from - to;
            direction.Normalize();
            Vector normal = new Vector(-direction.Y, direction.X);

            double size = 10;

            Point p1 = to;
            Point p2 = to + direction * size + normal * (size / 2);
            Point p3 = to + direction * size - normal * (size / 2);

            _arrowHead = new Polygon
            {
                Fill = _path.Stroke,
                Points = new PointCollection { p1, p2, p3 },
                IsHitTestVisible = false
            };

            _containerCanvas.Children.Add(_arrowHead);
        }

        public override UIElement Visual => _containerCanvas;
        public override Rect Bounds => _geometry.Bounds;
    }
}