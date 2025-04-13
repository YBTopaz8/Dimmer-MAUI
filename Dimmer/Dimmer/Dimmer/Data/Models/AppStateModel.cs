namespace Dimmer.Data.Models;
public class AppStateModel : RealmObject
{

    [PrimaryKey]
    public string LocalDeviceId { get; set; } = Guid.NewGuid().ToString();
    public string? CurrentSongId { get; set; }
    public string? CurrentAlbumId { get; set; }
    public string? CurrentArtistId { get; set; }
    public string? CurrentGenreId { get; set; }
    public string? CurrentPlaylistId { get; set; }
    public string? CurrentUserId { get; set; }
    public string? CurrentTheme { get; set; }
    public string? CurrentLanguage { get; set; }
    public string? CurrentCountry { get; set; }
    public double LastKnownPosition { get; set; }
    public IList<string> UserMusicFoldersPreference { get; } = new List<string>();
    public AppStateModel()
    {
        
    }
}
