using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using WhiteBoard.Core.Services.Interfaces;

namespace WhiteBoard.Core.UndoRedo
{
    public class ChangeTextBoxStyleCommand : IUndoableCommand
    {
        private readonly TextBox _textBox;
        private readonly Brush _oldForeground;
        private readonly Brush _newForeground;

        public ChangeTextBoxStyleCommand(TextBox textBox, Brush newForeground)
        {
            _textBox = textBox;
            _oldForeground = textBox.Foreground;
            _newForeground = newForeground;
        }

        public void Execute()
        {
            _textBox.Foreground = _newForeground;
        }

        public void Undo()
        {
            _textBox.Foreground = _oldForeground;
        }
    }
}
