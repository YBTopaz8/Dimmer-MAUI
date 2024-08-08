using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Utilities.TypeConverters;
public class EmptyStringToMessageConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null)
        {
            return "No Results";
        }
        if(value?.GetType() == typeof(string))
        {
            if (string.IsNullOrEmpty((string)value))
            {
                return "No Results";
            }
            return value;

        }

        return "None";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
