namespace Dimmer.Utilities.StatsUtils.Albums;
public static class AlbumStats
{
    #region Helper Methods

    /// <summary>
    /// Retrieves all songs associated with a given Album ID from the entire library.
    /// </summary>
    private static List<SongModel> GetSongsByAlbumId(ObjectId albumId, IReadOnlyCollection<SongModel> allSongsInLibrary)
    {
        return [.. allSongsInLibrary.Where(s => s.Album != null && s.Album.Id == albumId)];
    }

    /// <summary>
    /// Filters all play events to get only those relevant to a specific collection of songs (e.g., songs on an album).
    /// </summary>
    private static List<DimmerPlayEvent> GetRelevantEventsForSongs(
        IReadOnlyCollection<SongModel> songs,
        IReadOnlyCollection<DimmerPlayEvent> allEvents)
    {
        if (songs == null || songs.Count==0)
            return new List<DimmerPlayEvent>();

        var songIds = songs.Select(s => s.Id).ToHashSet();
        return [.. allEvents.Where(e => e.SongId.HasValue && songIds.Contains(e.SongId.Value))];
    }

    private static bool IsPlayInitiationEventLocal(DimmerPlayEvent e)
    {
        // Play: 0, Resume: 2, Restarted: 6, SeekRestarted: 7, CustomRepeat: 8, Previous: 9
        return e.PlayType == 0 || e.PlayType == 2 || e.PlayType == 6 || e.PlayType == 7 || e.PlayType == 8 || e.PlayType == 9;
    }

    private static string FormatTimeSpanLocal(TimeSpan ts)
    {
        if ((int)ts.TotalDays > 0)
            return $"{(int)ts.TotalDays}d {ts.Hours:D2}h {ts.Minutes:D2}m {ts.Seconds:D2}s";
        if (ts.Hours > 0)
            return $"{ts.Hours:D2}h {ts.Minutes:D2}m {ts.Seconds:D2}s";
        return $"{ts.Minutes:D2}m {ts.Seconds:D2}s";
    }

    #endregion

    #region 1. Single Album Statistics

    public class AlbumSingleStatsSummary
    {
        public string AlbumName { get; set; } = "N/A";
        public ObjectId AlbumId { get; set; }
        public List<string> PrimaryArtistNames { get; set; } = new List<string>();
        public int? ReleaseYear { get; set; }

        // Counts based on library
        public int TotalTracksInLibraryForAlbum { get; set; }
        public double CalculatedTotalDurationSeconds { get; set; }
        public string CalculatedTotalDurationFormatted { get; set; } = "00m 00s";

        // Play-based Stats for the album
        public int TotalPlaysOnAlbum { get; set; }
        public int TotalSkipsOnAlbum { get; set; }
        public double TotalListeningTimeOnAlbumSeconds { get; set; }
        public string TotalListeningTimeOnAlbumFormatted { get; set; } = "00m 00s";
        public int UniqueSongsPlayedOnAlbum { get; set; }
        public double PercentageOfAlbumPlayed { get; set; } // (UniqueSongsPlayed / TotalTracksInLibrary)

        // Averages for songs on this album
        public double AveragePlaysPerSongOnAlbum { get; set; }
        public double AverageSkipsPerSongOnAlbum { get; set; }
        public double AverageListeningTimePerSongOnAlbumSeconds { get; set; }
        public string AverageListeningTimePerSongOnAlbumFormatted { get; set; } = "00m 00s";
        public double AverageSongRatingOnAlbum { get; set; }
        public double AverageSongDurationOnAlbumSeconds { get; set; }
        public string AverageSongDurationOnAlbumFormatted { get; set; } = "00m 00s";


        // Top Song Stats within this album
        public string? MostPlayedSongOnAlbumTitle { get; set; }
        public int MostPlayedSongOnAlbumPlayCount { get; set; }
        public string? MostSkippedSongOnAlbumTitle { get; set; }
        public int MostSkippedSongOnAlbumSkipCount { get; set; }
        public string? MostCompletedSongOnAlbumTitle { get; set; }
        public int MostCompletedSongOnAlbumCompletions { get; set; }
        public SongModel? MostPlayedSongModel { get; set; }
        public SongModel? MostSkippedSongModel { get; set; }
        public SongModel? MostCompletedSongModel { get; set; }


        // Time-based for the album
        public DateTimeOffset? FirstPlayDateForAlbum { get; set; }
        public DateTimeOffset? LastPlayDateForAlbum { get; set; }
        public int? MostCommonPlayHourForAlbum { get; set; }
        public string MostCommonPlayHourForAlbumFormatted { get; set; } = "N/A";
        public int TotalDistinctDaysAlbumActive { get; set; }

        // Device Stats for the album
        public int UniqueDevicesAlbumPlayedOn { get; set; }
        public string? MostCommonDeviceForAlbum { get; set; }

        // Properties of songs on this album
        public int FavoritedSongsOnAlbumCount { get; set; }
        public int SongsWithLyricsOnAlbumCount { get; set; }
        public int SongsWithSyncedLyricsOnAlbumCount { get; set; }
        public int SongsNeverPlayedOnAlbumCount { get; set; }
        public int SongsPlayedToCompletionAtLeastOnceOnAlbum { get; set; }
        public double LongestSongDurationOnAlbumSec { get; set; }
        public string? LongestSongOnAlbumTitle { get; set; }
        public double ShortestSongDurationOnAlbumSec { get; set; }
        public string? ShortestSongOnAlbumTitle { get; set; }

