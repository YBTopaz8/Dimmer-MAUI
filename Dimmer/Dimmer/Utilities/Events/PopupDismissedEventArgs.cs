namespace Dimmer.Utilities.Events;

public class PopupDismissedEventArgs : EventArgs
{
    public bool HasActionAfterDismissed { get; set; }
    public PopupDismissedActionEnums DismissedActionDescription { get; set; }
}

public enum PopupDismissedActionEnums
{
   None,
   GoToSingleSongDetails,
   GoToArtistPage,
   GoToDetailsPage,
   GoToStatsPage,
   GoToSettingsPage,
   GoToLyricsPage,
   GoToAlbumPage
}