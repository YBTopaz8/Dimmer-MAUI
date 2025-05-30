using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Utilities.StatsUtils;
/// <summary>
/// The collection stats.
/// </summary>
public static class CollectionStats
{
    // 1. Full summary for a collection
    /// <summary>
    /// Get the summary.
    /// </summary>
    /// <param name="songs">The songs.</param>
    /// <param name="Events">The events.</param>
    /// <returns>A CollectionStatsSummary</returns>
    public static CollectionStatsSummary GetSummary(IReadOnlyCollection<SongModel> songs, IReadOnlyCollection<DimmerPlayEvent> Events)
    {
        if (songs == null || songs.Count == 0)
            return new CollectionStatsSummary();

        var songIds = songs.Select(s => s.Id).ToHashSet();
        var relevantEvents = Events.Where(e => e.SongId.HasValue && songIds.Contains(e.SongId.Value)).ToList();

        var mostPlayed = songs.OrderByDescending(s => SongStats.GetPlayCount(s, Events)).FirstOrDefault();
        var mostSkipped = songs.OrderByDescending(s => SongStats.GetSkipCount(s, Events)).FirstOrDefault();

        var durations = songs.Select(s => s.DurationInSeconds).OrderBy(x => x).ToList();
        double medianDuration = durations.Count % 2 == 1 ?
            durations[durations.Count / 2] :
            (durations[durations.Count / 2] + durations[durations.Count / 2 - 1]) / 2;

        return new CollectionStatsSummary
        {
            TotalSongs = songs.Count,
            TotalPlayCount = songs.Sum(s => SongStats.GetPlayCount(s, Events)),
            TotalSkipCount = songs.Sum(s => SongStats.GetSkipCount(s, Events)),
            DistinctArtists = songs.Select(s => s.ArtistName).Distinct().Count(),
            DistinctAlbums = songs.Select(s => s.AlbumName).Distinct().Count(),
            AverageDuration = songs.Average(s => s.DurationInSeconds),
            TotalListeningTime = songs.Sum(s => SongStats.GetTotalListeningTime(s, Events)),
            UniqueDevices = relevantEvents.Where(e => !string.IsNullOrEmpty(e.DeviceName)).Select(e => e.DeviceName!).Distinct().Count(),
            SongsWithLyrics = songs.Count(s => s.HasLyrics),
            SongsWithSyncedLyrics = songs.Count(s => s.HasSyncedLyrics),
            SongsPlayedToCompletion = songs.Count(s => SongStats.WasEverCompleted(s, Events)),
            SongsFavorited = songs.Count(s => s.IsFavorite),
            MostPlayedSongCount = mostPlayed != null ? SongStats.GetPlayCount(mostPlayed, Events) : 0,
            MostPlayedSongTitle = mostPlayed?.Title,
            MostSkippedSongCount = mostSkipped != null ? SongStats.GetSkipCount(mostSkipped, Events) : 0,
            MostSkippedSongTitle = mostSkipped?.Title,
            EarliestAdded = songs.Min(s => s.DateCreated),
            LatestAdded = songs.Max(s => s.DateCreated),
            AverageRating = songs.Count == 0 ? 0 : songs.Average(s => s.Rating),
            MedianDuration = medianDuration,
            SongsNeverPlayed = songs.Count(s => SongStats.GetPlayCount(s, Events) == 0),
            SongsPlayedToday = songs.Count(s => Events.Any(e => e.SongId == s.Id && e.DatePlayed.Date == DateTimeOffset.Now.Date)),
            TotalDaysActive = relevantEvents.Select(e => e.DatePlayed.Date).Distinct().Count(),
            SongsPlayedAtNight = songs.Count(s => Events.Any(e => e.SongId == s.Id && (e.DatePlayed.Hour >= 22 || e.DatePlayed.Hour <= 5))),
            LongestSongSec = (int)songs.Max(s => s.DurationInSeconds),
            ShortestSongSec = (int)songs.Min(s => s.DurationInSeconds),
        };
    }

