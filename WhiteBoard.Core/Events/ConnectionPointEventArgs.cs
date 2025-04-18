using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace WhiteBoard.Core.Events
{
    public class ConnectionPointEventArgs : EventArgs
    {
        public string Direction { get; }
        public UIElement SourceElement { get; }
        public MouseButtonEventArgs MouseArgs { get; }

        public ConnectionPointEventArgs(string direction, UIElement sourceElement, MouseButtonEventArgs e)
        {
            Direction = direction;
            SourceElement = sourceElement;
            MouseArgs = e;
        }
    }
}
