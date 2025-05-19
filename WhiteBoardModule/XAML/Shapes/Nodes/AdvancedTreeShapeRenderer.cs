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
using SketchRoom.Models.Enums;
using WhiteBoard.Core.Models;
using ColorConverter = System.Windows.Media.ColorConverter;
using Color = System.Windows.Media.Color;

namespace WhiteBoardModule.XAML.Shapes.Nodes
{
    public class AdvancedTreeShapeRenderer : IShapeRenderer, IRestoreFromShape
    {
        private readonly List<StackPanel> _nodes = new();
        private StackPanel? _rootNode;
        private StackPanel? _rootPanel;
        private bool _isRestoring = false;
        private Border? _container;
        private FrameworkElement? _renderedElement;
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
            var localNodes = new List<StackPanel>();
            _rootPanel = new StackPanel();

            var container = new Border
            {
                BorderBrush = Brushes.DeepSkyBlue,
                BorderThickness = new Thickness(1.5),
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(4),
                Child = _rootPanel,
                Tag = localNodes // ✅ TAG pe container
            };

            _container = container;

            _rootNode = CreateNode("Solution", 0, true, localNodes);
            _rootPanel.Children.Add(_rootNode);

            var context = new TreeRenderContext
            {
                Container = container,
                RootPanel = _rootPanel,
                Nodes = localNodes
            };

            container.Tag = context;
            _renderedElement = container;
            return container;
        }

