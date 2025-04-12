
namespace Dimmer.Utilities.TypeConverters;
public class BoolToInverseConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null)
        {
            return false;
        }
        bool val = (bool)value;
        return !val;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {

        Debug.WriteLine(message: "None");
        return null;
    }
}

public class BoolToYesNoConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null)
        {
            return "No";
        }
        bool val = (bool)value;
        if (val)
        {
            return "Yes";
        }
        else
        {
            return "No";
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        Debug.WriteLine(message: "None");
        return null;
    }
}
