using SharpVectors.Converters;
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

namespace SketchRoom.Toolkit.Wpf
{
    /// <summary>
    /// Interaction logic for BpmnShapeControl.xaml
    /// </summary>
    public partial class BpmnShapeControl : UserControl, IInteractiveShape
    {
        public event MouseButtonEventHandler? ShapeClicked;
        public event EventHandler? ConnectionRequested;
        public event EventHandler<string>? ConnectionPointClicked;
        public BpmnShapeControl(Uri svgUri)
        {
            InitializeComponent();
            this.Cursor = Cursors.Hand;
            SvgViewbox.Source = svgUri;
            this.Width = 100;
            this.Height = 100;

            // Resize logic

            // Select on click
            this.MouseLeftButtonDown += OnMouseLeftButtonDown;
            SvgViewbox.MouseLeftButtonDown += OnMouseLeftButtonDown;

            ResizeLeft.DragDelta += ResizeLeft_DragDelta;
            ResizeRight.DragDelta += ResizeRight_DragDelta;
            ResizeTop.DragDelta += ResizeTop_DragDelta;
            ResizeBottom.DragDelta += ResizeBottom_DragDelta;

            this.MouseEnter += (_, _) => ShowConnectors();
            this.MouseLeave += (_, _) => HideConnectors();
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

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Important: Dacă apăsăm pe Thumb, nu selectăm
            if (e.OriginalSource is Thumb)
                return;

            //Select();
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

        public UIElement Visual => this;

        private void ConnectionHandle_Click(object sender, RoutedEventArgs e)
        {
            ConnectionRequested?.Invoke(this, EventArgs.Empty);
            e.Handled = true;
        }

        private void ShowConnectors()
        {
            ConnectorTop.Visibility = Visibility.Visible;
            ConnectorRight.Visibility = Visibility.Visible;
            ConnectorBottom.Visibility = Visibility.Visible;
            ConnectorLeft.Visibility = Visibility.Visible;
        }

        private void HideConnectors()
        {
            ConnectorTop.Visibility = Visibility.Collapsed;
            ConnectorRight.Visibility = Visibility.Collapsed;
            ConnectorBottom.Visibility = Visibility.Collapsed;
            ConnectorLeft.Visibility = Visibility.Collapsed;
        }

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
