using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace WhiteBoard.Core.Services.Interfaces
{
    public interface IZoomPanService
    {
        void Zoom(ScaleTransform scale, TranslateTransform translate, Point position, int delta);
        Point Pan(Point current, Point last, TranslateTransform translate);
    }
}
