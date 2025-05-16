using DrawingStateService;
using DrawingStateService.States;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WhiteBoard.Core;
using WhiteBoard.Core.Tools;
using WhiteBoard.Core.Services.Interfaces;
using WhiteBoard.Core.Colaboration.Interfaces;
using SharpVectors.Converters;
using SketchRoom.Models.Shapes;
using WhiteBoard.Core.Models;
using WhiteBoard.Core.Factory.Interfaces;
using System.Windows.Controls.Primitives;
using WhiteBoard.Core.Services;
using System.Windows.Documents;
using SketchRoom.Models.Enums;
using System.Windows.Media.Media3D;
using System.IO;

namespace SketchRoom.Toolkit.Wpf.Controls
{
    public partial class WhiteBoardControl : UserControl, IWhiteBoardAdapter
    {
        public Canvas DrawingCanvasPublic => DrawingCanvas;
        private readonly List<ITextInteractiveShape> _textShapes = new();
        private BpmnConnectorTool? _connectorTool;
        public List<BPMNConnection> _connections = new();
        private readonly Dictionary<FrameworkElement, BPMNNode> _nodes = new();
        private WhiteBoardHost _host;
        private IZoomPanService _zoomPanService;
        private IToolManager _toolManager;
        private ISnapService? _snapService;
        private SelectedToolService _selectedToolService;
        private IDrawingPreferencesService? _drawingPreferencesService;
        private IShapeSelectionService _shapeSelectionService;
        public readonly IDropService _dropService;
        private Cursor _previousCursor;
        private Point _lastPanPoint;
        private bool _isPanning;
        private ToolInterceptorService _toolInterceptorService;
        public event Action<List<Point>>? LineDrawn;
        public event Action<Point>? LivePointDrawn;
        public event Action<Point>? MouseMoved;
        private readonly IContextMenuService _contextMenuService;
        private readonly IBpmnShapeFactory _factory;
        private readonly WhiteBoard.Core.Services.Interfaces.ISelectionService _selectionService;
        private bool _isRightMouseHeld = false;
        private Point _rightClickStartPoint;
        private bool _isRightMouseMoving;
        private Point? _lastRightClickCanvasPosition = null;
        private readonly IClipboardService _clipboardService;
        private readonly IDrawingService _drawingService;
        private IDrawingTool? _previousTool;
        public BpmnConnectorTool? BpmnConnectorToolPublic => _connectorTool;
        public WhiteBoardControl(IDrawingService drawingService, IDrawingPreferencesService drawingPreferences)
        {
            InitializeComponent();
            _factory = ContainerLocator.Container.Resolve<IBpmnShapeFactory>();
            var undoRedoService = ContainerLocator.Container.Resolve<UndoRedoService>();
            var tabService = ContainerLocator.Container.Resolve<IWhiteBoardTabService>();
            var zOrderService = ContainerLocator.Container.Resolve<IZOrderService>();
            var shapeRenderFactory = ContainerLocator.Container.Resolve<IShapeRendererFactory>();

            _toolManager = tabService.GetCurrentToolManager();
            _drawingService = drawingService;
            _drawingPreferencesService = drawingPreferences;


            _drawingService.SetCanvas(DrawingCanvas);
            var canvasRenderer = ContainerLocator.Container.Resolve<ICanvasRenderer>();
            _zoomPanService = ContainerLocator.Container.Resolve<IZoomPanService>();
            _snapService = ContainerLocator.Container.Resolve<ISnapService>();



            _selectedToolService = ContainerLocator.Container.Resolve<SelectedToolService>();
            _toolInterceptorService = new ToolInterceptorService(_toolManager, _selectedToolService);
            _host = new WhiteBoardHost(DrawingCanvas, _toolManager, drawingService, canvasRenderer);
            _contextMenuService = ContainerLocator.Container.Resolve<IContextMenuService>();
            _shapeSelectionService = ContainerLocator.Container.Resolve<IShapeSelectionService>();
            var freeDrawTool = new FreeDrawTool(drawingService, DrawingCanvas, _drawingPreferencesService);
            freeDrawTool.StrokeCompleted += points => LineDrawn?.Invoke(points);
            freeDrawTool.PointDrawn += point => LivePointDrawn?.Invoke(point);
            freeDrawTool.PointerMoved += point => MouseMoved?.Invoke(point);

            _connectorTool = new BpmnConnectorTool(DrawingCanvas, _connections, _nodes, this, _toolManager, _snapService, undoRedoService, _drawingPreferencesService);
            _selectionService = new SelectionService(_connectorTool);

            var eraserTool = new EraserTool(drawingService, DrawingCanvas);
            var bpmnTool = new BpmnTool(DrawingCanvas, _snapService, SnapGridCanvas, _toolManager, undoRedoService, _selectionService);

            var connectorCurvedTool = new BpmnConnectorCurvedTool(DrawingCanvas, _connections, _nodes, this, _toolManager, _snapService, undoRedoService, _drawingPreferencesService);

            var rotateTool = new RotateTool(DrawingCanvas);
            _selectionService.SelectionChanged += OnSelectionChanged;
            var selectionTool = new SelectionTool(DrawingCanvas, _selectionService, _toolManager);

            var panTool = new PanTool(_zoomPanService, ZoomTranslate);
            var textTool = new TextTool(DrawingCanvas, _toolManager, _factory, _snapService, _drawingPreferencesService, _selectedToolService, _textShapes);

            var removeStrokeTool = new RemoveStrokeTool(drawingService, DrawingCanvas, _drawingPreferencesService);

            _toolManager.RegisterTool(freeDrawTool);
            _toolManager.RegisterTool(eraserTool);
            _toolManager.RegisterTool(bpmnTool);
            _toolManager.RegisterTool(_connectorTool);
            _toolManager.RegisterTool(rotateTool);
            _toolManager.RegisterTool(selectionTool);
            _toolManager.RegisterTool(panTool);
            _toolManager.RegisterTool(connectorCurvedTool);
            _toolManager.RegisterTool(textTool);
            _toolManager.RegisterTool(removeStrokeTool);

            var selecteToolService = ContainerLocator.Container.Resolve<SelectedToolService>();
            _dropService = new DropService(DrawingCanvas, _factory, _toolManager, _connectorTool!, connectorCurvedTool, _nodes, selecteToolService, undoRedoService,
                _drawingPreferencesService, zOrderService, shapeRenderFactory);

            this.KeyDown += WhiteBoardControl_KeyDown;
            this.Focusable = true;
            this.Focus();

            DrawingCanvas.ContextMenu = _contextMenuService.CreateContextMenu(ShapeContextType.WhiteBoardArea, this);
            DrawingCanvas.ContextMenuOpening += (s, e) =>
            {
                // Blochează deschiderea automată
                e.Handled = true;
            };
            _clipboardService = new ClipboardService(_dropService);
        }

