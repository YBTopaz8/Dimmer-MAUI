namespace Dimmer.Utils;



public class StatsRecalculator
{
    private readonly IRealmFactory _realmFactory;
    private readonly ILogger _logger;

    public StatsRecalculator(IRealmFactory realmFactory, ILogger logger)
    {
        _realmFactory = realmFactory;
        _logger = logger;
    }

    public async Task RecalculateAllStatisticsAsync()
    {
        _logger.LogInformation($"{DateTime.Now} Starting recalculation of all statistics...");

        using var realm = _realmFactory.GetRealmInstance();

        await realm.WriteAsync(() =>
        {
            var allSongs = realm.All<SongModel>();
            var allAlbums = realm.All<AlbumModel>();
            var allArtists = realm.All<ArtistModel>();

            //------------------------------------------
            // PHASE 1 — SONG-LEVEL STATISTICS
            //------------------------------------------
            foreach (var song in allSongs)
            {
                
                var historyList = song.PlayHistory.ToList();

                if (historyList.Count > 0)
                {
                    // --- Core Play Counts ---
                    song.PlayCount = historyList.Count;
                    song.PlayCompletedCount = historyList.Count(p => p.PlayType == (int)PlayType.Completed);
                    song.SkipCount = historyList.Count(p => p.PlayType == (int)PlayType.Skipped);
                    song.PauseCount = historyList.Count(p => p.PlayType == (int)PlayType.Pause);
                    song.ResumeCount = historyList.Count(p => p.PlayType == (int)PlayType.Resume);
                    song.SeekCount = historyList.Count(p => p.PlayType == (int)PlayType.Seeked);
                    song.RepeatCount = historyList.Count(p => p.PlayType == (int)PlayType.CustomRepeat);
                    song.PreviousCount = historyList.Count(p => p.PlayType == (int)PlayType.Previous);
                    song.RestartCount = historyList.Count(p => p.PlayType == (int)PlayType.Restarted || p.PlayType == (int)PlayType.SeekRestarted);

                    // --- Rates ---
                    song.ListenThroughRate = song.PlayCount > 0 ? (double)song.PlayCompletedCount / song.PlayCount : 0;
                    song.SkipRate = song.PlayCount > 0 ? (double)song.SkipCount / song.PlayCount : 0;

                    // --- Temporal Data ---
                    var orderedHistory = historyList.OrderBy(p => p.EventDate).ToList();
                    song.FirstPlayed = orderedHistory.FirstOrDefault()?.EventDate ?? DateTimeOffset.MinValue;
                    song.LastPlayed = orderedHistory.LastOrDefault()?.EventDate ?? DateTimeOffset.MinValue;
                    song.DiscoveryDate = song.FirstPlayed;
                    song.LastPlayEventType = orderedHistory.LastOrDefault()?.PlayType ?? -1;

                    // --- Eddington & Streaks ---
                    var completedCounts = historyList.Select(p => p.PlayType == (int)PlayType.Completed ? 1 : 0).ToList();
                    song.EddingtonNumber = CalculateEddingtonNumber(completedCounts);

                    var uniqueDays = historyList.Select(p => p.EventDate.Date).Distinct().OrderBy(d => d).ToList();
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
                    song.PauseCount = 0;
                    song.ResumeCount = 0;
                    song.SeekCount = 0;
                    song.RepeatCount = 0;
                    song.PreviousCount = 0;
                    song.RestartCount = 0;
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
                // Materialize notes to C# RAM so we can string.Join them safely
                var notes = song.UserNotes.ToList();
                song.UserNoteAggregatedText = notes.Count > 0
                    ? string.Join(" ", notes.Select(n => n.UserMessageText))
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

            //------------------------------------------
            // PHASE 2 — ALBUM-LEVEL STATISTICS
            //------------------------------------------
            foreach (var album in allAlbums)
            {
                
                // Materialize the songs once to prevent C++ boundary crossing in the loop
                var songsInAlbum = album.SongsInAlbum?.ToList();

                if (songsInAlbum?.Count > 0)
                {
                    int songCount = songsInAlbum.Count;
                    int playedCount = songsInAlbum.Count(s => s.PlayCompletedCount > 0);
                    album.CompletionPercentage = (double)playedCount / songCount;

                    double totalCompleted = 0, totalListenRate = 0;
                    foreach (var s in songsInAlbum)
                    {
                        totalCompleted += s.PlayCompletedCount;
                        totalListenRate += s.ListenThroughRate;
                    }

                    album.TotalCompletedPlays = (int)totalCompleted;
                    album.AverageSongListenThroughRate = songCount > 0 ? totalListenRate / songCount : 0;
                    album.TotalSkipCount = songsInAlbum.Sum(s => s.SkipCount);
                    album.DiscoveryDate = songsInAlbum.Min(s => s.FirstPlayed);

                    var completedPlaysList = songsInAlbum.Select(s => s.PlayCompletedCount).ToList();
                    album.EddingtonNumber = CalculateEddingtonNumber(completedPlaysList);

                    CalculatePareto(completedPlaysList, out int paretoCount, out double paretoPct);
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

            //------------------------------------------
            // PHASE 3 — ARTIST-LEVEL STATISTICS
            //------------------------------------------
            foreach (var artist in allArtists)
            {
                // Materialize once
                var songs = artist.Songs.ToList();

                if (songs.Count > 0)
                {
                    int songCount = songs.Count;
                    int playedCount = songs.Count(s => s.PlayCompletedCount > 0);
                    artist.CompletionPercentage = (double)playedCount / songCount;

                    double totalCompleted = 0, totalListenRate = 0;
                    foreach (var s in songs)
                    {
                        totalCompleted += s.PlayCompletedCount;
                        totalListenRate += s.ListenThroughRate;
                    }

                    artist.TotalSongsByArtist = songCount;
                    artist.TotalAlbumsByArtist = artist.Albums.Count();
                    artist.TotalCompletedPlays = (int)totalCompleted;
                    artist.AverageSongListenThroughRate = songCount > 0 ? totalListenRate / songCount : 0;

                    var completedPlaysList = songs.Select(s => s.PlayCompletedCount).ToList();
                    artist.EddingtonNumber = CalculateEddingtonNumber(completedPlaysList);
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

            //------------------------------------------
            // PHASE 4 — GLOBAL RANKINGS
            //------------------------------------------


            var sortedSongs = allSongs.ToList().OrderByDescending(s => s.EngagementScore > 0 ? s.EngagementScore : s.PopularityScore).ToList();
            int rank = 1;
            foreach (var song in sortedSongs) song.GlobalRank = rank++;

            var sortedAlbums = allAlbums.ToList().OrderByDescending(a => a.TotalCompletedPlays).ToList();
            rank = 1;
            foreach (var album in sortedAlbums)
            {
                album.OverallRank = rank++;
                int innerRank = 1;

                // Materialize before sorting
                var albumSongs = album.SongsInAlbum?.ToList().OrderByDescending(s => s.EngagementScore).ToList();
                foreach (var s in albumSongs) s.RankInAlbum = innerRank++;
            }

            var sortedArtists = allArtists.ToList().OrderByDescending(a => a.TotalCompletedPlays).ToList();
            rank = 1;
            foreach (var artist in sortedArtists)
            {
                artist.OverallRank = rank++;
                int innerRank = 1;

                // Materialize before sorting
                var artistSongs = artist.Songs.ToList().OrderByDescending(s => s.EngagementScore).ToList();
                foreach (var s in artistSongs) s.RankInArtist = innerRank++;
            }
        });

        _logger.LogInformation($"{DateTime.Now} Recalculation complete.");
    }

    /// <summary>
    /// Calculates the Eddington Number for a list of play counts.
    /// The Eddington number E is the maximum number such that a user has played E songs at least E times.
    /// </summary>
    private int CalculateEddingtonNumber(List<int> playCounts)
    {
        if (playCounts == null || playCounts.Count == 0) return 0;

        var sortedCounts = playCounts.OrderByDescending(c => c).ToList();
        int eddington = 0;
        for (int i = 0; i < sortedCounts.Count; i++)
        {
            if (sortedCounts[i] >= (i + 1)) eddington = i + 1;
            else break;
        }
        return eddington;
    }

    /// <summary>
    /// Identifies the top N% of items that account for M% of the total (Pareto Principle).
    /// Defaults to finding the smallest N% of items that account for >= 80% of plays.
    /// </summary>
    private void CalculatePareto(List<int> playCounts, out int topItemsCount, out double percentageOfTotalPlays, double targetPercentage = 0.80)
    {
        topItemsCount = 0;
        percentageOfTotalPlays = 0;

        if (playCounts == null || playCounts.Count == 0) return;

        var sortedCounts = playCounts.OrderByDescending(c => c).ToList();
        double totalPlays = sortedCounts.Sum();
        if (totalPlays == 0) return;

        double currentPlaysSum = 0;
        for (int i = 0; i < sortedCounts.Count; i++)
        {
            currentPlaysSum += sortedCounts[i];
            topItemsCount = i + 1;
            percentageOfTotalPlays = currentPlaysSum / totalPlays;

            if (percentageOfTotalPlays >= targetPercentage) break;
        }
        percentageOfTotalPlays *= 100; // Convert to actual percentage (0-100)
    }

    /// <summary>
    /// Calculates the maximum streak of consecutive days with an event.
    /// </summary>
    private int CalculateMaxStreak(List<DateTime> uniqueDates)
    {
        if (uniqueDates == null || uniqueDates.Count == 0) return 0;

        int maxStreak = 1;
        int currentStreak = 1;

        for (int i = 1; i < uniqueDates.Count; i++)
        {
            if ((uniqueDates[i] - uniqueDates[i - 1]).TotalDays == 1) currentStreak++;
            else currentStreak = 1;

            maxStreak = Math.Max(maxStreak, currentStreak);
        }
        return maxStreak;
    }
}