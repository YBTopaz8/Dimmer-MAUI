using Dimmer.Utilities.Extensions;

namespace Dimmer.Data.ModelView.NewFolder;
/// <summary>
/// Provides methods for generating "Top X" ranked lists from play event data.
/// These are designed for creating leaderboards and dynamic ranked collections.
/// </summary>
public static class TopStats
{
    // Define PlayType constants for clarity, matching your model's documentation
    private const int PlayType_Play = 0;
    private const int PlayType_Completed = 3;
    private const int PlayType_Seeked = 4;
    private const int PlayType_Skipped = 5;

    #region --- Core Ranking Methods ---

    /// <summary>
    /// Gets the top songs ranked by a specific event type (e.g., completions, skips).
    /// </summary>
    /// <param name="songs">The collection of all songs.</param>
    /// <param name="events">The collection of all play events.</param>
    /// <param name="count">The number of top songs to return.</param>
    /// <param name="playType">The type of play event to count for ranking. See <see cref="DimmerPlayEvent.PlayType"/>.</param>
    /// <param name="startDate">Optional start date to filter events.</param>
    /// <param name="endDate">Optional end date to filter events.</param>
    /// <returns>A ranked list of songs and their corresponding event counts.</returns>
    public static List<DimmerStats> GetTopSongsByEventType(
        IReadOnlyCollection<SongModel> songs, IReadOnlyCollection<DimmerPlayEvent> events, int count, int playType,
        DateTimeOffset? startDate = null, DateTimeOffset? endDate = null)
    {
        var songLookup = songs.ToDictionary(s => s.Id);
        var filteredEvents = FilterEvents(events, playType, startDate, endDate);

        return [.. filteredEvents
            .Where(e => e.SongId.HasValue)
            .GroupBy(e => e.SongId!.Value)
            .Select(g => new { SongId = g.Key, EventCount = g.Count() })
            .OrderByDescending(x => x.EventCount)
            .Take(count)
            .Where(x => songLookup.ContainsKey(x.SongId)) // Ensure song still exists in library
            .Select(x => (new DimmerStats(){Song=songLookup[x.SongId].ToModelView(),Count=x.EventCount }))];
    }

    /// <summary>
    /// Gets the top artists ranked by a specific event type.
    /// </summary>
    /// <returns>A ranked list of artist names and their corresponding event counts.</returns>
    public static List<DimmerStats> GetTopArtistsByEventType(
        IReadOnlyCollection<SongModel> songs, IReadOnlyCollection<DimmerPlayEvent> events, int count, int playType,
        DateTimeOffset? startDate = null, DateTimeOffset? endDate = null)
    {
        return GetTopRankedByProperty(songs, events, count, playType, s => s.ArtistName, startDate, endDate);
    }

    /// <summary>
    /// Gets the top albums ranked by a specific event type.
    /// </summary>
    /// <returns>A ranked list of album names and their corresponding event counts.</returns>
    public static List<DimmerStats> GetTopAlbumsByEventType(
        IReadOnlyCollection<SongModel> songs, IReadOnlyCollection<DimmerPlayEvent> events, int count, int playType,
        DateTimeOffset? startDate = null, DateTimeOffset? endDate = null)
    {
        return GetTopRankedByProperty(songs, events, count, playType, s => s.AlbumName, startDate, endDate);
    }

    /// <summary>
    /// Gets the top songs ranked by total listening time.
    /// </summary>
    /// <returns>A ranked list of songs and their total listening time in seconds.</returns>
    public static List<DimmerStats> GetTopSongsByListeningTime(
        IReadOnlyCollection<SongModel> songs, IReadOnlyCollection<DimmerPlayEvent> events, int count,
        DateTimeOffset? startDate = null, DateTimeOffset? endDate = null)
    {
        var songLookup = songs.ToDictionary(s => s.Id);
        // For listening time, we don't filter by a specific playType
        var filteredEvents = FilterEvents(events, null, startDate, endDate);

        return [.. filteredEvents
            .Where(e => e.SongId.HasValue && e.DateFinished > e.DatePlayed)
            .GroupBy(e => e.SongId!.Value)
            .Select(g => new
            {
                SongId = g.Key,
                // Sum the duration of each play session
                Time = g.Sum(ev => (ev.DateFinished - ev.DatePlayed).TotalSeconds)
            })
            .OrderByDescending(x => x.Time)
            .Take(count)
            .Where(x => songLookup.ContainsKey(x.SongId))
            .Select(x => (new DimmerStats (){Song= songLookup[x.SongId].ToModelView(), TotalSecondsNumeric=x.Time }))];
    }

    #endregion

