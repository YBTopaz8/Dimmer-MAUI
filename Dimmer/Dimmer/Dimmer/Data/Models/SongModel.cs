using MongoDB.Bson;
using static ATL.LyricsInfo;

namespace Dimmer.Data.Models;
/// <summary>
/// 
/// </summary>
/// <seealso cref="RealmObject" />
public partial class SongModel : RealmObject, IRealmObjectWithObjectId
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
    /// Gets or sets the title.
    /// </summary>
    /// <value>
    /// The title.
    /// </value>
    public string Title { get; set; }
    /// <summary>
    /// Gets or sets the name of the artist.
    /// </summary>
    /// <value>
    /// The name of the artist.
    /// </value>
    public string ArtistName { get; set; }
    /// <summary>
    /// Gets or sets the name of the album.
    /// </summary>
    /// <value>
    /// The name of the album.
    /// </value>
    public string AlbumName { get; set; } 
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
    [Backlink(nameof(PlaylistModel.Songs))]
    public IQueryable<PlaylistModel> Playlists { get; }
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
    public string Composer { get; set; } = string.Empty;
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
    public string? CoverImagePath { get; set; }
    /// <summary>
    /// Gets or sets the un synchronize lyrics.
    /// </summary>
    /// <value>
    /// The un synchronize lyrics.
    /// </value>
    public string? UnSyncLyrics { get; set; }
    
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
    /// Gets or sets the user identifier online.
    /// </summary>
    /// <value>
    /// The user identifier online.
    /// </value>
    public string? UserIDOnline { get; set; }

    public IList<UserNoteModel> UserNotes { get; }
    public AlbumModel? Album { get;  set; }
    public GenreModel? Genre { get;  set; }
    public IList<ArtistModel> ArtistIds { get; }
    [Backlink(nameof(TagModel.Songs))]
    public IQueryable<TagModel> Tags { get; }
    [Backlink(nameof(DimmerPlayEvent.Song))]
    public IQueryable<DimmerPlayEvent> PlayHistory { get; }
    public SongModel()
    {
        
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
