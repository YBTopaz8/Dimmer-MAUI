namespace Dimmer.Charts;

public static class CommonStatsHelper
{
    // 1. Total Listening Time
    public static TextStat GetTotalPlayTime(List<DimmerPlayEvent> events)
    {
        double totalSecs = events.Sum(e => Math.Max(0, e.PositionInSeconds));
        var ts = TimeSpan.FromSeconds(totalSecs);
        return new TextStat("Total Time", $"{(int)ts.TotalDays}d {ts.Hours}h {ts.Minutes}m");
    }

    // 2. Play vs Skip Ratio
    public static ChartPoint[] GetPlaySkipRatio(List<DimmerPlayEvent> events)
    {
        int comps = events.Count(e => e.PlayType == (int)PlayType.Completed);
        int skips = events.Count(e => e.PlayType == (int)PlayType.Skipped);
        return new[] { new ChartPoint("Completed", comps), new ChartPoint("Skipped", skips) };
    }

    // 3. Time of Day Heatmap (0-23 hours)
    public static IReadOnlyList<ChartPoint> GetTimeOfDayHeatmap(List<DimmerPlayEvent> events)
    {
        return events.GroupBy(e => e.DatePlayed.ToLocalTime().Hour)
                     .Select(g => new ChartPoint($"{g.Key}:00", g.Count(), g.Key))
                     .OrderBy(c => c.XValue).ToList();
    }

    // 4. Day of Week Heatmap
    public static IReadOnlyList<ChartPoint> GetDayOfWeekHeatmap(List<DimmerPlayEvent> events)
    {
        return events.GroupBy(e => e.DatePlayed.ToLocalTime().DayOfWeek)
                     .Select(g => new ChartPoint(g.Key.ToString(), g.Count(), (int)g.Key))
                     .OrderBy(c => c.XValue).ToList();
    }

    // 5. 12-Month Rolling Trend
    public static IReadOnlyList<TrendStat> GetRollingMonthlyTrend(List<DimmerPlayEvent> events)
    {
        var trend = new List<TrendStat>();
        for (int i = 0; i < 12; i++)
        {
            var start = DateTimeOffset.UtcNow.AddMonths(-(11 - i));
            var end = start.AddMonths(1);
            int count = events.Count(e => e.DatePlayed >= start && e.DatePlayed < end);
            trend.Add(new TrendStat(start.ToString("MMM yyyy"), count, 0)); // Change logic omitted for brevity
        }
        return trend;
    }

    // 6. Discovery & Lifespan
    public static TextStat GetDiscoveryLifespan(List<DimmerPlayEvent> events)
    {
        if (events.Count == 0) return new TextStat("Lifespan", "N/A");
        var first = events.Min(e => e.DatePlayed);
        var last = events.Max(e => e.DatePlayed);
        var days = (last - first).TotalDays;
        return new TextStat("Lifespan", $"{Math.Round(days)} days", $"Discovered {first:MMM d, yyyy}");
    }

    // 1. Time of Day Heatmap (0-23 hours) - Good for Radar or Bar Charts
    public static IReadOnlyList<ChartPoint> GetTimeOfDayHeatmap(IReadOnlyList<DimmerPlayEvent> events)
    {
        return events.GroupBy(e => e.DatePlayed.ToLocalTime().Hour)
                     .Select(g => new ChartPoint($"{g.Key}:00", g.Count(), g.Key))
                     .OrderBy(c => c.XValue).ToList();
    }

    // 2. Day of Week Heatmap - Good for Bar Charts
    public static IReadOnlyList<ChartPoint> GetDayOfWeekHeatmap(IReadOnlyList<DimmerPlayEvent> events)
    {
        return events.GroupBy(e => e.DatePlayed.ToLocalTime().DayOfWeek)
                     .Select(g => new ChartPoint(g.Key.ToString().Substring(0, 3), g.Count(), (int)g.Key))
                     .OrderBy(c => c.XValue).ToList();
    }