    /// <summary>
    /// (Chart: Polar/Radar) Gets the distribution of play events by the hour of the day for a single song.
    /// Insight: "Is this a morning, afternoon, or late-night song for me?"
    /// </summary>
    public static List<DimmerStats> GetPlayDistributionByHour(IReadOnlyCollection<DimmerPlayEvent> songEvents)
    {
        return songEvents
            .Where(e => e.PlayType is PlayType_Play or PlayType_Completed)
            .GroupBy(e => e.EventDate?.Hour)
            .Where(g => g.Key.HasValue)
            .Select(g => new DimmerStats
            {
                Name = $"{g.Key:00}:00", // "09:00"
                Count = g.Count()
            })
            .OrderBy(s => s.Name)
            .ToList();
    }

    /// <summary>
    /// (Chart: Pie/Doughnut) Gets the breakdown of interaction types (Play, Skip, Pause, etc.) for a single song.
    /// Insight: "Do I let this song finish, or do I usually skip or seek through it?"
    /// </summary>
    public static List<DimmerStats> GetPlayTypeDistribution(IReadOnlyCollection<DimmerPlayEvent> songEvents)
    {
        // Simple mapping for display purposes
        var playTypeMap = new Dictionary<int, string>
        {
            {0, "Started"}, {1, "Paused"}, {2, "Resumed"}, {3, "Completed"},
            {4, "Seeked"}, {5, "Skipped"}, {6, "Restarted"}
        };

        var t = songEvents
            .GroupBy(e => e.PlayType)
            .Select(g => new DimmerStats
            {
                Name = playTypeMap.TryGetValue(g.Key, out var name) ? name : $"Type {g.Key}",
                Count = g.Count()
            })
            .OrderByDescending(s => s.Count)
            .ToList();

        return t;
    }

    /// <summary>
    /// (Chart: Column/Bar) Tracks the play count of a single song over time (e.g., by month).
    /// Insight: "Has my interest in this song faded or grown over time?"
    /// </summary>
    public static List<DimmerStats> GetPlayHistoryOverTime(IReadOnlyCollection<DimmerPlayEvent> songEvents)
    {
        return songEvents
            .Where(e => e.PlayType == PlayType_Completed && e.EventDate.HasValue)
            .GroupBy(e => new { e.EventDate!.Value.Year, e.EventDate!.Value.Month })
            .Select(g => new DimmerStats
            {
                Name = $"{g.Key.Year}-{g.Key.Month:D2}", // "2023-11"
                Date = new DateTimeOffset(g.Key.Year, g.Key.Month, 1, 0, 0, 0, TimeSpan.Zero),
                Count = g.Count()
            })
            .OrderBy(s => s.Date)
            .ToList();
    }

    /// <summary>
    /// (Chart: Scatter) Identifies the exact moments in a song where the user skips or seeks away.
    /// Insight: "Do I always skip this song's intro? Where do I get bored?"
    /// </summary>
    public static List<DimmerStats> GetDropOffPoints(IReadOnlyCollection<DimmerPlayEvent> songEvents)
    {
        return songEvents
            .Where(e => e.PlayType == PlayType_Skipped && e.PositionInSeconds > 0)
            .Select(e => new DimmerStats
            {
                Date = e.EventDate ?? DateTimeOffset.MinValue,
                Value = e.PositionInSeconds // X-Axis: Position in song
            })
            .OrderBy(s => s.Date)
            .ToList();
    }

