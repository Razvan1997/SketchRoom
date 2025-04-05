using SharpVectors.Converters;
using SketchRoom.Models.Shapes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SketchRoom.Toolkit.Wpf.Controls
{
    /// <summary>
    /// Interaction logic for ShapesControl.xaml
    /// </summary>
    public partial class ShapesControl : System.Windows.Controls.UserControl
    {
        public ShapesControl()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(ShapesControl), new PropertyMetadata(null));

        public static readonly DependencyProperty SearchTextProperty =
            DependencyProperty.Register(nameof(SearchText), typeof(string), typeof(ShapesControl), new PropertyMetadata(string.Empty));

        public IEnumerable ItemsSource
        {
            get => (IEnumerable)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public string SearchText
        {
            get => (string)GetValue(SearchTextProperty);
            set => SetValue(SearchTextProperty, value);
        }

        public event Action<object>? ShapeDragStarted;

        private bool _isDragging = false;
        private DragAdorner? _dragAdorner;
        private AdornerLayer? _adornerLayer;
        private UIElement? _adornerTarget;

        private MouseEventHandler? _mouseMoveHandler;

        private void OnShapeMouseDown(object sender, MouseButtonEventArgs e)
        {
            _isDragging = true;
        }

        private void OnShapeMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
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
                else
                {
                    return;
                }

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

                DragDrop.DoDragDrop(border, shape, System.Windows.DragDropEffects.Copy);

                if (_adornerLayer != null && _dragAdorner != null)
                {
                    _adornerLayer.Remove(_dragAdorner);
                }

                CompositionTarget.Rendering -= OnRendering;
                _dragAdorner = null;
                _adornerLayer = null;

                ShapeDragStarted?.Invoke(shape);
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
            _left = x - 40; // offset to center
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
            return _transform; // poziționează corect SVG-ul sub cursor
        }
    }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;
    }

    public static class NativeMethods
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        public static extern bool GetCursorPos(out POINT lpPoint);


        public static Point GetMouseScreenPosition()
        {
            NativeMethods.GetCursorPos(out POINT point);
            return new Point(point.X, point.Y);
        }
    }
}
    
