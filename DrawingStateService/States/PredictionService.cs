using SketchRoom.AI.Predictions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows;

namespace DrawingStateService.States
{
    public class PredictionService
    {
        public string PredictCharacterFromLine(Polyline line)
        {
            if (line == null || line.Points.Count < 5)
                return null;

            var drawingVisual = new DrawingVisual();
            using (var dc = drawingVisual.RenderOpen())
            {
                dc.DrawRectangle(Brushes.Black, null, new Rect(0, 0, 28, 28));

                var bounds = VisualTreeHelper.GetDescendantBounds(line);
                var scaleX = 28 / bounds.Width;
                var scaleY = 28 / bounds.Height;
                var transform = new TransformGroup();
                transform.Children.Add(new TranslateTransform(-bounds.X, -bounds.Y));
                transform.Children.Add(new ScaleTransform(scaleX, scaleY));

                var geometry = line.RenderedGeometry.Clone();
                geometry.Transform = transform;

                var pen = new Pen(Brushes.White, 2);
                dc.DrawGeometry(null, pen, geometry);
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
                pixels[i] = intensity;
            }

            var predictor = new LetterPredictor("model.onnx");
            var result = predictor.Predict(pixels);
            return result.Label;
        }
    }
}
