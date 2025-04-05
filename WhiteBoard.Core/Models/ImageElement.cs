using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows;
using System.Windows.Controls;

namespace WhiteBoard.Core.Models
{
    public class ImageElement : WhiteBoardElement
    {
        private readonly Image _image;

        public BitmapImage Image { get; }

        public ImageElement(BitmapImage image, Rect bounds)
        {
            Image = image;

            _image = new Image
            {
                Source = image,
                Width = bounds.Width,
                Height = bounds.Height
            };

            Canvas.SetLeft(_image, bounds.X);
            Canvas.SetTop(_image, bounds.Y);
        }

        public override UIElement Visual => _image;

        public override Rect Bounds => new Rect(
            Canvas.GetLeft(_image),
            Canvas.GetTop(_image),
            _image.Width,
            _image.Height);
    }
}
