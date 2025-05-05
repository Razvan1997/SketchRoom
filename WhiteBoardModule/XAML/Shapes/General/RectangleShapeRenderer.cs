using SketchRoom.Models.Enums;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;
using WhiteBoard.Core.Models;
using WhiteBoard.Core.Services.Interfaces;

namespace WhiteBoardModule.XAML.Shapes.General
{
    public class RectangleShapeRenderer : IShapeRenderer, IBackgroundChangable, IStrokeChangable, IRestoreFromShape
    {
        private readonly bool _withBindings;
        private readonly IShapeSelectionService _selectionService;

        private Rectangle? _rectangle; // 🔵 Rectangle-ul real

        public RectangleShapeRenderer(bool withBindings = false)
        {
            _selectionService = ContainerLocator.Container.Resolve<IShapeSelectionService>();
            _withBindings = withBindings;
        }

        public UIElement CreatePreview()
        {
            var preferences = ContainerLocator.Container.Resolve<IDrawingPreferencesService>();

            var previewContent = new Rectangle
            {
                Fill = Brushes.Transparent,
                StrokeThickness = 2,
                Stretch = Stretch.Fill,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Stroke = preferences.SelectedColor
            };

            return new Viewbox
            {
                Width = 48,
                Height = 48,
                Stretch = Stretch.Uniform,
                Child = new Grid
                {
                    Width = 80,
                    Height = 80,
                    Background = Brushes.Transparent,
                    Children = { previewContent }
                }
            };
        }

        public UIElement Render()
        {
            var rect = new Rectangle
            {
                Fill = Brushes.Transparent,
                StrokeThickness = 2,
                Stretch = Stretch.Fill,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            if (_withBindings)
            {
                var preferences = ContainerLocator.Container.Resolve<IDrawingPreferencesService>();
                rect.SetBinding(Shape.StrokeProperty, new Binding(nameof(preferences.SelectedColor))
                {
                    Source = preferences
                });
            }
            else
            {
                var preferences = ContainerLocator.Container.Resolve<IDrawingPreferencesService>();
                rect.Stroke = preferences.SelectedColor;
            }

            rect.PreviewMouseLeftButtonDown += (s, e) =>
            {
                var pos = e.GetPosition(rect);

                if (IsMouseOverMargin(rect, pos))
                    _selectionService.Select(ShapePart.Margin, rect);
                else
                    _selectionService.Select(ShapePart.Border, rect);
            };

            _rectangle = rect; // 🔵 Stocăm Rectangle-ul ca să-l putem modifica ulterior

            return rect;
        }

        public void SetBackground(Brush brush)
        {
            _rectangle?.SetValue(Shape.FillProperty, brush);
        }

        public void SetStroke(Brush brush)
        {
            _rectangle?.SetValue(Shape.StrokeProperty, brush);
        }

        private bool IsMouseOverMargin(Rectangle rect, Point mousePos)
        {
            const double marginWidth = 6;

            return mousePos.X < marginWidth ||
                   mousePos.X > rect.ActualWidth - marginWidth ||
                   mousePos.Y < marginWidth ||
                   mousePos.Y > rect.ActualHeight - marginWidth;
        }

        public BPMNShapeModelWithPosition? ExportData(IInteractiveShape control)
        {
            if (control is not FrameworkElement fe || _rectangle == null)
                return null;

            string? fillColor = (_rectangle.Fill as SolidColorBrush)?.Color.ToString();
            string? strokeColor = (_rectangle.Stroke as SolidColorBrush)?.Color.ToString();

            return new BPMNShapeModelWithPosition
            {
                Type = ShapeType.Rectangle,
                Left = Canvas.GetLeft(fe),
                Top = Canvas.GetTop(fe),
                Width = fe.Width,
                Height = fe.Height,
                Name = fe.Name,
                Category = "General",
                SvgUri = null,
                ExtraProperties = new Dictionary<string, string>
        {
            { "Fill", fillColor ?? "#00FFFFFF" },      // Transparent fallback
            { "Stroke", strokeColor ?? "#FFFFFFFF" }   // White fallback
        }
            };
        }

        public void Restore(Dictionary<string, string> extraProperties)
        {
            if (_rectangle == null)
                return;

            if (extraProperties.TryGetValue("Fill", out var fillHex))
            {
                try { _rectangle.Fill = (SolidColorBrush)(new BrushConverter().ConvertFromString(fillHex)); }
                catch { _rectangle.Fill = Brushes.Transparent; }
            }

            if (extraProperties.TryGetValue("Stroke", out var strokeHex))
            {
                try { _rectangle.Stroke = (SolidColorBrush)(new BrushConverter().ConvertFromString(strokeHex)); }
                catch { _rectangle.Stroke = Brushes.White; }
            }
        }
    }
}
