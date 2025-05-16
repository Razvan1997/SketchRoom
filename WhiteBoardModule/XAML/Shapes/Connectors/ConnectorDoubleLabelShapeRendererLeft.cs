using SketchRoom.Models.Enums;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using WhiteBoard.Core.Models;
using WhiteBoard.Core.Services.Interfaces;

namespace WhiteBoardModule.XAML.Shapes.Connectors
{
    public class ConnectorDoubleLabelShapeRendererLeft : IShapeRenderer, IStrokeChangable, IForegroundChangable, IRestoreFromShape
    {
        private readonly bool _withBindings;
        private StackPanel? _stackPanel;
        private readonly IShapeSelectionService _selectionService;
        public ConnectorDoubleLabelShapeRendererLeft(bool withBindings = false)
        {
            _withBindings = withBindings;
            _selectionService = ContainerLocator.Container.Resolve<IShapeSelectionService>();
        }

        public UIElement CreatePreview()
        {
            var preferences = ContainerLocator.Container.Resolve<IDrawingPreferencesService>();

            var sourceLabel = new TextBlock
            {
                Text = "Source",
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = preferences.SelectedColor,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(20, 0, 0, 1)
            };

            var inlineLabel = new TextBlock
            {
                Text = "Label",
                FontSize = 10,
                FontWeight = FontWeights.Normal,
                Foreground = preferences.SelectedColor,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var leftArrow = new Polygon
            {
                Points = new PointCollection
                {
                    new Point(10, 0),
                    new Point(0, 5),
                    new Point(10, 10)
                },
                Fill = preferences.SelectedColor,
                Width = 10,
                Height = 10,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left
            };

            var leftLine = new Rectangle
            {
                Height = 2,
                Fill = preferences.SelectedColor,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            var rightLine = new Rectangle
            {
                Height = 2,
                Fill = preferences.SelectedColor,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            var lineGrid = new Grid
            {
                Height = 30,
                Width = 100
            };

            lineGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            lineGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            lineGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            lineGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            Grid.SetColumn(leftArrow, 0);
            Grid.SetColumn(leftLine, 1);
            Grid.SetColumn(inlineLabel, 2);
            Grid.SetColumn(rightLine, 3);

            lineGrid.Children.Add(leftArrow);
            lineGrid.Children.Add(leftLine);
            lineGrid.Children.Add(inlineLabel);
            lineGrid.Children.Add(rightLine);

            var preview = new StackPanel
            {
                Orientation = Orientation.Vertical,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Children = { sourceLabel, lineGrid }
            };

            return new Viewbox
            {
                Width = 80,
                Height = 80,
                Stretch = Stretch.Uniform,
                Child = preview
            };
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

            var sourceBox = new TextBox
            {
                Text = "Source",
                FontSize = 14,
                FontWeight = preferences.FontWeight,
                Foreground = preferences.SelectedColor,
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 0, 0, -10),
                MinWidth = 60,
                Name = "SourceLabel"
            };
            sourceBox.LayoutUpdated += (s, e) => AdjustSizeToContent(sourceBox);
            sourceBox.PreviewMouseLeftButtonDown += (s, e) =>
            {
                var pos = e.GetPosition(sourceBox);

                if (IsMouseOver(sourceBox, e))
                {
                    _selectionService.Select(ShapePart.Text, sourceBox);
                    return;
                }
            };


            if (_withBindings)
            {
                sourceBox.SetBinding(TextBox.FontWeightProperty, new Binding(nameof(preferences.FontWeight)) { Source = preferences });
                sourceBox.SetBinding(TextBox.ForegroundProperty, new Binding(nameof(preferences.SelectedColor)) { Source = preferences });
            }

            var labelBox = new TextBox
            {
                Text = "Label",
                FontSize = 14,
                FontWeight = preferences.FontWeight,
                Foreground = preferences.SelectedColor,
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                MinWidth = 60,
                Height = 24,
                Name = "ConnectorLabelText"
            };
            labelBox.LayoutUpdated += (s, e) => AdjustSizeToContent(labelBox);
            labelBox.PreviewMouseLeftButtonDown += (s, e) =>
            {
                var pos = e.GetPosition(labelBox);

                if (IsMouseOver(labelBox, e))
                {
                    _selectionService.Select(ShapePart.Text, labelBox);
                    return;
                }
            };

            //labelBox.GotFocus += (s, e) => labelBox.Foreground = preferences.SelectedColor;

            if (_withBindings)
            {
                labelBox.SetBinding(TextBox.FontWeightProperty, new Binding(nameof(preferences.FontWeight)) { Source = preferences });
                labelBox.SetBinding(TextBox.FontSizeProperty, new Binding(nameof(preferences.FontSize)) { Source = preferences });
                labelBox.SetBinding(TextBox.ForegroundProperty, new Binding(nameof(preferences.SelectedColor)) { Source = preferences });
            }

            var arrow = new Polygon
            {
                Points = new PointCollection
                {
                    new Point(10, 0),
                    new Point(0, 5),
                    new Point(10, 10)
                },
                Fill = preferences.SelectedColor,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left,
                Width = 10,
                Height = 10,
                Name = "ConnectorArrow"
            };

            var leftLine = new Rectangle
            {
                Height = 2,
                Fill = preferences.SelectedColor,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Name = "ConnectorLineLeft"
            };

            var rightLine = new Rectangle
            {
                Height = 2,
                Fill = preferences.SelectedColor,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Name = "ConnectorLineRight"
            };

            var lineGrid = new Grid
            {
                Height = 40
            };

            lineGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            lineGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            lineGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            lineGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            Grid.SetColumn(arrow, 0);
            Grid.SetColumn(leftLine, 1);
            Grid.SetColumn(labelBox, 2);
            Grid.SetColumn(rightLine, 3);

            lineGrid.Children.Add(arrow);
            lineGrid.Children.Add(leftLine);
            lineGrid.Children.Add(labelBox);
            lineGrid.Children.Add(rightLine);

            var stackPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                VerticalAlignment = VerticalAlignment.Center
            };

            stackPanel.Children.Add(sourceBox);
            stackPanel.Children.Add(lineGrid);

            stackPanel.Tag = new Dictionary<string, object>
            {
                { "LabelText", labelBox },
                { "LeftLine", leftLine },
                { "RightLine", rightLine },
                { "Arrow", arrow },
                { "SourceLabel", sourceBox }
            };

            _stackPanel = stackPanel;
            return stackPanel;
        }

        public void SetForeground(Brush brush)
        {
            if (_stackPanel?.Tag is not Dictionary<string, object> tag) return;

            if (tag["LabelText"] is TextBox label)
                label.Foreground = brush;

            if (tag["SourceLabel"] is TextBox source)
                source.Foreground = brush;
        }

        public void SetStroke(Brush brush)
        {
            if (_stackPanel?.Tag is not Dictionary<string, object> tag) return;

            if (tag["LeftLine"] is Rectangle left)
                left.Fill = brush;

            if (tag["RightLine"] is Rectangle right)
                right.Fill = brush;

            if (tag["Arrow"] is Polygon arrow)
                arrow.Fill = brush;
        }

        public BPMNShapeModelWithPosition? ExportData(IInteractiveShape control)
        {
            if (_stackPanel == null || control is not FrameworkElement fe)
                return null;

            var position = new Point(Canvas.GetLeft(fe), Canvas.GetTop(fe));
            var size = new Size(fe.Width, fe.Height);

            string? labelText = null, sourceText = null;
            string? labelColor = null, sourceColor = null;
            string? lineColor = null;
            string? labelFontSize = null, labelFontWeight = null;
            string? sourceFontSize = null, sourceFontWeight = null;

            if (_stackPanel.Tag is Dictionary<string, object> tag)
            {
                if (tag.TryGetValue("LabelText", out var lblObj) && lblObj is TextBox label)
                {
                    labelText = label.Text;
                    labelColor = (label.Foreground as SolidColorBrush)?.Color.ToString();
                    labelFontSize = label.FontSize.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    labelFontWeight = label.FontWeight.ToString();
                }

                if (tag.TryGetValue("SourceLabel", out var srcObj) && srcObj is TextBox source)
                {
                    sourceText = source.Text;
                    sourceColor = (source.Foreground as SolidColorBrush)?.Color.ToString();
                    sourceFontSize = source.FontSize.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    sourceFontWeight = source.FontWeight.ToString();
                }

                if (tag.TryGetValue("LeftLine", out var leftObj) && leftObj is Rectangle line)
                {
                    lineColor = (line.Fill as SolidColorBrush)?.Color.ToString();
                }
            }

            return new BPMNShapeModelWithPosition
            {
                Type = ShapeType.ConnectorDoubleLabelLeft,
                Left = position.X,
                Top = position.Y,
                Width = size.Width,
                Height = size.Height,
                Name = fe.Name,
                Category = "Connector",
                SvgUri = null,
                ExtraProperties = new Dictionary<string, string>
                {
                    { "LabelText", labelText ?? "" },
                    { "LabelColor", labelColor ?? "" },
                    { "LabelFontSize", labelFontSize ?? "" },
                    { "LabelFontWeight", labelFontWeight ?? "" },
                    { "SourceText", sourceText ?? "" },
                    { "SourceColor", sourceColor ?? "" },
                    { "SourceFontSize", sourceFontSize ?? "" },
                    { "SourceFontWeight", sourceFontWeight ?? "" },
                    { "LineColor", lineColor ?? "" }
                }
            };
        }

        public void Restore(Dictionary<string, string> extraProperties)
        {
            var weightConverter = new FontWeightConverter();
            if (_stackPanel?.Tag is not Dictionary<string, object> tag) return;

            if (tag.TryGetValue("LabelText", out var labelObj) && labelObj is TextBox label)
            {
                if (extraProperties.TryGetValue("LabelText", out var labelText))
                    label.Text = labelText;

                if (extraProperties.TryGetValue("LabelColor", out var labelColor))
                    label.Foreground = ShapeStyleRestorer.ConvertToBrush(labelColor);

                if (extraProperties.TryGetValue("LabelFontSize", out var fontSizeStr) &&
        double.TryParse(fontSizeStr, System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out var fontSize))
                    label.FontSize = fontSize;

                if (extraProperties.TryGetValue("LabelFontWeight", out var fontWeightStr))
                {
                    try { label.FontWeight = (FontWeight)weightConverter.ConvertFromString(fontWeightStr); }
                    catch { label.FontWeight = FontWeights.Normal; }
                }
            }

            if (tag.TryGetValue("SourceLabel", out var sourceObj) && sourceObj is TextBox source)
            {
                if (extraProperties.TryGetValue("SourceText", out var sourceText))
                    source.Text = sourceText;

                if (extraProperties.TryGetValue("SourceColor", out var sourceColor))
                    source.Foreground = ShapeStyleRestorer.ConvertToBrush(sourceColor);

                if (extraProperties.TryGetValue("SourceFontSize", out var fontSizeStr) &&
        double.TryParse(fontSizeStr, System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out var fontSize))
                    source.FontSize = fontSize;

                if (extraProperties.TryGetValue("SourceFontWeight", out var fontWeightStr))
                {
                    try { source.FontWeight = (FontWeight)weightConverter.ConvertFromString(fontWeightStr); }
                    catch { source.FontWeight = FontWeights.Normal; }
                }
            }

            if (extraProperties.TryGetValue("LineColor", out var lineColor))
            {
                var brush = ShapeStyleRestorer.ConvertToBrush(lineColor);

                if (tag.TryGetValue("LeftLine", out var left) && left is Shape leftLine)
                    leftLine.Fill = brush;

                if (tag.TryGetValue("RightLine", out var right) && right is Shape rightLine)
                    rightLine.Fill = brush;

                if (tag.TryGetValue("Arrow", out var arrow) && arrow is Shape arrowShape)
                    arrowShape.Fill = brush;
            }
        }

        private bool IsMouseOver(UIElement element, MouseEventArgs e)
        {
            var pos = e.GetPosition(element);
            var rect = new Rect(0, 0, element.RenderSize.Width, element.RenderSize.Height);
            return rect.Contains(pos);
        }
    }
}
