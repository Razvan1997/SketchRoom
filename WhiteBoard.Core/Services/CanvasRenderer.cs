using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using WhiteBoard.Core.Models;
using WhiteBoard.Core.Services.Interfaces;

namespace WhiteBoard.Core.Services
{
    public class CanvasRenderer : ICanvasRenderer
    {
        private const string RemoteCursorTag = "RemoteCursor";

        public void RenderElement(Canvas canvas, WhiteBoardElement element)
        {
            canvas.Children.Add(element.Visual);
        }

        public void Clear(Canvas canvas)
        {
            canvas.Children.Clear();
        }

        public void RenderRemoteCursor(Canvas canvas, Point position, BitmapImage? image)
        {
            // Caută dacă există deja un cursor remote
            var existing = canvas.Children
                .OfType<Image>()
                .FirstOrDefault(img => img.Tag?.ToString() == RemoteCursorTag);

            if (existing != null)
            {
                Canvas.SetLeft(existing, position.X);
                Canvas.SetTop(existing, position.Y);
                if (image != null)
                    existing.Source = image;
                return;
            }

            // Dacă nu există, creează unul nou
            var remoteCursor = new Image
            {
                Width = 24,
                Height = 24,
                Source = image,
                Tag = RemoteCursorTag,
                IsHitTestVisible = false
            };

            Canvas.SetLeft(remoteCursor, position.X);
            Canvas.SetTop(remoteCursor, position.Y);

            canvas.Children.Add(remoteCursor);
        }

        public bool HasVisual(Canvas canvas, UIElement visual)
        {
            return canvas.Children.Contains(visual);
        }
    }
}
