using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;
using WhiteBoard.Core.Services.Interfaces;
using System.Windows.Shapes;
using SketchRoom.Models.Enums;
using WhiteBoard.Core.Models;

namespace WhiteBoardModule.XAML.Shapes.Entity
{
    public class UmlClassShapeRenderer : IShapeRenderer, IRestoreFromShape
    {
        private readonly bool _withBindings;

        public UmlClassShapeRenderer(bool withBindings = false)
        {
            _withBindings = withBindings;
        }

        public UIElement CreatePreview()
        {
            var preview = new StackPanel();

            preview.Children.Add(new TextBlock
            {
                Text = "Classname",
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)),
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(2)
            });

            preview.Children.Add(new TextBlock { Text = "+ field: type", Foreground = Brushes.White, Margin = new Thickness(4) });
            preview.Children.Add(new TextBlock { Text = "+ method(param): type", Foreground = Brushes.White, Margin = new Thickness(4) });

            return new Viewbox
            {
                Width = 100,
                Height = 100,
                Stretch = Stretch.Uniform,
                Child = new Border
                {
                    BorderBrush = Brushes.DeepSkyBlue,
                    BorderThickness = new Thickness(1.5),
                    Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                    CornerRadius = new CornerRadius(6),
                    Child = preview
                }
            };
        }

        public UIElement Render()
        {
            var preferences = ContainerLocator.Container.Resolve<IDrawingPreferencesService>();

            var titleBox = new TextBox
            {
                Text = "Classname",
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)),
                Foreground = Brushes.White,
                Style = (Style)Application.Current.FindResource("DarkTextBoxStyle"),
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1),
                Margin = new Thickness(2),
                TextAlignment = TextAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Center
            };

            var fieldsPanel = new StackPanel { Name = "FieldsPanel" };
            var methodsPanel = new StackPanel { Name = "MethodsPanel" };

            fieldsPanel.Children.Add(CreateLineRow(true, fieldsPanel));
            fieldsPanel.Children.Add(CreateLineRow(true, fieldsPanel));
            methodsPanel.Children.Add(CreateLineRow(false, methodsPanel));

            var separator = new Rectangle
            {
                Height = 1,
                Margin = new Thickness(0, 4, 0, 4),
                Fill = Brushes.Gray
            };

            var togglePanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
            var fieldRadio = new RadioButton { Content = "Field", IsChecked = true, Margin = new Thickness(4), Foreground = Brushes.White };
            var methodRadio = new RadioButton { Content = "Method", Margin = new Thickness(4), Foreground = Brushes.White };
            togglePanel.Children.Add(fieldRadio);
            togglePanel.Children.Add(methodRadio);

            var addButton = new Button
            {
                Content = "+",
                Width = 24,
                Height = 24,
                Margin = new Thickness(4),
                Background = new SolidColorBrush(Color.FromRgb(65, 65, 68)),
                Foreground = Brushes.White,
                BorderBrush = Brushes.Gray
            };

            addButton.Click += (s, e) =>
            {
                if (fieldRadio.IsChecked == true)
                    fieldsPanel.Children.Add(CreateLineRow(true, fieldsPanel));
                else
                    methodsPanel.Children.Add(CreateLineRow(false, methodsPanel));
            };

            var mainStack = new StackPanel();
            mainStack.Children.Add(titleBox);
            mainStack.Children.Add(fieldsPanel);
            mainStack.Children.Add(separator);
            mainStack.Children.Add(methodsPanel);
            mainStack.Children.Add(togglePanel);
            mainStack.Children.Add(addButton);

            return new Grid
            {
                Children =
            {
                new Border
                {
                    BorderBrush = Brushes.DeepSkyBlue,
                    BorderThickness = new Thickness(1.5),
                    Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(6),
                    Child = mainStack
                }
            }
            };
        }

        private UIElement CreateLineRow(bool isField, Panel parentPanel)
        {
            var row = new DockPanel { Margin = new Thickness(2), LastChildFill = false };

            var prefix = new TextBlock
            {
                Text = "+",
                Foreground = Brushes.White,
                Margin = new Thickness(2),
                VerticalAlignment = VerticalAlignment.Center
            };
            DockPanel.SetDock(prefix, Dock.Left);
            row.Children.Add(prefix);

            var nameBox = new TextBox
            {
                Text = isField ? "field" : "method",
                Width = 80,
                Margin = new Thickness(2),
                Foreground = Brushes.White,
                Style = (Style)Application.Current.FindResource("DarkTextBoxStyle"),
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1)
            };
            DockPanel.SetDock(nameBox, Dock.Left);
            row.Children.Add(nameBox);

            if (!isField)
            {
                var paramBox = new TextBox
                {
                    Text = "()",
                    Width = 40,
                    Margin = new Thickness(2),
                    Foreground = Brushes.White,
                    Style = (Style)Application.Current.FindResource("DarkTextBoxStyle"),
                    Background = Brushes.Transparent,
                    BorderBrush = Brushes.Gray,
                    BorderThickness = new Thickness(1)
                };
                DockPanel.SetDock(paramBox, Dock.Left);
                row.Children.Add(paramBox);
            }

            var colon = new TextBlock
            {
                Text = ":",
                Foreground = Brushes.White,
                Margin = new Thickness(2),
                VerticalAlignment = VerticalAlignment.Center
            };
            DockPanel.SetDock(colon, Dock.Left);
            row.Children.Add(colon);

            var typeBox = new TextBox
            {
                Text = "type",
                Style = (Style)Application.Current.FindResource("DarkTextBoxStyle"),
                Width = 60,
                Margin = new Thickness(2),
                Foreground = Brushes.White,
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1)
            };
            DockPanel.SetDock(typeBox, Dock.Left);
            row.Children.Add(typeBox);

            var removeBtn = new Button
            {
                Content = "✖",
                Width = 22,
                Height = 22,
                Margin = new Thickness(2),
                Background = Brushes.Transparent,
                Foreground = Brushes.Red,
                BorderBrush = Brushes.Gray,
                FontWeight = FontWeights.Bold,
                Padding = new Thickness(0)
            };

            removeBtn.Click += (s, e) => { parentPanel.Children.Remove(row); };

            DockPanel.SetDock(removeBtn, Dock.Right);
            row.Children.Add(removeBtn);

            return row;
        }

        public BPMNShapeModelWithPosition? ExportData(IInteractiveShape control)
        {
            if (control is not FrameworkElement fe)
                return null;

            var border = fe as Border ?? fe.FindName("border") as Border;
            if (border?.Child is not StackPanel mainStack)
                return null;

            var extra = new Dictionary<string, string>();

            if (mainStack.Children[0] is TextBox titleBox)
                extra["ClassName"] = titleBox.Text;

            if (mainStack.Children.OfType<StackPanel>().FirstOrDefault(p => p.Name == "FieldsPanel") is StackPanel fieldsPanel)
            {
                int i = 1;
                foreach (var row in fieldsPanel.Children.OfType<DockPanel>())
                {
                    var elements = row.Children.OfType<UIElement>().ToList();

                    var name = elements.OfType<TextBox>().ElementAtOrDefault(0)?.Text ?? "";
                    var type = elements.OfType<TextBox>().ElementAtOrDefault(1)?.Text ?? "";

                    extra[$"Field{i}_Name"] = name;
                    extra[$"Field{i}_Type"] = type;
                    i++;
                }
            }

            if (mainStack.Children.OfType<StackPanel>().FirstOrDefault(p => p.Name == "MethodsPanel") is StackPanel methodsPanel)
            {
                int j = 1;
                foreach (var row in methodsPanel.Children.OfType<DockPanel>())
                {
                    var elements = row.Children.OfType<UIElement>().ToList();

                    var name = elements.OfType<TextBox>().ElementAtOrDefault(0)?.Text ?? "";
                    var parameters = elements.OfType<TextBox>().ElementAtOrDefault(1)?.Text ?? "";
                    var type = elements.OfType<TextBox>().ElementAtOrDefault(2)?.Text ?? "";

                    extra[$"Method{j}_Name"] = name;
                    extra[$"Method{j}_Params"] = parameters;
                    extra[$"Method{j}_Type"] = type;
                    j++;
                }
            }

            return new BPMNShapeModelWithPosition
            {
                Type = ShapeType.UmlClassTypeShape,
                Left = Canvas.GetLeft(fe),
                Top = Canvas.GetTop(fe),
                Width = fe.Width,
                Height = fe.Height,
                Name = fe.Name,
                Category = "Entity",
                SvgUri = null,
                ExtraProperties = extra
            };
        }

        public void Restore(Dictionary<string, string> extraProperties)
        {
            if (extraProperties == null || extraProperties.Count == 0)
                return;

            // Găsește Border-ul cu StackPanel-ul principal
            var border = VisualTreeHelper.GetChildrenCount(Application.Current.MainWindow) > 0
                ? FindDescendant<Border>(Application.Current.MainWindow, b => b.Child is StackPanel stack &&
                    stack.Children.OfType<TextBox>().Any(t => t.Text == "Classname"))
                : null;

            if (border?.Child is not StackPanel mainStack)
                return;

            var preferences = ContainerLocator.Container.Resolve<IDrawingPreferencesService>();

            // Restaurează numele clasei
            if (mainStack.Children[0] is TextBox titleBox && extraProperties.TryGetValue("ClassName", out var className))
            {
                titleBox.Text = className;
            }

            // Găsește panourile
            var fieldsPanel = mainStack.Children.OfType<StackPanel>().FirstOrDefault(p => p.Name == "FieldsPanel");
            var methodsPanel = mainStack.Children.OfType<StackPanel>().FirstOrDefault(p => p.Name == "MethodsPanel");

            if (fieldsPanel != null)
            {
                fieldsPanel.Children.Clear();
                int i = 1;
                while (extraProperties.TryGetValue($"Field{i}_Name", out var name))
                {
                    var type = extraProperties.TryGetValue($"Field{i}_Type", out var t) ? t : "type";

                    var row = CreateLineRow(true, fieldsPanel);
                    if (row is DockPanel dock)
                    {
                        var tbs = dock.Children.OfType<TextBox>().ToList();
                        if (tbs.Count >= 2)
                        {
                            tbs[0].Text = name;
                            tbs[1].Text = type;
                        }
                    }

                    fieldsPanel.Children.Add(row);
                    i++;
                }
            }

            if (methodsPanel != null)
            {
                methodsPanel.Children.Clear();
                int j = 1;
                while (extraProperties.TryGetValue($"Method{j}_Name", out var name))
                {
                    var parameters = extraProperties.TryGetValue($"Method{j}_Params", out var p) ? p : "()";
                    var type = extraProperties.TryGetValue($"Method{j}_Type", out var t) ? t : "type";

                    var row = CreateLineRow(false, methodsPanel);
                    if (row is DockPanel dock)
                    {
                        var tbs = dock.Children.OfType<TextBox>().ToList();
                        if (tbs.Count >= 3)
                        {
                            tbs[0].Text = name;
                            tbs[1].Text = parameters;
                            tbs[2].Text = type;
                        }
                    }

                    methodsPanel.Children.Add(row);
                    j++;
                }
            }
        }

        private T? FindDescendant<T>(DependencyObject parent, Func<T, bool>? predicate = null) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T typed && (predicate == null || predicate(typed)))
                    return typed;

                var result = FindDescendant(child, predicate);
                if (result != null)
                    return result;
            }

            return null;
        }
    }
}
