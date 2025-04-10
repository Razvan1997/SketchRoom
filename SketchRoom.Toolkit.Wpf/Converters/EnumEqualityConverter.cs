using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace SketchRoom.Toolkit.Wpf.Converters
{
    public class EnumEqualityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.ToString() == parameter?.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isChecked = (bool)value;

            if (!isChecked)
            {
                return Enum.Parse(targetType, "None"); // dezactivare
            }

            return Enum.Parse(targetType, parameter.ToString());
        }
    }
}
