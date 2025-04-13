using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows;

namespace WhiteBoardModule.XAML.StyleUpdater
{
    public static class ShapeStyleUpdater
    {
        public static void Apply(object? content, FontWeight weight, double size, Brush brush)
        {
            switch (content)
            {
                case Border { Child: StackPanel stack }:
                    ApplyToTextBoxes(stack.Children.OfType<TextBox>(), weight, size, brush);
                    break;

                case Viewbox { Child: Canvas canvas }:
                    ApplyToShapes(canvas.Children.OfType<Shape>(), brush);
                    break;

                case Shape shape:
                    shape.Stroke = brush;
                    break;

                case Grid grid when grid.Tag is Dictionary<string, object> tag:
                    ApplyToTaggedElements(tag, brush, weight, size);
                    break;

                case StackPanel stack when stack.Tag is Dictionary<string, object> tag2:
                    ApplyToTaggedElements(tag2, brush, weight, size);
                    break;
            }
        }

        private static void ApplyToTextBoxes(IEnumerable<TextBox> boxes, FontWeight weight, double size, Brush brush)
        {
            foreach (var tb in boxes)
            {
                tb.FontWeight = weight;
                tb.FontSize = size;

                if (tb.IsFocused)
                    tb.Foreground = brush;
            }
        }

        private static void ApplyToShapes(IEnumerable<Shape> shapes, Brush brush)
        {
            foreach (var shape in shapes)
                shape.Stroke = brush;
        }

        private static void ApplyToTaggedElements(Dictionary<string, object> tag, Brush brush, FontWeight weight, double size)
        {
            void SetFill(string key)
            {
                if (tag.TryGetValue(key, out var val) && val is Shape shape)
                    shape.Fill = brush;
            }

            SetFill("LeftLine");
            SetFill("RightLine");
            SetFill("Arrow");

            void SetTextBox(string key)
            {
                if (tag.TryGetValue(key, out var val) && val is TextBox tb)
                {
                    tb.FontWeight = weight;
                    tb.FontSize = size;

                    if (tb.IsFocused)
                        tb.Foreground = brush;
                }
            }

            SetTextBox("LabelText");
            SetTextBox("SourceLabel");
            SetTextBox("SideLabel");

            if (tag.TryGetValue("ContainerBorder", out var borderObj) && borderObj is Border border)
            {
                border.BorderBrush = brush;
            }
        }
    }
}
