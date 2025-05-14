using SketchRoom.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WhiteBoard.Core.Models;
using WhiteBoard.Core.Services.Interfaces;

namespace WhiteBoardModule.XAML.Shapes.Containers
{
    public class HorizontalPoolLaneOneRenderer : IShapeRenderer, IRestoreFromShape
    {
        private readonly bool _withBindings;
        private readonly IShapeSelectionService _selectionService;
        private Grid? _renderedGrid;
        public HorizontalPoolLaneOneRenderer(bool withBindings = false)
        {
            _withBindings = withBindings;
            _selectionService = ContainerLocator.Container.Resolve<IShapeSelectionService>();
        }

        public UIElement CreatePreview()
        {
            var previewGrid = new Grid
            {
                Width = 60,
                Height = 60,
                Background = Brushes.Transparent
            };

            // Rând pentru Pool + 3 lane-uri
            previewGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            for (int i = 0; i < 3; i++)
                previewGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // Coloane: lane label + content
            previewGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            previewGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var poolText = new TextBlock
            {
                Text = "Pool",
                FontSize = 8,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var poolBorder = new Border
            {
                BorderBrush = Brushes.White,
                BorderThickness = new Thickness(2),
                Background = Brushes.Transparent,
                Child = poolText
            };

            Grid.SetRow(poolBorder, 0);
            Grid.SetColumnSpan(poolBorder, 2);
            previewGrid.Children.Add(poolBorder);

            for (int i = 0; i < 3; i++)
            {
                var laneText = new TextBlock
                {
                    Text = $"Lane {i + 1}",
                    FontSize = 6,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White,
                    LayoutTransform = new RotateTransform(-90),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var sideBorder = new Border
                {
                    Background = Brushes.Black,
                    BorderBrush = Brushes.White,
                    BorderThickness = new Thickness(2),
                    Width = 10,
                    Child = laneText
                };

                Grid.SetRow(sideBorder, i + 1);
                Grid.SetColumn(sideBorder, 0);
                previewGrid.Children.Add(sideBorder);

                var contentBorder = new Border
                {
                    BorderBrush = Brushes.White,
                    BorderThickness = new Thickness(1),
                    Background = Brushes.Transparent
                };

                Grid.SetRow(contentBorder, i + 1);
                Grid.SetColumn(contentBorder, 1);
                previewGrid.Children.Add(contentBorder);
            }

            return new Viewbox
            {
                Width = 48,
                Height = 48,
                Stretch = Stretch.Uniform,
                Child = previewGrid
            };
        }

        public UIElement Render()
        {
            var preferences = ContainerLocator.Container.Resolve<IDrawingPreferencesService>();

            var outerGrid = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Background = Brushes.Transparent
            };

            // Row for "Pool" title
            outerGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // 3 lane rows
            for (int i = 0; i < 3; i++)
                outerGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // Columns: lane label + content
            outerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            outerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // --- Pool title (editable TextBox) ---
            var poolTextBox = new TextBox
            {
                Text = "Pool",
                Name = "PoolTextBox",
                FontWeight = FontWeights.Bold,
                FontSize = preferences.FontSize + 2,
                Foreground = Brushes.White,
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                Padding = new Thickness(4),
                IsReadOnly = false
            };

            poolTextBox.PreviewMouseLeftButtonDown += (s, e) =>
            {
                _selectionService.Select(ShapePart.Text, poolTextBox);
            };
            var poolBorder = new Border
            {
                BorderBrush = Brushes.White,
                BorderThickness = new Thickness(2),
                Background = Brushes.Transparent,
                Child = poolTextBox
            };

            Grid.SetRow(poolBorder, 0);
            Grid.SetColumnSpan(poolBorder, 2);
            outerGrid.Children.Add(poolBorder);

            // --- Lane labels and content borders ---
            for (int i = 0; i < 3; i++)
            {
                var laneTextBox = new TextBox
                {
                    Text = $"Lane {i + 1}",
                    Name = $"Lane{i + 1}TextBox",
                    FontWeight = preferences.FontWeight,
                    FontSize = preferences.FontSize,
                    Foreground = Brushes.White,
                    Background = Brushes.Transparent,
                    BorderBrush = Brushes.Transparent,
                    TextAlignment = TextAlignment.Center,
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    LayoutTransform = new RotateTransform(-90),
                    IsReadOnly = false
                };
                laneTextBox.PreviewMouseLeftButtonDown += (s, e) =>
                {
                    _selectionService.Select(ShapePart.Text, laneTextBox);
                };

                var sideBorder = new Border
                {
                    Background = Brushes.Black,
                    BorderBrush = Brushes.White,
                    BorderThickness = new Thickness(2),
                    Width = 30,
                    Child = laneTextBox
                };

                Grid.SetRow(sideBorder, i + 1);
                Grid.SetColumn(sideBorder, 0);
                outerGrid.Children.Add(sideBorder);

                var contentBorder = new Border
                {
                    BorderBrush = Brushes.White,
                    BorderThickness = new Thickness(2),
                    Background = Brushes.Transparent
                };

                contentBorder.PreviewMouseLeftButtonDown += (s, e) =>
                {
                    var border = (Border)s;
                    var pos = e.GetPosition(border);

                    if (IsMouseOverMargin(outerGrid, pos))
                        _selectionService.Select(ShapePart.Margin, contentBorder);
                    else
                        _selectionService.Select(ShapePart.Border, contentBorder);
                };

                Grid.SetRow(contentBorder, i + 1);
                Grid.SetColumn(contentBorder, 1);
                outerGrid.Children.Add(contentBorder);
            }
            _renderedGrid = outerGrid;
            return outerGrid;
        }

        private bool IsMouseOverMargin(Grid grid, Point mousePos)
        {
            const double marginWidth = 6;

            return mousePos.X < marginWidth ||
                   mousePos.X > grid.ActualWidth - marginWidth ||
                   mousePos.Y < marginWidth ||
                   mousePos.Y > grid.ActualHeight - marginWidth;
        }

        public BPMNShapeModelWithPosition? ExportData(IInteractiveShape control)
        {
            if (control is not FrameworkElement fe || _renderedGrid == null)
                return null;

            var position = new Point(Canvas.GetLeft(fe), Canvas.GetTop(fe));
            var size = new Size(fe.Width, fe.Height);

            var extra = new Dictionary<string, string>();

            foreach (var child in _renderedGrid.Children)
            {
                if (child is Border border)
                {
                    int row = Grid.GetRow(border);
                    int col = Grid.GetColumn(border);

                    if (border.Child is TextBox tb)
                    {
                        string prefix = row == 0 ? "Pool" : $"Lane{row}";

                        extra[$"{prefix}Name"] = tb.Text;

                        if (tb.Foreground is SolidColorBrush fgBrush)
                            extra[$"{prefix}Foreground"] = fgBrush.Color.ToString();

                        if (border.Background is SolidColorBrush bgBrush)
                            extra[$"{prefix}Background"] = bgBrush.Color.ToString();
                    }
                    else if (col == 1 && row >= 1 && row <= 3)
                    {
                        // Border de content (fără TextBox)
                        string prefix = $"Lane{row}";
                        if (border.Background is SolidColorBrush bgBrush)
                            extra[$"{prefix}ContentBackground"] = bgBrush.Color.ToString();
                    }
                }
            }

            return new BPMNShapeModelWithPosition
            {
                Type = ShapeType.ContainerHorizontalPoolLineOneShape,
                Left = position.X,
                Top = position.Y,
                Width = size.Width,
                Height = size.Height,
                Name = fe.Name,
                Category = "Pool",
                SvgUri = null,
                ExtraProperties = extra
            };
        }

        public void Restore(Dictionary<string, string> extraProperties)
        {
            if (_renderedGrid == null) return;

            foreach (var child in _renderedGrid.Children.OfType<Border>())
            {
                int row = Grid.GetRow(child);
                int col = Grid.GetColumn(child);

                if (child.Child is TextBox tb)
                {
                    string? prefix = tb.Name switch
                    {
                        "PoolTextBox" => "Pool",
                        "Lane1TextBox" => "Lane1",
                        "Lane2TextBox" => "Lane2",
                        "Lane3TextBox" => "Lane3",
                        _ => null
                    };

                    if (prefix == null)
                        continue;

                    // Restore text
                    if (extraProperties.TryGetValue($"{prefix}Name", out var text))
                        tb.Text = text;

                    // Restore foreground
                    if (extraProperties.TryGetValue($"{prefix}Foreground", out var fg) &&
                        ColorConverter.ConvertFromString(fg) is Color fgColor)
                    {
                        tb.Foreground = new SolidColorBrush(fgColor);
                    }

                    // Restore background (label area)
                    if (extraProperties.TryGetValue($"{prefix}Background", out var bg) &&
                        ColorConverter.ConvertFromString(bg) is Color bgColor)
                    {
                        child.Background = new SolidColorBrush(bgColor);
                    }
                }
                else if (col == 1 && row >= 1 && row <= 3)
                {
                    // Restore background for lane content area
                    string key = $"Lane{row}ContentBackground";
                    if (extraProperties.TryGetValue(key, out var bg) &&
                        ColorConverter.ConvertFromString(bg) is Color bgColor)
                    {
                        child.Background = new SolidColorBrush(bgColor);
                    }
                }
            }
        }
    }
}
