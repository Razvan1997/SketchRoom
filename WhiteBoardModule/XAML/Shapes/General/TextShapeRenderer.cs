using SketchRoom.Models.Enums;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using WhiteBoard.Core.Models;
using WhiteBoard.Core.Services.Interfaces;

namespace WhiteBoardModule.XAML.Shapes.General
{
    public class TextShapeRenderer : IShapeRenderer, IBackgroundChangable, IForegroundChangable, IRestoreFromShape
    {
        private readonly bool _withBindings;
        private readonly IShapeSelectionService _selectionService;
        private RichTextBox _richTextBox;
        public TextShapeRenderer(bool withBindings = false)
        {
            _withBindings = withBindings;
            _selectionService = ContainerLocator.Container.Resolve<IShapeSelectionService>();
        }

        public UIElement CreatePreview()
        {
            var previewContent = Render();

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
                    _selectionService.Select(ShapePart.Text, richText);
                }
            };
            richText.PreviewMouseLeftButtonDown += (s, e) =>
            {
                if (!richText.IsKeyboardFocusWithin)
                {
                    richText.Focus();
                    RaiseClickToParent(richText, e);
                    e.Handled = true; // Opțional dacă vrei să previi altceva
                }
            };

            _richTextBox = richText;

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

        public void SetBackground(Brush brush)
        {
            _richTextBox.Background = brush;
        }

        public void SetStroke(Brush brush)
        {
            //throw new NotImplementedException();
        }

        public void SetForeground(Brush brush)
        {
            _richTextBox.Foreground = brush;
            _richTextBox.Foreground = brush;

            foreach (Block block in _richTextBox.Document.Blocks)
            {
                if (block is Paragraph paragraph)
                {
                    foreach (Inline inline in paragraph.Inlines)
                    {
                        if (inline is Run run)
                        {
                            run.Foreground = brush;
                        }
                    }
                }
            }
        }

        public BPMNShapeModelWithPosition? ExportData(IInteractiveShape control)
        {
            if (control is not FrameworkElement fe)
                return null;

            var background = (_richTextBox?.Background as SolidColorBrush)?.Color.ToString() ?? "#00FFFFFF";
            var foreground = (_richTextBox?.Foreground as SolidColorBrush)?.Color.ToString() ?? "#FF000000";

            string textContent = "";
            if (_richTextBox?.Document != null)
            {
                var range = new TextRange(_richTextBox.Document.ContentStart, _richTextBox.Document.ContentEnd);
                textContent = range.Text.Trim();
            }

            return new BPMNShapeModelWithPosition
            {
                Type = ShapeType.ShapeText,
                Left = Canvas.GetLeft(fe),
                Top = Canvas.GetTop(fe),
                Width = fe.Width,
                Height = fe.Height,
                Name = fe.Name,
                Category = "General",
                SvgUri = null,
                ExtraProperties = new Dictionary<string, string>
        {
            { "Background", background },
            { "Foreground", foreground },
            { "Text", textContent }
        }
            };
        }

        public void Restore(Dictionary<string, string> extraProperties)
        {
            if (_richTextBox == null)
                return;

            if (extraProperties.TryGetValue("Background", out var bgColor))
            {
                try { _richTextBox.Background = (Brush)new BrushConverter().ConvertFromString(bgColor); }
                catch { _richTextBox.Background = Brushes.Transparent; }
            }

            if (extraProperties.TryGetValue("Foreground", out var fgColor))
            {
                try
                {
                    var brush = (Brush)new BrushConverter().ConvertFromString(fgColor);
                    SetForeground(brush); // actualizează și pe Run-uri
                }
                catch { }
            }

            if (extraProperties.TryGetValue("Text", out var text))
            {
                _richTextBox.Document.Blocks.Clear();
                var paragraph = new Paragraph(new Run(text))
                {
                    TextAlignment = TextAlignment.Center,
                    FontSize = 14,
                    Foreground = _richTextBox.Foreground
                };
                _richTextBox.Document.Blocks.Add(paragraph);
            }
        }
    }


}
