using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using WhiteBoard.Core.Models;
using WhiteBoard.Core.Services.Interfaces;

namespace WhiteBoard.Core.Services
{
    public class DrawingService : IDrawingService
    {
        private readonly List<FreeDrawStroke> _strokes = new();
        private FreeDrawStroke? _externalStrokePreview;
        private Canvas? _canvas;

        public IReadOnlyList<FreeDrawStroke> RecentStrokes => _strokes.AsReadOnly();

        public void SetCanvas(Canvas canvas) => _canvas = canvas;

        public FreeDrawStroke StartStroke(Point startPoint, Brush color, double thickness)
        {
            var stroke = new FreeDrawStroke
            {
                Color = color,
                Thickness = thickness
            };
            stroke.AddPoint(startPoint);
            _strokes.Add(stroke);
            return stroke;
        }

        public void AddPointToStroke(FreeDrawStroke stroke, Point point)
        {
            stroke.AddPoint(point);
        }

        public void FinishStroke(FreeDrawStroke stroke)
        {
            // Future logic: undo/redo etc.
        }

        public void RemoveStroke(FreeDrawStroke stroke)
        {
            _strokes.Remove(stroke);
            if (_canvas != null)
                _canvas.Children.Remove(stroke.Visual);
        }

        public void Clear()
        {
            foreach (var stroke in _strokes)
                _canvas?.Children.Remove(stroke.Visual);

            _strokes.Clear();
        }

        public void ErasePointsNear(Point center, double radius)
        {
            double radiusSquared = radius * radius;

            foreach (var stroke in _strokes.ToList())
            {
                if (stroke.Visual is not Polyline polyline)
                    continue;

                // Verificare rapidă: dacă bounding box-ul nu atinge cercul, sari peste stroke
                if (!stroke.Bounds.IntersectsWith(new Rect(center.X - radius, center.Y - radius, radius * 2, radius * 2)))
                    continue;

                var originalPoints = polyline.Points.ToList();
                var cleanedSegments = new List<List<Point>>();
                var currentSegment = new List<Point>();

                foreach (var p in originalPoints)
                {
                    if ((p - center).LengthSquared <= radiusSquared)
                    {
                        // punctul e sub radieră => închide segmentul curent
                        if (currentSegment.Count >= 2)
                            cleanedSegments.Add(new List<Point>(currentSegment));
                        currentSegment.Clear();
                    }
                    else
                    {
                        currentSegment.Add(p);
                    }
                }

                // adaugă segmentul final dacă e valid
                if (currentSegment.Count >= 2)
                    cleanedSegments.Add(currentSegment);

                // înlocuiește stroke-ul vechi
                _canvas?.Children.Remove(stroke.Visual);
                _strokes.Remove(stroke);

                foreach (var segment in cleanedSegments)
                {
                    var newStroke = new FreeDrawStroke
                    {
                        Color = stroke.Color,
                        Thickness = stroke.Thickness
                    };

                    foreach (var pt in segment)
                        newStroke.AddPoint(pt);

                    _strokes.Add(newStroke);
                    _canvas?.Children.Add(newStroke.Visual);
                }
            }
        }


        public IEnumerable<FreeDrawStroke> RemoveStrokesNear(Point position, double radius)
        {
            var toRemove = _strokes
                .Where(s => s.Points.Any(p => Distance(p, position) <= radius))
                .ToList();

            foreach (var stroke in toRemove)
                RemoveStroke(stroke);

            return toRemove;
        }

        private double Distance(Point p1, Point p2)
        {
            double dx = p1.X - p2.X;
            double dy = p1.Y - p2.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        // === Colaborare live ===

        public void BeginExternalStroke()
        {
            _externalStrokePreview = new FreeDrawStroke
            {
                Color = Brushes.Blue,
                Thickness = 2
            };
        }

        public void AddExternalStroke(IEnumerable<Point> points, Brush color, double thickness)
        {
            var stroke = new FreeDrawStroke
            {
                Color = color,
                Thickness = thickness
            };

            foreach (var p in points)
                stroke.AddPoint(p);

            _strokes.Add(stroke);
        }

        public void PreviewExternalPoint(Point point, Brush color)
        {
            _externalStrokePreview ??= new FreeDrawStroke
            {
                Color = color,
                Thickness = 2
            };

            _externalStrokePreview.AddPoint(point);
        }

        public IEnumerable<WhiteBoardElement> GetElements() => _strokes;
        public FreeDrawStroke? GetExternalPreviewStroke() => _externalStrokePreview;
    }
}
