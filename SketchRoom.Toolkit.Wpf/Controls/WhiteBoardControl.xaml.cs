using SketchRoom.AI.Predictions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SketchRoom.Toolkit.Wpf.Controls
{
    /// <summary>
    /// Interaction logic for WhiteBoardControl.xaml
    /// </summary>
    public partial class WhiteBoardControl : UserControl
    {
        private const double MinZoom = 0.3;
        private const double MaxZoom = 3.0;
        private static readonly TimeSpan RemoteDrawThrottle = TimeSpan.FromMilliseconds(16);

        private Polyline _currentLine;
        private Polyline _remoteLine;
        private Image _cursorImage;
        private bool _isDrawing;
        private bool _isPanning;
        private Point _lastMousePosition;
        private DateTime _lastRemoteDrawTime = DateTime.MinValue;

        public event Action<List<Point>> LineDrawn;
        public event Action<Point> LivePointDrawn;
        public event Action<Point> MouseMoved;

        public WhiteBoardControl() => InitializeComponent();

        private Point GetLogicalPosition(MouseEventArgs e)
        {
            if (DrawingCanvas.RenderTransform is TransformGroup tg && tg.Inverse != null)
                return tg.Inverse.Transform(e.GetPosition(DrawingCanvas));

            return e.GetPosition(DrawingCanvas);
        }

        private void Canvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers != ModifierKeys.Control) return;

            double zoomFactor = e.Delta > 0 ? 1.1 : 0.9;
            var position = e.GetPosition(DrawingCanvas);
            double newScale = ZoomScale.ScaleX * zoomFactor;

            if (newScale < MinZoom || newScale > MaxZoom) return;

            ZoomScale.ScaleX = newScale;
            ZoomScale.ScaleY = newScale;

            ZoomTranslate.X = (1 - zoomFactor) * position.X + ZoomTranslate.X * zoomFactor;
            ZoomTranslate.Y = (1 - zoomFactor) * position.Y + ZoomTranslate.Y * zoomFactor;

            e.Handled = true;
        }

        private void Canvas_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                _isPanning = true;
                _lastMousePosition = e.GetPosition(this);
                DrawingCanvas.CaptureMouse();
                Cursor = Cursors.SizeAll;
                return;
            }

            var drawingService = ContainerLocator.Container.Resolve<DrawingStateService.DrawingStateService>();

            _isDrawing = true;
            _currentLine = new Polyline
            {
                Stroke = drawingService.SelectedColor,
                StrokeThickness = 2
            };

            var pos = GetLogicalPosition(e);
            _currentLine.Points.Add(pos);
            DrawingCanvas.Children.Add(_currentLine);

            LivePointDrawn?.Invoke(pos);
        }

        private void Canvas_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_isPanning)
            {
                var current = e.GetPosition(this);
                var delta = current - _lastMousePosition;

                ZoomTranslate.X += delta.X;
                ZoomTranslate.Y += delta.Y;

                _lastMousePosition = current;
                return;
            }

            if (_isDrawing && _currentLine != null)
            {
                var pos = GetLogicalPosition(e);
                _currentLine.Points.Add(pos);

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

            if (_isDrawing && _currentLine != null)
            {
                LineDrawn?.Invoke(_currentLine.Points.ToList());
                _isDrawing = false;

                PredictCharacterFromCurrentLine();
            }
        }

        public void AddLine(IEnumerable<Point> points, Brush color, double thickness = 2)
        {
            var polyline = new Polyline
            {
                Stroke = color,
                StrokeThickness = thickness
            };

            foreach (var p in points)
                polyline.Points.Add(p);

            DrawingCanvas.Children.Add(polyline);
        }

        public void AddLivePoint(Point point, Brush color, double thickness = 2)
        {
            var now = DateTime.Now;
            if ((now - _lastRemoteDrawTime).TotalMilliseconds < 16)
                return;

            _lastRemoteDrawTime = now;

            if (_remoteLine == null)
            {
                _remoteLine = new Polyline
                {
                    Stroke = color,
                    StrokeThickness = thickness
                };
                DrawingCanvas.Children.Add(_remoteLine);
            }

            _remoteLine.Points.Add(point);
        }

        public void MoveCursorImage(Point point, BitmapImage image = null)
        {
            if (_cursorImage == null)
            {
                _cursorImage = new Image
                {
                    Width = 20,
                    Height = 20
                };
                DrawingCanvas.Children.Add(_cursorImage);
            }

            if (image != null)
                _cursorImage.Source = image;

            Canvas.SetLeft(_cursorImage, point.X - 20);
            Canvas.SetTop(_cursorImage, point.Y - 20);
        }

        public void ResetLiveLine()
        {
            if (_remoteLine != null)
            {
                DrawingCanvas.Children.Remove(_remoteLine);
                _remoteLine = null;
            }
        }

        public void StartNewRemoteLine()
        {
            ResetLiveLine();
        }

        private void PredictCharacterFromCurrentLine()
        {
            if (_currentLine == null || _currentLine.Points.Count < 5)
                return;

            var bounds = VisualTreeHelper.GetDescendantBounds(_currentLine);
            var drawingVisual = new DrawingVisual();

            using (var dc = drawingVisual.RenderOpen())
            {
                var vb = new VisualBrush(_currentLine)
                {
                    Stretch = Stretch.Uniform,
                    AlignmentX = AlignmentX.Center,
                    AlignmentY = AlignmentY.Center
                };

                dc.DrawRectangle(vb, null, new Rect(0, 0, 28, 28));
            }

            var bmp = new RenderTargetBitmap(28, 28, 96, 96, PixelFormats.Pbgra32);
            bmp.Render(drawingVisual);

            var pixels = new float[28 * 28];
            var bytes = new byte[28 * 28 * 4];
            bmp.CopyPixels(bytes, 28 * 4, 0);

            for (int i = 0; i < 28 * 28; i++)
            {
                var r = bytes[i * 4 + 2];
                var g = bytes[i * 4 + 1];
                var b = bytes[i * 4];
                var intensity = (r + g + b) / 3f / 255f;
                pixels[i] = 1f - intensity; // fundal negru, desen alb
            }

            try
            {
                var predictor = new LetterPredictor("mnist-12.onnx");
                var prediction = predictor.Predict(pixels);

                MessageBox.Show($"Litera detectată: {prediction}", "Predicție AI", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Eroare la predicție: " + ex.Message);
            }
        }
    }
}
