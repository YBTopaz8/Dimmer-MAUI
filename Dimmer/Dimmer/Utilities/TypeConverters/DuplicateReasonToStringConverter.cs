namespace Dimmer.Utilities.TypeConverters;

public class DuplicateReasonToStringConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DuplicateReason reasons)
        {
            // This will create a comma-separated list like "LowerBitrate, DifferentFolder"
            return reasons.ToString();
        }
        return string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
