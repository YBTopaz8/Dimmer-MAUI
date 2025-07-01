namespace Dimmer.Utilities.StatsUtils;
using Dimmer.Data.Models; // Ensure this using directive matches your project structure

using System;
using System.Collections.Generic;
using System.Linq;



public static class SongStatTwop
{
    #region Core Logic Methods (taking pre-filtered songSpecificEvents)

    // Defines what constitutes a "play" initiation for counting purposes.
    private static bool IsPlayInitiationEvent(DimmerPlayEvent e)
    {
        // Play: 0, Resume: 2, Restarted: 6, SeekRestarted: 7, CustomRepeat: 8, Previous: 9
        return e.PlayType == 0 || e.PlayType == 2 || e.PlayType == 6 || e.PlayType == 7 || e.PlayType == 8 || e.PlayType == 9;
    }

    private static int CoreGetPlayCount(SongModel song, IReadOnlyCollection<DimmerPlayEvent> songSpecificEvents)
    {
        return songSpecificEvents.Count(IsPlayInitiationEvent);
    }

    private static int CoreGetSkipCount(SongModel song, IReadOnlyCollection<DimmerPlayEvent> songSpecificEvents)
    {
        return songSpecificEvents.Count(e => e.PlayType == 5); // PlayType 5 is Skipped
    }

    private static double CoreGetTotalListeningTime(SongModel song, IReadOnlyCollection<DimmerPlayEvent> songSpecificEvents)
    {
        if (song.DurationInSeconds <= 0)
            return 0;

        double totalListeningTime = 0;

        // Add full duration for each event that signifies a completion.
        totalListeningTime += songSpecificEvents.Count(e => e.WasPlayCompleted) * song.DurationInSeconds;

        // Add partial listening time from skips that weren't already counted as completions.
        totalListeningTime += songSpecificEvents
            .Where(e => e.PlayType == 5 && !e.WasPlayCompleted)
            .Sum(e =>
            {
                double position = Math.Max(0, e.PositionInSeconds); // Ensure non-negative
                return Math.Min(position, song.DurationInSeconds); // Cap at song duration
            });

        return totalListeningTime;
    }

    private static bool CoreWasEverCompleted(SongModel song, IReadOnlyCollection<DimmerPlayEvent> songSpecificEvents)
    {
        // PlayType 3 is "Completed"
        return songSpecificEvents.Any(e => e.WasPlayCompleted || e.PlayType == 3);
    }

    private static List<string> CoreGetPlayedDevices(SongModel song, IReadOnlyCollection<DimmerPlayEvent> songSpecificEvents)
    {
        return [.. songSpecificEvents.Where(e => !string.IsNullOrEmpty(e.DeviceName))
                             .Select(e => e.DeviceName!)
                             .Distinct()
                             .OrderBy(name => name)];
    }

    private static double CoreGetAvgPercentListened(SongModel song, IReadOnlyCollection<DimmerPlayEvent> songSpecificEvents)
    {
        if (song.DurationInSeconds <= 0)
            return 0;

        var terminalEvents = songSpecificEvents
            .Where(e => e.PlayType == 1 || e.PlayType == 3 || e.PlayType == 5) // Pause, Completed, Skipped
            .ToList();

        if (terminalEvents.Count==0)
            return 0;

        double totalPercentageSum = 0;
        foreach (var e in terminalEvents)
        {
            double listenedDurationInSegment;
            if (e.WasPlayCompleted || e.PlayType == 3)
            {
                listenedDurationInSegment = song.DurationInSeconds;
            }
            else // Pause or Skip
            {
                listenedDurationInSegment = e.PositionInSeconds;
            }

            listenedDurationInSegment = Math.Max(0, listenedDurationInSegment);
            listenedDurationInSegment = Math.Min(listenedDurationInSegment, song.DurationInSeconds);

            totalPercentageSum += (listenedDurationInSegment / song.DurationInSeconds);
        }

        return (totalPercentageSum / terminalEvents.Count) * 100.0;
    }

