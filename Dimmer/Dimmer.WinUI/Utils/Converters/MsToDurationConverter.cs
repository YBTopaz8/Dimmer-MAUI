using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Visibility = Microsoft.UI.Xaml.Visibility;

namespace Dimmer.WinUI.Utils.Converters;

public partial class MsToDurationConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        // Last.fm returns duration in milliseconds (int)
        if (value is int ms)
        {
            if (ms == 0) return "Playing"; // Or "" depending on preference

            var ts = TimeSpan.FromMilliseconds(ms);
            return $"{ts.Minutes}:{ts.Seconds:D2}";
        }
        return "0:00";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

// 2. Extracts the largest image URL from the List<Image>
public partial class TrackImageConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        string? url = null;

        // Logic to extract the URL
        if (value is List<Hqub.Lastfm.Entities.Image> images && images.Count > 0)
        {
            // Get the largest image available
            url = images.LastOrDefault()?.Url;
        }

        // Fallback if URL is missing
        if (string.IsNullOrEmpty(url))
        {
            url = "ms-appx:///Assets/music_note_placeholder.png";
        }

        try
        {
            return new BitmapImage(new Uri(url));
        }
        catch
        {
            return new BitmapImage(new Uri("ms-appx:///Assets/music_note_placeholder.png"));
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

// 3. Boolean to Visibility
public partial class BoolToVisConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool b) return b ? Visibility.Visible : Visibility.Collapsed;
        return Visibility.Collapsed;
    }
    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}

// 4. Boolean to Heart Color (Red if true, Transparent/Gray if false)
public partial class BoolToHeartColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool b && b) return new SolidColorBrush(Microsoft.UI.Colors.Red);
        return new SolidColorBrush(Microsoft.UI.Colors.Gray);
    }
    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}