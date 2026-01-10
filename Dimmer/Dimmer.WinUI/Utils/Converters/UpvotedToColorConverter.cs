using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace Dimmer.WinUI.Utils.Converters;

public class UpvotedToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool hasUpvoted && hasUpvoted)
        {
            return new SolidColorBrush(Color.FromArgb(255, 59, 130, 246)); // Blue when upvoted
        }
        return new SolidColorBrush(Color.FromArgb(255, 107, 114, 128)); // Gray when not upvoted
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