    private static DateTimeOffset? CoreGetLastPlayedDate(SongModel song, IReadOnlyCollection<DimmerPlayEvent> songSpecificEvents)
    {
        if (songSpecificEvents.Count==0)
            return null;
        // Considers any event's DatePlayed for "last activity"
        return songSpecificEvents.Max(e => e.DatePlayed);
    }

    #endregion

    #region A. Methods for CollectionStats (take allEvents and filter internally)

    private static IReadOnlyCollection<DimmerPlayEvent> GetEventsForSong(SongModel song, IReadOnlyCollection<DimmerPlayEvent> allEvents)
    {
        return [.. allEvents.Where(e => e.SongId.HasValue && e.SongId.Value == song.Id)];
    }

    public static int GetPlayCount(SongModel song, IReadOnlyCollection<DimmerPlayEvent> allEvents)
    {
        var relevantEvents = GetEventsForSong(song, allEvents);
        return CoreGetPlayCount(song, relevantEvents);
    }

    public static int GetSkipCount(SongModel song, IReadOnlyCollection<DimmerPlayEvent> allEvents)
    {
        var relevantEvents = GetEventsForSong(song, allEvents);
        return CoreGetSkipCount(song, relevantEvents);
    }

    public static double GetTotalListeningTime(SongModel song, IReadOnlyCollection<DimmerPlayEvent> allEvents)
    {
        var relevantEvents = GetEventsForSong(song, allEvents);
        return CoreGetTotalListeningTime(song, relevantEvents);
    }

    public static bool WasEverCompleted(SongModel song, IReadOnlyCollection<DimmerPlayEvent> allEvents)
    {
        var relevantEvents = GetEventsForSong(song, allEvents);
        return CoreWasEverCompleted(song, relevantEvents);
    }

    public static List<string> GetPlayedDevices(SongModel song, IReadOnlyCollection<DimmerPlayEvent> allEvents)
    {
        var relevantEvents = GetEventsForSong(song, allEvents);
        return CoreGetPlayedDevices(song, relevantEvents);
    }

    public static double GetAvgPercentListened(SongModel song, IReadOnlyCollection<DimmerPlayEvent> allEvents)
    {
        var relevantEvents = GetEventsForSong(song, allEvents);
        return CoreGetAvgPercentListened(song, relevantEvents);
    }

    public static DateTimeOffset? GetLastPlayedDate(SongModel song, IReadOnlyCollection<DimmerPlayEvent> allEvents)
    {
        var relevantEvents = GetEventsForSong(song, allEvents);
        return CoreGetLastPlayedDate(song, relevantEvents);
    }
    #endregion

    #region B. Detailed stats for a single song (take pre-filtered songSpecificEvents)

    private static string GetPlayTypeName(int playType)
    {
        return playType switch
        {
            0 => "Play",
            1 => "Pause",
            2 => "Resume",
            3 => "Completed",
            4 => "Seeked",
            5 => "Skipped",
            6 => "Restarted",
            7 => "SeekRestarted",
            8 => "CustomRepeat",
            9 => "Previous",
            _ => $"Unknown ({playType})"
        };
    }

    /// <summary>Gets the total number of play initiations for the song from its specific events.</summary>
    public static int GetTotalPlays(SongModel song, IReadOnlyCollection<DimmerPlayEvent> songSpecificEvents)
    {
        return CoreGetPlayCount(song, songSpecificEvents);
    }

    /// <summary>Gets the total number of skips for the song from its specific events.</summary>
    public static int GetTotalSkips(SongModel song, IReadOnlyCollection<DimmerPlayEvent> songSpecificEvents)
    {
        return CoreGetSkipCount(song, songSpecificEvents);
    }

    /// <summary>Calculates the total listening time for the song from its specific events.</summary>
    public static double GetSongTotalListeningTime(SongModel song, IReadOnlyCollection<DimmerPlayEvent> songSpecificEvents)
    {
        return CoreGetTotalListeningTime(song, songSpecificEvents);
    }

    /// <summary>Calculates the average percentage of the song listened to per terminal event (Pause, Skip, Complete).</summary>
    public static double GetSongAvgPercentListened(SongModel song, IReadOnlyCollection<DimmerPlayEvent> songSpecificEvents)
    {
        return CoreGetAvgPercentListened(song, songSpecificEvents);
    }

