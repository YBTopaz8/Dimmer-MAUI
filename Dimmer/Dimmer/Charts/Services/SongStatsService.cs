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

        int plays = events.Count(e => e.PlayType == 0);
        int skips = events.Count(e => e.PlayType == 5);
        int completes = events.Count(e => e.WasPlayCompleted || e.PlayType == 3);

        // Specifics
        var compRate = new TextStat("Completion Rate", plays > 0 ? $"{((double)completes / plays) * 100:F1}%" : "0%");
        var avgDur = new TextStat("Avg Listen", TimeSpan.FromSeconds(events.Where(e => e.PlayType == 5 || e.PlayType == 3).Select(e => e.PositionInSeconds).DefaultIfEmpty(0).Average()).ToString(@"mm\:ss"));
        var bingeGroup = events.Where(e => e.PlayType == 3).GroupBy(e => e.DatePlayed.Date).OrderByDescending(g => g.Count()).FirstOrDefault();
        var binge = new TextStat("Binge Factor", bingeGroup != null ? $"{bingeGroup.Count()} plays in 1 day" : "N/A");

        var radar = new List<ChartPoint> { new("Plays", plays), new("Skips", skips), new("Completions", completes), new("Repeats", events.Count(e => e.PlayType == 6 || e.PlayType == 8)) };
        var dropOff = events.Where(e => e.PlayType == 5 && e.PositionInSeconds > 0).GroupBy(e => Math.Floor(e.PositionInSeconds / 10) * 10).Select(g => new ChartPoint($"{g.Key}s", g.Count(), g.Key)).OrderBy(c => c.XValue).ToList();

        // 5. Eddington Number
        var dailyPlays = events.GroupBy(e => e.DatePlayed.Date).Select(g => g.Count()).OrderByDescending(c => c).ToList();
        int eddington = dailyPlays.Where((count, index) => count >= index + 1).Count();
        var eddStat = new TextStat("Eddington No.", eddington.ToString(), $"Played {eddington}+ times on {eddington}+ days");

        // 6. Play Streak
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

        // 7. Time To Skip
        var skipEvents = events.Where(e => e.PlayType == 5 && e.PositionInSeconds > 0).ToList();
        var avgPatience = skipEvents.Count != 0 ? TimeSpan.FromSeconds(skipEvents.Average(e => e.PositionInSeconds)) : TimeSpan.Zero;
        var patienceStat = new TextStat("Avg Time-to-Skip", avgPatience.ToString(@"mm\:ss"), "Patience before skipping");

        // Pairings & Predictability
        Dictionary<ObjectId, int> pairingsDict = new();
        int totalFollowUps = 0;
        for (int i = 0; i < allEvents.Count - 1; i++)
        {
            if (allEvents[i].SongId == song.Id && allEvents[i + 1].SongId.HasValue && allEvents[i + 1].SongId != song.Id && (allEvents[i + 1].DatePlayed - allEvents[i].DatePlayed).TotalMinutes < 15)
            {
                pairingsDict[allEvents[i + 1].SongId.Value] = pairingsDict.GetValueOrDefault(allEvents[i + 1].SongId.Value) + 1;
                totalFollowUps++;
            }
        }
        var pairings = pairingsDict.OrderByDescending(kvp => kvp.Value).Take(10).Select(kvp =>
        {
            var dimEvent = allEvents.First(x => x.SongId == kvp.Key).ToDimmerPlayEventView()! ;
            bool isPresentOnDevice=false;
            var songInDB = bgRealm.Find<SongModel>(dimEvent.SongId);

            dimEvent.SongId = ObjectId.Empty;
            if (songInDB is not null)
            {
                isPresentOnDevice = true;
                dimEvent.SongId = songInDB.Id;
                dimEvent.CoverImagePath = songInDB.CoverImagePath;
            }
            var songPair = new SongPairing(dimEvent.SongName, kvp.Value, "Played Next", dimEvent.CoverImagePath, null,dimEvent.SongId,isPresentOnDevice);
            return songPair;
        }).ToList();
        var predict = new TextStat("Predictability", totalFollowUps > 0 && pairings.Count != 0 ? $"{((double)pairings.First().TimesPlayedTogether / totalFollowUps) * 100:F0}%" : "N/A", "Chance of playing top pair next");

        // 1. Hour of Power (The specific hour this song peaks)
        var hourOfPower = events.GroupBy(e => e.DatePlayed.ToLocalTime().Hour).OrderByDescending(g => g.Count()).FirstOrDefault();
        var hourOfPowerStat = new TextStat("Hour of Power", hourOfPower != null ? $"{hourOfPower.Key}:00" : "N/A", "Most frequent listening hour");

        // 2. Seasonal Preference (Spring, Summer, Fall, Winter)
        var seasonPlays = events.GroupBy(e => (e.DatePlayed.Month % 12) / 3).OrderByDescending(g => g.Count()).FirstOrDefault();
        string seasonName = seasonPlays?.Key switch { 0 => "Winter", 1 => "Spring", 2 => "Summer", 3 => "Fall", _ => "Unknown" };
        var seasonalStat = new TextStat("Seasonal Vibe", seasonName, "Highest played season");

        // 3. Repeat Offender (Max consecutive plays in one sitting)
        int maxConsecutive = 0, currentConsecutive = 0;
        for (int i = 1; i < events.Count; i++)
        {
            if ((events[i].DatePlayed - events[i - 1].DatePlayed).TotalMinutes < (song.DurationInSeconds / 60.0) + 2) currentConsecutive++;
            else currentConsecutive = 0;
            maxConsecutive = Math.Max(maxConsecutive, currentConsecutive);
        }
        var repeatStat = new TextStat("Repeat Offender", $"{maxConsecutive + 1}x", "Most consecutive loops");

        // 4. Night Owl vs Early Bird (AM vs PM dominance)
        int amPlays = events.Count(e => e.DatePlayed.ToLocalTime().Hour < 12);
        int pmPlays = events.Count - amPlays;
        var amPmStat = new TextStat("Time Bias", amPlays > pmPlays ? "Early Bird (AM)" : "Night Owl (PM)", $"{Math.Max(amPlays, pmPlays)} plays");

        // 5. Resurrection Factor (Longest time between discovering and actually bingeing)
        var firstPlay = events.Min(e => e.DatePlayed);
        var firstBinge = bingeGroup?.Key ?? firstPlay.Date; // From your existing binge logic
        var resFactor = new TextStat("Resurrection", $"{(firstBinge - firstPlay).TotalDays:F0} days", "From discovery to peak binge");

        // 6. Milestone Tracker (Next big play milestone)
        int[] milestones = { 10, 50, 100, 500, 1000 };
        int nextMilestone = milestones.FirstOrDefault(m => m > plays);
        var milestoneStat = new TextStat("Next Milestone", nextMilestone > 0 ? $"{plays}/{nextMilestone}" : "Legendary", "Plays to next tier");

        // 7. Skip Velocity (Are they skipping earlier or later over time?)
        var recentSkips = events.Where(e => e.PlayType == 5).OrderByDescending(e => e.DatePlayed).Take(5).Select(e => e.PositionInSeconds);
        var oldSkips = events.Where(e => e.PlayType == 5).OrderBy(e => e.DatePlayed).Take(5).Select(e => e.PositionInSeconds);
        double recentAvg = recentSkips.Any() ? recentSkips.Average() : 0;
        double oldAvg = oldSkips.Any() ? oldSkips.Average() : 0;
        var skipVelStat = new TextStat("Skip Trend", recentAvg > oldAvg ? "More Patient" : "Less Patient", "Patience trend over time");

        // 8. Anniversary (When is its next discovery birthday?)
        var nextAnni = new DateTimeOffset(DateTime.UtcNow.Year, firstPlay.Month, firstPlay.Day, 0, 0, 0, TimeSpan.Zero);
        if (nextAnni < DateTimeOffset.UtcNow) nextAnni = nextAnni.AddYears(1);
        var anniStat = new TextStat("Discovery Anniversary", $"In {(nextAnni - DateTimeOffset.UtcNow).TotalDays:F0} days", firstPlay.ToString("MMM d"));


        return new SongSnapshot(CommonStatsHelper.GetTotalPlayTime(events),seasonalStat,repeatStat,amPmStat,resFactor, CommonStatsHelper.GetPlaySkipRatio(events),milestoneStat,skipVelStat,anniStat,CommonStatsHelper.GetTimeOfDayHeatmap(events), CommonStatsHelper.GetDayOfWeekHeatmap(events),
            CommonStatsHelper.GetRollingMonthlyTrend(events), CommonStatsHelper.GetRollingWeeklyTrend(events), CommonStatsHelper.GetDiscoveryLifespan(events),  
            compRate, avgDur, binge, predict, eddStat, streakStat, patienceStat, radar, dropOff, pairings);
    }

    private record SongSnapshot(TextStat TotalTime,TextStat SeasonalStat, TextStat RepeatStat, TextStat AmPmStat, TextStat ResFactor, IReadOnlyList<ChartPoint> PlaySkipRatio,TextStat MileStoneStat,TextStat SkipVelStat,TextStat AnnivStat, IReadOnlyList<ChartPoint> TimeOfDayHeatmap, IReadOnlyList<ChartPoint> DayOfWeekHeatmap, IReadOnlyList<TrendStat> MonthlyTrend,  IReadOnlyList<TrendStat> WeeklyTrend, TextStat Lifespan, TextStat CompRate, TextStat AvgDuration, TextStat Binge, TextStat Predict, TextStat Eddington, TextStat Streak, TextStat TimeToSkip, IReadOnlyList<ChartPoint> Radar, IReadOnlyList<ChartPoint> DropOff, IReadOnlyList<SongPairing> Pairings)
    {
        public static SongSnapshot Empty()
        {
            return new SongSnapshot(TotalTime: new TextStat("", ""), SeasonalStat: new TextStat("", ""), RepeatStat: new TextStat("", ""), AmPmStat: new TextStat("", ""), ResFactor: new TextStat("", ""), PlaySkipRatio: new List<ChartPoint>(), MileStoneStat: new TextStat("", ""), SkipVelStat: new TextStat("", ""), AnnivStat: new TextStat("", ""), TimeOfDayHeatmap: new List<ChartPoint>(), DayOfWeekHeatmap: new List<ChartPoint>(), MonthlyTrend: new List<TrendStat>(), WeeklyTrend: new List<TrendStat>(), Lifespan: new TextStat("", ""), CompRate: new TextStat("", ""), AvgDuration: new TextStat("", ""), Binge: new TextStat("", ""), Predict: new TextStat("", ""), Eddington: new TextStat("", ""), Streak: new TextStat("", ""), TimeToSkip: new TextStat("", ""), Radar: new List<ChartPoint>(), DropOff: new List<ChartPoint>(), Pairings: new List<SongPairing>());
        }
    }
}