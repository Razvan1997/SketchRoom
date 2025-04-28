using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using WhiteBoard.Core.Services.Interfaces;

namespace WhiteBoard.Core.UndoRedo
{
    public class ResizeShapeCommand : IUndoableCommand
    {
        private readonly FrameworkElement _element;
        private readonly double _oldWidth;
        private readonly double _oldHeight;
        private readonly double _newWidth;
        private readonly double _newHeight;

        public ResizeShapeCommand(FrameworkElement element, double newWidth, double newHeight)
        {
            _element = element;
            _oldWidth = element.Width;
            _oldHeight = element.Height;
            _newWidth = newWidth;
            _newHeight = newHeight;
        }

        public void Execute()
        {
            _element.Width = _newWidth;
            _element.Height = _newHeight;
        }

        public void Undo()
        {
            _element.Width = _oldWidth;
            _element.Height = _oldHeight;
        }
    }
}
