namespace Dimmer.Utilities.StatsUtils;
public static class SongStats
{
    // 1. Play count
    public static int GetPlayCount(SongModel song, IReadOnlyCollection<DimmerPlayEvent> events)
        => events.Count(e => e.SongId == song.Id && e.PlayType == 0);

    // 2. Skip count
    public static int GetSkipCount(SongModel song, IReadOnlyCollection<DimmerPlayEvent> events)
        => events.Count(e => e.SongId == song.Id && e.PlayType == 5);

    // 3. Complete play count
    public static int GetCompletedPlayCount(SongModel song, IReadOnlyCollection<DimmerPlayEvent> events)
        => events.Count(e => e.SongId == song.Id && e.WasPlayCompleted);

    // 4. Average percent listened (as a fraction)
    public static double GetAvgPercentListened(SongModel song, IReadOnlyCollection<DimmerPlayEvent> events)
    {
        var relevant = events.Where(e => e.SongId == song.Id && song.DurationInSeconds > 0);
        if (!relevant.Any())
            return 0;
        return relevant.Average(e => Math.Min(e.PositionInSeconds / song.DurationInSeconds, 1.0));
    }

    // 5. Last played date
    public static DateTimeOffset? GetLastPlayedDate(SongModel song, IReadOnlyCollection<DimmerPlayEvent> events)
        => events.Where(e => e.SongId == song.Id && e.PlayType == 0)
                 .OrderByDescending(e => e.DatePlayed)
                 .FirstOrDefault()?.DatePlayed;

    // 6. First played date
    public static DateTimeOffset? GetFirstPlayedDate(SongModel song, IReadOnlyCollection<DimmerPlayEvent> events)
        => events.Where(e => e.SongId == song.Id && e.PlayType == 0)
                 .OrderBy(e => e.DatePlayed)
                 .FirstOrDefault()?.DatePlayed;

    // 7. Devices song was played on
    public static List<string> GetPlayedDevices(SongModel song, IReadOnlyCollection<DimmerPlayEvent> events)
        => events.Where(e => e.SongId == song.Id && !string.IsNullOrEmpty(e.DeviceName))
                 .Select(e => e.DeviceName!)
                 .Distinct()
                 .ToList();

    // 8. Most used device
    public static string? GetMostUsedDevice(SongModel song, IReadOnlyCollection<DimmerPlayEvent> events)
        => events.Where(e => e.SongId == song.Id && !string.IsNullOrEmpty(e.DeviceName))
                 .GroupBy(e => e.DeviceName)
                 .OrderByDescending(g => g.Count())
                 .FirstOrDefault()?.Key;

    // 9. Longest single playback session (in seconds)
    public static double GetLongestSession(SongModel song, IReadOnlyCollection<DimmerPlayEvent> events)
        => events.Where(e => e.SongId == song.Id)
                 .Select(e => e.DateFinished.Subtract(e.DatePlayed).TotalSeconds)
                 .DefaultIfEmpty(0)
                 .Max();

    // 10. Total listening time (seconds)
    public static double GetTotalListeningTime(SongModel song, IReadOnlyCollection<DimmerPlayEvent> events)
        => events.Where(e => e.SongId == song.Id)
                 .Sum(e => e.DateFinished.Subtract(e.DatePlayed).TotalSeconds);

    // 11. Most active hour for this song
    public static int? GetMostActiveHour(SongModel song, IReadOnlyCollection<DimmerPlayEvent> events)
        => events.Where(e => e.SongId == song.Id)
                 .GroupBy(e => e.DatePlayed.Hour)
                 .OrderByDescending(g => g.Count())
                 .FirstOrDefault()?.Key;

    // 12. Was ever played to completion
    public static bool WasEverCompleted(SongModel song, IReadOnlyCollection<DimmerPlayEvent> events)
        => events.Any(e => e.SongId == song.Id && e.WasPlayCompleted);

    // 13. Number of times resumed
    public static int GetResumeCount(SongModel song, IReadOnlyCollection<DimmerPlayEvent> events)
        => events.Count(e => e.SongId == song.Id && e.PlayType == 2);

    // 14. Distinct days song was played
    public static int GetDistinctDaysPlayed(SongModel song, IReadOnlyCollection<DimmerPlayEvent> events)
        => events.Where(e => e.SongId == song.Id)
                 .Select(e => e.DatePlayed.Date)
                 .Distinct()
                 .Count();

    // 15. Average position when skipped (seconds)
    public static double GetAvgPositionWhenSkipped(SongModel song, IReadOnlyCollection<DimmerPlayEvent> events)
    {
        var skips = events.Where(e => e.SongId == song.Id && e.PlayType == 5);
        if (!skips.Any())
            return 0;
        return skips.Average(e => e.PositionInSeconds);
    }

