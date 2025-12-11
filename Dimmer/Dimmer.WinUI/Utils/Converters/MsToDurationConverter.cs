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
        string? url = null;

        // 1. Extract URL from Last.fm Image List
        if (value is List<Hqub.Lastfm.Entities.Image> images && images.Count > 0)
        {
            // Last.fm returns images in size order: small, medium, large, extralarge.
            // Taking the last one usually gives the best quality.
            url = images.LastOrDefault()?.Url;
        }

        // 2. Handle missing URL (Placeholder)
        if (string.IsNullOrEmpty(url))
        {
            // Ensure this path matches an actual image in your project assets
            url = "ms-appx:///Assets/music_note_placeholder.png";
        }

        // 3. Return BitmapImage object (Fixes InvalidCastException)
        try
        {
            return new BitmapImage(new Uri(url));
        }
        catch (Exception)
        {
            // Fallback if Uri is malformed
            return new BitmapImage(new Uri("ms-appx:///Assets/music_note_placeholder.png"));
        }
    }
    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}

// 2. Extracts the largest image URL from the List<Image>
public partial class TrackImageConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is List<Hqub.Lastfm.Entities.Image> images && images.Count > 0)
        {
            // Last.fm usually returns small, medium, large, extralarge. We take the last one.
            var url = images.LastOrDefault()?.Url;
            if (!string.IsNullOrEmpty(url)) return url;
        }
        // Return a placeholder image from your assets
        return "ms-appx:///Assets/music_note_placeholder.png";
    }
    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
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