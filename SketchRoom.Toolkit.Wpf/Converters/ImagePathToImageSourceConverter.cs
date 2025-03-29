using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace SketchRoom.Toolkit.Wpf.Converters
{
    public class ImagePathToImageSourceConverter : IValueConverter
    {
        private static readonly string DefaultImagePath =
            "pack://application:,,,/SketchRoom.Toolkit.Wpf;component/Resources/unknown.png";

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var path = value as string;
            try
            {
                if (!string.IsNullOrWhiteSpace(path))
                    return new BitmapImage(new Uri(path, UriKind.RelativeOrAbsolute));
            }
            catch
            {
                // fallback
            }

            return new BitmapImage(new Uri(DefaultImagePath, UriKind.RelativeOrAbsolute));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
