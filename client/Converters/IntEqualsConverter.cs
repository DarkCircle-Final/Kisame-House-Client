using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace client.Converters
{
    // int 선택값 <-> 라디오버튼 변환
    public class IntEqualsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int i && int.TryParse(parameter?.ToString(), out var p))
                return i == p;
            return false;
        }

        // 체크된 라디오 값만 반영, 체크해제 무시
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b && b && int.TryParse(parameter?.ToString(), out var p))
                return p;
            return BindableProperty.UnsetValue;
        }

    }
}
