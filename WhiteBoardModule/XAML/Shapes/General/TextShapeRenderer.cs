using SketchRoom.Toolkit.Wpf.Converters;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using WhiteBoard.Core.Services.Interfaces;

namespace WhiteBoardModule.XAML.Shapes.General
{
    public class TextShapeRenderer : IShapeRenderer
    {
        private readonly bool _withBindings;

        public TextShapeRenderer(bool withBindings = false)
        {
            _withBindings = withBindings;
        }

        public UIElement CreatePreview()
        {
            var previewContent = Render();

            // Wrapper scalabil pentru preview în formă pătrată
            return new Viewbox
            {
                Width = 48,
                Height = 48,
                Stretch = Stretch.Uniform,
                Child = new Grid
                {
                    Width = 100,
                    Height = 100,
                    Background = Brushes.Transparent,
                    Children = { previewContent }
                }
            };
        }

        public UIElement Render()
        {
            var preferences = ContainerLocator.Container.Resolve<IDrawingPreferencesService>();

            var titleBox = new TextBox
            {
                Text = "Title",
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            titleBox.PreviewMouseLeftButtonDown += (s, e) =>
            {
                if (!titleBox.IsKeyboardFocusWithin)
                {
                    titleBox.Focus();
                    e.Handled = true;
                }

                RaiseClickToParent(titleBox, e);
            };

            var paragraphBox = new TextBox
            {
                Text = "Example paragraph for a longer description.",
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            paragraphBox.PreviewMouseLeftButtonDown += (s, e) =>
            {
                if (!paragraphBox.IsKeyboardFocusWithin)
                {
                    paragraphBox.Focus();
                    e.Handled = true;
                }

                RaiseClickToParent(paragraphBox, e);
            };

            if (_withBindings)
            {
                titleBox.SetBinding(TextBox.FontWeightProperty, new Binding(nameof(preferences.FontWeight)) { Source = preferences });
                titleBox.SetBinding(TextBox.FontSizeProperty, new Binding(nameof(preferences.FontSize)) { Source = preferences, Converter = new AddConverter(), ConverterParameter = 4 });
                titleBox.SetBinding(TextBox.ForegroundProperty, new Binding(nameof(preferences.SelectedColor)) { Source = preferences });

                paragraphBox.SetBinding(TextBox.FontWeightProperty, new Binding(nameof(preferences.FontWeight)) { Source = preferences });
                paragraphBox.SetBinding(TextBox.FontSizeProperty, new Binding(nameof(preferences.FontSize)) { Source = preferences });
                paragraphBox.SetBinding(TextBox.ForegroundProperty, new Binding(nameof(preferences.SelectedColor)) { Source = preferences });
            }
            else
            {
                titleBox.FontWeight = preferences.FontWeight;
                titleBox.FontSize = preferences.FontSize + 4;
                titleBox.Foreground = preferences.SelectedColor;

                paragraphBox.FontWeight = preferences.FontWeight;
                paragraphBox.FontSize = preferences.FontSize;
                paragraphBox.Foreground = preferences.SelectedColor;
            }

            return new Border
            {
                Padding = new Thickness(8),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(1),
                BorderBrush = Brushes.Transparent,
                Child = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Children = { titleBox, paragraphBox }
                }
            };
        }

        private void RaiseClickToParent(UIElement source, MouseButtonEventArgs e)
        {
            var parent = VisualTreeHelper.GetParent(source);
            while (parent != null && parent is not GenericShapeControl)
                parent = VisualTreeHelper.GetParent(parent);

            if (parent is GenericShapeControl shape)
            {
                shape.RaiseClick(e);
            }
        }
    }


}
