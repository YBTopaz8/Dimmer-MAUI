using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace Dimmer.WinUI.Utils.Converters;

public class TypeToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is string type)
        {
            return type.ToLower() switch
            {
                "bug" => new SolidColorBrush(Color.FromArgb(255, 239, 68, 68)), // Red
                "feature" => new SolidColorBrush(Color.FromArgb(255, 34, 197, 94)), // Green
                _ => new SolidColorBrush(Color.FromArgb(255, 107, 114, 128)) // Gray
            };
        }
        return new SolidColorBrush(Color.FromArgb(255, 107, 114, 128));
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
