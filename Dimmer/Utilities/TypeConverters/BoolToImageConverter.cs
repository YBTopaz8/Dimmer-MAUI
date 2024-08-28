namespace Dimmer.Utilities.TypeConverters;
public class BoolToImageConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool MyBoolValue && MyBoolValue is true)
        {
            return MaterialTwoTone.Favorite;
        }
        else
        {
            return MaterialTwoTone.Favorite_border;
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
