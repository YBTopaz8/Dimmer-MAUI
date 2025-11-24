namespace Dimmer.Utilities.TypeConverters;

public class EnumToIntConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if(value?.GetType() == typeof(Enum))
        {
            return (int)value;
        }

        return value;


    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TimeSpan timeSpan)
        {
            return timeSpan.TotalMilliseconds;
        }
        return 0;
    }

}
