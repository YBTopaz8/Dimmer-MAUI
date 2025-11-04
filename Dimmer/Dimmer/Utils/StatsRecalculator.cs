using Hqub.Lastfm.Entities;

using Realms;

namespace Dimmer.Utils;
public class StatsRecalculator
{
    IRealmFactory realmFactory;
    [ThreadStatic] private static Realm? _realm;

    private readonly ILogger _logger;

    public StatsRecalculator(IRealmFactory _realmFactory, ILogger logger)
    {
        realmFactory=_realmFactory;
        _logger = logger;
    }

    public void RecalculateAllStatistics()
    {
        var sw = Stopwatch.StartNew();
        _logger.LogInformation($"{DateTime.Now} Starting recalculation of all statistics...");
        _realm ??= realmFactory.GetRealmInstance();
        var allSongs = _realm.All<SongModel>().ToList();
        var allAlbums = _realm.All<AlbumModel>().ToList();
        var allArtists = _realm.All<ArtistModel>().ToList();

        //------------------------------------------
        // PHASE 1 — SONG-LEVEL STATISTICS
        //------------------------------------------
        foreach (var chunk in allSongs.Chunk(500))
        {
            _realm.Write(() =>
            {
                foreach (var song in chunk)
                {
                    if (song.PlayHistory?.Any() == true)
                    {
                        // --- Core Play Counts ---
                        song.PlayCount = song.PlayHistory.Count;
                        song.PlayCompletedCount = song.PlayHistory.Count(p => p.PlayType == (int)PlayType.Completed);
                        song.SkipCount = song.PlayHistory.Count(p => p.PlayType == (int)PlayType.Skipped);
                        song.PauseCount = song.PlayHistory.Count(p => p.PlayType == (int)PlayType.Pause);
                        song.ResumeCount = song.PlayHistory.Count(p => p.PlayType == (int)PlayType.Resume);
                        song.SeekCount = song.PlayHistory.Count(p => p.PlayType == (int)PlayType.Seeked || p.PlayType == (int)PlayType.SeekRestarted);
                        song.RepeatCount = song.PlayHistory.Count(p => p.PlayType == (int)PlayType.CustomRepeat);
                        song.PreviousCount = song.PlayHistory.Count(p => p.PlayType == (int)PlayType.Previous);
                        song.RestartCount = song.PlayHistory.Count(p => p.PlayType == (int)PlayType.Restarted);

                        // --- Rates ---
                        song.ListenThroughRate = song.PlayCount > 0 ? (double)song.PlayCompletedCount / song.PlayCount : 0;
                        song.SkipRate = song.PlayCount > 0 ? (double)song.SkipCount / song.PlayCount : 0;

                        // --- Temporal Data ---
                        var ordered = song.PlayHistory.OrderBy(p => p.EventDate);
                        song.FirstPlayed = ordered.FirstOrDefault()?.EventDate ?? DateTimeOffset.MinValue;
                        song.LastPlayed = ordered.LastOrDefault()?.EventDate ?? DateTimeOffset.MinValue;
                        song.DiscoveryDate = song.FirstPlayed;
                        song.LastPlayEventType = ordered.LastOrDefault()?.PlayType ?? -1;

                        // --- Eddington & Streaks ---
                        song.EddingtonNumber = CalculateEddingtonNumber(song.PlayHistory
                            .Select(p => p.PlayType == (int)PlayType.Completed ? 1 : 0)
                            .ToList());

                        var uniqueDays = song.PlayHistory
                            .Select(p => p.EventDate.Date)
                            .Distinct()
                            .OrderBy(d => d)
                            .ToList();

                        song.PlayStreakDays = CalculateMaxStreak(uniqueDays);

                        // --- Engagement Score ---
                        double engagementScore = 0;
                        engagementScore += song.PlayCompletedCount * 3.0;
                        engagementScore -= song.SkipCount * 1.5;
                        engagementScore += song.ListenThroughRate * 10.0;
                        engagementScore += (song.TotalPlayDurationSeconds / 60.0) * 0.1;
                        if (song.IsFavorite) engagementScore += 20;

                        if (song.LastPlayed != DateTimeOffset.MinValue)
                        {
                            var daysSince = (DateTimeOffset.UtcNow - song.LastPlayed).TotalDays;
                            engagementScore *= Math.Max(0.1, 1.0 - (daysSince / 365.0)); // 1-year decay
                        }

                        song.EngagementScore = Math.Max(0, engagementScore);
                        song.NumberOfTimesFaved = song.ManualFavoriteCount + (song.PlayCompletedCount / 4);
                    }
                    else
                    {
                        // --- No Play History ---
                        song.PlayCount = 0;
                        song.PlayCompletedCount = 0;
                        song.SkipCount = 0;
                        song.ListenThroughRate = 0;
                        song.SkipRate = 0;
                        song.FirstPlayed = DateTimeOffset.MinValue;
                        song.LastPlayed = DateTimeOffset.MinValue;
                        song.DiscoveryDate = DateTimeOffset.MinValue;
                        song.TotalPlayDurationSeconds = 0;
                        song.EddingtonNumber = 0;
                        song.PlayStreakDays = 0;
                        song.LastPlayEventType = -1;
                        song.EngagementScore = 0;
                        song.NumberOfTimesFaved = song.ManualFavoriteCount;
                    }

                    // --- Popularity Score ---
                    song.PopularityScore = (song.PlayCompletedCount * 1.5) - (song.SkipCount * 0.5) + song.PlayCount;
                    if (song.IsFavorite) song.PopularityScore += 50;

                    // --- Has Synced Lyrics ---
                    song.HasSyncedLyrics = !string.IsNullOrEmpty(song.SyncLyrics);

                    // --- Aggregated Notes ---
                    song.UserNoteAggregatedText = song.UserNotes?.Any() == true
                        ? string.Join(" ", song.UserNotes.Select(n => n.UserMessageText))
                        : null;

                    // --- Searchable Text ---
                    var sb = new StringBuilder();
                    sb.Append(song.Title ?? "").Append(' ')
                      .Append(song.OtherArtistsName ?? "").Append(' ')
                      .Append(song.AlbumName ?? "").Append(' ')
                      .Append(song.GenreName ?? "").Append(' ')
                      .Append(song.SyncLyrics ?? "").Append(' ')
                      .Append(song.UnSyncLyrics ?? "").Append(' ')
                      .Append(song.Composer ?? "").Append(' ')
                      .Append(song.UserNoteAggregatedText ?? "");
                    song.SearchableText = sb.ToString().ToLowerInvariant();
                }
            });


            //------------------------------------------
            // PHASE 2 — ALBUM-LEVEL STATISTICS
            //------------------------------------------
            _realm.Write(() =>
            {
                foreach (var album in allAlbums)
                {
                    var songsInAlbum = album.SongsInAlbum;
                    if (songsInAlbum?.Any() == true)
                    {
                        int songCount = songsInAlbum.Count();
                        int playedCount = songsInAlbum.Filter("PlayCompletedCount > 0").Count();
                        album.CompletionPercentage = (double)playedCount / songCount;

                        double totalCompleted = 0, totalListenRate = 0;
                        foreach (var s in songsInAlbum)
                        {
                            totalCompleted += s.PlayCompletedCount;
                            totalListenRate += s.ListenThroughRate;
                        }

                        album.TotalCompletedPlays = (int)totalCompleted;
                        album.AverageSongListenThroughRate = songCount > 0 ? totalListenRate / songCount : 0;
                        album.TotalSkipCount = songsInAlbum.ToArray().Sum(s => s.SkipCount);
                        album.DiscoveryDate = songsInAlbum.ToArray().Min(s => s.FirstPlayed);

                        album.EddingtonNumber = CalculateEddingtonNumber(songsInAlbum.ToList().Select(s => s.PlayCompletedCount).ToList());

                        CalculatePareto(songsInAlbum.ToList().Select(s => s.PlayCompletedCount).ToList(), out int paretoCount, out double paretoPct);
                        album.ParetoTopSongsCount = paretoCount;
                        album.ParetoPercentage = paretoPct;
                    }
                    else
                    {
                        album.CompletionPercentage = 0;
                        album.TotalCompletedPlays = 0;
                        album.TotalPlayDurationSeconds = 0;
                        album.AverageSongListenThroughRate = 0;
                        album.TotalSkipCount = 0;
                        album.DiscoveryDate = DateTimeOffset.MinValue;
                        album.EddingtonNumber = 0;
                        album.ParetoTopSongsCount = 0;
                        album.ParetoPercentage = 0;
                    }
                }
            });

            //------------------------------------------
            // PHASE 3 — ARTIST-LEVEL STATISTICS
            //------------------------------------------
            _realm.Write(() =>
            {
                foreach (var artist in allArtists)
                {
                    var songs = artist.Songs;
                    if (songs?.Any() == true)
                    {
                        int songCount = songs.Count();
                        int playedCount = songs.Filter("PlayCompletedCount > 0").Count();
                        artist.CompletionPercentage = (double)playedCount / songCount;

                        double totalCompleted = 0, totalListenRate = 0;
                        foreach (var s in songs)
                        {
                            totalCompleted += s.PlayCompletedCount;
                            totalListenRate += s.ListenThroughRate;
                        }

                        artist.TotalCompletedPlays = (int)totalCompleted;
                        artist.AverageSongListenThroughRate = totalListenRate / songCount;
                        artist.EddingtonNumber = CalculateEddingtonNumber(songs.ToList().Select(s => s.PlayCompletedCount).ToList());
                    }
                    else
                    {
                        artist.CompletionPercentage = 0;
                        artist.TotalCompletedPlays = 0;
                        artist.AverageSongListenThroughRate = 0;
                        artist.TotalSkipCount = 0;
                        artist.DiscoveryDate = DateTimeOffset.MinValue;
                        artist.EddingtonNumber = 0;
                        artist.ParetoTopSongsCount = 0;
                        artist.ParetoPercentage = 0;
                    }
                }
            });

            //------------------------------------------
            // PHASE 4 — GLOBAL RANKINGS
            //------------------------------------------
            _realm.Write(() =>
            {
                // --- Global Song Ranking ---
                int rank = 1;
                foreach (var song in allSongs.OrderByDescending(s => s.EngagementScore > 0 ? s.EngagementScore : s.PopularityScore))
                    song.GlobalRank = rank++;

                // --- Album Ranks + Per-Song Album Rank ---
                rank = 1;
                foreach (var album in allAlbums.OrderByDescending(a => a.TotalCompletedPlays))
                {
                    album.OverallRank = rank++;
                    int innerRank = 1;

                    var songsInAlbum = album.SongsInAlbum?
                        .OrderByDescending(s => s.EngagementScore)
                        .ToList();

                    if (songsInAlbum != null)
                    {
                        foreach (var s in songsInAlbum)
                            s.RankInAlbum = innerRank++;
                    }
                }

                // --- Artist Ranks + Per-Song Artist Rank ---
                rank = 1;
                foreach (var artist in allArtists.OrderByDescending(a => a.TotalCompletedPlays))
                {
                    artist.OverallRank = rank++;
                    int innerRank = 1;

                    var songsByArtist = artist.Songs?
                        .OrderByDescending(s => s.EngagementScore)
                        .ToList();

                    if (songsByArtist != null)
                    {
                        foreach (var s in songsByArtist)
                            s.RankInArtist = innerRank++;
                    }
                }

            });
        }
    }

