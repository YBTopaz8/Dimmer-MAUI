namespace Dimmer.Data.Models;

[Dimmer.Utils.Preserve(AllMembers = true)]
public partial class ArtistModel : RealmObject, IRealmObjectWithObjectId
{
    public IList<string> EarnedAchievementIds { get; }

    /// <summary>
    /// Gets or sets the Url.
    /// </summary>
    public string Url { get; set; }
    public bool IsNew { get; set; }
    public bool IsFavorite { get; set; }

    [PrimaryKey]
    [MapTo("_id")]
    public ObjectId Id { get; set; }
    public string Name { get; set; }
    public string? Bio { get; set; }
    public DateTimeOffset? DateCreated { get; set; } = DateTimeOffset.UtcNow;
    public string? DeviceName { get; set; }
    public string? DeviceFormFactor { get; set; }
    public string? DeviceModel { get; set; }
    public string? DeviceManufacturer { get; set; }
    public string? DeviceVersion { get; set; }
    [Backlink(nameof(SongModel.ArtistToSong))]
    public IQueryable<SongModel> Songs { get; }

    [Backlink(nameof(AlbumModel.Artists))]
    public IQueryable<AlbumModel> Albums { get; }



    public IList<TagModel> Tags { get; } = null!;
    public string? ImagePath { get; set; }
    public IList<UserNoteModel> UserNotes { get; } = null!;

    /// <summary>
    /// The percentage of an artist's songs that have been played at least once.
    /// </summary>
    public double CompletionPercentage { get; set; }

    /// <summary>
    /// The total number of times any song by this artist has been played to completion.
    /// </summary>
    [Indexed]
    public int TotalCompletedPlays { get; set; }
    
    public int TotalSongsByArtist { get; set; }
    public int TotalAlbumsByArtist { get; set; }

    /// <summary>
    /// The average ListenThroughRate of all songs by this artist. Indicates artist consistency.
    /// </summary>
    public double AverageSongListenThroughRate { get; set; }
    public double TotalSkipCount { get; set; }
    public DateTimeOffset DiscoveryDate { get; set; }
    public double EddingtonNumber { get; set; }
    public double ParetoTopSongsCount { get; set; }
    public double ParetoPercentage { get; set; }

    /// <summary>
    /// The overall rank of this artist in the library, based on their total plays.
    /// </summary>
    [Indexed]
    public int OverallRank { get; set; }

    public ArtistModel()
    {

    }
}
