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
        Point GetSnappedPoint(Point rawPoint, double gridSize = 10);
        Point GetSnappedPoint(Point rawPoint, IEnumerable<FrameworkElement> others, FrameworkElement movingElement, out List<Line> snapLines);
    }
}
