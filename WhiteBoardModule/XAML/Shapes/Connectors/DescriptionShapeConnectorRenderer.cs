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

namespace WhiteBoardModule.XAML.Shapes.Connectors
{
    public class DescriptionShapeConnectorRenderer : IShapeRenderer
    {
        private readonly bool _withBindings;

        public DescriptionShapeConnectorRenderer(bool withBindings = false)
        {
            _withBindings = withBindings;
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
    }
}
