using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.WinUI.Utils.Converters;

public class TimeSpanToSecondsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is TimeSpan ts)
        {
            // If parameter is 'format', return string for TextBlock
            if (parameter is string param && param == "format")
            {
                return ts.ToString(@"mm\:ss");
            }
            // Otherwise return TotalSeconds for Slider
            return ts.TotalSeconds;
        }
        return 0.0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is double seconds)
        {
            return TimeSpan.FromSeconds(seconds);
        }
        return TimeSpan.Zero;
    }
}