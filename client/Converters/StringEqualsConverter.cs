using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace client.Converters
{
    public class StringEqualsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value?.ToString() == parameter?.ToString();

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => (bool)value ? parameter?.ToString() : Binding.DoNothing;
    }
}