    /// <summary>Checks if the song was ever marked as completed from its specific events.</summary>
    public static bool WasSongEverCompleted(SongModel song, IReadOnlyCollection<DimmerPlayEvent> songSpecificEvents)
    {
        return CoreWasEverCompleted(song, songSpecificEvents);
    }

    /// <summary>Gets the date this song was last played based on any of its specific events.</summary>
    public static DateTimeOffset? GetSongLastPlayedDate(SongModel song, IReadOnlyCollection<DimmerPlayEvent> songSpecificEvents)
    {
        return CoreGetLastPlayedDate(song, songSpecificEvents);
    }

    /// <summary>Gets the date this song was first played based on any of its specific events.</summary>
    public static DateTimeOffset? GetSongFirstPlayedDate(SongModel song, IReadOnlyCollection<DimmerPlayEvent> songSpecificEvents)
    {
        if (songSpecificEvents.Count==0)
            return null;
        return songSpecificEvents.Min(e => e.DatePlayed);
    }

    /// <summary>Counts how many times the song was completed, based on its specific events.</summary>
    public static int GetNumberOfCompletions(SongModel song, IReadOnlyCollection<DimmerPlayEvent> songSpecificEvents)
    {
        return songSpecificEvents.Count(e => e.WasPlayCompleted || e.PlayType == 3);
    }

    /// <summary>Gets a list of unique device names on which this song was played, from its specific events.</summary>
    public static List<string> GetSongPlayedDevices(SongModel song, IReadOnlyCollection<DimmerPlayEvent> songSpecificEvents)
    {
        return CoreGetPlayedDevices(song, songSpecificEvents);
    }

    /// <summary>Determines the most frequent hour of the day (0-23) the song was played.</summary>
    public static int? GetMostFrequentPlayHour(SongModel song, IReadOnlyCollection<DimmerPlayEvent> songSpecificEvents)
    {
        if (!songSpecificEvents.Any(IsPlayInitiationEvent))
            return null;
        return songSpecificEvents
                         .Where(IsPlayInitiationEvent)
                         .GroupBy(e => e.DatePlayed.Hour)
                         .OrderByDescending(g => g.Count())
                         .ThenBy(g => g.Key)
                         .FirstOrDefault()?.Key;
    }

    /// <summary>Gets the distribution of play initiations per day.</summary>
    public static List<LabelValue> GetPlayCountPerDay(SongModel song, IReadOnlyCollection<DimmerPlayEvent> songSpecificEvents)
    {
        if (!songSpecificEvents.Any(IsPlayInitiationEvent))
            return [];
        return [.. songSpecificEvents.Where(IsPlayInitiationEvent)
                         .GroupBy(e => e.DatePlayed.Date)
                         .Select(g => new LabelValue(g.Key.ToString("yyyy-MM-dd"), g.Count()))
                         .OrderBy(lv => lv.Label)];
    }

    /// <summary>Gets the distribution of play initiations per hour of the day.</summary>
    public static List<LabelValue> GetPlayCountPerHourOfDay(SongModel song, IReadOnlyCollection<DimmerPlayEvent> songSpecificEvents)
    {
        if (!songSpecificEvents.Any(IsPlayInitiationEvent))
            return [];
        return [.. songSpecificEvents.Where(IsPlayInitiationEvent)
                         .GroupBy(e => e.DatePlayed.Hour)
                         .Select(g => new LabelValue($"{g.Key:D2}:00 - {g.Key:D2}:59", g.Count()))
                         .OrderBy(lv => int.Parse(lv.Label.AsSpan(0, 2)))];
    }

    /// <summary>Gets the distribution of play initiations per day of the week.</summary>
    public static List<LabelValue> GetPlayCountPerDayOfWeek(SongModel song, IReadOnlyCollection<DimmerPlayEvent> songSpecificEvents)
    {
        if (!songSpecificEvents.Any(IsPlayInitiationEvent))
            return [];
        return [.. songSpecificEvents.Where(IsPlayInitiationEvent)
                         .GroupBy(e => e.DatePlayed.DayOfWeek)
                         .Select(g => new LabelValue(g.Key.ToString(), g.Count()))
                         .OrderBy(lv => (int)Enum.Parse<DayOfWeek>(lv.Label))];
    }

