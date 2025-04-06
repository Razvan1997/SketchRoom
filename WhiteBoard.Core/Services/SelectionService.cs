using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using WhiteBoard.Core.Models;
using WhiteBoard.Core.Services.Interfaces;

namespace WhiteBoard.Core.Services
{
    public class SelectionService : ISelectionService
    {
        private readonly List<UIElement> _selected = new();
        private readonly Dictionary<UIElement, UIElement> _selectionMarkers = new(); // poate fi Path sau Rectangle
        public event EventHandler? SelectionChanged;
        public IReadOnlyList<UIElement> SelectedElements => _selected.AsReadOnly();

        public Rect GetBoundsFromPoints(IEnumerable<Point> points)
        {
            if (points == null || !points.Any())
                return Rect.Empty;

            double minX = points.Min(p => p.X);
            double minY = points.Min(p => p.Y);
            double maxX = points.Max(p => p.X);
            double maxY = points.Max(p => p.Y);

            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }

        public void HandleSelection(Rect bounds, Canvas canvas)
        {
            ClearSelection(canvas);

            foreach (UIElement element in canvas.Children.OfType<UIElement>().ToList())
            {
                Rect elementRect;
                UIElement? marker = null;

                if (element is FrameworkElement fe && !(element is Path))
                {
                    double left = Canvas.GetLeft(fe);
                    double top = Canvas.GetTop(fe);
                    double width = fe.ActualWidth;
                    double height = fe.ActualHeight;

                    if (width == 0 || height == 0)
                        continue;

                    elementRect = new Rect(left, top, width, height);

                    if (!bounds.IntersectsWith(elementRect))
                        continue;

                    marker = new Rectangle
                    {
                        Width = width,
                        Height = height,
                        Stroke = Brushes.DeepSkyBlue,
                        StrokeThickness = 2,
                        StrokeDashArray = new DoubleCollection { 4, 2 },
                        IsHitTestVisible = false,
                        RadiusX = (fe is Ellipse) ? width / 2 : 0,
                        RadiusY = (fe is Ellipse) ? height / 2 : 0
                    };

                    Canvas.SetLeft(marker, left);
                    Canvas.SetTop(marker, top);
                }
                else if (element is Path path && path.Data != null)
                {
                    elementRect = path.Data.GetRenderBounds(new Pen(path.Stroke, path.StrokeThickness));

                    if (!bounds.IntersectsWith(elementRect))
                        continue;

                    marker = new Path
                    {
                        Data = path.Data.Clone(), // clone geometry
                        Stroke = Brushes.DeepSkyBlue,
                        StrokeThickness = 3,
                        StrokeDashArray = new DoubleCollection { 4, 2 },
                        IsHitTestVisible = false
                    };
                }

                if (marker != null)
                {
                    _selected.Add(element);
                    _selectionMarkers[element] = marker;
                    canvas.Children.Add(marker);

                    // animare stroke
                    var animation = new DoubleAnimation
                    {
                        From = 0,
                        To = 6,
                        Duration = new Duration(TimeSpan.FromSeconds(0.5)),
                        RepeatBehavior = RepeatBehavior.Forever
                    };

                    if (marker is Shape shape)
                        shape.BeginAnimation(Shape.StrokeDashOffsetProperty, animation);
                }
            }

            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        public void ClearSelection(Canvas canvas)
        {
            foreach (var marker in _selectionMarkers.Values)
            {
                if (marker is Shape shape)
                    shape.BeginAnimation(Shape.StrokeDashOffsetProperty, null);

                canvas.Children.Remove(marker);
            }

            _selectionMarkers.Clear();
            _selected.Clear();
        }
    }
}
