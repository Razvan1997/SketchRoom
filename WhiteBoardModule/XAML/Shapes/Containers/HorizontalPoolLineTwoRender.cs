using SketchRoom.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WhiteBoard.Core.Models;
using WhiteBoard.Core.Services.Interfaces;

namespace WhiteBoardModule.XAML.Shapes.Containers
{
    public class HorizontalPoolLineTwoRender : IShapeRenderer
    {
        private readonly bool _withBindings;
        private readonly IShapeSelectionService _selectionService;
        private Grid? _renderedGrid;
        public HorizontalPoolLineTwoRender(bool withBindings = false)
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

            // 3 lane-uri
            for (int i = 0; i < 3; i++)
                previewGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // Coloane: Pool + Lane
            previewGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            previewGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            previewGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Pool label
            var poolLabel = new TextBlock
            {
                Text = "Pool",
                FontSize = 6,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                LayoutTransform = new RotateTransform(-90),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var poolBorder = new Border
            {
                Background = Brushes.Black,
                BorderBrush = Brushes.White,
                BorderThickness = new Thickness(2),
                Width = 10,
                Child = poolLabel
            };

            Grid.SetColumn(poolBorder, 0);
            Grid.SetRowSpan(poolBorder, 3);
            previewGrid.Children.Add(poolBorder);

            for (int i = 0; i < 3; i++)
            {
                var laneLabel = new TextBlock
                {
                    Text = $"Lane {i + 1}",
                    FontSize = 6,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White,
                    LayoutTransform = new RotateTransform(-90),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var laneBorder = new Border
                {
                    Background = Brushes.Black,
                    BorderBrush = Brushes.White,
                    BorderThickness = new Thickness(2),
                    Width = 10,
                    Child = laneLabel
                };

                Grid.SetRow(laneBorder, i);
                Grid.SetColumn(laneBorder, 1);
                previewGrid.Children.Add(laneBorder);

                var contentBorder = new Border
                {
                    BorderBrush = Brushes.White,
                    BorderThickness = new Thickness(2),
                    Background = Brushes.Transparent
                };

                Grid.SetRow(contentBorder, i);
                Grid.SetColumn(contentBorder, 2);
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

            var grid = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Background = Brushes.Transparent
            };

            // 3 lane-uri
            for (int i = 0; i < 3; i++)
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // Coloane: Pool, Lane labels, Content
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Pool TextBox (rotit)
            var poolBox = new TextBox
            {
                Text = "Pool",
                FontWeight = FontWeights.Bold,
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

            poolBox.PreviewMouseLeftButtonDown += (s, e) =>
            {
                _selectionService.Select(ShapePart.Text, poolBox);
            };

            var poolBorder = new Border
            {
                Background = Brushes.Black,
                BorderBrush = Brushes.White,
                BorderThickness = new Thickness(2),
                Width = 30,
                Child = poolBox
            };

            Grid.SetColumn(poolBorder, 0);
            Grid.SetRowSpan(poolBorder, 3);
            grid.Children.Add(poolBorder);

            for (int i = 0; i < 3; i++)
            {
                var laneBox = new TextBox
                {
                    Text = $"Lane {i + 1}",
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

                laneBox.PreviewMouseLeftButtonDown += (s, e) =>
                {
                    _selectionService.Select(ShapePart.Text, laneBox);
                };

                var laneBorder = new Border
                {
                    Background = Brushes.Black,
                    BorderBrush = Brushes.White,
                    BorderThickness = new Thickness(2),
                    Width = 30,
                    Child = laneBox
                };

                Grid.SetRow(laneBorder, i);
                Grid.SetColumn(laneBorder, 1);
                grid.Children.Add(laneBorder);

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

                    if (IsMouseOverMargin(border, pos))
                        _selectionService.Select(ShapePart.Margin, border);
                    else
                        _selectionService.Select(ShapePart.Border, border);
                };

                Grid.SetRow(contentBorder, i);
                Grid.SetColumn(contentBorder, 2);
                grid.Children.Add(contentBorder);
            }

            return grid;
        }

        private bool IsMouseOverMargin(Border border, Point mousePos)
        {
            const double marginWidth = 6;

            return mousePos.X < marginWidth ||
                   mousePos.X > border.ActualWidth - marginWidth ||
                   mousePos.Y < marginWidth ||
                   mousePos.Y > border.ActualHeight - marginWidth;
        }

        public BPMNShapeModelWithPosition? ExportData(IInteractiveShape control)
        {
            if (control is not FrameworkElement fe)
                return null;

            if (fe is not Grid grid)
                return null;

            var position = new Point(Canvas.GetLeft(fe), Canvas.GetTop(fe));
            var size = new Size(fe.Width, fe.Height);

            string poolText = "";
            var laneTexts = new List<string>();

            foreach (var child in grid.Children)
            {
                if (child is Border border && border.Child is TextBox tb)
                {
                    int column = Grid.GetColumn(border);
                    int row = Grid.GetRow(border);

                    if (column == 0)
                        poolText = tb.Text;
                    else if (column == 1)
                        laneTexts.Add(tb.Text);
                }
            }

            return new BPMNShapeModelWithPosition
            {
                Type = ShapeType.ContainerHorizontalPoolLineTwoShape,
                Left = position.X,
                Top = position.Y,
                Width = size.Width,
                Height = size.Height,
                Name = fe.Name,
                Category = "Pool",
                SvgUri = null,
                ExtraProperties = new Dictionary<string, string>
        {
            { "PoolName", poolText },
            { "Lane1", laneTexts.ElementAtOrDefault(0) ?? "" },
            { "Lane2", laneTexts.ElementAtOrDefault(1) ?? "" },
            { "Lane3", laneTexts.ElementAtOrDefault(2) ?? "" }
        }
            };
        }

        public void Restore(Dictionary<string, string> extraProperties)
        {
            if (_renderedGrid == null) return;

            foreach (var child in _renderedGrid.Children.OfType<Border>())
            {
                if (child.Child is TextBox tb)
                {
                    if (tb.Name == "PoolTextBox" && extraProperties.TryGetValue("PoolName", out var poolText))
                        tb.Text = poolText;

                    if (tb.Name == "Lane1TextBox" && extraProperties.TryGetValue("Lane1", out var lane1))
                        tb.Text = lane1;

                    if (tb.Name == "Lane2TextBox" && extraProperties.TryGetValue("Lane2", out var lane2))
                        tb.Text = lane2;

                    if (tb.Name == "Lane3TextBox" && extraProperties.TryGetValue("Lane3", out var lane3))
                        tb.Text = lane3;
                }
            }
        }
    }
}
