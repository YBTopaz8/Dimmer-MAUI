namespace Dimmer.Utils;
public class StatsRecalculator
{
    private readonly Realm _realm;
    private readonly ILogger _logger;

    public StatsRecalculator(Realm realm, ILogger logger)
    {
        _realm = realm;
        _logger = logger;
    }

    public void RecalculateAllStatistics()
    {
        var sw = Stopwatch.StartNew();
        _logger.LogInformation($"{DateTime.Now} Starting recalculation of all statistics...");

        var allSongsCached = _realm.All<SongModel>().ToList();
        var allAlbums = _realm.All<AlbumModel>().ToList();
        var allArtists = _realm.All<ArtistModel>().ToList();

        foreach (var chunk in allSongsCached.Chunk(500))
        {

        
            _realm.Write(() =>
            {
                // --- Phase 1: Update Song-level Statistics ---
                foreach (var song in chunk)
                {
                    // Ensure a song always has a PlayHistory object initialized if needed,
                    // or handle nulls defensively. Here, we assume PlayHistory can be null/empty.
                    if (song.PlayHistory?.Any() == true)
                    {
                        // 1. Core Play Counts and Rates
                        // Use the enum values directly for clarity
                        song.PlayCount = song.PlayHistory.Count;
                        song.PlayCompletedCount = song.PlayHistory.Count(p => p.PlayType == (int)PlayType.Completed);
                        song.SkipCount = song.PlayHistory.Count(p => p.PlayType == (int)PlayType.Skipped);
                        song.PauseCount = song.PlayHistory.Count(p => p.PlayType == (int)PlayType.Pause);
                        song.ResumeCount = song.PlayHistory.Count(p => p.PlayType == (int)PlayType.Resume);
                        song.SeekCount = song.PlayHistory.Count(p => p.PlayType == (int)PlayType.Seeked || p.PlayType == (int)PlayType.SeekRestarted);
                        song.RepeatCount = song.PlayHistory.Count(p => p.PlayType == (int)PlayType.CustomRepeat);
                        song.PreviousCount = song.PlayHistory.Count(p => p.PlayType == (int)PlayType.Previous);
                        song.RestartCount = song.PlayHistory.Count(p => p.PlayType == (int)PlayType.Restarted);


                        if (song.PlayCount > 0)
                        {
                            song.ListenThroughRate = (double)song.PlayCompletedCount / song.PlayCount;
                            song.SkipRate = (double)song.SkipCount / song.PlayCount;
                        }
                        else
                        {
                            song.ListenThroughRate = 0;
                            song.SkipRate = 0;
                        }

                        // --- First and Last Played Dates (any interaction) ---
                        var allPlaysOrdered = song.PlayHistory.OrderBy(p => p.EventDate);
                        song.FirstPlayed = allPlaysOrdered.FirstOrDefault()?.EventDate ?? DateTimeOffset.MinValue;
                        song.LastPlayed = allPlaysOrdered.LastOrDefault()?.EventDate ?? DateTimeOffset.MinValue;
                        song.DiscoveryDate = song.FirstPlayed; // Alias for clarity

                        // Last event type
                        song.LastPlayEventType = allPlaysOrdered.LastOrDefault()?.PlayType ?? -1;

                  

                        // --- Eddington Number for Song ---
                        song.EddingtonNumber = CalculateEddingtonNumber(song.PlayHistory.Select(p => p.PlayType == (int)PlayType.Completed ? 1 : 0).ToList());

                        // --- Play Streak (Simplified: Days with at least one play) ---
                        var uniquePlayDays = song.PlayHistory
                                                .Select(p => p.EventDate.Date)
                                                .Distinct()
                                                .OrderBy(d => d)
                                                .ToList();
                        song.PlayStreakDays = CalculateMaxStreak(uniquePlayDays);

                        // --- Engagement Score (More sophisticated than simple PopularityScore) ---
                        // This is a customizable formula. Adjust weights as desired.
                        double engagementScore = 0;
                        engagementScore += song.PlayCompletedCount * 3.0; // Strong bonus for completing
                        engagementScore -= song.SkipCount * 1.5;         // Penalty for skipping
                        //engagementScore += song.RepeatCount * 2.0;       // Bonus for repeating
                        engagementScore += song.ListenThroughRate * 10.0; // Strong bonus for high listen-through rate
                        engagementScore += (song.TotalPlayDurationSeconds / 60.0) * 0.1; // Small bonus for total minutes listened
                        if (song.IsFavorite)
                        {
                            engagementScore += 20; // Bonus for explicit favorite
                        }
                        // Add a recency bias: more recent plays matter more
                        if (song.LastPlayed != DateTimeOffset.MinValue)
                        {
                            var daysSinceLastPlay = (DateTimeOffset.UtcNow - song.LastPlayed).TotalDays;
                            engagementScore *= Math.Max(0.1, 1.0 - (daysSinceLastPlay / 365.0)); // Decay over a year
                        }
                        engagementScore = Math.Max(0, engagementScore);

                        int autoFavs = song.PlayCompletedCount / 4;
                        song.NumberOfTimesFaved = song.ManualFavoriteCount + autoFavs;
                        song.EngagementScore = engagementScore;

                    }
                    else // No play history for the song
                    {
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

                    // Old PopularityScore (can keep or replace with EngagementScore)
                    // If keeping, consider using the more refined counts
                    song.PopularityScore = (song.PlayCompletedCount * 1.5) - (song.SkipCount * 0.5) + song.PlayCount;
                    if (song.IsFavorite)
                    {
                        song.PopularityScore += 50; // Big bonus for being a favorite
                    }


                    song.HasSyncedLyrics = !string.IsNullOrEmpty(song.SyncLyrics);


                    // 2. Update Aggregated Notes
                    song.UserNoteAggregatedText = song.UserNotes?.Any() == true 
                    ? string.Join(" ", song.UserNotes.Select(n => n.UserMessageText))
                    : null;


                    // 3. Update the main SearchableText field
                    // Only update if it's null or empty, or if you want to force an update every time.
                    // For performance, updating only if changed is better. For robustness, always update.
                    // If (!string.IsNullOrEmpty(song.SearchableText)) continue; // Remove this if you want to always update
                    var sb = new StringBuilder();
                    sb.Append(song.Title ?? "").Append(' ');
                    sb.Append(song.OtherArtistsName ?? "").Append(' ');
                    sb.Append(song.AlbumName ?? "").Append(' ');
                    sb.Append(song.GenreName ?? "").Append(' ');
                    sb.Append(song.SyncLyrics ?? "").Append(' ');
                    sb.Append(song.UnSyncLyrics ?? "").Append(' ');
                    sb.Append(song.Composer ?? "").Append(' ');
                    sb.Append(song.UserNoteAggregatedText ?? ""); // Include the notes in the "any" search

                    song.SearchableText = sb.ToString().ToLowerInvariant();
                }

                // --- Phase 2: Update Album-level Statistics ---
                foreach (var album in allAlbums)
                {
                    var songsInAlbum = album.SongsInAlbum; // Assuming this is a backlink/linkingobjects field

                    if (songsInAlbum?.Any() == true)
                    {
                        int songCount = songsInAlbum.Count();

                        // Aggregate from songsint songCount = songsInAlbum.Count(); // .Count() is supported and fast
                        if (songCount > 0)
                        {
                            int playedSongsCount = songsInAlbum.Filter("PlayCompletedCount > 0").Count();
                            album.CompletionPercentage = (double)playedSongsCount / songCount;

                            // Manual, memory-efficient aggregation
                            double totalCompletedPlays = 0;
                            double totalListenThroughRate = 0;
                            foreach (var song in songsInAlbum) // This iterates efficiently
                            {
                                totalCompletedPlays += song.PlayCompletedCount;
                                totalListenThroughRate += song.ListenThroughRate;
                            }

                            album.TotalCompletedPlays = (int)totalCompletedPlays; 
                        
                            album.AverageSongListenThroughRate = songCount > 0 ? totalListenThroughRate / songCount : 0;
                        }
                        album.TotalSkipCount = songsInAlbum.Sum(s => s.SkipCount);  //TODO; REDO IN REALM

                        // Discovery Date (earliest first play of any song in the album)
                        album.DiscoveryDate = songsInAlbum.Min(s => s.FirstPlayed);

                        // Eddington Number for Album (based on completed plays of its songs)
                        album.EddingtonNumber = CalculateEddingtonNumber(songsInAlbum.ToList().Select(s => s.PlayCompletedCount).ToList());

                        // Pareto Principle for Album
                        CalculatePareto(songsInAlbum.ToList().Select(s => s.PlayCompletedCount).ToList(), out int paretoCount, out double paretoPlaysPercentage);
                        album.ParetoTopSongsCount = paretoCount;
                        album.ParetoPercentage = paretoPlaysPercentage; // Percentage of total plays accounted for by top X songs

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

                // --- Phase 3: Update Artist-level Statistics ---
                foreach (var artist in allArtists)
                {
                    var songsByArtist = artist.Songs; // Assuming this is a backlink/linkingobjects field

                    if (songsByArtist?.Any() == true)
                    {
                        int songCount = songsByArtist.Count();
                        if (songCount > 0)
                        {
                            int playedSongsCount = songsByArtist.Filter("PlayCompletedCount > 0").Count();
                            artist.CompletionPercentage = (double)playedSongsCount / songCount;

                            // Manual, memory-efficient aggregation
                            double totalCompletedPlays = 0;
                            double totalListenThroughRate = 0;
                            foreach (var song in songsByArtist) // This iterates efficiently
                            {
                                totalCompletedPlays += song.PlayCompletedCount;
                                totalListenThroughRate += song.ListenThroughRate;
                            }

                            artist.TotalCompletedPlays = (int)totalCompletedPlays;
                            artist.AverageSongListenThroughRate = totalListenThroughRate / songCount;
                        }
                        //artist.TotalSkipCount = songsByArtist.Sum(s => s.SkipCount);

                        // Discovery Date
                        //artist.DiscoveryDate = songsByArtist.Min(s => s.FirstPlayed);

                        // Eddington Number for Artist
                        artist.EddingtonNumber = CalculateEddingtonNumber(songsByArtist.ToList().Select(s => s.PlayCompletedCount).ToList());

                        // Pareto Principle for Artist
                        ////CalculatePareto(songsByArtist.Select(s => s.PlayCompletedCount).ToList(), out int paretoCount, out double paretoPlaysPercentage);
                        //artist.ParetoTopSongsCount = paretoCount;
                        //artist.ParetoPercentage = paretoPlaysPercentage;
                    }
                    else
                    {
                        artist.CompletionPercentage = 0;
                        artist.TotalCompletedPlays = 0;
                        //artist.TotalPlayDurationSeconds = 0;
                        artist.AverageSongListenThroughRate = 0;
                        artist.TotalSkipCount = 0;
                        artist.DiscoveryDate = DateTimeOffset.MinValue;
                        artist.EddingtonNumber = 0;
                        artist.ParetoTopSongsCount = 0;
                        artist.ParetoPercentage = 0;
                    }
                }

         
                // --- Phase 5: Global and Category-Specific Ranking ---
                // Global Song Rank (Using EngagementScore for a more meaningful rank)
                int rank = 1;
                var sortedSongs = allSongsCached.OrderByDescending(s => s.EngagementScore > 0 ? s.EngagementScore : s.PopularityScore); // Fallback to PopularityScore
                foreach (var song in sortedSongs)
                {
                    song.GlobalRank = rank++;
                }

                // Album Ranks & In-Album Song Ranks
                rank = 1;
                var sortedAlbums = allAlbums.OrderByDescending(a => a.TotalCompletedPlays);
                foreach (var album in sortedAlbums)
                {
                    album.OverallRank = rank++;
                    int albumSongRank = 1;
                    var sortedSongsInAlbum = album.SongsInAlbum?.OrderByDescending(s => s.EngagementScore); // Rank within album by engagement
                    if (sortedSongsInAlbum != null)
                    {
                        foreach (var song in sortedSongsInAlbum)
                        {
                            song.RankInAlbum = albumSongRank++;
                        }
                    }
                }

                // Artist Ranks & In-Artist Song Ranks
                rank = 1;
                var sortedArtists = allArtists.OrderByDescending(a => a.TotalCompletedPlays);
                foreach (var artist in sortedArtists)
                {
                    artist.OverallRank = rank++;
                    int artistSongRank = 1;
                    var sortedSongsByArtist = artist.Songs?.OrderByDescending(s => s.EngagementScore); // Rank within artist by engagement
                    if (sortedSongsByArtist != null)
                    {
                        foreach (var song in sortedSongsByArtist)
                        {
                            song.RankInArtist = artistSongRank++;
                        }
                    }
                }

           
            });
        }

        sw.Stop();
        _logger.LogInformation("Finished recalculating all statistics.");
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