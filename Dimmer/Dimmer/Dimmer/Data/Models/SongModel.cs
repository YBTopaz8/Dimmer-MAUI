namespace Dimmer.Data.Models;
/// <summary>
/// 
/// </summary>
/// <seealso cref="RealmObject" />
public partial class SongModel : RealmObject
{

    /// <summary>
    /// Gets or sets the local device identifier.
    /// </summary>
    /// <value>
    /// The local device identifier.
    /// </value>
    [PrimaryKey]
    public string LocalDeviceId { get; set; } = Guid.NewGuid().ToString();
    /// <summary>
    /// Gets or sets the title.
    /// </summary>
    /// <value>
    /// The title.
    /// </value>
    public string Title { get; set; } = "Unknown Title";
    /// <summary>
    /// Gets or sets the name of the artist.
    /// </summary>
    /// <value>
    /// The name of the artist.
    /// </value>
    public string ArtistName { get; set; } = "Unknown Artist Name";
    /// <summary>
    /// Gets or sets the name of the album.
    /// </summary>
    /// <value>
    /// The name of the album.
    /// </value>
    public string AlbumName { get; set; } = "Unknown Album Name";
    /// <summary>
    /// Gets or sets the genre.
    /// </summary>
    /// <value>
    /// The genre.
    /// </value>
    public string Genre { get; set; } = "Unknown Genre";
    /// <summary>
    /// Gets or sets the file path.
    /// </summary>
    /// <value>
    /// The file path.
    /// </value>
    public string FilePath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the duration in seconds.
    /// </summary>
    /// <value>
    /// The duration in seconds.
    /// </value>
    public double DurationInSeconds { get; set; }
    /// <summary>
    /// Gets or sets the release year.
    /// </summary>
    /// <value>
    /// The release year.
    /// </value>
    public int? ReleaseYear { get; set; }
    /// <summary>
    /// Gets or sets the track number.
    /// </summary>
    /// <value>
    /// The track number.
    /// </value>
    public int? TrackNumber { get; set; }
    /// <summary>
    /// Gets or sets the file format.
    /// </summary>
    /// <value>
    /// The file format.
    /// </value>
    public string FileFormat { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the size of the file.
    /// </summary>
    /// <value>
    /// The size of the file.
    /// </value>
    public long FileSize { get; set; }
    /// <summary>
    /// Gets or sets the bit rate.
    /// </summary>
    /// <value>
    /// The bit rate.
    /// </value>
    public int? BitRate { get; set; }
    /// <summary>
    /// Gets or sets the rating.
    /// </summary>
    /// <value>
    /// The rating.
    /// </value>
    public int Rating { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether this instance has lyrics.
    /// </summary>
    /// <value>
    ///   <c>true</c> if this instance has lyrics; otherwise, <c>false</c>.
    /// </value>
    public bool HasLyrics { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether this instance has synced lyrics.
    /// </summary>
    /// <value>
    ///   <c>true</c> if this instance has synced lyrics; otherwise, <c>false</c>.
    /// </value>
    public bool HasSyncedLyrics { get; set; }

    /// <summary>
    /// Gets or sets the synchronize lyrics.
    /// </summary>
    /// <value>
    /// The synchronize lyrics.
    /// </value>
    public string SyncLyrics { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the cover image path.
    /// </summary>
    /// <value>
    /// The cover image path.
    /// </value>
    public string CoverImagePath { get; set; } = "musicnoteslider.png";
    /// <summary>
    /// Gets or sets the un synchronize lyrics.
    /// </summary>
    /// <value>
    /// The un synchronize lyrics.
    /// </value>
    public string UnSyncLyrics { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether this instance is playing.
    /// </summary>
    /// <value>
    ///   <c>true</c> if this instance is playing; otherwise, <c>false</c>.
    /// </value>
    public bool IsPlaying { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether this instance is favorite.
    /// </summary>
    /// <value>
    ///   <c>true</c> if this instance is favorite; otherwise, <c>false</c>.
    /// </value>
    public bool IsFavorite { get; set; }
    /// <summary>
    /// Gets or sets the achievement.
    /// </summary>
    /// <value>
    /// The achievement.
    /// </value>
    public string Achievement { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether this instance is file exists.
    /// </summary>
    /// <value>
    ///   <c>true</c> if this instance is file exists; otherwise, <c>false</c>.
    /// </value>
    public bool IsFileExists { get; set; } = true;
    /// <summary>
    /// Gets or sets the last date updated.
    /// </summary>
    /// <value>
    /// The last date updated.
    /// </value>
    public DateTimeOffset? LastDateUpdated { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the date created.
    /// </summary>
    /// <value>
    /// The date created.
    /// </value>
    public string? DateCreated { get; set; } = DateTime.UtcNow.ToString("o");
    /// <summary>
    /// Gets or sets the name of the device.
    /// </summary>
    /// <value>
    /// The name of the device.
    /// </value>
    public string? DeviceName { get; set; } = DeviceInfo.Current.Name;
    /// <summary>
    /// Gets or sets the device form factor.
    /// </summary>
    /// <value>
    /// The device form factor.
    /// </value>
    public string? DeviceFormFactor { get; set; } = DeviceInfo.Current.Idiom.ToString();
    /// <summary>
    /// Gets or sets the device model.
    /// </summary>
    /// <value>
    /// The device model.
    /// </value>
    public string? DeviceModel { get; set; } = DeviceInfo.Current.Model;
    /// <summary>
    /// Gets or sets the device manufacturer.
    /// </summary>
    /// <value>
    /// The device manufacturer.
    /// </value>
    public string? DeviceManufacturer { get; set; } = DeviceInfo.Current.Manufacturer;
    /// <summary>
    /// Gets or sets the device version.
    /// </summary>
    /// <value>
    /// The device version.
    /// </value>
    public string? DeviceVersion { get; set; } = DeviceInfo.Current.VersionString;
    /// <summary>
    /// Gets or sets the user identifier online.
    /// </summary>
    /// <value>
    /// The user identifier online.
    /// </value>
    public string? UserIDOnline { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SongModel"/> class.
    /// </summary>
    public SongModel()
    {
        
    }
}


