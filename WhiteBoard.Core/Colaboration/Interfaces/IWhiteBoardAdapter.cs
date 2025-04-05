using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;

namespace WhiteBoard.Core.Colaboration.Interfaces
{
    public interface IWhiteBoardAdapter
    {
        void StartNewRemoteLine();
        void AddLine(IEnumerable<Point> points, Brush color, double thickness);
        void AddLivePoint(Point point, Brush color);
        void MoveCursorImage(Point position, BitmapImage? image);
    }
}
