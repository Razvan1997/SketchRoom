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
        public IReadOnlyList<UIElement> SelectedElements => _selected.AsReadOnly();

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

            var allConnections = _connectorTool.GetAllConnections().Select(c => c.Visual).ToHashSet();

            foreach (UIElement element in canvas.Children.OfType<UIElement>().ToList())
            {
                if (element is FrameworkElement fe)
                {
                    var isConnector = fe.Tag?.ToString() == "Connector";
                    var isInteractive = fe.Tag?.ToString() == "interactive";
                    var isConnection = _connectorTool.GetAllConnections().Any(c => c.Visual == fe);
                    var isShapeInMap = _connectorTool.GetAllConnections().Any(c => c.From?.Visual == fe || c.To?.Visual == fe);

                    if (!isConnector && !isInteractive && !isConnection && !isShapeInMap)
                        continue;
                }
                Rect elementRect;
                UIElement? marker = null;

                if (element is Path path && path.Data != null)
                {
                    elementRect = path.Data.GetRenderBounds(new Pen(path.Stroke, path.StrokeThickness));

                    if (!bounds.IntersectsWith(elementRect))
                        continue;

                    marker = new Path
                    {
                        Data = path.Data.Clone(),
                        Stroke = Brushes.DeepSkyBlue,
                        StrokeThickness = 3,
                        StrokeDashArray = new DoubleCollection { 4, 2 },
                        IsHitTestVisible = false
                    };
                }
                else if (element is Canvas canvasWrapper)
                {
                    var path2 = canvasWrapper.Children.OfType<Path>().FirstOrDefault();
                    if (path2 != null && path2.Data != null)
                    {
                        elementRect = path2.Data.GetRenderBounds(new Pen(path2.Stroke, path2.StrokeThickness));

                        if (!bounds.IntersectsWith(elementRect))
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
                else if (element is FrameworkElement shapeFe)
                {
                    if (!shapeFe.IsLoaded || shapeFe.ActualWidth == 0 || shapeFe.ActualHeight == 0)
                        continue;

                    if (VisualTreeHelper.GetParent(shapeFe) is Visual parent)
                    {
                        var transform = shapeFe.TransformToAncestor(parent);
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
    }
}
