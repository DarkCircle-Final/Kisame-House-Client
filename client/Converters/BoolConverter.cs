using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace client.Converters
{
    /// <summary>
    /// 값이 null 이거나 빈 문자열이면 False,
    /// 그 외에는 True 를 반환하는 컨버터
    /// </summary>
    public class BoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return false;

            if (value is string s && string.IsNullOrWhiteSpace(s))
                return false;

            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}