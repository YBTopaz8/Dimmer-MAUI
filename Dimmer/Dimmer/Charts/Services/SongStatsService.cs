using System;
using System.Collections.Generic;
using System.Text;

namespace Dimmer.Charts.Services;



public class SongStatsService
{
    private readonly IRealmFactory _realmF;
    private readonly Realm _mainThreadRealm;
    private readonly BehaviorSubject<ObjectId?> _currentSongId = new(null);
    private readonly BehaviorSubject<bool> _isLoading = new(false);

    public void SetSongId(ObjectId id) => _currentSongId.OnNext(id);
    public IObservable<bool> IsLoading => _isLoading.AsObservable();

    // 6 Common Outputs
    public IObservable<TextStat> TotalTime { get; }
    public IObservable<IReadOnlyList<ChartPoint>> PlaySkipRatio { get; }
    public IObservable<IReadOnlyList<ChartPoint>> TimeOfDayHeatmap { get; }
    public IObservable<IReadOnlyList<ChartPoint>> DayOfWeekHeatmap { get; }
    public IObservable<IReadOnlyList<TrendStat>> MonthlyTrend { get; }
    public IObservable<IReadOnlyList<TrendStat>> WeeklyTrend { get; }
    public IObservable<TextStat> Lifespan { get; }

    // 10 Distinct Song Outputs
    public IObservable<TextStat> CompletionRate { get; }
    public IObservable<TextStat> AvgListenDuration { get; }
    public IObservable<TextStat> BingeFactor { get; }
    public IObservable<TextStat> Predictability { get; }
    public IObservable<TextStat> EddingtonNumber { get; }
    public IObservable<TextStat> PlayStreak { get; }
    public IObservable<TextStat> TimeToSkip { get; }
    public IObservable<IReadOnlyList<ChartPoint>> ActionRadar { get; }
    public IObservable<IReadOnlyList<ChartPoint>> DropOffHeatmap { get; }
    public IObservable<IReadOnlyList<SongPairing>> PerfectPairings { get; }
    public IObservable<IReadOnlyList<TrendStat>> DailyTrend { get; internal set; }
    public object WeekendVsWeekday { get; internal set; }
    public object AveragePlaysPerActiveDay { get; internal set; }
    public object LongestDrought { get; internal set; }
    public object ConsistencyScore { get; internal set; }
    public object MaxSessionDuration { get; internal set; }
    public object PeakBingeIntensity { get; internal set; }
    public object HourOfPower { get; internal set; }
    public object SeasonalVibe { get; internal set; }
    public object RepeatOffender { get; internal set; }
    public object TimeBias { get; internal set; }
    public object Resurrection { get; internal set; }
    public object NextMilestone { get; internal set; }
    public object SkipTrend { get; internal set; }
    public object DiscoveryAnniversary { get; internal set; }

