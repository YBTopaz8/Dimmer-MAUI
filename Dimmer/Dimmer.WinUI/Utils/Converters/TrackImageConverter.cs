using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.WinUI.Utils.Converters;


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