    // 2. List of all unique devices
    /// <summary>
    /// Get unique devices.
    /// </summary>
    /// <param name="songs">The songs.</param>
    /// <param name="Events">The events.</param>
    /// <returns><![CDATA[List<string>]]></returns>
    public static List<string> GetUniqueDevices(IReadOnlyCollection<SongModel> songs, IReadOnlyCollection<DimmerPlayEvent> Events)
    {
        var songIds = songs.Select(s => s.Id).ToHashSet();
        return Events.Where(e => e.SongId.HasValue && songIds.Contains(e.SongId.Value) && !string.IsNullOrEmpty(e.DeviceName))
                     .Select(e => e.DeviceName!)
                     .Distinct().ToList();
    }

    // 3. Distribution of play counts
    /// <summary>
    /// Get play count distribution.
    /// </summary>
    /// <param name="songs">The songs.</param>
    /// <param name="Events">The events.</param>
    /// <returns><![CDATA[Dictionary<int, int>]]></returns>
    public static Dictionary<int, int> GetPlayCountDistribution(IReadOnlyCollection<SongModel> songs, IReadOnlyCollection<DimmerPlayEvent> Events)
        => songs.GroupBy(s => SongStats.GetPlayCount(s, Events))
                .ToDictionary(g => g.Key, g => g.Count());

    // 4. Song(s) with max skips
    /// <summary>
    /// Get songs with max skips.
    /// </summary>
    /// <param name="songs">The songs.</param>
    /// <param name="Events">The events.</param>
    /// <returns><![CDATA[List<SongModel>]]></returns>
    public static List<SongModel> GetSongsWithMaxSkips(IReadOnlyCollection<SongModel> songs, IReadOnlyCollection<DimmerPlayEvent> Events)
    {
        int max = songs.Max(s => SongStats.GetSkipCount(s, Events));
        return songs.Where(s => SongStats.GetSkipCount(s, Events) == max).ToList();
    }

    // 5. Song(s) with min plays
    /// <summary>
    /// Get songs with min plays.
    /// </summary>
    /// <param name="songs">The songs.</param>
    /// <param name="Events">The events.</param>
    /// <returns><![CDATA[List<SongModel>]]></returns>
    public static List<SongModel> GetSongsWithMinPlays(IReadOnlyCollection<SongModel> songs, IReadOnlyCollection<DimmerPlayEvent> Events)
    {
        int min = songs.Min(s => SongStats.GetPlayCount(s, Events));
        return songs.Where(s => SongStats.GetPlayCount(s, Events) == min).ToList();
    }

    // 6. Song(s) most played on a single day
    /// <summary>
    /// Get song most played single day.
    /// </summary>
    /// <param name="songs">The songs.</param>
    /// <param name="Events">The events.</param>
    /// <returns>A SongModel?</returns>
    public static SongModel? GetSongMostPlayedSingleDay(IReadOnlyCollection<SongModel> songs, IReadOnlyCollection<DimmerPlayEvent> Events)
    {
        int max = 0;
        SongModel? result = null;
        foreach (var s in songs)
        {
            var groups = Events.Where(e => e.SongId == s.Id).GroupBy(e => e.DatePlayed.Date).Select(g => g.Count());
            if (groups.Any())
            {
                int dayMax = groups.Max();
                if (dayMax > max)
                { max = dayMax; result = s; }
            }
        }
        return result;
    }

    // 7. Song(s) played on most devices
    /// <summary>
    /// Get song played on most devices.
    /// </summary>
    /// <param name="songs">The songs.</param>
    /// <param name="Events">The events.</param>
    /// <returns>A SongModel?</returns>
    public static SongModel? GetSongPlayedOnMostDevices(IReadOnlyCollection<SongModel> songs, IReadOnlyCollection<DimmerPlayEvent> Events)
    {
        int max = 0;
        SongModel? res = null;
        foreach (var s in songs)
        {
            int count = SongStats.GetPlayedDevices(s, Events).Count;
            if (count > max)
            { max = count; res = s; }
        }
        return res;
    }

    // 8. Percent of songs played at least once
    /// <summary>
    /// Get percent songs played.
    /// </summary>
    /// <param name="songs">The songs.</param>
    /// <param name="Events">The events.</param>
    /// <returns>A double</returns>
    public static double GetPercentSongsPlayed(IReadOnlyCollection<SongModel> songs, IReadOnlyCollection<DimmerPlayEvent> Events)
        => 100.0 * songs.Count(s => SongStats.GetPlayCount(s, Events) > 0) / Math.Max(1, songs.Count);

