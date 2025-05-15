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
        private TextBox? _titleBox;
        private StackPanel? _fieldsPanel;
        private StackPanel? _methodsPanel;
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

            _titleBox = new TextBox
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

            _fieldsPanel = new StackPanel { Name = "FieldsPanel" };
            _methodsPanel = new StackPanel { Name = "MethodsPanel" };

            _fieldsPanel.Children.Add(CreateLineRow(true, _fieldsPanel));
            _fieldsPanel.Children.Add(CreateLineRow(true, _fieldsPanel));
            _methodsPanel.Children.Add(CreateLineRow(false, _methodsPanel));

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
                    _fieldsPanel.Children.Add(CreateLineRow(true, _fieldsPanel));
                else
                    _methodsPanel.Children.Add(CreateLineRow(false, _methodsPanel));
            };

            var mainStack = new StackPanel();
            mainStack.Children.Add(_titleBox);
            mainStack.Children.Add(_fieldsPanel);
            mainStack.Children.Add(separator);
            mainStack.Children.Add(_methodsPanel);
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
            if (_titleBox == null || _fieldsPanel == null || _methodsPanel == null || control is not FrameworkElement fe)
                return null;

            var extra = new Dictionary<string, string>
            {
                ["ClassName"] = _titleBox.Text
            };

            if (_titleBox.Background is SolidColorBrush bg)
                extra["ClassName_Background"] = bg.Color.ToString();

            int i = 1;
            foreach (var row in _fieldsPanel.Children.OfType<DockPanel>())
            {
                var tbs = row.Children.OfType<TextBox>().ToList();
                if (tbs.Count >= 2)
                {
                    extra[$"Field{i}_Name"] = tbs[0].Text;
                    extra[$"Field{i}_Type"] = tbs[1].Text;
                }
                i++;
            }

            int j = 1;
            foreach (var row in _methodsPanel.Children.OfType<DockPanel>())
            {
                var tbs = row.Children.OfType<TextBox>().ToList();
                if (tbs.Count >= 3)
                {
                    extra[$"Method{j}_Name"] = tbs[0].Text;
                    extra[$"Method{j}_Params"] = tbs[1].Text;
                    extra[$"Method{j}_Type"] = tbs[2].Text;
                }
                j++;
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

        public void Restore(Dictionary<string, string> extra)
        {
            if (_titleBox == null || _fieldsPanel == null || _methodsPanel == null)
                return;

            if (extra.TryGetValue("ClassName", out var name))
                _titleBox.Text = name;

            if (extra.TryGetValue("ClassName_Background", out var bgColor))
            {
                try
                {
                    _titleBox.Background = (SolidColorBrush)new BrushConverter().ConvertFromString(bgColor);
                }
                catch
                {
                    _titleBox.Background = new SolidColorBrush(Color.FromRgb(45, 45, 48));
                }
            }

            _fieldsPanel.Children.Clear();
            _methodsPanel.Children.Clear();

            var prefs = ContainerLocator.Container.Resolve<IDrawingPreferencesService>();

            int i = 1;
            while (extra.TryGetValue($"Field{i}_Name", out var fname))
            {
                var ftype = extra.TryGetValue($"Field{i}_Type", out var t) ? t : "type";
                var row = CreateLineRow(true, _fieldsPanel);

                if (row is DockPanel dock)
                {
                    var tbs = dock.Children.OfType<TextBox>().ToList();
                    if (tbs.Count >= 2)
                    {
                        tbs[0].Text = fname;
                        tbs[1].Text = ftype;
                    }
                }

                _fieldsPanel.Children.Add(row);
                i++;
            }

            int j = 1;
            while (extra.TryGetValue($"Method{j}_Name", out var mname))
            {
                var mparams = extra.TryGetValue($"Method{j}_Params", out var p) ? p : "()";
                var mtype = extra.TryGetValue($"Method{j}_Type", out var t) ? t : "type";
                var row = CreateLineRow(false, _methodsPanel);

                if (row is DockPanel dock)
                {
                    var tbs = dock.Children.OfType<TextBox>().ToList();
                    if (tbs.Count >= 3)
                    {
                        tbs[0].Text = mname;
                        tbs[1].Text = mparams;
                        tbs[2].Text = mtype;
                    }
                }

                _methodsPanel.Children.Add(row);
                j++;
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