    /// <summary>Gets the distribution of play initiations per month.</summary>
    public static List<LabelValue> GetPlayCountPerMonth(SongModel song, IReadOnlyCollection<DimmerPlayEvent> songSpecificEvents)
    {
        if (!songSpecificEvents.Any(IsPlayInitiationEvent))
            return [];
        return [.. songSpecificEvents.Where(IsPlayInitiationEvent)
                         .GroupBy(e => new { e.DatePlayed.Year, e.DatePlayed.Month })
                         .Select(g => new LabelValue(new DateTime(g.Key.Year, g.Key.Month, 1).ToString("yyyy-MM"), g.Count()))
                         .OrderBy(lv => lv.Label)];
    }

    /// <summary>Gets a list of distinct dates on which the song had play initiations.</summary>
    public static List<DateTimeOffset> GetDatesPlayed(SongModel song, IReadOnlyCollection<DimmerPlayEvent> songSpecificEvents)
    {
        return [.. songSpecificEvents
                         .Where(IsPlayInitiationEvent)
                         .Select(e => e.DatePlayed.Date)
                         .Distinct()
                         .OrderBy(d => d)
                         .Select(d => new DateTimeOffset(d, TimeSpan.Zero))];
    }

    /// <summary>Calculates the longest streak of consecutive days the song was played.</summary>
    public static int GetLongestListeningStreak(SongModel song, IReadOnlyCollection<DimmerPlayEvent> songSpecificEvents)
    {
        var distinctPlayDates = GetDatesPlayed(song, songSpecificEvents).Select(d => d.Date).ToList();

        if (distinctPlayDates.Count == 0)
            return 0;
        if (distinctPlayDates.Count == 1)
            return 1;

        int maxStreak = 1;
        int currentStreak = 1;
        for (int i = 1; i < distinctPlayDates.Count; i++)
        {
            if (distinctPlayDates[i] == distinctPlayDates[i - 1].AddDays(1))
            {
                currentStreak++;
            }
            else
            {
                maxStreak = Math.Max(maxStreak, currentStreak);
                currentStreak = 1;
            }
        }
        return Math.Max(maxStreak, currentStreak);
    }

    /// <summary>Calculates the average time (in days) between play initiations.</summary>
    public static double? GetAverageTimeBetweenPlays(SongModel song, IReadOnlyCollection<DimmerPlayEvent> songSpecificEvents)
    {
        var playInitiationEvents = songSpecificEvents
            .Where(IsPlayInitiationEvent)
            .OrderBy(e => e.DatePlayed)
            .ToList();

        if (playInitiationEvents.Count < 2)
            return null;

        double totalDaysDifference = 0;
        for (int i = 1; i < playInitiationEvents.Count; i++)
        {
            totalDaysDifference += (playInitiationEvents[i].DatePlayed - playInitiationEvents[i-1].DatePlayed).TotalDays;
        }
        return totalDaysDifference / (playInitiationEvents.Count - 1);
    }

    /// <summary>Gets the distribution of all play event types for the song.</summary>
    public static List<LabelValue> GetPlayTypeDistribution(SongModel song, IReadOnlyCollection<DimmerPlayEvent> songSpecificEvents)
    {
        if (songSpecificEvents.Count==0)
            return [];
        return [.. songSpecificEvents.GroupBy(e => e.PlayType)
                         .Select(g => new LabelValue(GetPlayTypeName(g.Key), g.Count()))
                         .OrderByDescending(lv => lv.Value) // Common to sort by count
                         .ThenBy(lv => lv.Label)];
    }

    /// <summary>Counts the number of "Seeked" (PlayType 4) events.</summary>
    public static int GetSeekFrequency(SongModel song, IReadOnlyCollection<DimmerPlayEvent> songSpecificEvents)
    {
        return songSpecificEvents.Count(e => e.PlayType == 4);
    }