    // 3. 14-Day Rolling Daily Trend - Good for Line Charts
    public static IReadOnlyList<TrendStat> GetRollingDailyTrend(IReadOnlyList<DimmerPlayEvent> events)
    {
        var trend = new List<TrendStat>();
        for (int i = 0; i < 14; i++)
        {
            var targetDay = DateTimeOffset.UtcNow.Date.AddDays(-(13 - i));
            int cur = events.Count(e => e.DatePlayed.Date == targetDay);
            int prev = events.Count(e => e.DatePlayed.Date == targetDay.AddDays(-1));
            trend.Add(new TrendStat(targetDay.ToString("MMM dd"), cur, cur - prev));
        }
        return trend;
    }

    // 4. 12-Week Rolling Weekly Trend - Good for Bar or Line Charts
    public static IReadOnlyList<TrendStat> GetRollingWeeklyTrend(IReadOnlyList<DimmerPlayEvent> events)
    {
        var trend = new List<TrendStat>();
        for (int i = 0; i < 12; i++)
        {
            var start = DateTimeOffset.UtcNow.Date.AddDays(-(7 * (11 - i)));
            var end = start.AddDays(7);
            var prevStart = start.AddDays(-7);

            int cur = events.Count(e => e.DatePlayed >= start && e.DatePlayed < end);
            int prev = events.Count(e => e.DatePlayed >= prevStart && e.DatePlayed < start);
            trend.Add(new TrendStat($"Week of {start:MMM d}", cur, cur - prev));
        }
        return trend;
    }

    // 5. 12-Month Rolling Trend - Good for Bar Charts
    public static IReadOnlyList<TrendStat> GetRollingMonthlyTrend(IReadOnlyList<DimmerPlayEvent> events)
    {
        var trend = new List<TrendStat>();
        for (int i = 0; i < 12; i++)
        {
            var start = DateTimeOffset.UtcNow.AddMonths(-(11 - i));
            var end = start.AddMonths(1);
            int cur = events.Count(e => e.DatePlayed >= start && e.DatePlayed < end);
            int prev = events.Count(e => e.DatePlayed >= start.AddMonths(-1) && e.DatePlayed < start);
            trend.Add(new TrendStat(start.ToString("MMM yy"), cur, cur - prev));
        }
        return trend;
    }

    // 6. Play vs Skip Ratio - Good for Pie Charts
    public static IReadOnlyList<ChartPoint> GetPlaySkipRatio(IReadOnlyList<DimmerPlayEvent> events)
    {
        int comps = events.Count(e => e.PlayType == 3 || e.WasPlayCompleted); // 3 usually represents Completed
        int skips = events.Count(e => e.PlayType == 5); // 5 usually represents Skipped
        return new List<ChartPoint> { new("Completed", comps), new("Skipped", skips) };
    }



    // 7. Total Listening Time
    public static TextStat GetTotalPlayTime(IReadOnlyList<DimmerPlayEvent> events)
    {
        double totalSecs = events.Sum(e => Math.Max(0, e.PositionInSeconds));
        var ts = TimeSpan.FromSeconds(totalSecs);
        return new TextStat("Total Time", $"{(int)ts.TotalDays}d {ts.Hours}h", $"{ts.Minutes}m {ts.Seconds}s");
    }

    // 8. Discovery & Lifespan
    public static TextStat GetDiscoveryLifespan(IReadOnlyList<DimmerPlayEvent> events)
    {
        if (events.Count == 0) return new TextStat("Lifespan", "N/A");
        var first = events.Min(e => e.DatePlayed);
        var last = events.Max(e => e.DatePlayed);
        var days = (last - first).TotalDays;
        return new TextStat("Lifespan", $"{Math.Max(1, Math.Round(days))} days", $"Discovered {first:MMM d, yyyy}");
    }

    // 9. Weekend vs Weekday Warrior
    public static TextStat GetWeekendVsWeekday(IReadOnlyList<DimmerPlayEvent> events)
    {
        if (events.Count == 0) return new TextStat("Listening Habits", "N/A");
        int weekend = events.Count(e => e.DatePlayed.ToLocalTime().DayOfWeek == DayOfWeek.Saturday || e.DatePlayed.ToLocalTime().DayOfWeek == DayOfWeek.Sunday);
        int weekday = events.Count - weekend;

        string label = weekend > weekday ? "Weekend Warrior" : "Weekday Worker";
        double pct = weekend > weekday ? ((double)weekend / events.Count) * 100 : ((double)weekday / events.Count) * 100;

        return new TextStat("When You Listen", label, $"{pct:F0}% of plays");
    }

