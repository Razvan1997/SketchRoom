using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows;
using WhiteBoard.Core.Services.Interfaces;
using System.Windows.Controls;
using System.Windows.Documents;

namespace WhiteBoard.Core.UndoRedo
{
    public class ChangeTextStyleCommand : IUndoableCommand
    {
        private readonly RichTextBox _richTextBox;
        private readonly TextRange _textRange;

        private readonly object _oldFontWeight;
        private readonly object _oldFontSize;
        private readonly object _oldForeground;
        private readonly object _oldBackground;

        private readonly object _newFontWeight;
        private readonly object _newFontSize;
        private readonly object _newForeground;
        private readonly object _newBackground;

        public ChangeTextStyleCommand(RichTextBox richTextBox, TextRange range, FontWeight newWeight, double newSize, Brush newColor, Brush newBackgroundColor)
        {
            _richTextBox = richTextBox;
            _textRange = range;

            var oldFontWeightRaw = range.GetPropertyValue(TextElement.FontWeightProperty);
            _oldFontWeight = oldFontWeightRaw is FontWeight fw ? fw : richTextBox.FontWeight;

            var oldFontSizeRaw = range.GetPropertyValue(TextElement.FontSizeProperty);
            _oldFontSize = oldFontSizeRaw is double fs ? fs : richTextBox.FontSize;

            var oldForegroundRaw = range.GetPropertyValue(TextElement.ForegroundProperty);
            _oldForeground = oldForegroundRaw is Brush fg ? fg : richTextBox.Foreground;

            _oldBackground = richTextBox.Background;

            _newFontWeight = newWeight;
            _newFontSize = newSize;
            _newForeground = newColor;
            _newBackground = newBackgroundColor;
        }

        public void Execute()
        {
            _textRange.ApplyPropertyValue(TextElement.FontWeightProperty, _newFontWeight);
            _textRange.ApplyPropertyValue(TextElement.FontSizeProperty, _newFontSize);
            _textRange.ApplyPropertyValue(TextElement.ForegroundProperty, _newForeground);
            _textRange.ApplyPropertyValue(TextElement.BackgroundProperty, _newBackground);
        }

        public void Undo()
        {
            _textRange.ApplyPropertyValue(TextElement.FontWeightProperty, _oldFontWeight);
            _textRange.ApplyPropertyValue(TextElement.FontSizeProperty, _oldFontSize);
            _textRange.ApplyPropertyValue(TextElement.ForegroundProperty, _oldForeground);
            _textRange.ApplyPropertyValue(TextElement.BackgroundProperty, _oldBackground);
        }
    }
}
