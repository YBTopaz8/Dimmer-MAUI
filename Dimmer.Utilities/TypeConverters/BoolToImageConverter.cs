namespace Dimmer.Utilities.TypeConverters;
public class BoolToImageConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool MyBoolValue && MyBoolValue is true)
        {
            return MaterialCommunity.Heart;
        }
        else
        {
            return MaterialCommunity.HeartOutline;
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
