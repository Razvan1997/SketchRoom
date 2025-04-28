using SketchRoom.Models.Enums;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using WhiteBoard.Core.Events;
using WhiteBoard.Core.Services;
using WhiteBoard.Core.Services.Interfaces;
using WhiteBoard.Core.UndoRedo;
using WhiteBoardModule.Events;
using WhiteBoardModule.XAML.Interfaces;
using WhiteBoardModule.XAML.Managers;
using WhiteBoardModule.XAML.Shapes.Containers;
using WhiteBoardModule.XAML.Shapes.Entity;
using WhiteBoardModule.XAML.Shapes.Tables;
using WhiteBoardModule.XAML.StyleUpdater;

namespace WhiteBoardModule.XAML
{
    /// <summary>
    /// Interaction logic for EllipseShape.xaml
    /// </summary>
    public partial class GenericShapeControl : UserControl, IInteractiveShape, IUpdateStyle, IForegroundChangable, IShapeAddedXaml
    {
        private readonly IContextMenuService _contextMenuService;
        public event EventHandler<ConnectionPointEventArgs>? ConnectionPointClicked;
        public event MouseButtonEventHandler? ShapeClicked;
        public event EventHandler<ConnectionPointEventArgs>? ConnectionPointTargetClicked;
        public bool EnableConnectors { get; set; } = false;
        private readonly IShapeRendererFactory _rendererFactory = new ShapeRendererFactory();
        public IShapeRenderer? _renderer;
        private TextBox _textBox;
        private readonly IEventAggregator _eventAggregator;
        public bool IsPreview { get; set; }
        public Guid? SourceTableId { get; set; }

        private double _originalWidth;
        private double _originalHeight;
        private readonly ShapeActionsManager _actionsManager;
        public event EventHandler<ShapeActionEventArgs>? ShapeActionRequested;
        public GenericShapeControl()
        {
            InitializeComponent();
            _eventAggregator = ContainerLocator.Container.Resolve<IEventAggregator>();
            _eventAggregator.GetEvent<TableResizedEvent>().Subscribe(OnTableResize);
            _contextMenuService = ContainerLocator.Container.Resolve<IContextMenuService>();

            this.MouseLeftButtonDown += OnMouseLeftButtonDown;
            this.MouseMove += ForwardMouseMove;
            this.MouseUp += ForwardMouseUp;
            this.MouseDown += ForwardMouseDown;

            this.MouseEnter += (_, _) => ShowConnectors();
            this.MouseLeave += (_, _) => HideConnectors();

            Loaded += (_, _) => InitResizeThumbs();

            _actionsManager = new ShapeActionsManager(this);
            this.ShapeActionRequested += _actionsManager.HandleAction;
        }

