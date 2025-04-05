using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using WhiteBoard.Core.Models;

namespace WhiteBoard.Core.Services.Interfaces
{
    public interface ICanvasRenderer
    {
        void RenderElement(Canvas canvas, WhiteBoardElement element);
        void Clear(Canvas canvas);
        void RenderRemoteCursor(Canvas canvas, Point position, BitmapImage? image);
        bool HasVisual(Canvas canvas, UIElement visual);
    }
}
