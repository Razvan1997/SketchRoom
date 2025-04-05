using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
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

        public IReadOnlyList<FreeDrawStroke> RecentStrokes => _strokes.AsReadOnly();

        public FreeDrawStroke StartStroke(Point startPoint, Brush color, double thickness)
        {
            var stroke = new FreeDrawStroke
            {
                Color = color,
                Thickness = thickness
            };
            stroke.Points.Add(startPoint);
            _strokes.Add(stroke);
            return stroke;
        }

        public void AddPointToStroke(FreeDrawStroke stroke, Point point)
        {
            stroke.Points.Add(point);
        }

        public void FinishStroke(FreeDrawStroke stroke)
        {
            // Poți salva într-o istorie, aplica undo/redo etc.
        }

        public void RemoveStroke(FreeDrawStroke stroke)
        {
            _strokes.Remove(stroke);
        }

        public void Clear()
        {
            _strokes.Clear();
        }

        // === FUNCȚII PENTRU DATE EXTERNE (live colaborare) ===

        public void BeginExternalStroke()
        {
            _externalStrokePreview = new FreeDrawStroke
            {
                Color = Brushes.Blue, // sau altă culoare default pentru "partener"
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

            foreach (var point in points)
            {
                stroke.AddPoint(point);
            }

            _strokes.Add(stroke);
        }

        public void PreviewExternalPoint(Point point, Brush color)
        {
            if (_externalStrokePreview == null)
            {
                _externalStrokePreview = new FreeDrawStroke
                {
                    Color = color,
                    Thickness = 2
                };
            }

            _externalStrokePreview.AddPoint(point);
        }

        public IEnumerable<WhiteBoardElement> GetElements()
        {
            return _strokes;
        }

        public FreeDrawStroke? GetExternalPreviewStroke()
        {
            return _externalStrokePreview;
        }
    }
}
