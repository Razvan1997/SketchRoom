using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using WhiteBoard.Core.Services.Interfaces;

namespace WhiteBoard.Core.UndoRedo
{
    public class AddShapeCommand : IUndoableCommand
    {
        private readonly Canvas _canvas;
        private readonly FrameworkElement _element;

        public AddShapeCommand(Canvas canvas, FrameworkElement element)
        {
            _canvas = canvas;
            _element = element;
        }

        public void Execute()
        {
            if (!_canvas.Children.Contains(_element))
                _canvas.Children.Add(_element);
        }

        public void Undo()
        {
            if (_canvas.Children.Contains(_element))
                _canvas.Children.Remove(_element);
        }
    }
}
