using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.WinUI.Utils.Converters;


public partial class BoolToPlayingPauseConv : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, string culture)
    {
        if (value is bool boolVal)
        {
            return boolVal ? "Playing" : "Paused";
        }
        return "Paused";
    }
    public object? ConvertBack(object? value, Type targetType, object? parameter, string culture)
    {
        return "Paused";
    }
}