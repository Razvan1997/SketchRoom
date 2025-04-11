using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WhiteBoard.Core.Services.Interfaces;
using WhiteBoard.Core.Tools;

namespace WhiteBoard.Core.Behaviors
{
    public class TextToolBehavior : IToolBehavior
    {
        private readonly TextTool _tool;

        public TextToolBehavior(TextTool tool)
        {
            _tool = tool;
        }

        public void OnMouseDown(Point pos, MouseButtonEventArgs e)
        {
            _tool.OnMouseDown(pos);
            e.Handled = true;
        }

        public void OnMouseMove(Point pos, MouseEventArgs e)
        {
            _tool.OnMouseMove(pos);
        }

        public void OnMouseUp(Point pos, MouseButtonEventArgs e)
        {
            _tool.OnMouseUp(pos);
        }
    }
}