    // 9. Top N played songs
    /// <summary>
    /// Get top N played songs.
    /// </summary>
    /// <param name="songs">The songs.</param>
    /// <param name="Events">The events.</param>
    /// <param name="n">The N.</param>
    /// <returns><![CDATA[List<SongModel>]]></returns>
    public static List<SongModel> GetTopNPlayedSongs(IReadOnlyCollection<SongModel> songs, IReadOnlyCollection<DimmerPlayEvent> Events, int n)
        => songs.OrderByDescending(s => SongStats.GetPlayCount(s, Events)).Take(n).ToList();

    // 10. Songs never skipped
    /// <summary>
    /// Get songs never skipped.
    /// </summary>
    /// <param name="songs">The songs.</param>
    /// <param name="Events">The events.</param>
    /// <returns><![CDATA[List<SongModel>]]></returns>
    public static List<SongModel> GetSongsNeverSkipped(IReadOnlyCollection<SongModel> songs, IReadOnlyCollection<DimmerPlayEvent> Events)
        => songs.Where(s => SongStats.GetSkipCount(s, Events) == 0).ToList();

    // 11. Songs with max duration
    /// <summary>
    /// Get songs with max duration.
    /// </summary>
    /// <param name="songs">The songs.</param>
    /// <param name="Events">The events.</param>
    /// <returns><![CDATA[List<SongModel>]]></returns>
    public static List<SongModel> GetSongsWithMaxDuration(IReadOnlyCollection<SongModel> songs, IReadOnlyCollection<DimmerPlayEvent> Events)
    {
        double max = songs.Max(s => s.DurationInSeconds);
        return songs.Where(s => s.DurationInSeconds == max).ToList();
    }

    // 12. Most common play hour for the collection
    /// <summary>
    /// Get most common play hour.
    /// </summary>
    /// <param name="songs">The songs.</param>
    /// <param name="Events">The events.</param>
    /// <returns>An int?</returns>
    public static int? GetMostCommonPlayHour(IReadOnlyCollection<SongModel> songs, IReadOnlyCollection<DimmerPlayEvent> Events)
    {
        var songIds = songs.Select(s => s.Id).ToHashSet();
        return Events.Where(e => e.SongId.HasValue && songIds.Contains(e.SongId.Value))
                     .GroupBy(e => e.DatePlayed.Hour)
                     .OrderByDescending(g => g.Count())
                     .FirstOrDefault()?.Key;
    }

    // 13. Number of songs played this week
    /// <summary>
    /// Get songs played this week.
    /// </summary>
    /// <param name="songs">The songs.</param>
    /// <param name="Events">The events.</param>
    /// <returns>An int</returns>
    public static int GetSongsPlayedThisWeek(IReadOnlyCollection<SongModel> songs, IReadOnlyCollection<DimmerPlayEvent> Events)
    {
        var weekAgo = DateTimeOffset.Now.AddDays(-7);
        var ids = songs.Select(s => s.Id).ToHashSet();
        return Events.Where(e => e.SongId.HasValue && ids.Contains(e.SongId.Value) && e.DatePlayed > weekAgo)
                     .Select(e => e.SongId!.Value)
                     .Distinct().Count();
    }

    // 14. Song(s) with highest average percent listened
    /// <summary>
    /// Get song with highest avg percent listened.
    /// </summary>
    /// <param name="songs">The songs.</param>
    /// <param name="Events">The events.</param>
    /// <returns>A SongModel?</returns>
    public static SongModel? GetSongWithHighestAvgPercentListened(IReadOnlyCollection<SongModel> songs, IReadOnlyCollection<DimmerPlayEvent> Events)
        => songs.OrderByDescending(s => SongStats.GetAvgPercentListened(s, Events)).FirstOrDefault();

    // 15. Count of songs played to completion at least once
    /// <summary>
    /// Count songs played converts to completion.
    /// </summary>
    /// <param name="songs">The songs.</param>
    /// <param name="Events">The events.</param>
    /// <returns>An int</returns>
    public static int CountSongsPlayedToCompletion(IReadOnlyCollection<SongModel> songs, IReadOnlyCollection<DimmerPlayEvent> Events)
        => songs.Count(s => SongStats.WasEverCompleted(s, Events));

