using SketchRoom.Models.Enums;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using WhiteBoard.Core.Models;
using WhiteBoard.Core.Services.Interfaces;

namespace WhiteBoardModule.XAML.Shapes.General
{
    public class StraightBraceRightShapeRenderer : IShapeRenderer, IRestoreFromShape
    {
        private readonly bool _withBindings;
        private readonly IShapeSelectionService _selectionService;
        private Grid? _lastRenderedGrid;
        public StraightBraceRightShapeRenderer(bool withBindings = false)
        {
            _withBindings = withBindings;
            _selectionService = ContainerLocator.Container.Resolve<IShapeSelectionService>();
        }

        public UIElement CreatePreview()
        {
            var shape = CreateBrace();

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

        public UIElement Render()
        {
            var grid = new Grid
            {
                Background = Brushes.Transparent,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            var brace = CreateBrace();
            grid.Children.Add(brace);

            grid.PreviewMouseLeftButtonDown += (s, e) =>
            {
                var clickedPath = FindFirstPathInChildren(grid);

                if (clickedPath != null)
                {
                    var pos = e.GetPosition(clickedPath);
                    _selectionService.Select(ShapePart.Margin, clickedPath);
                }
            };
            _lastRenderedGrid = grid;
            return grid;
        }

        private UIElement CreateBrace()
        {
            var geometry = new StreamGeometry();

            using (var ctx = geometry.Open())
            {
                // Linie verticală
                ctx.BeginFigure(new Point(0.66, 0.0), false, false);
                ctx.LineTo(new Point(0.66, 1.0), true, false);

                // Linie sus spre dreapta
                ctx.BeginFigure(new Point(0.66, 0.0), false, false);
                ctx.LineTo(new Point(1.0, 0.0), true, false);

                // Linie jos spre dreapta
                ctx.BeginFigure(new Point(0.66, 1.0), false, false);
                ctx.LineTo(new Point(1.0, 1.0), true, false);

                // Linie mijloc spre stânga
                ctx.BeginFigure(new Point(0.66, 0.5), false, false);
                ctx.LineTo(new Point(0.0, 0.5), true, false);
            }

            geometry.Freeze();

            return new Path
            {
                Data = geometry,
                Stroke = Brushes.White,
                StrokeThickness = 2,
                Tag = "Bracket",
                Stretch = Stretch.Fill,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
        }

        private bool IsMouseOverMargin(Path path, Point mousePos)
        {
            const double marginWidth = 6;

            Rect bounds = path.RenderedGeometry.Bounds;

            return mousePos.X < bounds.Left + marginWidth ||
                   mousePos.X > bounds.Right - marginWidth ||
                   mousePos.Y < bounds.Top + marginWidth ||
                   mousePos.Y > bounds.Bottom - marginWidth;
        }

        private Path? FindFirstPathInChildren(DependencyObject parent)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is Path path)
                    return path;

                var found = FindFirstPathInChildren(child);
                if (found != null)
                    return found;
            }
            return null;
        }

        public BPMNShapeModelWithPosition? ExportData(IInteractiveShape control)
        {
            if (control is not FrameworkElement fe)
                return null;

            var path = FindFirstPathInChildren(fe) as Path;

            string? strokeColor = (path?.Stroke as SolidColorBrush)?.Color.ToString();

            return new BPMNShapeModelWithPosition
            {
                Type = ShapeType.StraightBraceRightShape,
                Left = Canvas.GetLeft(fe),
                Top = Canvas.GetTop(fe),
                Width = fe.Width,
                Height = fe.Height,
                Name = fe.Name,
                Category = "General",
                SvgUri = null,
                ExtraProperties = new Dictionary<string, string>
        {
            { "Stroke", strokeColor ?? "#FFFFFFFF" },
            { "StrokeThickness", (path?.StrokeThickness.ToString() ?? "2") }
        }
            };
        }

        public void Restore(Dictionary<string, string> extraProperties)
        {
            var path = FindFirstPathInChildren(_lastRenderedGrid ?? new Grid());
            if (path == null)
                return;

            if (extraProperties.TryGetValue("Stroke", out var strokeHex))
            {
                try { path.Stroke = (SolidColorBrush)(new BrushConverter().ConvertFromString(strokeHex)); }
                catch { path.Stroke = Brushes.White; }
            }

            if (extraProperties.TryGetValue("StrokeThickness", out var thicknessStr) &&
                double.TryParse(thicknessStr, out var thickness))
            {
                path.StrokeThickness = thickness;
            }
        }
    }
}
