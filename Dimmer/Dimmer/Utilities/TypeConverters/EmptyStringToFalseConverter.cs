namespace Dimmer.Utilities.TypeConverters;

/// <summary>
/// Converts an empty or null string to false, and a non-empty string to true.
/// Useful for visibility bindings where you want to hide an element when text is empty.
/// </summary>
public class EmptyStringToFalseConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string stringValue)
        {
            return !string.IsNullOrWhiteSpace(stringValue);
        }
        
        return false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}