    /// <summary>Counts the number of "Restarted" or "SeekRestarted" (PlayType 6 or 7) events.</summary>
    public static int GetRestartFrequency(SongModel song, IReadOnlyCollection<DimmerPlayEvent> songSpecificEvents)
    {
        return songSpecificEvents.Count(e => e.PlayType == 6 || e.PlayType == 7);
    }

    /// <summary>Gets counts of play initiations on weekends vs weekdays.</summary>
    public static (int WeekendPlays, int WeekdayPlays) GetPlaysWeekendsVsWeekdays(SongModel song, IReadOnlyCollection<DimmerPlayEvent> songSpecificEvents)
    {
        var playInitiationEvents = songSpecificEvents.Where(IsPlayInitiationEvent);
        int weekendPlays = playInitiationEvents.Count(e => e.DatePlayed.DayOfWeek == DayOfWeek.Saturday || e.DatePlayed.DayOfWeek == DayOfWeek.Sunday);
        int weekdayPlays = playInitiationEvents.Count(e => e.DatePlayed.DayOfWeek >= DayOfWeek.Monday && e.DatePlayed.DayOfWeek <= DayOfWeek.Friday);
        return (weekendPlays, weekdayPlays);
    }

    /// <summary>Gets counts of play initiations during night vs day hours.</summary>
    /// <remarks>Night: 10 PM (22:00) to 5:59 AM. Day: 6:00 AM to 9:59 PM.</remarks>
    public static (int NightPlays, int DayPlays) GetPlaysNightVsDay(SongModel song, IReadOnlyCollection<DimmerPlayEvent> songSpecificEvents)
    {
        var playInitiationEvents = songSpecificEvents.Where(IsPlayInitiationEvent);
        int nightPlays = playInitiationEvents.Count(e => e.DatePlayed.Hour >= 22 || e.DatePlayed.Hour <= 5);
        int dayPlays = playInitiationEvents.Count(e => e.DatePlayed.Hour > 5 && e.DatePlayed.Hour < 22);
        return (nightPlays, dayPlays);
    }

    private static string ToRoman(int number)
    {
        if (number < 0 || number > 3999)
            return number.ToString();
        if (number == 0)
            return "N/A"; // Or "Nulla" or "" based on preference

        string[] M = ["", "M", "MM", "MMM"];
        string[] C = ["", "C", "CC", "CCC", "CD", "D", "DC", "DCC", "DCCC", "CM"];
        string[] X = ["", "X", "XX", "XXX", "XL", "L", "LX", "LXX", "LXXX", "XC"];
        string[] I = ["", "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX"];

        return M[number / 1000] + C[(number % 1000) / 100] + X[(number % 100) / 10] + I[number % 10];
    }

    /// <summary>Converts the song's play count to a Roman numeral string.</summary>
    public static string GetPlayCountRoman(SongModel song, IReadOnlyCollection<DimmerPlayEvent> songSpecificEvents)
    {
        int playCount = GetTotalPlays(song, songSpecificEvents);
        return ToRoman(playCount);
    }

    /// <summary>Checks if the song's play count is a Fibonacci number.</summary>
    public static bool IsPlayCountFibonacci(SongModel song, IReadOnlyCollection<DimmerPlayEvent> songSpecificEvents)
    {
        int playCount = GetTotalPlays(song, songSpecificEvents);
        if (playCount < 0)
            return false;
        if (playCount == 0 || playCount == 1)
            return true;
        int a = 0, b = 1;
        while (b < playCount && b > 0) // b > 0 check to prevent overflow issues with large Fibonacci numbers
        {
            int temp = b;
            b = a + b;
            a = temp;
        }
        return b == playCount;
    }

    /// <summary>Calculates the sum of the digits of the song's play count.</summary>
    public static int GetPlayCountDigitSum(SongModel song, IReadOnlyCollection<DimmerPlayEvent> songSpecificEvents)
    {
        int playCount = GetTotalPlays(song, songSpecificEvents);
        if (playCount < 0)
            playCount = 0;
        return playCount.ToString().Sum(c => c - '0');
    }

