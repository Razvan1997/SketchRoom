using SketchRoom.Models.Enums;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WhiteBoard.Core.Events;
using WhiteBoard.Core.Helpers;
using WhiteBoard.Core.Models;
using WhiteBoard.Core.Services;
using WhiteBoard.Core.Services.Interfaces;
using WhiteBoard.Core.Tools;
using WhiteBoard.Core.UndoRedo;
using WhiteBoardModule.Events;
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
        private RotateTransform _rotateTransform;
        private TranslateTransform _translateTransform;
        private ScaleTransform _scaleTransform;
        private TransformGroup _transformGroup;
        private bool _isRotating = false;
        private Point _rotateStart;
        private Point _resizeStart;
        private Viewbox? _imageContainer;
        private static readonly Dictionary<ShapeType, ShapeContextType> _contextMap = new()
        {
            { ShapeType.Rectangle, ShapeContextType.GenericShape },
            { ShapeType.Ellipse, ShapeContextType.GenericShape },
            { ShapeType.Triangle, ShapeContextType.GenericShape },
            { ShapeType.BorderTextBox, ShapeContextType.BorderTextBoxShape },
            { ShapeType.EntityShape, ShapeContextType.EntityShape },
            { ShapeType.TableShape, ShapeContextType.TableShape },
            { ShapeType.ShapeText, ShapeContextType.TextArea },
            { ShapeType.ConnectorDoubleShapeLabel, ShapeContextType.ConnectorDouble },
            { ShapeType.ConnectorShapeLabel, ShapeContextType.ConnectorSimpleLabel },
            { ShapeType.ConnectorDoubleLabelLeft, ShapeContextType.ConnectorDouble },
            { ShapeType.ConnectorLabelLeft, ShapeContextType.ConnectorSimpleLabel },
            { ShapeType.ConnectorDescriptionShape, ShapeContextType.DescriptionShapeConnector },
        };

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

        private ShapeType _shapeType;
        public GenericShapeControl()
        {
            InitializeComponent();
            _eventAggregator = ContainerLocator.Container.Resolve<IEventAggregator>();
            _eventAggregator.GetEvent<TableResizedEvent>().Subscribe(OnTableResize);
            _eventAggregator.GetEvent<SimpleLinesResizedEvent>().Subscribe(OnSimpleLinesResize);
            _contextMenuService = ContainerLocator.Container.Resolve<IContextMenuService>();

            this.MouseLeftButtonDown += OnMouseLeftButtonDown;
            this.MouseMove += ForwardMouseMove;
            this.MouseUp += ForwardMouseUp;
            this.MouseDown += ForwardMouseDown;

            this.MouseEnter += (s, e) =>
            {
                if (!RotateIcon.IsMouseOver)
                    ShowConnectors();
            };
            this.MouseLeave += (_, _) => HideConnectors();

            Loaded += (_, _) => InitResizeThumbs();

            _actionsManager = new ShapeActionsManager(this);
            this.ShapeActionRequested += _actionsManager.HandleAction;

            this.PreviewMouseRightButtonUp += (s, e) =>
            {
                if (this.ContextMenu != null)
                {
                    this.ContextMenu.PlacementTarget = this;
                    this.ContextMenu.Placement = PlacementMode.MousePoint;
                    this.ContextMenu.IsOpen = true;
                    e.Handled = true;
                }
            };

            RotateIcon.PreviewMouseLeftButtonDown += RotateIcon_PreviewMouseLeftButtonDown;
            RotateIcon.PreviewMouseLeftButtonUp += RotateIcon_PreviewMouseLeftButtonUp;
            RotateIcon.PreviewMouseMove += RotateIcon_PreviewMouseMove;

            _rotateTransform = new RotateTransform();
            _translateTransform = new TranslateTransform();
            _scaleTransform = new ScaleTransform();
            _transformGroup = new TransformGroup();

            _transformGroup.Children.Add(_scaleTransform);
            _transformGroup.Children.Add(_rotateTransform);
            _transformGroup.Children.Add(_translateTransform);

            this.RenderTransform = _transformGroup;
            this.RenderTransformOrigin = new Point(0.5, 0.5);
        }

        private void OnSimpleLinesResize(SimpleLinesResizeInfo info)
        {
            if (IsPreview || SourceTableId != info.SourceId)
                return;

            this.Width = info.NewSize.Width;
            this.Height = info.NewSize.Height;

            if (info.IsLastRowExpanded && info.VerticalOffset > 0)
            {
                // Se extinde ultimul rând → urcăm forma
                double top = Canvas.GetTop(this);
                Canvas.SetTop(this, top - info.VerticalOffset);
            }
        }

        private void RotateIcon_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var tabService = ContainerLocator.Container.Resolve<IWhiteBoardTabService>();
            var toolManager = tabService.GetCurrentToolManager();

            if (VisualTreeHelper.GetParent(this) is Canvas canvas &&
                toolManager.GetToolByName("RotateTool") is RotateTool rt)
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

            if (VisualTreeHelper.GetParent(this) is Canvas canvas &&
                ContainerLocator.Container.Resolve<IWhiteBoardTabService>()
                    .GetCurrentToolManager().ActiveTool is RotateTool rt)
            {
                Point pos = e.GetPosition(canvas);
                rt.OnMouseMove(pos);
            }
        }

        private void RotateIcon_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isRotating) return;

            if (VisualTreeHelper.GetParent(this) is Canvas canvas &&
                ContainerLocator.Container.Resolve<IWhiteBoardTabService>()
                    .GetCurrentToolManager().ActiveTool is RotateTool rt)
            {
                rt.OnMouseUp(e.GetPosition(canvas));
                ContainerLocator.Container.Resolve<IWhiteBoardTabService>()
                    .GetCurrentToolManager().SetActive("BpmnTool");

                _isRotating = false;
                e.Handled = true;
            }
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

        public void SetShape(ShapeType shape, double rotationAngle = 0)
        {
            _shapeType = shape;
            if (shape != ShapeType.Image)
            {
                _renderer = _rendererFactory.CreateRenderer(shape, withBindings: false);
            }
            double gridSize = 20;

            double rawWidth = 120;
            double rawHeight = 120;

            this.Width = SnapToGrid(rawWidth, gridSize);
            this.Height = SnapToGrid(rawHeight, gridSize);

            if (shape == ShapeType.Image)
            {
                var uri = ShapeMetadata.GetSvgUri(this);
                if (uri != null)
                {
                    var image = new Image
                    {
                        Source = new BitmapImage(uri),
                        Stretch = Stretch.Uniform,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        //Width = rawWidth,
                        //Height = rawHeight
                    };

                    var container = new Viewbox
                    {
                        Stretch = Stretch.Fill,
                        Child = image
                    };
                    _imageContainer = container;
                    ShapePresenter.Content = container;

                    
                }
                return;
            }

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
                if (_renderer is SimpleLinesRenderer simpleLines)
                {
                    SourceTableId = simpleLines.Id;
                }
            }

            if (_contextMap.TryGetValue(shape, out var contextType))
            {
                SetupContextMenu(contextType);
            }

            ApplyRotation(rotationAngle);
        }

        public void SetShapePreview(ShapeType shape)
        {
            IsPreview = true;
            var preview = _rendererFactory.CreateRenderPreview(shape);
            ShapePresenter.Content = preview;
            if (FindName("RotateIcon") is UIElement rotate)
            {
                var parent = VisualTreeHelper.GetParent(rotate) as Panel;
                parent?.Children.Remove(rotate);
            }
        }

        public void SetPosition(Point pos)
        {
            Canvas.SetLeft(this, pos.X);
            Canvas.SetTop(this, pos.Y);
        }

        public void ApplyRotation(double angle)
        {
            _rotateTransform.Angle = angle;
        }

        public void Select()
        {
            ResizeTopLeft.Visibility = ResizeTop.Visibility = ResizeTopRight.Visibility =
            ResizeRight.Visibility = ResizeBottomRight.Visibility = ResizeBottom.Visibility =
            ResizeBottomLeft.Visibility = ResizeLeft.Visibility = Visibility.Visible;

            RotateIcon.Visibility = Visibility.Visible;

            ResizeTopLeft.Cursor = Cursors.SizeNWSE;
            ResizeTop.Cursor = GetRotatedCursor("Top");
            ResizeTopRight.Cursor = Cursors.SizeNESW;
            ResizeRight.Cursor = GetRotatedCursor("Right");
            ResizeBottomRight.Cursor = Cursors.SizeNWSE;
            ResizeBottom.Cursor = GetRotatedCursor("Bottom");
            ResizeBottomLeft.Cursor = Cursors.SizeNESW;
            ResizeLeft.Cursor = GetRotatedCursor("Left");
        }

        public void Deselect()
        {
            ResizeTopLeft.Visibility = ResizeTop.Visibility = ResizeTopRight.Visibility =
            ResizeRight.Visibility = ResizeBottomRight.Visibility = ResizeBottom.Visibility =
            ResizeBottomLeft.Visibility = ResizeLeft.Visibility = Visibility.Collapsed;

            RotateIcon.Visibility = Visibility.Collapsed;
        }

        public UIElement Visual => this;

        public string Text { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public TextBox EditableText => throw new NotImplementedException();

        public IShapeRenderer? Renderer => _renderer;
        public ShapeType GetShapeType() => _shapeType;

        private void InitResizeThumbs()
        {
            ResizeTopLeft.DragDelta += (s, e) => Resize(-e.HorizontalChange, -e.VerticalChange, true, true);
            ResizeTop.DragDelta += (s, e) => Resize(0, -e.VerticalChange, false, true);
            ResizeTopRight.DragDelta += (s, e) => Resize(e.HorizontalChange, -e.VerticalChange, false, true);
            ResizeRight.DragDelta += (s, e) => Resize(e.HorizontalChange, 0, false, false);
            ResizeBottomRight.DragDelta += (s, e) => Resize(e.HorizontalChange, e.VerticalChange, false, false);
            ResizeBottom.DragDelta += (s, e) => Resize(0, e.VerticalChange, false, false);
            ResizeBottomLeft.DragDelta += (s, e) => Resize(-e.HorizontalChange, e.VerticalChange, true, false);
            ResizeLeft.DragDelta += (s, e) => Resize(-e.HorizontalChange, 0, true, false);

            // DragStarted for undo
            ResizeTopLeft.DragStarted += SaveOriginalSize;
            ResizeTop.DragStarted += SaveOriginalSize;
            ResizeTopRight.DragStarted += SaveOriginalSize;
            ResizeRight.DragStarted += SaveOriginalSize;
            ResizeBottomRight.DragStarted += SaveOriginalSize;
            ResizeBottom.DragStarted += SaveOriginalSize;
            ResizeBottomLeft.DragStarted += SaveOriginalSize;
            ResizeLeft.DragStarted += SaveOriginalSize;

            // DragCompleted for commit
            ResizeTopLeft.DragCompleted += CommitResize;
            ResizeTop.DragCompleted += CommitResize;
            ResizeTopRight.DragCompleted += CommitResize;
            ResizeRight.DragCompleted += CommitResize;
            ResizeBottomRight.DragCompleted += CommitResize;
            ResizeBottom.DragCompleted += CommitResize;
            ResizeBottomLeft.DragCompleted += CommitResize;
            ResizeLeft.DragCompleted += CommitResize;

            // Connector click handlers
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

        private void ResizeStart(object sender, DragStartedEventArgs e)
        {
            if (VisualTreeHelper.GetParent(this) is Canvas canvas)
                _resizeStart = Mouse.GetPosition(canvas);
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

        public void RequestChangeCurrentFontSize(double size)
        {
            ShapeActionRequested?.Invoke(this, new ShapeActionEventArgs(ShapeActionType.ChangeFontsize, size));
        }

        public void SetForeground(Brush brush)
        {
            _textBox.Foreground = brush;
        }

        private Cursor GetRotatedCursor(string position)
        {
            double angle = _rotateTransform.Angle % 360;
            if (angle < 0) angle += 360;

            // Normalizează în cadrane de 45°
            int sector = (int)((angle + 22.5) / 45) % 8;

            // Maparea poziției și sectorului la cursor
            return (position, sector) switch
            {
                ("Top", 0 or 4) => Cursors.SizeNS,
                ("Top", 1 or 5) => Cursors.SizeNESW,
                ("Top", 2 or 6) => Cursors.SizeWE,
                ("Top", 3 or 7) => Cursors.SizeNWSE,

                ("Bottom", 0 or 4) => Cursors.SizeNS,
                ("Bottom", 1 or 5) => Cursors.SizeNESW,
                ("Bottom", 2 or 6) => Cursors.SizeWE,
                ("Bottom", 3 or 7) => Cursors.SizeNWSE,

                ("Left", 0 or 4) => Cursors.SizeWE,
                ("Left", 1 or 5) => Cursors.SizeNWSE,
                ("Left", 2 or 6) => Cursors.SizeNS,
                ("Left", 3 or 7) => Cursors.SizeNESW,

                ("Right", 0 or 4) => Cursors.SizeWE,
                ("Right", 1 or 5) => Cursors.SizeNWSE,
                ("Right", 2 or 6) => Cursors.SizeNS,
                ("Right", 3 or 7) => Cursors.SizeNESW,

                ("TopLeft", 0 or 4) => Cursors.SizeNWSE,
                ("TopLeft", 1 or 5) => Cursors.SizeNS,
                ("TopLeft", 2 or 6) => Cursors.SizeNESW,
                ("TopLeft", 3 or 7) => Cursors.SizeWE,

                ("TopRight", 0 or 4) => Cursors.SizeNESW,
                ("TopRight", 1 or 5) => Cursors.SizeWE,
                ("TopRight", 2 or 6) => Cursors.SizeNWSE,
                ("TopRight", 3 or 7) => Cursors.SizeNS,

                ("BottomLeft", 0 or 4) => Cursors.SizeNESW,
                ("BottomLeft", 1 or 5) => Cursors.SizeWE,
                ("BottomLeft", 2 or 6) => Cursors.SizeNWSE,
                ("BottomLeft", 3 or 7) => Cursors.SizeNS,

                ("BottomRight", 0 or 4) => Cursors.SizeNWSE,
                ("BottomRight", 1 or 5) => Cursors.SizeNS,
                ("BottomRight", 2 or 6) => Cursors.SizeNESW,
                ("BottomRight", 3 or 7) => Cursors.SizeWE,

                _ => Cursors.Arrow
            };
        }

        public BPMNShapeModelWithPosition ExportData()
        {
            if (_shapeType == ShapeType.Image && this is FrameworkElement fe)
            {
                var uri = ShapeMetadata.GetSvgUri(fe);
                if (uri == null)
                    throw new InvalidOperationException("Image shape has no SvgUri metadata.");

                return new BPMNShapeModelWithPosition
                {
                    Id = Guid.TryParse(ShapeMetadata.GetShapeId(this), out var parsedId)
                    ? parsedId
                    : throw new InvalidOperationException("Missing or invalid shape ID."),
                    Type = ShapeType.Image,
                    SvgUri = uri,
                    Left = Canvas.GetLeft(fe),
                    Top = Canvas.GetTop(fe),
                    Width = this.Width,
                    Height = this.Height,
                    RotationAngle = (_transformGroup?.Children.OfType<RotateTransform>().FirstOrDefault()?.Angle) ?? 0
                };
            }

            if (Renderer == null)
                throw new InvalidOperationException("Renderer is missing.");

            var model = Renderer.ExportData(this);
            model.RotationAngle = (_transformGroup?.Children.OfType<RotateTransform>().FirstOrDefault()?.Angle) ?? 0;
            if (this.Content is Grid grid)
            {
                var textBox = grid.Children
                    .OfType<TextBox>()
                    .FirstOrDefault(tb => tb.Tag?.ToString() == "interactive");

                if (textBox != null)
                {
                    model.ExtraProperties ??= new Dictionary<string, string>();
                    model.ExtraProperties["Text"] = textBox.Text;
                    model.ExtraProperties["FontSize"] = textBox.FontSize.ToString();
                    model.ExtraProperties["Foreground"] = textBox.Foreground.ToString();
                    model.ExtraProperties["FontWeight"] = textBox.FontWeight.ToString();
                    model.ExtraProperties["TextWrapping"] = textBox.TextWrapping.ToString();
                }
            }
            model.ExtraProperties ??= new Dictionary<string, string>();
            model.ExtraProperties["RotationAngle"] = model.RotationAngle.ToString(CultureInfo.InvariantCulture);
            model.Id = Guid.TryParse(ShapeMetadata.GetShapeId(this), out var parsed)
                    ? parsed
                    : throw new InvalidOperationException("Missing or invalid shape ID.");
            return model;
        }
    }
}
