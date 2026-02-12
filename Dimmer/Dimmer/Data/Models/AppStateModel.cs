



using Dimmer.UIUtils;

namespace Dimmer.Data.Models;

[Dimmer.Utils.Preserve(AllMembers = true)]
public partial class AppStateModel : RealmObject, IRealmObjectWithObjectId
{

    public bool IsNew { get; set; }
    [PrimaryKey]
    [MapTo("_id")]
    public ObjectId Id { get; set; }
    public string CurrentSongId { get; set; }
    public string? CurrentAlbumId { get; set; }
    public string? CurrentArtistId { get; set; }
    public string? CurrentGenreId { get; set; }
    public string? CurrentPlaylistId { get; set; }
    public string? CurrentUserId { get; set; }
    public string? CurrentTheme { get; set; }
    public string? CurrentLanguage { get; set; }
    public string? LastFMSessionKey { get; set; }
    public string? CurrentCountry { get; set; }
    public int RepeatModePreference { get; set; }
    public bool ShuffleStatePreference { get; set; }
    public int ShuffleModePreference { get; set; }
    public double VolumeLevelPreference { get; set; }
    public bool IsDarkModePreference { get; set; }
    public bool IsFirstTimeUser { get; set; }
    public double PlaybackSpeed { get; set; } = 1.0;
    // e.g. 0.5–2.0× speed
    public bool MinimizeToTrayPreference { get; set; }
    public bool IsStickToTop { get; set; }

    public string? EqualizerPreset { get; set; }
    public double LastKnownPosition { get; set; }
    public IList<UserMusicFoldersPreference> UserMusicFolders { get; }
    public IList<string> LastOpenedWindows { get; }
    public string LastKnownQuery { get; set; }
    public string LastKnownPlaybackQuery { get; set; }
    public int LastKnownPlaybackQueueIndex { get; set; }

    public IList<SongModel> LastKnownPlaybackQueue { get; }
    public bool LastKnownShuffleState { get; set; }
    public int CurrentRepeatMode { get; set; }
    public int LastKnownRepeatState { get; set; }
    public bool IsMiniLyricsViewEnabled { get; set; }
    public string PreferredMiniLyricsViewPosition { get; set; }
    public string PreferredLyricsSource { get; set; }
    public string AllowLyricsContribution { get; set; }
    public string PreferredLyricsFormat { get; internal set; }
    public bool AllowBackNavigationWithMouseFour { get; internal set; }
    public int AppTheme { get; internal set; }
    public string LastfmUsername { get; internal set; }
    public bool ScrobbleToLastFM { get; internal set; }
    public bool KeepScreenOnDuringLyrics { get; set; } = true;
    public bool OpenMediaUIOnNotificationTap { get; set; }

    public AppStateModel()
    {

    }
    public AppStateModel(AppStateModel source)
    {
        ArgumentNullException.ThrowIfNull(source);

        // Copy all properties
        // Note: We are explicitly setting Id from the source.
        // If you wanted a new ID for the copy, you'd omit this line or re-initialize.
        this.Id = source.Id;
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
        this.ShuffleModePreference = source.ShuffleModePreference;
        this.VolumeLevelPreference = source.VolumeLevelPreference;
        this.IsDarkModePreference = source.IsDarkModePreference;
        this.IsFirstTimeUser = source.IsFirstTimeUser;
        this.PlaybackSpeed = source.PlaybackSpeed;
        this.MinimizeToTrayPreference = source.MinimizeToTrayPreference;
        this.IsStickToTop = source.IsStickToTop;
        this.EqualizerPreset = source.EqualizerPreset;
        this.LastKnownPosition = source.LastKnownPosition;
        this.KeepScreenOnDuringLyrics = source.KeepScreenOnDuringLyrics;

        // For collections, we need to create new collections and copy the items
        // to ensure the new instance has its own independent lists.
        // Since UserMusicFoldersPreference and LastOpenedWindows are initialized
        // with `new List<string>()` by their property initializers,
        // `this.UserMusicFoldersPreference` and `this.LastOpenedWindows`
        // are already new, empty List<string> instances here. We just add to them.

        if (source.UserMusicFolders != null)
        {
            foreach (var item in source.UserMusicFolders)
            {
                this.UserMusicFolders.Add(item);
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

public class UserMusicFoldersPreference :EmbeddedObject
{
    public string ReadableFolderPath { get; set; }
    public string SystemFolderPath { get; set; }
    public bool IsBlackListed{ get; set; }
    public UserMusicFoldersPreference()
    {
        
    }

}