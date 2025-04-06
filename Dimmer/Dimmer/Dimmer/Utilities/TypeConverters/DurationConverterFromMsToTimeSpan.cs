using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Utilities.TypeConverters;
public class DurationConverterFromMsToTimeSpan : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double duration)
        {
            TimeSpan time = TimeSpan.FromSeconds(duration);
            string timeString = time.ToString(@"mm\:ss");
            return timeString;
        }
        return "00:00";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TimeSpan timeSpan)
        {
            return timeSpan.TotalMilliseconds;
        }
        return 0;
    }

}

