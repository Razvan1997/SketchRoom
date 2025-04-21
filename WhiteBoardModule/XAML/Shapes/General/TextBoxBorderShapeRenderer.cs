using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows;
using WhiteBoard.Core.Services.Interfaces;
using SketchRoom.Models.Enums;
using System.Windows.Input;

namespace WhiteBoardModule.XAML.Shapes.General
{
    public class TextBoxBorderShapeRenderer : IShapeRenderer
    {
        private readonly bool _withBindings;
        private readonly IShapeSelectionService _selectionService;

        public TextBoxBorderShapeRenderer(bool withBindings = false)
        {
            _withBindings = withBindings;
            _selectionService = ContainerLocator.Container.Resolve<IShapeSelectionService>();
        }

        public UIElement Render()
        {
            var preferences = ContainerLocator.Container.Resolve<IDrawingPreferencesService>();

            var border = new Border
            {
                BorderThickness = new Thickness(2),
                Background = Brushes.White,
                CornerRadius = new CornerRadius(8),
                BorderBrush = Brushes.Black,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            var textBox = new TextBox
            {
                Text = "Editable text",
                Width = 150,
                Height = 40,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                FontSize = 16,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                Foreground = Brushes.Black
            };

            var textContainer = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            textContainer.Children.Add(textBox);
            border.Child = textContainer;

            // Click în zona border-ului
            border.PreviewMouseLeftButtonDown += (s, e) =>
            {
                var pos = e.GetPosition(border);

                if (IsMouseOver(textBox, e))
                {
                    _selectionService.Select(ShapePart.Text, border, textBox);
                    return;
                }

                if (IsMouseOverMargin(border, pos))
                    _selectionService.Select(ShapePart.Margin, border, textBox);
                else
                    _selectionService.Select(ShapePart.Border, border, textBox);
            };

            // Hover logic
            border.MouseMove += (s, e) =>
            {
                if (_selectionService.Current == ShapePart.Margin) return;

                var pos = e.GetPosition(border);
                border.BorderThickness = IsMouseOverMargin(border, pos) ? new Thickness(4) : new Thickness(2);
            };

            border.MouseLeave += (s, e) =>
            {
                if (_selectionService.Current != ShapePart.Margin)
                    border.BorderThickness = new Thickness(2);
            };

            _selectionService.ApplyVisual(border, textBox);
            return border;
        }

        private bool IsMouseOver(UIElement element, MouseEventArgs e)
        {
            var pos = e.GetPosition(element);
            var rect = new Rect(0, 0, element.RenderSize.Width, element.RenderSize.Height);
            return rect.Contains(pos);
        }

        private bool IsMouseOverMargin(Border border, Point mousePos)
        {
            const double marginWidth = 6;

            return mousePos.X < marginWidth ||
                   mousePos.X > border.ActualWidth - marginWidth ||
                   mousePos.Y < marginWidth ||
                   mousePos.Y > border.ActualHeight - marginWidth;
        }

        public UIElement CreatePreview()
        {
            return new Viewbox
            {
                Width = 60,
                Height = 60,
                Stretch = Stretch.Uniform,
                Child = new Border
                {
                    BorderThickness = new Thickness(2),
                    BorderBrush = Brushes.Gray,
                    Background = Brushes.White,
                    Padding = new Thickness(8),
                    CornerRadius = new CornerRadius(8),
                    Width = 80,
                    Height = 50,
                    Child = new TextBlock
                    {
                        Text = "Preview",
                        FontSize = 16,
                        Foreground = Brushes.Gray,
                        Background = Brushes.Transparent,
                        TextAlignment = TextAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                }
            };
        }
    }
}
