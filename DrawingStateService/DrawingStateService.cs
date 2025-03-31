
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace DrawingStateService
{
    public class DrawingStateService 
    {
        public Brush SelectedColor { get; set; }
        public bool IsSelectionModeEnabled { get; set; }


        public void HandleSelection(Rect overlayBounds, Canvas canvas, Polyline latestLine = null)
        {
            var segmenter = new CharacterSegmentation();
            var result = segmenter.PredictFromOverlay(overlayBounds, canvas);

            // 🔤 Desenează caracterul în canvas (dacă e linie disponibilă)
            AddPredictedCharacterToCanvas(result, latestLine, canvas);

            // 🧹 Șterge liniile din zona selectată
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

        public void AddPredictedCharacterToCanvas(string digit, Polyline sourceLine, Canvas canvas)
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
                IsHitTestVisible = false
            };

            canvas.Children.Add(textBlock);
            Canvas.SetLeft(textBlock, midX - 10);
            Canvas.SetTop(textBlock, midY - 20);
        }
    }

}
