
using static ATL.LyricsInfo;

namespace Dimmer.Data.ModelView;
public partial class SongModelView : ObservableObject
{
    [ObservableProperty]
    public partial ObjectId Id { get; set; }
    [ObservableProperty]
    public partial string Title { get; set; }

    [ObservableProperty]
    public partial string TitleDurationKey { get; set; }


    public void SetTitleAndDuration(string title, double duration)
    {
       
        Title = title;
        DurationInSeconds = duration;
        TitleDurationKey = $"{title.ToLowerInvariant().Trim()}|{duration}";
    }
    [ObservableProperty]
    public partial string ArtistName { get; set; } 

    [ObservableProperty]
    public partial string AlbumName { get; set; } 

    [ObservableProperty]
    public partial AlbumModelView Album { get; set; } = new();
    [ObservableProperty]
    public partial ObservableCollection<ArtistModelView?> ArtistToSong { get; set; }
    [ObservableProperty]
    public partial GenreModelView Genre { get; set; } = new();
    [ObservableProperty]
    public partial string GenreName { get; set; } = string.Empty; 
    [ObservableProperty]
    public partial string FilePath { get; set; } 
    [ObservableProperty]
    public partial double DurationInSeconds { get; set; }
    [ObservableProperty]
    public partial int? ReleaseYear { get; set; }
    [ObservableProperty]
    public partial int? TrackNumber { get; set; }
    [ObservableProperty]
    public partial string FileFormat { get; set; } = string.Empty;
    [ObservableProperty]
    public partial long FileSize { get; set; }
    [ObservableProperty]
    public partial int? BitRate { get; set; }
    [ObservableProperty]
    public partial int Rating { get; set; } = 0;
    [ObservableProperty]
    public partial bool HasLyrics { get; set; }
    [ObservableProperty]
    public partial bool HasSyncedLyrics { get; set; }
    [ObservableProperty]
    public partial string SyncLyrics { get; set; } = string.Empty;

    [ObservableProperty]
    public partial byte[]? CoverImageBytes { get; set; }

    [ObservableProperty]
    public partial bool? IsInstrumental { get; set; }
    [ObservableProperty]
    public partial string CoverImagePath { get; set; }
    [ObservableProperty]
    public partial string? UnSyncLyrics { get; set; } = string.Empty;
    [ObservableProperty]
    public partial bool IsPlaying { get; set; }
    [ObservableProperty]
    public partial bool IsCurrentPlayingHighlight { get; set; } = false;
    [ObservableProperty]
    public partial bool IsFavorite { get; set; }
    [ObservableProperty]
    public partial string Achievement { get; set; } = string.Empty;
    [ObservableProperty]
    public partial bool IsFileExists { get; set; } = true;
    [ObservableProperty]
    public partial DateTimeOffset? LastDateUpdated { get; set; } = DateTimeOffset.UtcNow;
    [ObservableProperty]
    public partial DateTimeOffset? DateCreated { get; set; } = DateTimeOffset.UtcNow;
    [ObservableProperty]
    public partial string? DeviceName { get; set; }
    [ObservableProperty]
    public partial string? DeviceFormFactor { get; set; }
    [ObservableProperty]
    public partial string? DeviceModel { get; set; }
    [ObservableProperty]
    public partial string? DeviceManufacturer { get; set; }
    [ObservableProperty]
    public partial string? DeviceVersion { get; set; }
    [ObservableProperty]
    public partial string OtherArtistsName { get; set; }


    [ObservableProperty]
    public partial string Lyricist { get; set; } = string.Empty;

