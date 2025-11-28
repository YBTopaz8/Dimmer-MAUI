using Dimmer.Interfaces.Services.Interfaces.FileProcessing.FileProcessorUtils;

using MongoDB.Bson.Serialization.Attributes;

namespace Dimmer.Data.Models;

public partial class SongModel : RealmObject, IRealmObjectWithObjectId
{

    [PrimaryKey]
    [MapTo("_id")]
    public ObjectId Id { get; set; }
    public string Title { get; set; }
    public string ArtistName { get; set; }
    public string OtherArtistsName { get; set; }
    public string AlbumName { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public double DurationInSeconds { get; set; }
    public bool IsHidden { get; set; }

    public int? ReleaseYear { get; set; }
    public int NumberOfTimesFaved { get; set; }
    public int ManualFavoriteCount { get; set; }

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

    public bool? IsInstrumental { get; set; }
    public string? SyncLyrics { get; set; } = string.Empty;
    public string? CoverImagePath { get; set; }

    public int? TrackTotal { get; set; }
    public double SampleRate { get; set; }
    public string Encoder { get; set; }
    public int BitDepth { get; set; }
    public int NbOfChannels { get; set; }

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
    public string GenreName { get; set; }
    public IList<ArtistModel> ArtistToSong { get; } = null!;
    public IList<PlaylistModel> PlaylistsHavingSong { get; } = null!;
    public IList<TagModel> Tags { get; } = null!;

    public IList<SyncLyrics> EmbeddedSync { get; } = null!;
    public IList<DimmerPlayEvent> PlayHistory { get; } = null!;

    public bool IsNew { get; set; }
    public float? BPM { get; set; }
    public IList<UserNoteModel> UserNotes { get; } = null!;

    [BsonIgnore]
    public string LyricsText
    {
        get
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(UnSyncLyrics))
            {
                sb.AppendLine(UnSyncLyrics);
            }
            if (EmbeddedSync != null && EmbeddedSync.Any())
            {
                foreach (var line in EmbeddedSync)
                {
                    sb.AppendLine(line.Text);
                }
            }
            return sb.ToString();
        }
    }

    /// <summary>
    /// A computed, indexed property to enforce uniqueness based on Title and Duration.
    /// This acts as our "composite primary key" for business logic.
    /// </summary>
    [Indexed]
    public string? TitleDurationKey { get; private set; }


    // This is a "setter" method that should ALWAYS be used to set the title or duration
    // to ensure the key is updated correctly.
    public void SetTitleAndDuration(string? title, double duration)
    {
        if (title is null)
        {
            return;
        }
        Title = title;
        DurationInSeconds = duration;
        TitleDurationKey = $"{title.ToLowerInvariant().Trim()}|{duration}";
    }


    public SongModel()
    {

    }

    [Indexed]
    public int SongTypeValue { get; set; } = (int)SongType.Track;

    [Ignored]
    public SongType SongType { get => (SongType)SongTypeValue; set => SongTypeValue = (int)value; }

    public ObjectId? ParentSongId { get; set; }
    public double? SegmentStartTime { get; set; }
    public double? SegmentEndTime { get; set; }
    public int SegmentEndBehaviorValue { get; set; } = (int)SegmentEndBehavior.Stop;

    [Ignored]
    public SegmentEndBehavior SegmentEndBehavior { get => (SegmentEndBehavior)SegmentEndBehaviorValue; set => SegmentEndBehaviorValue = (int)value; }
    public string CoverArtHash { get;  set; }

    /// <summary>
    /// A pre-computed, concatenated string of all searchable text fields for this song.
    /// This is used for the 'any:' TQL query.
    /// Decorating with [FullText] enables high-performance text searches.
    /// </summary>
    [Indexed(IndexType.FullText)]
    public string? SearchableText { get; set; }

    /// <summary>
    /// The aggregated text from all user notes. Used for 'note:' TQL query.
    /// This avoids complex and slow queries over embedded objects.
    /// </summary>
    [Indexed]
    public string? UserNoteAggregatedText { get; set; }

    /// <summary>
    /// The date and time this song was last played to completion.
    /// This is essential for fast sorting and filtering by 'played:'.
    /// </summary>
    [Indexed]
    public DateTimeOffset LastPlayed { get; set; }

    /// <summary>
    /// A simple count of total plays. Faster than querying PlayHistory.Count() every time.
    /// </summary>
    [Indexed]
    public int PlayCount { get; set; }

    /// <summary>
    /// A simple count of completed plays. Faster than querying PlayHistory.Count(p => ...).
    /// </summary>
    [Indexed]
    public int PlayCompletedCount { get; set; }

    [Indexed]
    public int SkipCount { get; set; }


    /// <summary>
    /// The ratio of completed plays to total plays (starts). A value between 0.0 and 1.0.
    /// High value indicates a "sticky" song the user doesn't skip.
    /// </summary>
    public double ListenThroughRate { get; set; }

    /// <summary>
    /// The ratio of skips to total plays (starts). A value between 0.0 and 1.0.
    /// High value indicates a "burnout" song the user is tired of.
    /// </summary>
    public double SkipRate { get; set; }

    /// <summary>
    /// The date and time this song was first played. Useful for "On this day" or "Discovery" features.
    /// </summary>
    [Indexed]
    public DateTimeOffset FirstPlayed { get; set; }

    /// <summary>
    /// A calculated score representing the song's overall importance to the user.
    /// Considers play counts, completion rate, skips, and favorite status.
    /// </summary>
    public double PopularityScore { get; set; }

    /// <summary>
    /// The song's overall rank (e.g., #1, #2) in the entire library based on PopularityScore.
    /// </summary>
    [Indexed]
    public int GlobalRank { get; set; }

    /// <summary>
    /// The song's rank within its primary album, based on play count.
    /// </summary>
    [Indexed]
    public int RankInAlbum { get; set; }

    /// <summary>
    /// The song's rank among all songs by its primary artist, based on play count.
    /// </summary>
    [Indexed]
    public int RankInArtist { get; set; }
    public int PauseCount { get;  set; }
    public int ResumeCount { get;  set; }
    public int SeekCount { get;  set; }
    public int LastPlayEventType { get;  set; }
    public int PlayStreakDays { get;  set; }
    public int EddingtonNumber { get;  set; }
    public double EngagementScore { get; set; }
    public int TotalPlayDurationSeconds { get;  set; }
    public int RepeatCount { get;  set; }
    public int PreviousCount { get;  set; }
    public int RestartCount { get;  set; }
    public DateTimeOffset DiscoveryDate { get;  set; }
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
    public string? Text { get; set; }
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

    public string Id { get; set; } = TaggingUtils.GenerateId("UNote");
    public string? UserMessageText { get; set; }
    public string? UserMessageImagePath { get; set; }
    public string? UserMessageAudioPath { get; set; }
    public bool IsPinned { get; set; }
    public int UserRating { get; set; }
    public string? MessageColor { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ModifiedAt { get; set; }
}
