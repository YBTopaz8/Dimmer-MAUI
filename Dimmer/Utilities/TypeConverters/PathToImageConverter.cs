﻿namespace Dimmer_MAUI.Utilities.TypeConverters;
public class PathToImageConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null)
            return "musicnoteslider.png";

        var path = LyricsService.SaveOrGetCoverImageToFilePath((string)value);
        if (string.IsNullOrEmpty(path))
        {
            path = "musicnoteslider.png";
        }
        return path;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
public class VolumeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null)
            return "0";

        return ((double)value*100).ToString("N1")+"%";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