        private void SetupContextMenu(ShapeContextType shapeType)
        {
            this.ContextMenu = _contextMenuService.CreateContextMenu(shapeType, this);
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
            if (shape == ShapeType.Rectangle || shape == ShapeType.Ellipse || shape == ShapeType.Triangle)
            {
                SetupContextMenu(ShapeContextType.GenericShape);
            }
            if (shape == ShapeType.BorderTextBox)
            {
                SetupContextMenu(ShapeContextType.BorderTextBoxShape);
            }
            if (shape == ShapeType.EntityShape)
            {
                SetupContextMenu(ShapeContextType.EntityShape);
            }

            _renderer = _rendererFactory.CreateRenderer(shape, withBindings: false);

            double gridSize = 20;

            double rawWidth = 120;
            double rawHeight = 120;

            this.Width = SnapToGrid(rawWidth, gridSize);
            this.Height = SnapToGrid(rawHeight, gridSize);

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

            if (_renderer is ListContainerRenderer listRender)
            {
                listRender.ConnectionPointClicked += (s, args) =>
                {
                    ConnectionPointClicked?.Invoke(this, args);
                };

                listRender.ConnectionPointTargetClicked += (s, args) =>
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

        public IShapeRenderer? Renderer => _renderer;

        private void InitResizeThumbs()
        {
            ResizeLeft.DragDelta += (s, e) => Resize(-e.HorizontalChange, 0, true, false);
            ResizeRight.DragDelta += (s, e) => Resize(e.HorizontalChange, 0, false, false);
            ResizeTop.DragDelta += (s, e) => Resize(0, -e.VerticalChange, false, true);
            ResizeBottom.DragDelta += (s, e) => Resize(0, e.VerticalChange, false, false);

            ResizeLeft.DragStarted += SaveOriginalSize;
            ResizeRight.DragStarted += SaveOriginalSize;
            ResizeTop.DragStarted += SaveOriginalSize;
            ResizeBottom.DragStarted += SaveOriginalSize;

            ResizeLeft.DragCompleted += CommitResize;
            ResizeRight.DragCompleted += CommitResize;
            ResizeTop.DragCompleted += CommitResize;
            ResizeBottom.DragCompleted += CommitResize;


            ConnectorTop.MouseLeftButtonDown += (s, e) => RaiseConnector("Top", e);
            ConnectorRight.MouseLeftButtonDown += (s, e) => RaiseConnector("Right", e);
            ConnectorBottom.MouseLeftButtonDown += (s, e) => RaiseConnector("Bottom", e);
            ConnectorLeft.MouseLeftButtonDown += (s, e) => RaiseConnector("Left", e);
        }

        private void SaveOriginalSize(object sender, DragStartedEventArgs e)
        {
            _originalWidth = this.Width;
            _originalHeight = this.Height;
        }

        private void CommitResize(object sender, DragCompletedEventArgs e)
        {
            double newWidth = this.Width;
            double newHeight = this.Height;

            if (_originalWidth != newWidth || _originalHeight != newHeight)
            {
                var command = new ResizeShapeCommand(this, newWidth, newHeight);
                var undoService = ContainerLocator.Container.Resolve<UndoRedoService>();
                undoService.ExecuteCommand(command);
            }
        }

        private void Resize(double dx, double dy, bool left, bool top)
        {
            if (dx != 0)
            {
                double newWidth = Math.Max(this.Width + dx, this.MinWidth);
                double snappedWidth = SnapToGrid(newWidth, 20);
                double widthChange = snappedWidth - this.Width;
                this.Width = snappedWidth;

                if (left)
                    Canvas.SetLeft(this, Canvas.GetLeft(this) - widthChange);
            }

            if (dy != 0)
            {
                double newHeight = Math.Max(this.Height + dy, this.MinHeight);
                double snappedHeight = SnapToGrid(newHeight, 20);
                double heightChange = snappedHeight - this.Height;
                this.Height = snappedHeight;

                if (top)
                    Canvas.SetTop(this, Canvas.GetTop(this) - heightChange);
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

        private double SnapToGrid(double value, double gridSize)
        {
            return Math.Round(value / gridSize) * gridSize;
        }

        public void AddTextToCenter()
        {
            var textBox = new TextBox
            {
                Background = Brushes.Transparent,
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                BorderBrush = Brushes.Transparent,
                Padding = new Thickness(4),
                Tag = "interactive",
                AcceptsReturn = true,
                AcceptsTab = true,
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                MinWidth = 80,
                MaxWidth = 300,
                FontSize = 14,
                Text = "Enter text..."
            };

            var selectionService = ContainerLocator.Container.Resolve<IShapeSelectionService>();

            textBox.PreviewMouseLeftButtonDown += (s, e) =>
            {
                if (!textBox.IsKeyboardFocusWithin)
                {
                    textBox.Focus();
                }

                selectionService.Select(ShapePart.Text, textBox);

                e.Handled = true;
            };

            (this.Content as Grid)?.Children.Add(textBox);
        }

        public void RequestChangeBackgroundColor(Brush brush)
        {
            ShapeActionRequested?.Invoke(this, new ShapeActionEventArgs(ShapeActionType.ChangeBackgroundColor, brush));
        }

        public void RequestChangeStrokeColor(Brush brush)
        {
            ShapeActionRequested?.Invoke(this, new ShapeActionEventArgs(ShapeActionType.ChangeStrokeColor, brush));
        }

        public void RequestChangeForegroundColor(Brush brush)
        {
            ShapeActionRequested?.Invoke(this, new ShapeActionEventArgs(ShapeActionType.ChangeForegroundColor, brush));
        }

        public void SetForeground(Brush brush)
        {
            _textBox.Foreground = brush;
        }
    }
}
