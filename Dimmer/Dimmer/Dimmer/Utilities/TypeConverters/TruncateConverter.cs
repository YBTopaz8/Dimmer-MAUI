using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Utilities.TypeConverters;
public class TruncateConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string text)
        {
            int maxLength = 200; // Default max length
            if (parameter is int paramLength && paramLength > 0)
            {
                maxLength = paramLength;
            }

            if (text.Length > maxLength)
            {
                return text.Substring(0, maxLength) + "...";
            }
        }
        return value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}