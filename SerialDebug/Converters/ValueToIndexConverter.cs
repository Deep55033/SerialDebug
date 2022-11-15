using System;
using System.Globalization;
using System.Windows.Data;

namespace SerialDebug.Converters
{
    internal class ValueToIndexConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (int)value - 1 < 0 ? 0 : (int)value - 1;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (int)value + 1;
        }
    }
}