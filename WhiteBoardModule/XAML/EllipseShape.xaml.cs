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
    public partial class EllipseShape : UserControl, IInteractiveShape
    {
        public event EventHandler<string>? ConnectionPointClicked;
        public bool EnableConnectors { get; set; } = false;
        public EllipseShape()
        {
            InitializeComponent();

            this.MouseLeftButtonDown += OnMouseLeftButtonDown;

            // ✅ Propagă MouseMove pentru drag&drop
            this.MouseMove += ForwardMouseMove;
            this.MouseUp += ForwardMouseUp;
            this.MouseDown += ForwardMouseDown;

            ResizeLeft.DragDelta += ResizeLeft_DragDelta;
            ResizeRight.DragDelta += ResizeRight_DragDelta;
            ResizeTop.DragDelta += ResizeTop_DragDelta;
            ResizeBottom.DragDelta += ResizeBottom_DragDelta;

            this.MouseEnter += (_, _) => ShowConnectors();
            this.MouseLeave += (_, _) => HideConnectors();
        }

        public event MouseButtonEventHandler? ShapeClicked;


        private void ForwardMouseMove(object sender, MouseEventArgs e)
        {
            var parent = VisualTreeHelper.GetParent(this) as UIElement;
            if (parent != null)
            {
                var newEvent = new MouseEventArgs(e.MouseDevice, e.Timestamp)
                {
                    RoutedEvent = UIElement.MouseMoveEvent,
                    Source = this
                };
                parent.RaiseEvent(newEvent);
            }
        }

        private void ForwardMouseDown(object sender, MouseButtonEventArgs e)
        {
            var parent = VisualTreeHelper.GetParent(this) as UIElement;
            if (parent != null)
            {
                var newEvent = new MouseButtonEventArgs(e.MouseDevice, e.Timestamp, e.ChangedButton)
                {
                    RoutedEvent = UIElement.MouseDownEvent,
                    Source = this
                };
                parent.RaiseEvent(newEvent);
            }
        }

        private void ForwardMouseUp(object sender, MouseButtonEventArgs e)
        {
            var parent = VisualTreeHelper.GetParent(this) as UIElement;
            if (parent != null)
            {
                var newEvent = new MouseButtonEventArgs(e.MouseDevice, e.Timestamp, e.ChangedButton)
                {
                    RoutedEvent = UIElement.MouseUpEvent,
                    Source = this
                };
                parent.RaiseEvent(newEvent);
            }
        }
        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is Thumb) return;

            ShapeClicked?.Invoke(this, e);
            e.Handled = true;
        }

        public void Select()
        {
            ResizeLeft.Visibility = Visibility.Visible;
            ResizeRight.Visibility = Visibility.Visible;
            ResizeTop.Visibility = Visibility.Visible;
            ResizeBottom.Visibility = Visibility.Visible;
        }

        public void Deselect()
        {
            ResizeLeft.Visibility = Visibility.Collapsed;
            ResizeRight.Visibility = Visibility.Collapsed;
            ResizeTop.Visibility = Visibility.Collapsed;
            ResizeBottom.Visibility = Visibility.Collapsed;
        }

        public void SetPosition(Point position)
        {
            Canvas.SetLeft(this, position.X);
            Canvas.SetTop(this, position.Y);
        }

        private void ResizeLeft_DragDelta(object sender, DragDeltaEventArgs e)
        {
            double newWidth = Math.Max(this.ActualWidth - e.HorizontalChange, this.MinWidth);
            double deltaX = this.ActualWidth - newWidth;

            this.Width = newWidth;
            Canvas.SetLeft(this, Canvas.GetLeft(this) + deltaX);
        }

        private void ResizeRight_DragDelta(object sender, DragDeltaEventArgs e)
        {
            double newWidth = Math.Max(this.ActualWidth + e.HorizontalChange, this.MinWidth);
            this.Width = newWidth;
        }

        private void ResizeTop_DragDelta(object sender, DragDeltaEventArgs e)
        {
            double newHeight = Math.Max(this.ActualHeight - e.VerticalChange, this.MinHeight);
            double deltaY = this.ActualHeight - newHeight;

            this.Height = newHeight;
            Canvas.SetTop(this, Canvas.GetTop(this) + deltaY);
        }

        private void ResizeBottom_DragDelta(object sender, DragDeltaEventArgs e)
        {
            double newHeight = Math.Max(this.ActualHeight + e.VerticalChange, this.MinHeight);
            this.Height = newHeight;
        }

        private void ShowConnectors()
        {
            if (!EnableConnectors) return;

            ConnectorTop.Visibility = Visibility.Visible;
            ConnectorRight.Visibility = Visibility.Visible;
            ConnectorBottom.Visibility = Visibility.Visible;
            ConnectorLeft.Visibility = Visibility.Visible;
        }

        private void HideConnectors()
        {
            if (!EnableConnectors) return;

            ConnectorTop.Visibility = Visibility.Collapsed;
            ConnectorRight.Visibility = Visibility.Collapsed;
            ConnectorBottom.Visibility = Visibility.Collapsed;
            ConnectorLeft.Visibility = Visibility.Collapsed;
        }

        public UIElement Visual => this;

        private void Connector_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender == ConnectorTop) ConnectionPointClicked?.Invoke(this, "Top");
            else if (sender == ConnectorRight) ConnectionPointClicked?.Invoke(this, "Right");
            else if (sender == ConnectorBottom) ConnectionPointClicked?.Invoke(this, "Bottom");
            else if (sender == ConnectorLeft) ConnectionPointClicked?.Invoke(this, "Left");

            e.Handled = true;
        }
    }
}
