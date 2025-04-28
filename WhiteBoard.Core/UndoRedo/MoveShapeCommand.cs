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
    public class MoveShapeCommand : IUndoableCommand
    {
        private readonly FrameworkElement _element;
        private readonly Point _from;
        private readonly Point _to;

        public MoveShapeCommand(FrameworkElement element, Point from, Point to)
        {
            _element = element;
            _from = from;
            _to = to;
        }

        public void Execute()
        {
            Canvas.SetLeft(_element, _to.X);
            Canvas.SetTop(_element, _to.Y);
        }

        public void Undo()
        {
            Canvas.SetLeft(_element, _from.X);
            Canvas.SetTop(_element, _from.Y);
        }
    }
}
