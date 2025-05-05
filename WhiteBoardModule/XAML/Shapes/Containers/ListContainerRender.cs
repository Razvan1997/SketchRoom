using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;
using WhiteBoard.Core.Services.Interfaces;
using SketchRoom.Models.Enums;
using System.Windows.Input;
using System.Windows.Shapes;
using WhiteBoard.Core.Events;
using WhiteBoard.Core.Models;

namespace WhiteBoardModule.XAML.Shapes.Containers
{
    public class ListContainerRenderer : IShapeRenderer, IRestoreFromShape
    {
        private readonly bool _withBindings;
        private readonly IShapeSelectionService _selectionService;
        public event EventHandler<ConnectionPointEventArgs>? ConnectionPointClicked;
        public event EventHandler<ConnectionPointEventArgs>? ConnectionPointTargetClicked;
        private Border? _renderedBorder;
        public ListContainerRenderer(bool withBindings = false)
        {
            _withBindings = withBindings;
            _selectionService = ContainerLocator.Container.Resolve<IShapeSelectionService>();
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

        public UIElement Render()
        {
            _renderedBorder = CreateListUI() as Border;
            return _renderedBorder!;
        }

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

            if (!isPreview)
            {
                titleBox.PreviewMouseLeftButtonDown += (s, e) =>
                {
                    _selectionService.Select(ShapePart.Text, (UIElement)s);
                };
            }

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
            var grid = new Grid
            {
                Margin = new Thickness(2),
                Background = Brushes.Transparent
            };

            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) }); // Left connector
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // TextBox
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) }); // Right connector

            // 🔵 Left connector
            var leftConnector = new Rectangle
            {
                Width = 8,
                Height = 12,
                Fill = Brushes.DodgerBlue,
                Visibility = Visibility.Collapsed,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Cursor = Cursors.Cross,
                Tag = "Connector"
            };

            if (!isPreview)
            {
                leftConnector.MouseLeftButtonDown += (s, e) =>
                {
                    ConnectionPointClicked?.Invoke(this, new ConnectionPointEventArgs("Left", leftConnector, e));
                    e.Handled = true;
                };
            }

            Grid.SetColumn(leftConnector, 0);
            grid.Children.Add(leftConnector);

            // 📝 TextBox
            var itemBox = new TextBox
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

            if (!isPreview)
            {
                itemBox.PreviewMouseLeftButtonDown += (s, e) =>
                {
                    _selectionService.Select(ShapePart.Text, (UIElement)s);
                };
            }

            Grid.SetColumn(itemBox, 1);
            grid.Children.Add(itemBox);

            // 🔵 Right connector
            var rightConnector = new Rectangle
            {
                Width = 8,
                Height = 12,
                Fill = Brushes.DodgerBlue,
                Visibility = Visibility.Collapsed,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Cursor = Cursors.Cross,
                Tag = "Connector"
            };

            if (!isPreview)
            {
                rightConnector.MouseLeftButtonDown += (s, e) =>
                {
                    ConnectionPointClicked?.Invoke(this, new ConnectionPointEventArgs("Right", rightConnector, e));
                    e.Handled = true;
                };
            }

            Grid.SetColumn(rightConnector, 2);
            grid.Children.Add(rightConnector);

            if (!isPreview)
            {
                grid.MouseEnter += (_, _) =>
                {
                    leftConnector.Visibility = Visibility.Visible;
                    rightConnector.Visibility = Visibility.Visible;
                };
                grid.MouseLeave += (_, _) =>
                {
                    leftConnector.Visibility = Visibility.Collapsed;
                    rightConnector.Visibility = Visibility.Collapsed;
                };
            }

            return grid;
        }

        public BPMNShapeModelWithPosition? ExportData(IInteractiveShape control)
        {
            if (control is not FrameworkElement fe)
                return null;

            var position = new Point(Canvas.GetLeft(fe), Canvas.GetTop(fe));
            var size = new Size(fe.Width, fe.Height);

            var extraProps = new Dictionary<string, string>();

            if (fe is Border border && border.Child is StackPanel stack)
            {
                // Titlu
                if (stack.Children[0] is TextBox titleBox)
                {
                    extraProps["Title"] = titleBox.Text;
                }

                // Items
                if (stack.Children.OfType<StackPanel>().FirstOrDefault(p => p.Name == "ItemsPanel") is StackPanel itemsPanel)
                {
                    int index = 1;
                    foreach (var item in itemsPanel.Children.OfType<Grid>())
                    {
                        var textBox = item.Children.OfType<TextBox>().FirstOrDefault();
                        if (textBox != null)
                            extraProps[$"Item{index++}"] = textBox.Text;
                    }
                }
            }

            return new BPMNShapeModelWithPosition
            {
                Type = ShapeType.ListContainerShape,
                Left = position.X,
                Top = position.Y,
                Width = size.Width,
                Height = size.Height,
                Name = fe.Name,
                Category = "Container",
                SvgUri = null,
                ExtraProperties = extraProps
            };
        }

        public void Restore(Dictionary<string, string> extraProperties)
        {
            if (_renderedBorder?.Child is not StackPanel stack)
                return;

            // Restore titlu
            if (stack.Children[0] is TextBox titleBox &&
                extraProperties.TryGetValue("Title", out var title))
            {
                titleBox.Text = title;
            }

            // Restore itemi
            if (stack.Children.OfType<StackPanel>().FirstOrDefault(p => p.Name == "ItemsPanel") is StackPanel itemsPanel)
            {
                itemsPanel.Children.Clear();
                int i = 1;
                while (extraProperties.TryGetValue($"Item{i}", out var itemText))
                {
                    var item = CreateItem(ContainerLocator.Container.Resolve<IDrawingPreferencesService>(), itemText, false);
                    itemsPanel.Children.Add(item);
                    i++;
                }
            }
        }
    }
}
