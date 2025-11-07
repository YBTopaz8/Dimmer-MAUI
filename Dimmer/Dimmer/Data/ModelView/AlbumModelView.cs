
namespace Dimmer.Data.ModelView;
public partial class AlbumModelView : ObservableObject
{
    [ObservableProperty]
    public partial string Name { get; set; }
    //[ObservableProperty]
    //public partial ObservableCollection<SongModelView>? Songs{ get; set; }
    [ObservableProperty]
    public partial ObjectId Id { get; set; }
    [ObservableProperty]
    public partial int? ReleaseYear { get; set; }
    [ObservableProperty]
    public partial bool IsNew { get; set; }
    [ObservableProperty]
    public partial int NumberOfTracks { get; set; }
    [ObservableProperty]
    public partial string? TotalDuration { get; set; }
    [ObservableProperty]
    public partial string? Description { get; set; }
    [ObservableProperty]
    public partial string? ImagePath { get; set; } = "musicalbum.png";
    [ObservableProperty] public partial byte[]? ImageBytes { get; set; }

  
    public DateTimeOffset? DateCreated { get; set; } = DateTimeOffset.UtcNow;
    public string? DeviceName { get; set; }
    public string? DeviceFormFactor { get; set; }
    public string? DeviceModel { get; set; }

    public int? TrackTotal { get; set; }
    public int? DiscTotal { get; set; }
    public int? DiscNumber { get; set; }
    public string? DeviceManufacturer { get; set; }
    public string? DeviceVersion { get; set; }
    [ObservableProperty]
    public partial bool IsCurrentlySelected { get; set; }
    [ObservableProperty]
    public partial double AverageSongListenThroughRate { get; set; }

    [ObservableProperty]
    public partial DateTimeOffset DiscoveryDate { get; set; }

    [ObservableProperty]
    public partial double CompletionPercentage { get; set; }
    [ObservableProperty]
    public partial double OverallRank { get; set; }
    [ObservableProperty]
    public partial int TotalCompletedPlays { get; set; }

    [ObservableProperty]
    public partial double EddingtonNumber { get; set; }
    [ObservableProperty]
    public partial double ParetoTopSongsCount { get; set; }
    [ObservableProperty]
    public partial double ParetoPercentage { get; set; }
    [ObservableProperty]
    public partial double TotalSkipCount { get; set; }
    [ObservableProperty]
    public partial int TotalPlayDurationSeconds { get; set; }
    [ObservableProperty]
    public partial List<ArtistModelView>? Artists { get;  set; }
    [ObservableProperty]
    public partial ObservableCollection<SongModelView>? SongsInAlbum { get; set; }
    [ObservableProperty]
    public partial bool IsFavorite { get; internal set; }
}
public class AlbumGroupViewModel : ObservableObject
{
    public string? AlbumName { get; set; }
    public string? AlbumCoverSource { get; set; } // Path or URL to the cover
    public int Year { get; set; }
    public ObservableCollection<SongModelView>? SongsInAlbum { get; set; }
}