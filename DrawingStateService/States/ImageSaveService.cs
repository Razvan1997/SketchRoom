using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace DrawingStateService.States
{
    public class ImageSaveService
    {
        private static int _imageIndex = 0;

        public void SaveTrainingImage(RenderTargetBitmap bmp, string label)
        {
            var folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dataset", label);
            Directory.CreateDirectory(folderPath);

            var filePath = Path.Combine(folderPath, $"{_imageIndex}.png");

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bmp));
            using var fs = new FileStream(filePath, FileMode.Create);
            encoder.Save(fs);

            _imageIndex++;
        }
    }
}
