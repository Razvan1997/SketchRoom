using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shapes;

namespace WhiteBoard.Core.Services.Interfaces
{
    public interface ISnapService
    {
        public Point GetSnappedPointCursor(
    Point rawPoint,
    IEnumerable<FrameworkElement> others,
    FrameworkElement movingElement,
    out List<Line> snapLines,
    bool snapX = true,
    bool snapY = true);
        Point GetSnappedPoint(Point rawPoint, double gridSize = 10);
        Point GetSnappedPoint(Point rawPoint, IEnumerable<FrameworkElement> others, FrameworkElement movingElement, out List<Line> snapLines);
        List<Line> GetSnapGuides(Point rawPoint, IEnumerable<FrameworkElement> others, FrameworkElement movingElement);
    }
}