    // 16. Count of favorite songs played today
    /// <summary>
    /// Count favorites played today.
    /// </summary>
    /// <param name="songs">The songs.</param>
    /// <param name="Events">The events.</param>
    /// <returns>An int</returns>
    public static int CountFavoritesPlayedToday(IReadOnlyCollection<SongModel> songs, IReadOnlyCollection<DimmerPlayEvent> Events)
    {
        var today = DateTimeOffset.Now.Date;
        return songs.Count(s => s.IsFavorite && Events.Any(e => e.SongId == s.Id && e.DatePlayed.Date == today));
    }

    // 17. Number of songs played only once
    /// <summary>
    /// Count songs played once.
    /// </summary>
    /// <param name="songs">The songs.</param>
    /// <param name="Events">The events.</param>
    /// <returns>An int</returns>
    public static int CountSongsPlayedOnce(IReadOnlyCollection<SongModel> songs, IReadOnlyCollection<DimmerPlayEvent> Events)
        => songs.Count(s => SongStats.GetPlayCount(s, Events) == 1);

    // 18. Average number of devices per song
    /// <summary>
    /// Avgs devices per song.
    /// </summary>
    /// <param name="songs">The songs.</param>
    /// <param name="Events">The events.</param>
    /// <returns>A double</returns>
    public static double AvgDevicesPerSong(IReadOnlyCollection<SongModel> songs, IReadOnlyCollection<DimmerPlayEvent> Events)
        => songs.Average(s => SongStats.GetPlayedDevices(s, Events).Count);

    // 19. Song(s) played most recently in collection
    /// <summary>
    /// Get most recently played song.
    /// </summary>
    /// <param name="songs">The songs.</param>
    /// <param name="Events">The events.</param>
    /// <returns>A SongModel?</returns>
    public static SongModel? GetMostRecentlyPlayedSong(IReadOnlyCollection<SongModel> songs, IReadOnlyCollection<DimmerPlayEvent> Events)
        => songs.OrderByDescending(s => SongStats.GetLastPlayedDate(s, Events) ?? DateTimeOffset.MinValue).FirstOrDefault();

    // 20. Count of songs skipped but never completed
    /// <summary>
    /// Count songs skipped never completed.
    /// </summary>
    /// <param name="songs">The songs.</param>
    /// <param name="Events">The events.</param>
    /// <returns>An int</returns>
    public static int CountSongsSkippedNeverCompleted(IReadOnlyCollection<SongModel> songs, IReadOnlyCollection<DimmerPlayEvent> Events)
        => songs.Count(s => SongStats.GetSkipCount(s, Events) > 0 && !SongStats.WasEverCompleted(s, Events));

    // 21. Most common device used in collection
    /// <summary>
    /// Get most common device.
    /// </summary>
    /// <param name="songs">The songs.</param>
    /// <param name="Events">The events.</param>
    /// <returns>A string?</returns>
    public static string? GetMostCommonDevice(IReadOnlyCollection<SongModel> songs, IReadOnlyCollection<DimmerPlayEvent> Events)
    {
        var songIds = songs.Select(s => s.Id).ToHashSet();
        return Events.Where(e => e.SongId.HasValue && songIds.Contains(e.SongId.Value) && !string.IsNullOrEmpty(e.DeviceName))
                     .GroupBy(e => e.DeviceName)
                     .OrderByDescending(g => g.Count())
                     .FirstOrDefault()?.Key;
    }

    // 22. Songs never played to completion
    /// <summary>
    /// Get songs never completed.
    /// </summary>
    /// <param name="songs">The songs.</param>
    /// <param name="Events">The events.</param>
    /// <returns><![CDATA[List<SongModel>]]></returns>
    public static List<SongModel> GetSongsNeverCompleted(IReadOnlyCollection<SongModel> songs, IReadOnlyCollection<DimmerPlayEvent> Events)
        => songs.Where(s => !SongStats.WasEverCompleted(s, Events)).ToList();

    // 23. Collection play count as Roman numeral
    /// <summary>
    /// Get play count roman.
    /// </summary>
    /// <param name="songs">The songs.</param>
    /// <param name="Events">The events.</param>
    /// <returns>A string</returns>
    public static string GetPlayCountRoman(IReadOnlyCollection<SongModel> songs, IReadOnlyCollection<DimmerPlayEvent> Events)
        => NerdySongStats.PlayCountRoman(new SongModel { }, new[] { new DimmerPlayEvent { SongId = new MongoDB.Bson.ObjectId() } }) // just use the method above with sum
            .Replace("N", songs.Sum(s => SongStats.GetPlayCount(s, Events)).ToString());

