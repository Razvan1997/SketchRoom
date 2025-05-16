using SketchRoom.Models.Enums;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WhiteBoard.Core.Models;
using WhiteBoard.Core.Services.Interfaces;

namespace WhiteBoardModule.XAML.Shapes.General
{
    public class TextBoxBorderShapeRenderer : IShapeRenderer, IBackgroundChangable, IStrokeChangable, IForegroundChangable, IRestoreFromShape
    {
        private readonly bool _withBindings;
        private readonly IShapeSelectionService _selectionService;
        private Border _border;
        private TextBox _textBox;
        public TextBoxBorderShapeRenderer(bool withBindings = false)
        {
            _withBindings = withBindings;
            _selectionService = ContainerLocator.Container.Resolve<IShapeSelectionService>();
        }

        public UIElement Render()
        {
            var preferences = ContainerLocator.Container.Resolve<IDrawingPreferencesService>();

            var border = new Border
            {
                Name = "border",
                BorderThickness = new Thickness(2),
                Background = Brushes.White,
                CornerRadius = new CornerRadius(8),
                BorderBrush = Brushes.Black,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            var textBox = new TextBox
            {
                Text = "Editable text",
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                FontSize = 16,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                Foreground = Brushes.Black,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            var textContainer = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            textContainer.Children.Add(textBox);
            border.Child = textContainer;

            // Click în zona border-ului
            border.PreviewMouseLeftButtonDown += (s, e) =>
            {
                var pos = e.GetPosition(border);

                if (IsMouseOver(textBox, e))
                {
                    _selectionService.Select(ShapePart.Text, textBox);
                    return;
                }

                if (IsMouseOverMargin(border, pos))
                    _selectionService.Select(ShapePart.Margin, border);
                else
                    _selectionService.Select(ShapePart.Border, border);
            };

            // Hover logic
            border.MouseMove += (s, e) =>
            {
                if (_selectionService.Current == ShapePart.Margin) return;

                var pos = e.GetPosition(border);
                border.BorderThickness = IsMouseOverMargin(border, pos) ? new Thickness(4) : new Thickness(2);
            };

            border.MouseLeave += (s, e) =>
            {
                if (_selectionService.Current != ShapePart.Margin)
                    border.BorderThickness = new Thickness(2);
            };

            _selectionService.ApplyVisual(border);
            _border = border;
            _textBox = textBox;
            return border;
        }

        private bool IsMouseOver(UIElement element, MouseEventArgs e)
        {
            var pos = e.GetPosition(element);
            var rect = new Rect(0, 0, element.RenderSize.Width, element.RenderSize.Height);
            return rect.Contains(pos);
        }

        private bool IsMouseOverMargin(Border border, Point mousePos)
        {
            const double marginWidth = 6;

            return mousePos.X < marginWidth ||
                   mousePos.X > border.ActualWidth - marginWidth ||
                   mousePos.Y < marginWidth ||
                   mousePos.Y > border.ActualHeight - marginWidth;
        }

        public UIElement CreatePreview()
        {
            return new Viewbox
            {
                Width = 60,
                Height = 60,
                Stretch = Stretch.Uniform,
                Child = new Border
                {
                    BorderThickness = new Thickness(2),
                    BorderBrush = Brushes.Gray,
                    Background = Brushes.White,
                    Padding = new Thickness(8),
                    CornerRadius = new CornerRadius(8),
                    Width = 80,
                    Height = 50,
                    Child = new TextBlock
                    {
                        Text = "Preview",
                        FontSize = 16,
                        Foreground = Brushes.Gray,
                        Background = Brushes.Transparent,
                        TextAlignment = TextAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                }
            };
        }

        public void SetBackground(Brush brush)
        {
            _border.Background = brush;
        }

        public void SetStroke(Brush brush)
        {
            _border.BorderBrush = brush;
        }

        public void SetForeground(Brush brush)
        {
            _textBox.Foreground = brush;
        }

        public BPMNShapeModelWithPosition? ExportData(IInteractiveShape control)
        {
            if (control is not FrameworkElement fe)
                return null;

            if (_border == null || _textBox == null)
                return null;

            return new BPMNShapeModelWithPosition
            {
                Type = ShapeType.BorderTextBox,
                Left = Canvas.GetLeft(fe),
                Top = Canvas.GetTop(fe),
                Width = fe.Width,
                Height = fe.Height,
                Name = fe.Name,
                Category = "General",
                SvgUri = null,
                ExtraProperties = new Dictionary<string, string>
        {
            { "Background", (_border.Background as SolidColorBrush)?.Color.ToString() ?? "#FFFFFFFF" },
            { "BorderBrush", (_border.BorderBrush as SolidColorBrush)?.Color.ToString() ?? "#FF000000" },
            { "Foreground", (_textBox.Foreground as SolidColorBrush)?.Color.ToString() ?? "#FF000000" },
            { "TextShape", _textBox.Text ?? "" },
            { "FontSize", _textBox.FontSize.ToString(System.Globalization.CultureInfo.InvariantCulture) },
            { "FontWeight", _textBox.FontWeight.ToString() },
            { "FontStyle", _textBox.FontStyle.ToString() },
            { "TextWrapping", _textBox.TextWrapping.ToString() }
        }
            };
        }

        public void Restore(Dictionary<string, string> extraProperties)
        {
            if (_border == null || _textBox == null)
                return;

            var brushConverter = new BrushConverter();
            var fontWeightConverter = new FontWeightConverter();

            if (extraProperties.TryGetValue("Background", out var bgColor))
            {
                try { _border.Background = (Brush)brushConverter.ConvertFromString(bgColor); }
                catch { _border.Background = Brushes.White; }
            }

            if (extraProperties.TryGetValue("BorderBrush", out var strokeColor))
            {
                try { _border.BorderBrush = (Brush)brushConverter.ConvertFromString(strokeColor); }
                catch { _border.BorderBrush = Brushes.Black; }
            }

            if (extraProperties.TryGetValue("Foreground", out var fgColor))
            {
                try { _textBox.Foreground = (Brush)brushConverter.ConvertFromString(fgColor); }
                catch { _textBox.Foreground = Brushes.Black; }
            }

            if (extraProperties.TryGetValue("TextShape", out var text))
            {
                _textBox.Text = text;
            }

            if (extraProperties.TryGetValue("FontSize", out var fontSizeStr) &&
                double.TryParse(fontSizeStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var fontSize))
            {
                _textBox.FontSize = fontSize;
            }

            if (extraProperties.TryGetValue("FontWeight", out var fontWeightStr))
            {
                try { _textBox.FontWeight = (FontWeight)fontWeightConverter.ConvertFromString(fontWeightStr); }
                catch { _textBox.FontWeight = FontWeights.Normal; }
            }

            if (extraProperties.TryGetValue("FontStyle", out var fontStyleStr))
            {
                var styleConverter = new FontStyleConverter();
                try
                {
                    var style = (FontStyle)styleConverter.ConvertFromString(fontStyleStr);
                    _textBox.FontStyle = style;
                }
                catch
                {
                    _textBox.FontStyle = FontStyles.Normal;
                }
            }

            if (extraProperties.TryGetValue("TextWrapping", out var wrapStr) &&
                Enum.TryParse(wrapStr, out TextWrapping wrapping))
            {
                _textBox.TextWrapping = wrapping;
            }
        }
    }
}
