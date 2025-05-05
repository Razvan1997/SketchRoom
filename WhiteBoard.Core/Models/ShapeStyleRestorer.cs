using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WhiteBoard.Core.Services.Interfaces;

namespace WhiteBoard.Core.Models
{
    public static class ShapeStyleRestorer
    {
        public static void ApplyStyle(BPMNShapeModelWithPosition shape, FrameworkElement element)
        {
            if (shape.ExtraProperties == null || shape.ExtraProperties.Count == 0)
                return;

            if (element is not IInteractiveShape interactiveShape)
                return;

            // Width / Height
            if (shape.Width > 0)
                element.Width = shape.Width;

            if (shape.Height > 0)
                element.Height = shape.Height;

            // Position (redundant aici, dar util dacă nu s-a aplicat deja)
            Canvas.SetLeft(element, shape.Left);
            Canvas.SetTop(element, shape.Top);

            // Rotation (dacă există în ExtraProperties)
            if (shape.ExtraProperties.TryGetValue("Rotation", out var rotationStr) &&
                double.TryParse(rotationStr, out var angle))
            {
                var transformGroup = element.RenderTransform as TransformGroup ?? new TransformGroup();
                var rotate = transformGroup.Children.OfType<RotateTransform>().FirstOrDefault();
                if (rotate == null)
                {
                    rotate = new RotateTransform();
                    transformGroup.Children.Add(rotate);
                }

                rotate.Angle = angle;
                element.RenderTransform = transformGroup;
                element.RenderTransformOrigin = new Point(0.5, 0.5);
            }

            if (shape.ExtraProperties.TryGetValue("Text", out var textValue) &&
                element is IInteractiveShape interactive)
            {
                // Creezi noul TextBox
                var textBox = new TextBox
                {
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    BorderBrush = Brushes.Transparent,
                    Padding = new Thickness(4),
                    Tag = "interactive",
                    AcceptsReturn = true,
                    AcceptsTab = true,
                    TextWrapping = TextWrapping.Wrap,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                    MinWidth = 80,
                    MaxWidth = 300,
                    FontSize = 14,
                    Text = textValue
                };

                if (shape.ExtraProperties.TryGetValue("FontSize", out var fontSizeStr) &&
                    double.TryParse(fontSizeStr, out var fontSize))
                    textBox.FontSize = fontSize;

                if (shape.ExtraProperties.TryGetValue("Foreground", out var fgStr))
                    textBox.Foreground = ConvertToBrush(fgStr);

                if (shape.ExtraProperties.TryGetValue("TextWrapping", out var wrapStr) &&
                    Enum.TryParse<TextWrapping>(wrapStr, out var wrap))
                    textBox.TextWrapping = wrap;

                if (element is ContentControl cc && cc.Content is Grid grid)
                {
                    grid.Children.Add(textBox);
                }
            }

            if (element is IShapeAddedXaml shapeAdded)
            {
                var renderer = shapeAdded.Renderer;

                if (renderer is IRestoreFromShape restorable)
                {
                    restorable.Restore(shape.ExtraProperties);
                }
            }
        }

        public static Brush ConvertToBrush(string hex)
        {
            try
            {
                return (SolidColorBrush)(new BrushConverter().ConvertFromString(hex) ?? Brushes.Transparent);
            }
            catch
            {
                return Brushes.Transparent;
            }
        }
    }
}
