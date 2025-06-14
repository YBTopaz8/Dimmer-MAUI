using ATL;

namespace Dimmer.Data.Models;
public partial class SongModel : RealmObject, IRealmObjectWithObjectId
{

    [PrimaryKey]
    public ObjectId Id { get; set; }
    public string Title { get; set; }
    public string ArtistName { get; set; }
    public string OtherArtistsName { get; set; }
    public string AlbumName { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public double DurationInSeconds { get; set; }

    public int? ReleaseYear { get; set; }
    public int? TrackNumber { get; set; }
    public string FileFormat { get; set; } = string.Empty;
    public string Lyricist { get; set; } = string.Empty;
    public string Composer { get; set; } = string.Empty;
    public string Conductor { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public int? DiscNumber { get; set; }
    public int? DiscTotal { get; set; }
    public long FileSize { get; set; }
    public int? BitRate { get; set; }
    public int Rating { get; set; }
    public bool HasLyrics { get; set; }
    public bool HasSyncedLyrics { get; set; }

    public string? SyncLyrics { get; set; } = string.Empty;
    public string? CoverImagePath { get; set; }
    public byte[]? CoverImageBytes { get; set; }
    public byte[]? ArtistImageBytes { get; set; }
    public byte[]? AlbumImageBytes { get; set; }
    public string? UnSyncLyrics { get; set; }

    public bool IsFavorite { get; set; }
    public string Achievement { get; set; } = string.Empty;
    public bool IsFileExists { get; set; } = true;
    public DateTimeOffset? LastDateUpdated { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? DateCreated { get; set; } = DateTimeOffset.UtcNow;
    public string? DeviceName { get; set; }
    public string? DeviceFormFactor { get; set; }
    public string? DeviceModel { get; set; }
    public string? DeviceManufacturer { get; set; }
    public string? DeviceVersion { get; set; }
    public string? UserIDOnline { get; set; }

    public AlbumModel Album { get; set; }
    public ArtistModel Artist { get; set; }
    public GenreModel Genre { get; set; }
    public IList<ArtistModel> ArtistIds { get; } = null!;
    public IList<TagModel> Tags { get; } = null!;

    public IList<SyncLyrics> EmbeddedSync { get; } = null!;
    public IList<DimmerPlayEvent> PlayHistory { get; } = null!;

    public bool IsNew { get; set; }
    [Backlink(nameof(PlaylistModel.SongsInPlaylist))]
    public IQueryable<PlaylistModel> Playlists { get; } = null!;
    public IList<UserNoteModel> UserNotes { get; } = null!;

    public SongModel()
    {

    }
}
public partial class SyncLyrics : EmbeddedObject
{
    /// <summary>
    /// Timestamp of the phrase, in milliseconds
    /// </summary>
    public int TimestampMs { get; set; }
    /// <summary>
    /// Text
    /// </summary>
    public string Text { get; set; }
    public SyncLyrics()
    {
    }
    public SyncLyrics(int timestampMs, string text)
    {
        TimestampMs = timestampMs;
        Text = text;
    }
}
public partial class UserNoteModel : EmbeddedObject
{

    public string Id { get; set; } = AudioFileUtils.GenerateId("UNote");
    public string? UserMessageText { get; set; }
    public string? UserMessageImagePath { get; set; }
    public string? UserMessageAudioPath { get; set; }
    public bool IsPinned { get; set; }
    public int UserRating { get; set; }
    public string? MessageColor { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ModifiedAt { get; set; }
}