    // 24. Song(s) played at the exact same minute
    /// <summary>
    /// Get songs played at same minute.
    /// </summary>
    /// <param name="songs">The songs.</param>
    /// <param name="Events">The events.</param>
    /// <returns><![CDATA[List<(SongModel, SongModel)>]]></returns>
    public static List<(SongModel, SongModel)> GetSongsPlayedAtSameMinute(IReadOnlyCollection<SongModel> songs, IReadOnlyCollection<DimmerPlayEvent> Events)
    {
        var dict = new Dictionary<(int hour, int min), List<SongModel>>();
        foreach (var s in songs)
        {
            foreach (var e in Events.Where(e => e.SongId == s.Id))
            {
                var key = (e.DatePlayed.Hour, e.DatePlayed.Minute);
                if (!dict.ContainsKey(key))
                    dict[key] = new List<SongModel>();
                if (!dict[key].Contains(s))
                    dict[key].Add(s);
            }
        }
        return dict.Values.Where(list => list.Count > 1)
            .SelectMany(list => list.SelectMany((x, i) => list.Skip(i + 1).Select(y => (x, y)))).ToList();
    }

    // 25. Distribution of ratings in collection
    /// <summary>
    /// Get rating distribution.
    /// </summary>
    /// <param name="songs">The songs.</param>
    /// <param name="Events">The events.</param>
    /// <returns><![CDATA[Dictionary<int, int>]]></returns>
    public static Dictionary<int, int> GetRatingDistribution(IReadOnlyCollection<SongModel> songs, IReadOnlyCollection<DimmerPlayEvent> Events)
        => songs.GroupBy(s => s.Rating).ToDictionary(g => g.Key, g => g.Count());

    // 1. Eddington Number for collection (same idea: max E such that at least E songs have at least E plays)
    /// <summary>
    /// Get eddington number.
    /// </summary>
    /// <param name="songs">The songs.</param>
    /// <param name="Events">The events.</param>
    /// <returns>An int</returns>
    public static int GetEddingtonNumber(IReadOnlyCollection<SongModel> songs, IReadOnlyCollection<DimmerPlayEvent> Events)
    {
        var playCounts = songs.Select(s => SongStats.GetPlayCount(s, Events))
                              .OrderByDescending(x => x)
                              .ToList();
        int E = 0;
        for (int i = 0; i < playCounts.Count; i++)
            if (playCounts[i] >= i + 1)
                E = i + 1;
            else
                break;
        return E;
    }

    // 2. Fraction of collection whose play count is a Fibonacci number
    /// <summary>
    /// Fractions fibonacci play counts.
    /// </summary>
    /// <param name="songs">The songs.</param>
    /// <param name="Events">The events.</param>
    /// <returns>A double</returns>
    public static double FractionFibonacciPlayCounts(IReadOnlyCollection<SongModel> songs, IReadOnlyCollection<DimmerPlayEvent> Events)
    {
        bool IsFib(int n)
        {
            int a = 0, b = 1;
            while (b < n)
            { int t = b; b += a; a = t; }
            return n == 0 || n == 1 || n == b;
        }
        int fibs = songs.Count(s => IsFib(SongStats.GetPlayCount(s, Events)));
        return songs.Count == 0 ? 0 : (double)fibs / songs.Count;
    }

    // 3. Golden ratio of median play count to mean play count
    /// <summary>
    /// Goldens ratio median mean.
    /// </summary>
    /// <param name="songs">The songs.</param>
    /// <param name="Events">The events.</param>
    /// <returns>A double</returns>
    public static double GoldenRatioMedianMean(IReadOnlyCollection<SongModel> songs, IReadOnlyCollection<DimmerPlayEvent> Events)
    {
        var pcs = songs.Select(s => SongStats.GetPlayCount(s, Events)).OrderBy(x => x).ToList();
        double median = pcs.Count % 2 == 1 ? pcs[pcs.Count / 2] : (pcs[pcs.Count / 2] + pcs[pcs.Count / 2 - 1]) / 2.0;
        double mean = pcs.Count == 0 ? 0 : pcs.Average();
        return mean == 0 ? 0 : median / mean;
    }

