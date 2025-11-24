namespace Dimmer.WinUI.Utils.Converters;

public partial class BoolToInverseVisibilityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, string culture)
    {
        if (value is bool boolVal)
        {
            return boolVal ? WinUIVisibility.Collapsed : WinUIVisibility.Visible;
        }
        return WinUIVisibility.Visible;
    }
    public object? ConvertBack(object? value, Type targetType, object? parameter, string culture)
    {
        if (value is WinUIVisibility visibility)
        {
            return visibility == WinUIVisibility.Collapsed;
        }
        return false;
    }
}