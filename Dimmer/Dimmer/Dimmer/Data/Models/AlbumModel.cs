using MongoDB.Bson;

namespace Dimmer.Data.Models;
/// <summary>
/// 
/// </summary>
/// <seealso cref="RealmObject" />
public partial class AlbumModel : RealmObject, IRealmObjectWithObjectId
{

    public bool IsNewOrModified { get; set; }

    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    /// <value>
    /// The name.
    /// </value>
    public string? Name { get; set; }
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
    /// <summary>
    /// Gets or sets the local device identifier.
    /// </summary>
    /// <value>
    /// The local device identifier.
    /// </value>
    [PrimaryKey]
    public ObjectId Id { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AlbumModel"/> class.
    /// </summary>
    public IList<ArtistModel> ArtistIds { get; }

    [Backlink(nameof(SongModel.Album))]
    public IQueryable<SongModel>? SongsInAlbum { get; }

    public IList<TagModel> Tags { get; }

    public IList<UserNoteModel>? UserNotes { get; }
    public AlbumModel()
    {
    }

}