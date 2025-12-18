using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.WinUI.Utils.Converters;


public partial class StringNullOrEmptyToVisibility : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, string culture)
    {
        string? val = (string?)value;

        if (string.IsNullOrEmpty(val))
        {
            return WinUIVisibility.Collapsed;
        }
        return WinUIVisibility.Visible;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, string culture)
    {
        throw new NotImplementedException();
    }
}