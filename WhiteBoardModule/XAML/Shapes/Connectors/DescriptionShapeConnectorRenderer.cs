using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows;
using WhiteBoard.Core.Services.Interfaces;
using WhiteBoard.Core.Models;
using SketchRoom.Models.Enums;
using System.Windows.Input;

namespace WhiteBoardModule.XAML.Shapes.Connectors
{
    public class DescriptionShapeConnectorRenderer : IShapeRenderer, IRestoreFromShape, IStrokeChangable, IForegroundChangable
    {
        private readonly bool _withBindings;
        private Grid? RenderedElement;
        private readonly IShapeSelectionService _selectionService;
        public DescriptionShapeConnectorRenderer(bool withBindings = false)

        {
            _withBindings = withBindings;
            _selectionService = ContainerLocator.Container.Resolve<IShapeSelectionService>();
        }

        public UIElement CreatePreview()
        {
            var preferences = ContainerLocator.Container.Resolve<IDrawingPreferencesService>();

            var diamond = CreateDiamond(preferences.SelectedColor);
            var line = CreateLine(preferences.SelectedColor);
            var label = new TextBlock
            {
                Text = "Description",
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = preferences.SelectedColor,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var grid = new Grid
            {
                Width = 100,
                Height = 40
            };

            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            Grid.SetColumn(diamond, 0);
            Grid.SetColumn(line, 1);
            Grid.SetColumn(label, 2);

            grid.Children.Add(diamond);
            grid.Children.Add(line);
            grid.Children.Add(label);

            return new Viewbox
            {
                Width = 80,
                Height = 80,
                Stretch = Stretch.Uniform,
                Child = grid
            };
        }
        private bool IsMouseOver(UIElement element, MouseEventArgs e)
        {
            var pos = e.GetPosition(element);
            var rect = new Rect(0, 0, element.RenderSize.Width, element.RenderSize.Height);
            return rect.Contains(pos);
        }
        private void AdjustSizeToContent(TextBox textBox)
        {
            var formattedText = new FormattedText(
                textBox.Text,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(textBox.FontFamily, textBox.FontStyle, textBox.FontWeight, textBox.FontStretch),
                textBox.FontSize,
                Brushes.Black,
                new NumberSubstitution(),
                1);

            // Adaugă puțin padding ca să nu fie tăiat textul
            textBox.Height = formattedText.Height + 10;
        }
        public UIElement Render()
        {
            var preferences = ContainerLocator.Container.Resolve<IDrawingPreferencesService>();

            var diamond = CreateDiamond(preferences.SelectedColor);
            var line = CreateLine(preferences.SelectedColor);
            var textBox = new TextBox
            {
                Text = "Description",
                FontSize = 14,
                FontWeight = preferences.FontWeight,
                Foreground = preferences.SelectedColor,
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                MinWidth = 80,
                Height = 24,
                Name = "ConnectorDescriptionText"
            };
            textBox.LayoutUpdated += (s, e) => AdjustSizeToContent(textBox);
            textBox.PreviewMouseLeftButtonDown += (s, e) =>
            {
                var pos = e.GetPosition(textBox);

                if (IsMouseOver(textBox, e))
                {
                    _selectionService.Select(ShapePart.Text, textBox);
                    return;
                }
            };

            if (_withBindings)
            {
                textBox.SetBinding(TextBox.FontWeightProperty, new Binding(nameof(preferences.FontWeight)) { Source = preferences });
                textBox.SetBinding(TextBox.ForegroundProperty, new Binding(nameof(preferences.SelectedColor)) { Source = preferences });
            }

            var grid = new Grid
            {
                VerticalAlignment = VerticalAlignment.Center,
                Height = 40
            };

            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            Grid.SetColumn(diamond, 0);
            Grid.SetColumn(line, 1);
            Grid.SetColumn(textBox, 2);

            grid.Children.Add(diamond);
            grid.Children.Add(line);
            grid.Children.Add(textBox);

            grid.Tag = new Dictionary<string, object>
            {
                { "Diamond", diamond },
                { "Line", line },
                { "DescriptionText", textBox }
            };
            RenderedElement = grid;
            return grid;
        }

        private Polygon CreateDiamond(Brush fill)
        {
            return new Polygon
            {
                Points = new PointCollection
                {
                    new Point(5, 0),
                    new Point(10, 5),
                    new Point(5, 10),
                    new Point(0, 5)
                },
                Fill = fill,
                Width = 10,
                Height = 10,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
        }

        private Rectangle CreateLine(Brush fill)
        {
            return new Rectangle
            {
                Height = 2,
                Fill = fill,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
        }

        public BPMNShapeModelWithPosition? ExportData(IInteractiveShape control)
        {
            if (control is not FrameworkElement fe)
                return null;

            var position = new Point(Canvas.GetLeft(fe), Canvas.GetTop(fe));
            var size = new Size(fe.Width, fe.Height);

            var angle = 0.0;
            if (fe.RenderTransform is TransformGroup tg &&
                tg.Children.OfType<RotateTransform>().FirstOrDefault() is RotateTransform rt)
            {
                angle = rt.Angle;
            }

            string? text = null, textColor = null, lineColor = null, fontWeight = null, fontSize = null;

            if (RenderedElement?.Tag is Dictionary<string, object> tag)
            {
                if (tag.TryGetValue("DescriptionText", out var txtObj) && txtObj is TextBox txt)
                {
                    text = txt.Text;
                    textColor = (txt.Foreground as SolidColorBrush)?.Color.ToString();
                    fontWeight = txt.FontWeight.ToString();
                    fontSize = txt.FontSize.ToString();
                }

                if (tag.TryGetValue("Line", out var lineObj) && lineObj is Rectangle line)
                {
                    lineColor = (line.Fill as SolidColorBrush)?.Color.ToString();
                }
            }

            return new BPMNShapeModelWithPosition
            {
                Type = SketchRoom.Models.Enums.ShapeType.ConnectorDescriptionShape,
                Left = position.X,
                Top = position.Y,
                Width = size.Width,
                Height = size.Height,
                Name = fe.Name,
                Category = "Connector",
                SvgUri = null,
                RotationAngle = angle,
                ExtraProperties = new Dictionary<string, string>
                {
                     { "DescriptionText", text ?? "" },
                     { "TextColor", textColor ?? "" },
                     { "LineColor", lineColor ?? "" },
                     { "FontWeight", fontWeight ?? "Normal" },
                     { "FontSize", fontSize ?? "" }
                }
            };
        }

        public void Restore(Dictionary<string, string> extraProperties)
        {
            if (extraProperties == null || extraProperties.Count == 0)
                return;

            if (RenderedElement is not Grid grid || grid.Tag is not Dictionary<string, object> tag)
                return;

            if (tag.TryGetValue("DescriptionText", out var textObj) && textObj is TextBox textBox)
            {
                if (extraProperties.TryGetValue("DescriptionText", out var text))
                    textBox.Text = text;

                if (extraProperties.TryGetValue("TextColor", out var color))
                    textBox.Foreground = ShapeStyleRestorer.ConvertToBrush(color);

                if (extraProperties.TryGetValue("FontWeight", out var fwStr))
                {
                    try
                    {
                        var converter = new FontWeightConverter();
                        var weight = (FontWeight)converter.ConvertFromString(fwStr);
                        textBox.FontWeight = weight;
                    }
                    catch
                    {
                        textBox.FontWeight = FontWeights.Normal;
                    }
                }
                if (extraProperties.TryGetValue("FontSize", out var fontSizeStr) &&
                    double.TryParse(fontSizeStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var fontSize))
                {
                    textBox.FontSize = fontSize;
                }
            }

            if (tag.TryGetValue("Line", out var lineObj) && lineObj is Rectangle line &&
                extraProperties.TryGetValue("LineColor", out var lineColor))
            {
                line.Fill = ShapeStyleRestorer.ConvertToBrush(lineColor);
            }
        }

        public void SetForeground(Brush brush)
        {
            if (RenderedElement?.Tag is not Dictionary<string, object> tag) return;

            if (tag.TryGetValue("DescriptionText", out var txtObj) && txtObj is TextBox label)
                label.Foreground = brush;
        }

        public void SetStroke(Brush brush)
        {
            if (RenderedElement?.Tag is not Dictionary<string, object> tag) return;

            if (tag.TryGetValue("Line", out var lineObj) && lineObj is Rectangle line)
                line.Fill = brush;

            if (tag.TryGetValue("Diamond", out var diamondObj) && diamondObj is Polygon diamond)
                diamond.Fill = brush;
        }
    }
}
