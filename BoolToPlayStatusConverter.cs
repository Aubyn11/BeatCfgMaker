using System;
using System.Globalization;
using System.Windows.Data;

namespace BeatCfgMaker
{
    public class BoolToPlayStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isPlaying)
            {
                return isPlaying ? "正在播放..." : "已暂停";
            }
            return "未播放";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}