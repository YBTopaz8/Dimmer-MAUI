namespace Dimmer.Utilities.StatsUtils.Artists;
public static class ArtistStats
{
    #region Helper Methods

    /// <summary>
    /// Retrieves all songs associated with a given artist ID from the entire library.
    /// </summary>
    private static List<SongModel> GetSongsByArtistId(ObjectId artistId, IReadOnlyCollection<SongModel> allSongsInLibrary)
    {
        return allSongsInLibrary.Where(s => s.ArtistIds.Any(a => a.Id == artistId)).ToList();
    }

    /// <summary>
    /// Filters all play events to get only those relevant to a specific collection of songs.
    /// </summary>
    private static List<DimmerPlayEvent> GetRelevantEventsForArtistSongs(
        IReadOnlyCollection<SongModel> songsByArtist,
        IReadOnlyCollection<DimmerPlayEvent> allEvents)
    {
        if (songsByArtist == null || songsByArtist.Count==0)
            return new List<DimmerPlayEvent>();

        var songIds = songsByArtist.Select(s => s.Id).ToHashSet();
        return allEvents.Where(e => e.SongId.HasValue && songIds.Contains(e.SongId.Value)).ToList();
    }

    /// <summary>
    /// Defines what constitutes a "play" initiation for counting purposes within ArtistStats specific aggregations.
    /// </summary>
    private static bool IsPlayInitiationEventLocal(DimmerPlayEvent e)
    {
        // Play: 0, Resume: 2, Restarted: 6, SeekRestarted: 7, CustomRepeat: 8, Previous: 9
        return e.PlayType == 0 || e.PlayType == 2 || e.PlayType == 6 || e.PlayType == 7 || e.PlayType == 8 || e.PlayType == 9;
    }

    private static string FormatTimeSpan(TimeSpan ts)
    {
        if ((int)ts.TotalDays > 0)
            return $"{(int)ts.TotalDays}d {ts.Hours:D2}h {ts.Minutes:D2}m {ts.Seconds:D2}s";
        if (ts.Hours > 0)
            return $"{ts.Hours:D2}h {ts.Minutes:D2}m {ts.Seconds:D2}s";
        return $"{ts.Minutes:D2}m {ts.Seconds:D2}s";
    }

    #endregion

    #region 1. Single Artist Stats

    public class ArtistSingleStatsSummary
    {
        public string ArtistName { get; set; } = "N/A";
        public ObjectId ArtistId { get; set; }

        // Basic Counts
        public int TotalSongsInLibrary { get; set; }
        public int TotalAlbumsInLibrary { get; set; }

        // Play-based Stats
        public int TotalPlaysAcrossAllSongs { get; set; }
        public int TotalSkipsAcrossAllSongs { get; set; }
        public double TotalListeningTimeSeconds { get; set; }
        public string TotalListeningTimeFormatted { get; set; } = "00m 00s";
        public int UniqueSongsPlayed { get; set; }
        public double PercentageOfArtistCatalogPlayed { get; set; }

        // Top Song Stats
        public string? MostPlayedSongTitle { get; set; }
        public int MostPlayedSongPlayCount { get; set; }
        public SongModel? MostPlayedSong { get; set; }
        public string? MostSkippedSongTitle { get; set; }
        public int MostSkippedSongSkipCount { get; set; }
        public SongModel? MostSkippedSong { get; set; }
        public string? MostCompletedSongTitle { get; set; }
        public int MostCompletedSongCompletions { get; set; }
        public SongModel? MostCompletedSong { get; set; }

        // Averages & Distributions
        public double AveragePlaysPerPlayedSong { get; set; }
        public double AverageSkipsPerSongInCatalog { get; set; }
        public double AverageListeningTimePerPlayedSongSeconds { get; set; }
        public string AverageListeningTimePerPlayedSongFormatted { get; set; } = "00m 00s";
        public double AverageSongDurationSeconds { get; set; }
        public string AverageSongDurationFormatted { get; set; } = "00m 00s";
        public double AverageRatingOfPlayedSongs { get; set; }

        // Time-based
        public DateTimeOffset? FirstEverPlayDate { get; set; }
        public DateTimeOffset? LastEverPlayDate { get; set; }
        public int? MostCommonPlayHour { get; set; }
        public string MostCommonPlayHourFormatted { get; set; } = "N/A";
        public int TotalDistinctDaysActive { get; set; }

