using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using WhiteBoard.Core.Helpers;

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
        public Point? FromOffset { get; set; }
        public Point? ToOffset { get; set; }

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
        public List<Point> OriginalPathPoints { get; set; } = new();
        public List<BezierSegmentData> OriginalBezierSegments { get; set; } = new();

        public string? StartDirection { get; set; }
        public string? EndDirection { get; set; }

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

            // Inițializează lista de puncte originale și segmente Bezier
            OriginalPathPoints = new List<Point>();
            OriginalBezierSegments = new List<BezierSegmentData>();

            foreach (var figure in bezierGeometry.Figures)
            {
                _figure.StartPoint = figure.StartPoint;
                _figure.Segments.Clear();

                // ✅ Adaugă StartPoint ca primul punct de referință
                OriginalPathPoints.Add(figure.StartPoint);

                foreach (var seg in figure.Segments)
                {
                    var segClone = seg.Clone();

                    if (seg is BezierSegment bezier)
                    {
                        OriginalBezierSegments.Add(new BezierSegmentData
                        {
                            Point1 = bezier.Point1,
                            Point2 = bezier.Point2,
                            Point3 = bezier.Point3
                        });

                        OriginalPathPoints.Add(bezier.Point3);
                    }
                    else if (seg is LineSegment line)
                    {
                        OriginalPathPoints.Add(line.Point);
                    }

                    _figure.Segments.Add(segClone);
                }

                _geometry.Figures.Clear();
                _geometry.Figures.Add(_figure);
            }

            // reconstruim săgeata
            if (bezierGeometry.Figures.FirstOrDefault()?.Segments.LastOrDefault() is BezierSegment lastBezier)
                SetArrowFromTo(lastBezier.Point2, lastBezier.Point3);
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

            OriginalPathPoints = pointList;

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
                BezierSegments = new List<BezierSegmentData>(),
                FromOffset = this.FromOffset,
                ToOffset = this.ToOffset,

                StartDirection = this.StartDirection,
                EndDirection = this.EndDirection
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

            if (From?.Visual is FrameworkElement fromEl)
                model.FromId = ShapeMetadata.GetShapeId(fromEl);
            if (To?.Visual is FrameworkElement toEl)
                model.ToId = ShapeMetadata.GetShapeId(toEl);

            return model.PathPoints.Count >= 2 ? model : null;
        }

        public void RecalculateGeometry()
        {
            if (From?.Visual is not FrameworkElement fromFe || To?.Visual is not FrameworkElement toFe)
                return;

            if (OriginalBezierSegments.Count == 0)
            {
                if (OriginalPathPoints.Count < 2) return;

                var fromCurrent = new Point(Canvas.GetLeft(fromFe), Canvas.GetTop(fromFe)) +
                                  (Vector)(FromOffset ?? new Point(fromFe.ActualWidth / 2, fromFe.ActualHeight / 2));
                var toCurrent = new Point(Canvas.GetLeft(toFe), Canvas.GetTop(toFe)) +
                                (Vector)(ToOffset ?? new Point(toFe.ActualWidth / 2, toFe.ActualHeight / 2));

                var fromOriginal = OriginalPathPoints.First();
                var toOriginal = OriginalPathPoints.Last();

                var deltaFrom = fromCurrent - fromOriginal;
                var deltaTo = toCurrent - toOriginal;

                var movedPoints = new List<Point>();

                for (int i = 0; i < OriginalPathPoints.Count; i++)
                {
                    if (i == 0)
                        movedPoints.Add(OriginalPathPoints[0] + deltaFrom);
                    else if (i == OriginalPathPoints.Count - 1)
                        movedPoints.Add(OriginalPathPoints[^1] + deltaTo);
                    else
                        movedPoints.Add(OriginalPathPoints[i]);
                }

                SetCustomPath(movedPoints, addArrow: true);
                _path.Data = _geometry; // 💡 important pentru vizual
                return;
            }

            if (OriginalBezierSegments.Count > 0 && OriginalPathPoints.Count > 0)
            {
                var fromCurr = GetCanvasPoint(fromFe, FromOffset);
                var toCurr = GetCanvasPoint(toFe, ToOffset);

                // Calculează delta dintre pozițiile curente și cele originale
                var deltaFrom = fromCurr - OriginalPathPoints.First();
                var deltaTo = toCurr - OriginalPathPoints.Last();

                var figure = new PathFigure { StartPoint = fromCurr };
                Point currentPoint = fromCurr;

                for (int i = 0; i < OriginalBezierSegments.Count; i++)
                {
                    var bezier = OriginalBezierSegments[i];
                    // Pentru primul control point: aplicăm deltaFrom
                    var p1 = bezier.Point1 + deltaFrom;
                    var p2 = bezier.Point2 + deltaTo;
                    var p3 = bezier.Point3 + deltaTo;

                    figure.Segments.Add(new BezierSegment(p1, p2, p3, true));
                    currentPoint = p3;
                }

                // Verifică dacă aveam un punct final în plus (linia după bezier)
                if (OriginalPathPoints.Count > OriginalBezierSegments.Count + 1)
                {
                    var lastOriginal = OriginalPathPoints.Last();
                    var adjustedLast = lastOriginal + deltaTo;
                    figure.Segments.Add(new LineSegment(adjustedLast, true));
                }

                _geometry.Figures.Clear();
                _geometry.Figures.Add(figure);
                _path.Data = _geometry;

                // reconstrucția săgeții
                if (figure.Segments.LastOrDefault() is LineSegment lastLine)
                {
                    SetArrowFromTo(currentPoint, lastLine.Point);
                }
                else
                {
                    var lastBezier = OriginalBezierSegments.Last();
                    SetArrowFromTo(lastBezier.Point2 + deltaTo, lastBezier.Point3 + deltaTo);
                }
            }
        }

        private Point GetCanvasPoint(FrameworkElement fe, Point? offset)
        {
            return new Point(Canvas.GetLeft(fe), Canvas.GetTop(fe)) +
                   (Vector)(offset ?? new Point(fe.ActualWidth / 2, fe.ActualHeight / 2));
        }
    }


}