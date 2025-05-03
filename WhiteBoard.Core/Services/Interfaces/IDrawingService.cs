using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WhiteBoard.Core.Models;

namespace WhiteBoard.Core.Services.Interfaces
{
    public interface IDrawingService
    {
        FreeDrawStroke StartStroke(Point startPoint, Brush color, double thickness);
        void AddPointToStroke(FreeDrawStroke stroke, Point point);
        void FinishStroke(FreeDrawStroke stroke);
        void RemoveStroke(FreeDrawStroke stroke);
        IReadOnlyList<FreeDrawStroke> RecentStrokes { get; }
        void Clear();

        // Colaborare live
        void BeginExternalStroke();
        void AddExternalStroke(IEnumerable<Point> points, Brush color, double thickness);
        void PreviewExternalPoint(Point point, Brush color);
        FreeDrawStroke? GetExternalPreviewStroke();

        // Elemente vizibile
        IEnumerable<WhiteBoardElement> GetElements();

        // Ștergere completă a stroke-urilor apropiate
        IEnumerable<FreeDrawStroke> RemoveStrokesNear(Point position, double radius);

        // Ștergere doar a punctelor din stroke-uri (ca în Paint)
        void ErasePointsNear(Point position, double radius);

        // Legare canvas
        void SetCanvas(Canvas canvas);
    }
}
