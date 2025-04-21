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

namespace SketchRoom.Toolkit.Wpf.Controls
{
    public partial class WhiteBoardControl : UserControl, IWhiteBoardAdapter
    {
        private readonly List<ITextInteractiveShape> _textShapes = new();
        private BpmnConnectorTool? _connectorTool;
        private readonly List<BPMNConnection> _connections = new();
        private readonly Dictionary<FrameworkElement, BPMNNode> _nodes = new();
        private WhiteBoardHost _host;
        private IZoomPanService _zoomPanService;
        private IToolManager _toolManager;
        private ISnapService? _snapService;
        private SelectedToolService _selectedToolService;
        private IDrawingPreferencesService? _drawingPreferencesService;
        private readonly IDropService _dropService;
        private Cursor _previousCursor;
        private Point _lastPanPoint;
        private bool _isPanning;
        private ToolInterceptorService _toolInterceptorService;
        public event Action<List<Point>>? LineDrawn;
        public event Action<Point>? LivePointDrawn;
        public event Action<Point>? MouseMoved;

        private readonly IBpmnShapeFactory _factory;
        private readonly WhiteBoard.Core.Services.Interfaces.ISelectionService _selectionService;
        public WhiteBoardControl()
        {
            InitializeComponent();
            _factory = ContainerLocator.Container.Resolve<IBpmnShapeFactory>();

            var tabService = ContainerLocator.Container.Resolve<IWhiteBoardTabService>();
            _toolManager = tabService.GetCurrentToolManager();
            var drawingService = ContainerLocator.Container.Resolve<IDrawingService>();
            var canvasRenderer = ContainerLocator.Container.Resolve<ICanvasRenderer>();
            _zoomPanService = ContainerLocator.Container.Resolve<IZoomPanService>();
            _snapService = ContainerLocator.Container.Resolve<ISnapService>();
            _drawingPreferencesService = ContainerLocator.Container.Resolve<IDrawingPreferencesService>();
            _selectedToolService = ContainerLocator.Container.Resolve<SelectedToolService>();
            _toolInterceptorService = new ToolInterceptorService(_toolManager, _selectedToolService);
            _host = new WhiteBoardHost(DrawingCanvas, _toolManager, drawingService, canvasRenderer);

            var freeDrawTool = new FreeDrawTool(drawingService, DrawingCanvas);
            freeDrawTool.StrokeCompleted += points => LineDrawn?.Invoke(points);
            freeDrawTool.PointDrawn += point => LivePointDrawn?.Invoke(point);
            freeDrawTool.PointerMoved += point => MouseMoved?.Invoke(point);

            var eraserTool = new EraserTool(drawingService, DrawingCanvas);
            var bpmnTool = new BpmnTool(DrawingCanvas, _snapService, SnapGridCanvas, _toolManager);

            _connectorTool = new BpmnConnectorTool(DrawingCanvas, _connections, _nodes, this, _toolManager, _snapService);
            var connectorCurvedTool = new BpmnConnectorCurvedTool(DrawingCanvas, _connections, _nodes, this, _toolManager, _snapService);

            var rotateTool = new RotateTool(DrawingCanvas);
            _selectionService = new SelectionService(_connectorTool);
            _selectionService.SelectionChanged += OnSelectionChanged;
            var selectionTool = new SelectionTool(DrawingCanvas, _selectionService,_toolManager);

            var panTool = new PanTool(_zoomPanService, ZoomTranslate);
            var textTool = new TextTool(DrawingCanvas, _toolManager, _factory, _snapService, _drawingPreferencesService, _selectedToolService, _textShapes);

            _toolManager.RegisterTool(freeDrawTool);
            _toolManager.RegisterTool(eraserTool);
            _toolManager.RegisterTool(bpmnTool);
            _toolManager.RegisterTool(_connectorTool);
            _toolManager.RegisterTool(rotateTool);
            _toolManager.RegisterTool(selectionTool);
            _toolManager.RegisterTool(panTool);
            _toolManager.RegisterTool(connectorCurvedTool);
            _toolManager.RegisterTool(textTool);

            var selecteToolService = ContainerLocator.Container.Resolve<SelectedToolService>();
            _dropService = new DropService(DrawingCanvas, _factory, _toolManager, _connectorTool!, connectorCurvedTool, _nodes, selecteToolService);

            this.KeyDown += WhiteBoardControl_KeyDown;
            this.Focusable = true;
            this.Focus();
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
                if (_toolManager.ActiveTool is BpmnConnectorTool connectorTool)
                {
                    connectorTool.DeleteSelectedConnections();
                }

                foreach (var el in _selectionService.SelectedElements.ToList())
                {
                    DrawingCanvas.Children.Remove(el);
                }

                _selectionService.ClearSelection(DrawingCanvas);



                e.Handled = true;
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
            var currentTool = _toolManager.ActiveTool?.Name;

            _toolInterceptorService.InterceptToolSwitch(e);

            var afterInterceptTool = _toolManager.ActiveTool?.Name;

            //if (currentTool == afterInterceptTool && Keyboard.Modifiers == ModifierKeys.Control)
            //{
            //    _toolManager.SetActive("Pan");
            //}
            if ( Keyboard.Modifiers == ModifierKeys.Control)
            {
                _toolManager.SetActive("Pan");
            }
            DeselectAllTexts();

            _host.HandleMouseDown(logicalPos, e);
        }

        private void OnCanvasMouseMove(object sender, MouseEventArgs e)
        {
            var logicalPos = GetLogicalPosition(e);

            if (e.OriginalSource is Thumb)
                return;

            if (_isPanning)
            {
                var current = e.GetPosition(this);
                _lastPanPoint = _zoomPanService.Pan(current, _lastPanPoint, ZoomTranslate);
                return;
            }
            var activeTool = _toolManager.ActiveTool;

            if (activeTool is IDrawingTool drawingTool && drawingTool.IsDrawing)
            {
                _toolInterceptorService.IsUserActing = true;
            }
            else
            {
                _toolInterceptorService.IsUserActing = false;
            }

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
                return;
            }
            _host.HandleMouseUp(logicalPos,e);
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
            //var logicalPos = GetLogicalPosition(e);
            //_toolManager.SetActive("Selection"); // ← activăm tool-ul
            //_host.HandleMouseDown(logicalPos,e);
        }

        private void DrawingRoot_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            var logicalPos = GetLogicalPosition(e);
            _host.HandleMouseUp(logicalPos, e);
        }
    }
}
