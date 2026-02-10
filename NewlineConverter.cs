using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows;

namespace BeatCfgMaker
{
    public class NewlineConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string text)
            {
                // 将字符串中的换行符转换为XAML可识别的格式
                return text.Replace("\\n", Environment.NewLine);
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string text)
            {
                // 将XAML中的换行符转换回字符串格式
                return text.Replace(Environment.NewLine, "\\n");
            }
            return value;
        }
    }
}