    // 16. Play count by device form factor
    public static Dictionary<string, int> GetPlayCountByDeviceFormFactor(SongModel song, IReadOnlyCollection<DimmerPlayEvent> events)
        => events.Where(e => e.SongId == song.Id && !string.IsNullOrEmpty(e.DeviceFormFactor))
                 .GroupBy(e => e.DeviceFormFactor!)
                 .ToDictionary(g => g.Key, g => g.Count());

    // 17. Longest gap between plays (in days)
    public static double GetLongestGapBetweenPlays(SongModel song, IReadOnlyCollection<DimmerPlayEvent> events)
    {
        var plays = events.Where(e => e.SongId == song.Id && e.PlayType == 0)
                          .OrderBy(e => e.DatePlayed)
                          .Select(e => e.DatePlayed)
                          .ToList();
        if (plays.Count < 2)
            return 0;
        return plays.Zip(plays.Skip(1), (a, b) => (b - a).TotalDays).Max();
    }

    // 18. Shortest gap between plays (in minutes)
    public static double GetShortestGapBetweenPlays(SongModel song, IReadOnlyCollection<DimmerPlayEvent> events)
    {
        var plays = events.Where(e => e.SongId == song.Id && e.PlayType == 0)
                          .OrderBy(e => e.DatePlayed)
                          .Select(e => e.DatePlayed)
                          .ToList();
        if (plays.Count < 2)
            return 0;
        return plays.Zip(plays.Skip(1), (a, b) => (b - a).TotalMinutes).Min();
    }

    // 19. Most recent device used
    public static string? GetMostRecentDevice(SongModel song, IReadOnlyCollection<DimmerPlayEvent> events)
        => events.Where(e => e.SongId == song.Id && !string.IsNullOrEmpty(e.DeviceName))
                 .OrderByDescending(e => e.DatePlayed)
                 .FirstOrDefault()?.DeviceName;

    // 20. Number of times played as favorite
    public static int GetPlayCountAsFavorite(SongModel song, IReadOnlyCollection<DimmerPlayEvent> events)
        => song.IsFavorite ? GetPlayCount(song, events) : 0;

    // 21. Number of skips after 50% played
    public static int GetSkipAfterHalfPlayedCount(SongModel song, IReadOnlyCollection<DimmerPlayEvent> events)
        => events.Count(e => e.SongId == song.Id && e.PlayType == 5 && song.DurationInSeconds > 0 && e.PositionInSeconds / song.DurationInSeconds >= 0.5);

    // 22. Number of play sessions with lyric view opened
    // (Assuming you have play Events for when lyrics opened)
    public static int GetLyricViewSessions(SongModel song, IReadOnlyCollection<DimmerPlayEvent> events)
        => events.Count(e => e.SongId == song.Id && e.PlayType == 10); // suppose 10 = LyricView

    // 23. Days since last play
    public static int? GetDaysSinceLastPlay(SongModel song, IReadOnlyCollection<DimmerPlayEvent> events)
    {
        var last = GetLastPlayedDate(song, events);
        if (last == null)
            return null;
        return (int)(DateTimeOffset.Now - last.Value).TotalDays;
    }

    // 24. Number of unique devices ever played on
    public static int GetUniqueDevicesCount(SongModel song, IReadOnlyCollection<DimmerPlayEvent> events)
        => GetPlayedDevices(song, events).Count;

    // 25. Was the song ever played at midnight?
    public static bool WasPlayedAtMidnight(SongModel song, IReadOnlyCollection<DimmerPlayEvent> events)
        => events.Any(e => e.SongId == song.Id && e.DatePlayed.Hour == 0);
}

public static class SongComparison
{
    // 1. Which song has higher play count?
    public static SongModel GetMorePlayedSong(SongModel songA, SongModel songB, IReadOnlyCollection<DimmerPlayEvent> events)
        => SongStats.GetPlayCount(songA, events) >= SongStats.GetPlayCount(songB, events) ? songA : songB;

    // 2. Which song was played more recently?
    public static SongModel GetMostRecentlyPlayedSong(SongModel songA, SongModel songB, IReadOnlyCollection<DimmerPlayEvent> events)
        => (SongStats.GetLastPlayedDate(songA, events) ?? DateTimeOffset.MinValue) >= (SongStats.GetLastPlayedDate(songB, events) ?? DateTimeOffset.MinValue) ? songA : songB;

    // 3. Which song has more skips?
    public static SongModel GetMoreSkippedSong(SongModel songA, SongModel songB, IReadOnlyCollection<DimmerPlayEvent> events)
        => SongStats.GetSkipCount(songA, events) >= SongStats.GetSkipCount(songB, events) ? songA : songB;