    // 4. Total collection play count as sum of digits (nerdy stat)
    /// <summary>
    /// Collection play count digit sum.
    /// </summary>
    /// <param name="songs">The songs.</param>
    /// <param name="Events">The events.</param>
    /// <returns>An int</returns>
    public static int CollectionPlayCountDigitSum(IReadOnlyCollection<SongModel> songs, IReadOnlyCollection<DimmerPlayEvent> Events)
        => songs.Sum(s => SongStats.GetPlayCount(s, Events)).ToString().ToCharArray().Sum(c => c - '0');

    // 5. How many songs are played exactly "e" times (rounded)
    /// <summary>
    /// Count songs played E times.
    /// </summary>
    /// <param name="songs">The songs.</param>
    /// <param name="Events">The events.</param>
    /// <returns>An int</returns>
    public static int CountSongsPlayedETimes(IReadOnlyCollection<SongModel> songs, IReadOnlyCollection<DimmerPlayEvent> Events)
        => songs.Count(s => SongStats.GetPlayCount(s, Events) == (int)Math.Round(Math.E));

    // 6. Most common last digit in play counts
    /// <summary>
    /// Mosts common last digit play count.
    /// </summary>
    /// <param name="songs">The songs.</param>
    /// <param name="Events">The events.</param>
    /// <returns>An int</returns>
    public static int MostCommonLastDigitPlayCount(IReadOnlyCollection<SongModel> songs, IReadOnlyCollection<DimmerPlayEvent> Events)
        => songs.Select(s => SongStats.GetPlayCount(s, Events) % 10)
                .GroupBy(x => x).OrderByDescending(g => g.Count()).FirstOrDefault()?.Key ?? 0;

    // 7. Sum of unique primes in play counts
    /// <summary>
    /// Sums unique prime play counts.
    /// </summary>
    /// <param name="songs">The songs.</param>
    /// <param name="Events">The events.</param>
    /// <returns>An int</returns>
    public static int SumUniquePrimePlayCounts(IReadOnlyCollection<SongModel> songs, IReadOnlyCollection<DimmerPlayEvent> Events)
    {
        bool IsPrime(int n)
        {
            if (n < 2)
                return false;
            for (int i = 2; i <= Math.Sqrt(n); i++)
                if (n % i == 0)
                    return false;
            return true;
        }
        return songs.Select(s => SongStats.GetPlayCount(s, Events))
                    .Where(IsPrime)
                    .Distinct()
                    .Sum();
    }

    // 8. Collection play count divided by π
    /// <summary>
    /// Collection play count div pi.
    /// </summary>
    /// <param name="songs">The songs.</param>
    /// <param name="Events">The events.</param>
    /// <returns>A double</returns>
    public static double CollectionPlayCountDivPi(IReadOnlyCollection<SongModel> songs, IReadOnlyCollection<DimmerPlayEvent> Events)
        => songs.Sum(s => SongStats.GetPlayCount(s, Events)) / Math.PI;

    // 9. Number of songs whose play count is a multiple of 42
    /// <summary>
    /// Songs play count is42 multiple.
    /// </summary>
    /// <param name="songs">The songs.</param>
    /// <param name="Events">The events.</param>
    /// <returns>An int</returns>
    public static int SongsPlayCountIs42Multiple(IReadOnlyCollection<SongModel> songs, IReadOnlyCollection<DimmerPlayEvent> Events)
        => songs.Count(s => SongStats.GetPlayCount(s, Events) != 0 && SongStats.GetPlayCount(s, Events) % 42 == 0);

    // 10. Number of distinct Collatz steps for all play counts
    /// <summary>
    /// Distincts collatz step counts.
    /// </summary>
    /// <param name="songs">The songs.</param>
    /// <param name="Events">The events.</param>
    /// <returns>An int</returns>
    public static int DistinctCollatzStepCounts(IReadOnlyCollection<SongModel> songs, IReadOnlyCollection<DimmerPlayEvent> Events)
    {
        int Collatz(int n)
        {
            int steps = 0;
            while (n > 1)
            {
                n = n % 2 == 0 ? n / 2 : 3 * n + 1;
                steps++;
            }
            return steps;
        }
        return songs.Select(s => Collatz(SongStats.GetPlayCount(s, Events))).Distinct().Count();
    }
}
