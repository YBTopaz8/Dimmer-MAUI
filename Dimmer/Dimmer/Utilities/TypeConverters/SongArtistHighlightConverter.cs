namespace Dimmer.Utilities.TypeConverters;

public class SongArtistHighlightConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {

        // Parameter will be "Opacity" or "FontWeight"
        bool isByArtist = false;

        if (value is SongModelView song)
        {
            // You can check either the bool flag we set in Step 2, or check directly:
            isByArtist = song.IsBySelectedArtist;
        }

        string mode = parameter as string ?? "Opacity";

        if (mode == "Opacity")
        {
            // 1.0 for tracks by the artist, 0.4 for other tracks in the album
            return isByArtist ? 1.0 : 0.4;
        }
        else if (mode == "FontWeight")
        {
            return isByArtist ? FontWeights.SemiBold : FontWeight.Regular;
        }

        return 1.0;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // This is not needed for one-way bindings.
        throw new NotImplementedException();
    }
}
