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
using WhiteBoard.Core.Models;

namespace WhiteBoardModule.XAML.Shapes.Entity
{
    public class ObjectTypeShapeRenderer : IShapeRenderer, IRestoreFromShape
    {
        private readonly bool _withBindings;
        private static readonly List<string> _types = new() { "string", "int", "bool", "float", "object" };
        private static readonly List<string> _access = new() { "public", "private", "protected", "internal" };
        private Border? _mainBorder;
        private StackPanel? _mainStack;
        private TextBox? _titleBox;
        private StackPanel? _fieldPanel;
        public ObjectTypeShapeRenderer(bool withBindings = false)
        {
            _withBindings = withBindings;
        }

        public UIElement CreatePreview()
        {
            var panel = new StackPanel
            {
                Background = Brushes.Transparent
            };

            var title = new TextBlock
            {
                Text = "Object:Type",
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)),
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(2)
            };

            panel.Children.Add(title);

            for (int i = 0; i < 3; i++)
            {
                panel.Children.Add(new TextBlock
                {
                    Text = $"field{i + 1} = value{i + 1}",
                    Foreground = Brushes.White,
                    Margin = new Thickness(4, 2, 4, 2)
                });
            }

            return new Viewbox
            {
                Width = 60,
                Height = 60,
                Stretch = Stretch.Uniform,
                Child = new Border
                {
                    BorderBrush = Brushes.DeepSkyBlue,
                    BorderThickness = new Thickness(1.5),
                    Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                    CornerRadius = new CornerRadius(6),
                    Child = panel
                }
            };
        }

        public UIElement Render()
        {
            var preferences = ContainerLocator.Container.Resolve<IDrawingPreferencesService>();

            _mainStack = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Background = Brushes.Transparent
            };

            _titleBox = new TextBox
            {
                Text = "Object:Type",
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)),
                Foreground = Brushes.White,
                BorderBrush = Brushes.Gray,
                Style = (Style)Application.Current.FindResource("DarkTextBoxStyle"),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(2),
                TextAlignment = TextAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Center
            };

            _mainStack.Children.Add(_titleBox);

            _fieldPanel = new StackPanel { Name = "FieldsPanel" };

            for (int i = 0; i < 3; i++)
                _fieldPanel.Children.Add(CreateFieldRow(preferences, _fieldPanel));

            _mainStack.Children.Add(_fieldPanel);

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
                _fieldPanel.Children.Add(CreateFieldRow(preferences, _fieldPanel));
            };

            _mainStack.Children.Add(addButton);

            _mainBorder = new Border
            {
                BorderBrush = Brushes.DeepSkyBlue,
                BorderThickness = new Thickness(1.5),
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(6),
                Child = _mainStack
            };

            return new Grid
            {
                Children = { _mainBorder }
            };
        }

        private UIElement CreateFieldRow(IDrawingPreferencesService preferences, Panel parentPanel)
        {
            var row = new DockPanel
            {
                Margin = new Thickness(2),
                LastChildFill = false
            };

            // Field
            var fieldBox = new TextBox
            {
                Text = "field",
                Width = 80,
                Margin = new Thickness(2),
                FontSize = preferences.FontSize,
                Foreground = Brushes.White,
                Style = (Style)Application.Current.FindResource("DarkTextBoxStyle"),
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1)
            };
            DockPanel.SetDock(fieldBox, Dock.Left);
            row.Children.Add(fieldBox);

            // Equals
            var equalsText = new TextBlock
            {
                Text = "=",
                Foreground = Brushes.White,
                Margin = new Thickness(2),
                VerticalAlignment = VerticalAlignment.Center
            };
            DockPanel.SetDock(equalsText, Dock.Left);
            row.Children.Add(equalsText);

            // Value
            var valueBox = new TextBox
            {
                Text = "value",
                Width = 80,
                Margin = new Thickness(2),
                FontSize = preferences.FontSize,
                Foreground = Brushes.White,
                Style = (Style)Application.Current.FindResource("DarkTextBoxStyle"),
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1)
            };
            DockPanel.SetDock(valueBox, Dock.Left);
            row.Children.Add(valueBox);

            // Type ComboBox
            var typeCombo = new ComboBox
            {
                ItemsSource = _types,
                SelectedItem = _types[0],
                Width = 70,
                Margin = new Thickness(2),
                Background = Brushes.Black,
                Foreground = Brushes.White,
                Style = (Style)Application.Current.FindResource("DarkComboBoxStyle"),
                BorderBrush = Brushes.Gray
            };
            DockPanel.SetDock(typeCombo, Dock.Left);
            row.Children.Add(typeCombo);

            // Access ComboBox
            var accessCombo = new ComboBox
            {
                ItemsSource = _access,
                SelectedItem = _access[0],
                Width = 80,
                Margin = new Thickness(2),
                Style = (Style)Application.Current.FindResource("DarkComboBoxStyle"),
                Background = Brushes.Black,
                Foreground = Brushes.White,
                BorderBrush = Brushes.Gray
            };
            DockPanel.SetDock(accessCombo, Dock.Left);
            row.Children.Add(accessCombo);

            // Remove button
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

            removeBtn.Click += (s, e) =>
            {
                parentPanel.Children.Remove(row);
            };

            DockPanel.SetDock(removeBtn, Dock.Right);
            row.Children.Add(removeBtn);

            return row;
        }

        public BPMNShapeModelWithPosition? ExportData(IInteractiveShape control)
        {
            if (_mainStack == null || _fieldPanel == null || control is not FrameworkElement fe)
                return null;

            var extra = new Dictionary<string, string>();

            if (_titleBox != null)
            {
                extra["ObjectTitle"] = _titleBox.Text;

                if (_titleBox.Background is SolidColorBrush bg)
                {
                    extra["ObjectTitle_Background"] = bg.Color.ToString();
                }
            }

            int index = 1;
            foreach (var row in _fieldPanel.Children.OfType<DockPanel>())
            {
                var textBoxes = row.Children.OfType<TextBox>().ToList();
                var comboBoxes = row.Children.OfType<ComboBox>().ToList();

                if (textBoxes.Count >= 2)
                {
                    extra[$"Field{index}_Name"] = textBoxes[0].Text;
                    extra[$"Field{index}_Value"] = textBoxes[1].Text;
                }

                var typeBox = comboBoxes.FirstOrDefault(cb => cb.ItemsSource == _types);
                var accessBox = comboBoxes.FirstOrDefault(cb => cb.ItemsSource == _access);

                if (typeBox != null)
                    extra[$"Field{index}_Type"] = typeBox.SelectedItem?.ToString() ?? "";

                if (accessBox != null)
                    extra[$"Field{index}_Access"] = accessBox.SelectedItem?.ToString() ?? "";

                index++;
            }

            return new BPMNShapeModelWithPosition
            {
                Type = ShapeType.ObjectTypeShape,
                Left = Canvas.GetLeft(fe),
                Top = Canvas.GetTop(fe),
                Width = fe.Width,
                Height = fe.Height,
                Name = fe.Name,
                Category = "Object",
                SvgUri = null,
                ExtraProperties = extra
            };
        }

        public void Restore(Dictionary<string, string> extraProperties)
        {
            if (_titleBox == null || _fieldPanel == null || extraProperties == null)
                return;

            // Restaurare titlu
            if (extraProperties.TryGetValue("ObjectTitle", out var title))
                _titleBox.Text = title;

            if (extraProperties.TryGetValue("ObjectTitle_Background", out var bgColor))
            {
                try
                {
                    _titleBox.Background = (SolidColorBrush)new BrushConverter().ConvertFromString(bgColor);
                }
                catch
                {
                    _titleBox.Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)); // fallback
                }
            }

            // Curăță și reconstruiește câmpurile
            _fieldPanel.Children.Clear();
            var preferences = ContainerLocator.Container.Resolve<IDrawingPreferencesService>();

            int index = 1;
            while (extraProperties.TryGetValue($"Field{index}_Name", out var fieldName))
            {
                var fieldValue = extraProperties.TryGetValue($"Field{index}_Value", out var val) ? val : "";
                var type = extraProperties.TryGetValue($"Field{index}_Type", out var t) ? t : _types[0];
                var access = extraProperties.TryGetValue($"Field{index}_Access", out var a) ? a : _access[0];

                var row = CreateFieldRow(preferences, _fieldPanel);

                if (row is DockPanel dock)
                {
                    var textBoxes = dock.Children.OfType<TextBox>().ToList();
                    var comboBoxes = dock.Children.OfType<ComboBox>().ToList();

                    if (textBoxes.Count >= 2)
                    {
                        textBoxes[0].Text = fieldName;
                        textBoxes[1].Text = fieldValue;
                    }

                    comboBoxes.FirstOrDefault(cb => cb.ItemsSource == _types)!.SelectedItem =
                        _types.Contains(type) ? type : _types[0];

                    comboBoxes.FirstOrDefault(cb => cb.ItemsSource == _access)!.SelectedItem =
                        _access.Contains(access) ? access : _access[0];
                }

                _fieldPanel.Children.Add(row);
                index++;
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
