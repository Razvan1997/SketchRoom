using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;
using WhiteBoard.Core.Services.Interfaces;

namespace WhiteBoardModule.XAML.Shapes.Entity
{
    public class ObjectTypeShapeRenderer : IShapeRenderer
    {
        private readonly bool _withBindings;
        private static readonly List<string> _types = new() { "string", "int", "bool", "float", "object" };
        private static readonly List<string> _access = new() { "public", "private", "protected", "internal" };

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

            var mainStack = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Background = Brushes.Transparent
            };

            var titleBox = new TextBox
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

            mainStack.Children.Add(titleBox);

            var fieldPanel = new StackPanel { Name = "FieldsPanel" };

            // Adaugă câteva default
            for (int i = 0; i < 3; i++)
                fieldPanel.Children.Add(CreateFieldRow(preferences, fieldPanel));

            mainStack.Children.Add(fieldPanel);

            // Buton + jos
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
                fieldPanel.Children.Add(CreateFieldRow(preferences, fieldPanel));
            };

            mainStack.Children.Add(addButton);

            // Încapsulare în Border stil Entity
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
    }
}