        private void OnSelectionChanged(object? sender, EventArgs e)
        {
            Dispatcher.InvokeAsync(() =>
            {
                FocusManager.SetFocusedElement(this, this);
                Keyboard.Focus(this);
            }, System.Windows.Threading.DispatcherPriority.Input);
        }

        private void WhiteBoardControl_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                _connectorTool?.DeleteSelectedConnections();
                foreach (var el in _selectionService.SelectedElements.ToList())
                {
                    DrawingCanvas.Children.Remove(el);
                }
                _selectionService.ClearSelection(DrawingCanvas);

                if (_toolManager.ActiveTool is BpmnTool bpmnTool && bpmnTool.SelectedShape is FrameworkElement fe)
                {
                    DrawingCanvas.Children.Remove(fe);
                    bpmnTool.DeselectCurrent();
                }

                e.Handled = true;
            }

            // CTRL + C
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.C)
            {
                IInteractiveShape? selectedShape = null;

                // Dacă tool-ul activ este BpmnTool și are o formă selectată
                if (_toolManager.ActiveTool is BpmnTool bpmnTool && bpmnTool.SelectedShape is IInteractiveShape bpmnSelected)
                {
                    selectedShape = bpmnSelected;
                }
                else
                {
                    // Altfel, folosim selecția generală
                    selectedShape = _selectionService.SelectedElements.OfType<IInteractiveShape>().FirstOrDefault();
                }

                if (selectedShape != null)
                {
                    _clipboardService.Copy(selectedShape);
                }

                e.Handled = true;
                return;
            }

            // CTRL + V
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.V)
            {
                var mousePosition = Mouse.GetPosition(DrawingCanvas);
                var newShape = _clipboardService.Paste(mousePosition);

                if (newShape != null && newShape.Visual is FrameworkElement fe)
                {
                    _dropService.PlaceElementOnCanvas(fe, mousePosition);
                    _dropService.RegisterNodeWhenReady(fe);
                    _dropService.SetupConnectorButton(fe);
                }

                e.Handled = true;
                return;
            }
        }
        private void DeselectAllTexts()
        {
            foreach (var txt in _textShapes)
                txt.Deselect();
        }
        private void Canvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                _zoomPanService.Zoom(ZoomScale, ZoomTranslate, e.GetPosition(DrawingCanvas), e.Delta);
                e.Handled = true;
            }
        }

        private void OnCanvasLeftClickStart(object sender, MouseButtonEventArgs e)
        {
            var logicalPos = GetLogicalPosition(e);
            _toolInterceptorService.InterceptToolSwitch(e);

            if (Keyboard.Modifiers == ModifierKeys.Control && e.LeftButton == MouseButtonState.Pressed)
            {
                // Salvează tool-ul curent și activează pan temporar
                _previousTool = _toolManager.ActiveTool;
                _toolManager.SetActive("Pan");
                _isPanning = true;
                _lastPanPoint = e.GetPosition(this);
                Cursor = Cursors.Hand;
                DrawingCanvas.CaptureMouse();
                e.Handled = true;
                return;
            }

            DeselectAllTexts();

            if (!IsClickOnSelectableShape(e.OriginalSource))
            {
                if (_selectionService.SelectedElements.Count > 0)
                    _selectionService.DeselectAll(DrawingCanvas);

                _shapeSelectionService.Deselect();
            }

            _host.HandleMouseDown(logicalPos, e);
        }

        private void OnCanvasMouseMove(object sender, MouseEventArgs e)
        {
            var logicalPos = GetLogicalPosition(e);

            if (e.OriginalSource is Thumb)
                return;

            if (_isRightMouseHeld)
            {
                var currentPoint = e.GetPosition(this);
                var delta = (currentPoint - _rightClickStartPoint);

                if (!_isRightMouseMoving && (Math.Abs(delta.X) > SystemParameters.MinimumHorizontalDragDistance ||
                                             Math.Abs(delta.Y) > SystemParameters.MinimumVerticalDragDistance))
                {
                    _isRightMouseMoving = true;
                    var logicalPosMove = GetLogicalPosition(e);
                    _toolManager.SetActive("Selection");
                    var fakeButtonEvent = new MouseButtonEventArgs(e.MouseDevice, e.Timestamp, MouseButton.Right)
                    {
                        RoutedEvent = Mouse.MouseDownEvent,
                        Source = e.Source
                    };

                    _host.HandleMouseDown(logicalPosMove, fakeButtonEvent); // începe selecția
                }

                if (_isRightMouseMoving)
                {
                    _host.HandleMouseMove(logicalPos, e);
                }

                return;
            }

            if (_isPanning)
            {
                var current = e.GetPosition(this);
                _lastPanPoint = _zoomPanService.Pan(current, _lastPanPoint, ZoomTranslate);
                return;
            }

            var activeTool = _toolManager.ActiveTool;
            if (activeTool is IDrawingTool drawingTool && drawingTool.IsDrawing)
                _toolInterceptorService.IsUserActing = true;
            else
                _toolInterceptorService.IsUserActing = false;

            _host.HandleMouseMove(logicalPos, e);
        }

        private void OnCanvasLeftClickEnd(object sender, MouseButtonEventArgs e)
        {
            var logicalPos = GetLogicalPosition(e);

            if (_isPanning)
            {
                _isPanning = false;
                Cursor = _previousCursor ?? Cursors.Arrow;
                DrawingCanvas.ReleaseMouseCapture();

                // ✅ Revino la tool-ul anterior
                if (_previousTool != null)
                {
                    _toolManager.SetActive(_previousTool.Name);
                    _previousTool = null;
                }

                e.Handled = true;
                return;
            }

            _host.HandleMouseUp(logicalPos, e);
        }

        private Point GetLogicalPosition(MouseEventArgs e)
        {
            if (DrawingCanvas.RenderTransform is TransformGroup tg && tg.Inverse != null)
                return tg.Inverse.Transform(e.GetPosition(DrawingCanvas));
            return e.GetPosition(DrawingCanvas);
        }

        public void StartNewRemoteLine() => _host.StartRemoteLine();
        public void AddLine(IEnumerable<Point> points, Brush color, double thickness) => _host.AddRemoteLine(points, color, thickness);
        public void AddLivePoint(Point point, Brush color) => _host.AddRemoteLivePoint(point, color);
        public void MoveCursorImage(Point position, BitmapImage? image) => _host.UpdateCursor(position, image);

        private void DrawingCanvas_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(BPMNShapeModel)))
                e.Effects = DragDropEffects.Copy;
            else
                e.Effects = DragDropEffects.None;

            e.Handled = true;
        }

        private void DrawingCanvas_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(typeof(BPMNShapeModel)) is not BPMNShapeModel shape)
                return;

            var dropPos = e.GetPosition(DrawingCanvas);

            var visualElement = _dropService.HandleDrop(shape, dropPos);
            if (visualElement == null)
                return;

            _dropService.PlaceElementOnCanvas(visualElement, dropPos);
            _dropService.RegisterNodeWhenReady(visualElement);
            _dropService.SetupConnectorButton(visualElement);
        }

        private void DrawingRoot_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (IsClickOnSelectableShape(e.OriginalSource))
            {
                _isRightMouseHeld = false;
                return;
            }

            _isRightMouseHeld = true;
            _isRightMouseMoving = false; // Resetăm

            _rightClickStartPoint = e.GetPosition(this);
        }

        private void DrawingRoot_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (e.Handled) return; 

            _isRightMouseHeld = false;
            var logicalPos = GetLogicalPosition(e);
            _lastRightClickCanvasPosition = logicalPos;

            if (_isRightMouseMoving)
            {
                _isRightMouseMoving = false;
                _host.HandleMouseUp(logicalPos, e);
                return;
            }

            var contextMenu = GetContextMenuFromVisualTree(e.OriginalSource as DependencyObject);
            if (contextMenu != null)
            {
                _host.HandleMouseUp(logicalPos, e);

                contextMenu.PlacementTarget = this;
                contextMenu.Placement = PlacementMode.MousePoint;
                contextMenu.IsOpen = true;

                e.Handled = true;
                return;
            }

            _host.HandleMouseUp(logicalPos, e);

            if (DrawingCanvas.ContextMenu != null)
            {
                DrawingCanvas.ContextMenu.PlacementTarget = DrawingCanvas;
                DrawingCanvas.ContextMenu.Placement = PlacementMode.MousePoint;
                DrawingCanvas.ContextMenu.IsOpen = true;
                e.Handled = true;
            }
        }

        private bool IsClickOnSelectableShape(object source)
        {
            // Poți adăuga aici orice tip vrei să excluzi
            return source is FrameworkElement fe &&
                   fe != DrawingCanvas &&
                   !(fe is Canvas) &&
                   !(fe is Adorner) &&
                   !(fe is WhiteBoardControl);
        }

        private static ContextMenu? GetContextMenuFromVisualTree(DependencyObject source)
        {
            while (source != null)
            {
                if (source is FrameworkElement fe && fe.ContextMenu != null)
                    return fe.ContextMenu;

                if (source is Visual || source is Visual3D)
                {
                    source = VisualTreeHelper.GetParent(source);
                }
                else
                {
                    source = LogicalTreeHelper.GetParent(source);
                }
            }

            return null;
        }

        public void CopySelectedElements()
        {
            // (blank)
        }

        public void PasteElements()
        {
            // (blank)
        }

        public void DeleteSelectedElements()
        {
            // (blank)
        }

        public async void AddImageAtPosition()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select an Image",
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif",
                Multiselect = false
            };

            if (dialog.ShowDialog() == true)
            {
                var selectedPath = dialog.FileName;

                if (!string.IsNullOrEmpty(selectedPath))
                {
                    var uri = new Uri(selectedPath);
                    var _shapeFactory = ContainerLocator.Container.Resolve<IGenericShapeFactory>();
                    var shapeInstance = _shapeFactory.Create(ShapeType.Image); 
                    var shape = new BPMNShapeModel
                    {
                        Name = System.IO.Path.GetFileNameWithoutExtension(selectedPath),
                        SvgUri = uri,
                        Category = "Images",
                        Type = ShapeType.Image,
                        ShapeContent = shapeInstance // 🔑 IMPORTANT!
                    };

                    var position = _lastRightClickCanvasPosition ?? new Point(this.ActualWidth / 2, this.ActualHeight / 2);

                    var visualElement = _dropService.HandleDrop(shape, position);
                    if (visualElement != null)
                    {
                        _dropService.PlaceElementOnCanvas(visualElement, position);
                        _dropService.RegisterNodeWhenReady(visualElement);
                        _dropService.SetupConnectorButton(visualElement);
                    }
                }
            }
        }

        public void SaveToFile(string filePath, string format)
        {
            DrawingCanvas.Measure(new Size(DrawingCanvas.Width, DrawingCanvas.Height));
            DrawingCanvas.Arrange(new Rect(new Size(DrawingCanvas.Width, DrawingCanvas.Height)));

            var dpi = 96d;
            var rtb = new RenderTargetBitmap(
                (int)DrawingCanvas.Width,
                (int)DrawingCanvas.Height,
                dpi,
                dpi,
                PixelFormats.Pbgra32);

            rtb.Render(DrawingCanvas);

            BitmapEncoder encoder = format.ToLower() switch
            {
                "jpeg" or "jpg" => new JpegBitmapEncoder(),
                "bmp" => new BmpBitmapEncoder(),
                "tiff" => new TiffBitmapEncoder(),
                _ => new PngBitmapEncoder()
            };

            encoder.Frames.Add(BitmapFrame.Create(rtb));

            using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            encoder.Save(stream);
        }

    }
}
