using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Data.ModelView;
public partial class AppStateModelView: ObservableObject
{

    [PrimaryKey]
    public string LocalDeviceId { get; set; } = Guid.NewGuid().ToString();
    public string CurrentSongId { get; set; }
    public string? CurrentAlbumId { get; set; }
    public string? CurrentArtistId { get; set; }
    public string? CurrentGenreId { get; set; }
    public string? CurrentPlaylistId { get; set; }
    public string? CurrentUserId { get; set; }
    public string? CurrentTheme { get; set; }
    public string? CurrentLanguage { get; set; }
    public string? CurrentCountry { get; set; }
    public int RepeatModePreference { get; set; }
    public bool ShuffleStatePreference { get; set; }
    public double VolumeLevelPreference { get; set; }
    public bool IsDarkModePreference { get; set; }
    public bool IsFirstTimeUser { get; set; }
    public double PlaybackSpeed { get; set; } = 1.0;
    // e.g. 0.5–2.0× speed
    public bool MinimizeToTrayPreference { get; set; }
    public bool IsStickToTop { get; set; }
    public bool IsShowCloseConfirmation { get; set; }

    public string? EqualizerPreset { get; set; }
    public double LastKnownPosition { get; set; }
    public IList<string> UserMusicFoldersPreference { get; } = new List<string>();
    public IList<string> LastOpenedWindows { get; } = new List<string>();


    public AppStateModelView()
    {

    }
    public AppStateModelView(AppStateModel source)
    {
        if (source == null)
        {
            // Or handle this differently, e.g., initialize with defaults,
            // but typically copying from null is an error.
            // The default property initializers will still apply if you don't throw.
            throw new ArgumentNullException(nameof(source));
        }

        // Copy all properties
        // Note: We are explicitly setting LocalDeviceId from the source.
        // If you wanted a new ID for the copy, you'd omit this line or re-initialize.
        this.LocalDeviceId = source.LocalDeviceId;
        this.CurrentSongId = source.CurrentSongId;
        this.CurrentAlbumId = source.CurrentAlbumId;
        this.CurrentArtistId = source.CurrentArtistId;
        this.CurrentGenreId = source.CurrentGenreId;
        this.CurrentPlaylistId = source.CurrentPlaylistId;
        this.CurrentUserId = source.CurrentUserId;
        this.CurrentTheme = source.CurrentTheme;
        this.CurrentLanguage = source.CurrentLanguage;
        this.CurrentCountry = source.CurrentCountry;
        this.RepeatModePreference = source.RepeatModePreference;
        this.ShuffleStatePreference = source.ShuffleStatePreference;
        this.VolumeLevelPreference = source.VolumeLevelPreference;
        this.IsDarkModePreference = source.IsDarkModePreference;
        this.IsFirstTimeUser = source.IsFirstTimeUser;
        this.PlaybackSpeed = source.PlaybackSpeed;
        this.MinimizeToTrayPreference = source.MinimizeToTrayPreference;
        this.IsStickToTop = source.IsStickToTop;
        this.EqualizerPreset = source.EqualizerPreset;
        this.LastKnownPosition = source.LastKnownPosition;

        // For collections, we need to create new collections and copy the items
        // to ensure the new instance has its own independent lists.
        // Since UserMusicFoldersPreference and LastOpenedWindows are initialized
        // with `new List<string>()` by their property initializers,
        // `this.UserMusicFoldersPreference` and `this.LastOpenedWindows`
        // are already new, empty List<string> instances here. We just add to them.

        if (source.UserMusicFoldersPreference != null)
        {
            foreach (var item in source.UserMusicFoldersPreference)
            {
                this.UserMusicFoldersPreference.Add(item);
            }
            // Alternative using LINQ (if you prefer, but foreach is clear):
            // this.UserMusicFoldersPreference.Clear(); // Should be empty already
            // ((List<string>)this.UserMusicFoldersPreference).AddRange(source.UserMusicFoldersPreference);
        }

        if (source.LastOpenedWindows != null)
        {
            foreach (var item in source.LastOpenedWindows)
            {
                this.LastOpenedWindows.Add(item);
            }
            // Alternative using LINQ:
            // this.LastOpenedWindows.Clear(); // Should be empty already
            // ((List<string>)this.LastOpenedWindows).AddRange(source.LastOpenedWindows);
        }
    }
}
