namespace Dimmer.Data.Models;
public partial class PlaylistModel : RealmObject, IRealmObjectWithObjectId
{
    /// <summary>
    /// Gets or sets the local device identifier.
    /// </summary>
    /// <value>
    /// The local device identifier.
    /// </value>
    [PrimaryKey]
    public ObjectId Id { get; set; }
    /// <summary>
    /// Gets or sets the name of the playlist.
    /// </summary>
    /// <value>
    /// The name of the playlist.
    /// </value>
    public string PlaylistName { get; set; } = "Unknown Playlist";
    /// <summary>
    /// Gets or sets the date created.
    /// </summary>
    /// <value>
    /// The date created.
    /// </value>
    public DateTimeOffset DateCreated { get; set; } = DateTimeOffset.UtcNow;

    public IList<string>? SongInPlaylist { get; }
    public string? CurrentSongId { get; set; }
    public string? Description { get; set; }
    public string? CoverImagePath { get; set; }
    public string? Color { get; set; }
    public string? PlaylistType { get; set; } = "General";

    public IList<PlaylistEvent>? PlaylistEvents { get; }
    public string? DeviceName { get; set; } 
    
    public UserModel? User { get; set; }
    //3 PL ; general,
    // invisible when in an artist/album
    // custom
}

public partial class PlaylistEvent : EmbeddedObject
{
    // Save the enum value as an int in Realm.
    public int PlayTypeValue { get; set; }

    // This property is for your code; Realm will ignore it.
    
    public PlayType PlayType
    {
        get => (PlayType)PlayTypeValue;
        set => PlayTypeValue = (int)value;
    }
    public string? DateCreated { get; set; } = DateTime.UtcNow.ToString("o");
    public string? EventSongId { get; set; }


}