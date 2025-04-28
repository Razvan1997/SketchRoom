using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;

namespace WhiteBoard.Core.Services.Interfaces
{
    public interface IZOrderService
    {
        void BringToFront(UIElement element, Canvas canvas);
        void SendToBack(UIElement element, Canvas canvas);
        void MoveForward(UIElement element, Canvas canvas);
        void MoveBackward(UIElement element, Canvas canvas);
    }
}
