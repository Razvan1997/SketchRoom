using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows;
using System.Windows.Data;
using WhiteBoard.Core.Services.Interfaces;
using SketchRoom.Models.Enums;
using WhiteBoardModule.XAML.Interfaces;

namespace WhiteBoardModule.XAML.Shapes.General
{
    public class TriangleShapeRenderer : IShapeRenderer, IBackgroundChangable, IStrokeChangable
    {
        private readonly bool _withBindings;
        private readonly IShapeSelectionService _selectionService;
        private Polygon _triangle;
        public TriangleShapeRenderer(bool withBindings = false)
        {
            _withBindings = withBindings;
            _selectionService = ContainerLocator.Container.Resolve<IShapeSelectionService>();
        }

        public UIElement CreatePreview()
        {
            var content = Render();

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
                    Children = { content }
                }
            };
        }

        public UIElement Render()
        {
            var triangle = new Polygon
            {
                Points = new PointCollection
                {
                    new Point(50, 0),    // top center
                    new Point(100, 100), // bottom right
                    new Point(0, 100)    // bottom left
                },
                Fill = Brushes.Transparent,
                StrokeThickness = 2,
                IsHitTestVisible = false
            };

            var preferences = ContainerLocator.Container.Resolve<IDrawingPreferencesService>();
            triangle.Stroke = preferences.SelectedColor;

            var canvas = new Canvas
            {
                Width = 100,
                Height = 100,
                Background = Brushes.Transparent,
                Children = { triangle }
            };

            var viewbox = new Viewbox
            {
                Stretch = Stretch.Uniform,
                Margin = new Thickness(4),
                Child = canvas
            };

            viewbox.PreviewMouseLeftButtonDown += (s, e) =>
            {
                if (e.OriginalSource is Canvas canvas)
                {
                    var clickedTriangle = FindPolygonInCanvas(canvas);

                    if (clickedTriangle != null)
                    {
                        var pos = e.GetPosition(clickedTriangle);

                        if (IsMouseOverMargin(clickedTriangle, pos))
                            _selectionService.Select(ShapePart.Margin, clickedTriangle);
                        else
                            _selectionService.Select(ShapePart.Border, clickedTriangle);
                    }
                }
            };

            _triangle = triangle;
            return viewbox;
        }

        public void SetBackground(Brush brush)
        {
            _triangle?.SetValue(Shape.FillProperty, brush);
        }

        public void SetStroke(Brush brush)
        {
            _triangle?.SetValue(Shape.StrokeProperty, brush);
        }

        private bool IsMouseOverMargin(Polygon triangle, Point mousePos)
        {
            const double marginWidth = 6;

            var points = triangle.Points;
            if (points.Count < 3)
                return false;

            // verificăm față de fiecare muchie
            return IsNearLine(points[0], points[1], mousePos, marginWidth) ||
                   IsNearLine(points[1], points[2], mousePos, marginWidth) ||
                   IsNearLine(points[2], points[0], mousePos, marginWidth);
        }

        private bool IsNearLine(Point p1, Point p2, Point p, double threshold)
        {
            // distanța de la punct la segmentul de linie
            double dx = p2.X - p1.X;
            double dy = p2.Y - p1.Y;

            if (dx == 0 && dy == 0)
            {
                // segment de lungime 0
                dx = p.X - p1.X;
                dy = p.Y - p1.Y;
                return Math.Sqrt(dx * dx + dy * dy) <= threshold;
            }

            // proiecție pe segment
            double t = ((p.X - p1.X) * dx + (p.Y - p1.Y) * dy) / (dx * dx + dy * dy);
            t = Math.Max(0, Math.Min(1, t)); // clamp între 0 și 1

            double closestX = p1.X + t * dx;
            double closestY = p1.Y + t * dy;

            dx = p.X - closestX;
            dy = p.Y - closestY;

            return Math.Sqrt(dx * dx + dy * dy) <= threshold;
        }

        private Polygon? FindPolygonInCanvas(Canvas canvas)
        {
            foreach (var child in canvas.Children)
            {
                if (child is Polygon polygon)
                    return polygon;
            }
            return null;
        }
    }
}
