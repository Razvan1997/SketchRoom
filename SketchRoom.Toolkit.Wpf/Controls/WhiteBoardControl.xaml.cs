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

namespace SketchRoom.Toolkit.Wpf.Controls
{
    public partial class WhiteBoardControl : UserControl, IWhiteBoardAdapter
    {
        private BpmnConnectorTool? _connectorTool;
        private readonly List<BPMNConnection> _connections = new();
        private readonly Dictionary<FrameworkElement, BPMNNode> _nodes = new();
        private WhiteBoardHost _host;
        private IZoomPanService _zoomPanService;
        private IToolManager _toolManager;
        private ISelectionService _selectionService;
        private ISnapService? _snapService;

        private bool _isPanning;
        private Point _lastPanPoint;
        private Cursor _previousCursor;

        private bool _isSelecting;
        private Point _selectionStart;
        private Rectangle? _selectionRectangle;

        public event Action<List<Point>>? LineDrawn;
        public event Action<Point>? LivePointDrawn;
        public event Action<Point>? MouseMoved;

        private readonly IBpmnShapeFactory _factory;

        public WhiteBoardControl()
        {
            InitializeComponent();
            _factory = ContainerLocator.Container.Resolve<IBpmnShapeFactory>();

            _toolManager = ContainerLocator.Container.Resolve<IToolManager>();
            var drawingService = ContainerLocator.Container.Resolve<IDrawingService>();
            var canvasRenderer = ContainerLocator.Container.Resolve<ICanvasRenderer>();
            _zoomPanService = ContainerLocator.Container.Resolve<IZoomPanService>();
            _selectionService = ContainerLocator.Container.Resolve<ISelectionService>();
            _snapService = ContainerLocator.Container.Resolve<ISnapService>();

            _host = new WhiteBoardHost(DrawingCanvas, _toolManager, drawingService, canvasRenderer);

            var freeDrawTool = new FreeDrawTool(drawingService, DrawingCanvas);
            freeDrawTool.StrokeCompleted += points => LineDrawn?.Invoke(points);
            freeDrawTool.PointDrawn += point => LivePointDrawn?.Invoke(point);
            freeDrawTool.PointerMoved += point => MouseMoved?.Invoke(point);

            var eraserTool = new EraserTool(drawingService, DrawingCanvas);
            _toolManager.RegisterTool(freeDrawTool);
            _toolManager.RegisterTool(eraserTool);

            var bpmnTool = new BpmnTool(DrawingCanvas, _snapService, SnapGridCanvas);
            _toolManager.RegisterTool(bpmnTool);

            _connectorTool = new BpmnConnectorTool(DrawingCanvas, _connections, _nodes, this, _toolManager, _snapService);
            _toolManager.RegisterTool(_connectorTool);

            _toolManager.SetActive("FreeDraw");

            DrawingCanvas.PreviewMouseRightButtonDown += Canvas_PreviewMouseRightButtonDown;
            DrawingCanvas.PreviewMouseRightButtonUp += Canvas_PreviewMouseRightButtonUp;

            this.KeyDown += WhiteBoardControl_KeyDown;
            this.Focusable = true;
            this.Focus();
        }

