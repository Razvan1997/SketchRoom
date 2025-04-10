using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace WhiteBoard.Core.Services.Interfaces
{
    public interface IToolBehavior
    {
        void OnMouseDown(Point position, MouseButtonEventArgs e);
        void OnMouseMove(Point position, MouseEventArgs e);
        void OnMouseUp(Point position, MouseButtonEventArgs e);
    }
}
