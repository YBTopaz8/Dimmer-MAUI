using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Data.ModelView;
public class CollectionStatsSummary
{
    public int TotalSongs { get; set; }
    public int TotalPlayCount { get; set; }
    public int TotalSkipCount { get; set; }
    public int DistinctArtists { get; set; }
    public int DistinctAlbums { get; set; }
    public double AverageDuration { get; set; }
    public double TotalListeningTime { get; set; }
    public int UniqueDevices { get; set; }
    public int SongsWithLyrics { get; set; }
    public int SongsWithSyncedLyrics { get; set; }
    public int SongsPlayedToCompletion { get; set; }
    public int SongsFavorited { get; set; }
    public int MostPlayedSongCount { get; set; }
    public string? MostPlayedSongTitle { get; set; }
    public int MostSkippedSongCount { get; set; }
    public string? MostSkippedSongTitle { get; set; }
    public DateTimeOffset? EarliestAdded { get; set; }
    public DateTimeOffset? LatestAdded { get; set; }
    public double AverageRating { get; set; }
    public double MedianDuration { get; set; }
    public int SongsNeverPlayed { get; set; }
    public int SongsPlayedToday { get; set; }
    public int TotalDaysActive { get; set; }
    public int SongsPlayedAtNight { get; set; }
    public int LongestSongSec { get; set; }
    public int ShortestSongSec { get; set; }
}