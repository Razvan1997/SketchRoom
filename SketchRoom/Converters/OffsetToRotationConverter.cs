using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace SketchRoom.Converters
{
    public class OffsetToRotationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double offset)
            {
                return offset switch
                {
                    < 5 => -2.5,
                    < 15 => -1,
                    < 30 => 0.5,
                    < 50 => 1.5,
                    _ => 2
                };
            }

            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }
}
