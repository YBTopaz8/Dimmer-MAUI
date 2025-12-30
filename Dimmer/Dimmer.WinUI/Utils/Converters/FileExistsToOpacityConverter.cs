using System;

namespace Dimmer.WinUI.Utils.Converters;

/// <summary>
/// Converts IsFileExists boolean to opacity value for visual indication
/// </summary>
public partial class FileExistsToOpacityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool isFileExists)
        {
            // Return lower opacity (0.5) for unavailable songs
            return isFileExists ? 1.0 : 0.5;
        }
        return 1.0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
