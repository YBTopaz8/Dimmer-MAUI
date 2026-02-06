using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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