namespace Dimmer.Data.Models;
/// <summary>
/// 
/// </summary>
/// <seealso cref="RealmObject" />
public partial class AlbumModel : RealmObject, IRealmObjectWithObjectId
{

    public IList<string> EarnedAchievementIds { get; }
    public bool IsNew { get; set; }

    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    /// <value>
    /// The name.
    /// </value>
    public string Name { get; set; }
    /// <summary>
    /// Gets or sets the Url.
    /// </summary>
    public string Url { get; set; }
    /// <summary>
    /// Gets or sets the release year.
    /// </summary>
    /// <value>
    /// The release year.
    /// </value>
    public int? ReleaseYear { get; set; }
    /// <summary>
    /// Gets or sets the description.
    /// </summary>
    /// <value>
    /// The description.
    /// </value>
    public string? Description { get; set; }
    /// <summary>
    /// Gets or sets the number of tracks.
    /// </summary>
    /// <value>
    /// The number of tracks.
    /// </value>
    public int NumberOfTracks { get; set; }
    public int? TrackTotal { get; set; }
    public int? DiscTotal { get; set; }
    public int? DiscNumber { get; set; }
    /// <summary>
    /// Gets or sets the image path.
    /// </summary>
    /// <value>
    /// The image path.
    /// </value>
    public string? ImagePath { get; set; } = "musicalbum.png";
    /// <summary>
    /// Gets or sets the total duration.
    /// </summary>
    /// <value>
    /// The total duration.
    /// </value>
    public string? TotalDuration { get; set; }

    /// <summary>
    /// Gets or sets the date created.
    /// </summary>
    /// <value>
    /// The date created.
    /// </value>
    public DateTimeOffset? DateCreated { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>
    /// Gets or sets the name of the device.
    /// </summary>
    /// <value>
    /// The name of the device.
    /// </value>
    public string? DeviceName { get; set; }
    /// <summary>
    /// Gets or sets the device form factor.
    /// </summary>
    /// <value>
    /// The device form factor.
    /// </value>
    public string? DeviceFormFactor { get; set; }
    /// <summary>
    /// Gets or sets the device model.
    /// </summary>
    /// <value>
    /// The device model.
    /// </value>
    public string? DeviceModel { get; set; }
    /// <summary>
    /// Gets or sets the device manufacturer.
    /// </summary>
    /// <value>
    /// The device manufacturer.
    /// </value>
    public string? DeviceManufacturer { get; set; }
    /// <summary>
    /// Gets or sets the device version.
    /// </summary>
    /// <value>
    /// The device version.
    /// </value>
    public string? DeviceVersion { get; set; }
    public ArtistModel? Artist{ get; set; }
    /// <summary>
    /// Gets or sets the local device identifier.
    /// </summary>
    /// <value>
    /// The local device identifier.
    /// </value>
    [PrimaryKey]
    [MapTo("_id")]
    public ObjectId Id { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AlbumModel"/> class.
    /// </summary>
    public IList<ArtistModel> Artists { get; } = null!;

    [Backlink(nameof(SongModel.Album))]
    public IQueryable<SongModel>? SongsInAlbum { get; }



    public IList<TagModel> Tags { get; } = null!;

    public IList<UserNoteModel>? UserNotes { get; } = null!;


    /// <summary>
    /// The percentage of songs in this album that have been played at least once.
    /// A value between 0.0 and 1.0.
    /// </summary>
   
    public double CompletionPercentage { get; set; }

    /// <summary>
    /// The total number of times any song from this album has been played to completion.
    /// </summary>
    [Indexed]
    public int TotalCompletedPlays { get; set; }

    /// <summary>
    /// The average ListenThroughRate of all songs in this album. Indicates "album quality".
    /// High value means the album is "all killer, no filler".
    /// </summary>
    
    public double AverageSongListenThroughRate { get; set; }
    public double EddingtonNumber { get; set; }
    public double ParetoTopSongsCount { get; set; }
    public double ParetoPercentage { get; set; }
    public double TotalSkipCount { get; set; }

    /// <summary>
    /// The overall rank of this album in the library, based on its total plays.
    /// </summary>
    [Indexed]
    public int OverallRank { get; set; }
    public int TotalPlayDurationSeconds { get; set; }
    public bool IsFavorite { get; set; }
    public DateTimeOffset DiscoveryDate { get;  set; }

    public AlbumModel()
    {
    }

}