using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.WinUI.Utils.Converters;

public partial class IsNullToVisibilityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, string culture)
    {
        var param = value as string;
        if
            (param != null && param == "inv")
        {
            if (value is null)
            {
                return WinUIVisibility.Collapsed;
            }
            else
            {
                return WinUIVisibility.Visible; 
            }

        }
        if (value is null)
        {
            return WinUIVisibility.Collapsed;
        }
        return WinUIVisibility.Visible;
    }
    public object? ConvertBack(object? value, Type targetType, object? parameter, string culture)
    {
        if (value is WinUIVisibility visibility)
        {
            return visibility == WinUIVisibility.Visible;
        }
        return false;
    }
}