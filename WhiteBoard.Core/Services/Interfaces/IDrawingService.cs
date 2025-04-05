using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
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

        void BeginExternalStroke();
        void AddExternalStroke(IEnumerable<Point> points, Brush color, double thickness);
        void PreviewExternalPoint(Point point, Brush color);
        IEnumerable<WhiteBoardElement> GetElements();

        FreeDrawStroke? GetExternalPreviewStroke();
    }
}