        private StackPanel CreateNode(string name, int level, bool isRoot, List<StackPanel> targetList)
        {
            var nodeContainer = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 6, 0, 6)
            };

            var canvas = new Canvas
            {
                Width = 20,
                IsHitTestVisible = false
            };

            var contentLayout = new StackPanel();

            var bullet = new SvgViewbox
            {
                Width = 14,
                Height = 14,
                Stretch = Stretch.Uniform,
                Margin = new Thickness(2),
                Source = new Uri("pack://application:,,,/WhiteBoardModule;component/SVG/folder.svg")
            };

            var color = GetRandomColor();

            var bulletContainer = new Border
            {
                Child = bullet,
                Background = new SolidColorBrush(color)
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

            var childrenPanel = new StackPanel();

            contentLayout.Children.Add(contentPanel);
            contentLayout.Children.Add(childrenPanel);

            nodeContainer.Children.Add(canvas);
            nodeContainer.Children.Add(contentLayout);

            // Metadata
            var metadata = new TreeNodeMetadata
            {
                Canvas = canvas,
                Bullet = bullet,
                ChildrenPanel = childrenPanel,
                LineColor = color
            };

            nodeContainer.Tag = metadata;
            targetList.Add(nodeContainer); // ✅ adaugă doar în lista primită

            // Evenimente
            addBtn.Click += (s, e) =>
            {
                var child = CreateNode("Child", level + 1, false, targetList);
                childrenPanel.Children.Add(child);
                RedrawAllConnections(targetList);
            };

            removeBtn.Click += (s, e) =>
            {
                if (nodeContainer.Parent is Panel parent)
                {
                    parent.Children.Remove(nodeContainer);
                    targetList.Remove(nodeContainer);
                    RedrawAllConnections(targetList);
                }
            };

            return nodeContainer;
        }

        private void RedrawAllConnections(List<StackPanel> nodeList)
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                foreach (var node in nodeList)
                {
                    if (node.Tag is not TreeNodeMetadata metadata) continue;

                    // 🔧 Asigură-te că layout-ul este complet
                    metadata.Bullet.UpdateLayout();
                    metadata.Canvas.UpdateLayout();
                    RedrawConnectionsForNode(metadata);
                }
            }, DispatcherPriority.ApplicationIdle); // 🟢 Așteaptă până când totul este randat
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

        public BPMNShapeModelWithPosition? ExportData(IInteractiveShape control)
        {
            if (control is not FrameworkElement fe)
                return null;

            // 🔴 AICI era problema: _container != control!
            var context = FindTaggedContext(fe); // nu _container

            if (context == null)
                return null;

            var nodeList = context.Nodes;

            var extra = new Dictionary<string, string>();

            for (int i = 0; i < nodeList.Count; i++)
            {
                if (nodeList[i].Tag is not TreeNodeMetadata meta)
                    continue;

                var contentPanel = ((StackPanel)((StackPanel)nodeList[i].Children[1]).Children[0]);
                var nameBox = contentPanel.Children.OfType<TextBox>().FirstOrDefault();

                int parentIndex = -1;
                for (int j = 0; j < nodeList.Count; j++)
                {
                    if (nodeList[j].Tag is TreeNodeMetadata parentMeta &&
                        parentMeta.ChildrenPanel.Children.Contains(nodeList[i]))
                    {
                        parentIndex = j;
                        break;
                    }
                }

                extra[$"Node{i}_Text"] = nameBox?.Text ?? $"Node{i}";
                extra[$"Node{i}_Color"] = meta.LineColor.ToString();
                extra[$"Node{i}_Parent"] = parentIndex.ToString();
            }

            extra["NodeCount"] = nodeList.Count.ToString();

            return new BPMNShapeModelWithPosition
            {
                Type = ShapeType.AdvancedTreeShapeRenderer,
                Left = Canvas.GetLeft(fe),
                Top = Canvas.GetTop(fe),
                Width = fe.Width,
                Height = fe.Height,
                Name = fe.Name ?? "AdvancedTree",
                Category = "Nodes",
                SvgUri = null,
                ExtraProperties = extra
            };
        }

        public void Restore(Dictionary<string, string> extraProperties)
        {
            if (!extraProperties.TryGetValue("NodeCount", out var cStr) || !int.TryParse(cStr, out var count))
                return;

            if (_renderedElement == null)
                return;

            var context = FindTaggedContext(_renderedElement);
            if (context == null)
                return;

            var container = context.Container;
            var rootPanel = context.RootPanel;
            var nodeList = context.Nodes;

            nodeList.Clear();
            rootPanel.Children.Clear();

            _isRestoring = true;

            // Creează nodurile
            var rootText = extraProperties.TryGetValue("Node0_Text", out var rootT) ? rootT : "Solution";
            var rootColor = extraProperties.TryGetValue("Node0_Color", out var colorStr) &&
                            ColorConverter.ConvertFromString(colorStr) is Color parsedColor
                                ? parsedColor
                                : GetRandomColor();

            var allNodes = new List<StackPanel>();
            var rootNode = CreateNode(rootText, 0, true, nodeList);
            if (rootNode.Tag is TreeNodeMetadata rootMeta)
                rootMeta.LineColor = parsedColor;

            rootPanel.Children.Add(rootNode);
            allNodes.Add(rootNode);

            for (int i = 1; i < count; i++)
            {
                var text = extraProperties.TryGetValue($"Node{i}_Text", out var t) ? t : $"Node{i}";
                var color = extraProperties.TryGetValue($"Node{i}_Color", out var c) &&
                            ColorConverter.ConvertFromString(c) is Color col ? col : GetRandomColor();

                var node = CreateNode(text, 1, false, nodeList);
                if (node.Tag is TreeNodeMetadata meta)
                    meta.LineColor = color;

                allNodes.Add(node);
            }

            for (int i = 1; i < count; i++)
            {
                if (!extraProperties.TryGetValue($"Node{i}_Parent", out var pStr) || !int.TryParse(pStr, out var parentIdx))
                    continue;

                if (parentIdx >= 0 && parentIdx < allNodes.Count &&
                    allNodes[parentIdx].Tag is TreeNodeMetadata parentMeta)
                {
                    parentMeta.ChildrenPanel.Children.Add(allNodes[i]);
                }
            }

            int bulletsToLoad = nodeList.Count;
            int loadedCount = 0;

            foreach (var node in nodeList)
            {
                if (node.Tag is TreeNodeMetadata meta)
                {
                    meta.Bullet.Loaded += Bullet_Loaded;
                }
            }

            void Bullet_Loaded(object sender, RoutedEventArgs e)
            {
                loadedCount++;
                if (loadedCount >= bulletsToLoad)
                {
                    // toate au fost încărcate, putem trasa liniile
                    foreach (var node in nodeList)
                    {
                        if (node.Tag is TreeNodeMetadata m)
                        {
                            RedrawConnectionsForNode(m);
                        }
                    }

                    _isRestoring = false;
                }

                // elimină handlerul ca să nu se repete
                if (sender is FrameworkElement fe)
                    fe.Loaded -= Bullet_Loaded;
            }
        }

        private Border? FindTaggedBorder(DependencyObject parent)
        {
            if (parent is Border border && border.Tag is List<StackPanel>)
                return border;

            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                var result = FindTaggedBorder(child);
                if (result != null)
                    return result;
            }

            return null;
        }

        private TreeRenderContext? FindTaggedContext(DependencyObject parent)
        {
            if (parent is Border border && border.Tag is TreeRenderContext context)
                return context;

            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                var result = FindTaggedContext(child);
                if (result != null)
                    return result;
            }

            return null;
        }

        private class TreeNodeMetadata
        {
            public Canvas Canvas { get; set; } = default!;
            public SvgViewbox Bullet { get; set; } = default!;
            public StackPanel ChildrenPanel { get; set; } = default!;
            public System.Windows.Media.Color LineColor { get; set; }
        }

        private class TreeRenderContext
        {
            public Border Container { get; set; } = null!;
            public StackPanel RootPanel { get; set; } = null!;
            public List<StackPanel> Nodes { get; set; } = new();
        }
    }
}

