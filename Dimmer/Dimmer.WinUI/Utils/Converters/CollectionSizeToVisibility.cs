using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.WinUI.Utils.Converters;

public partial class CollectionSizeToVisibility : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, string culture)
    {
        int? val = (int?)value;
        if (val < 1)
        {
            return false;
        }
        return true;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, string culture)
    {
        throw new NotImplementedException();
    }
}

