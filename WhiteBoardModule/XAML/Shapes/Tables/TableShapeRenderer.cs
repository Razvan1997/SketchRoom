using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;
using WhiteBoard.Core.Services.Interfaces;
using Microsoft.ML.Transforms;
using System.Windows.Input;
using System.Windows.Shapes;

namespace WhiteBoardModule.XAML.Shapes.Tables
{
    public class TableShapeRenderer : IShapeRenderer, IShapeTableProvider
    {
        private EditableTableControl? _editableTableControl;
        private Brush _headerBackground = Brushes.Black;
        private Brush _headerForeground = Brushes.White;
        private Brush _cellBackground = Brushes.White;
        private Brush _cellForeground = Brushes.Black;

        private int _rows = 3;
        private int _columns = 3;
        private string[,] _cellValues;
        private Grid _grid;
        private double _initialWidth;
        private double _initialHeight;

        public TableShapeRenderer()
        {
        }

        public void SetInitialSize(double width, double height)
        {
            _initialWidth = width;
            _initialHeight = height;
        }

        public UIElement CreatePreview()
        {
            var previewGrid = new Grid
            {
                Width = 60,
                Height = 60,
                Background = Brushes.White
            };

            for (int i = 0; i < _columns; i++)
                previewGrid.ColumnDefinitions.Add(new ColumnDefinition());

            for (int i = 0; i < _rows; i++)
                previewGrid.RowDefinitions.Add(new RowDefinition());

            for (int row = 0; row < _rows; row++)
                for (int col = 0; col < _columns; col++)
                    AddCell(previewGrid, row, col, isPreview: true);

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
            _editableTableControl = new EditableTableControl();
            return _editableTableControl;
        }
        public ITableShapeRender? TableShape => _editableTableControl;

        private void AddCell(Grid grid, int row, int col, bool isPreview = false)
        {
            var isHeader = row == 0;
            UIElement content;

            if (isPreview)
            {
                content = new TextBlock
                {
                    Text = "sample",
                    FontSize = 10,
                    Foreground = isHeader ? _headerForeground : _cellForeground,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextWrapping = TextWrapping.Wrap
                };
            }
            else
            {
                var textBox = new TextBox
                {
                    Text = _cellValues[row, col],
                    FontSize = 14,
                    MinWidth = 30,
                    MinHeight = 20,
                    Padding = new Thickness(4),
                    Foreground = isHeader ? _headerForeground : _cellForeground,
                    Background = isHeader ? _headerBackground : _cellBackground,
                    BorderThickness = new Thickness(0),
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center
                };

                int rIndex = row, cIndex = col;

                textBox.TextChanged += (s, e) => _cellValues[rIndex, cIndex] = textBox.Text;
                content = textBox;
            }

            var border = new Border
            {
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(0.5),
                Background = isHeader ? _headerBackground : _cellBackground,
                Child = content
            };

            Grid.SetRow(border, row);
            Grid.SetColumn(border, col);
            grid.Children.Add(border);
        }

    }
}
