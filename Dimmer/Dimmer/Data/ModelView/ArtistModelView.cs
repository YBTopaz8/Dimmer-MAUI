
namespace Dimmer.Data.ModelView;

public partial class ArtistModelView : ObservableObject
{
    [ObservableProperty]
    public partial ObjectId Id { get; set; }
    [ObservableProperty]
    public partial string Url { get; set; }
    [ObservableProperty]
    public partial string? Name { get; set; }
    [ObservableProperty]
    public partial string? ImagePath { get; set; }
    [ObservableProperty]
    public partial byte[]? ImageBytes { get; set; }
    [ObservableProperty]
    public partial string? Bio { get; set; }
    [ObservableProperty]
    public partial bool IsCurrentlySelected { get; set; }
    [ObservableProperty]
    public partial bool IsVisible { get; set; }
    [ObservableProperty]
    public partial bool IsNew { get; set; }
    [ObservableProperty]
    public partial bool IsFavorite { get; set; }


    public DateTimeOffset? DateCreated { get; set; } = DateTimeOffset.UtcNow;
    public string? DeviceName { get; set; }
    public string? DeviceFormFactor { get; set; }
    public string? DeviceModel { get; set; }
    public string? DeviceManufacturer { get; set; }
    public string? DeviceVersion { get; set; }

    /// <summary>
    /// The percentage of an artist's songs that have been played at least once.
    /// </summary>
    [ObservableProperty]
    public partial double CompletionPercentage { get; set; }

    /// <summary>
    /// The total number of times any song by this artist has been played to completion.
    /// </summary>
    
    [ObservableProperty]
    public partial int TotalCompletedPlays { get; set; }

    /// <summary>
    /// The average ListenThroughRate of all songs by this artist. Indicates artist consistency.
    /// </summary>
    [ObservableProperty]
    public partial double AverageSongListenThroughRate { get; set; }

    /// <summary>
    /// The overall rank of this artist in the library, based on their total plays.
    /// </summary>
    
    [ObservableProperty]
    public partial int OverallRank { get; set; }

    [ObservableProperty]
    public partial double TotalSkipCount { get; set; }
    [ObservableProperty]
    public partial DateTimeOffset DiscoveryDate { get; set; }
    [ObservableProperty]
    public partial double EddingtonNumber { get; set; }
    [ObservableProperty]
    public partial double ParetoTopSongsCount { get; set; }
    [ObservableProperty]
    public partial double ParetoPercentage { get; set; }
    [ObservableProperty]
    public partial int TotalSongsByArtist { get; set; }
    [ObservableProperty]
    public partial int TotalAlbumsByArtist { get; set; }
    [ObservableProperty]
    public partial ObservableCollection<string> ListOfSimilarArtists { get;  set; }
}