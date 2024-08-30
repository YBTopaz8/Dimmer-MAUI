namespace Dimmer.Utilities.TypeConverters;
public class PathToImageConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null) return string.Empty;
        
        var path = LyricsService.SaveOrGetCoverImageToFilePath((string)value);

        return path;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
