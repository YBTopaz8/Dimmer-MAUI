namespace Dimmer.Data.ModelView;
/// <summary>
/// The collection stats summary.
/// </summary>
public partial class CollectionStatsSummary : ObservableObject
{
    /// <summary>
    /// Gets or sets the total songs.
    /// </summary>
    [ObservableProperty] public partial int TotalSongs { get; set; }
    /// <summary>
    /// Gets or sets the total play count.
    /// </summary>
    [ObservableProperty] public partial int TotalPlayCount { get; set; }
    /// <summary>
    /// Gets or sets the total skip count.
    /// </summary>
    [ObservableProperty] public partial int TotalSkipCount { get; set; }
    /// <summary>
    /// Gets or sets the distinct artists.
    /// </summary>
    [ObservableProperty] public partial int DistinctArtists { get; set; }
    /// <summary>
    /// Gets or sets the distinct albums.
    /// </summary>
    [ObservableProperty] public partial int DistinctAlbums { get; set; }
    /// <summary>
    /// Gets or sets the average duration.
    /// </summary>
    [ObservableProperty] public partial double AverageDuration { get; set; }
    /// <summary>
    /// Gets or sets the total listening time.
    /// </summary>
    [ObservableProperty] public partial double TotalListeningTime { get; set; }
    /// <summary>
    /// Gets or sets the unique devices.
    /// </summary>
    [ObservableProperty] public partial int UniqueDevices { get; set; }
    /// <summary>
    /// Gets or sets the songs with lyrics.
    /// </summary>
    [ObservableProperty] public partial int SongsWithLyrics { get; set; }
    /// <summary>
    /// Gets or sets the songs with synced lyrics.
    /// </summary>
    [ObservableProperty] public partial int SongsWithSyncedLyrics { get; set; }
    /// <summary>
    /// Gets or sets the songs played converts to completion.
    /// </summary>
    [ObservableProperty] public partial int SongsPlayedToCompletion { get; set; }
    /// <summary>
    /// Gets or sets the songs favorited.
    /// </summary>
    [ObservableProperty] public partial int SongsFavorited { get; set; }
    /// <summary>
    /// Gets or sets the most played song count.
    /// </summary>
    [ObservableProperty] public partial int MostPlayedSongCount { get; set; }
    /// <summary>
    /// Gets or sets the most played song title.
    /// </summary>
    [ObservableProperty] public partial string? MostPlayedSongTitle { get; set; }
    /// <summary>
    /// Gets or sets the most skipped song count.
    /// </summary>
    [ObservableProperty] public partial int MostSkippedSongCount { get; set; }
    /// <summary>
    /// Gets or sets the most skipped song title.
    /// </summary>
    [ObservableProperty] public partial string? MostSkippedSongTitle { get; set; }
    /// <summary>
    /// Gets or sets the earliest added.
    /// </summary>
    [ObservableProperty] public partial DateTimeOffset? EarliestAdded { get; set; }
    /// <summary>
    /// Gets or sets the latest added.
    /// </summary>
    [ObservableProperty] public partial DateTimeOffset? LatestAdded { get; set; }
    /// <summary>
    /// Gets or sets the average rating.
    /// </summary>
    [ObservableProperty] public partial double AverageRating { get; set; }
    /// <summary>
    /// Gets or sets the median duration.
    /// </summary>
    [ObservableProperty] public partial double MedianDuration { get; set; }
    /// <summary>
    /// Gets or sets the songs never played.
    /// </summary>
    [ObservableProperty] public partial int SongsNeverPlayed { get; set; }
    /// <summary>
    /// Gets or sets the songs played today.
    /// </summary>
    [ObservableProperty] public partial int SongsPlayedToday { get; set; }
    /// <summary>
    /// Gets or sets the total days active.
    /// </summary>
    [ObservableProperty] public partial int TotalDaysActive { get; set; }
    /// <summary>
    /// Gets or sets the songs played at night.
    /// </summary>
    [ObservableProperty] public partial int SongsPlayedAtNight { get; set; }
    /// <summary>
    /// Gets or sets the longest song sec.
    /// </summary>
    [ObservableProperty] public partial int LongestSongSec { get; set; }
    /// <summary>
    /// Gets or sets the shortest song sec.
    /// </summary>
    [ObservableProperty] public partial int ShortestSongSec { get; set; }
}