    public SongStatsService(IRealmFactory realmF)
    {
        _realmF = realmF;
        _mainThreadRealm = _realmF.GetRealmInstance();

        var snapshotStream = _currentSongId.Where(id => id.HasValue).Select(id => id.Value)
           .Select(songId => _mainThreadRealm.All<DimmerPlayEvent>().Where(e => e.SongId == songId).AsObservableChangeSet()
               .Throttle(TimeSpan.FromMilliseconds(250)).Select(_ => songId)
               .Select(id => Observable.FromAsync(() =>
               {
                   _isLoading.OnNext(true);
                   return Task.Run(() =>
                   {
                       using var bgRealm = _realmF.GetRealmInstance();
                       var bgSong = bgRealm.Find<SongModel>(id);
                       var songEvents = bgRealm.All<DimmerPlayEvent>().Filter("SongId == $0", (QueryArgument)id).OrderBy(e => e.DatePlayed).ToList();
                       var allEvents = bgRealm.All<DimmerPlayEvent>().OrderBy(e => e.DatePlayed).ToList();
                       return CalculateSnapshot(bgSong, songEvents, allEvents, bgRealm);
                   });
               })).Switch()).Switch().Do(_ => _isLoading.OnNext(false)).Publish().RefCount();

        // Bind Common
        TotalTime = snapshotStream.Select(s => s.TotalTime).ObserveOn(RxSchedulers.UI);
        PlaySkipRatio = snapshotStream.Select(s => s.PlaySkipRatio).ObserveOn(RxSchedulers.UI);
        TimeOfDayHeatmap = snapshotStream.Select(s => s.TimeOfDayHeatmap).ObserveOn(RxSchedulers.UI);
        DayOfWeekHeatmap = snapshotStream.Select(s => s.DayOfWeekHeatmap).ObserveOn(RxSchedulers.UI);
        MonthlyTrend = snapshotStream.Select(s => s.MonthlyTrend).ObserveOn(RxSchedulers.UI);
        WeeklyTrend = snapshotStream.Select(s => s.WeeklyTrend).ObserveOn(RxSchedulers.UI);
        Lifespan = snapshotStream.Select(s => s.Lifespan).ObserveOn(RxSchedulers.UI);

        // Bind Specific
        CompletionRate = snapshotStream.Select(s => s.CompRate).ObserveOn(RxSchedulers.UI);
        AvgListenDuration = snapshotStream.Select(s => s.AvgDuration).ObserveOn(RxSchedulers.UI);
        BingeFactor = snapshotStream.Select(s => s.Binge).ObserveOn(RxSchedulers.UI);
        Predictability = snapshotStream.Select(s => s.Predict).ObserveOn(RxSchedulers.UI);
        EddingtonNumber = snapshotStream.Select(s => s.Eddington).ObserveOn(RxSchedulers.UI);
        PlayStreak = snapshotStream.Select(s => s.Streak).ObserveOn(RxSchedulers.UI);
        TimeToSkip = snapshotStream.Select(s => s.TimeToSkip).ObserveOn(RxSchedulers.UI);
        ActionRadar = snapshotStream.Select(s => s.Radar).ObserveOn(RxSchedulers.UI);
        DropOffHeatmap = snapshotStream.Select(s => s.DropOff).ObserveOn(RxSchedulers.UI);
        PerfectPairings = snapshotStream.Select(s => s.Pairings).ObserveOn(RxSchedulers.UI);


        DiscoveryAnniversary = snapshotStream.Select(s => s.Lifespan).ObserveOn(RxSchedulers.UI);
    }
    private SongSnapshot CalculateSnapshot(SongModel? song, List<DimmerPlayEvent> events, List<DimmerPlayEvent> allEvents, Realm bgRealm)
    {
        if (song == null || events.Count == 0) return SongSnapshot.Empty();

        // ========================================================
        // OPTIMIZATION 1: Pre-filter lists so we only iterate ONCE
        // ========================================================
        var playEvents = new List<DimmerPlayEvent>();
        var skipEvents = new List<DimmerPlayEvent>();
        var completeEvents = new List<DimmerPlayEvent>();

        // Cache local times to save heavy CPU datetime math during groupings
        var localHours = new List<int>(events.Count);

        foreach (var e in events)
        {
            if (e.PlayType == 0) playEvents.Add(e);
            else if (e.PlayType == 5) skipEvents.Add(e);

            if (e.WasPlayCompleted || e.PlayType == 3) completeEvents.Add(e);

            localHours.Add(e.DatePlayed.ToLocalTime().Hour);
        }

        int plays = playEvents.Count;
        int skips = skipEvents.Count;
        int completes = completeEvents.Count;

        // ========================================================
        // FAST METRICS
        // ========================================================
        var compRate = new TextStat("Completion Rate", plays > 0 ? $"{((double)completes / plays) * 100:F1}%" : "0%");

        // Use the pre-filtered skip/complete events
        var avgDurList = events.Where(e => e.PlayType == 5 || e.PlayType == 3).Select(e => e.PositionInSeconds).ToList();
        var avgDur = new TextStat("Avg Listen", TimeSpan.FromSeconds(avgDurList.Count > 0 ? avgDurList.Average() : 0).ToString(@"mm\:ss"));

        var bingeGroup = completeEvents.GroupBy(e => e.DatePlayed.Date).OrderByDescending(g => g.Count()).FirstOrDefault();
        var binge = new TextStat("Binge Factor", bingeGroup != null ? $"{bingeGroup.Count()} plays in 1 day" : "N/A");

        var radar = new List<ChartPoint> {
        new("Plays", plays),
        new("Skips", skips),
        new("Completions", completes),
        new("Repeats", events.Count(e => e.PlayType == 6 || e.PlayType == 8))
    };

        var dropOff = skipEvents.Where(e => e.PositionInSeconds > 0)
            .GroupBy(e => Math.Floor(e.PositionInSeconds / 10) * 10)
            .Select(g => new ChartPoint($"{g.Key}s", g.Count(), g.Key))
            .OrderBy(c => c.XValue).ToList();

        // ========================================================
        // COMPLEX METRICS
        // ========================================================

        // Eddington Number
        var dailyPlays = events.GroupBy(e => e.DatePlayed.Date).Select(g => g.Count()).OrderByDescending(c => c).ToList();
        int eddington = dailyPlays.Where((count, index) => count >= index + 1).Count();
        var eddStat = new TextStat("Eddington No.", eddington.ToString(), $"Played {eddington}+ times on {eddington}+ days");

        // PlayAsync Streak
        int maxStreak = 0, currentStreak = 0;
        DateTime? lastDate = null;
        foreach (var date in events.Select(e => e.DatePlayed.Date).Distinct().OrderBy(d => d))
        {
            if (lastDate == null || (date - lastDate.Value).TotalDays == 1) currentStreak++;
            else currentStreak = 1;
            maxStreak = Math.Max(maxStreak, currentStreak);
            lastDate = date;
        }
        var streakStat = new TextStat("Max Play Streak", $"{maxStreak} Days", "Consecutive days played");

        // Time To Skip
        var avgPatience = skipEvents.Count != 0 ? TimeSpan.FromSeconds(skipEvents.Average(e => e.PositionInSeconds)) : TimeSpan.Zero;
        var patienceStat = new TextStat("Avg Time-to-Skip", avgPatience.ToString(@"mm\:ss"), "Patience before skipping");

        // Pairings (Using the optimized Realm Query)
        Dictionary<ObjectId, int> pairingsDict = new();
        int totalFollowUps = 0;

        foreach (var ev in events)
        {
            var nextEndTime = ev.DatePlayed.AddMinutes(15);
            var nextEvent = bgRealm.All<DimmerPlayEvent>()
                .Where(e => e.DatePlayed > ev.DatePlayed && e.DatePlayed <= nextEndTime)
                .OrderBy(e => e.DatePlayed)
                .FirstOrDefault();

            if (nextEvent != null && nextEvent.SongId.HasValue && nextEvent.SongId != song.Id)
            {
                pairingsDict[nextEvent.SongId.Value] = pairingsDict.GetValueOrDefault(nextEvent.SongId.Value) + 1;
                totalFollowUps++;
            }
        }

        var pairings = pairingsDict.OrderByDescending(kvp => kvp.Value).Take(10).Select(kvp =>
        {
            var pairedSongInDB = bgRealm.Find<SongModel>(kvp.Key);
            return new SongPairing(
                PairedSongTitle: pairedSongInDB?.Title ?? "Unknown Song",
                TimesPlayedTogether: kvp.Value,
                Context: "Played Next",
                CoverImagePath: pairedSongInDB?.CoverImagePath,
                songTitleDurationKey: pairedSongInDB?.TitleDurationKey,
                songId: kvp.Key,
                isPresentOnDevice: pairedSongInDB != null
            );
        }).ToList();

        var predict = new TextStat("Predictability", totalFollowUps > 0 && pairings.Count != 0 ? $"{((double)pairings.First().TimesPlayedTogether / totalFollowUps) * 100:F0}%" : "N/A", "Chance of playing top pair next");

        // ========================================================
        // DATE & TIME METRICS (Using the cached localHours!)
        // ========================================================
        var hourOfPower = localHours.GroupBy(h => h).OrderByDescending(g => g.Count()).FirstOrDefault();
        var hourOfPowerStat = new TextStat("Hour of Power", hourOfPower != null ? $"{hourOfPower.Key}:00" : "N/A", "Most frequent listening hour");

        var seasonPlays = events.GroupBy(e => (e.DatePlayed.Month % 12) / 3).OrderByDescending(g => g.Count()).FirstOrDefault();
        string seasonName = seasonPlays?.Key switch { 0 => "Winter", 1 => "Spring", 2 => "Summer", 3 => "Fall", _ => "Unknown" };
        var seasonalStat = new TextStat("Seasonal Vibe", seasonName, "Highest played season");

        // Repeat Offender
        int maxConsecutive = 0, currentConsecutive = 0;
        for (int i = 1; i < events.Count; i++)
        {
            if ((events[i].DatePlayed - events[i - 1].DatePlayed).TotalMinutes < (song.DurationInSeconds / 60.0) + 2) currentConsecutive++;
            else currentConsecutive = 0;
            maxConsecutive = Math.Max(maxConsecutive, currentConsecutive);
        }
        var repeatStat = new TextStat("Repeat Offender", $"{maxConsecutive + 1}x", "Most consecutive loops");

        // Night Owl vs Early Bird (Using cached localHours!)
        int amPlays = localHours.Count(h => h < 12);
        int pmPlays = events.Count - amPlays;
        var amPmStat = new TextStat("Time Bias", amPlays > pmPlays ? "Early Bird (AM)" : "Night Owl (PM)", $"{Math.Max(amPlays, pmPlays)} plays");

        // Resurrection Factor
        var firstPlay = events.Min(e => e.DatePlayed);
        var firstBinge = bingeGroup?.Key ?? firstPlay.Date;
        var resFactor = new TextStat("Resurrection", $"{(firstBinge - firstPlay).TotalDays:F0} days", "From discovery to peak binge");

        // Milestone Tracker
        int[] milestones = { 10, 50, 100, 500, 1000, 5000, 10000 };
        int nextMilestone = milestones.FirstOrDefault(m => m > plays);
        var milestoneStat = new TextStat("Next Milestone", nextMilestone > 0 ? $"{plays}/{nextMilestone}" : "Legendary", "Plays to next tier");

        // Skip Velocity (Using pre-filtered skipEvents!)
        var recentSkips = skipEvents.OrderByDescending(e => e.DatePlayed).Take(5).Select(e => e.PositionInSeconds).ToList();
        var oldSkips = skipEvents.OrderBy(e => e.DatePlayed).Take(5).Select(e => e.PositionInSeconds).ToList();
        double recentAvg = recentSkips.Any() ? recentSkips.Average() : 0;
        double oldAvg = oldSkips.Any() ? oldSkips.Average() : 0;
        var skipVelStat = new TextStat("Skip Trend", recentAvg > oldAvg ? "More Patient" : "Less Patient", "Patience trend over time");

        // Anniversary
        var nextAnni = new DateTimeOffset(DateTime.UtcNow.Year, firstPlay.Month, firstPlay.Day, 0, 0, 0, TimeSpan.Zero);
        if (nextAnni < DateTimeOffset.UtcNow) nextAnni = nextAnni.AddYears(1);
        var anniStat = new TextStat("Discovery Anniversary", $"In {(nextAnni - DateTimeOffset.UtcNow).TotalDays:F0} days", firstPlay.ToString("MMM d"));

        return new SongSnapshot(
            CommonStatsHelper.GetTotalPlayTime(events), seasonalStat, repeatStat, amPmStat, resFactor,
            CommonStatsHelper.GetPlaySkipRatio(events), milestoneStat, skipVelStat, anniStat,
            CommonStatsHelper.GetTimeOfDayHeatmap(events), CommonStatsHelper.GetDayOfWeekHeatmap(events),
            CommonStatsHelper.GetRollingMonthlyTrend(events), CommonStatsHelper.GetRollingWeeklyTrend(events),
            CommonStatsHelper.GetDiscoveryLifespan(events), compRate, avgDur, binge, predict, eddStat,
            streakStat, patienceStat, radar, dropOff, pairings);
    }
    private record SongSnapshot(TextStat TotalTime,TextStat SeasonalStat, TextStat RepeatStat, TextStat AmPmStat, TextStat ResFactor, IReadOnlyList<ChartPoint> PlaySkipRatio,TextStat MileStoneStat,TextStat SkipVelStat,TextStat AnnivStat, IReadOnlyList<ChartPoint> TimeOfDayHeatmap, IReadOnlyList<ChartPoint> DayOfWeekHeatmap, IReadOnlyList<TrendStat> MonthlyTrend,  IReadOnlyList<TrendStat> WeeklyTrend, TextStat Lifespan, TextStat CompRate, TextStat AvgDuration, TextStat Binge, TextStat Predict, TextStat Eddington, TextStat Streak, TextStat TimeToSkip, IReadOnlyList<ChartPoint> Radar, IReadOnlyList<ChartPoint> DropOff, IReadOnlyList<SongPairing> Pairings)
    {
        public static SongSnapshot Empty()
        {
            return new SongSnapshot(TotalTime: new TextStat("", ""), SeasonalStat: new TextStat("", ""), RepeatStat: new TextStat("", ""), AmPmStat: new TextStat("", ""), ResFactor: new TextStat("", ""), PlaySkipRatio: new List<ChartPoint>(), MileStoneStat: new TextStat("", ""), SkipVelStat: new TextStat("", ""), AnnivStat: new TextStat("", ""), TimeOfDayHeatmap: new List<ChartPoint>(), DayOfWeekHeatmap: new List<ChartPoint>(), MonthlyTrend: new List<TrendStat>(), WeeklyTrend: new List<TrendStat>(), Lifespan: new TextStat("", ""), CompRate: new TextStat("", ""), AvgDuration: new TextStat("", ""), Binge: new TextStat("", ""), Predict: new TextStat("", ""), Eddington: new TextStat("", ""), Streak: new TextStat("", ""), TimeToSkip: new TextStat("", ""), Radar: new List<ChartPoint>(), DropOff: new List<ChartPoint>(), Pairings: new List<SongPairing>());
        }
    }
}