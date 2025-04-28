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

namespace WhiteBoardModule.XAML.Shapes.General
{
    public class StraightBraceRightShapeRenderer : IShapeRenderer
    {
        private readonly bool _withBindings;
        private readonly IShapeSelectionService _selectionService;
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
    }
}
