using System;
using Microsoft.UI.Xaml.Data;

namespace Dimmer.WinUI.Utils.Converters;

/// <summary>
/// Converts song title and IsFileExists to display title with [Unavailable] prefix
/// </summary>
public partial class TitleWithAvailabilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (parameter is bool isFileExists && !isFileExists && value is string title)
        {
            return $"[Unavailable] {title}";
        }
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