        // Device Stats
        public int UniqueDevicesPlayedOn { get; set; }
        public string? MostCommonDevice { get; set; }

        // Song Properties of Artist's Catalog
        public int FavoritedSongsByArtistCount { get; set; }
        public int SongsWithLyricsCount { get; set; }
        public int SongsWithSyncedLyricsCount { get; set; }
        public int SongsNeverPlayedCount { get; set; }
        public int SongsPlayedToCompletionAtLeastOnce { get; set; }
        public double LongestSongDurationSec { get; set; }
        public string? LongestSongTitle { get; set; }
        public double ShortestSongDurationSec { get; set; }
        public string? ShortestSongTitle { get; set; }

        // Collaboration
        public int SongsAsSoloArtist { get; set; }
        public int SongsAsCollaborator { get; set; }
        public List<string> TopCollaborators { get; set; } = new List<string>(); // Name (count)

        // "Nerdy" or Advanced
        public int ArtistEddingtonNumber { get; set; }
        public string MostCommonGenre { get; set; } = "N/A";

        // Library Activity for Artist's Songs
        public DateTimeOffset? EarliestSongAddedDate { get; set; }
        public DateTimeOffset? LatestSongAddedDate { get; set; }
        public int TotalRawPlayEventsForArtistSongs { get; set; } // All event types for their songs
    }

