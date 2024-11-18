namespace Dimmer_MAUI.Utilities.TypeConverters;
public class DateTimeOffsetToLocalDateConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DateTimeOffset dateTimeOffset)
        {
            
            return dateTimeOffset.ToLocalTime().ToString("f");
        }
        return null; 
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
public class DateTimeToLocalTimeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DateTimeOffset dateTimeOffset)
        {
            // Convert DateTimeOffset to local DateTime
            return dateTimeOffset.ToLocalTime().ToString("h:mm:ss tt");
        }
        return null; // Or you could return Binding.DoNothing;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
public class DateTimeToLocalDateConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DateTimeOffset dateTimeOffset)
        {
            // Convert DateTimeOffset to local DateTime
            return dateTimeOffset.ToLocalTime().ToString("f");
        }
        return null; // Or you could return Binding.DoNothing;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}