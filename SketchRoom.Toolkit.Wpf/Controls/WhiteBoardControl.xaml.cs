using DrawingStateService;
using DrawingStateService.States;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SketchRoom.Toolkit.Wpf.Controls
{
    public partial class WhiteBoardControl : UserControl
    {
        private readonly DrawingStateService.DrawingStateService _stateService;
        private readonly DrawingService _drawingService;
        private readonly PanAndZoomService _panZoomService;
        private readonly SelectionService _selectionService;
        private readonly LiveRemoteDrawingService _remoteDrawingService;
        private readonly ImageSaveService _imageSaveService;

        private Polyline _currentLine;
        private bool _isDrawing;
        private bool _isPanning;
        private Point _lastMousePosition;

        private bool _isSelecting;
        private Point _selectionStart;
        private Rectangle _selectionRectangle;

        public event Action<List<Point>> LineDrawn;
        public event Action<Point> LivePointDrawn;
        public event Action<Point> MouseMoved;

        public WhiteBoardControl()
        {
            InitializeComponent();
            //DrawingCanvas.PreviewMouseRightButtonDown += (s, e) => SaveRecentLinesAsImage();

            _stateService = ContainerLocator.Container.Resolve<DrawingStateService.DrawingStateService>();
            _drawingService = ContainerLocator.Container.Resolve<DrawingService>();
            _panZoomService = ContainerLocator.Container.Resolve<PanAndZoomService>();
            _selectionService = ContainerLocator.Container.Resolve<SelectionService>();
            _remoteDrawingService = ContainerLocator.Container.Resolve<LiveRemoteDrawingService>();
            _imageSaveService = ContainerLocator.Container.Resolve<ImageSaveService>();
        }


        private Point GetLogicalPosition(MouseEventArgs e)
        {
            if (DrawingCanvas.RenderTransform is TransformGroup tg && tg.Inverse != null)
                return tg.Inverse.Transform(e.GetPosition(DrawingCanvas));

            return e.GetPosition(DrawingCanvas);
        }

        private void Canvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers != ModifierKeys.Control) return;

            _panZoomService.Zoom(ZoomScale, ZoomTranslate, e.GetPosition(DrawingCanvas), e.Delta);
            e.Handled = true;
        }

        private void Canvas_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_stateService.IsDraggingText) return;

            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                _isPanning = true;
                _lastMousePosition = e.GetPosition(this);
                DrawingCanvas.CaptureMouse();
                Cursor = Cursors.SizeAll;
                return;
            }

            var pos = GetLogicalPosition(e);

            if (_stateService.IsSelectionModeEnabled)
            {
                _isSelecting = true;
                _selectionStart = pos;
                _selectionRectangle = _selectionService.StartSelection(pos);
                DrawingCanvas.Children.Add(_selectionRectangle);
                Canvas.SetLeft(_selectionRectangle, pos.X);
                Canvas.SetTop(_selectionRectangle, pos.Y);
                return;
            }

            _isDrawing = true;
            _currentLine = _drawingService.StartNewLine(pos, _stateService.SelectedColor);
            DrawingCanvas.Children.Add(_currentLine);

            LivePointDrawn?.Invoke(pos);
        }

        private void Canvas_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_isPanning)
            {
                _lastMousePosition = _panZoomService.CalculatePan(e.GetPosition(this), _lastMousePosition, ZoomTranslate);
                return;
            }

            var pos = GetLogicalPosition(e);

            if (_isSelecting && _selectionRectangle != null)
            {
                _selectionService.UpdateSelection(_selectionRectangle, _selectionStart, pos);
                return;
            }

            if (_isDrawing && _currentLine != null)
            {
                _drawingService.AddPointToLine(_currentLine, pos);
                LivePointDrawn?.Invoke(pos);
                MouseMoved?.Invoke(pos);
            }
        }

        private void Canvas_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isPanning)
            {
                _isPanning = false;
                DrawingCanvas.ReleaseMouseCapture();
                Cursor = Cursors.Arrow;
                return;
            }

            if (_isSelecting && _selectionRectangle != null)
            {
                _isSelecting = false;

                var overlayBounds = _selectionService.GetSelectionBounds(_selectionRectangle);
                DrawingCanvas.Children.Remove(_selectionRectangle);
                _selectionRectangle = null;

                _selectionService.HandleSelection(overlayBounds, DrawingCanvas, _currentLine);
                _stateService.IsSelectionModeEnabled = false;
                return;
            }

            if (_isDrawing && _currentLine != null)
            {
                LineDrawn?.Invoke(_currentLine.Points.ToList());
                _isDrawing = false;
                _drawingService.FinishLine(_currentLine);
            }
        }

        public void AddLine(IEnumerable<Point> points, Brush color, double thickness = 2)
        {
            var line = _drawingService.StartNewLine(points.First(), color, thickness);
            foreach (var p in points.Skip(1))
                _drawingService.AddPointToLine(line, p);
            DrawingCanvas.Children.Add(line);
        }

        public void AddLivePoint(Point point, Brush color, double thickness = 2)
        {
            _remoteDrawingService.AddLivePoint(DrawingCanvas, point, color, thickness);
        }

        public void ResetLiveLine() => _remoteDrawingService.ResetLiveLine(DrawingCanvas);

        public void StartNewRemoteLine() => ResetLiveLine();

        public void MoveCursorImage(Point point, BitmapImage image = null)
        {
            _remoteDrawingService.MoveCursorImage(DrawingCanvas, point, image);
        }

        private void SaveRecentLinesAsImage()
        {
            if (!_drawingService.RecentLines.Any()) return;

            var geometryGroup = new GeometryGroup();
            foreach (var line in _drawingService.RecentLines)
                geometryGroup.Children.Add(line.RenderedGeometry.Clone());

            var bounds = geometryGroup.Bounds;

            var drawingVisual = new DrawingVisual();
            using (var dc = drawingVisual.RenderOpen())
            {
                dc.DrawRectangle(Brushes.Black, null, new Rect(0, 0, 28, 28));

                var transform = new TransformGroup();
                transform.Children.Add(new TranslateTransform(-bounds.X, -bounds.Y));
                transform.Children.Add(new ScaleTransform(28 / bounds.Width, 28 / bounds.Height));

                geometryGroup.Transform = transform;
                dc.DrawGeometry(null, new Pen(Brushes.White, 2), geometryGroup);
            }

            var bmp = new RenderTargetBitmap(28, 28, 96, 96, PixelFormats.Pbgra32);
            bmp.Render(drawingVisual);

            _imageSaveService.SaveTrainingImage(bmp, "3");

            foreach (var line in _drawingService.RecentLines)
                DrawingCanvas.Children.Remove(line);

            _drawingService.ClearRecentLines();
        }
    }
}
