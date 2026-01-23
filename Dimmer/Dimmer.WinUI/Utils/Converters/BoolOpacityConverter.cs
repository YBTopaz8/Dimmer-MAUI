using Microsoft.UI.Xaml.Data;

namespace Dimmer.WinUI.Utils.Converters;

public class BoolOpacityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool boolValue)
        {
            return boolValue ? 1.0 : 0.4;
        }
        return 0.4;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
