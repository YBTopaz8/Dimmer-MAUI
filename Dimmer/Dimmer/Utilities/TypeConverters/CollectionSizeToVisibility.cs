namespace Dimmer.Utilities.TypeConverters;

public class CollectionSizeToVisibility : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {

        
        int val = (int)value;
        if(val < 1)
        {
            return false;
        }
        return true;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
