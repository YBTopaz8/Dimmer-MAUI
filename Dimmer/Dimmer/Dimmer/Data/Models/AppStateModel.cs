namespace Dimmer.Data.Models;
public class AppStateModel : RealmObject
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

    public string? EqualizerPreset { get; set; }
    public double LastKnownPosition { get; set; }
    public IList<string> UserMusicFoldersPreference { get; } = new List<string>();
    public IList<string> LastOpenedWindows { get; } = new List<string>();


    public AppStateModel()
    {
        
    }
}