    // 4. Which song is played on more devices?
    public static SongModel GetSongWithMoreDevices(SongModel songA, SongModel songB, IReadOnlyCollection<DimmerPlayEvent> events)
        => SongStats.GetUniqueDevicesCount(songA, events) >= SongStats.GetUniqueDevicesCount(songB, events) ? songA : songB;

    // 5. Difference in total listening time (seconds)
    public static double GetListeningTimeDifference(SongModel songA, SongModel songB, IReadOnlyCollection<DimmerPlayEvent> events)
        => Math.Abs(SongStats.GetTotalListeningTime(songA, events) - SongStats.GetTotalListeningTime(songB, events));

    // 6. Both played on same device(s)
    public static List<string> GetCommonDevices(SongModel songA, SongModel songB, IReadOnlyCollection<DimmerPlayEvent> events)
    {
        var devicesA = SongStats.GetPlayedDevices(songA, events);
        var devicesB = SongStats.GetPlayedDevices(songB, events);
        return devicesA.Intersect(devicesB).ToList();
    }

    // 7. Are both songs ever played back-to-back (adjacent in history)?
    public static bool WerePlayedBackToBack(SongModel songA, SongModel songB, IReadOnlyCollection<DimmerPlayEvent> events)
    {
        var songEvents = events
            .Where(e => e.SongId == songA.Id || e.SongId == songB.Id)
            .OrderBy(e => e.DatePlayed)
            .ToList();
        for (int i = 0; i < songEvents.Count - 1; i++)
        {
            if ((songEvents[i].SongId == songA.Id && songEvents[i+1].SongId == songB.Id) ||
                (songEvents[i].SongId == songB.Id && songEvents[i+1].SongId == songA.Id))
                return true;
        }
        return false;
    }

    // 8. Which song is more often skipped before halfway?
    public static SongModel GetMoreSkippedBeforeHalf(SongModel songA, SongModel songB, IReadOnlyCollection<DimmerPlayEvent> events)
        => SongStats.GetSkipAfterHalfPlayedCount(songA, events) <= SongStats.GetSkipAfterHalfPlayedCount(songB, events) ? songA : songB;

    // 9. Both played on same day?
    public static bool WerePlayedSameDay(SongModel songA, SongModel songB, IReadOnlyCollection<DimmerPlayEvent> events)
    {
        var daysA = events.Where(e => e.SongId == songA.Id).Select(e => e.DatePlayed.Date).Distinct();
        var daysB = events.Where(e => e.SongId == songB.Id).Select(e => e.DatePlayed.Date).Distinct();
        return daysA.Intersect(daysB).Any();
    }

    // 10. Which song has a longer average session?
    public static SongModel GetSongWithLongerAvgSession(SongModel songA, SongModel songB, IReadOnlyCollection<DimmerPlayEvent> events)
        => SongStats.GetLongestSession(songA, events) >= SongStats.GetLongestSession(songB, events) ? songA : songB;

    // 11. Which song has more distinct days played?
    public static SongModel GetSongWithMoreDistinctDaysPlayed(SongModel songA, SongModel songB, IReadOnlyCollection<DimmerPlayEvent> events)
        => SongStats.GetDistinctDaysPlayed(songA, events) >= SongStats.GetDistinctDaysPlayed(songB, events) ? songA : songB;

    // 12. Which song has a higher average percent listened?
    public static SongModel GetSongWithHigherAvgPercentListened(SongModel songA, SongModel songB, IReadOnlyCollection<DimmerPlayEvent> events)
        => SongStats.GetAvgPercentListened(songA, events) >= SongStats.GetAvgPercentListened(songB, events) ? songA : songB;

    // 13. Which was played on more unique device models?
    public static SongModel GetSongWithMoreDeviceModels(SongModel songA, SongModel songB, IReadOnlyCollection<DimmerPlayEvent> events)
    {
        int a = events.Where(e => e.SongId == songA.Id && !string.IsNullOrEmpty(e.DeviceModel)).Select(e => e.DeviceModel!).Distinct().Count();
        int b = events.Where(e => e.SongId == songB.Id && !string.IsNullOrEmpty(e.DeviceModel)).Select(e => e.DeviceModel!).Distinct().Count();
        return a >= b ? songA : songB;
    }

    // 14. Both ever completed in same session? (Close timestamps)
    public static bool WereBothCompletedSameSession(SongModel songA, SongModel songB, IReadOnlyCollection<DimmerPlayEvent> events)
    {
        var aCompleted = events.Where(e => e.SongId == songA.Id && e.WasPlayCompleted);
        var bCompleted = events.Where(e => e.SongId == songB.Id && e.WasPlayCompleted);
        foreach (var a in aCompleted)
            foreach (var b in bCompleted)
                if (Math.Abs((a.DatePlayed - b.DatePlayed).TotalMinutes) < 10)
                    return true;
        return false;
    }

    // 15. Are both songs marked as favorite?
    public static bool AreBothFavorites(SongModel songA, SongModel songB)
        => songA.IsFavorite && songB.IsFavorite;

