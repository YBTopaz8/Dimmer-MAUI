namespace Dimmer.WinUI.Utils.Converters;

public partial class CollectionSizeToVisibility : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, string culture)
    {
        int? val = (int?)value;
        if (val < 1)
        {
            

            return WinUIVisibility.Collapsed;
        }
        return WinUIVisibility.Visible;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, string culture)
    {
        throw new NotImplementedException();
    }
}



public partial class CollectionSizeToVisibilityInverted : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, string culture)
    {
        int? val = (int?)value;
        if (val < 1)
        {
            

            return WinUIVisibility.Visible;
        }
        return WinUIVisibility.Collapsed;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, string culture)
    {
        throw new NotImplementedException();
    }
}


