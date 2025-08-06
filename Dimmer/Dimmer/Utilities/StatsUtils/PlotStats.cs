namespace Dimmer.Utilities.StatsUtils;
using System;
using System.Collections.Generic;
using System.Linq;

using Dimmer.Data.Models;

// Helper for pie/bar plotting


public static class PlotStats
{
    // 1. Play count per song (Bar/Column)
    public static List<LabelValue> GetPlayCountPerSong(IReadOnlyCollection<SongModel> songs, IReadOnlyCollection<DimmerPlayEvent> events)
    {
        return [.. songs.Select(s => new LabelValue(s.Title, SongStats.GetPlayCount(s, events)))];
    }

    // 2. Play count per artist (Bar/Pie)
    public static List<LabelValue> GetPlayCountPerArtist(IReadOnlyCollection<SongModel> songs, IReadOnlyCollection<DimmerPlayEvent> events)
    {
        return [.. songs.GroupBy(s => s.ArtistName)
                    .Select(g => new LabelValue(g.Key, g.Sum(s => SongStats.GetPlayCount(s, events))))
                    .OrderByDescending(x => x.Value)];
    }

    // 3. Play count per album (Bar/Pie)
    public static List<LabelValue> GetPlayCountPerAlbum(IReadOnlyCollection<SongModel> songs, IReadOnlyCollection<DimmerPlayEvent> events)
    {
        return [.. songs.GroupBy(s => s.AlbumName)
                    .Select(g => new LabelValue(g.Key, g.Sum(s => SongStats.GetPlayCount(s, events))))
                    .OrderByDescending(x => x.Value)];
    }

    // 4. Play count per genre (Bar/Pie)
    public static List<LabelValue> GetPlayCountPerGenre(IReadOnlyCollection<SongModel> songs, IReadOnlyCollection<DimmerPlayEvent> events)
    {
        return [.. songs.GroupBy(s => s.Genre?.ToString() ?? "Unknown")
                    .Select(g => new LabelValue(g.Key, g.Sum(s => SongStats.GetPlayCount(s, events))))
                    .OrderByDescending(x => x.Value)];
    }

    // 5. Skips per song (Bar)
    public static List<LabelValue> GetSkipCountPerSong(IReadOnlyCollection<SongModel> songs, IReadOnlyCollection<DimmerPlayEvent> events)
    {
        return [.. songs.Select(s => new LabelValue(s.Title, SongStats.GetSkipCount(s, events)))];
    }

    // 6. Play count over time (Line/Scatter: x = date, y = plays)
    public static List<LabelValue> GetPlayCountOverTime(IReadOnlyCollection<DimmerPlayEvent> events)
    {
        return [.. events.Where(e => e.PlayType == 0)
                     .GroupBy(e => e.DatePlayed.Date)
                     .OrderBy(g => g.Key)
                     .Select(g => new LabelValue(g.Key.ToString("yyyy-MM-dd"), g.Count()))];
    }

    // 7. Skips over time (Line)
    public static List<LabelValue> GetSkipCountOverTime(IReadOnlyCollection<DimmerPlayEvent> events)
    {
        return [.. events.Where(e => e.PlayType == 5)
                     .GroupBy(e => e.DatePlayed.Date)
                     .OrderBy(g => g.Key)
                     .Select(g => new LabelValue(g.Key.ToString("yyyy-MM-dd"), g.Count()))];
    }

    // 8. Plays per hour of day (Bar/Line)
    public static List<LabelValue> GetPlaysByHour(IReadOnlyCollection<DimmerPlayEvent> events)
    {
        return [.. events.Where(e => e.PlayType == 0)
                     .GroupBy(e => e.DatePlayed.Hour)
                     .OrderBy(g => g.Key)
                     .Select(g => new LabelValue($"{g.Key}:00", g.Count()))];
    }

    // 9. Plays per day of week (Bar/Pie)
    public static List<LabelValue> GetPlaysByDayOfWeek(IReadOnlyCollection<DimmerPlayEvent> events)
    {
        return [.. events.Where(e => e.PlayType == 0)
                     .GroupBy(e => e.DatePlayed.DayOfWeek)
                     .OrderBy(g => g.Key)
                     .Select(g => new LabelValue(g.Key.ToString(), g.Count()))];
    }