    public static ArtistSingleStatsSummary GetSingleArtistStats(
        ArtistModel targetArtist,
        IReadOnlyCollection<SongModel> allSongsInLibrary,
        IReadOnlyCollection<DimmerPlayEvent> allEvents)
    {
        if (targetArtist == null)
            return new ArtistSingleStatsSummary { ArtistName = "Error: Artist not provided" };

        var songsByArtist = GetSongsByArtistId(targetArtist.Id, allSongsInLibrary);
        var summary = new ArtistSingleStatsSummary
        {
            ArtistName = targetArtist.Name ?? "Unknown Artist",
            ArtistId = targetArtist.Id,
            TotalSongsInLibrary = songsByArtist.Count
        };

        if (songsByArtist.Count==0)
            return summary; // No songs by this artist in the library

        var relevantEventsForArtistSongs = GetRelevantEventsForArtistSongs(songsByArtist, allEvents);

        summary.TotalAlbumsInLibrary = songsByArtist.Select(s => s.AlbumName).Distinct(StringComparer.OrdinalIgnoreCase).Count();

        summary.TotalPlaysAcrossAllSongs = songsByArtist.Sum(s => SongStats.GetPlayCount(s, allEvents));
        summary.TotalSkipsAcrossAllSongs = songsByArtist.Sum(s => SongStats.GetSkipCount(s, allEvents));
        summary.TotalListeningTimeSeconds = songsByArtist.Sum(s => SongStats.GetTotalListeningTime(s, allEvents));
        summary.TotalListeningTimeFormatted = FormatTimeSpan(TimeSpan.FromSeconds(summary.TotalListeningTimeSeconds));

        var playedSongsByArtist = songsByArtist.Where(s => SongStats.GetPlayCount(s, allEvents) > 0).ToList();
        summary.UniqueSongsPlayed = playedSongsByArtist.Count;
        summary.PercentageOfArtistCatalogPlayed = summary.TotalSongsInLibrary > 0 ? (double)summary.UniqueSongsPlayed / summary.TotalSongsInLibrary * 100 : 0;

        var mostPlayedSong = songsByArtist.OrderByDescending(s => SongStats.GetPlayCount(s, allEvents)).FirstOrDefault();
        if (mostPlayedSong != null)
        {
            summary.MostPlayedSongTitle = mostPlayedSong.Title;
            summary.MostPlayedSongPlayCount = SongStats.GetPlayCount(mostPlayedSong, allEvents);
            summary.MostPlayedSong = mostPlayedSong;
        }

        var mostSkippedSong = songsByArtist.OrderByDescending(s => SongStats.GetSkipCount(s, allEvents)).FirstOrDefault();
        if (mostSkippedSong != null)
        {
            summary.MostSkippedSongTitle = mostSkippedSong.Title;
            summary.MostSkippedSongSkipCount = SongStats.GetSkipCount(mostSkippedSong, allEvents);
            summary.MostSkippedSong = mostSkippedSong;
        }

        var mostCompletedSongData = songsByArtist
            .Select(s => new
            {
                Song = s,
                Completions = allEvents.Count(e => e.SongId.HasValue && e.SongId.Value == s.Id && (e.PlayType == 3 || e.WasPlayCompleted))
            })
            .Where(x => x.Completions > 0)
            .OrderByDescending(x => x.Completions)
            .ThenBy(x => x.Song.Title)
            .FirstOrDefault();
        if (mostCompletedSongData != null)
        {
            summary.MostCompletedSongTitle = mostCompletedSongData.Song.Title;
            summary.MostCompletedSongCompletions = mostCompletedSongData.Completions;
            summary.MostCompletedSong = mostCompletedSongData.Song;
        }

        summary.AveragePlaysPerPlayedSong = summary.UniqueSongsPlayed > 0 ? (double)summary.TotalPlaysAcrossAllSongs / summary.UniqueSongsPlayed : 0;
        summary.AverageSkipsPerSongInCatalog = summary.TotalSongsInLibrary > 0 ? (double)summary.TotalSkipsAcrossAllSongs / summary.TotalSongsInLibrary : 0;
        summary.AverageListeningTimePerPlayedSongSeconds = summary.UniqueSongsPlayed > 0 ? summary.TotalListeningTimeSeconds / summary.UniqueSongsPlayed : 0;
        summary.AverageListeningTimePerPlayedSongFormatted = FormatTimeSpan(TimeSpan.FromSeconds(summary.AverageListeningTimePerPlayedSongSeconds));

        summary.AverageSongDurationSeconds = songsByArtist.Average(s => s.DurationInSeconds);
        summary.AverageSongDurationFormatted = FormatTimeSpan(TimeSpan.FromSeconds(summary.AverageSongDurationSeconds));
        summary.AverageRatingOfPlayedSongs = playedSongsByArtist.Count!=0 ? playedSongsByArtist.Average(s => s.Rating) : 0;

        var artistPlayInitiationEvents = relevantEventsForArtistSongs.Where(IsPlayInitiationEventLocal).ToList();
        if (artistPlayInitiationEvents.Count!=0)
        {
            summary.FirstEverPlayDate = artistPlayInitiationEvents.Min(e => e.DatePlayed);
            summary.LastEverPlayDate = artistPlayInitiationEvents.Max(e => e.DatePlayed);
            summary.MostCommonPlayHour = artistPlayInitiationEvents.GroupBy(e => e.DatePlayed.Hour)
                                        .OrderByDescending(g => g.Count())
                                        .FirstOrDefault()?.Key;
            if (summary.MostCommonPlayHour.HasValue)
                summary.MostCommonPlayHourFormatted = $"{summary.MostCommonPlayHour.Value:D2}:00 - {summary.MostCommonPlayHour.Value:D2}:59";
            summary.TotalDistinctDaysActive = artistPlayInitiationEvents.Select(e => e.DatePlayed.Date).Distinct().Count();
        }

        var allDeviceNamesForArtist = relevantEventsForArtistSongs.Where(e => !string.IsNullOrEmpty(e.DeviceName)).Select(e => e.DeviceName!).ToList();
        summary.UniqueDevicesPlayedOn = allDeviceNamesForArtist.Distinct(StringComparer.OrdinalIgnoreCase).Count();
        summary.MostCommonDevice = allDeviceNamesForArtist.GroupBy(d => d, StringComparer.OrdinalIgnoreCase)
                                    .OrderByDescending(g => g.Count())
                                    .FirstOrDefault()?.Key;

        summary.FavoritedSongsByArtistCount = songsByArtist.Count(s => s.IsFavorite);
        summary.SongsWithLyricsCount = songsByArtist.Count(s => s.HasLyrics);
        summary.SongsWithSyncedLyricsCount = songsByArtist.Count(s => s.HasSyncedLyrics);
        summary.SongsNeverPlayedCount = songsByArtist.Count(s => SongStats.GetPlayCount(s, allEvents) == 0);
        summary.SongsPlayedToCompletionAtLeastOnce = songsByArtist.Count(s => SongStats.WasEverCompleted(s, allEvents));

        var longestSong = songsByArtist.OrderByDescending(s => s.DurationInSeconds).FirstOrDefault();
        if (longestSong != null)
        { summary.LongestSongDurationSec = longestSong.DurationInSeconds; summary.LongestSongTitle = longestSong.Title; }
        var shortestSong = songsByArtist.OrderBy(s => s.DurationInSeconds).FirstOrDefault();
        if (shortestSong != null)
        { summary.ShortestSongDurationSec = shortestSong.DurationInSeconds; summary.ShortestSongTitle = shortestSong.Title; }

        summary.SongsAsSoloArtist = songsByArtist.Count(s => s.ArtistIds.Count == 1); // Assuming current artist is the one.
        summary.SongsAsCollaborator = songsByArtist.Count(s => s.ArtistIds.Count > 1);
        var collabCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var song in songsByArtist.Where(s => s.ArtistIds.Count > 1))
        {
            foreach (var artist in song.ArtistIds)
            {
                if (artist.Id != targetArtist.Id && !string.IsNullOrEmpty(artist.Name))
                {
                    collabCounts.TryGetValue(artist.Name, out int currentCount);
                    collabCounts[artist.Name] = currentCount + 1;
                }
            }
        }
        summary.TopCollaborators = collabCounts.OrderByDescending(kvp => kvp.Value)
                                               .Take(5)
                                               .Select(kvp => $"{kvp.Key} ({kvp.Value} songs)")
                                               .ToList();

