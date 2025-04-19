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

namespace WhiteBoardModule.XAML.Shapes.Nodes
{
    public class AdvancedTreeShapeRenderer : IShapeRenderer
    {
        private readonly bool _withBindings;

        public AdvancedTreeShapeRenderer(bool withBindings = false)
        {
            _withBindings = withBindings;
        }

        public UIElement CreatePreview()
        {
            return new Viewbox
            {
                Width = 80,
                Height = 80,
                Child = new TextBlock
                {
                    Text = "Tree",
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
        }

        public UIElement Render()
        {
            var rootPanel = new StackPanel();
            var container = new Border
            {
                BorderBrush = Brushes.DeepSkyBlue,
                BorderThickness = new Thickness(1.5),
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(4),
                Child = rootPanel
            };

            var rootNode = CreateNode("Solution", 0);
            rootPanel.Children.Add(rootNode.Node);
            return container;
        }

        private class TreeNodeResult
        {
            public StackPanel Node { get; set; } = default!;
            public Ellipse Bullet { get; set; } = default!;
            public StackPanel ChildrenPanel { get; set; } = default!;
            public Canvas ConnectionCanvas { get; set; } = default!;
        }

        private TreeNodeResult CreateNode(string nodeName, int level)
        {
            var nodeContainer = new StackPanel { Margin = new Thickness(0, 2, 0, 2) };
            var canvas = new Canvas { IsHitTestVisible = false };
            nodeContainer.Children.Add(canvas);

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(level * 20 + 20) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var bullet = new Ellipse
            {
                Width = 8,
                Height = 8,
                Fill = Brushes.DeepSkyBlue,
                Margin = new Thickness(2),
                VerticalAlignment = VerticalAlignment.Center
            };

            var childrenPanel = new StackPanel();

            var contentPanel = new StackPanel { Orientation = Orientation.Horizontal };

            var nameBox = new TextBox
            {
                Text = nodeName,
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1),
                Background = Brushes.Transparent,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 4, 0),
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

            contentPanel.Children.Add(bullet);
            contentPanel.Children.Add(nameBox);
            contentPanel.Children.Add(addBtn);
            contentPanel.Children.Add(removeBtn);

            Grid.SetColumn(contentPanel, 1);
            grid.Children.Add(contentPanel);

            nodeContainer.Children.Add(grid);
            nodeContainer.Children.Add(childrenPanel);

            var result = new TreeNodeResult
            {
                Node = nodeContainer,
                Bullet = bullet,
                ChildrenPanel = childrenPanel,
                ConnectionCanvas = canvas
            };

            addBtn.Click += (s, e) =>
            {
                var child = CreateNode("Child", level + 1);
                childrenPanel.Children.Add(child.Node);
                RedrawAllConnections(nodeContainer);
            };

            removeBtn.Click += (s, e) =>
            {
                var parent = nodeContainer.Parent as Panel;
                parent?.Children.Remove(nodeContainer);
                RedrawAllConnections(parent?.Parent as StackPanel);
            };

            return result;
        }

        private void RedrawAllConnections(UIElement root)
        {
            if (root is not StackPanel nodePanel) return;

            foreach (var child in nodePanel.Children.OfType<StackPanel>())
            {
                var canvas = child.Children.OfType<Canvas>().FirstOrDefault();
                var grid = child.Children.OfType<Grid>().FirstOrDefault();
                var contentPanel = grid?.Children.OfType<StackPanel>().FirstOrDefault();
                var bullet = contentPanel?.Children.OfType<Ellipse>().FirstOrDefault();
                var childrenPanel = child.Children.OfType<StackPanel>().Skip(1).FirstOrDefault();

                if (canvas == null || bullet == null || childrenPanel == null)
                    continue;

                canvas.Children.Clear();

                var parentCenter = bullet.PointToScreen(new Point(bullet.Width / 2, bullet.Height / 2));
                parentCenter = canvas.PointFromScreen(parentCenter);

                var childPoints = new List<Point>();

                foreach (UIElement subChild in childrenPanel.Children)
                {
                    if (subChild is not StackPanel subStack) continue;
                    var subGrid = subStack.Children.OfType<Grid>().FirstOrDefault();
                    var subContent = subGrid?.Children.OfType<StackPanel>().FirstOrDefault();
                    var subBullet = subContent?.Children.OfType<Ellipse>().FirstOrDefault();
                    if (subBullet == null) continue;

                    var childCenter = subBullet.PointToScreen(new Point(subBullet.Width / 2, subBullet.Height / 2));
                    childCenter = canvas.PointFromScreen(childCenter);
                    childPoints.Add(childCenter);

                    canvas.Children.Add(new Line
                    {
                        X1 = parentCenter.X,
                        Y1 = childCenter.Y,
                        X2 = childCenter.X,
                        Y2 = childCenter.Y,
                        Stroke = Brushes.DimGray,
                        StrokeThickness = 1
                    });
                }

                if (childPoints.Count >= 2)
                {
                    var yTop = childPoints.Min(p => p.Y);
                    var yBottom = childPoints.Max(p => p.Y);
                    canvas.Children.Add(new Line
                    {
                        X1 = parentCenter.X,
                        Y1 = yTop,
                        X2 = parentCenter.X,
                        Y2 = yBottom,
                        Stroke = Brushes.DimGray,
                        StrokeThickness = 1
                    });
                }

                RedrawAllConnections(childrenPanel);
            }
        }
    }
}

