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
using WhiteBoard.Core.Tools;

namespace WhiteBoard.Core.Services
{
    public class SelectionService : ISelectionService
    {
        private readonly BpmnConnectorTool _connectorTool;
        private readonly List<UIElement> _selected = new();
        private readonly Dictionary<UIElement, UIElement> _selectionMarkers = new(); // poate fi Path sau Rectangle
        public event EventHandler? SelectionChanged;
        public IReadOnlyList<UIElement> SelectedElements
        {
            get
            {
                var connectorVisuals = _connectorTool.SelectedConnections
                    .Where(c => c.Visual is UIElement)
                    .Select(c => c.Visual!)
                    .ToList();

                return _selected.Concat(connectorVisuals).Distinct().ToList();
            }
        }

        public SelectionService(BpmnConnectorTool connectorTool)
        {
            _connectorTool = connectorTool;
        }

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

            var allConnections = _connectorTool.GetAllConnections().ToList();
            var selectionGeometry = new RectangleGeometry(bounds);

            foreach (UIElement element in canvas.Children.OfType<UIElement>().ToList())
            {
                bool isShapeOrConnection = allConnections.Any(c => c.Visual == element) ||
                                           allConnections.Any(c => c.From?.Visual == element || c.To?.Visual == element) ||
                                           (element is FrameworkElement f && f.Tag?.ToString() == "interactive");

                if (!isShapeOrConnection)
                    continue;

                UIElement? marker = null;

                // Detectează dacă elementul este un Path (conexiune directă)
                if (element is Path path && path.Data != null)
                {
                    var pen = new Pen(path.Stroke, path.StrokeThickness);
                    var widenedGeometry = path.Data.GetWidenedPathGeometry(pen);

                    if (!widenedGeometry.FillContainsWithDetail(selectionGeometry).HasFlag(IntersectionDetail.Intersects))
                        continue;

                    var conn = FindConnectionByPath(path);
                    if (conn != null)
                        _connectorTool.AddToSelection(conn);

                    marker = new Path
                    {
                        Data = path.Data.Clone(),
                        Stroke = Brushes.DeepSkyBlue,
                        StrokeThickness = 3,
                        StrokeDashArray = new DoubleCollection { 4, 2 },
                        IsHitTestVisible = false
                    };
                }
                // Detectează dacă elementul este un Canvas care conține un Path (conexiune)
                else if (element is Canvas canvasWrapper)
                {
                    var path2 = canvasWrapper.Children.OfType<Path>().FirstOrDefault();
                    if (path2 != null && path2.Data != null)
                    {
                        var pen = new Pen(path2.Stroke, path2.StrokeThickness);
                        var widenedGeometry = path2.Data.GetWidenedPathGeometry(pen);

                        if (!selectionGeometry.FillContainsWithDetail(widenedGeometry).HasFlag(IntersectionDetail.FullyContains) &&
                        !selectionGeometry.FillContainsWithDetail(widenedGeometry).HasFlag(IntersectionDetail.Intersects))
                            continue;

                        var conn = FindConnectionByVisual(canvasWrapper);
                        if (conn != null)
                            _connectorTool.AddToSelection(conn);

                        marker = new Path
                        {
                            Data = path2.Data.Clone(),
                            Stroke = Brushes.DeepSkyBlue,
                            StrokeThickness = 3,
                            StrokeDashArray = new DoubleCollection { 4, 2 },
                            IsHitTestVisible = false
                        };

                        Canvas.SetLeft(marker, 0);
                        Canvas.SetTop(marker, 0);
                    }
                }
                // Detectează shape-uri (entități)
                else if (element is FrameworkElement shapeFe)
                {
                    if (!shapeFe.IsLoaded || shapeFe.ActualWidth == 0 || shapeFe.ActualHeight == 0)
                        continue;

                    var transform = shapeFe.TransformToAncestor(canvas);
                    var transformedBounds = transform.TransformBounds(new Rect(0, 0, shapeFe.ActualWidth, shapeFe.ActualHeight));

                    if (!bounds.IntersectsWith(transformedBounds))
                        continue;

                    marker = new Rectangle
                    {
                        Width = transformedBounds.Width,
                        Height = transformedBounds.Height,
                        Stroke = Brushes.DeepSkyBlue,
                        StrokeThickness = 2,
                        StrokeDashArray = new DoubleCollection { 4, 2 },
                        IsHitTestVisible = false
                    };

                    Canvas.SetLeft(marker, transformedBounds.Left);
                    Canvas.SetTop(marker, transformedBounds.Top);
                }

                if (marker != null)
                {
                    _selected.Add(element);
                    _selectionMarkers[element] = marker;
                    canvas.Children.Add(marker);

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

        private BPMNConnection? FindConnectionByPath(Path path)
        {
            return _connectorTool.GetAllConnections()
                .FirstOrDefault(c =>
                    c.Visual is Canvas canvas &&
                    canvas.Children.OfType<Path>().Any(p => ReferenceEquals(p, path)));
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

        private BPMNConnection? FindConnectionByVisual(UIElement visual)
        {
            return _connectorTool.GetAllConnections()
                .FirstOrDefault(c => c.Visual == visual);
        }

        public void DeselectAll(Canvas canvas)
        {
            foreach (var marker in _selectionMarkers.Values)
            {
                if (marker is Shape shape)
                    shape.BeginAnimation(Shape.StrokeDashOffsetProperty, null);

                canvas.Children.Remove(marker);
            }

            _selectionMarkers.Clear();
            _selected.Clear();

            _connectorTool.DeselectAllConnections();

            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        public void UpdateSelectionMarkersPosition()
        {
            foreach (var pair in _selectionMarkers)
            {
                var element = pair.Key;
                var marker = pair.Value;

                if (element is FrameworkElement fe && marker is Rectangle rect)
                {
                    double left = Canvas.GetLeft(fe);
                    double top = Canvas.GetTop(fe);

                    rect.Width = fe.ActualWidth;
                    rect.Height = fe.ActualHeight;

                    Canvas.SetLeft(rect, left);
                    Canvas.SetTop(rect, top);
                }
            }
        }
        public IEnumerable<BPMNConnection> GetAllConnections()
        {
            return _connectorTool.GetAllConnections();
        }
    }
}
