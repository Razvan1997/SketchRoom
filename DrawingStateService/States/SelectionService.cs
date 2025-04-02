using SketchRoom.AI.Predictions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace DrawingStateService.States
{
    public class SelectionService
    {
        private DrawingStateService _stateService;

        public SelectionService()
        {
            _stateService = ContainerLocator.Container.Resolve<DrawingStateService>();
        }


        public Rectangle StartSelection(Point start)
        {
            return new Rectangle
            {
                Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#800080")),
                StrokeThickness = 2,
                Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#40FF00FF")),
                IsHitTestVisible = false
            };
        }

        public void UpdateSelection(Rectangle rectangle, Point start, Point current)
        {
            double x = Math.Min(current.X, start.X);
            double y = Math.Min(current.Y, start.Y);
            double width = Math.Abs(current.X - start.X);
            double height = Math.Abs(current.Y - start.Y);

            Canvas.SetLeft(rectangle, x);
            Canvas.SetTop(rectangle, y);
            rectangle.Width = width;
            rectangle.Height = height;
        }

        public Rect GetSelectionBounds(Rectangle rectangle)
        {
            double x = Canvas.GetLeft(rectangle);
            double y = Canvas.GetTop(rectangle);
            return new Rect(x, y, rectangle.Width, rectangle.Height);
        }

        public void HandleSelection(Rect overlayBounds, Canvas canvas, Polyline latestLine = null)
        {
            var segmenter = new CharacterSegmentation();
            var result = segmenter.PredictFromOverlay(overlayBounds, canvas);

            AddPredictedCharacterToCanvas(result, latestLine, canvas);

            var toRemove = new List<UIElement>();
            foreach (var child in canvas.Children)
            {
                if (child is Polyline line)
                {
                    if (line.Points.Any(p => overlayBounds.Contains(p)))
                    {
                        toRemove.Add(line);
                    }
                }
            }

            foreach (var item in toRemove)
                canvas.Children.Remove(item);
        }

        private void AddPredictedCharacterToCanvas(string digit, Polyline sourceLine, Canvas canvas)
        {
            if (string.IsNullOrWhiteSpace(digit) || sourceLine == null)
                return;

            var points = sourceLine.Points;
            if (points.Count < 2)
                return;

            var firstPoint = points.First();
            var lastPoint = points.Last();

            var midX = (firstPoint.X + lastPoint.X) / 2;
            var midY = (firstPoint.Y + lastPoint.Y) / 2;

            var textBlock = new TextBlock
            {
                Text = digit,
                FontSize = 32,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.Black,
                Background = Brushes.Transparent,
                Cursor = Cursors.SizeAll
            };

            Canvas.SetLeft(textBlock, midX - 10);
            Canvas.SetTop(textBlock, midY - 20);

            Point? dragStart = null;

            textBlock.PreviewMouseLeftButtonDown += (s, e) =>
            {
                dragStart = e.GetPosition(canvas);
                textBlock.CaptureMouse();
                _stateService.IsDraggingText = true;
                e.Handled = true;
            };
            
            textBlock.PreviewMouseMove += (s, e) =>
            {
                if (dragStart.HasValue && e.LeftButton == MouseButtonState.Pressed)
                {
                    Point currentPos = e.GetPosition(canvas);
                    var delta = currentPos - dragStart.Value;

                    double left = Canvas.GetLeft(textBlock);
                    double top = Canvas.GetTop(textBlock);

                    Canvas.SetLeft(textBlock, left + delta.X);
                    Canvas.SetTop(textBlock, top + delta.Y);

                    dragStart = currentPos;
                }
            };

            textBlock.PreviewMouseLeftButtonUp += (s, e) =>
            {
                textBlock.ReleaseMouseCapture();
                dragStart = null;
                _stateService.IsDraggingText = false;
                e.Handled = true;
            };

            canvas.Children.Add(textBlock);
        }
    }
}
