


namespace Dimmer.Utilities.TypeConverters;
public class BoolToImageConverter : IValueConverter
{
    public object TrueValue { get; set; }
    public object FalseValue { get; set; }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    => value is bool b && b ? TrueValue : FalseValue;
    

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
