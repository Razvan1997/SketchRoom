using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
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
                _path.Stroke = value ? Brushes.Red : _originalStroke;
                _path.StrokeDashArray = value ? new DoubleCollection { 2, 2 } : null;

                if (_arrowHead != null)
                    _arrowHead.Fill = _path.Stroke;
            }
        }
        private Brush _originalStroke = Brushes.Black;

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

            _path.MouseEnter += (s, e) =>
            {
                var neon = new DropShadowEffect
                {
                    Color = Colors.DeepSkyBlue,
                    BlurRadius = 25,
                    ShadowDepth = 0,
                    Opacity = 1
                };
                _path.Effect = neon;
            };

            _path.MouseLeave += (s, e) =>
            {
                _path.Effect = null;
            };

            _containerCanvas = new Canvas();
            _containerCanvas.Children.Add(_path);
        }
        public BPMNConnection(BPMNNode from, BPMNNode? to, PathGeometry bezierGeometry)
    : this(from, to)
{
            _figure.Segments.Clear();
            _geometry.Figures.Clear();

            foreach (var figure in bezierGeometry.Figures)
            {
                var copy = new PathFigure { StartPoint = figure.StartPoint, IsClosed = false, IsFilled = true };
                foreach (var seg in figure.Segments)
                    copy.Segments.Add(seg.Clone()); // clone to avoid shared references
                _geometry.Figures.Add(copy);
            }

            // adaugă săgeata la capătul curbei
            if (bezierGeometry.Figures.FirstOrDefault()?.Segments.LastOrDefault() is BezierSegment bezier)
                SetArrowFromTo(bezier.Point2, bezier.Point3); // Point2 -> control, Point3 -> final
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
                SetArrowFromTo(pointList[^2], pointList[^1]);
        }

        public void SetStroke(Brush stroke)
        {
            _originalStroke = stroke;
            _path.Stroke = stroke;

            if (_arrowHead != null)
                _arrowHead.Fill = stroke;
        }

        public override UIElement Visual => _containerCanvas;
        public override Rect Bounds => _geometry.Bounds;

        public void SetArrowFromTo(Point from, Point to)
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

        public BPMNConnectionExportModel? Export()
        {
            var model = new BPMNConnectionExportModel
            {
                CreatedAt = this.CreatedAt,
                IsCurved = this.Geometry.Figures.Any(f => f.Segments.OfType<BezierSegment>().Any()),
                PathPoints = new List<Point>(),
                StrokeHex = (_path.Stroke as SolidColorBrush)?.Color.ToString(),
                BezierSegments = new List<BezierSegmentData>()
            };

            foreach (var figure in this.Geometry.Figures)
            {
                model.PathPoints.Add(figure.StartPoint);

                foreach (var segment in figure.Segments)
                {
                    if (segment is LineSegment line)
                        model.PathPoints.Add(line.Point);
                    else if (segment is BezierSegment bezier)
                    {
                        model.BezierSegments.Add(new BezierSegmentData
                        {
                            Point1 = bezier.Point1,
                            Point2 = bezier.Point2,
                            Point3 = bezier.Point3
                        });

                        model.PathPoints.Add(bezier.Point3);
                    }
                }
            }

            if (From?.Visual is FrameworkElement fromEl && fromEl.Tag is string fromId)
                model.FromId = fromId;

            if (To?.Visual is FrameworkElement toEl && toEl.Tag is string toId)
                model.ToId = toId;

            return model.PathPoints.Count >= 2 ? model : null;
        }
    }


}