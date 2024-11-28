namespace Dimmer_MAUI.Utilities.TypeConverters;
public class IndexToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int currentIndex && parameter is string viewIndexString && int.TryParse(viewIndexString, out int viewIndex))
        {
            return currentIndex == viewIndex;
        }
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
