using System;
using System.Collections.Generic;
using System.Text;

namespace Dimmer.Charts.Services;

public enum DateRangeFilter
{
    
    AllTime,
    Today,
    Last7Days,
    Last15Days,
    Last30Days,
    Last365Days,
    Last90Days,
}

public class GeneralStatsService
{
    private readonly Realm _realm; // Assume injected/factory
    private readonly IRealmFactory realmF;

    // INPUT TRIGGERS
    private readonly BehaviorSubject<DateRangeFilter> _dateFilter = new(DateRangeFilter.AllTime);
    public void SetDateFilter(DateRangeFilter filter) => _dateFilter.OnNext(filter);

    // OUTPUTS (UI Binds to these)
    public IObservable<TextStat> TotalListeningTime { get; }
    public IObservable<IReadOnlyList<LeaderboardItem>> TopSongsInPeriod { get; }

    // "AI-Like" Insights
    public IObservable<IReadOnlyList<InsightStat>> LibraryInsights { get; }
    public IObservable<IReadOnlyList<DateChartPoint>> PlaysOverTimeChart { get; }



    // DynamicData collection for Leaderboards
    public IObservable<IChangeSet<SongModelView, ObjectId>> TopSongsLeaderboard { get; }

    public GeneralStatsService(IRealmFactory realmFactory)
    {
        _realm = realmFactory.GetRealmInstance();

        realmF = realmFactory;
        // 1. THE REACTIVE PIPELINE
        // Combine the Events table changes with the Date Filter changes
        var generalSnapshotStream = Observable.CombineLatest(
                _realm.All<DimmerPlayEvent>().AsObservableChangeSet().ToCollection(),
                _dateFilter,
                (allEvents, filter) => new { allEvents, filter }
            )
            .Throttle(TimeSpan.FromMilliseconds(500))
            // OFFLOAD TO BACKGROUND THREAD: Heavy math ahead!
            .Select(data => Observable.FromAsync(() => Task.Run(() =>
                CalculateGeneralSnapshot(data.allEvents, data.filter)
            )))
            .Switch() // Ensure only the latest calculation survives
            .Publish()
            .RefCount();

        // 2. DISPATCH OUTPUTS TO UI THREAD
        TotalListeningTime = generalSnapshotStream.Select(s => s.TotalTime).ObserveOn(RxSchedulers.UI);
        TopSongsInPeriod = generalSnapshotStream.Select(s => s.TopSongs).ObserveOn(RxSchedulers.UI);
        PlaysOverTimeChart = generalSnapshotStream.Select(s => s.PlaysOverTime).ObserveOn(RxSchedulers.UI);
        LibraryInsights = generalSnapshotStream.Select(s => s.Insights).ObserveOn(RxSchedulers.UI);
    }

    // THE BRAIN: Calculates everything efficiently in one pass
    private GeneralSnapshot CalculateGeneralSnapshot(IReadOnlyCollection<DimmerPlayEvent> allEvents, DateRangeFilter filter)
    {
        // 1. Apply Date Filter (Answers Q1)
        var cutoff = GetCutoffDate(filter);
        var periodEvents = allEvents.Where(e => e.EventDate >= cutoff).ToList();

        // 2. Basic Stats
        double totalSeconds = periodEvents.Sum(e => Math.Max(0, e.PositionInSeconds));
        var ts = TimeSpan.FromSeconds(totalSeconds);
        var totalTime = new TextStat("Total Time", $"{(int)ts.TotalDays}d {ts.Hours}h");

        // 3. Top Songs in Period (Answers Q1)
        var topSongs = periodEvents.Where(e => e.PlayType == 0)
            .GroupBy(e => e.SongId)
            .Where(g => g.Key.HasValue)
            .OrderByDescending(g => g.Count())
            .Take(10)
            .Select((g, i) =>
            {
                var song = GetSong(g.Key.Value);
                return new LeaderboardItem($"#{i + 1}", song?.Title ?? "Unknown", $"{g.Count()} plays");
            }).ToList();

        // 4. GENERATE INSIGHTS (The AI stuff)
        var insights = new List<InsightStat>();

        // Insight A: Acquired Tastes (Answers Q2)
        // Logic: Songs where the average date of SKIPS is significantly older than the average date of COMPLETIONS.
        var acquiredTaste = allEvents.Where(e => e.SongId.HasValue && (e.PlayType == 3 || e.PlayType == 5))
            .GroupBy(e => e.SongId)
            .Select(g => new
            {
                SongId = g.Key,
                AvgSkipDate = g.Where(x => x.PlayType == 5).Select(x =>
                {
                    var s = x.EventDate.Ticks ;
                    return s;
                }).DefaultIfEmpty(0).Average(),
                AvgCompleteDate = g.Where(x => x.PlayType == 3).Select(x => x.EventDate.Ticks).DefaultIfEmpty(0).Average(),
                TotalCompletes = g.Count(x => x.PlayType == 3)
            })
            // If skip date is > 0 (it was skipped), complete date is newer, and it has been completed a lot recently
            .Where(x => x.AvgSkipDate > 0 && x.AvgCompleteDate > x.AvgSkipDate && x.TotalCompletes > 5)
            .OrderByDescending(x => x.TotalCompletes)
            .FirstOrDefault();

        if (acquiredTaste != null)
        {
            var song = GetSong(acquiredTaste.SongId!.Value);
            insights.Add(new InsightStat("Acquired Taste",
                $"You used to skip '{song?.Title}', but you've completed it {acquiredTaste.TotalCompletes} times recently.", "📈"));
        }

        // Insight B: Forgotten Favorites (Answers Q3)
        // Logic: High play count all-time, but 0 plays in the last 15 days.
        var fifteenDaysAgo = DateTimeOffset.UtcNow.AddDays(-15);
        var forgottenFav = allEvents
            .GroupBy(e => e.SongId)
            .Select(g => new
            {
                SongId = g.Key,
                TotalPlays = g.Count(x => x.PlayType == 0),
                LastPlayed = g.Max(x => x.EventDate)
            })
            .Where(x => x.TotalPlays > 20 && x.LastPlayed < fifteenDaysAgo)
            .OrderByDescending(x => x.TotalPlays)
            .FirstOrDefault();

        if (forgottenFav != null)
        {
            var song = GetSong(forgottenFav.SongId!.Value);
            insights.Add(new InsightStat("Forgotten Favorite",
                $"You've played '{song?.Title}' {forgottenFav.TotalPlays} times, but haven't listened since {forgottenFav.LastPlayed:MMM yyyy}.", "🕸️"));
        }

        return new GeneralSnapshot(totalTime, topSongs, new List<DateChartPoint>(), insights);
    }

    private SongModel? GetSong(ObjectId id)
    {
        return realmF.GetRealmInstance().Find<SongModel>(id);
    }

    private DateTimeOffset GetCutoffDate(DateRangeFilter filter) => filter switch
    {
        DateRangeFilter.Last7Days => DateTimeOffset.UtcNow.AddDays(-7),
        DateRangeFilter.Last30Days => DateTimeOffset.UtcNow.AddDays(-30),
        DateRangeFilter.Last90Days => DateTimeOffset.UtcNow.AddDays(-90),
        DateRangeFilter.Last365Days => DateTimeOffset.UtcNow.AddYears(-1),
        _ => DateTimeOffset.MinValue
    };

    private record GeneralSnapshot(
        TextStat TotalTime,
        IReadOnlyList<LeaderboardItem> TopSongs,
        IReadOnlyList<DateChartPoint> PlaysOverTime,
        IReadOnlyList<InsightStat> Insights);

}