    // 10. Play device usage (Pie/Bar)
    public static List<LabelValue> GetPlayDeviceDistribution(IReadOnlyCollection<DimmerPlayEvent> events)
    {
        return [.. events.Where(e => e.PlayType == 0 && !string.IsNullOrEmpty(e.DeviceName))
                     .GroupBy(e => e.DeviceName)
                     .Select(g => new LabelValue(g.Key, g.Count()))
                     .OrderByDescending(x => x.Value)];
    }

    // 11. Completion rate per song (Bar)
    public static List<LabelValue> GetCompletionRatePerSong(IReadOnlyCollection<SongModel> songs, IReadOnlyCollection<DimmerPlayEvent> events)
    {
        return [.. songs.Select(s =>
            {
                int total = SongStats.GetPlayCount(s, events);
                int completed = SongStats.GetCompletedPlayCount(s, events);
                double rate = total == 0 ? 0 : 100.0 * completed / total;
                return new LabelValue(s.Title, rate);
            })];
    }

    // 12. Favorite status distribution (Pie)
    public static List<LabelValue> GetFavoriteStatusDistribution(IReadOnlyCollection<SongModel> songs)
    {
        return new List<LabelValue>
        {
            new LabelValue("Favorite", songs.Count(s => s.IsFavorite)),
            new LabelValue("Not Favorite", songs.Count(s => !s.IsFavorite))
        };
    }

    // 13. Rating distribution (Bar/Pie)
    public static List<LabelValue> GetRatingDistribution(IReadOnlyCollection<SongModel> songs)
    {
        return [.. songs.GroupBy(s => s.Rating)
                    .OrderBy(g => g.Key)
                    .Select(g => new LabelValue($"Rating {g.Key}", g.Count()))];
    }

    // 14. Play count histogram (Bar/Bins)
    public static List<LabelValue> GetPlayCountHistogram(IReadOnlyCollection<SongModel> songs, IReadOnlyCollection<DimmerPlayEvent> events, int binSize = 5)
    {
        var hist = songs.GroupBy(s => SongStats.GetPlayCount(s, events) / binSize)
                        .OrderBy(g => g.Key)
                        .Select(g => new LabelValue($"{g.Key * binSize}-{(g.Key + 1) * binSize - 1}", g.Count()))
                        .ToList();
        return hist;
    }

    // 15. Play count per device form factor (Pie/Bar)
    public static List<LabelValue> GetPlayCountByDeviceFormFactor(IReadOnlyCollection<DimmerPlayEvent> events)
    {
        return [.. events.Where(e => e.PlayType == 0 && !string.IsNullOrEmpty(e.DeviceFormFactor))
                     .GroupBy(e => e.DeviceFormFactor)
                     .Select(g => new LabelValue(g.Key, g.Count()))
                     .OrderByDescending(x => x.Value)];
    }

    // 16. Play count per file format (Pie/Bar)
    public static List<LabelValue> GetPlayCountByFileFormat(IReadOnlyCollection<SongModel> songs, IReadOnlyCollection<DimmerPlayEvent> events)
    {
        return [.. songs.GroupBy(s => s.FileFormat)
                    .Select(g => new LabelValue(g.Key, g.Sum(s => SongStats.GetPlayCount(s, events))))
                    .OrderByDescending(x => x.Value)];
    }

    // 17. Total listening time per song (Bar)
    public static List<LabelValue> GetListeningTimePerSong(IReadOnlyCollection<SongModel> songs, IReadOnlyCollection<DimmerPlayEvent> events)
    {
        return [.. songs.Select(s => new LabelValue(s.Title, SongStats.GetTotalListeningTime(s, events) / 60.0))];
    }

    // 18. Average session duration per song (Bar)
    public static List<LabelValue> GetAvgSessionDurationPerSong(IReadOnlyCollection<SongModel> songs, IReadOnlyCollection<DimmerPlayEvent> events)
    {
        return [.. songs.Select(s =>
            {
                var playEvents = events.Where(e => e.SongId == s.Id).ToList();
                double avg = playEvents.Count!=0 ? playEvents.Average(e => (e.DateFinished - e.DatePlayed).TotalSeconds) : 0;
                return new LabelValue(s.Title, avg);
            })];
    }

