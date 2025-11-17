namespace Dimmer.WinUI.Utils.Converters;

public partial class BoolToImageConverter : IValueConverter
{
    public object TrueValue { get; set; }
    public object FalseValue { get; set; }

    public object? Convert(object? value, Type targetType, object? parameter, string culture)
    {
        var imgSourceStr = parameter as string;
        if (string.IsNullOrEmpty(imgSourceStr)) return null;
        var ImgSourceUri = new Uri(imgSourceStr);
        
        var imgSource = new Microsoft.UI.Xaml.Media.Imaging.SvgImageSource(ImgSourceUri);
        
        return value is bool b && b ? imgSource : null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, string culture)
    {
        throw new NotImplementedException();
    }
}