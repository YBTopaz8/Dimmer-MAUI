
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

    [ObservableProperty]
    public partial int NumberOfTimesFaved { get; set; }

    

    public void SetTitleAndDuration(string title, double duration)
    {
       
        Title = title;
        DurationInSeconds = duration;
        TitleDurationKey = $"{title.ToLowerInvariant().Trim()}|{duration}";
    }
    [ObservableProperty]
    public partial string ArtistName { get; set; } 
    [ObservableProperty]
    public partial ArtistModel Artist { get; set; } 

    [ObservableProperty]
    public partial string AlbumName { get; set; } 

    [ObservableProperty]
    public partial bool HasLyricsColumnIsFiltered { get; set; } 

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

    public string DurationFormatted
    {
        get
        {
            TimeSpan time = TimeSpan.FromSeconds(DurationInSeconds);
            if (time.Hours > 0)
                return time.ToString(@"hh\:mm\:ss");
            else
                return time.ToString(@"mm\:ss");
        }
    }
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
   
    public bool HasSyncedLyrics => SyncLyrics.Length > 0;
    [ObservableProperty]
    public partial string SyncLyrics { get; set; } = string.Empty;

    [ObservableProperty]
    public partial byte[]? CoverImageBytes { get; set; }

    [ObservableProperty]
    public partial bool IsInstrumental { get; set; }
    [ObservableProperty]
    public partial string CoverImagePath { get; set; }
    [ObservableProperty]
    public partial string? UnSyncLyrics { get; set; } = string.Empty;
    //[ObservableProperty]
    //public partial bool IsPlaying { get; set; }
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
    public partial ObservableCollection<PlaylistModelView> PlaylistsHavingSong { get; set; } = new();

    [ObservableProperty]
    public partial ObservableCollection<UserNoteModelView> UserNoteAggregatedCol { get; set; } = new();
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
    [ObservableProperty]
    public partial int PlayCount { get; set; }
    [ObservableProperty]
    public partial int PlayCompletedCount { get; set; }
    [ObservableProperty]
    public partial int SkipCount { get; set; }
    [ObservableProperty]
    public partial DateTimeOffset? LastPlayed { get; set; }
    [ObservableProperty]
    public partial string? SearchableText { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial string? UserNoteAggregatedText { get; private set; }=string.Empty;   


    public void RefreshDenormalizedProperties()
    {

        // 1. Update Play Counts and Last Played
        if (PlayEvents.Any())
        {
            PlayCount = PlayEvents.Count;
            PlayCompletedCount = PlayEvents.Count(p => p.PlayType == (int)PlayType.Completed);
            NumberOfTimesFaved= PlayEvents.Count(p => p.PlayType == (int)PlayType.Favorited);
            var lastPlayEvent = PlayEvents
                .Where(p => p.PlayType == (int)PlayType.Completed)
                .OrderByDescending(p => p.EventDate)
                .FirstOrDefault();
            if (lastPlayEvent is not null)
            {
                LastPlayed = lastPlayEvent.EventDate.Value;
            }

            SkipCount = PlayEvents?.Count(x => x.PlayType == (int)PlayType.Skipped) ?? 0;
        }
        else
        {
            PlayCount = 0;
            PlayCompletedCount = 0;
            LastPlayed = DateTimeOffset.MinValue;
        }

        // 2. Update Aggregated Notes
        if (UserNoteAggregatedCol.Any())
        {
            UserNoteAggregatedText = string.Join(" ", UserNoteAggregatedCol.Select(n => n.UserMessageText));
        }
        else
        {
            UserNoteAggregatedCol = null;
        }



        // 3. Update the main SearchableText field
        var sb = new StringBuilder();
        sb.Append(Title).Append(' ');
        sb.Append(OtherArtistsName).Append(' ');
        sb.Append(AlbumName).Append(' ');
        sb.Append(GenreName).Append(' ');
        sb.Append(SyncLyrics).Append(' ');
        sb.Append(UnSyncLyrics).Append(' ');
        sb.Append(Composer).Append(' ');
        sb.Append(UserNoteAggregatedText); // Include the notes in the "any" search

        SearchableText = sb.ToString().ToLowerInvariant();
    }
    public SongModelView()
    {
        PlayEvents.CollectionChanged += (s, e) => UpdateSkipCount();
    }
    private void UpdateSkipCount()
    {
    }




    public override int GetHashCode()
    {
        return HashCode.Combine(Id);
    }

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
    public partial Color? CurrentPlaySongDominantColor { get;  set; }

    [ObservableProperty]
    public partial bool IsHidden { get;  set; }
    public string CoverArtHash { get; set; }
    [ObservableProperty]
    public partial ObservableCollection<LyricPhraseModelView> EmbeddedSync { get;  set; }
    [ObservableProperty]
    public partial double ListenThroughRate { get; set; }


    //[ObservableProperty]
    //public partial double CompletionPercentage { get; set; }

    [ObservableProperty]
    public partial double SkipRate { get; set; }
    [ObservableProperty]
    public partial int GlobalRank { get; set; }
    [ObservableProperty]
    public partial double PopularityScore { get; set; }
    [ObservableProperty]
    public partial DateTimeOffset FirstPlayed { get; set; }
    //[ObservableProperty]
    //public partial int TotalCompletedPlays { get; set; }
    [ObservableProperty]
    public partial int RankInAlbum { get; set; }
    [ObservableProperty]
    public partial int RankInArtist { get; set; }

    [ObservableProperty]
    public partial int PauseCount { get;  set; }
    [ObservableProperty]
    public partial int ResumeCount { get;  set; }
    [ObservableProperty]
    public partial int SeekCount { get;  set; }
    [ObservableProperty]
    public partial int LastPlayEventType { get;  set; }
    [ObservableProperty]
    public partial int PlayStreakDays { get;  set; }
    [ObservableProperty]
    public partial int EddingtonNumber { get;  set; }
    [ObservableProperty]
    public partial double EngagementScore { get;  set; }
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

public enum SongType { Track, Segment }
public enum SegmentEndBehavior { Stop, LoopSegment, PlayThrough }