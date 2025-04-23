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
using System.Windows.Threading;
using SharpVectors.Converters;
using System.Drawing;
using System.Windows.Ink;

namespace WhiteBoardModule.XAML.Shapes.Nodes
{
    public class AdvancedTreeShapeRenderer : IShapeRenderer
    {
        private readonly List<StackPanel> _nodes = new();

        public AdvancedTreeShapeRenderer(bool withBindings = false)
        {
            // Constructor păstrat
        }

        public UIElement CreatePreview()
        {
            var tree = Render();

            return new Viewbox
            {
                Width = 120,
                Height = 120,
                Child = tree
            };
        }

        public UIElement Render()
        {
            var rootPanel = new StackPanel();
            var container = new Border
            {
                BorderBrush = Brushes.DeepSkyBlue,
                BorderThickness = new Thickness(1.5),
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 30, 30)),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(4),
                Child = rootPanel
            };

            var rootNode = CreateNode("Solution", 0, true);
            rootPanel.Children.Add(rootNode);
            return container;
        }

        private StackPanel CreateNode(string name, int level, bool isRoot = false)
        {
            var nodeContainer = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 6, 0, 6) };

            var canvas = new Canvas { Width = 20, IsHitTestVisible = false };

            var contentLayout = new StackPanel();

            var bullet = new SvgViewbox
            {
                Width = 14,
                Height = 14,
                Stretch = Stretch.Uniform,
                Margin = new Thickness(2),
                Source = new Uri("pack://application:,,,/WhiteBoardModule;component/SVG/folder.svg"),
            };
            var color = GetRandomColor();
            var bulletContainer = new Border
            {
                Child = bullet,
                Background = new SolidColorBrush(color) // va fi moștenită ca currentColor
            };

            var nameBox = new TextBox
            {
                Text = name,
                BorderBrush = Brushes.Gray,
                Style = (Style)Application.Current.FindResource("DarkTextBoxStyle"),
                BorderThickness = new Thickness(1),
                Background = Brushes.Transparent,
                Foreground = Brushes.White,
                Margin = new Thickness(2, 0, 4, 0),
                MinWidth = 100
            };

            var addBtn = new Button
            {
                Content = "+",
                Width = 20,
                Height = 20,
                Margin = new Thickness(2),
                Background = Brushes.Transparent,
                Foreground = Brushes.LightGreen,
                BorderBrush = Brushes.Gray
            };

            var removeBtn = new Button
            {
                Content = "✖",
                Width = 20,
                Height = 20,
                Margin = new Thickness(2),
                Background = Brushes.Transparent,
                Foreground = Brushes.Red,
                BorderBrush = Brushes.Gray
            };

            var contentPanel = new StackPanel { Orientation = Orientation.Horizontal };
            contentPanel.Children.Add(bulletContainer);
            contentPanel.Children.Add(nameBox);
            contentPanel.Children.Add(addBtn);
            contentPanel.Children.Add(removeBtn);

            var childrenPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 0) };
            contentLayout.Children.Add(contentPanel);
            contentLayout.Children.Add(childrenPanel);

            nodeContainer.Children.Add(canvas);
            nodeContainer.Children.Add(contentLayout);

            var metadata = new TreeNodeMetadata
            {
                Canvas = canvas,
                Bullet = bullet,
                ChildrenPanel = childrenPanel,
                LineColor = color
            };
            nodeContainer.Tag = metadata;
            _nodes.Add(nodeContainer);

            addBtn.Click += (s, e) =>
            {
                var child = CreateNode("Child", level + 1);
                childrenPanel.Children.Add(child);
                RedrawAllConnections();
            };

            removeBtn.Click += (s, e) =>
            {
                if (nodeContainer.Parent is Panel parent)
                {
                    parent.Children.Remove(nodeContainer);
                    _nodes.Remove(nodeContainer);
                    RedrawAllConnections();
                }
            };

            return nodeContainer;
        }

        private void RedrawAllConnections()
        {
            foreach (var node in _nodes)
            {
                if (node.Tag is not TreeNodeMetadata metadata) continue;

                metadata.Bullet.Dispatcher.InvokeAsync(() =>
                {
                    metadata.Bullet.UpdateLayout();
                    RedrawConnectionsForNode(metadata);
                }, DispatcherPriority.Render);
            }
        }

        private void RedrawConnectionsForNode(TreeNodeMetadata meta)
        {
            var canvas = meta.Canvas;
            var bullet = meta.Bullet;
            var childrenPanel = meta.ChildrenPanel;

            canvas.Children.Clear();

            if (!bullet.IsLoaded || PresentationSource.FromVisual(bullet) == null)
                return;

            var parentCenter = bullet.PointToScreen(new System.Windows.Point(bullet.Width / 2, bullet.Height / 2));
            parentCenter = canvas.PointFromScreen(parentCenter);

            var children = childrenPanel.Children.OfType<StackPanel>()
                .Where(c => c.Tag is TreeNodeMetadata)
                .ToList();

            if (children.Count == 0) return; // 🔥 Do NOT draw anything if no children

            var childCenters = children.Select(child =>
            {
                var childMeta = (TreeNodeMetadata)child.Tag;
                var bulletChild = childMeta.Bullet;
                var center = bulletChild.PointToScreen(new System.Windows.Point(bulletChild.Width / 2, bulletChild.Height / 2));
                return canvas.PointFromScreen(center);
            }).ToList();

            double verticalX = parentCenter.X;
            double yTop = childCenters.Min(p => p.Y);
            double yBottom = childCenters.Max(p => p.Y);

            // 1. Linie verticală de la părinte până la cel mai jos copil
            canvas.Children.Add(new Line
            {
                X1 = verticalX,
                Y1 = parentCenter.Y,
                X2 = verticalX,
                Y2 = childCenters.Max(p => p.Y),
                Stroke = new SolidColorBrush(meta.LineColor),
                StrokeThickness = 2
            });

            // 2. Linie verticală între toți copiii (doar dacă > 1)
            if (childCenters.Count > 1)
            {
                canvas.Children.Add(new Line
                {
                    X1 = verticalX,
                    Y1 = childCenters.Min(p => p.Y),
                    X2 = verticalX,
                    Y2 = childCenters.Max(p => p.Y),
                    Stroke = new SolidColorBrush(meta.LineColor),
                    StrokeThickness = 2
                });
            }

            // 3. Coturi spre fiecare copil
            foreach (var center in childCenters)
            {
                canvas.Children.Add(new Line
                {
                    X1 = verticalX,
                    Y1 = center.Y,
                    X2 = center.X + 4, // asigură-te că e centrul bullet-ului
                    Y2 = center.Y,
                    Stroke = new SolidColorBrush(meta.LineColor),
                    StrokeThickness = 2
                });
            }
        }

        private System.Windows.Media.Color GetRandomColor()
        {
            var rand = new Random();
            return System.Windows.Media.Color.FromRgb(
                (byte)rand.Next(100, 255),
                (byte)rand.Next(100, 255),
                (byte)rand.Next(100, 255));
        }

        private class TreeNodeMetadata
        {
            public Canvas Canvas { get; set; } = default!;
            public SvgViewbox Bullet { get; set; } = default!;
            public StackPanel ChildrenPanel { get; set; } = default!;
            public System.Windows.Media.Color LineColor { get; set; }
        }
    }
}

