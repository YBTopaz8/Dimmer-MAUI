namespace Dimmer.Data.Models;

/// <summary>
/// 
/// </summary>
/// <seealso cref="RealmObject" />
public partial class PlayDateAndCompletionStateSongLink : RealmObject
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
    /// Indicates the type of play action performed.
    /// Possible VALID values for <see cref="PlayType" />:
    /// <list type="bullet"><item><term>0</term><description>Play</description></item><item><term>1</term><description>Pause</description></item><item><term>2</term><description>Resume</description></item><item><term>3</term><description>Completed</description></item><item><term>4</term><description>Seeked</description></item><item><term>5</term><description>Skipped</description></item><item><term>6</term><description>Restarted</description></item><item><term>7</term><description>SeekRestarted</description></item><item><term>8</term><description>CustomRepeat</description></item><item><term>9</term><description>Previous</description></item></list>
    /// </summary>
    /// <value>
    /// The type of the play.
    /// </value>
    public int PlayType { get; set; }
    /// <summary>
    /// Gets or sets the date played.
    /// </summary>
    /// <value>
    /// The date played.
    /// </value>
    public DateTimeOffset DatePlayed { get; set; }
    /// <summary>
    /// Gets or sets the date finished.
    /// </summary>
    /// <value>
    /// The date finished.
    /// </value>
    public DateTimeOffset DateFinished { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether [was play completed].
    /// </summary>
    /// <value>
    ///   <c>true</c> if [was play completed]; otherwise, <c>false</c>.
    /// </value>
    public bool WasPlayCompleted { get; set; }
    /// <summary>
    /// Gets or sets the position in seconds.
    /// </summary>
    /// <value>
    /// The position in seconds.
    /// </value>
    public double PositionInSeconds { get; set; }
    /// <summary>
    /// Gets or sets the event date.
    /// </summary>
    /// <value>
    /// The event date.
    /// </value>
    public DateTimeOffset? EventDate { get; set; } = DateTimeOffset.UtcNow;
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
    /// Initializes a new instance of the <see cref="PlayDateAndCompletionStateSongLink"/> class.
    /// </summary>
    
    public SongModel Song { get; set; }
    public ArtistModel Artist{ get; set; }
    public AlbumModel Album { get; set; }
    public GenreModel Genre { get; set; }


    public PlayDateAndCompletionStateSongLink()
    {

    }

}