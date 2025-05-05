using SketchRoom.Models.Enums;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using WhiteBoard.Core.Models;
using WhiteBoard.Core.Services.Interfaces;

namespace WhiteBoardModule.XAML.Shapes.General
{
    public class EllipseShapeRenderer : IShapeRenderer, IBackgroundChangable, IStrokeChangable, IRestoreFromShape
    {
        private readonly bool _withBindings;
        private readonly IShapeSelectionService _selectionService;
        private Ellipse _ellipse;

        public EllipseShapeRenderer(bool withBindings = false)
        {
            _selectionService = ContainerLocator.Container.Resolve<IShapeSelectionService>();
            _withBindings = withBindings;
        }

        public UIElement Render()
        {
            var ellipse = new Ellipse
            {
                Fill = Brushes.Transparent,
                StrokeThickness = 2,
                Stretch = Stretch.Fill,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            var preferences = ContainerLocator.Container.Resolve<IDrawingPreferencesService>();
            ellipse.Stroke = preferences.SelectedColor;

            ellipse.PreviewMouseLeftButtonDown += (s, e) =>
            {
                var pos = e.GetPosition(ellipse);

                if (IsMouseOverMargin(ellipse, pos))
                    _selectionService.Select(ShapePart.Margin, ellipse);
                else
                    _selectionService.Select(ShapePart.Border, ellipse);
            };
            _ellipse = ellipse;
            return ellipse;
        }

        public UIElement CreatePreview()
        {
            var preferences = ContainerLocator.Container.Resolve<IDrawingPreferencesService>();
            var shape = new Ellipse
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
                    Children = { shape }
                }
            };
        }

        public void SetBackground(Brush brush)
        {
            _ellipse?.SetValue(Shape.FillProperty, brush);
        }

        public void SetStroke(Brush brush)
        {
            _ellipse?.SetValue(Shape.StrokeProperty, brush);
        }

        private bool IsMouseOverMargin(Ellipse ellipse, Point mousePos)
        {
            if (ellipse.ActualWidth <= 0 || ellipse.ActualHeight <= 0)
                return false;

            double centerX = ellipse.ActualWidth / 2;
            double centerY = ellipse.ActualHeight / 2;

            double radiusX = centerX;
            double radiusY = centerY;

            double normalizedX = (mousePos.X - centerX) / radiusX;
            double normalizedY = (mousePos.Y - centerY) / radiusY;

            double distance = Math.Sqrt(normalizedX * normalizedX + normalizedY * normalizedY);

            const double marginTolerance = 0.08;

            return Math.Abs(distance - 1) <= marginTolerance;
        }

        public BPMNShapeModelWithPosition? ExportData(IInteractiveShape control)
        {
            if (control is not FrameworkElement fe || _ellipse == null)
                return null;

            string? fillColor = (_ellipse.Fill as SolidColorBrush)?.Color.ToString();
            string? strokeColor = (_ellipse.Stroke as SolidColorBrush)?.Color.ToString();

            return new BPMNShapeModelWithPosition
            {
                Type = ShapeType.Ellipse,
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
            if (_ellipse == null)
                return;

            if (extraProperties.TryGetValue("Fill", out var fillHex))
            {
                try { _ellipse.Fill = (SolidColorBrush)(new BrushConverter().ConvertFromString(fillHex)); }
                catch { _ellipse.Fill = Brushes.Transparent; }
            }

            if (extraProperties.TryGetValue("Stroke", out var strokeHex))
            {
                try { _ellipse.Stroke = (SolidColorBrush)(new BrushConverter().ConvertFromString(strokeHex)); }
                catch { _ellipse.Stroke = Brushes.White; }
            }
        }

    }
}