        private void WhiteBoardControl_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                if (_toolManager.ActiveTool is BpmnConnectorTool connectorTool)
                {
                    connectorTool.DeleteSelectedConnections();
                }
            }
        }

        private void Canvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                _zoomPanService.Zoom(ZoomScale, ZoomTranslate, e.GetPosition(DrawingCanvas), e.Delta);
                e.Handled = true;
            }
        }

        private void Canvas_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var logicalPos = GetLogicalPosition(e);

            if (_toolManager.ActiveTool?.Name == "Connector")
            {
                // Evită selectarea liniei deja trasate
                if (e.OriginalSource is Path path && path.Data is Geometry)
                    return;

                // Dacă se trasează o linie, continuăm trasarea
                if (_connectorTool?.IsDrawing == true)
                {
                    _connectorTool.OnMouseDown(logicalPos);
                    e.Handled = true;
                    return;
                }

                if (e.OriginalSource is Rectangle)
                {
                    _connectorTool.OnMouseDown(logicalPos);
                    e.Handled = true;
                    return;
                }

                // Dacă nu se trasează, click pe whiteboard = deselectare + schimbare tool
                if (e.OriginalSource == DrawingCanvas)
                {
                    _connectorTool?.DeselectCurrent();
                    _connectorTool?.DeselectAllConnections();
                    _toolManager.SetActive("FreeDraw");
                    _host.HandleMouseDown(logicalPos);
                }

                e.Handled = true;
                return;
            }

            // Alte tool-uri (ex: BpmnTool)
            if (e.OriginalSource == DrawingCanvas)
            {
                if (_toolManager.ActiveTool is BpmnTool bpmnTool)
                {
                    bpmnTool.DeselectCurrent();
                    _toolManager.SetActive("FreeDraw");
                }
                else if (_toolManager.ActiveTool is BpmnConnectorTool bpmnConnectorTool)
                {
                    bpmnConnectorTool.DeselectCurrent();
                    bpmnConnectorTool.DeselectAllConnections();
                    _toolManager.SetActive("FreeDraw");
                }

                _host.HandleMouseDown(logicalPos);
            }
        }

        private void Canvas_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            var logicalPos = GetLogicalPosition(e);

            if (_toolManager.ActiveTool?.Name == "Connector")
            {
                _connectorTool?.OnMouseMove(logicalPos);
                return;
            }

            if (e.OriginalSource is Thumb)
                return;
            if (_isPanning)
            {
                var current = e.GetPosition(this);
                _lastPanPoint = _zoomPanService.Pan(current, _lastPanPoint, ZoomTranslate);
                return;
            }

            if (_isSelecting && _selectionRectangle != null)
            {
                var current = GetLogicalPosition(e);
                UpdateSelectionRect(_selectionRectangle, _selectionStart, current);
                return;
            }

            _host.HandleMouseMove(logicalPos);
        }

        private void Canvas_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var logicalPos = GetLogicalPosition(e);

            if (_toolManager.ActiveTool?.Name == "Connector")
            {
                _connectorTool?.OnMouseUp(logicalPos);
                return;
            }

            if (_isPanning)
            {
                _isPanning = false;
                Cursor = _previousCursor ?? Cursors.Arrow;
                DrawingCanvas.ReleaseMouseCapture();
                return;
            }

            _host.HandleMouseUp(logicalPos);
        }

        private void Canvas_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isSelecting = true;
            _selectionStart = GetLogicalPosition(e);

            _selectionRectangle = new Rectangle
            {
                Stroke = Brushes.Blue,
                StrokeThickness = 1,
                Fill = new SolidColorBrush(Color.FromArgb(60, 0, 120, 255)),
                IsHitTestVisible = false
            };

            DrawingCanvas.Children.Add(_selectionRectangle);
            Canvas.SetLeft(_selectionRectangle, _selectionStart.X);
            Canvas.SetTop(_selectionRectangle, _selectionStart.Y);
        }

        private void Canvas_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isSelecting && _selectionRectangle != null)
            {
                var end = GetLogicalPosition(e);
                var bounds = new Rect(_selectionStart, end);

                DrawingCanvas.Children.Remove(_selectionRectangle);
                _selectionRectangle = null;
                _isSelecting = false;

                _selectionService.HandleSelection(bounds, DrawingCanvas);
            }
        }

        private void UpdateSelectionRect(Rectangle rect, Point start, Point current)
        {
            double x = Math.Min(start.X, current.X);
            double y = Math.Min(start.Y, current.Y);
            double width = Math.Abs(current.X - start.X);
            double height = Math.Abs(current.Y - start.Y);

            Canvas.SetLeft(rect, x);
            Canvas.SetTop(rect, y);
            rect.Width = width;
            rect.Height = height;
        }

        private Point GetLogicalPosition(MouseEventArgs e)
        {
            if (DrawingCanvas.RenderTransform is TransformGroup tg && tg.Inverse != null)
                return tg.Inverse.Transform(e.GetPosition(DrawingCanvas));
            return e.GetPosition(DrawingCanvas);
        }

        public void StartNewRemoteLine()
        {
            _host.StartRemoteLine();
        }

        public void AddLine(IEnumerable<Point> points, Brush color, double thickness)
        {
            _host.AddRemoteLine(points, color, thickness);
        }

        public void AddLivePoint(Point point, Brush color)
        {
            _host.AddRemoteLivePoint(point, color);
        }

        public void MoveCursorImage(Point position, BitmapImage? image)
        {
            _host.UpdateCursor(position, image);
        }


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
            FrameworkElement? visualElement = null;

            if (shape.SvgUri is not null)
            {
                // Creează forma SVG din factory
                var element = new BpmnWhiteBoardElement(shape.SvgUri, _factory);
                element.SetPosition(dropPos);
                visualElement = element.Visual as FrameworkElement;

                if (element.Visual is IInteractiveShape interactiveSvg)
                {
                    interactiveSvg.ShapeClicked += (s, evt) =>
                    {
                        _toolManager.SetActive("BpmnTool");

                        if (_toolManager.ActiveTool is BpmnTool bpmnTool)
                        {
                            var pos = evt.GetPosition(DrawingCanvas);
                            bpmnTool.OnMouseDown(pos);
                        }

                        evt.Handled = true;
                    };
                }
            }
            else if (shape.ShapeContent is IInteractiveShape shapePrototype)
            {
                // Creează instanță nouă din tipul formei
                var type = shapePrototype.GetType();
                if (Activator.CreateInstance(type) is IInteractiveShape newInstance)
                {
                    var element = new BpmnWhiteBoardElementXaml(newInstance);
                    element.SetPosition(dropPos);
                    visualElement = element.Visual as FrameworkElement;

                    element.Clicked += (s, evt) =>
                    {
                        _toolManager.SetActive("BpmnTool");

                        if (_toolManager.ActiveTool is BpmnTool bpmnTool)
                        {
                            var pos = evt.GetPosition(DrawingCanvas);
                            bpmnTool.OnMouseDown(pos);
                        }

                        evt.Handled = true;
                    };
                }
            }

            if (visualElement != null)
            {
                Canvas.SetLeft(visualElement, dropPos.X);
                Canvas.SetTop(visualElement, dropPos.Y);
                DrawingCanvas.Children.Add(visualElement);

                void RegisterNode()
                {
                    var pos = new Point(Canvas.GetLeft(visualElement), Canvas.GetTop(visualElement));
                    var width = visualElement.ActualWidth;
                    var height = visualElement.ActualHeight;

                    if (width > 0 && height > 0)
                    {
                        var node = new BPMNNode(pos, width, height);
                        _nodes[visualElement] = node;
                    }
                }

                // Dacă shape-ul e deja măsurat
                if (visualElement.IsLoaded && visualElement.ActualWidth > 0 && visualElement.ActualHeight > 0)
                {
                    RegisterNode();
                }
                else
                {
                    // Dacă nu e măsurat încă, așteaptă să se încarce
                    visualElement.Loaded += (s, e) => RegisterNode();
                }

                // 🔗 Conectare cu butonul de „bulină” dacă e BpmnShapeControl
                if (visualElement is BpmnShapeControl shapeControl)
                {
                    shapeControl.ConnectionPointClicked += (s, direction) =>
                    {
                        if (s is IInteractiveShape interactive)
                        {
                            _connectorTool?.SetSelected(interactive, direction); // nou
                        }
                        _toolManager.SetActive("Connector");
                    };
                }
            }
        }
    }
}
