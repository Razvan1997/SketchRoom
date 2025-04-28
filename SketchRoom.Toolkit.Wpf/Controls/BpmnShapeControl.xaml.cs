using SharpVectors.Converters;
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
using WhiteBoard.Core.Events;
using WhiteBoard.Core.Services.Interfaces;
using WhiteBoard.Core.Tools;

namespace SketchRoom.Toolkit.Wpf
{
    /// <summary>
    /// Interaction logic for BpmnShapeControl.xaml
    /// </summary>
    public partial class BpmnShapeControl : UserControl, IInteractiveShape
    {
        public RotateTransform RotateTransform { get; }
        public TranslateTransform TranslateTransform { get; }
        public ScaleTransform ScaleTransform { get; }
        public TransformGroup TransformGroup { get; }

        private bool _isRotating = false;
        private Point _rotateStart;
        public event MouseButtonEventHandler? ShapeClicked;
        public event EventHandler? ConnectionRequested;
        public event EventHandler<ConnectionPointEventArgs>? ConnectionPointClicked;
        public event EventHandler<ConnectionPointEventArgs>? ConnectionPointTargetClicked;
        public event EventHandler<ShapeActionEventArgs> ShapeActionRequested;

        public bool EnableConnectors { get; set; } = false;
        private readonly ISnapService _snapService;
        public BpmnShapeControl(Uri svgUri)
        {
            InitializeComponent();

            RotateTransform = new RotateTransform();
            TranslateTransform = new TranslateTransform();
            ScaleTransform = new ScaleTransform();
            TransformGroup = new TransformGroup();

            TransformGroup.Children.Add(ScaleTransform);
            TransformGroup.Children.Add(RotateTransform);
            TransformGroup.Children.Add(TranslateTransform);

            this.RenderTransform = TransformGroup;
            this.RenderTransformOrigin = new Point(0.5, 0.5);

            _snapService = ContainerLocator.Container.Resolve<ISnapService>();
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

            this.MouseEnter += (s, e) =>
            {
                if (!RotateIcon.IsMouseOver)
                    ShowConnectors();
            };

            this.MouseLeave += (_, _) => HideConnectors();

            RotateIcon.PreviewMouseLeftButtonDown += RotateIcon_PreviewMouseLeftButtonDown;
            RotateIcon.PreviewMouseLeftButtonUp += RotateIcon_PreviewMouseLeftButtonUp;
            RotateIcon.PreviewMouseMove += RotateIcon_PreviewMouseMove;

            ConnectorTop.MouseLeftButtonDown += Connector_MouseLeftButtonDown;
            ConnectorRight.MouseLeftButtonDown += Connector_MouseLeftButtonDown;
            ConnectorBottom.MouseLeftButtonDown += Connector_MouseLeftButtonDown;
            ConnectorLeft.MouseLeftButtonDown += Connector_MouseLeftButtonDown;

            //this.Tag = "Snappable";
        }

        private void ResizeLeft_DragDelta(object sender, DragDeltaEventArgs e)
        {
            double newWidth = Math.Max(this.ActualWidth - e.HorizontalChange, this.MinWidth);
            double snappedWidth = SnapToGrid(newWidth, 20);
            double deltaX = this.ActualWidth - snappedWidth;

            this.Width = snappedWidth;
            Canvas.SetLeft(this, Canvas.GetLeft(this) + deltaX);
        }

        private void ResizeRight_DragDelta(object sender, DragDeltaEventArgs e)
        {
            double newWidth = Math.Max(this.ActualWidth + e.HorizontalChange, this.MinWidth);
            this.Width = SnapToGrid(newWidth, 20);
        }

        private void ResizeTop_DragDelta(object sender, DragDeltaEventArgs e)
        {
            double newHeight = Math.Max(this.ActualHeight - e.VerticalChange, this.MinHeight);
            double snappedHeight = SnapToGrid(newHeight, 20);
            double deltaY = this.ActualHeight - snappedHeight;

            this.Height = snappedHeight;
            Canvas.SetTop(this, Canvas.GetTop(this) + deltaY);
        }

        private void ResizeBottom_DragDelta(object sender, DragDeltaEventArgs e)
        {
            double newHeight = Math.Max(this.ActualHeight + e.VerticalChange, this.MinHeight);
            this.Height = SnapToGrid(newHeight, 20);
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
            if (!EnableConnectors) return;

            ResizeLeft.Visibility = Visibility.Visible;
            ResizeRight.Visibility = Visibility.Visible;
            ResizeTop.Visibility = Visibility.Visible;
            ResizeBottom.Visibility = Visibility.Visible;
            RotateIcon.Visibility = Visibility.Visible;
        }

        public void Deselect()
        {
            if (!EnableConnectors) return;

            ResizeLeft.Visibility = Visibility.Collapsed;
            ResizeRight.Visibility = Visibility.Collapsed;
            ResizeTop.Visibility = Visibility.Collapsed;
            ResizeBottom.Visibility = Visibility.Collapsed;
            RotateIcon.Visibility = Visibility.Collapsed;
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
            if (sender is UIElement element)
            {
                string direction = element == ConnectorTop ? "Top" :
                                   element == ConnectorRight ? "Right" :
                                   element == ConnectorBottom ? "Bottom" :
                                   element == ConnectorLeft ? "Left" : "";

                if (!string.IsNullOrEmpty(direction))
                {
                    ConnectionPointClicked?.Invoke(this, new ConnectionPointEventArgs(direction, element, e));
                    e.Handled = true;
                }
            }
        }

        private void RotateIcon_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var tabService = ContainerLocator.Container.Resolve<IWhiteBoardTabService>();
            var toolManager = tabService.GetCurrentToolManager();

            var canvas = VisualTreeHelper.GetParent(this) as Canvas;
            if (canvas != null && toolManager.GetToolByName("RotateTool") is RotateTool rt)
            {
                _isRotating = true;
                _rotateStart = e.GetPosition(canvas);
                rt.StartRotation(this, _rotateStart);
                toolManager.SetActive("RotateTool");
                e.Handled = true;
            }
        }

        private void RotateIcon_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isRotating) return;

            var canvas = VisualTreeHelper.GetParent(this) as Canvas;
            var tabService = ContainerLocator.Container.Resolve<IWhiteBoardTabService>();
            var toolManager = tabService.GetCurrentToolManager();
            if (canvas != null && toolManager.ActiveTool is RotateTool rt)
            {
                Point pos = e.GetPosition(canvas);
                rt.OnMouseMove(pos);
            }
        }

        private void RotateIcon_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var canvas = VisualTreeHelper.GetParent(this) as Canvas;
            var tabService = ContainerLocator.Container.Resolve<IWhiteBoardTabService>();
            var toolManager = tabService.GetCurrentToolManager();
            if (canvas != null && _isRotating && toolManager.ActiveTool is RotateTool rt)
            {
                rt.OnMouseUp(e.GetPosition(canvas));
                toolManager.SetActive("BpmnTool");
                _isRotating = false;
                e.Handled = true;
            }
        }

        public void SetShape(ShapeType shape)
        {
            //throw new NotImplementedException();
        }

        private double SnapToGrid(double value, double gridSize)
        {
            return Math.Round(value / gridSize) * gridSize;
        }

        public void AddTextToCenter()
        {
            //throw new NotImplementedException();
        }

        public void RequestChangeBackgroundColor(Brush brush)
        {
            throw new NotImplementedException();
        }

        public void RequestChangeStrokeColor(Brush brush)
        {
            throw new NotImplementedException();
        }

        public void RequestChangeForegroundColor(Brush brush)
        {
            throw new NotImplementedException();
        }
    }
}
