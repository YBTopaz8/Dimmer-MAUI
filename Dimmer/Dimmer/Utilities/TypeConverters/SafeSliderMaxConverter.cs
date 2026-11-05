namespace Dimmer.Utilities.TypeConverters;

public class SafeSliderMaxConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double d && d > 0)
            return d;
        if (value is int i && i > 0)
            return (double)i;

        // Fallback to 1.0 so Maximum (1.0) is always strictly greater than Minimum (0.0)
        return 1.0;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}