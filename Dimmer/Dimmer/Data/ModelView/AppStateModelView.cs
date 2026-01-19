namespace Dimmer.Data.ModelView;

[Utils.Preserve(AllMembers = true)]
public partial class AppStateModelView: ObservableObject
{

    [PrimaryKey]
    public ObjectId Id { get; set; }
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

    public string LastKnownQuery { get;  set; }
    public string LastKnownPlaybackQuery { get;  set; }
    public int LastKnownPlaybackQueueIndex { get;  set; }
    public List<SongModelView> LastKnownPlaybackQueue { get;  set; }
    public bool LastKnownShuffleState { get;  set; }
    public int CurrentRepeatMode { get;  set; }
    public bool IsMiniLyricsViewEnabled { get; set; }
    public string PreferredMiniLyricsViewPosition { get; set; }
    public string PreferredLyricsSource { get; set; }
    public string AllowLyricsContribution { get; set; }
    public bool AllowBackNavigationWithMouseFour { get;  set; }

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
        // Note: We are explicitly setting Id from the source.
        // If you wanted a new ID for the copy, you'd omit this line or re-initialize.
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

       
    }
}
