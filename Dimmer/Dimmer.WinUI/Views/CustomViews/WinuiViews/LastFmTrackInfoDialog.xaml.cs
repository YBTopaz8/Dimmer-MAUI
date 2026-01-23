using Hqub.Lastfm.Entities;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Dimmer.WinUI.Views.CustomViews.WinuiViews;

public sealed partial class LastFmTrackInfoDialog : ContentDialog
{
    public LastFmTrackInfoDialog()
    {
        InitializeComponent();
    }

    public void SetTrackInfo(Track track)
    {
        if (track == null)
            return;

        // Set Title
        TitleText.Text = track.Name ?? "Unknown Track";

        // Set Artist
        ArtistText.Text = track.Artist?.Name ?? "Unknown Artist";

        // Set Album
        AlbumText.Text = track.Album?.Name ?? string.Empty;
        if (string.IsNullOrEmpty(AlbumText.Text))
            AlbumText.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;

        // Set Cover Art
        if (track.Images != null && track.Images.Count > 0)
        {
            // Try to get the largest image (usually last in list)
            var imageUrl = track.Images.LastOrDefault()?.Url;
            if (!string.IsNullOrEmpty(imageUrl) && Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri))
            {
                try
                {
                    CoverArtImage.Source = new BitmapImage(uri);
                }
                catch (Exception)
                {
                    // Silent failure is acceptable - if image cannot be loaded,
                    // the image control will remain empty which is preferable to
                    // showing an error to the user for a non-critical UI element
                }
            }
        }

        // Set Loved Status
        if (track.UserLoved)
        {
            LovedPanel.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
        }

        // Set Now Playing Status
        if (track.NowPlaying)
        {
            NowPlayingPanel.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
        }

        // Set Statistics
        if (track.Statistics != null)
        {
            if (track.Statistics.Listeners > 0)
                ListenersText.Text = track.Statistics.Listeners.ToString("N0");

            if (track.Statistics.PlayCount > 0)
                PlayCountText.Text = track.Statistics.PlayCount.ToString("N0");
        }
    }
}
