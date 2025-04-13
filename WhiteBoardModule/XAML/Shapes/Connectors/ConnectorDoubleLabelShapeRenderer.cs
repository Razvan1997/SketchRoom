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
using WhiteBoardModule.XAML.Shapes.General;

namespace WhiteBoardModule.XAML.Shapes.Connectors
{
    public class ConnectorDoubleLabelShapeRenderer : IShapeRenderer
    {
        private readonly bool _withBindings;

        public ConnectorDoubleLabelShapeRenderer(bool withBindings = false)
        {
            _withBindings = withBindings;
        }

        public UIElement CreatePreview()
        {
            // Simulare simplificată a formei cu două etichete și săgeată

            var sourceLabel = new TextBlock
            {
                Text = "Source",
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.Black,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 20, 1)
            };

            var inlineLabel = new TextBlock
            {
                Text = "Label",
                FontSize = 10,
                FontWeight = FontWeights.Normal,
                Foreground = Brushes.Black,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var leftLine = new Rectangle
            {
                Height = 2,
                Fill = Brushes.Black,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            var rightLine = new Rectangle
            {
                Height = 2,
                Fill = Brushes.Black,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Stretch
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

            var lineGrid = new Grid
            {
                Height = 30,
                Width = 100
            };

            lineGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            lineGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            lineGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            lineGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            Grid.SetColumn(leftLine, 0);
            Grid.SetColumn(inlineLabel, 1);
            Grid.SetColumn(rightLine, 2);
            Grid.SetColumn(arrow, 3);

            lineGrid.Children.Add(leftLine);
            lineGrid.Children.Add(inlineLabel);
            lineGrid.Children.Add(rightLine);
            lineGrid.Children.Add(arrow);

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

        public UIElement Render()
        {
            var preferences = ContainerLocator.Container.Resolve<IDrawingPreferencesService>();

            // TextBox pentru Source (deasupra liniei)
            var sourceBox = new TextBox
            {
                Text = "Source",
                FontSize = preferences.FontSize,
                FontWeight = preferences.FontWeight,
                Foreground = preferences.SelectedColor,
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 0, 0, -10),
                MinWidth = 60
            };

            if (_withBindings)
            {
                sourceBox.SetBinding(TextBox.FontWeightProperty, new Binding(nameof(preferences.FontWeight)) { Source = preferences });
                sourceBox.SetBinding(TextBox.FontSizeProperty, new Binding(nameof(preferences.FontSize)) { Source = preferences });
                sourceBox.SetBinding(TextBox.ForegroundProperty, new Binding(nameof(preferences.SelectedColor)) { Source = preferences });
            }

            // TextBox pentru Label (în linie)
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
                labelBox.Foreground = preferences.SelectedColor;
            };

            if (_withBindings)
            {
                labelBox.SetBinding(TextBox.FontWeightProperty, new Binding(nameof(preferences.FontWeight)) { Source = preferences });
                labelBox.SetBinding(TextBox.FontSizeProperty, new Binding(nameof(preferences.FontSize)) { Source = preferences });
                labelBox.SetBinding(TextBox.ForegroundProperty, new Binding(nameof(preferences.SelectedColor)) { Source = preferences });
            }

            // Linii
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

            // Săgeată
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

            // Grid pentru săgeată
            var lineGrid = new Grid
            {
                Height = 40
            };
            lineGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            lineGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            lineGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            lineGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            Grid.SetColumn(leftLine, 0);
            Grid.SetColumn(labelBox, 1);
            Grid.SetColumn(rightLine, 2);
            Grid.SetColumn(arrow, 3);

            lineGrid.Children.Add(leftLine);
            lineGrid.Children.Add(labelBox);
            lineGrid.Children.Add(rightLine);
            lineGrid.Children.Add(arrow);

            // StackPanel vertical (sourceBox deasupra săgeții)
            var stackPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                VerticalAlignment = VerticalAlignment.Center
            };

            stackPanel.Children.Add(sourceBox);
            stackPanel.Children.Add(lineGrid);

            // Setări de binding dacă e cazul
            if (_withBindings)
            {
                sourceBox.SetBinding(TextBox.FontWeightProperty, new Binding(nameof(preferences.FontWeight)) { Source = preferences });
                sourceBox.SetBinding(TextBox.FontSizeProperty, new Binding(nameof(preferences.FontSize)) { Source = preferences });
                sourceBox.SetBinding(TextBox.ForegroundProperty, new Binding(nameof(preferences.SelectedColor)) { Source = preferences });

                labelBox.SetBinding(TextBox.FontWeightProperty, new Binding(nameof(preferences.FontWeight)) { Source = preferences });
                labelBox.SetBinding(TextBox.FontSizeProperty, new Binding(nameof(preferences.FontSize)) { Source = preferences });
            }

            // Tag pentru UpdateStyle()
            stackPanel.Tag = new Dictionary<string, object>
            {
                { "LabelText", labelBox },
                { "LeftLine", leftLine },
                { "RightLine", rightLine },
                { "Arrow", arrow },
                { "SourceLabel", sourceBox }
            };

            return stackPanel;
        }
    }
}