    // 16. Days between first play of A and first play of B
    public static int? DaysBetweenFirstPlays(SongModel songA, SongModel songB, IReadOnlyCollection<DimmerPlayEvent> events)
    {
        var firstA = SongStats.GetFirstPlayedDate(songA, events);
        var firstB = SongStats.GetFirstPlayedDate(songB, events);
        if (firstA == null || firstB == null)
            return null;
        return (int)Math.Abs((firstA.Value - firstB.Value).TotalDays);
    }

    // 17. Are both songs from same album?
    public static bool AreFromSameAlbum(SongModel songA, SongModel songB)
        => songA.AlbumName == songB.AlbumName;

    // 18. Are both songs from same artist?
    public static bool AreFromSameArtist(SongModel songA, SongModel songB)
        => songA.ArtistName == songB.ArtistName;

    // 19. Was one always played after the other (never before)?
    public static bool IsAlwaysPlayedAfter(SongModel songA, SongModel songB, IReadOnlyCollection<DimmerPlayEvent> events)
    {
        var playsA = events.Where(e => e.SongId == songA.Id).OrderBy(e => e.DatePlayed).Select(e => e.DatePlayed);
        var playsB = events.Where(e => e.SongId == songB.Id).OrderBy(e => e.DatePlayed).Select(e => e.DatePlayed);
        if (!playsA.Any() || !playsB.Any())
            return false;
        return playsA.Min() > playsB.Max();
    }

    // 20. Which song is skipped more often after 80%?
    public static SongModel GetSongSkippedMoreAfter80Percent(SongModel songA, SongModel songB, IReadOnlyCollection<DimmerPlayEvent> events)
    {
        int a = events.Count(e => e.SongId == songA.Id && e.PlayType == 5 && songA.DurationInSeconds > 0 && e.PositionInSeconds / songA.DurationInSeconds >= 0.8);
        int b = events.Count(e => e.SongId == songB.Id && e.PlayType == 5 && songB.DurationInSeconds > 0 && e.PositionInSeconds / songB.DurationInSeconds >= 0.8);
        return a >= b ? songA : songB;
    }

    // 21. Who has more play Events at night? (20:00-5:00)
    public static SongModel GetMoreNightPlays(SongModel songA, SongModel songB, IReadOnlyCollection<DimmerPlayEvent> events)
    {
        int a = events.Count(e => e.SongId == songA.Id && (e.DatePlayed.Hour >= 20 || e.DatePlayed.Hour <= 5));
        int b = events.Count(e => e.SongId == songB.Id && (e.DatePlayed.Hour >= 20 || e.DatePlayed.Hour <= 5));
        return a >= b ? songA : songB;
    }

    // 22. Who has more average skips per day?
    public static SongModel GetSongWithMoreSkipsPerDay(SongModel songA, SongModel songB, IReadOnlyCollection<DimmerPlayEvent> events)
    {
        double a = SongStats.GetSkipCount(songA, events) / (double)Math.Max(1, SongStats.GetDistinctDaysPlayed(songA, events));
        double b = SongStats.GetSkipCount(songB, events) / (double)Math.Max(1, SongStats.GetDistinctDaysPlayed(songB, events));
        return a >= b ? songA : songB;
    }

    // 23. Are both played on same manufacturer devices?
    public static bool HaveCommonManufacturer(SongModel songA, SongModel songB, IReadOnlyCollection<DimmerPlayEvent> events)
    {
        var a = events.Where(e => e.SongId == songA.Id && !string.IsNullOrEmpty(e.DeviceManufacturer)).Select(e => e.DeviceManufacturer!).Distinct();
        var b = events.Where(e => e.SongId == songB.Id && !string.IsNullOrEmpty(e.DeviceManufacturer)).Select(e => e.DeviceManufacturer!).Distinct();
        return a.Intersect(b).Any();
    }

    // 24. Which song has higher rating?
    public static SongModel GetHigherRatedSong(SongModel songA, SongModel songB)
        => songA.Rating >= songB.Rating ? songA : songB;

    // 25. Which song was last played on a different device?
    public static SongModel GetSongWithMoreRecentDifferentDevice(SongModel songA, SongModel songB, IReadOnlyCollection<DimmerPlayEvent> events)
    {
        var devA = SongStats.GetMostRecentDevice(songA, events);
        var devB = SongStats.GetMostRecentDevice(songB, events);
        if (devA == devB)
            return null;
        var lastA = SongStats.GetLastPlayedDate(songA, events);
        var lastB = SongStats.GetLastPlayedDate(songB, events);
        if (lastA == null && lastB == null)
            return null;
        if (lastA == null)
            return songB;
        if (lastB == null)
            return songA;
        return lastA >= lastB ? songA : songB;
    }
}