using SketchRoom.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WhiteBoard.Core.Services.Interfaces;

namespace WhiteBoardModule.XAML
{
    /// <summary>
    /// Interaction logic for EllipseShape.xaml
    /// </summary>
    public partial class GenericShapeControl : UserControl, IInteractiveShape
    {
        public event EventHandler<string>? ConnectionPointClicked;
        public event MouseButtonEventHandler? ShapeClicked;
        public bool EnableConnectors { get; set; } = false;

        public GenericShapeControl()
        {
            InitializeComponent();

            this.MouseLeftButtonDown += OnMouseLeftButtonDown;
            this.MouseMove += ForwardMouseMove;
            this.MouseUp += ForwardMouseUp;
            this.MouseDown += ForwardMouseDown;

            this.MouseEnter += (_, _) => ShowConnectors();
            this.MouseLeave += (_, _) => HideConnectors();

            Loaded += (_, _) => InitResizeThumbs();
        }

        public void SetShape(ShapeType shape)
        {
            ShapePresenter.Content = shape switch
            {
                ShapeType.Ellipse => new Ellipse { Stroke = Brushes.Black, Fill = Brushes.Transparent, StrokeThickness = 2, IsHitTestVisible = false },
                ShapeType.Rectangle => new Rectangle { Stroke = Brushes.Black, Fill = Brushes.Transparent, StrokeThickness = 2, IsHitTestVisible = false },
                ShapeType.Triangle => new Polygon
                {
                    Points = new PointCollection { new Point(50, 0), new Point(100, 100), new Point(0, 100) },
                    Stroke = Brushes.Black,
                    Fill = Brushes.Transparent,
                    StrokeThickness = 2,
                    IsHitTestVisible = false
                },
                _ => throw new NotImplementedException()
            };
        }

        public void SetPosition(Point pos)
        {
            Canvas.SetLeft(this, pos.X);
            Canvas.SetTop(this, pos.Y);
        }

        public void Select()
        {
            ResizeLeft.Visibility = ResizeRight.Visibility = ResizeTop.Visibility = ResizeBottom.Visibility = Visibility.Visible;
        }

        public void Deselect()
        {
            ResizeLeft.Visibility = ResizeRight.Visibility = ResizeTop.Visibility = ResizeBottom.Visibility = Visibility.Collapsed;
        }

        public UIElement Visual => this;

        private void InitResizeThumbs()
        {
            ResizeLeft.DragDelta += (s, e) => Resize(-e.HorizontalChange, 0, true, false);
            ResizeRight.DragDelta += (s, e) => Resize(e.HorizontalChange, 0, false, false);
            ResizeTop.DragDelta += (s, e) => Resize(0, -e.VerticalChange, false, true);
            ResizeBottom.DragDelta += (s, e) => Resize(0, e.VerticalChange, false, false);

            ConnectorTop.MouseLeftButtonDown += (s, e) => RaiseConnector("Top", e);
            ConnectorRight.MouseLeftButtonDown += (s, e) => RaiseConnector("Right", e);
            ConnectorBottom.MouseLeftButtonDown += (s, e) => RaiseConnector("Bottom", e);
            ConnectorLeft.MouseLeftButtonDown += (s, e) => RaiseConnector("Left", e);
        }

        private void Resize(double dx, double dy, bool left, bool top)
        {
            if (dx != 0)
            {
                double newWidth = Math.Max(this.ActualWidth + dx, this.MinWidth);
                this.Width = newWidth;
                if (left) Canvas.SetLeft(this, Canvas.GetLeft(this) - dx);
            }

            if (dy != 0)
            {
                double newHeight = Math.Max(this.ActualHeight + dy, this.MinHeight);
                this.Height = newHeight;
                if (top) Canvas.SetTop(this, Canvas.GetTop(this) - dy);
            }
        }

        private void ShowConnectors()
        {
            if (!EnableConnectors) return;
            ConnectorTop.Visibility = ConnectorRight.Visibility = ConnectorBottom.Visibility = ConnectorLeft.Visibility = Visibility.Visible;
        }

        private void HideConnectors()
        {
            if (!EnableConnectors) return;
            ConnectorTop.Visibility = ConnectorRight.Visibility = ConnectorBottom.Visibility = ConnectorLeft.Visibility = Visibility.Collapsed;
        }

        private void ForwardMouseMove(object sender, MouseEventArgs e)
        {
            (VisualTreeHelper.GetParent(this) as UIElement)?.RaiseEvent(new MouseEventArgs(e.MouseDevice, e.Timestamp) { RoutedEvent = MouseMoveEvent, Source = this });
        }

        private void ForwardMouseDown(object sender, MouseButtonEventArgs e)
        {
            (VisualTreeHelper.GetParent(this) as UIElement)?.RaiseEvent(new MouseButtonEventArgs(e.MouseDevice, e.Timestamp, e.ChangedButton) { RoutedEvent = MouseDownEvent, Source = this });
        }

        private void ForwardMouseUp(object sender, MouseButtonEventArgs e)
        {
            (VisualTreeHelper.GetParent(this) as UIElement)?.RaiseEvent(new MouseButtonEventArgs(e.MouseDevice, e.Timestamp, e.ChangedButton) { RoutedEvent = MouseUpEvent, Source = this });
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is Thumb) return;
            ShapeClicked?.Invoke(this, e);
            e.Handled = true;
        }

        private void RaiseConnector(string position, MouseButtonEventArgs e)
        {
            ConnectionPointClicked?.Invoke(this, position);
            e.Handled = true;
        }

        
    }
}
