using SharpVectors.Converters;
using SketchRoom.Models.Enums;
using SketchRoom.Models.Shapes;
using System.Collections;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace SketchRoom.Toolkit.Wpf.Controls
{
    public partial class ShapesControl : UserControl
    {
        private CollectionViewSource _groupedItemsSource = new();
        public Func<ShapeType, UIElement>? PreviewFactory { get; set; }

        private Popup? _hoverPreview;
        public ShapesControl()
        {
            InitializeComponent();
            _groupedItemsSource.GroupDescriptions.Add(new PropertyGroupDescription("Category"));
        }

        public static readonly DependencyProperty ItemsSourceProperty =
     DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable<ShapeCategoryGroup>), typeof(ShapesControl),
         new PropertyMetadata(null));

        public IEnumerable<ShapeCategoryGroup> ItemsSource
        {
            get => (IEnumerable<ShapeCategoryGroup>)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public static readonly DependencyProperty SearchTextProperty =
            DependencyProperty.Register(nameof(SearchText), typeof(string), typeof(ShapesControl),
                new PropertyMetadata(string.Empty, OnSearchTextChanged));


        public string SearchText
        {
            get => (string)GetValue(SearchTextProperty);
            set => SetValue(SearchTextProperty, value);
        }

        public ICollectionView GroupedItems => _groupedItemsSource.View;

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ShapesControl control && e.NewValue is IEnumerable<BPMNShapeModel> allShapes)
            {
                control.ApplySearchFilter(allShapes);
            }
        }

        private static void OnSearchTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ShapesControl control && control.ItemsSource is IEnumerable<BPMNShapeModel> allShapes)
            {
                control.ApplySearchFilter(allShapes);
            }
        }

        private void ApplySearchFilter(IEnumerable<BPMNShapeModel> allShapes)
        {
            var filtered = string.IsNullOrWhiteSpace(SearchText)
                ? allShapes
                : allShapes.Where(s => s.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase)).ToList();

            _groupedItemsSource.Source = filtered;
            _groupedItemsSource.View?.Refresh();
        }

        public event Action<object>? ShapeDragStarted;

        private bool _isDragging = false;
        private DragAdorner? _dragAdorner;
        private AdornerLayer? _adornerLayer;
        private UIElement? _adornerTarget;

        private void OnShapeMouseDown(object sender, MouseButtonEventArgs e)
        {
            _isDragging = true;
        }

        private void OnShapeMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDragging || e.LeftButton != MouseButtonState.Pressed)
                return;

            if (sender is Border border && border.DataContext is BPMNShapeModel shape)
            {
                _isDragging = false;
                UIElement preview;

                if (shape.SvgUri is not null)
                {
                    preview = new SvgViewbox
                    {
                        Source = shape.SvgUri,
                        Width = 80,
                        Height = 80
                    };
                }
                else if (shape.ShapeContent is UIElement element)
                {
                    preview = element;
                }
                else return;

                _adornerTarget = Window.GetWindow(this)?.Content as UIElement;

                if (_adornerTarget != null)
                {
                    _adornerLayer = AdornerLayer.GetAdornerLayer(_adornerTarget);

                    if (_adornerLayer != null)
                    {
                        _dragAdorner = new DragAdorner(_adornerTarget, preview);
                        _adornerLayer.Add(_dragAdorner);
                        CompositionTarget.Rendering += OnRendering;
                    }
                }

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    DragDrop.DoDragDrop(border, shape, DragDropEffects.Copy);

                    if (_adornerLayer != null && _dragAdorner != null)
                    {
                        _adornerLayer.Remove(_dragAdorner);
                    }

                    CompositionTarget.Rendering -= OnRendering;
                    _dragAdorner = null;
                    _adornerLayer = null;

                    ShapeDragStarted?.Invoke(shape);

                }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            }
        }

        private void OnShapeMouseUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
        }

        private void OnRendering(object? sender, EventArgs e)
        {
            if (_dragAdorner != null)
            {
                var screenPos = NativeMethods.GetMouseScreenPosition();
                var relative = _adornerTarget?.PointFromScreen(screenPos) ?? new Point(0, 0);
                _dragAdorner.UpdatePosition(relative.X, relative.Y);
            }
        }

        private void OnShapeMouseEnter(object sender, MouseEventArgs e)
        {
            //if (sender is Border border && border.DataContext is BPMNShapeModel shape)
            //{
            //    UIElement content;

            //    if (shape.SvgUri != null)
            //    {
            //        content = new SvgViewbox
            //        {
            //            Source = shape.SvgUri,
            //            Width = 100,
            //            Height = 100,
            //            Stretch = Stretch.Uniform
            //        };
            //    }
            //    else if (shape.Type.HasValue && PreviewFactory is not null)
            //    {
            //        content = PreviewFactory.Invoke(shape.Type.Value);
            //    }
            //    else return;
            //    content.RenderTransform = new ScaleTransform(1.5, 1.5);
            //    content.RenderTransformOrigin = new Point(0.5, 0.5);
            //    _hoverPreview = new Popup
            //    {
            //        AllowsTransparency = true,
            //        Placement = PlacementMode.Mouse,
            //        StaysOpen = false,
            //        PopupAnimation = PopupAnimation.Fade,
            //        Child = new Border
            //        {
            //            Background = Brushes.White,
            //            Padding = new Thickness(8),
            //            CornerRadius = new CornerRadius(6),
            //            BorderBrush = Brushes.Black,
            //            BorderThickness = new Thickness(1),
            //            Child = content,
            //            Width = 150, // sau orice dimensiune vrei
            //            Height = 150,
            //        }
            //    };

            //    PopupHost.Children.Add(_hoverPreview);
            //    _hoverPreview.IsOpen = true;
            //}
        }

        private void OnShapeMouseLeave(object sender, MouseEventArgs e)
        {
            //if (_hoverPreview is not null)
            //{
            //    _hoverPreview.IsOpen = false;
            //    PopupHost.Children.Remove(_hoverPreview);
            //    _hoverPreview = null;
            //}
        }
    }

    public class DragAdorner : Adorner
    {
        private readonly VisualBrush _brush;
        private readonly TranslateTransform _transform;

        private double _left;
        private double _top;

        public DragAdorner(UIElement adornedElement, UIElement visual)
            : base(adornedElement)
        {
            _brush = new VisualBrush(visual)
            {
                Opacity = 0.85,
                Stretch = Stretch.Uniform
            };

            _transform = new TranslateTransform();
            IsHitTestVisible = false;
        }

        public void UpdatePosition(double x, double y)
        {
            _left = x - 40;
            _top = y - 40;
            _transform.X = _left;
            _transform.Y = _top;

            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext dc)
        {
            dc.DrawRectangle(_brush, null, new Rect(0, 0, 80, 80));
        }

        protected override Size MeasureOverride(Size constraint) => new Size(80, 80);
        protected override Size ArrangeOverride(Size finalSize) => finalSize;

        public override GeneralTransform GetDesiredTransform(GeneralTransform transform)
        {
            return _transform;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;
    }

    public static class NativeMethods
    {
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetCursorPos(out POINT lpPoint);

        public static Point GetMouseScreenPosition()
        {
            GetCursorPos(out POINT point);
            return new Point(point.X, point.Y);
        }
    }
}