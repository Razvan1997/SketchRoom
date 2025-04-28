using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;
using WhiteBoard.Core.Services.Interfaces;

namespace WhiteBoard.Core.UndoRedo
{
    public class ChangeBorderStyleCommand : IUndoableCommand
    {
        private readonly Border _border;
        private readonly Brush _oldBrush;
        private readonly Brush _newBrush;
        private readonly Thickness _oldThickness;
        private readonly Thickness _newThickness;
        private readonly string _target; // "Background", "BorderBrush"

        public ChangeBorderStyleCommand(Border border, Brush newBrush, Thickness newThickness, string target)
        {
            _border = border;
            _target = target;

            if (target == "Background")
            {
                _oldBrush = border.Background;
            }
            else
            {
                _oldBrush = border.BorderBrush;
            }

            _oldThickness = border.BorderThickness;
            _newBrush = newBrush;
            _newThickness = newThickness;
        }

        public void Execute()
        {
            if (_target == "Background")
            {
                _border.Background = _newBrush;
            }
            else
            {
                _border.BorderBrush = _newBrush;
            }

            _border.BorderThickness = _newThickness;
        }

        public void Undo()
        {
            if (_target == "Background")
            {
                _border.Background = _oldBrush;
            }
            else
            {
                _border.BorderBrush = _oldBrush;
            }

            _border.BorderThickness = _oldThickness;
        }
    }
}