        var playCountsForArtistSongs = songsByArtist.Select(s => SongStats.GetPlayCount(s, allEvents)).OrderByDescending(pc => pc).ToList();
        int E_artist = 0;
        for (int i = 0; i < playCountsForArtistSongs.Count; i++)
            if (playCountsForArtistSongs[i] >= i + 1)
                E_artist = i + 1;
            else
                break;
        summary.ArtistEddingtonNumber = E_artist;

        summary.MostCommonGenre = songsByArtist.Where(s => s.Genre != null && !string.IsNullOrEmpty(s.Genre.Name))
                                    .GroupBy(s => s.Genre!.Name, StringComparer.OrdinalIgnoreCase)
                                    .OrderByDescending(g => g.Count())
                                    .FirstOrDefault()?.Key ?? "N/A";

        var validDateCreatedSongs = songsByArtist.Where(s => s.DateCreated.HasValue).ToList();
        if (validDateCreatedSongs.Count!=0)
        {
            summary.EarliestSongAddedDate = validDateCreatedSongs.Min(s => s.DateCreated!.Value);
            summary.LatestSongAddedDate = validDateCreatedSongs.Max(s => s.DateCreated!.Value);
        }
        summary.TotalRawPlayEventsForArtistSongs = relevantEventsForArtistSongs.Count;