    // 19. Number of unique devices per song (Bar)
    public static List<LabelValue> GetUniqueDeviceCountPerSong(IReadOnlyCollection<SongModel> songs, IReadOnlyCollection<DimmerPlayEvent> events)
    {
        return [.. songs.Select(s => new LabelValue(s.Title, SongStats.GetPlayedDevices(s, events).Count))];
    }

    // 20. Songs played per day (Line/Bar)
    public static List<LabelValue> GetSongsPlayedPerDay(IReadOnlyCollection<DimmerPlayEvent> events)
    {
        return [.. events.Where(e => e.PlayType == 0)
                     .GroupBy(e => e.DatePlayed.Date)
                     .OrderBy(g => g.Key)
                     .Select(g => new LabelValue(g.Key.ToString("yyyy-MM-dd"), g.Select(e => e.SongId).Distinct().Count()))];
    }

    // 21. Skips per device (Bar/Pie)
    public static List<LabelValue> GetSkipCountByDevice(IReadOnlyCollection<DimmerPlayEvent> events)
    {
        return [.. events.Where(e => e.PlayType == 5 && !string.IsNullOrEmpty(e.DeviceName))
                     .GroupBy(e => e.DeviceName)
                     .Select(g => new LabelValue(g.Key, g.Count()))
                     .OrderByDescending(x => x.Value)];
    }

    // 22. Completion rate per artist (Bar)
    public static List<LabelValue> GetCompletionRatePerArtist(IReadOnlyCollection<SongModel> songs, IReadOnlyCollection<DimmerPlayEvent> events)
    {
        return [.. songs.GroupBy(s => s.ArtistName)
                    .Select(g =>
                    {
                        int plays = g.Sum(s => SongStats.GetPlayCount(s, events));
                        int completes = g.Sum(s => SongStats.GetCompletedPlayCount(s, events));
                        double rate = plays == 0 ? 0 : 100.0 * completes / plays;
                        return new LabelValue(g.Key, rate);
                    })];
    }

    // 23. Duration distribution (Histogram/Bar)
    public static List<LabelValue> GetDurationHistogram(IReadOnlyCollection<SongModel> songs, int binSec = 60)
    {
        var hist = songs.GroupBy(s => (int)s.DurationInSeconds / binSec)
                        .OrderBy(g => g.Key)
                        .Select(g => new LabelValue($"{g.Key * binSec}-{(g.Key + 1) * binSec - 1}s", g.Count()))
                        .ToList();
        return hist;
    }

    // 24. Play/skip ratio per song (Scatter, Bar)
    public static List<LabelValue> GetPlaySkipRatioPerSong(IReadOnlyCollection<SongModel> songs, IReadOnlyCollection<DimmerPlayEvent> events)
    {
        return [.. songs.Select(s =>
            {
                int plays = SongStats.GetPlayCount(s, events);
                int skips = SongStats.GetSkipCount(s, events);
                double ratio = skips == 0 ? plays : (double)plays / skips;
                return new LabelValue(s.Title, ratio);
            })];
    }

    // 25. Hourly play heatmap data (Scatter/Heatmap)
    public static List<(int Hour, int DayOfWeek, int Count)> GetHourlyPlayHeatmap(IReadOnlyCollection<DimmerPlayEvent> events)
    {
        return [.. events.Where(e => e.PlayType == 0)
                     .GroupBy(e => new { e.DatePlayed.Hour, Day = (int)e.DatePlayed.DayOfWeek })
                     .Select(g => (g.Key.Hour, g.Key.Day, g.Count()))];
    }

    // 26. Song rating vs play count (Scatter)
    public static List<(int PlayCount, int Rating, string Title)> GetRatingVsPlayCount(IReadOnlyCollection<SongModel> songs, IReadOnlyCollection<DimmerPlayEvent> events)
    {
        return [.. songs.Select(s => (SongStats.GetPlayCount(s, events), s.Rating, s.Title))];
    }

    // 27. Days between first and last play per song (Bar)
    public static List<LabelValue> GetDaysActivePerSong(IReadOnlyCollection<SongModel> songs, IReadOnlyCollection<DimmerPlayEvent> events)
    {
        return [.. songs.Select(s =>
            {
                var first = SongStats.GetFirstPlayedDate(s, events);
                var last = SongStats.GetLastPlayedDate(s, events);
                int days = (first != null && last != null) ? (int)(last.Value - first.Value).TotalDays : 0;
                return new LabelValue(s.Title, days);
            })];
    }

