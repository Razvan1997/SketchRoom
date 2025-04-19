using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;
using WhiteBoard.Core.Services.Interfaces;

namespace WhiteBoardModule.XAML.Shapes.Containers
{
    public class ListContainerRenderer : IShapeRenderer
    {
        private readonly bool _withBindings;

        public ListContainerRenderer(bool withBindings = false)
        {
            _withBindings = withBindings;
        }

        public UIElement CreatePreview()
        {
            var grid = CreateListUI(isPreview: true);
            return new Viewbox
            {
                Width = 60,
                Height = 60,
                Stretch = Stretch.Uniform,
                Child = grid
            };
        }

        public UIElement Render() => CreateListUI();

        private UIElement CreateListUI(bool isPreview = false)
        {
            var preferences = ContainerLocator.Container.Resolve<IDrawingPreferencesService>();

            var stack = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Background = Brushes.Transparent
            };

            var titleBox = new TextBox
            {
                Text = "List",
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)),
                Foreground = Brushes.White,
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1),
                Margin = new Thickness(2),
                TextAlignment = TextAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                IsReadOnly = isPreview
            };
            stack.Children.Add(titleBox);

            // Container pentru itemi
            var itemsPanel = new StackPanel
            {
                Name = "ItemsPanel"
            };

            for (int i = 0; i < 3; i++)
            {
                itemsPanel.Children.Add(CreateItem(preferences, $"Item {i + 1}", isPreview));
            }

            stack.Children.Add(itemsPanel);

            if (!isPreview)
            {
                var addButton = new Button
                {
                    Content = "+",
                    Margin = new Thickness(2),
                    Width = 24,
                    Height = 24,
                    Background = new SolidColorBrush(Color.FromRgb(65, 65, 68)),
                    Foreground = Brushes.White,
                    BorderBrush = Brushes.Gray,
                    BorderThickness = new Thickness(1),
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                addButton.Click += (s, e) =>
                {
                    itemsPanel.Children.Insert(itemsPanel.Children.Count,
                        CreateItem(preferences, $"Item {itemsPanel.Children.Count + 1}", false));
                };

                stack.Children.Add(addButton);
            }

            var border = new Border
            {
                BorderBrush = Brushes.DeepSkyBlue,
                BorderThickness = new Thickness(1.5),
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(4),
                Child = stack
            };

            return border;
        }

        private UIElement CreateItem(IDrawingPreferencesService preferences, string text, bool isPreview)
        {
            return new TextBox
            {
                Text = text,
                FontSize = preferences.FontSize,
                FontWeight = preferences.FontWeight,
                Foreground = Brushes.White,
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(4, 2, 0, 2),
                Margin = new Thickness(2),
                IsReadOnly = isPreview
            };
        }
    }
}
