



namespace Dimmer.Utilities.TypeConverters;
public class CountToVisibilityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || parameter is null)
        {
            return false;
        }
        string param = (string)parameter;
        LyricsDownloadContent? val = (LyricsDownloadContent)value;

        if (param == "syncview")
        {
            if (val?.SyncedLyrics?.Length > 0)
            {
                return true;
            }
            return false;
        }
        if (param == "unsyncview")
        {
            if (val?.PlainLyrics?.Length > 0)
            {
                return true;
            }
            return false;
        }
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}



public class IsNotNullOrEmptyConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is string str && !string.IsNullOrEmpty(str);
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}