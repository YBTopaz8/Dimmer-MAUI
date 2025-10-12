namespace Dimmer.Utilities.TypeConverters;
public class IsNotNullConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value != null;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // This is not needed for one-way bindings.
        throw new NotImplementedException();
    }
}