    /// <summary>
    /// Calculates the Eddington Number for a list of play counts.
    /// The Eddington number E is the maximum number such that a user has played E songs at least E times.
    /// </summary>
    /// <param name="playCounts">A list of play counts for individual items (e.g., songs).</param>
    /// <returns>The Eddington Number.</returns>
    private int CalculateEddingtonNumber(List<int> playCounts)
    {
        if (playCounts == null || playCounts.Count==0)
            return 0;

        var sortedCounts = playCounts.OrderByDescending(c => c).ToList();
        int eddington = 0;
        for (int i = 0; i < sortedCounts.Count; i++)
        {
            // If the (i+1)-th highest play count is >= (i+1), then we can achieve an E of (i+1)
            // Example: 1st song played >= 1 time, 2nd song played >= 2 times, etc.
            if (sortedCounts[i] >= (i + 1))
            {
                eddington = i + 1;
            }
            else
            {
                break; // No further E can be achieved
            }
        }
        return eddington;
    }

    /// <summary>
    /// Identifies the top N% of items that account for M% of the total (Pareto Principle).
    /// Defaults to finding the smallest N% of items that account for >= 80% of plays.
    /// </summary>
    /// <param name="playCounts">A list of play counts for individual items.</param>
    /// <param name="topItemsCount">Output: The number of items contributing to the Pareto principle.</param>
    /// <param name="percentageOfTotalPlays">Output: The percentage of total plays accounted for by these items.</param>
    /// <param name="targetPercentage">The target percentage of total plays to reach (e.g., 80 for 80/20 rule).</param>
    private void CalculatePareto(List<int> playCounts, out int topItemsCount, out double percentageOfTotalPlays, double targetPercentage = 0.80)
    {
        topItemsCount = 0;
        percentageOfTotalPlays = 0;

        if (playCounts == null || playCounts.Count==0)
            return;

        var sortedCounts = playCounts.OrderByDescending(c => c).ToList();
        double totalPlays = sortedCounts.Sum();
        if (totalPlays == 0)
            return;

        double currentPlaysSum = 0;
        for (int i = 0; i < sortedCounts.Count; i++)
        {
            currentPlaysSum += sortedCounts[i];
            topItemsCount = i + 1;
            percentageOfTotalPlays = currentPlaysSum / totalPlays;

            if (percentageOfTotalPlays >= targetPercentage)
            {
                break;
            }
        }
        percentageOfTotalPlays *= 100; // Convert to actual percentage (0-100)
    }

    /// <summary>
    /// Calculates the maximum streak of consecutive days with an event.
    /// </summary>
    /// <param name="uniqueDates">A sorted list of unique dates (Date-only) where events occurred.</param>
    /// <returns>The maximum consecutive streak in days.</returns>
    private int CalculateMaxStreak(List<DateTime> uniqueDates)
    {
        if (uniqueDates == null || uniqueDates.Count == 0)
            return 0;

        int maxStreak = 1;
        int currentStreak = 1;

        for (int i = 1; i < uniqueDates.Count; i++)
        {
            if ((uniqueDates[i] - uniqueDates[i - 1]).TotalDays == 1)
            {
                currentStreak++;
            }
            else
            {
                currentStreak = 1;
            }
            maxStreak = Math.Max(maxStreak, currentStreak);
        }
        return maxStreak;
    }
}