        return summary;
    }

    // Overload for selection by index from a source song
    public static ArtistSingleStatsSummary GetSingleArtistStats(
        SongModel sourceSongForArtistSelection,
        int artistIndexInSourceSong,
        IReadOnlyCollection<SongModel> allSongsInLibrary,
        IReadOnlyCollection<DimmerPlayEvent> allEvents)
    {
        if (sourceSongForArtistSelection?.ArtistIds == null || artistIndexInSourceSong < 0 || artistIndexInSourceSong >= sourceSongForArtistSelection.ArtistIds.Count)
            return new ArtistSingleStatsSummary { ArtistName = "Error: Invalid artist selection criteria." };

        ArtistModel targetArtist = sourceSongForArtistSelection.ArtistIds[artistIndexInSourceSong];
        return GetSingleArtistStats(targetArtist, allSongsInLibrary, allEvents);
    }

    #endregion

    #region 2. Two Artist Comparison

    public class ArtistComparisonResult
    {
        public ArtistSingleStatsSummary Artist1Stats { get; set; } = new ArtistSingleStatsSummary();
        public ArtistSingleStatsSummary Artist2Stats { get; set; } = new ArtistSingleStatsSummary();
        public int SharedSongsInLibraryCount { get; set; }
        public int SharedSongsTotalPlays { get; set; }
        public List<string> SharedSongTitles { get; set; } = new List<string>();
    }

    public static ArtistComparisonResult CompareTwoArtists(
        ArtistModel artist1, ArtistModel artist2,
        IReadOnlyCollection<SongModel> allSongsInLibrary,
        IReadOnlyCollection<DimmerPlayEvent> allEvents)
    {
        if (artist1 == null || artist2 == null)
            return new ArtistComparisonResult { /* Error state can be indicated in ArtistStats names */ };
        if (artist1.Id == artist2.Id) // Comparing an artist to themselves
            return new ArtistComparisonResult { Artist1Stats = GetSingleArtistStats(artist1, allSongsInLibrary, allEvents), Artist2Stats = new ArtistSingleStatsSummary { ArtistName = "Same as Artist 1" } };


        var result = new ArtistComparisonResult
        {
            Artist1Stats = GetSingleArtistStats(artist1, allSongsInLibrary, allEvents),
            Artist2Stats = GetSingleArtistStats(artist2, allSongsInLibrary, allEvents)
        };

        var songsByArtist1Ids = GetSongsByArtistId(artist1.Id, allSongsInLibrary).Select(s => s.Id).ToHashSet();
        var songsByArtist2 = GetSongsByArtistId(artist2.Id, allSongsInLibrary);

        var sharedSongs = songsByArtist2.Where(s => songsByArtist1Ids.Contains(s.Id)).ToList();
        result.SharedSongsInLibraryCount = sharedSongs.Count;
        result.SharedSongTitles = sharedSongs.Select(s => s.Title ?? "Untitled").OrderBy(t => t).ToList();
        if (sharedSongs.Count!=0)
            result.SharedSongsTotalPlays = sharedSongs.Sum(s => SongStats.GetPlayCount(s, allEvents));

        return result;
    }

    // Overload for selection by index
    public static ArtistComparisonResult CompareTwoArtists(
        SongModel sourceSong, int artist1Index, int artist2Index,
        IReadOnlyCollection<SongModel> allSongsInLibrary,
        IReadOnlyCollection<DimmerPlayEvent> allEvents)
    {
        if (sourceSong?.ArtistIds == null ||
            artist1Index < 0 || artist1Index >= sourceSong.ArtistIds.Count ||
            artist2Index < 0 || artist2Index >= sourceSong.ArtistIds.Count)
            return new ArtistComparisonResult { /* Error state */ };

        ArtistModel art1 = sourceSong.ArtistIds[artist1Index];
        ArtistModel art2 = sourceSong.ArtistIds[artist2Index];
        return CompareTwoArtists(art1, art2, allSongsInLibrary, allEvents);
    }

    #endregion

    #region 3. Single Artist Plottable Data

    public class ArtistPlottableData
    {
        public string ArtistName { get; set; } = "N/A";
        public ObjectId ArtistId { get; set; }
        public List<LabelValue> PlaysPerMonth { get; set; } = new List<LabelValue>();
        public List<LabelValue> PlaysPerDayOfWeek { get; set; } = new List<LabelValue>();
        public List<LabelValue> PlaysPerHourOfDay { get; set; } = new List<LabelValue>();
        public List<LabelValue> SongPlayCountDistribution { get; set; } = new List<LabelValue>(); // X-axis: Play Count, Y-axis: Num Songs
        public List<LabelValue> SongSkipCountDistribution { get; set; } = new List<LabelValue>(); // X-axis: Skip Count, Y-axis: Num Songs
        public List<LabelValue> SongDurationDistributionMinutes { get; set; } = new List<LabelValue>(); // X-axis: Duration Bucket (min), Y-axis: Num Songs
    }

    public static ArtistPlottableData GetSingleArtistPlottableData(
        ArtistModel targetArtist,
        IReadOnlyCollection<SongModel> allSongsInLibrary,
        IReadOnlyCollection<DimmerPlayEvent> allEvents)
    {
        if (targetArtist == null)
            return new ArtistPlottableData { ArtistName = "Error: Artist not provided" };

        var songsByArtist = GetSongsByArtistId(targetArtist.Id, allSongsInLibrary);
        var data = new ArtistPlottableData { ArtistName = targetArtist.Name ?? "Unknown Artist", ArtistId = targetArtist.Id };
        if (songsByArtist.Count==0)
            return data;

        var relevantArtistEvents = GetRelevantEventsForArtistSongs(songsByArtist, allEvents);
        var playInitiationEvents = relevantArtistEvents.Where(IsPlayInitiationEventLocal).ToList();

        data.PlaysPerMonth = playInitiationEvents
            .GroupBy(e => new DateTime(e.DatePlayed.Year, e.DatePlayed.Month, 1))
            .Select(g => new LabelValue(g.Key.ToString("yyyy-MM"), g.Count()))
            .OrderBy(lv => lv.Label).ToList();
        data.PlaysPerDayOfWeek = playInitiationEvents
            .GroupBy(e => e.DatePlayed.DayOfWeek)
            .Select(g => new LabelValue(g.Key.ToString(), g.Count()))
            .OrderBy(lv => (int)Enum.Parse<DayOfWeek>(lv.Label)).ToList();
        data.PlaysPerHourOfDay = playInitiationEvents
            .GroupBy(e => e.DatePlayed.Hour)
            .Select(g => new LabelValue($"{g.Key:D2}:00", g.Count()))
            .OrderBy(lv => lv.Label).ToList();
        data.SongPlayCountDistribution = songsByArtist
            .Select(s => SongStats.GetPlayCount(s, allEvents))
            .GroupBy(pc => pc)
            .Select(g => new LabelValue(g.Key.ToString(), g.Count()))
            .OrderBy(lv => int.TryParse(lv.Label, out int val) ? val : int.MaxValue).ToList();
        data.SongSkipCountDistribution = songsByArtist
            .Select(s => SongStats.GetSkipCount(s, allEvents))
            .GroupBy(sc => sc)
            .Select(g => new LabelValue(g.Key.ToString(), g.Count()))
            .OrderBy(lv => int.TryParse(lv.Label, out int val) ? val : int.MaxValue).ToList();
        data.SongDurationDistributionMinutes = songsByArtist
            .GroupBy(s => (int)(s.DurationInSeconds / 60)) // Bucket by minute
            .Select(g => new { BucketValue = g.Key, Count = g.Count() })
            .OrderBy(x => x.BucketValue)
            .Select(x => new LabelValue($"{x.BucketValue} min", x.Count)).ToList();
        // Alt label: .Select(x => new LabelValue($"{x.BucketValue * 60}-{(x.BucketValue * 60) + 59}s", x.Count)).ToList();


        return data;
    }

    // Overload for selection by index
    public static ArtistPlottableData GetSingleArtistPlottableData(
        SongModel sourceSong, int artistIndex,
        IReadOnlyCollection<SongModel> allSongsInLibrary,
        IReadOnlyCollection<DimmerPlayEvent> allEvents)
    {
        if (sourceSong?.ArtistIds == null || artistIndex < 0 || artistIndex >= sourceSong.ArtistIds.Count)
            return new ArtistPlottableData { ArtistName = "Error: Invalid artist selection criteria." };

        ArtistModel targetArtist = sourceSong.ArtistIds[artistIndex];
        return GetSingleArtistPlottableData(targetArtist, allSongsInLibrary, allEvents);
    }

    #endregion

    #region 4. Multiple Artist Comparison (List of Summaries)

    public static List<ArtistSingleStatsSummary> GetMultipleArtistStats(
        List<ArtistModel> targetArtists,
        IReadOnlyCollection<SongModel> allSongsInLibrary,
        IReadOnlyCollection<DimmerPlayEvent> allEvents)
    {
        if (targetArtists == null || targetArtists.Count==0)
            return new List<ArtistSingleStatsSummary>();

        return targetArtists.Select(artist => GetSingleArtistStats(artist, allSongsInLibrary, allEvents)).ToList();
    }

    // Overload for selection by indices
    public static List<ArtistSingleStatsSummary> GetMultipleArtistStats(
        SongModel sourceSong, List<int> artistIndices,
        IReadOnlyCollection<SongModel> allSongsInLibrary,
        IReadOnlyCollection<DimmerPlayEvent> allEvents)
    {
        if (sourceSong?.ArtistIds == null || artistIndices == null || artistIndices.Count==0)
            return new List<ArtistSingleStatsSummary>();

        var artistsToQuery = new List<ArtistModel>();
        foreach (int index in artistIndices.Distinct()) // Distinct to avoid duplicates
        {
            if (index >= 0 && index < sourceSong.ArtistIds.Count)
                artistsToQuery.Add(sourceSong.ArtistIds[index]);
        }
        return GetMultipleArtistStats(artistsToQuery, allSongsInLibrary, allEvents);
    }

    #endregion
}
