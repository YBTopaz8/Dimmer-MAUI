
namespace Dimmer.Utilities.TypeConverters;
public class BytesToMegabytesConverter : IValueConverter
{

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null)
        {
            return "0 MB"; // Default case if value is null
        }
        if (value is long vl)
        {
            return (vl / 1024.0 / 1024.0).ToString("0.##") + " MB";
        }
        else if (value is double vd)
        {
            return (vd / 1024.0 / 1024.0).ToString("0.##") + " MB";
        }
        return "0 MB"; // Default case if conversion fails
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