    /// <summary>Represents a summary of statistics for a single song.</summary>
    public class SongSingleStatsSummary
    {
        public string SongTitle { get; set; } = string.Empty;
        public string ArtistName { get; set; } = string.Empty;
        public double DurationInSeconds { get; set; }
        public int TotalPlays { get; set; }
        public int TotalSkips { get; set; }
        public int Completions { get; set; }
        public double TotalListeningTimeSeconds { get; set; }
        public string TotalListeningTimeFormatted { get; set; } = string.Empty;
        public double AveragePercentListened { get; set; }
        public DateTimeOffset? FirstPlayedDate { get; set; }
        public DateTimeOffset? LastPlayedDate { get; set; }
        public List<string> DevicesPlayedOn { get; set; } = [];
        public int? MostFrequentPlayHour { get; set; } // 0-23
        public string MostFrequentPlayHourFormatted { get; set; } = string.Empty;
        public int SeekFrequency { get; set; }
        public int RestartFrequency { get; set; }
        public bool WasEverCompleted { get; set; }
        public int LongestListeningStreakDays { get; set; }
        public double? AverageDaysBetweenPlays { get; set; }
        public string PlayCountInRomanNumerals { get; set; } = string.Empty;
        public bool IsPlayCountFibonacci { get; set; }
        public int PlayCountDigitSum { get; set; }
    }

    /// <summary>Generates a comprehensive summary of statistics for the given song and its events.</summary>
    public static SongSingleStatsSummary GetSingleSongSummary(SongModel song, IReadOnlyCollection<DimmerPlayEvent> songSpecificEvents)
    {
        if (song == null)
            throw new ArgumentNullException(nameof(song));

        var summary = new SongSingleStatsSummary
        {
            SongTitle = song.Title,
            ArtistName = song.ArtistName,
            DurationInSeconds = song.DurationInSeconds,
            TotalPlays = GetTotalPlays(song, songSpecificEvents),
            TotalSkips = GetTotalSkips(song, songSpecificEvents),
            Completions = GetNumberOfCompletions(song, songSpecificEvents),
            TotalListeningTimeSeconds = GetSongTotalListeningTime(song, songSpecificEvents),
            AveragePercentListened = GetSongAvgPercentListened(song, songSpecificEvents),
            FirstPlayedDate = GetSongFirstPlayedDate(song, songSpecificEvents),
            LastPlayedDate = GetSongLastPlayedDate(song, songSpecificEvents),
            DevicesPlayedOn = GetSongPlayedDevices(song, songSpecificEvents),
            MostFrequentPlayHour = GetMostFrequentPlayHour(song, songSpecificEvents),
            SeekFrequency = GetSeekFrequency(song, songSpecificEvents),
            RestartFrequency = GetRestartFrequency(song, songSpecificEvents),
            WasEverCompleted = WasSongEverCompleted(song, songSpecificEvents),
            LongestListeningStreakDays = GetLongestListeningStreak(song, songSpecificEvents),
            AverageDaysBetweenPlays = GetAverageTimeBetweenPlays(song, songSpecificEvents),
            PlayCountInRomanNumerals = GetPlayCountRoman(song, songSpecificEvents),
            IsPlayCountFibonacci = IsPlayCountFibonacci(song, songSpecificEvents),
            PlayCountDigitSum = GetPlayCountDigitSum(song, songSpecificEvents)
        };

        TimeSpan ts = TimeSpan.FromSeconds(summary.TotalListeningTimeSeconds);
        if ((int)ts.TotalDays > 0)
        {
            summary.TotalListeningTimeFormatted = $"{(int)ts.TotalDays}d {ts.Hours:D2}h {ts.Minutes:D2}m {ts.Seconds:D2}s";
        }
        else if (ts.TotalHours > 0)
        {
            summary.TotalListeningTimeFormatted = $"{ts.Hours:D2}h {ts.Minutes:D2}m {ts.Seconds:D2}s";
        }
        else
        {
            summary.TotalListeningTimeFormatted = $"{ts.Minutes:D2}m {ts.Seconds:D2}s";
        }

        if (summary.MostFrequentPlayHour.HasValue)
        {
            summary.MostFrequentPlayHourFormatted = $"{summary.MostFrequentPlayHour.Value:D2}:00 - {summary.MostFrequentPlayHour.Value:D2}:59";
        }

        return summary;
    }
    #endregion
}