    // 10. Average Plays Per Active Day (Intensity)
    public static TextStat GetAveragePlaysPerActiveDay(IReadOnlyList<DimmerPlayEvent> events)
    {
        if (events.Count == 0) return new TextStat("Daily Intensity", "0 plays/day");
        int uniqueDays = events.Select(e => e.DatePlayed.Date).Distinct().Count();
        double avg = (double)events.Count / uniqueDays;
        return new TextStat("Daily Intensity", $"{avg:F1} plays", $"On days you actually listen");
    }

    // 11. Longest Drought (Gap between plays)
    public static TextStat GetLongestDrought(IReadOnlyList<DimmerPlayEvent> events)
    {
        if (events.Count < 2) return new TextStat("Longest Break", "N/A", "Not enough data");

        var orderedDates = events.Select(e => e.DatePlayed).OrderBy(d => d).ToList();
        double maxDroughtDays = 0;

        for (int i = 1; i < orderedDates.Count; i++)
        {
            var diff = (orderedDates[i] - orderedDates[i - 1]).TotalDays;
            if (diff > maxDroughtDays) maxDroughtDays = diff;
        }

        return new TextStat("Longest Break", $"{Math.Round(maxDroughtDays)} days", "Maximum gap between plays");
    }

    // 12. Consistency (Active Days Percentage)
    public static TextStat GetConsistencyScore(IReadOnlyList<DimmerPlayEvent> events)
    {
        if (events.Count == 0) return new TextStat("Consistency Score", "0%");

        var first = events.Min(e => e.DatePlayed).Date;
        var today = DateTimeOffset.UtcNow.Date;
        var lifespanDays = Math.Max(1, (today - first).TotalDays + 1);

        var activeDays = events.Select(e => e.DatePlayed.Date).Distinct().Count();
        double pct = (activeDays / lifespanDays) * 100;

        return new TextStat("Consistency Score", $"{pct:F1}%", "Days active since discovery");
    }

    // 13. Max Session Duration (Group events closer than 30 mins)
    public static TextStat GetMaxSessionDuration(IReadOnlyList<DimmerPlayEvent> events)
    {
        if (events.Count == 0) return new TextStat("Longest Session", "0m");

        var ordered = events.OrderBy(e => e.DatePlayed).ToList();
        double maxSessionSecs = 0;
        double currentSessionSecs = ordered[0].PositionInSeconds;

        for (int i = 1; i < ordered.Count; i++)
        {
            // If the gap between this play and the last play is less than 30 minutes, it's the same session
            if ((ordered[i].DatePlayed - ordered[i - 1].DatePlayed).TotalMinutes <= 30)
            {
                currentSessionSecs += ordered[i].PositionInSeconds;
            }
            else
            {
                if (currentSessionSecs > maxSessionSecs) maxSessionSecs = currentSessionSecs;
                currentSessionSecs = ordered[i].PositionInSeconds; // Reset
            }
        }
        if (currentSessionSecs > maxSessionSecs) maxSessionSecs = currentSessionSecs;

        var ts = TimeSpan.FromSeconds(maxSessionSecs);
        return new TextStat("Longest Session", ts.TotalHours >= 1 ? $"{(int)ts.TotalHours}h {ts.Minutes}m" : $"{ts.Minutes}m {ts.Seconds}s", "Continuous listening");
    }

    // 14. Peak Binge Intensity (Most plays in a single day)
    public static TextStat GetPeakBingeIntensity(IReadOnlyList<DimmerPlayEvent> events)
    {
        if (events.Count == 0) return new TextStat("Peak Binge", "0 plays");

        var maxDay = events.GroupBy(e => e.DatePlayed.Date)
                           .OrderByDescending(g => g.Count())
                           .FirstOrDefault();

        return new TextStat("Peak Binge", $"{maxDay?.Count() ?? 0} plays", maxDay?.Key.ToString("MMM d, yyyy") ?? "");
    }
}
