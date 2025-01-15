namespace Dimmer_MAUI.Utilities.TypeConverters;
public class BoolToImageConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var vm = IPlatformApplication.Current!.Services.GetService<HomePageVM>();
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
