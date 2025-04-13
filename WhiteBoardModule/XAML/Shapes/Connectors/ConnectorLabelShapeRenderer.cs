using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows;
using WhiteBoard.Core.Services.Interfaces;
using System.Windows.Data;

namespace WhiteBoardModule.XAML.Shapes.Connectors
{
    public class ConnectorLabelShapeRenderer : IShapeRenderer
    {
        private readonly bool _withBindings;

        public ConnectorLabelShapeRenderer(bool withBindings = false)
        {
            _withBindings = withBindings;
        }

        public UIElement CreatePreview()
        {
            var label = new TextBlock
            {
                Text = "Label",
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.Black,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var leftLine = new Rectangle
            {
                Height = 2,
                Fill = Brushes.Black,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Center
            };

            var rightLine = new Rectangle
            {
                Height = 2,
                Fill = Brushes.Black,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Center
            };

            var arrow = new Polygon
            {
                Points = new PointCollection
        {
            new Point(0, 0),
            new Point(10, 5),
            new Point(0, 10)
        },
                Fill = Brushes.Black,
                Width = 10,
                Height = 10,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            var grid = new Grid
            {
                Width = 100,
                Height = 40
            };

            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            Grid.SetColumn(leftLine, 0);
            Grid.SetColumn(label, 1);
            Grid.SetColumn(rightLine, 2);
            Grid.SetColumn(arrow, 3);

            grid.Children.Add(leftLine);
            grid.Children.Add(label);
            grid.Children.Add(rightLine);
            grid.Children.Add(arrow);

            return new Viewbox
            {
                Width = 80,
                Height = 80,
                Stretch = Stretch.Uniform,
                Child = grid
            };
        }

        public UIElement Render()
        {
            var preferences = ContainerLocator.Container.Resolve<IDrawingPreferencesService>();

            // Creează TextBox (label)
            var labelBox = new TextBox
            {
                Text = "Label",
                FontSize = preferences.FontSize,
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

            labelBox.GotFocus += (s, e) =>
            {
                labelBox.Foreground = preferences.SelectedColor; // sau orice culoare vrei tu
            };

            if (_withBindings)
            {
                labelBox.SetBinding(TextBox.FontWeightProperty, new Binding(nameof(preferences.FontWeight)) { Source = preferences });
                labelBox.SetBinding(TextBox.FontSizeProperty, new Binding(nameof(preferences.FontSize)) { Source = preferences });
                labelBox.SetBinding(TextBox.ForegroundProperty, new Binding(nameof(preferences.SelectedColor)) { Source = preferences });
            }

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

            var arrow = new Polygon
            {
                Points = new PointCollection
                {
                    new Point(0, 0),
                    new Point(10, 5),
                    new Point(0, 10)
                },
                Fill = preferences.SelectedColor,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right,
                Width = 10,
                Height = 10,
                Name = "ConnectorArrow"
            };

            var grid = new Grid
            {
                VerticalAlignment = VerticalAlignment.Center,
                Height = 40
            };

            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            Grid.SetColumn(leftLine, 0);
            Grid.SetColumn(labelBox, 1);
            Grid.SetColumn(rightLine, 2);
            Grid.SetColumn(arrow, 3);

            grid.Children.Add(leftLine);
            grid.Children.Add(labelBox);
            grid.Children.Add(rightLine);
            grid.Children.Add(arrow);

            grid.Tag = new Dictionary<string, object>
            {
                { "LabelText", labelBox },
                { "LeftLine", leftLine },
                { "RightLine", rightLine },
                { "Arrow", arrow }
            };

            return grid;
        }
    }
}
