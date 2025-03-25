namespace Dimmer_MAUI.Utilities.TypeConverters;
public class CountToVisibilityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        string param = (string)parameter;
        Content? val = (Content)value;

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
