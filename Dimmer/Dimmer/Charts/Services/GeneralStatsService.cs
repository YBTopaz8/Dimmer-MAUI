using System;
using System.Collections.Generic;
using System.Text;
using static Parse.LiveQuery.Subscription;

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
    private readonly IRealmFactory _realmF;
    private readonly Realm _mainThreadRealm;
    private readonly BehaviorSubject<DateRangeFilter> _dateFilter = new(DateRangeFilter.AllTime);
    public void SetDateFilter(DateRangeFilter filter) => _dateFilter.OnNext(filter);

    // 6 Common
    public IObservable<TextStat> TotalTime { get; }
    public IObservable<IReadOnlyList<ChartPoint>> PlaySkipRatio { get; }
    public IObservable<IReadOnlyList<ChartPoint>> TimeOfDayHeatmap { get; }
    public IObservable<IReadOnlyList<ChartPoint>> DayOfWeekHeatmap { get; }
    public IObservable<IReadOnlyList<TrendStat>> MonthlyTrend { get; }
    public IObservable<TextStat> Lifespan { get; }

    // 10 Distinct + Leaderboards
    public IObservable<TextStat> IntrovertExtrovertScore { get; }
    public IObservable<TextStat> SkipToCompletionRatio { get; }
    public IObservable<TextStat> CenterOfGravityYear { get; }
    public IObservable<TextStat> ParetoPrinciple { get; }
    public IObservable<TextStat> PrimaryDecade { get; }
    public IObservable<TextStat> Adventurousness { get; }
    public IObservable<TextStat> LibraryChurn { get; }

    public IObservable<IReadOnlyList<LeaderboardItem>> TopSongs { get; }
    public IObservable<IReadOnlyList<LeaderboardItem>> TopArtists { get; }
    public IObservable<IReadOnlyList<LeaderboardItem>> MusicalTrinity { get; }
    public IObservable<IReadOnlyList<LeaderboardItem>> ForgottenGems { get; }
    public IObservable<IReadOnlyList<LeaderboardItem>> FibonacciArtists { get; }
    public IObservable<IReadOnlyList<HealthIssue>> LibraryHealth { get; }

    public GeneralStatsService(IRealmFactory realmF)
    {
        _realmF = realmF;
        _mainThreadRealm = _realmF.GetRealmInstance();
        var dbChangeSignal = _mainThreadRealm.All<DimmerPlayEvent>().AsObservableChangeSet().Select(_ => true);

        var snapshotStream = Observable.CombineLatest(dbChangeSignal, _dateFilter, (_, f) => f)
            .Throttle(TimeSpan.FromMilliseconds(500))
            .Select(filter => Observable.FromAsync(() => Task.Run(() =>
            {
                using var bgRealm = _realmF.GetRealmInstance();
                return CalculateSnapshot(bgRealm, filter);
            }))).Switch().Publish().RefCount();

        TotalTime = snapshotStream.Select(s => s.TotalTime).ObserveOn(RxSchedulers.UI);
        PlaySkipRatio = snapshotStream.Select(s => s.PlaySkipRatio).ObserveOn(RxSchedulers.UI);
        TimeOfDayHeatmap = snapshotStream.Select(s => s.TimeOfDayHeatmap).ObserveOn(RxSchedulers.UI);
        DayOfWeekHeatmap = snapshotStream.Select(s => s.DayOfWeekHeatmap).ObserveOn(RxSchedulers.UI);
        MonthlyTrend = snapshotStream.Select(s => s.MonthlyTrend).ObserveOn(RxSchedulers.UI);
        Lifespan = snapshotStream.Select(s => s.Lifespan).ObserveOn(RxSchedulers.UI);

        IntrovertExtrovertScore = snapshotStream.Select(s => s.IntroExtro).ObserveOn(RxSchedulers.UI);
        SkipToCompletionRatio = snapshotStream.Select(s => s.SkipRatio).ObserveOn(RxSchedulers.UI);
        CenterOfGravityYear = snapshotStream.Select(s => s.CenterYear).ObserveOn(RxSchedulers.UI);
        ParetoPrinciple = snapshotStream.Select(s => s.Pareto).ObserveOn(RxSchedulers.UI);
        PrimaryDecade = snapshotStream.Select(s => s.PrimaryDecade).ObserveOn(RxSchedulers.UI);
        Adventurousness = snapshotStream.Select(s => s.Adventurous).ObserveOn(RxSchedulers.UI);
        LibraryChurn = snapshotStream.Select(s => s.Churn).ObserveOn(RxSchedulers.UI);

        TopSongs = snapshotStream.Select(s => s.TopSongs).ObserveOn(RxSchedulers.UI);
        TopArtists = snapshotStream.Select(s => s.TopArtists).ObserveOn(RxSchedulers.UI);
        MusicalTrinity = snapshotStream.Select(s => s.Trinity).ObserveOn(RxSchedulers.UI);
        ForgottenGems = snapshotStream.Select(s => s.ForgottenGems).ObserveOn(RxSchedulers.UI);
        FibonacciArtists = snapshotStream.Select(s => s.Fibonacci).ObserveOn(RxSchedulers.UI);
        LibraryHealth = snapshotStream.Select(s => s.Health).ObserveOn(RxSchedulers.UI);
    }

    private GeneralSnapshot CalculateSnapshot(Realm bgRealm, DateRangeFilter filter)
    {
        var cutoff = filter switch { DateRangeFilter.Today => DateTimeOffset.UtcNow.Date, DateRangeFilter.Last7Days => DateTimeOffset.UtcNow.AddDays(-7), DateRangeFilter.Last30Days => DateTimeOffset.UtcNow.AddDays(-30), DateRangeFilter.Last365Days => DateTimeOffset.UtcNow.AddYears(-1), _ => DateTimeOffset.MinValue };
        var allEvents = bgRealm.All<DimmerPlayEvent>().ToList();
        var periodEvents = cutoff == DateTimeOffset.MinValue ? allEvents : allEvents.Where(e => e.EventDate >= cutoff).ToList();
        var allSongs = bgRealm.All<SongModel>().ToList();

        // 1 & 2. Introvert & SkipRatio
        int introvert = allSongs.Count(s => s.DurationInSeconds > 300 || !s.HasLyrics);
        var introExtro = new TextStat("Intro/Extrovert", allSongs.Count > 0 ? $"{((double)introvert / allSongs.Count) * 100:F0}% Introvert" : "N/A");
        double skips = periodEvents.Count(e => e.PlayType == 5);
        double comps = periodEvents.Count(e => e.PlayType == 3);
        var skipRatio = new TextStat("Global Skip Ratio", comps > 0 ? $"{skips / comps:F2}" : "0");

        // 3 & 4. Gravity & Decade
        var years = periodEvents.Select(e => bgRealm.Find<SongModel>(e.SongId)?.ReleaseYear).Where(y => y.HasValue).ToList();
        var centerYear = new TextStat("Center of Gravity", years.Count != 0 ? Math.Round(years.Average(y => y!.Value)).ToString() : "N/A");
        var decade = years.Select(y => (y!.Value / 10) * 10).GroupBy(d => d).OrderByDescending(g => g.Count()).FirstOrDefault();
        var primDecade = new TextStat("Primary Decade", decade != null ? $"{decade.Key}s" : "N/A");

        // 5. Pareto
        var artPlays = periodEvents.Where(e => e.PlayType == 0).GroupBy(e => bgRealm.Find<SongModel>(e.SongId)?.Artist?.Id).Where(g => g.Key != null).Select(g => g.Count()).OrderByDescending(c => c).ToList();
        double pPct = artPlays.Count != 0 ? (double)artPlays.Take(Math.Max(1, (int)(artPlays.Count * 0.2))).Sum() / artPlays.Sum() * 100 : 0;
        var pareto = new TextStat("Pareto (80/20)", $"{pPct:F1}%", "Plays from top 20% artists");

        // 6 & 7. Adv & Churn
        var adv = new TextStat("Adventurousness", $"{allEvents.GroupBy(e => e.SongId).Count(g => g.Count() == 1)} tracks", "Songs played exactly once");
        var churn = new TextStat("Library Churn", $"+{allSongs.Count(s => s.DateCreated > DateTimeOffset.UtcNow.AddDays(-30))}", "Songs added in last 30 days");

        // Leaderboards
        var topSongs = periodEvents.Where(e => e.PlayType == 0 && e.SongId.HasValue).GroupBy(e => e.SongId!.Value).OrderByDescending(g => g.Count()).Take(10).Select((g, i) => new LeaderboardItem($"#{i + 1}", bgRealm.Find<SongModel>(g.Key)?.Title ?? "Ukn", $"{g.Count()} plays", "")).ToList();
        var topArtistsObj = periodEvents.Where(e => e.PlayType == 0 && e.SongId.HasValue).GroupBy(e => bgRealm.Find<SongModel>(e.SongId.Value)?.Artist).Where(g => g.Key != null).OrderByDescending(g => g.Count()).Take(10).ToList();
        var topArt = topArtistsObj.Select((g, i) => new LeaderboardItem($"#{i + 1}", g.Key.Name, $"{g.Count()} plays", g.Key.ImagePath)).ToList();
        var trinity = topArt.Take(3).Select((a, i) => new LeaderboardItem($"Pillar {i + 1}", a.Name, "")).ToList();
        var forgot = allSongs.Where(s => s.PlayHistory.Count > 10 && s.PlayHistory.Max(p => p.DatePlayed) < DateTimeOffset.UtcNow.AddMonths(-6)).Take(10).Select((s, i) => new LeaderboardItem($"#{i + 1}", s.Title, "Long lost", s.CoverImagePath)).ToList();
        var fibs = new HashSet<int> { 1, 2, 3, 5, 8, 13, 21, 34, 55 };
        var startD = allEvents.Count != 0 ? allEvents.Min(e => e.DatePlayed) : DateTimeOffset.UtcNow;
        var fibArt = allEvents.GroupBy(e => bgRealm.Find<SongModel>(e.SongId)?.Artist).Where(g => g.Key != null && fibs.Contains((int)(g.Min(e => e.DatePlayed) - startD).TotalDays)).Select(g => new LeaderboardItem("", g.Key.Name, $"Day {(int)(g.Min(e => e.DatePlayed) - startD).TotalDays}")).ToList();


        // 1. Staleness Index (Plays of older songs vs newly added songs)
        var thirtyDaysAgo = DateTimeOffset.UtcNow.AddDays(-30);
        var oldSongs = allSongs.Where(s => s.DateCreated < thirtyDaysAgo).Select(s => s.Id).ToHashSet();
        int oldPlays =allEvents.Count(e => e.SongId.HasValue && oldSongs.Contains(e.SongId.Value));
        var staleStat = new TextStat("Staleness Index",allEvents.Count > 0 ? $"{((double)oldPlays /allEvents.Count) * 100:F0}%" : "0%", "Plays belonging to older tracks");

        // 2. The Core Anchor (Longest tenured, highly played song)
        var anchor = allSongs.OrderBy(s => s.DateCreated).ThenByDescending(s =>allEvents.Count(e => e.SongId == s.Id)).FirstOrDefault();
        var anchorStat = new TextStat("The Anchor", anchor?.Title ?? "N/A", "Oldest highly played track");

        // 3. Flow Disruption (Song causing the most playlist exits/skips)
        var disruptor = allSongs.OrderByDescending(s => allEvents.Count(e => e.SongId == s.Id && e.PlayType == 5)).FirstOrDefault();
        var disruptorStat = new TextStat("Flow Disruptor", disruptor?.Title ?? "N/A", "Highest skip count");

        // 4. BPM Range / Volatility
        var bpms = allSongs.Where(s => s.BPM > 0).Select(s => s.BPM!.Value).ToList();
        var bpmStat = new TextStat("BPM Range", bpms.Any() ? $"{bpms.Min()} - {bpms.Max()}" : "N/A", "Tempo volatility");

        // 5. Instrumental vs Vocal
        int inst = allSongs.Count(s => s.IsInstrumental == true || !s.HasLyrics);
        var instStat = new TextStat("Instrumental Ratio", allSongs.Count > 0 ? $"{((double)inst / allSongs.Count) * 100:F0}%" : "0%", "Non-vocal tracks");

        // 6. Top Decade represented
        var topDecade = allSongs.Where(s => s.ReleaseYear > 0).GroupBy(s => (s.ReleaseYear / 10) * 10).OrderByDescending(g => g.Count()).FirstOrDefault();
        var decadeStat = new TextStat("Defining Era", topDecade != null ? $"{topDecade.Key}s" : "N/A", "Most common release decade");

        // 7. Playlist Format (EP, LP, or Mixtape size)
        string sizeLabel = allSongs.Count < 7 ? "EP Sized" : allSongs.Count < 20 ? "LP Sized" : "Mixtape Sized";
        var sizeStat = new TextStat("Playlist Scale", sizeLabel, $"{allSongs.Count} tracks");

        // 8. Size-to-Time Ratio (Are these short punk tracks or long epics?)
        var ratioStat = new TextStat("Avg Track Size", allSongs.Count > 0 ? TimeSpan.FromSeconds(allSongs.Sum(s => s.DurationInSeconds) / allSongs.Count).ToString(@"mm\:ss") : "00:00", "Duration per track");

        return new GeneralSnapshot(staleStat,anchorStat,disruptorStat,bpmStat,instStat,decadeStat, sizeStat, ratioStat,
            CommonStatsHelper.GetTotalPlayTime(periodEvents), CommonStatsHelper.GetPlaySkipRatio(periodEvents), CommonStatsHelper.GetTimeOfDayHeatmap(periodEvents), CommonStatsHelper.GetDayOfWeekHeatmap(periodEvents), CommonStatsHelper.GetRollingMonthlyTrend(periodEvents), CommonStatsHelper.GetDiscoveryLifespan(periodEvents),
            introExtro, skipRatio, centerYear, pareto, primDecade, adv, churn, topSongs, topArt, trinity, forgot, fibArt, new List<HealthIssue>());
    }

    private record GeneralSnapshot(TextStat StaleStat,TextStat AnchorStat, TextStat DisruptorStat, TextStat Bpm, TextStat InstrumentalStat, TextStat DecadeStat, TextStat SizeStat, TextStat RadioStat, TextStat TotalTime, IReadOnlyList<ChartPoint> PlaySkipRatio, IReadOnlyList<ChartPoint> TimeOfDayHeatmap, IReadOnlyList<ChartPoint> DayOfWeekHeatmap, IReadOnlyList<TrendStat> MonthlyTrend, TextStat Lifespan, TextStat IntroExtro, TextStat SkipRatio, TextStat CenterYear, TextStat Pareto, TextStat PrimaryDecade, TextStat Adventurous, TextStat Churn, IReadOnlyList<LeaderboardItem> TopSongs, IReadOnlyList<LeaderboardItem> TopArtists, IReadOnlyList<LeaderboardItem> Trinity, IReadOnlyList<LeaderboardItem> ForgottenGems, IReadOnlyList<LeaderboardItem> Fibonacci, IReadOnlyList<HealthIssue> Health);
}