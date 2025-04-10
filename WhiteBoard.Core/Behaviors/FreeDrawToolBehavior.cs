using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WhiteBoard.Core.Services.Interfaces;

namespace WhiteBoard.Core.Behaviors
{
    public class FreeDrawToolBehavior : IToolBehavior
    {
        private readonly WhiteBoardHost _host;

        public FreeDrawToolBehavior(WhiteBoardHost host)
        {
            _host = host;
        }

        public void OnMouseDown(Point position, MouseButtonEventArgs e)
        {
            _host.HandleMouseDown(position);
        }

        public void OnMouseMove(Point position, MouseEventArgs e)
        {
            _host.HandleMouseMove(position);
        }

        public void OnMouseUp(Point position, MouseButtonEventArgs e)
        {
            _host.HandleMouseUp(position);
        }
    }
}