    // 28. Favorite play count over time (Line)
    public static List<LabelValue> GetFavoritePlayCountOverTime(IReadOnlyCollection<SongModel> songs, IReadOnlyCollection<DimmerPlayEvent> events)
    {
        var favIds = songs.Where(s => s.IsFavorite).Select(s => s.Id).ToHashSet();
        return [.. events.Where(e => e.PlayType == 0 && e.SongId != null && favIds.Contains(e.SongId.Value))
                     .GroupBy(e => e.DatePlayed.Date)
                     .OrderBy(g => g.Key)
                     .Select(g => new LabelValue(g.Key.ToString("yyyy-MM-dd"), g.Count()))];
    }

    // 29. Album popularity over time (Line/Area)
    public static List<LabelValue> GetAlbumPopularityOverTime(IReadOnlyCollection<SongModel> songs, IReadOnlyCollection<DimmerPlayEvent> events, string album)
    {
        var songIds = songs.Where(s => s.AlbumName == album).Select(s => s.Id).ToHashSet();
        return [.. events.Where(e => e.PlayType == 0 && e.SongId != null && songIds.Contains(e.SongId.Value))
                     .GroupBy(e => e.DatePlayed.Date)
                     .OrderBy(g => g.Key)
                     .Select(g => new LabelValue(g.Key.ToString("yyyy-MM-dd"), g.Count()))];
    }

    // 30. Device usage by hour (Heatmap/Scatter)
    public static List<(string Device, int Hour, int Count)> GetDeviceUsageByHour(IReadOnlyCollection<DimmerPlayEvent> events)
    {
        return [.. events.Where(e => e.PlayType == 0 && !string.IsNullOrEmpty(e.DeviceName))
                     .GroupBy(e => new { e.DeviceName, e.DatePlayed.Hour })
                     .Select(g => (g.Key.DeviceName!, g.Key.Hour, g.Count()))];
    }

    // 31. Last digit distribution in play counts (Bar)
    public static List<LabelValue> GetPlayCountLastDigitDistribution(IReadOnlyCollection<SongModel> songs, IReadOnlyCollection<DimmerPlayEvent> events)
    {
        return [.. songs.Select(s => SongStats.GetPlayCount(s, events))
                    .GroupBy(c => c % 10)
                    .OrderBy(g => g.Key)
                    .Select(g => new LabelValue(g.Key.ToString(), g.Count()))];
    }

    // 32. Completion vs skips per song (Scatter)
    public static List<(int Skips, int Completes, string Title)> GetCompletionVsSkips(IReadOnlyCollection<SongModel> songs, IReadOnlyCollection<DimmerPlayEvent> events)
    {
        return [.. songs.Select(s => (SongStats.GetSkipCount(s, events), SongStats.GetCompletedPlayCount(s, events), s.Title))];
    }

    // 33. Total play/skip/completion per artist (Stacked Bar)
    public static List<(string Artist, int Plays, int Skips, int Completes)> GetArtistPlaySkipComplete(IReadOnlyCollection<SongModel> songs, IReadOnlyCollection<DimmerPlayEvent> events)
    {
        return [.. songs.GroupBy(s => s.ArtistName)
                    .Select(g => (
                        g.Key,
                        g.Sum(s => SongStats.GetPlayCount(s, events)),
                        g.Sum(s => SongStats.GetSkipCount(s, events)),
                        g.Sum(s => SongStats.GetCompletedPlayCount(s, events))))];
    }

    // 34. Most played hour per song (Scatter/Bar)
    public static List<LabelValue> GetMostPlayedHourPerSong(IReadOnlyCollection<SongModel> songs, IReadOnlyCollection<DimmerPlayEvent> events)
    {
        return [.. songs.Select(s =>
            {
                var hour = SongStats.GetMostActiveHour(s, events);
                return new LabelValue(s.Title, hour ?? -1);
            })];
    }

    // 35. Unique play days per song (Bar)
    public static List<LabelValue> GetUniquePlayDaysPerSong(IReadOnlyCollection<SongModel> songs, IReadOnlyCollection<DimmerPlayEvent> events)
    {
        return [.. songs.Select(s => new LabelValue(s.Title, SongStats.GetDistinctDaysPlayed(s, events)))];
    }
}
