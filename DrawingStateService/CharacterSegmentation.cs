using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;
using System.Windows.Controls;
using SketchRoom.AI.Predictions;
using System.IO;
using System.Reflection.Emit;
using System.Windows.Shapes;

namespace DrawingStateService
{
    public  class CharacterSegmentation
    {

        private List<BitmapSource> SplitCharacters(BitmapSource source)
        {
            int width = source.PixelWidth;
            int height = source.PixelHeight;
            int stride = width * 4;
            byte[] pixels = new byte[height * stride];
            source.CopyPixels(pixels, stride, 0);

            List<int> columnSum = new();
            for (int x = 0; x < width; x++)
            {
                int sum = 0;
                for (int y = 0; y < height; y++)
                {
                    int idx = y * stride + x * 4;
                    byte r = pixels[idx + 2];
                    byte g = pixels[idx + 1];
                    byte b = pixels[idx];
                    byte a = pixels[idx + 3];
                    if (a > 20 && (r + g + b) / 3 > 30)
                        sum++;
                }
                columnSum.Add(sum);
            }

            // 🔍 detectăm gapuri goale între caractere
            List<int> gapLengths = new();
            int currentGap = 0;
            foreach (int sum in columnSum)
            {
                if (sum == 0)
                    currentGap++;
                else
                {
                    if (currentGap > 0)
                        gapLengths.Add(currentGap);
                    currentGap = 0;
                }
            }

            // 📊 alegem un minGap adaptiv
            int minGap = 3;
            if (gapLengths.Count > 0)
            {
                minGap = Math.Max(2, (int)Math.Round(gapLengths.Average() * 0.6));
            }

            int minCharWidth = 2;
            List<(int start, int end)> segments = new();
            bool inChar = false;
            int start = 0;
            int gapCount = 0;

            for (int i = 0; i < columnSum.Count; i++)
            {
                if (columnSum[i] > 0)
                {
                    if (!inChar)
                    {
                        inChar = true;
                        start = i;
                    }
                    gapCount = 0;
                }
                else if (inChar)
                {
                    gapCount++;
                    if (gapCount >= minGap)
                    {
                        int end = i - gapCount;
                        if (end - start >= minCharWidth)
                            segments.Add((start, end));
                        inChar = false;
                    }
                }
            }

            if (inChar && columnSum.Count - start >= minCharWidth)
                segments.Add((start, columnSum.Count - 1));

            // ✂️ tăiem caracterele
            var characters = new List<BitmapSource>();
            foreach (var (s, e) in segments)
            {
                var widthCrop = Math.Min(e - s, source.PixelWidth - s);
                if (widthCrop <= 0) continue;

                var cropped = new CroppedBitmap(source, new Int32Rect(s, 0, widthCrop, height));
                characters.Add(cropped);
            }

            return characters;
        }

        private BitmapSource ResizeTo28x28(BitmapSource src)
        {
            var group = new DrawingGroup();
            group.Children.Add(new ImageDrawing(src, new Rect(0, 0, 28, 28)));

            var drawingVisual = new DrawingVisual();
            using (var context = drawingVisual.RenderOpen())
                context.DrawDrawing(group);

            var bmp = new RenderTargetBitmap(28, 28, 96, 96, PixelFormats.Pbgra32);
            bmp.Render(drawingVisual);
            return bmp;
        }

        private float[] ConvertToFloatArray(BitmapSource bmp)
        {
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
            return pixels;
        }

        public string PredictFromOverlay(Rect overlayBounds, Canvas drawingCanvas)
        {
            var fullImage = RenderOverlayGeometry(overlayBounds, drawingCanvas);
            if (fullImage == null)
            {
                MessageBox.Show("Nimic de procesat.");
                return "";
            }

            // SEGMENTARE
            var characterImages = SplitCharacters(fullImage);
            if (characterImages.Count == 0)
            {
                MessageBox.Show("Nu s-au detectat caractere.");
                return "";
            }

            var predictor = new LetterPredictor("model.onnx");
            string result = "";

            int i = 0;
            foreach (var charBmp in characterImages)
            {
                var resized = ResizeTo28x28(charBmp);
                var pixels = ConvertToFloatArray(resized);
                var prediction = predictor.Predict(pixels);
                result += prediction.Label;

                SaveBitmapToFile(resized, $"char_{i}"); // doar pentru debug
                i++;
            }

            return result;

            //MessageBox.Show($"Text detectat: {result}", "Predicție", MessageBoxButton.OK);
        }

        private RenderTargetBitmap RenderOverlayGeometry(Rect bounds, Canvas canvas)
        {
            var geometryGroup = new GeometryGroup();

            foreach (UIElement child in canvas.Children)
            {
                if (child is Polyline line)
                {
                    var lineBounds = VisualTreeHelper.GetDescendantBounds(line);
                    if (bounds.Contains(lineBounds))
                        geometryGroup.Children.Add(line.RenderedGeometry.Clone());
                }
            }

            if (geometryGroup.Children.Count == 0)
                return null;

            var geoBounds = geometryGroup.Bounds;

            int width = (int)Math.Ceiling(geoBounds.Width + 10);  
            int height = (int)Math.Ceiling(geoBounds.Height + 10); 

            var visual = new DrawingVisual();
            using (var dc = visual.RenderOpen())
            {
                dc.DrawRectangle(Brushes.Black, null, new Rect(0, 0, width, height));

                var transform = new TransformGroup();
                transform.Children.Add(new TranslateTransform(-geoBounds.X + 5, -geoBounds.Y + 5)); 
                geometryGroup.Transform = transform;

                var pen = new Pen(Brushes.White, 2)
                {
                    LineJoin = PenLineJoin.Miter,
                    StartLineCap = PenLineCap.Flat,
                    EndLineCap = PenLineCap.Flat
                };
                dc.DrawGeometry(null, pen, geometryGroup);
            }

            var bmp = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            bmp.Render(visual);
            return bmp;
        }

        private void SaveBitmapToFile(BitmapSource bmp, string name)
        {
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bmp));

            var path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), name + ".png");
            using var fs = new FileStream(path, FileMode.Create);
            encoder.Save(fs);
        }
    }
}
