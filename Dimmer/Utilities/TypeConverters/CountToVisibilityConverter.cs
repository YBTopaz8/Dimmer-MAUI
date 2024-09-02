using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer_MAUI.Utilities.TypeConverters;
public class CountToVisibilityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        string param = (string)parameter;
        var val = (Content)value;

        if (param == "syncview")
        {
            if (val?.syncedLyrics?.Length > 0)
            {
                return true;
            }
            return false;
        }
        if (param == "unsyncview")
        {
            if (val?.plainLyrics?.Length > 0)
            {
                return true;
            }
            return false;
        }
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
