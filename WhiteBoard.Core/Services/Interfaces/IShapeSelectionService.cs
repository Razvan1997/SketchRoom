using SketchRoom.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace WhiteBoard.Core.Services.Interfaces
{
    public interface IShapeSelectionService
    {
        ShapePart Current { get; }

        void Select(ShapePart part, Border border, TextBox textBox);
        void ApplyVisual(Border border, TextBox textBox);

        // Pentru RichTextBox
        void SelectRich(ShapePart part, Border border, RichTextBox richTextBox);
        void ApplyVisual(Border border, RichTextBox richTextBox);
    }
}