    /// <summary>
    /// (Chart: Radial Bar) Shows which devices are used most often to play this specific song.
    /// Insight: "Is this a 'desktop work' song or a 'phone on-the-go' song?"
    /// </summary>
    public static List<DimmerStats> GetPlayDistributionByDevice(IReadOnlyCollection<DimmerPlayEvent> songEvents)
    {
        return songEvents
            .Where(e => e.PlayType is PlayType_Play or PlayType_Completed && !string.IsNullOrEmpty(e.DeviceName))
            .GroupBy(e => e.DeviceName!)
            .Select(g => new DimmerStats
            {
                Name = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(s => s.Count)
            .ToList();
    }

    /// <summary>
    /// (SURPRISE) (Chart: Single Number/KPI) Calculates the "binge factor" of a song.
    /// Insight: "How often do I play this song multiple times back-to-back?"
    /// </summary>
    /// <returns>A DimmerStats object where Count is the number of back-to-back plays.</returns>
    public static DimmerStats GetBingeFactor(IReadOnlyCollection<DimmerPlayEvent> songEvents, ObjectId songId)
    {
        var orderedEvents = songEvents.OrderBy(e => e.EventDate).ToList();
        int bingeCount = 0;

        for (int i = 0; i < orderedEvents.Count - 1; i++)
        {
            // Check if a 'Completed' event is immediately followed by a 'Play' event for the same song
            if (orderedEvents[i].SongId == songId && orderedEvents[i].PlayType == PlayType_Completed &&
                orderedEvents[i + 1].SongId == songId && orderedEvents[i + 1].PlayType == PlayType_Play)
            {
                bingeCount++;
            }
        }
        return new DimmerStats { Name = "Back-to-Back Plays", Count = bingeCount };
    }

    /// <summary>
    /// (SURPRISE) (Chart: Gauge/Single Number) Calculates the average percentage of the song listened to before stopping.
    /// Insight: "On average, how much of this song do I actually listen to?"
    /// </summary>
    public static DimmerStats GetAverageListenThroughPercent(IReadOnlyCollection<DimmerPlayEvent> songEvents, double songDurationSeconds)
    {
        if (songDurationSeconds <= 0)
            return new DimmerStats { Name = "Listen-Through %", Value = 0 };

        var listenDurations = songEvents
            .Where(e => e.PlayType is PlayType_Completed or PlayType_Skipped && e.PositionInSeconds > 0)
            .Select(e =>
            {
                var listenPercent = (e.PositionInSeconds / songDurationSeconds) * 100.0;
                return Math.Min(listenPercent, 100.0); // Cap at 100%
            });

        if (!listenDurations.Any())
            return new DimmerStats { Name = "Listen-Through %", Value = 0 };

        return new DimmerStats { Name = "Listen-Through %", Value = listenDurations.Average() };
    }




    //================================================================================
    #region --- 2. Global & Comparative Analysis (9 Methods) ---
    //================================================================================

    /// <summary>
    /// (Chart: Bar) Ranks artists by the total unique songs of theirs you have played.
    /// Insight: "Which artists' discographies have I explored the most?"
    /// </summary>
    public static List<DimmerStats> GetTopArtistsBySongVariety(IReadOnlyCollection<DimmerPlayEvent> events, IReadOnlyCollection<SongModel> songs, int count)
    {
        var songLookup = songs.ToDictionary(s => s.Id);
        return events
            .Where(e => e.SongId.HasValue && songLookup.ContainsKey(e.SongId.Value))
            .Select(e => songLookup[e.SongId.Value])
            .GroupBy(s => s.ArtistName)
            .Select(g => new DimmerStats
            {
                Name = g.Key,
                Count = g.Select(s => s.Id).Distinct().Count() // Count unique songs per artist
            })
            .OrderByDescending(s => s.Count)
            .Take(count)
            .ToList();
    }

    /// <summary>
    /// (Chart: Bar) Ranks songs by their "burnout" rate: high plays in the first 30 days, then a significant drop.
    /// Insight: "Which songs did I love intensely but get tired of quickly?"
    /// </summary>
    public static List<DimmerStats> GetTopBurnoutSongs(IReadOnlyCollection<DimmerPlayEvent> events, IReadOnlyCollection<SongModel> songs, int count)
    {
        var songLookup = songs.ToDictionary(s => s.Id);
        return events
            .Where(e => e.SongId.HasValue && songLookup.ContainsKey(e.SongId.Value) && e.PlayType == PlayType_Completed)
            .GroupBy(e => e.SongId!.Value)
            .Select(g =>
            {
                var firstPlay = g.Min(e => e.EventDate);
                if (!firstPlay.HasValue)
                    return null;

                var playsInFirstMonth = g.Count(e => e.EventDate < firstPlay.Value.AddDays(30));
                var totalPlays = g.Count();

                // Avoid division by zero and ensure the song is not new
                if (totalPlays < 5 || playsInFirstMonth == totalPlays)
                    return null;

                return new
                {
                    SongId = g.Key,
                    BurnoutRatio = (double)playsInFirstMonth / totalPlays
                };
            })
            .Where(x => x != null && x.BurnoutRatio > 0.5) // High ratio means most plays were upfront
            .OrderByDescending(x => x!.BurnoutRatio)
            .Take(count)
            .Select(x => new DimmerStats
            {
                Song = songLookup[x!.SongId].ToModelView(),
                Value = x.BurnoutRatio * 100 // As a percentage
            })
            .ToList();
    }

    /// <summary>
    /// (Chart: Stacked Bar) Shows the breakdown of plays per device for your top N artists.
    /// Insight: "Do I listen to Artist A on my desktop and Artist B on my phone?"
    /// </summary>
    public static List<DimmerStats> GetDeviceUsageByTopArtists(IReadOnlyCollection<DimmerPlayEvent> events, IReadOnlyCollection<SongModel> songs, int topArtistCount)
    {
        var songLookup = songs.ToDictionary(s => s.Id);
        var topArtists = events
            .GroupBy(e => e.SongId)
            .Where(g => g.Key.HasValue && songLookup.ContainsKey(g.Key.Value))
            .GroupBy(g => songLookup[g.Key.Value].ArtistName)
            .OrderByDescending(g => g.Count())
            .Take(topArtistCount)
            .Select(g => g.Key)
            .ToHashSet();

        return events
            .Where(e => e.SongId.HasValue && songLookup.ContainsKey(e.SongId.Value) && !string.IsNullOrEmpty(e.DeviceName) && topArtists.Contains(songLookup[e.SongId.Value].ArtistName))
            .GroupBy(e => new { Artist = songLookup[e.SongId.Value].ArtistName, Device = e.DeviceName! })
            .Select(g => new DimmerStats
            {
                Name = g.Key.Artist,
                Category = g.Key.Device, // Use Category for the second grouping key
                Count = g.Count()
            })
            .ToList();
    }

    /// <summary>
    /// (Chart: Bar) Ranks genres by total listening time.
    /// Insight: "Which genre do I spend the most time listening to, regardless of song count?"
    /// </summary>
    public static List<DimmerStats> GetTopGenresByListeningTime(IReadOnlyCollection<DimmerPlayEvent> events, IReadOnlyCollection<SongModel> songs, int count)
    {
        var songLookup = songs.ToDictionary(s => s.Id);
        return events
            .Where(e => e.SongId.HasValue && e.DateFinished > e.DatePlayed && songLookup.ContainsKey(e.SongId.Value) && !string.IsNullOrEmpty(songLookup[e.SongId.Value].Genre.Name))
            .GroupBy(e => songLookup[e.SongId.Value].Genre.Name!)
            .Select(g => new DimmerStats
            {
                Name = g.Key,
                TotalSecondsNumeric = g.Sum(ev => (ev.DateFinished - ev.DatePlayed).TotalSeconds)
            })
            .OrderByDescending(s => s.TotalSecondsNumeric)
            .Take(count)
            .ToList();
    }

    /// <summary>
    /// (SURPRISE) (Chart: Bar) Finds songs that were ignored for a long time and then "rediscovered".
    /// Insight: "What old favorites did I recently get back into?"
    /// </summary>
    public static List<DimmerStats> GetTopRediscoveredSongs(IReadOnlyCollection<DimmerPlayEvent> events, IReadOnlyCollection<SongModel> songs, int count)
    {
        var songLookup = songs.ToDictionary(s => s.Id);
        return events
            .Where(e => e.SongId.HasValue && e.EventDate.HasValue)
            .GroupBy(e => e.SongId.Value)
            .Select(g =>
            {
                var orderedPlays = g.OrderBy(ev => ev.EventDate).ToList();
                if (orderedPlays.Count < 2)
                    return null;

                TimeSpan? longestGap = null;
                DateTime? rediscoveryDate = null;

                for (int i = 0; i < orderedPlays.Count - 1; i++)
                {
                    var gap = orderedPlays[i + 1].EventDate - orderedPlays[i].EventDate;
                    if (gap.HasValue && (!longestGap.HasValue || gap.Value > longestGap.Value))
                    {
                        longestGap = gap;
                        rediscoveryDate = orderedPlays[i + 1].EventDate.Value.DateTime;
                    }
                }

                return new { SongId = g.Key, Gap = longestGap, Date = rediscoveryDate };
            })
            .Where(x => x != null && x.Gap.HasValue && x.Gap.Value.TotalDays > 90) // Minimum 3 months gap
            .OrderByDescending(x => x.Gap.Value)
            .Take(count)
            .Where(x => songLookup.ContainsKey(x.SongId))
            .Select(x => new DimmerStats
            {
                Song = songLookup[x.SongId].ToModelView(),
                Value = x.Gap.Value.TotalDays, // Value represents the gap in days
                Date = x.Date.Value
            })
            .ToList();
    }

    /// <summary>
    /// (SURPRISE) (Chart: Bar/Column) Ranks artists by their "skip rate".
    /// Insight: "Which artists' songs do I tend to skip most often?"
    /// </summary>
    public static List<DimmerStats> GetArtistsByHighestSkipRate(IReadOnlyCollection<DimmerPlayEvent> events, IReadOnlyCollection<SongModel> songs, int count)
    {
        var songLookup = songs.ToDictionary(s => s.Id);
        return events
            .Where(e => e.SongId.HasValue && songLookup.ContainsKey(e.SongId.Value))
            .GroupBy(e => songLookup[e.SongId.Value].ArtistName)
            .Select(g =>
            {
                var totalPlays = g.Count(ev => ev.PlayType == PlayType_Play || ev.PlayType == PlayType_Completed);
                var totalSkips = g.Count(ev => ev.PlayType == PlayType_Skipped);
                if (totalPlays == 0)
                    return null;

                return new
                {
                    ArtistName = g.Key,
                    SkipRate = (double)totalSkips / (totalPlays + totalSkips),
                    PlayCount = totalPlays
                };
            })
            .Where(x => x != null && x.PlayCount > 10) // Only consider artists with a decent number of plays
            .OrderByDescending(x => x.SkipRate)
            .Take(count)
            .Select(x => new DimmerStats
            {
                Name = x.ArtistName,
                Value = x.SkipRate * 100 // As a percentage
            })
            .ToList();
    }



    #region --- Convenience "Top" Methods ---

    public static List<DimmerStats> GetTopCompletedSongs(IReadOnlyCollection<SongModel> s, IReadOnlyCollection<DimmerPlayEvent> e, int count, DateTimeOffset? start = null, DateTimeOffset? end = null)
        => GetTopSongsByEventType(s, e, count, PlayType_Completed, start, end);

    public static List<DimmerStats> GetTopCompletedArtists(IReadOnlyCollection<SongModel> s, IReadOnlyCollection<DimmerPlayEvent> e, int count, DateTimeOffset? start = null, DateTimeOffset? end = null)
        => GetTopArtistsByEventType(s, e, count, PlayType_Completed, start, end);

    public static List<DimmerStats> GetTopCompletedAlbums(IReadOnlyCollection<SongModel> s, IReadOnlyCollection<DimmerPlayEvent> e, int count, DateTimeOffset? start = null, DateTimeOffset? end = null)
        => GetTopAlbumsByEventType(s, e, count, PlayType_Completed, start, end);

    // --- Based on SKIPS ---
    public static List<DimmerStats> GetTopSkippedSongs(IReadOnlyCollection<SongModel> s, IReadOnlyCollection<DimmerPlayEvent> e, int count, DateTimeOffset? start = null, DateTimeOffset? end = null)
        => GetTopSongsByEventType(s, e, count, PlayType_Skipped, start, end);

    public static List<DimmerStats> GetTopSkippedArtists(IReadOnlyCollection<SongModel> s, IReadOnlyCollection<DimmerPlayEvent> e, int count, DateTimeOffset? start = null, DateTimeOffset? end = null)
        => GetTopArtistsByEventType(s, e, count, PlayType_Skipped, start, end);

    // --- Based on SEEKS ---
    public static List<DimmerStats> GetTopSeekedSongs(IReadOnlyCollection<SongModel> s, IReadOnlyCollection<DimmerPlayEvent> e, int count, DateTimeOffset? start = null, DateTimeOffset? end = null)
        => GetTopSongsByEventType(s, e, count, PlayType_Seeked, start, end);

    #endregion

    #region --- Private Helpers ---

    /// <summary>
    /// A generic helper to rank by a string property of SongModelView (e.g., ArtistName, AlbumName).
    /// </summary>
    private static List<DimmerStats> GetTopRankedByProperty(
        IReadOnlyCollection<SongModel> songs, IReadOnlyCollection<DimmerPlayEvent> events, int count, int playType,
        Func<SongModelView, string?> propertySelector, DateTimeOffset? startDate, DateTimeOffset? endDate)
    {
        var songLookup = songs.ToDictionary(s => s.Id);
        var filteredEvents = FilterEvents(events, playType, startDate, endDate);

        return [.. filteredEvents
            .Where(e => e.SongId.HasValue && songLookup.ContainsKey(e.SongId.Value))
            .Select(e => propertySelector(songLookup[e.SongId.Value].ToModelView())) // Get the property (e.g., ArtistName)
            .Where(name => !string.IsNullOrEmpty(name))
            .GroupBy(name => name!)
            .Select(g =>( new DimmerStats(){Name= g.Key, Count= g.Count() }))
            .OrderByDescending(x => x.Count)
            .Take(count)];
    }

    /// <summary>
    /// Centralized logic for filtering events by type and date range.
    /// </summary>
    private static IEnumerable<DimmerPlayEvent> FilterEvents(IReadOnlyCollection<DimmerPlayEvent> events, int? playType, DateTimeOffset? startDate, DateTimeOffset? endDate)
    {
        IEnumerable<DimmerPlayEvent> query = events;

        if (playType.HasValue)
        {
            query = query.Where(e => e.PlayType == playType.Value);
        }
        if (startDate.HasValue)
        {
            query = query.Where(e => e.EventDate >= startDate.Value);
        }
        if (endDate.HasValue)
        {
            // To be inclusive of the end date, we check for less than the *next* day
            query = query.Where(e => e.EventDate < endDate.Value.AddDays(1));
        }
        return query;
    }

    #endregion




    /// <summary>
    /// (SURPRISE) (Chart: Bar/Sankey) For a given list of songs, ranks artists by how often a play of their song
    /// is followed by another play of a song by the same artist.
    /// Insight: "Which artist in this playlist makes me binge their music the most?"
    /// </summary>
    /// <param name="songsInList">The specific subset of songs to analyze.</param>
    /// <param name="allSongs">The entire song library to look up artist info.</param>
    /// <param name="events">All play events.</param>
    /// <param name="count">The number of top artists to return.</param>
    /// <returns>A ranked list of artists and their "loyalty" score.</returns>
    public static List<DimmerStats> GetPlaylistLoyalty(IReadOnlyCollection<SongModel> songsInList, IReadOnlyCollection<SongModel> allSongs, IReadOnlyCollection<DimmerPlayEvent> events, int count)
    {
        var listSongIds = songsInList.Select(s => s.Id).ToHashSet();
        var songLookup = allSongs.ToDictionary(s => s.Id);

        // Get all relevant play events, ordered by date
        var orderedEvents = events
            .Where(e => e.SongId.HasValue && e.PlayType == PlayType_Completed && songLookup.ContainsKey(e.SongId.Value))
            .OrderBy(e => e.EventDate)
            .ToList();

        var artistFollowUps = new Dictionary<string, int>();
        var artistOpportunities = new Dictionary<string, int>();

        for (int i = 0; i < orderedEvents.Count - 1; i++)
        {
            var currentEvent = orderedEvents[i];
            var nextEvent = orderedEvents[i+1];

            // We only care if the *first* song was in our target list.
            if (!currentEvent.SongId.HasValue || !listSongIds.Contains(currentEvent.SongId.Value))
                continue;

            var currentSong = songLookup[currentEvent.SongId.Value];
            if (string.IsNullOrEmpty(currentSong.ArtistName))
                continue;

            // This was an opportunity for a follow-up play.
            artistOpportunities[currentSong.ArtistName] = artistOpportunities.GetValueOrDefault(currentSong.ArtistName, 0) + 1;

            if (nextEvent.SongId.HasValue && songLookup.ContainsKey(nextEvent.SongId.Value))
            {
                var nextSong = songLookup[nextEvent.SongId.Value];
                if (currentSong.ArtistName == nextSong.ArtistName)
                {
                    // Successful follow-up!
                    artistFollowUps[currentSong.ArtistName] = artistFollowUps.GetValueOrDefault(currentSong.ArtistName, 0) + 1;
                }
            }
        }

        return artistOpportunities
            .Where(kvp => kvp.Value > 0) // Where there was at least one opportunity
            .Select(kvp =>
            {
                var artist = kvp.Key;
                var opportunities = kvp.Value;
                var followUps = artistFollowUps.GetValueOrDefault(artist, 0);
                var loyaltyRate = (double)followUps / opportunities * 100.0;
                return new DimmerStats
                {
                    Name = artist,
                    Value = loyaltyRate,
                    Count = followUps
                };
            })
            .OrderByDescending(s => s.Value)
            .Take(count)
            .ToList();
    }


    /// <summary>
    /// (Chart: Bar/Leaderboard) Ranks songs within a specific list by their completion rate ("stickiness").
    /// Insight: "Which songs in this playlist do I let finish most often?"
    /// </summary>
    /// <param name="songsInList">The specific subset of songs to analyze.</param>
    /// <param name="events">All play events.</param>
    /// <returns>A ranked list of songs and their completion percentage.</returns>
    public static List<DimmerStats> RankSongsByStickiness(IReadOnlyCollection<SongModel> songsInList, IReadOnlyCollection<DimmerPlayEvent> events)
    {
        var songIdsInList = songsInList.Select(s => s.Id).ToHashSet();

        return events
            .Where(e => e.SongId.HasValue && songIdsInList.Contains(e.SongId.Value))
            .GroupBy(e => e.SongId.Value)
            .Select(g =>
            {
                // A "start" is a play, a completion, or a skip.
                int starts = g.Count(ev => ev.PlayType is PlayType_Play or PlayType_Completed or PlayType_Skipped);
                int completions = g.Count(ev => ev.PlayType == PlayType_Completed);

                if (starts == 0)
                    return null;

                return new
                {
                    SongId = g.Key,
                    Stickiness = (double)completions / starts * 100.0
                };
            })
            .Where(x => x != null)
            .OrderByDescending(x => x.Stickiness)
            .Join(songsInList, stat => stat.SongId, song => song.Id, (stat, song) => new DimmerStats
            {
                Song = song.ToModelView(),
                Name = song.Title,
                Value = stat.Stickiness
            })
            .ToList();
    }


    /// <summary>
    /// (Chart: Radial Bar/Clock) Finds the most common hour of the day a song is played.
    /// Insight: "Is this my go-to morning song or a late-night anthem?"
    /// </summary>
    /// <returns>A DimmerStats object with the most frequent hour and the play count for that hour.</returns>
    public static DimmerStats GetPowerHour(IReadOnlyCollection<DimmerPlayEvent> songEvents)
    {
        var powerHourStat = GetPlayDistributionByHour(songEvents) // We can reuse your existing method!
            .OrderByDescending(s => s.Count)
            .FirstOrDefault();

        if (powerHourStat == null)
        {
            return new DimmerStats { Name = "Power Hour", Value = -1, StatTitle = "Not enough data" };
        }

        // Example: powerHourStat.Name is "09:00", we want to extract 9.
        int hour = int.Parse(powerHourStat.Name.Split(':')[0]);

        return new DimmerStats
        {
            Name = "Power Hour",
            Value = hour, // The 24-hour format hour
            Count = powerHourStat.Count,
            StatTitle = $"Most often played during the {hour:00}:00 hour."
        };
    }


    /// <summary>
    /// (SURPRISE) (Chart: KPI/Timeline) Calculates the longest consecutive streak of days a song was listened to.
    /// Insight: "What's the longest period I've listened to this song every single day?"
    /// </summary>
    /// <returns>A DimmerStats object with the peak streak count and the date range it occurred.</returns>
    public static DimmerStats GetListeningStreak(IReadOnlyCollection<DimmerPlayEvent> songEvents)
    {
        if (songEvents == null || songEvents.Count==0)
        {
            return new DimmerStats { Name = "Longest Streak", Count = 0 };
        }

        var completionDates = songEvents
            .Where(e => e.PlayType == PlayType_Completed && e.EventDate.HasValue)
            .Select(e => e.EventDate.Value.Date)
            .Distinct()
            .OrderBy(d => d)
            .ToList();

        if (completionDates.Count < 2)
        {
            return new DimmerStats { Name = "Longest Streak", Count = completionDates.Count };
        }

        int longestStreak = 0;
        int currentStreak = 1;
        DateTime streakEndDate = completionDates[0];

        for (int i = 1; i < completionDates.Count; i++)
        {
            if (completionDates[i] == completionDates[i - 1].AddDays(1))
            {
                currentStreak++;
            }
            else
            {
                if (currentStreak > longestStreak)
                {
                    longestStreak = currentStreak;
                    streakEndDate = completionDates[i - 1];
                }
                currentStreak = 1; // Reset streak
            }
        }

        // Final check for the last streak
        if (currentStreak > longestStreak)
        {
            longestStreak = currentStreak;
            streakEndDate = completionDates[^1];
        }

        var streakStartDate = streakEndDate.AddDays(-(longestStreak - 1));

        return new DimmerStats
        {
            Name = "Longest Streak",
            Count = longestStreak,
            Date = streakStartDate, // Start of the best streak
            DateValue = streakEndDate,   // End of the best streak
            StatTitle = $"Listened {longestStreak} days in a row."
        };
    }

    /// <summary>
    /// (SURPRISE) (Chart: CandleStick/Stock) Visualizes a song's play count trajectory over its first 7 days.
    /// Insight: "How did I react to this song in its first week? Was it instant love or a slow burn?"
    /// </summary>
    public static DimmerStats GetSongsFirstImpression(IReadOnlyCollection<DimmerPlayEvent> songEvents, SongModel song)
    {
        var firstPlayDate = songEvents
        .Where(e => e.EventDate.HasValue)
        .Min(e => e.EventDate?.Date);
        if (!firstPlayDate.HasValue)
            return new DimmerStats { StatTitle = "No Play History", Song = song.ToModelView() };

        var firstWeekEvents = songEvents
            .Where(e => e.PlayType == PlayType_Completed && e.EventDate.HasValue &&
                        e.EventDate.Value.Date >= firstPlayDate.Value && e.EventDate.Value.Date < firstPlayDate.Value.AddDays(7))
            .GroupBy(e => e.EventDate!.Value.Date)
            .ToDictionary(g => g.Key, g => g.Count());

        // Get plays for each of the 7 days, defaulting to 0
        var day1Plays = firstWeekEvents.GetValueOrDefault(firstPlayDate.Value, 0);
        var day7Plays = firstWeekEvents.GetValueOrDefault(firstPlayDate.Value.AddDays(6), 0);

        // If no plays in the first week, return a specific status
        if (firstWeekEvents.Count == 0)
            return new DimmerStats { StatTitle = "Not played in its first week", Song = song.ToModelView() };

        var highPlays = firstWeekEvents.Values.Max();
        var lowPlays = firstWeekEvents.Values.Min();

        return new DimmerStats
        {
            Song = song.ToModelView(),
            StatTitle = "First Week Impression",
            Open = day1Plays,     // Day 1 play count
            High = highPlays,     // Peak play count during the week
            Low = lowPlays,       // Lowest play count during the week
            Close = day7Plays,    // Day 7 play count
            Date = firstPlayDate.Value
        };

    }

/// <summary>
/// (Chart: RangeArea) Shows the daily listening window for a song over the last 30 days.
/// Insight: "What is my daily rhythm for this song? Is it a morning-only or all-day track?"
/// </summary>
public static List<DimmerStats> GetDailyListeningRhythm(IReadOnlyCollection<DimmerPlayEvent> songEvents, SongModel song)
{
    var recentEvents = songEvents
        .Where(e => e.EventDate.HasValue && e.EventDate > DateTimeOffset.UtcNow.AddDays(-30));

    return recentEvents
        .GroupBy(e => e.EventDate!.Value.Date)
        .Select(g => new
        {
            Date = g.Key,
            MinHour = g.Min(ev => ev.EventDate!.Value.Hour),
            MaxHour = g.Max(ev => ev.EventDate!.Value.Hour)
        })
        .OrderBy(x => x.Date)
        .Select(x => new DimmerStats
        {
            Song = song.ToModelView(), // Include the song for context
            Date = x.Date,
            Low = x.MinHour,    // Earliest hour played
            High = x.MaxHour   // Latest hour played
        })
        .ToList();

}

    /// <summary>
    /// (Chart: Donut/Pie) Shows the breakdown of plays for a single song by device.
    /// Insight: "Where do I listen to this song most often?"
    /// </summary>
    public static List<DimmerStats> GetSongDeviceBreakdown(IReadOnlyCollection<DimmerPlayEvent> songEvents, SongModel song)
    {
        if (songEvents == null || songEvents.Count==0)
            return new List<DimmerStats>();

        return songEvents
            .Where(e => e.PlayType is PlayType_Play or PlayType_Completed && !string.IsNullOrEmpty(e.DeviceName))
            .GroupBy(e => e.DeviceName!)
            .Select(g => new DimmerStats
            {
                Song = song.ToModelView(), // Pass song for context/navigation
                Name = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(s => s.Count)
            .ToList();
    }


    /// <summary>
    /// (Chart: Bubble) Compares artists by play count vs. song variety, with total listening time as a third dimension.
    /// Insight: "Which artists do I play the most, and which have the deepest discographies in my library?"
    /// </summary>
    public static List<DimmerStats> GetArtistFootprint(IReadOnlyCollection<SongModel> allSongs, IReadOnlyCollection<DimmerPlayEvent> events)
    {
        var songLookup = allSongs.ToDictionary(s => s.Id);

        return events
    .Where(e => e.SongId.HasValue && songLookup.ContainsKey(e.SongId.Value))
    .GroupBy(e => songLookup[e.SongId!.Value].ArtistName)
    .Select(g => new
    {
        ArtistName = g.Key,
        UniqueSongs = g.Select(ev => ev.SongId).Distinct().Count(),
        TotalPlays = g.Count(ev => ev.PlayType == PlayType_Completed),
        // The list of actual songs for this artist for potential navigation
        ContributingSongs = g.Select(ev => songLookup[ev.SongId!.Value].ToModelView()).Distinct().ToList()
    })
    .Select(x => new DimmerStats
    {
        Name = x.ArtistName,
        XValue = x.UniqueSongs,  // X-axis: Variety
        YValue = x.TotalPlays,    // Y-axis: Popularity (Play Count)
        Size = x.ContributingSongs.Count, // Bubble Size: Variety again, for emphasis
        ContributingSongs = x.ContributingSongs // Store the list of songs
    })
    .ToList();

    }


    /// <summary>
    /// (Gamification) (Chart: Bar) Ranks songs by the number of times they were completed, but only if they have never been skipped.
    /// Insight: "Which songs are my true 'perfect plays' that I always let finish?"
    /// </summary>
    public static List<DimmerStats> GetPerfectPlayLeaderboard(IReadOnlyCollection<SongModel> songsInList, IReadOnlyCollection<DimmerPlayEvent> events)
    {
        var songIdsInList = songsInList.Select(s => s.Id).ToHashSet();
        var songLookup = songsInList.ToDictionary(s => s.Id);

        // First, find all songs that have ever been skipped from the list
        var skippedSongIds = events
            .Where(e => e.SongId.HasValue && songIdsInList.Contains(e.SongId.Value) && e.PlayType == PlayType_Skipped)
            .Select(e => e.SongId.Value)
            .ToHashSet();

        // Now, group by song, filter out any that have been skipped, and count completions
        return events
            .Where(e => e.SongId.HasValue && songIdsInList.Contains(e.SongId.Value) && e.PlayType == PlayType_Completed)
            .GroupBy(e => e.SongId.Value)
            .Where(g => !skippedSongIds.Contains(g.Key)) // The "perfect play" filter
            .Select(g => new DimmerStats
            {
                Song = songLookup[g.Key].ToModelView(),
                Name = songLookup[g.Key].Title,
                Count = g.Count()
            })
            .OrderByDescending(s => s.Count)
            .ToList();
    }


}
#endregion