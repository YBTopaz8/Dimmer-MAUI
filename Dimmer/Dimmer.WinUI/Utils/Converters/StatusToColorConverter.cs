using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace Dimmer.WinUI.Utils.Converters;

public class StatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is string status)
        {
            return status.ToLower() switch
            {
                "open" => new SolidColorBrush(Color.FromArgb(255, 59, 130, 246)), // Blue
                "planned" => new SolidColorBrush(Color.FromArgb(255, 168, 85, 247)), // Purple
                "in-progress" => new SolidColorBrush(Color.FromArgb(255, 251, 146, 60)), // Orange
                "shipped" => new SolidColorBrush(Color.FromArgb(255, 34, 197, 94)), // Green
                "rejected" => new SolidColorBrush(Color.FromArgb(255, 239, 68, 68)), // Red
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
