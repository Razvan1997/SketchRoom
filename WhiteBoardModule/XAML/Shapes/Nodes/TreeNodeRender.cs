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
    public class TreeNodeRenderer : IShapeRenderer
    {
        private readonly bool _isPreview;
        private readonly string _description;
        private readonly List<TreeNodeRenderer> _children;

        public TreeNodeRenderer(string description, bool isPreview = false)
        {
            _description = description;
            _isPreview = isPreview;
            _children = new List<TreeNodeRenderer>();
        }

        public void AddChild(TreeNodeRenderer child) => _children.Add(child);
        public void RemoveChild(TreeNodeRenderer child) => _children.Remove(child);

        public UIElement Render()
        {
            var stack = new StackPanel { Orientation = Orientation.Vertical };

            stack.Children.Add(RenderNode());

            foreach (var child in _children)
            {
                var childElement = child.Render();
                if (childElement is FrameworkElement fe)
                {
                    fe.Margin = new Thickness(30, 0, 0, 0);
                    stack.Children.Add(fe);
                }
                else
                {
                    stack.Children.Add(childElement); // fallback
                }
            }

            return stack;
        }

        public UIElement CreatePreview()
        {
            var stack = new StackPanel { Orientation = Orientation.Vertical };

            stack.Children.Add(RenderNode(true));

            foreach (var child in _children)
            {
                var childElement = child.CreatePreview();
                if (childElement is FrameworkElement fe)
                {
                    fe.Margin = new Thickness(30, 0, 0, 0);
                    stack.Children.Add(fe);
                }
                else
                {
                    stack.Children.Add(childElement); // fallback
                }
            }

            return stack;
        }

        private UIElement RenderNode(bool preview = false)
        {
            var ellipse = new Ellipse
            {
                Width = 16,
                Height = 16,
                Fill = Brushes.SteelBlue,
                Stroke = Brushes.Black,
                StrokeThickness = 1,
                Margin = new Thickness(4, 0, 4, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            var arrow = new Polygon
            {
                Points = new PointCollection { new Point(0, 5), new Point(10, 10), new Point(0, 15) },
                Fill = Brushes.Black,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 0, 4, 0)
            };

            var content = new Border
            {
                Background = Brushes.LightYellow,
                BorderBrush = Brushes.DarkGray,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6),
                Child = preview
                    ? new TextBlock { Text = _description, FontWeight = FontWeights.SemiBold }
                    : new TextBox { Text = _description }
            };

            var addButton = new Button { Content = "+", Width = 24, Height = 24, Margin = new Thickness(5, 0, 0, 0) };
            var deleteButton = new Button { Content = "×", Width = 24, Height = 24, Margin = new Thickness(5, 0, 0, 0) };

            var row = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            row.Children.Add(ellipse);
            row.Children.Add(arrow);
            row.Children.Add(content);

            if (!preview)
            {
                row.Children.Add(addButton);
                row.Children.Add(deleteButton);

                addButton.Click += (s, e) =>
                {
                    var newNode = new TreeNodeRenderer("New Node");
                    AddChild(newNode);
                    RefreshVisual(row);
                };

                deleteButton.Click += (s, e) =>
                {
                    var parent = VisualTreeHelper.GetParent(row) as Panel;
                    parent?.Children.Remove(row);
                };
            }

            return row;
        }

        private void RefreshVisual(UIElement current)
        {
            var parent = VisualTreeHelper.GetParent(current);
            if (parent is Panel panel)
            {
                panel.Children.Clear();
                var rendered = Render();
                if (rendered is UIElement ui)
                    panel.Children.Add(ui);
            }
        }
    }
}
