using SharpVectors.Renderers;
using SketchRoom.Models.Enums;
using SketchRoom.Toolkit.Wpf.Converters;
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
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WhiteBoard.Core.Events;
using WhiteBoard.Core.Services.Interfaces;
using WhiteBoardModule.Events;
using WhiteBoardModule.ViewModels;
using WhiteBoardModule.XAML.Shapes.Entity;
using WhiteBoardModule.XAML.Shapes.Tables;
using WhiteBoardModule.XAML.StyleUpdater;

namespace WhiteBoardModule.XAML
{
    /// <summary>
    /// Interaction logic for EllipseShape.xaml
    /// </summary>
    public partial class GenericShapeControl : UserControl, IInteractiveShape, IUpdateStyle
    {
        public event EventHandler<ConnectionPointEventArgs>? ConnectionPointClicked;
        public event MouseButtonEventHandler? ShapeClicked;
        public event EventHandler<ConnectionPointEventArgs>? ConnectionPointTargetClicked;
        public bool EnableConnectors { get; set; } = false;
        private readonly IShapeRendererFactory _rendererFactory = new ShapeRendererFactory();
        private IShapeRenderer? _renderer;
        private readonly IEventAggregator _eventAggregator;
        public bool IsPreview { get; set; }
        public Guid? SourceTableId { get; set; }
        public GenericShapeControl()
        {
            InitializeComponent();

            _eventAggregator = ContainerLocator.Container.Resolve<IEventAggregator>();
            _eventAggregator.GetEvent<TableResizedEvent>().Subscribe(OnTableResize);

            this.MouseLeftButtonDown += OnMouseLeftButtonDown;
            this.MouseMove += ForwardMouseMove;
            this.MouseUp += ForwardMouseUp;
            this.MouseDown += ForwardMouseDown;

            this.MouseEnter += (_, _) => ShowConnectors();
            this.MouseLeave += (_, _) => HideConnectors();

            Loaded += (_, _) => InitResizeThumbs();
        }

        private void OnTableResize(TableResizeInfo info)
        {
            if (IsPreview) return;

            if (SourceTableId != info.SourceId)
                return; // evenimentul nu e pentru mine

            this.Width = info.NewSize.Width;
            this.Height = info.NewSize.Height;
        }

        public void SetInitialSize(double width, double height)
        {
            this.Width = width;
            this.Height = height;
            _renderer?.SetInitialSize(width, height);
        }

        public void SetShape(ShapeType shape)
        {
            _renderer = _rendererFactory.CreateRenderer(shape, withBindings: false);

            this.Width = 100;
            this.Height = 100;

            if (_renderer is EntityShapeRenderer entityRenderer)
            {
                entityRenderer.ConnectionPointClicked += (s, args) =>
                {
                    ConnectionPointClicked?.Invoke(this, args);
                };

                entityRenderer.ConnectionPointTargetClicked += (s, args) =>
                {
                    ConnectionPointTargetClicked?.Invoke(this, args);
                };
            }

            if (_renderer is TableShapeRenderer tableRenderer)
            {
                var rendered = tableRenderer.Render();

                if (rendered is EditableTableControl table)
                {
                    SourceTableId = table.Id;
                    ShapePresenter.Content = table;
                }
            }
            else
            {
                ShapePresenter.Content = _renderer.Render();
            }
        }

        public void SetShapePreview(ShapeType shape)
        {
            IsPreview = true;
            var preview = _rendererFactory.CreateRenderPreview(shape);
            ShapePresenter.Content = preview;
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

        public string Text { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public TextBox EditableText => throw new NotImplementedException();

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
            if (!EnableConnectors) return;

            if (e.OriginalSource is Rectangle rect && rect.Tag?.ToString() == "Connector")
            {
                ConnectionPointClicked?.Invoke(this, new ConnectionPointEventArgs(position, rect, e));
                e.Handled = true;
            }
        }

        public void UpdateStyle(FontWeight fontWeight, double fontSize, Brush foreground, bool applyBackground)
        {
            ShapeStyleUpdater.Apply(ShapePresenter.Content, fontWeight, fontSize, foreground, applyBackground);
        }

        public void RaiseClick(MouseButtonEventArgs e)
        {
            ShapeClicked?.Invoke(this, e);
        }
    }
}
