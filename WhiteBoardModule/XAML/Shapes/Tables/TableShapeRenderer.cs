using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;
using WhiteBoard.Core.Services.Interfaces;

namespace WhiteBoardModule.XAML.Shapes.Tables
{
    public class TableShapeRenderer : IShapeRenderer
    {
        public UIElement CreatePreview()
        {
            var previewGrid = new Grid
            {
                Width = 60,
                Height = 60,
                Background = Brushes.Transparent,
                ShowGridLines = false
            };

            for (int i = 0; i < 3; i++)
            {
                previewGrid.ColumnDefinitions.Add(new ColumnDefinition());
                previewGrid.RowDefinitions.Add(new RowDefinition());
            }

            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 3; col++)
                {
                    var cell = new Border
                    {
                        BorderBrush = Brushes.Black,
                        BorderThickness = new Thickness(0.5)
                    };

                    Grid.SetRow(cell, row);
                    Grid.SetColumn(cell, col);
                    previewGrid.Children.Add(cell);
                }
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
            var grid = new Grid
            {
                Background = Brushes.Transparent,
                ShowGridLines = true,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            for (int i = 0; i < 3; i++)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition());
                grid.RowDefinitions.Add(new RowDefinition());
            }

            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 3; col++)
                {
                    var cell = new Border
                    {
                        BorderBrush = Brushes.Black,
                        BorderThickness = new Thickness(0.5),
                        Child = new TextBlock
                        {
                            Text = $"R{row + 1}C{col + 1}",
                            FontSize = 14,
                            Foreground = Brushes.Black,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                            TextWrapping = TextWrapping.Wrap
                        }
                    };

                    Grid.SetRow(cell, row);
                    Grid.SetColumn(cell, col);
                    grid.Children.Add(cell);
                }
            }

            return new Viewbox
            {
                Stretch = Stretch.Uniform,
                Child = grid
            };
        }
    }
}
