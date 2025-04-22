namespace Dimmer.Data.Models;

/// <summary>
/// 
/// </summary>
/// <seealso cref="RealmObject" />
public partial class AlbumArtistGenreSongLink : RealmObject
{
    /// <summary>
    /// Gets or sets the local device identifier.
    /// </summary>
    /// <value>
    /// The local device identifier.
    /// </value>
    [PrimaryKey]
    public string? LocalDeviceId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the song identifier.
    /// </summary>
    /// <value>
    /// The song identifier.
    /// </value>
    public string? SongId { get; set; }
    /// <summary>
    /// Gets or sets the album identifier.
    /// </summary>
    /// <value>
    /// The album identifier.
    /// </value>
    public string? AlbumId { get; set; }
    /// <summary>
    /// Gets or sets the artist identifier.
    /// </summary>
    /// <value>
    /// The artist identifier.
    /// </value>
    public string? ArtistId { get; set; }
    /// <summary>
    /// Gets or sets the genre identifier.
    /// </summary>
    /// <value>
    /// The genre identifier.
    /// </value>
    public string? GenreId { get; set; }

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
    /// Initializes a new instance of the <see cref="AlbumArtistGenreSongLink"/> class.
    /// </summary>
    public AlbumArtistGenreSongLink()
    {
    }

}
