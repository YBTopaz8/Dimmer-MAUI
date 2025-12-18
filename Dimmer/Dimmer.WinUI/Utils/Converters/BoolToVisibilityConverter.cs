namespace Dimmer.WinUI.Utils.Converters;

public partial class BoolToVisibilityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, string culture)
    {
        if (value is bool boolVal)
        {
            if (boolVal)
            {
                return WinUIVisibility.Visible;
            }
            else
            {
                return WinUIVisibility.Collapsed;
            }
        }
        return WinUIVisibility.Collapsed;
    }
    public object? ConvertBack(object? value, Type targetType, object? parameter, string culture)
    {
        if (value is WinUIVisibility visibility)
        {
            return visibility == WinUIVisibility.Visible;
        }
        return false;
    }
}