using DrawingStateService;
using SketchRoom.AI.Predictions;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

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

        private bool _isSelecting;
        private Point _selectionStart;
        private Rectangle _selectionRectangle;
        private List<Polyline> _recentLines = new();
        public WhiteBoardControl()
        {
            InitializeComponent();
            DrawingCanvas.PreviewMouseRightButtonDown += Canvas_PreviewMouseRightButtonDown;
        }

        private void Canvas_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            SaveRecentLinesAsImage();
        }

        private void SaveRecentLinesAsImage()
        {
            if (_recentLines == null || _recentLines.Count == 0) return;

            var geometryGroup = new GeometryGroup();
            foreach (var line in _recentLines)
                geometryGroup.Children.Add(line.RenderedGeometry.Clone());

            var bounds = geometryGroup.Bounds;

            var drawingVisual = new DrawingVisual();
            using (var dc = drawingVisual.RenderOpen())
            {
                dc.DrawRectangle(Brushes.Black, null, new Rect(0, 0, 28, 28));

                var scaleX = 28 / bounds.Width;
                var scaleY = 28 / bounds.Height;
                var translateX = -bounds.X;
                var translateY = -bounds.Y;

                var transform = new TransformGroup();
                transform.Children.Add(new TranslateTransform(translateX, translateY));
                transform.Children.Add(new ScaleTransform(scaleX, scaleY));

                geometryGroup.Transform = transform;

                var pen = new Pen(Brushes.White, 2); // sau ce grosime vrei
                dc.DrawGeometry(null, pen, geometryGroup);
            }

            var bmp = new RenderTargetBitmap(28, 28, 96, 96, PixelFormats.Pbgra32);
            bmp.Render(drawingVisual);

            SaveTrainingImage(bmp, "3"); // schimbă labelul

            // Șterge liniile
            foreach (var line in _recentLines)
                DrawingCanvas.Children.Remove(line);
            _recentLines.Clear();
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
            var drawingService = ContainerLocator.Container.Resolve<DrawingStateService.DrawingStateService>();
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                _isPanning = true;
                _lastMousePosition = e.GetPosition(this);
                DrawingCanvas.CaptureMouse();
                Cursor = Cursors.SizeAll;
                return;
            }

            var pos = GetLogicalPosition(e);

            if (drawingService.IsSelectionModeEnabled)
            {
                _isSelecting = true;
                _selectionStart = pos;

                _selectionRectangle = new Rectangle
                {
                    Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#800080")), // mov închis
                    StrokeThickness = 2,
                    Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#40FF00FF")), // mov transparent
                    IsHitTestVisible = false
                };

                DrawingCanvas.Children.Add(_selectionRectangle);
                Canvas.SetLeft(_selectionRectangle, pos.X);
                Canvas.SetTop(_selectionRectangle, pos.Y);

                return;
            }

            _isDrawing = true;
            _currentLine = new Polyline
            {
                Stroke = drawingService.SelectedColor,
                StrokeThickness = 2
            };

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

            var pos = GetLogicalPosition(e);

            if (_isSelecting && _selectionRectangle != null)
            {
                double x = Math.Min(pos.X, _selectionStart.X);
                double y = Math.Min(pos.Y, _selectionStart.Y);
                double width = Math.Abs(pos.X - _selectionStart.X);
                double height = Math.Abs(pos.Y - _selectionStart.Y);

                Canvas.SetLeft(_selectionRectangle, x);
                Canvas.SetTop(_selectionRectangle, y);
                _selectionRectangle.Width = width;
                _selectionRectangle.Height = height;

                return;
            }

            if (_isDrawing && _currentLine != null)
            {
                _currentLine.Points.Add(pos);
                LivePointDrawn?.Invoke(pos);
                MouseMoved?.Invoke(pos);
            }
        }

        private void Canvas_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var drawingService = ContainerLocator.Container.Resolve<DrawingStateService.DrawingStateService>();

            if (_isPanning)
            {
                _isPanning = false;
                DrawingCanvas.ReleaseMouseCapture();
                Cursor = Cursors.Arrow;
                return;
            }

            if (_isSelecting)
            {
                _isSelecting = false;

                if (_selectionRectangle != null)
                {
                    var x = Canvas.GetLeft(_selectionRectangle);
                    var y = Canvas.GetTop(_selectionRectangle);
                    var width = _selectionRectangle.Width;
                    var height = _selectionRectangle.Height;

                    DrawingCanvas.Children.Remove(_selectionRectangle);
                    _selectionRectangle = null;

                    var overlayBounds = new Rect(x, y, width, height);

                    drawingService.HandleSelection(overlayBounds, DrawingCanvas, _currentLine);

                    drawingService.IsSelectionModeEnabled = false;
                }

                return;
            }

            if (_isDrawing && _currentLine != null)
            {
                LineDrawn?.Invoke(_currentLine.Points.ToList());
                _isDrawing = false;
                _recentLines.Add(_currentLine);
                
                //PredictCharacterFromCurrentLine();
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

        //private void PredictCharacterFromCurrentLine()
        //{
        //    if (_currentLine == null || _currentLine.Points.Count < 5)
        //        return;

        //    var drawingVisual = new DrawingVisual();

        //    using (var dc = drawingVisual.RenderOpen())
        //    {
        //        dc.DrawRectangle(Brushes.Black, null, new Rect(0, 0, 28, 28));

        //        var bounds = VisualTreeHelper.GetDescendantBounds(_currentLine);
        //        var scaleX = 28 / bounds.Width;
        //        var scaleY = 28 / bounds.Height;
        //        var translateX = -bounds.X;
        //        var translateY = -bounds.Y;

        //        var transform = new TransformGroup();
        //        transform.Children.Add(new TranslateTransform(translateX, translateY));
        //        transform.Children.Add(new ScaleTransform(scaleX, scaleY));

        //        var geometry = _currentLine.RenderedGeometry.Clone();
        //        geometry.Transform = transform;

        //        var pen = new Pen(Brushes.White, 2); // white
        //        dc.DrawGeometry(null, pen, geometry);
        //    }

        //    var bmp = new RenderTargetBitmap(28, 28, 96, 96, PixelFormats.Pbgra32);
        //    bmp.Render(drawingVisual);

        //    var pixels = new float[28 * 28];
        //    var bytes = new byte[28 * 28 * 4];
        //    bmp.CopyPixels(bytes, 28 * 4, 0);

        //    for (int i = 0; i < 28 * 28; i++)
        //    {
        //        var r = bytes[i * 4 + 2];
        //        var g = bytes[i * 4 + 1];
        //        var b = bytes[i * 4];
        //        var intensity = (r + g + b) / 3f / 255f;

        //        pixels[i] = intensity;
        //    }

        //    try
        //    {
        //        var predictor = new LetterPredictor("model.onnx");
        //        var prediction = predictor.Predict(pixels);

        //        const float confidenceThreshold = 0.7f;

        //        if (prediction.Confidence >= confidenceThreshold)
        //        {
        //            DrawingCanvas.Children.Remove(_currentLine);
        //            AddPredictedCharacterToCanvas(prediction.Label);
        //            _currentLine = null;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show("Eroare la predicție: " + ex.Message);
        //    }
        //}

        private static int _imageIndex = 0;

        private void SaveTrainingImage(RenderTargetBitmap bmp, string label)
        {
            var folderPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dataset", label);
            Directory.CreateDirectory(folderPath);

            var filePath = System.IO.Path.Combine(folderPath, $"{_imageIndex}.png");

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bmp));
            using var fs = new FileStream(filePath, FileMode.Create);
            encoder.Save(fs);

            _imageIndex++;
        }
    }


}