        // "Nerdy" or Advanced for the album
        public int AlbumEddingtonNumber { get; set; } // Max E such that E songs on album have >= E plays
        public string MainGenreOfAlbum { get; set; } = "N/A"; // Most common genre of songs on album
        public int TotalRawPlayEventsForAlbumSongs { get; set; }
        public int NumberOfCollaborativeTracksOnAlbum { get; set; } // Songs with >1 artist
        public int NumberOfSoloArtistTracksOnAlbum { get; set; } // Songs with 1 artist

        public DateTimeOffset? EarliestSongAddedDateToAlbum { get; set; }
        public DateTimeOffset? LatestSongAddedDateToAlbum { get; set; }
    }

    public static AlbumSingleStatsSummary GetSingleAlbumStats(
        AlbumModel targetAlbum,
        IReadOnlyCollection<SongModel> allSongsInLibrary,
        IReadOnlyCollection<DimmerPlayEvent> allEvents)
    {
        if (targetAlbum == null)
            return new AlbumSingleStatsSummary { AlbumName = "Error: Album not provided" };

        var songsOnAlbum = GetSongsByAlbumId(targetAlbum.Id, allSongsInLibrary);
        var summary = new AlbumSingleStatsSummary
        {
            AlbumName = targetAlbum.Name ?? "Unknown Album",
            AlbumId = targetAlbum.Id,
            ReleaseYear = targetAlbum.ReleaseYear,
            TotalTracksInLibraryForAlbum = songsOnAlbum.Count
        };

        if (songsOnAlbum.Count==0)
            return summary;

        summary.PrimaryArtistNames = songsOnAlbum
            .SelectMany(s => s.ArtistToSong.Select(a => a.Name))
            .Where(name => !string.IsNullOrEmpty(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name)
            .ToList()!;

        summary.CalculatedTotalDurationSeconds = songsOnAlbum.Sum(s => s.DurationInSeconds);
        summary.CalculatedTotalDurationFormatted = FormatTimeSpanLocal(TimeSpan.FromSeconds(summary.CalculatedTotalDurationSeconds));

        var relevantEventsForAlbum = GetRelevantEventsForSongs(songsOnAlbum, allEvents);

        summary.TotalPlaysOnAlbum = songsOnAlbum.Sum(s => SongStats.GetPlayCount(s, allEvents));
        summary.TotalSkipsOnAlbum = songsOnAlbum.Sum(s => SongStats.GetSkipCount(s, allEvents));
        summary.TotalListeningTimeOnAlbumSeconds = songsOnAlbum.Sum(s => SongStats.GetTotalListeningTime(s, allEvents));
        summary.TotalListeningTimeOnAlbumFormatted = FormatTimeSpanLocal(TimeSpan.FromSeconds(summary.TotalListeningTimeOnAlbumSeconds));

        var playedSongsOnAlbum = songsOnAlbum.Where(s => SongStats.GetPlayCount(s, allEvents) > 0).ToList();
        summary.UniqueSongsPlayedOnAlbum = playedSongsOnAlbum.Count;
        summary.PercentageOfAlbumPlayed = summary.TotalTracksInLibraryForAlbum > 0 ? (double)summary.UniqueSongsPlayedOnAlbum / summary.TotalTracksInLibraryForAlbum * 100 : 0;

        summary.AveragePlaysPerSongOnAlbum = summary.TotalTracksInLibraryForAlbum > 0 ? (double)summary.TotalPlaysOnAlbum / summary.TotalTracksInLibraryForAlbum : 0;
        summary.AverageSkipsPerSongOnAlbum = summary.TotalTracksInLibraryForAlbum > 0 ? (double)summary.TotalSkipsOnAlbum / summary.TotalTracksInLibraryForAlbum : 0;
        summary.AverageListeningTimePerSongOnAlbumSeconds = summary.TotalTracksInLibraryForAlbum > 0 ? summary.TotalListeningTimeOnAlbumSeconds / summary.TotalTracksInLibraryForAlbum : 0;
        summary.AverageListeningTimePerSongOnAlbumFormatted = FormatTimeSpanLocal(TimeSpan.FromSeconds(summary.AverageListeningTimePerSongOnAlbumSeconds));
        summary.AverageSongRatingOnAlbum = songsOnAlbum.Count!=0 ? songsOnAlbum.Average(s => s.Rating) : 0; // Or average of played songs: playedSongsOnAlbum.Any() ? playedSongsOnAlbum.Average(s=>s.Rating) : 0;
        summary.AverageSongDurationOnAlbumSeconds = songsOnAlbum.Average(s => s.DurationInSeconds);
        summary.AverageSongDurationOnAlbumFormatted = FormatTimeSpanLocal(TimeSpan.FromSeconds(summary.AverageSongDurationOnAlbumSeconds));


        var mostPlayedSong = songsOnAlbum.OrderByDescending(s => SongStats.GetPlayCount(s, allEvents)).FirstOrDefault();
        if (mostPlayedSong != null)
        {
            summary.MostPlayedSongOnAlbumTitle = mostPlayedSong.Title;
            summary.MostPlayedSongOnAlbumPlayCount = SongStats.GetPlayCount(mostPlayedSong, allEvents);
            summary.MostPlayedSongModel = mostPlayedSong;
        }

        var mostSkippedSong = songsOnAlbum.OrderByDescending(s => SongStats.GetSkipCount(s, allEvents)).FirstOrDefault();
        if (mostSkippedSong != null)
        {
            summary.MostSkippedSongOnAlbumTitle = mostSkippedSong.Title;
            summary.MostSkippedSongOnAlbumSkipCount = SongStats.GetSkipCount(mostSkippedSong, allEvents);
            summary.MostSkippedSongModel = mostSkippedSong;
        }

        var mostCompletedSongData = songsOnAlbum
            .Select(s => new
            {
                Song = s,
                Completions = relevantEventsForAlbum.Count(e => e.SongId.HasValue && e.SongId.Value == s.Id && (e.PlayType == 3 || e.WasPlayCompleted))
            })
            .Where(x => x.Completions > 0)
            .OrderByDescending(x => x.Completions)
            .ThenBy(x => x.Song.Title)
            .FirstOrDefault();

        if (mostCompletedSongData != null)
        {
            summary.MostCompletedSongOnAlbumTitle = mostCompletedSongData.Song.Title;
            summary.MostCompletedSongOnAlbumCompletions = mostCompletedSongData.Completions;
            summary.MostCompletedSongModel = mostCompletedSongData.Song;
        }


        var albumPlayInitiationEvents = relevantEventsForAlbum.Where(IsPlayInitiationEventLocal).ToList();
        if (albumPlayInitiationEvents.Count!=0)
        {
            summary.FirstPlayDateForAlbum = albumPlayInitiationEvents.Min(e => e.DatePlayed);
            summary.LastPlayDateForAlbum = albumPlayInitiationEvents.Max(e => e.DatePlayed);
            summary.MostCommonPlayHourForAlbum = albumPlayInitiationEvents.GroupBy(e => e.DatePlayed.Hour)
                                                .OrderByDescending(g => g.Count())
                                                .FirstOrDefault()?.Key;
            if (summary.MostCommonPlayHourForAlbum.HasValue)
                summary.MostCommonPlayHourForAlbumFormatted = $"{summary.MostCommonPlayHourForAlbum.Value:D2}:00 - {summary.MostCommonPlayHourForAlbum.Value:D2}:59";
            summary.TotalDistinctDaysAlbumActive = albumPlayInitiationEvents.Select(e => e.DatePlayed.Date).Distinct().Count();
        }

        var allDeviceNamesForAlbum = relevantEventsForAlbum.Where(e => !string.IsNullOrEmpty(e.DeviceName)).Select(e => e.DeviceName!).ToList();
        summary.UniqueDevicesAlbumPlayedOn = allDeviceNamesForAlbum.Distinct(StringComparer.OrdinalIgnoreCase).Count();
        summary.MostCommonDeviceForAlbum = allDeviceNamesForAlbum.GroupBy(d => d, StringComparer.OrdinalIgnoreCase)
                                        .OrderByDescending(g => g.Count())
                                        .FirstOrDefault()?.Key;

        summary.FavoritedSongsOnAlbumCount = songsOnAlbum.Count(s => s.IsFavorite);
        summary.SongsWithLyricsOnAlbumCount = songsOnAlbum.Count(s => s.HasLyrics);
        summary.SongsWithSyncedLyricsOnAlbumCount = songsOnAlbum.Count(s => s.HasSyncedLyrics);
        summary.SongsNeverPlayedOnAlbumCount = songsOnAlbum.Count(s => SongStats.GetPlayCount(s, allEvents) == 0);
        summary.SongsPlayedToCompletionAtLeastOnceOnAlbum = songsOnAlbum.Count(s => SongStats.WasEverCompleted(s, allEvents));

        var longestSong = songsOnAlbum.OrderByDescending(s => s.DurationInSeconds).FirstOrDefault();
        if (longestSong != null)
        { summary.LongestSongDurationOnAlbumSec = longestSong.DurationInSeconds; summary.LongestSongOnAlbumTitle = longestSong.Title; }
        var shortestSong = songsOnAlbum.OrderBy(s => s.DurationInSeconds).FirstOrDefault();
        if (shortestSong != null)
        { summary.ShortestSongDurationOnAlbumSec = shortestSong.DurationInSeconds; summary.ShortestSongOnAlbumTitle = shortestSong.Title; }

        var playCountsForAlbumSongs = songsOnAlbum.Select(s => SongStats.GetPlayCount(s, allEvents)).OrderByDescending(pc => pc).ToList();
        int E_album = 0;
        for (int i = 0; i < playCountsForAlbumSongs.Count; i++)
            if (playCountsForAlbumSongs[i] >= i + 1)
                E_album = i + 1;
            else
                break;
        summary.AlbumEddingtonNumber = E_album;

        summary.MainGenreOfAlbum = songsOnAlbum.Where(s => s.Genre != null && !string.IsNullOrEmpty(s.Genre.Name))
                                    .GroupBy(s => s.Genre!.Name, StringComparer.OrdinalIgnoreCase)
                                    .OrderByDescending(g => g.Count())
                                    .FirstOrDefault()?.Key ?? "N/A";
        summary.TotalRawPlayEventsForAlbumSongs = relevantEventsForAlbum.Count;
        summary.NumberOfCollaborativeTracksOnAlbum = songsOnAlbum.Count(s => s.ArtistToSong.Count > 1);
        summary.NumberOfSoloArtistTracksOnAlbum = songsOnAlbum.Count(s => s.ArtistToSong.Count == 1);

        var validDateCreatedSongs = songsOnAlbum.Where(s => s.DateCreated.HasValue).ToList();
        if (validDateCreatedSongs.Count!=0)
        {
            summary.EarliestSongAddedDateToAlbum = validDateCreatedSongs.Min(s => s.DateCreated!.Value);
            summary.LatestSongAddedDateToAlbum = validDateCreatedSongs.Max(s => s.DateCreated!.Value);
        }

        return summary;
    }

    // Overload: Get stats for the album of a given song
    public static AlbumSingleStatsSummary GetSingleAlbumStats(
        SongModel songFromAlbum,
        IReadOnlyCollection<SongModel> allSongsInLibrary,
        IReadOnlyCollection<DimmerPlayEvent> allEvents)
    {
        if (songFromAlbum?.Album == null)
            return new AlbumSingleStatsSummary { AlbumName = "Error: Song has no associated album or song is null." };

        return GetSingleAlbumStats(songFromAlbum.Album, allSongsInLibrary, allEvents);
    }


    public class AlbumPlottableData
    {
        public string AlbumName { get; set; } = "N/A";
        public ObjectId AlbumId { get; set; }
        public List<LabelValue> PlaysPerTrackNumber { get; set; } = new List<LabelValue>(); // X: Track #, Y: Plays
        public List<LabelValue> SkipsPerTrackNumber { get; set; } = new List<LabelValue>(); // X: Track #, Y: Skips
        public List<LabelValue> ListeningTimePerTrackNumberMinutes { get; set; } = new List<LabelValue>(); // X: Track #, Y: Mins
        public List<LabelValue> TrackDurationsMinutes { get; set; } = new List<LabelValue>(); // X: Track #, Y: Duration (min)
        public List<LabelValue> PlaysPerDayOfWeekForAlbum { get; set; } = new List<LabelValue>();
        public List<LabelValue> PlaysPerHourOfDayForAlbum { get; set; } = new List<LabelValue>();
    }

    public static AlbumPlottableData GetSingleAlbumPlottableData(
        AlbumModel targetAlbum,
        IReadOnlyCollection<SongModel> allSongsInLibrary,
        IReadOnlyCollection<DimmerPlayEvent> allEvents)
    {
        if (targetAlbum == null)
            return new AlbumPlottableData { AlbumName = "Error: Album not provided" };

        var songsOnAlbum = GetSongsByAlbumId(targetAlbum.Id, allSongsInLibrary)
                            .OrderBy(s => s.TrackNumber ?? int.MaxValue) // Ensure consistent order
                            .ThenBy(s => s.Title)
                            .ToList();

        var data = new AlbumPlottableData { AlbumName = targetAlbum.Name ?? "Unknown Album", AlbumId = targetAlbum.Id };
        if (songsOnAlbum.Count==0)
            return data;

        data.PlaysPerTrackNumber = [.. songsOnAlbum
            .Select(s => new LabelValue(
                s.TrackNumber.HasValue ? $"Track {s.TrackNumber:D2}" : (s.Title ?? "Unknown"),
                SongStats.GetPlayCount(s, allEvents)))];
        data.SkipsPerTrackNumber = [.. songsOnAlbum
            .Select(s => new LabelValue(
                s.TrackNumber.HasValue ? $"Track {s.TrackNumber:D2}" : (s.Title ?? "Unknown"),
                SongStats.GetSkipCount(s, allEvents)))];
        data.ListeningTimePerTrackNumberMinutes = [.. songsOnAlbum
            .Select(s => new LabelValue(
                s.TrackNumber.HasValue ? $"Track {s.TrackNumber:D2}" : (s.Title ?? "Unknown"),
                SongStats.GetTotalListeningTime(s, allEvents) / 60.0))];
        data.TrackDurationsMinutes = [.. songsOnAlbum
            .Select(s => new LabelValue(
                s.TrackNumber.HasValue ? $"Track {s.TrackNumber:D2}" : (s.Title ?? "Unknown"),
                s.DurationInSeconds / 60.0))];

        var relevantAlbumEvents = GetRelevantEventsForSongs(songsOnAlbum, allEvents);
        var albumPlayInitiationEvents = relevantAlbumEvents.Where(IsPlayInitiationEventLocal).ToList();

        data.PlaysPerDayOfWeekForAlbum = [.. albumPlayInitiationEvents
            .GroupBy(e => e.DatePlayed.DayOfWeek)
            .Select(g => new LabelValue(g.Key.ToString(), g.Count()))
            .OrderBy(lv => (int)Enum.Parse<DayOfWeek>(lv.Label))];
        data.PlaysPerHourOfDayForAlbum = [.. albumPlayInitiationEvents
            .GroupBy(e => e.DatePlayed.Hour)
            .Select(g => new LabelValue($"{g.Key:D2}:00", g.Count()))
            .OrderBy(lv => lv.Label)];

        return data;
    }

    // Overload for songFromAlbum
    public static AlbumPlottableData GetSingleAlbumPlottableData(
        SongModel songFromAlbum,
        IReadOnlyCollection<SongModel> allSongsInLibrary,
        IReadOnlyCollection<DimmerPlayEvent> allEvents)
    {
        if (songFromAlbum?.Album == null)
            return new AlbumPlottableData { AlbumName = "Error: Song has no associated album or song is null." };

        return GetSingleAlbumPlottableData(songFromAlbum.Album, allSongsInLibrary, allEvents);
    }


    #endregion

    #region 2. Artist's Contribution to a Specific Album

    public class ArtistAlbumContributionStatsSummary
    {
        public string ArtistName { get; set; } = "N/A";
        public ObjectId ArtistId { get; set; }
        public string AlbumName { get; set; } = "N/A";
        public ObjectId AlbumId { get; set; }

        // Artist's specific contribution to this album
        public int TotalSongsByArtistOnThisAlbum { get; set; }
        public int TotalPlaysForArtistOnThisAlbum { get; set; }
        public int TotalSkipsForArtistOnThisAlbum { get; set; }
        public double TotalListeningTimeForArtistOnThisAlbumSeconds { get; set; }
        public string TotalListeningTimeForArtistOnThisAlbumFormatted { get; set; } = "00m 00s";

        // Percentages relative to album totals (requires album summary or re-calc)
        public double PercentageOfAlbumTracksByThisArtist { get; set; } // (ArtistSongsOnAlbum / AlbumTotalTracks)
        public double PercentageOfAlbumPlaysByThisArtist { get; set; }  // (ArtistPlaysOnAlbum / AlbumTotalPlays)
        public double PercentageOfAlbumListeningTimeByThisArtist { get; set; } // (ArtistListeningOnAlbum / AlbumTotalListening)

        // Averages for this artist's songs on this album
        public double AveragePlaysPerArtistSongOnThisAlbum { get; set; }
        public double AverageSkipsPerArtistSongOnThisAlbum { get; set; }
        public double AverageListeningTimePerArtistSongOnThisAlbumSeconds { get; set; }
        public string AverageListeningTimePerArtistSongOnThisAlbumFormatted { get; set; } = "00m 00s";
        public double AverageRatingOfArtistSongsOnThisAlbum { get; set; }

        // Top song by this artist on this album
        public string? MostPlayedSongByArtistOnThisAlbumTitle { get; set; }
        public int MostPlayedSongByArtistOnThisAlbumPlayCount { get; set; }
        public SongModel? MostPlayedSongByArtistOnThisAlbumModel { get; set; }


        // Time-based for artist on this album
        public DateTimeOffset? FirstPlayDateForArtistOnThisAlbum { get; set; }
        public DateTimeOffset? LastPlayDateForArtistOnThisAlbum { get; set; }

        // Device stats for artist on this album
        public int UniqueDevicesArtistSongsOnThisAlbumPlayedOn { get; set; }
        public string? MostCommonDeviceForArtistOnThisAlbum { get; set; }

        // Properties of artist's songs on this album
        public int FavoritedSongsByArtistOnThisAlbumCount { get; set; }
        public int SongsByArtistOnThisAlbumWithLyricsCount { get; set; }
        public int SongsByArtistOnThisAlbumNeverPlayedCount { get; set; }
        public int SongsByArtistOnThisAlbumCompletedAtLeastOnce { get; set; }
        public double LongestSongByArtistOnThisAlbumSec { get; set; }
        public string? LongestSongByArtistOnThisAlbumTitle { get; set; }
        public double ShortestSongByArtistOnThisAlbumSec { get; set; }
        public string? ShortestSongByArtistOnThisAlbumTitle { get; set; }
        public int ArtistEddingtonNumberOnThisAlbum { get; set; } // For this artist's songs on this album

        // Collaboration context for artist's songs on this album
        public int ArtistSoloTracksOnThisAlbum { get; set; } // Artist is sole credited on their songs on this album
        public int ArtistCollabTracksOnThisAlbum { get; set; } // Artist has co-credits on their songs on this album
        public int TotalCompletionsForArtistOnThisAlbum { get; set; }
        public double ArtistAverageCompletionRateOnThisAlbumPercent { get; set; } // (completions / plays) or (completed songs / played songs)
        public int UniqueArtistSongsPlayedOnAlbum { get; set; }
    }

    public static ArtistAlbumContributionStatsSummary GetArtistAlbumContributionStats(
        ArtistModel targetArtist,
        AlbumModel targetAlbum,
        IReadOnlyCollection<SongModel> allSongsInLibrary,
        IReadOnlyCollection<DimmerPlayEvent> allEvents,
        AlbumSingleStatsSummary? albumOverallStats = null) // Optional pre-calculated album stats for percentages
    {
        if (targetArtist == null || targetAlbum == null)
            return new ArtistAlbumContributionStatsSummary { ArtistName = "Error: Artist or Album not provided" };

        // Get all songs by this artist that are ALSO on this album
        var songsByArtistOnThisAlbum = allSongsInLibrary
            .Where(s => s.Album != null && s.Album.Id == targetAlbum.Id &&
                        s.ArtistToSong.Any(a => a.Id == targetArtist.Id))
            .ToList();

        var summary = new ArtistAlbumContributionStatsSummary
        {
            ArtistName = targetArtist.Name ?? "Unknown Artist",
            ArtistId = targetArtist.Id,
            AlbumName = targetAlbum.Name ?? "Unknown Album",
            AlbumId = targetAlbum.Id,
            TotalSongsByArtistOnThisAlbum = songsByArtistOnThisAlbum.Count
        };

        if (songsByArtistOnThisAlbum.Count==0)
            return summary;

        var relevantEventsForArtistOnAlbum = GetRelevantEventsForSongs(songsByArtistOnThisAlbum, allEvents);

        summary.TotalPlaysForArtistOnThisAlbum = songsByArtistOnThisAlbum.Sum(s => SongStats.GetPlayCount(s, allEvents));
        summary.TotalSkipsForArtistOnThisAlbum = songsByArtistOnThisAlbum.Sum(s => SongStats.GetSkipCount(s, allEvents));
        summary.TotalListeningTimeForArtistOnThisAlbumSeconds = songsByArtistOnThisAlbum.Sum(s => SongStats.GetTotalListeningTime(s, allEvents));
        summary.TotalListeningTimeForArtistOnThisAlbumFormatted = FormatTimeSpanLocal(TimeSpan.FromSeconds(summary.TotalListeningTimeForArtistOnThisAlbumSeconds));

        if (albumOverallStats == null) // Recalculate if not provided
        {
            albumOverallStats = GetSingleAlbumStats(targetAlbum, allSongsInLibrary, allEvents);
        }

        if (albumOverallStats.TotalTracksInLibraryForAlbum > 0)
            summary.PercentageOfAlbumTracksByThisArtist = (double)summary.TotalSongsByArtistOnThisAlbum / albumOverallStats.TotalTracksInLibraryForAlbum * 100;
        if (albumOverallStats.TotalPlaysOnAlbum > 0)
            summary.PercentageOfAlbumPlaysByThisArtist = (double)summary.TotalPlaysForArtistOnThisAlbum / albumOverallStats.TotalPlaysOnAlbum * 100;
        if (albumOverallStats.TotalListeningTimeOnAlbumSeconds > 0)
            summary.PercentageOfAlbumListeningTimeByThisArtist = summary.TotalListeningTimeForArtistOnThisAlbumSeconds / albumOverallStats.TotalListeningTimeOnAlbumSeconds * 100;


        summary.AveragePlaysPerArtistSongOnThisAlbum = summary.TotalSongsByArtistOnThisAlbum > 0 ? (double)summary.TotalPlaysForArtistOnThisAlbum / summary.TotalSongsByArtistOnThisAlbum : 0;
        summary.AverageSkipsPerArtistSongOnThisAlbum = summary.TotalSongsByArtistOnThisAlbum > 0 ? (double)summary.TotalSkipsForArtistOnThisAlbum / summary.TotalSongsByArtistOnThisAlbum : 0;
        summary.AverageListeningTimePerArtistSongOnThisAlbumSeconds = summary.TotalSongsByArtistOnThisAlbum > 0 ? summary.TotalListeningTimeForArtistOnThisAlbumSeconds / summary.TotalSongsByArtistOnThisAlbum : 0;
        summary.AverageListeningTimePerArtistSongOnThisAlbumFormatted = FormatTimeSpanLocal(TimeSpan.FromSeconds(summary.AverageListeningTimePerArtistSongOnThisAlbumSeconds));
        summary.AverageRatingOfArtistSongsOnThisAlbum = songsByArtistOnThisAlbum.Average(s => s.Rating);

        var mostPlayedByArtistOnAlbum = songsByArtistOnThisAlbum.OrderByDescending(s => SongStats.GetPlayCount(s, allEvents)).FirstOrDefault();
        if (mostPlayedByArtistOnAlbum != null)
        {
            summary.MostPlayedSongByArtistOnThisAlbumTitle = mostPlayedByArtistOnAlbum.Title;
            summary.MostPlayedSongByArtistOnThisAlbumPlayCount = SongStats.GetPlayCount(mostPlayedByArtistOnAlbum, allEvents);
            summary.MostPlayedSongByArtistOnThisAlbumModel = mostPlayedByArtistOnAlbum;
        }

        var artistAlbumPlayInitiationEvents = relevantEventsForArtistOnAlbum.Where(IsPlayInitiationEventLocal).ToList();
        if (artistAlbumPlayInitiationEvents.Count!=0)
        {
            summary.FirstPlayDateForArtistOnThisAlbum = artistAlbumPlayInitiationEvents.Min(e => e.DatePlayed);
            summary.LastPlayDateForArtistOnThisAlbum = artistAlbumPlayInitiationEvents.Max(e => e.DatePlayed);
        }

        var devicesForArtistOnAlbum = relevantEventsForArtistOnAlbum.Where(e => !string.IsNullOrEmpty(e.DeviceName)).Select(e => e.DeviceName!).ToList();
        summary.UniqueDevicesArtistSongsOnThisAlbumPlayedOn = devicesForArtistOnAlbum.Distinct(StringComparer.OrdinalIgnoreCase).Count();
        summary.MostCommonDeviceForArtistOnThisAlbum = devicesForArtistOnAlbum.GroupBy(d => d, StringComparer.OrdinalIgnoreCase).OrderByDescending(g => g.Count()).FirstOrDefault()?.Key;

        summary.FavoritedSongsByArtistOnThisAlbumCount = songsByArtistOnThisAlbum.Count(s => s.IsFavorite);
        summary.SongsByArtistOnThisAlbumWithLyricsCount = songsByArtistOnThisAlbum.Count(s => s.HasLyrics);
        summary.SongsByArtistOnThisAlbumNeverPlayedCount = songsByArtistOnThisAlbum.Count(s => SongStats.GetPlayCount(s, allEvents) == 0);
        summary.SongsByArtistOnThisAlbumCompletedAtLeastOnce = songsByArtistOnThisAlbum.Count(s => SongStats.WasEverCompleted(s, allEvents));
        summary.TotalCompletionsForArtistOnThisAlbum = songsByArtistOnThisAlbum.Sum(s => relevantEventsForArtistOnAlbum.Count(e => e.SongId == s.Id && (e.PlayType == 3 || e.WasPlayCompleted)));

        var playedArtistSongsOnAlbum = songsByArtistOnThisAlbum.Where(s => SongStats.GetPlayCount(s, allEvents) > 0).ToList();
        summary.UniqueArtistSongsPlayedOnAlbum = playedArtistSongsOnAlbum.Count;
        if (summary.UniqueArtistSongsPlayedOnAlbum > 0)
            summary.ArtistAverageCompletionRateOnThisAlbumPercent = (double)summary.SongsByArtistOnThisAlbumCompletedAtLeastOnce / summary.UniqueArtistSongsPlayedOnAlbum * 100;
        else if (summary.TotalPlaysForArtistOnThisAlbum > 0) // Alternative if we define completion rate based on play events
            summary.ArtistAverageCompletionRateOnThisAlbumPercent = (double)summary.TotalCompletionsForArtistOnThisAlbum / summary.TotalPlaysForArtistOnThisAlbum * 100;


        var longestArtistSong = songsByArtistOnThisAlbum.OrderByDescending(s => s.DurationInSeconds).FirstOrDefault();
        if (longestArtistSong != null)
        { summary.LongestSongByArtistOnThisAlbumSec = longestArtistSong.DurationInSeconds; summary.LongestSongByArtistOnThisAlbumTitle = longestArtistSong.Title; }
        var shortestArtistSong = songsByArtistOnThisAlbum.OrderBy(s => s.DurationInSeconds).FirstOrDefault();
        if (shortestArtistSong != null)
        { summary.ShortestSongByArtistOnThisAlbumSec = shortestArtistSong.DurationInSeconds; summary.ShortestSongByArtistOnThisAlbumTitle = shortestArtistSong.Title; }

        var playCountsForArtistOnAlbum = songsByArtistOnThisAlbum.Select(s => SongStats.GetPlayCount(s, allEvents)).OrderByDescending(pc => pc).ToList();
        int E_artist_album = 0;
        for (int i = 0; i < playCountsForArtistOnAlbum.Count; i++)
            if (playCountsForArtistOnAlbum[i] >= i + 1)
                E_artist_album = i + 1;
            else
                break;
        summary.ArtistEddingtonNumberOnThisAlbum = E_artist_album;

        summary.ArtistSoloTracksOnThisAlbum = songsByArtistOnThisAlbum.Count(s => s.ArtistToSong.Count == 1 && s.ArtistToSong.First().Id == targetArtist.Id);
        summary.ArtistCollabTracksOnThisAlbum = songsByArtistOnThisAlbum.Count(s => s.ArtistToSong.Count > 1 && s.ArtistToSong.Any(a => a.Id == targetArtist.Id));

        return summary;
    }

    // Overload for selection from a song
    public static ArtistAlbumContributionStatsSummary GetArtistAlbumContributionStats(
        SongModel sourceSong, // Song from which to get album and artist
        int artistIndexInSourceSong, // Index of artist in sourceSong.ArtistToSong
        IReadOnlyCollection<SongModel> allSongsInLibrary,
        IReadOnlyCollection<DimmerPlayEvent> allEvents,
        AlbumSingleStatsSummary? albumOverallStats = null)
    {
        if (sourceSong?.Album == null || sourceSong.ArtistToSong == null ||
            artistIndexInSourceSong < 0 || artistIndexInSourceSong >= sourceSong.ArtistToSong.Count)
        {
            return new ArtistAlbumContributionStatsSummary { ArtistName = "Error: Invalid source song or artist index." };
        }
        ArtistModel targetArtist = sourceSong.ArtistToSong[artistIndexInSourceSong];
        AlbumModel targetAlbum = sourceSong.Album;
        return GetArtistAlbumContributionStats(targetArtist, targetAlbum, allSongsInLibrary, allEvents, albumOverallStats);
    }


    #endregion

    #region 3. Comparing Artist Contributions on a Single Album

    public class AlbumArtistComparisonBundle
    {
        public string AlbumName { get; set; } = "N/A";
        public ObjectId AlbumId { get; set; }
        public AlbumSingleStatsSummary AlbumOverallStats { get; set; } = new AlbumSingleStatsSummary();
        public List<ArtistAlbumContributionStatsSummary> ArtistContributions { get; set; } = new List<ArtistAlbumContributionStatsSummary>();
        // Additional comparative stats can be added here if needed, e.g.,
        // ArtistWithMostPlaysOnAlbum, ArtistWithHighestListeningTimeShare, etc.
        public string? ArtistWithMostTracksOnAlbum { get; set; }
        public string? ArtistWithMostPlaysOnAlbum { get; set; }
        public string? ArtistWithHighestListeningTimeShareOnAlbum { get; set; }
    }

    public static AlbumArtistComparisonBundle GetAlbumArtistComparisonBundle(
        AlbumModel targetAlbum,
        IReadOnlyCollection<SongModel> allSongsInLibrary,
        IReadOnlyCollection<DimmerPlayEvent> allEvents,
        List<ArtistModel>? specificArtistsToCompare = null) // Optional: if null, compares all artists on album
    {
        if (targetAlbum == null)
            return new AlbumArtistComparisonBundle { AlbumName = "Error: Album not provided" };

        var bundle = new AlbumArtistComparisonBundle
        {
            AlbumName = targetAlbum.Name ?? "Unknown Album",
            AlbumId = targetAlbum.Id,
            AlbumOverallStats = GetSingleAlbumStats(targetAlbum, allSongsInLibrary, allEvents)
        };

        if (bundle.AlbumOverallStats.TotalTracksInLibraryForAlbum == 0)
            return bundle; // No songs on album

        var songsOnThisAlbum = GetSongsByAlbumId(targetAlbum.Id, allSongsInLibrary);

        HashSet<ObjectId> artistsToConsiderIds;
        if (specificArtistsToCompare != null && specificArtistsToCompare.Count!=0)
        {
            artistsToConsiderIds = [.. specificArtistsToCompare.Select(a => a.Id)];
        }
        else // Get all distinct artists from the songs on this album
        {
            artistsToConsiderIds = [.. songsOnThisAlbum
                .SelectMany(s => s.ArtistToSong.Select(a => a.Id))
                .Distinct()];
        }

        // We need the full ArtistModel objects
        var allArtistModelsInLibrary = allSongsInLibrary.SelectMany(s => s.ArtistToSong).DistinctBy(a => a.Id).ToDictionary(a => a.Id);

        foreach (var artistId in artistsToConsiderIds)
        {
            if (allArtistModelsInLibrary.TryGetValue(artistId, out var artistModel))
            {
                var contribution = GetArtistAlbumContributionStats(artistModel, targetAlbum, allSongsInLibrary, allEvents, bundle.AlbumOverallStats);
                if (contribution.TotalSongsByArtistOnThisAlbum > 0) // Only add if they actually have songs on this album
                {
                    bundle.ArtistContributions.Add(contribution);
                }
            }
        }

        bundle.ArtistContributions = [.. bundle.ArtistContributions.OrderByDescending(c => c.TotalPlaysForArtistOnThisAlbum).ThenBy(c => c.ArtistName)];

        // Populate top artist stats for the bundle
        var topTrackArtist = bundle.ArtistContributions.OrderByDescending(c => c.TotalSongsByArtistOnThisAlbum).FirstOrDefault();
        if (topTrackArtist != null)
            bundle.ArtistWithMostTracksOnAlbum = $"{topTrackArtist.ArtistName} ({topTrackArtist.TotalSongsByArtistOnThisAlbum} tracks)";

        var topPlayArtist = bundle.ArtistContributions.OrderByDescending(c => c.TotalPlaysForArtistOnThisAlbum).FirstOrDefault();
        if (topPlayArtist != null)
            bundle.ArtistWithMostPlaysOnAlbum = $"{topPlayArtist.ArtistName} ({topPlayArtist.TotalPlaysForArtistOnThisAlbum} plays)";

        var topListenArtist = bundle.ArtistContributions.OrderByDescending(c => c.PercentageOfAlbumListeningTimeByThisArtist).FirstOrDefault();
        if (topListenArtist != null)
            bundle.ArtistWithHighestListeningTimeShareOnAlbum = $"{topListenArtist.ArtistName} ({topListenArtist.PercentageOfAlbumListeningTimeByThisArtist:F1}% listening time)";


        return bundle;
    }

    // Overload for selection from a song
    public static AlbumArtistComparisonBundle GetAlbumArtistComparisonBundle(
        SongModel songFromAlbum, // Album is taken from this song
        IReadOnlyCollection<SongModel> allSongsInLibrary,
        IReadOnlyCollection<DimmerPlayEvent> allEvents,
        List<ArtistModel>? specificArtistsToCompare = null) // Optional: if null, compares all artists on album
    {
        if (songFromAlbum?.Album == null)
            return new AlbumArtistComparisonBundle { AlbumName = "Error: Song has no associated album or song is null." };

        return GetAlbumArtistComparisonBundle(songFromAlbum.Album, allSongsInLibrary, allEvents, specificArtistsToCompare);
    }


    #endregion
}