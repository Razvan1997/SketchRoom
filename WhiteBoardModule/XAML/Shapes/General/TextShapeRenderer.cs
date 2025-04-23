using SketchRoom.Models.Enums;
using SketchRoom.Toolkit.Wpf.Converters;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using WhiteBoard.Core.Services;
using WhiteBoard.Core.Services.Interfaces;

namespace WhiteBoardModule.XAML.Shapes.General
{
    public class TextShapeRenderer : IShapeRenderer
    {
        private readonly bool _withBindings;
        private readonly IShapeSelectionService _selectionService;
        public TextShapeRenderer(bool withBindings = false)
        {
            _withBindings = withBindings;
            _selectionService = ContainerLocator.Container.Resolve<IShapeSelectionService>();
        }

        public UIElement CreatePreview()
        {
            var previewContent = Render();

            // Wrapper scalabil pentru preview în formă pătrată
            return new Viewbox
            {
                Width = 100,
                Height = 100,
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

            var richText = new RichTextBox
            {
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(4),
                Tag = "interactive",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                IsDocumentEnabled = true,
                AcceptsReturn = true
            };

            // Creează documentul
            var document = new FlowDocument
            {
                TextAlignment = TextAlignment.Center,
                PagePadding = new Thickness(0),
            };

            // Titlu
            var title = new Paragraph(new Run("Title"))
            {
                FontSize = preferences.FontSize + 4,
                FontWeight = preferences.FontWeight,
                Foreground = preferences.SelectedColor,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 8)
            };

            // Paragraf
            var paragraph = new Paragraph(new Run("Example paragraph for a longer description."))
            {
                FontSize = preferences.FontSize,
                FontWeight = preferences.FontWeight,
                Foreground = preferences.SelectedColor,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0)
            };

            document.Blocks.Add(title);
            document.Blocks.Add(paragraph);
            richText.Document = document;

            // Focus și selecție
            richText.MouseDoubleClick += (s, e) =>
            {
                if (!richText.IsKeyboardFocusWithin)
                {
                    richText.Focus();
                }

                RaiseClickToParent(richText, e);
            };

            richText.MouseMove += (s, e) =>
            {
                if (richText.IsFocused && !richText.Selection.IsEmpty)
                {
                    _selectionService.SelectRich(ShapePart.Text, GetContainingBorder(richText), richText);
                }
            };

            return new Border
            {
                Padding = new Thickness(8),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(1),
                BorderBrush = Brushes.Transparent,
                Tag = "interactive",
                Child = richText
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

        private Border GetContainingBorder(DependencyObject element)
        {
            while (element != null && element is not Border)
                element = VisualTreeHelper.GetParent(element);
            return element as Border;
        }
    }


}
