namespace Dimmer.Data.Models;
public partial class PlaylistModel : RealmObject, IRealmObjectWithObjectId
{
    [PrimaryKey]
    public ObjectId Id { get; set; } = ObjectId.GenerateNewId();

    public string PlaylistName { get; set; } = "New Playlist";
    public string? Description { get; set; }

    /// <summary>
    /// A flag to determine if the playlist is dynamic (query-based) or a manual collection of songs.
    /// </summary>
    public bool IsSmartPlaylist { get; set; }

    /// <summary>
    /// For Smart Playlists (IsSmartPlaylist = true).
    /// The full query string that defines the content of this playlist.
    /// Example: "artist:tool include genre:metal exclude year:<2000"
    /// </summary>
    public string? QueryText { get; set; }

    /// <summary>
    /// For Manual Playlists (IsSmartPlaylist = false).
    /// A list of song ObjectIds that the user has manually added.
    /// </summary>
    public IList<ObjectId> ManualSongIds { get; }

    // --- Optional Metadata ---
    public DateTimeOffset DateCreated { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Path to a custom cover image for the playlist. If null, UI can generate one.
    /// </summary>
    public string? CoverImagePath { get; set; }

    /// <summary>
    /// The user who created this playlist.
    /// </summary>
    public UserModel? User { get; set; }

    // --- Ignored Properties (Not in Database) ---
    [Ignored]
    public bool IsNew { get; set; }
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