    [ObservableProperty]
    public partial float? BPM { get; set; }
    [ObservableProperty]
    public partial string Composer { get; set; } = string.Empty;
    [ObservableProperty]
    public partial string Conductor { get; set; } = string.Empty;
    [ObservableProperty]
    public partial string Description { get; set; } = string.Empty;
    [ObservableProperty]
    public partial string Language { get; set; } = string.Empty;
    [ObservableProperty]
    public partial int? DiscNumber { get; set; }
    [ObservableProperty]
    public partial int? DiscTotal { get; set; }
    [ObservableProperty]
    public partial string? UserIDOnline { get; set; }
    [ObservableProperty]
    public partial bool IsNew { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<DimmerPlayEventView> PlayEvents { get; set; } = new();

    [ObservableProperty]
    public partial ObservableCollection<SyncLyricsView> EmbeddedSync { get; set; } = new();


    [ObservableProperty]
    public partial ObservableCollection<UserNoteModelView> UserNotes { get; set; } = new();
    // Override Equals to compare based on string
    public override bool Equals(object? obj)
    {
        if (string.IsNullOrEmpty(this.Title))
        {
            return false;
        }
        if (obj is SongModelView other)
        {

            if(this.TitleDurationKey is null && !string.IsNullOrEmpty(Title) && DurationInSeconds != 0)
            {
                SetTitleAndDuration(Title,DurationInSeconds);
            }
            if (other.TitleDurationKey is null && !string.IsNullOrEmpty(other.Title) && other.DurationInSeconds != 0)
            {
                other.SetTitleAndDuration(other.Title, other.DurationInSeconds);
            }
            return this.TitleDurationKey == other.TitleDurationKey;
        }

        return false;
    }
    public int PlayCount => PlayEvents?.Count ?? 0;
    public int PlayCompletedCount => PlayEvents
        .Where(x => x.PlayType == (int)PlayType.Completed).Count();
    public DateTimeOffset LastPlayed =>
       PlayEvents?
           .Where(x => x.PlayType == (int)PlayType.Completed)

           .OrderByDescending(x => x.EventDate)
           .FirstOrDefault()?
           .EventDate ?? DateTimeOffset.MinValue;
    [ObservableProperty]
    public partial string SearchableText { get; private set; }

    // This method is called after the object is created and its properties are set.
    public void PrecomputeSearchableText()
    {
        //var allNotes = UserNotes?.Select(note => note.UserMessageText ?? string.Empty).ToString()?? string.Empty;
        //if (!string.IsNullOrEmpty(allNotes))
        //{
        //    Debug.WriteLine(allNotes);
        //}
        var sb = new StringBuilder();
        sb.Append(Title?.ToLowerInvariant()).Append(' ');
        sb.Append(OtherArtistsName?.ToLowerInvariant()).Append(' ');
        sb.Append(AlbumName?.ToLowerInvariant()).Append(' ');
        sb.Append(UnSyncLyrics?.ToLowerInvariant()).Append(' ');
        sb.Append(SyncLyrics?.ToLowerInvariant()).Append(' ');
        sb.Append(Genre?.Name?.ToLowerInvariant()).Append(' '); // Example of adding more
        //sb.Append(allNotes?.ToLowerInvariant()).Append(' '); // Example of adding more
        //sb.Append(UserNoteAggregatedText?.ToLowerInvariant()).Append(' ');

        SearchableText = sb.ToString();
        if (string.IsNullOrEmpty(SearchableText) || string.IsNullOrWhiteSpace(SearchableText))
        {
            SearchableText=string.Empty;
        }
        HasSyncedLyrics= !string.IsNullOrEmpty(SyncLyrics) && SyncLyrics.Length > 1;
        HasLyrics = !string.IsNullOrEmpty(UnSyncLyrics) && UnSyncLyrics.Length > 1;
        IsFileExists = !string.IsNullOrEmpty(FilePath) && File.Exists(FilePath);

    }
    public string UserNoteAggregatedText =>
        UserNotes != null && UserNotes.Any()
        ? string.Join(" ", UserNotes.Select(n => n.UserMessageText))
        : string.Empty;

    public SongModelView()
    {
        PrecomputeSearchableText();
    }
    public override int GetHashCode()
    {
        return HashCode.Combine(Id);
    }

    [Indexed]
    [ObservableProperty]
    public partial int SongTypeValue { get; set; } = (int)SongType.Track;

    public SongType SongType { get => (SongType)SongTypeValue; set => SongTypeValue = (int)value; }

    [ObservableProperty]
    public partial ObjectId? ParentSongId { get; set; }
    [ObservableProperty]
    public partial double? SegmentStartTime { get; set; }
    [ObservableProperty]
    public partial double? SegmentEndTime { get; set; }
    [ObservableProperty]
    public partial int SegmentEndBehaviorValue { get; set; } = (int)SegmentEndBehavior.Stop;

    public SegmentEndBehavior SegmentEndBehavior { get => (SegmentEndBehavior)SegmentEndBehaviorValue; set => SegmentEndBehaviorValue = (int)value; }
    [ObservableProperty]
    public partial Color? CurrentPlaySongDominantColor { get; internal set; }
}

public partial class UserNoteModelView : ObservableObject
{
    [ObservableProperty]
    public partial string? UserMessageText { get; set; }
    [ObservableProperty]
    public partial DateTimeOffset CreatedAt { get; set; }
    [ObservableProperty]
    public partial DateTimeOffset ModifiedAt { get; set; }
    [ObservableProperty]
    public partial string? Id{ get; set; }
    [ObservableProperty]
    public partial string? UserMessageImagePath { get; set; }
    [ObservableProperty]
    public partial string? UserMessageAudioPath { get; set; }
    [ObservableProperty]
    public partial bool IsPinned { get; set; }
    [ObservableProperty]
    public partial int UserRating { get; set; }
    [ObservableProperty]
    public partial string? MessageColor { get; set; }
}
public partial class SyncLyricsView : ObservableObject
{
    public int TimestampMs { get; set; }
    public string? Text { get; set; }
    /// <summary>
    /// Start timestamp of the phrase, in milliseconds
    /// </summary>
    public int TimestampStart { get; }
    /// <summary>
    /// End timestamp of the phrase, in milliseconds
    /// </summary>
    public int TimestampEnd { get; set; }
    /// <summary>
    /// Text
    /// </summary>
    public List<LyricsPhrase>? Beats { get; }


    public bool IsLyricSynced { get; set; }

    // Constructor that accepts a LyricsInfo.LyricsPhrase object
    public SyncLyricsView(LyricsPhrase? phrase = null, int? nextPhraseTimestampMs = null)
    {

        if (phrase != null)
        {

            TimestampStart = phrase.TimestampStart;
            TimestampEnd = phrase.TimestampEnd;
            Text = phrase.Text;

        }
    }
    public SyncLyricsView(int timestampMs, string text)
    {
        TimestampMs = timestampMs;
        Text = text;
    }
    public SyncLyricsView()
    {

    }
    public SyncLyricsView(SyncLyrics syncLyricsDB)
    {
        Text= syncLyricsDB.Text;
        TimestampMs = syncLyricsDB.TimestampMs;

    }

}
public enum SongType { Track, Segment }
public enum SegmentEndBehavior { Stop, LoopSegment, PlayThrough }