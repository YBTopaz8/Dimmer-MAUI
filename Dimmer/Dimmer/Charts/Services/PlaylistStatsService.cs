

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Realms;

namespace Dimmer.Charts.Services;

public class PlaylistStatsService
{
    private readonly IRealmFactory _realmF;
    private readonly Realm _mainThreadRealm;
    private readonly BehaviorSubject<ObjectId?> _currentPlId = new(null);
    private readonly BehaviorSubject<bool> _isLoading = new(false);

    public void SetPlaylistId(ObjectId id) => _currentPlId.OnNext(id);
    public IObservable<bool> IsLoading => _isLoading.AsObservable();

    // 6 Common
    public IObservable<TextStat> TotalTime { get; }
    public IObservable<IReadOnlyList<ChartPoint>> PlaySkipRatio { get; }
    public IObservable<IReadOnlyList<ChartPoint>> TimeOfDayHeatmap { get; }
    public IObservable<IReadOnlyList<ChartPoint>> DayOfWeekHeatmap { get; }
    public IObservable<IReadOnlyList<TrendStat>> MonthlyTrend { get; }
    public IObservable<TextStat> Lifespan { get; }

    // 10 Distinct
    public IObservable<TextStat> DominantMood { get; }
    public IObservable<TextStat> PlaylistVelocity { get; }
    public IObservable<TextStat> WeakLink { get; }
    public IObservable<TextStat> AcousticElectricSplit { get; }
    public IObservable<TextStat> ArtistDiversity { get; }
    public IObservable<TextStat> AverageDuration { get; }
    public IObservable<TextStat> TqlAccuracy { get; }
    public IObservable<TextStat> LoyaltyScore { get; }
    public IObservable<IReadOnlyList<ChartPoint>> EraDistribution { get; }
    public IObservable<IReadOnlyList<InsightStat>> Insights { get; }

    public PlaylistStatsService(IRealmFactory realmF)
    {
        _realmF = realmF;
        _mainThreadRealm = _realmF.GetRealmInstance();

        var snapshotStream = _currentPlId.Where(id => id.HasValue).Select(id => id.Value)
           .Select(plId => _mainThreadRealm.All<DimmerPlayEvent>().AsObservableChangeSet().Throttle(TimeSpan.FromMilliseconds(250)).Select(_ => plId)
               .Select(id => Observable.FromAsync(() => Task.Run(() => {
                   using var bgRealm = _realmF.GetRealmInstance();
                   var pl = bgRealm.Find<PlaylistModel>(id);
                   var songs = pl?.SongsInPlaylist.ToList() ?? new List<SongModel>();
                   var sIds = songs.Select(s => s.Id).ToHashSet();
                   var evs = bgRealm.All<DimmerPlayEvent>().ToList().Where(e => e.SongId.HasValue && sIds.Contains(e.SongId.Value)).ToList();
                   return CalculateSnapshot(pl, songs, evs);
               }))).Switch()).Switch().Do( s=> _isLoading.OnNext(false)).Publish().RefCount();

        TotalTime = snapshotStream.Select(s => s.TotalTime).ObserveOn(RxSchedulers.UI);
        PlaySkipRatio = snapshotStream.Select(s => s.PlaySkipRatio).ObserveOn(RxSchedulers.UI);
        TimeOfDayHeatmap = snapshotStream.Select(s => s.TimeOfDayHeatmap).ObserveOn(RxSchedulers.UI);
        DayOfWeekHeatmap = snapshotStream.Select(s => s.DayOfWeekHeatmap).ObserveOn(RxSchedulers.UI);
        MonthlyTrend = snapshotStream.Select(s => s.MonthlyTrend).ObserveOn(RxSchedulers.UI);
        Lifespan = snapshotStream.Select(s => s.Lifespan).ObserveOn(RxSchedulers.UI);

        DominantMood = snapshotStream.Select(s => s.Mood).ObserveOn(RxSchedulers.UI);
        PlaylistVelocity = snapshotStream.Select(s => s.Velocity).ObserveOn(RxSchedulers.UI);
        WeakLink = snapshotStream.Select(s => s.WeakLink).ObserveOn(RxSchedulers.UI);
        AcousticElectricSplit = snapshotStream.Select(s => s.AcousticSplit).ObserveOn(RxSchedulers.UI);
        ArtistDiversity = snapshotStream.Select(s => s.Diversity).ObserveOn(RxSchedulers.UI);
        AverageDuration = snapshotStream.Select(s => s.AvgDur).ObserveOn(RxSchedulers.UI);
        TqlAccuracy = snapshotStream.Select(s => s.TqlAcc).ObserveOn(RxSchedulers.UI);
        LoyaltyScore = snapshotStream.Select(s => s.Loyalty).ObserveOn(RxSchedulers.UI);
        EraDistribution = snapshotStream.Select(s => s.EraCurve).ObserveOn(RxSchedulers.UI);
        Insights = snapshotStream.Select(s => s.Insights).ObserveOn(RxSchedulers.UI);
    }

    private PlaylistSnapshot CalculateSnapshot(PlaylistModel? pl, List<SongModel> songs, List<DimmerPlayEvent> events)
    {
        if (pl == null || songs.Count == 0) return PlaylistSnapshot.Empty();

        int plays = events.Count(e => e.PlayType == 0);
        int comps = events.Count(e => e.PlayType == 3);

        // 1 & 8. Mood & Loyalty
        var tags = songs.SelectMany(s => s.UserNotes).GroupBy(t => t.UserMessageText).OrderByDescending(g => g.Count()).FirstOrDefault();
        var mood = new TextStat("Dominant Mood", tags != null ? tags.Key : "Mixed", "Based on tags");
        var loyalty = new TextStat("Completion Rate", plays > 0 ? $"{((double)comps / plays) * 100:F1}%" : "0%", "Songs not skipped");

        // 2. Velocity
        DateTimeOffset created = pl.DateCreated;
        var weeks = Math.Max(1, (DateTimeOffset.UtcNow - created).TotalDays / 7);
        var velocity = new TextStat("Consumption Velocity", $"{plays / weeks:F1}/wk", "Average plays per week");

        // 3. Weak Link
        var weak = songs.OrderByDescending(s => events.Count(e => e.SongId == s.Id && e.PlayType == 5)).FirstOrDefault();
        var weakLink = new TextStat("Weak Link", weak != null ? weak.Title : "N/A", "Most skipped song here");

        // 4. Acoustic
        int ac = songs.Count(s => s.UserNotes.Any(t => t.UserMessageText == "Acoustic"));
        var acoustic = new TextStat("Acoustic Split", $"{ac} vs {songs.Count - ac}", "Acoustic vs Others");

        // 5. Diversity
        int unqArt = songs.Select(s => s.ArtistName).Distinct().Count();
        var diversity = new TextStat("Artist Diversity", $"{((double)unqArt / songs.Count) * 100:F0}%", $"{unqArt} unique artists");

        // 6. Avg Duration
        var avgDur = new TextStat("Avg Track Length", TimeSpan.FromSeconds(songs.Average(s => s.DurationInSeconds)).ToString(@"mm\:ss"));

        // 7. TQL (If Smart)
        var tql = new TextStat("TQL Status", pl.IsSmartPlaylist ? "Dynamic" : "Static List", pl.IsSmartPlaylist ? pl.QueryText : "");

        // Charts
        var era = songs.Where(s => s.ReleaseYear != null).GroupBy(s => (s.ReleaseYear! / 10) * 10).Select(g => new ChartPoint($"{g.Key}s", g.Count())).OrderBy(c => c.Label).ToList();

        // 1. Staleness Index (Plays of older songs vs newly added songs)
        var thirtyDaysAgo = DateTimeOffset.UtcNow.AddDays(-30);
        var oldSongs = songs.Where(s => s.DateCreated < thirtyDaysAgo).Select(s => s.Id).ToHashSet();
        int oldPlays = events.Count(e => e.SongId.HasValue && oldSongs.Contains(e.SongId.Value));
        var staleStat = new TextStat("Staleness Index", events.Count > 0 ? $"{((double)oldPlays / events.Count) * 100:F0}%" : "0%", "Plays belonging to older tracks");

        // 2. The Core Anchor (Longest tenured, highly played song)
        var anchor = songs.OrderBy(s => s.DateCreated).ThenByDescending(s => events.Count(e => e.SongId == s.Id)).FirstOrDefault();
        var anchorStat = new TextStat("The Anchor", anchor?.Title ?? "N/A", "Oldest highly played track");

        // 3. Flow Disruption (Song causing the most playlist exits/skips)
        var disruptor = songs.OrderByDescending(s => events.Count(e => e.SongId == s.Id && e.PlayType == 5)).FirstOrDefault();
        var disruptorStat = new TextStat("Flow Disruptor", disruptor?.Title ?? "N/A", "Highest skip count");

        // 4. BPM Range / Volatility
        var bpms = songs.Where(s => s.BPM > 0).Select(s => s.BPM!.Value).ToList();
        var bpmStat = new TextStat("BPM Range", bpms.Any() ? $"{bpms.Min()} - {bpms.Max()}" : "N/A", "Tempo volatility");

        // 5. Instrumental vs Vocal
        int inst = songs.Count(s => s.IsInstrumental == true || !s.HasLyrics);
        var instStat = new TextStat("Instrumental Ratio", songs.Count > 0 ? $"{((double)inst / songs.Count) * 100:F0}%" : "0%", "Non-vocal tracks");

        // 6. Top Decade represented
        var topDecade = songs.Where(s => s.ReleaseYear > 0).GroupBy(s => (s.ReleaseYear / 10) * 10).OrderByDescending(g => g.Count()).FirstOrDefault();
        var decadeStat = new TextStat("Defining Era", topDecade != null ? $"{topDecade.Key}s" : "N/A", "Most common release decade");

        // 7. Playlist Format (EP, LP, or Mixtape size)
        string sizeLabel = songs.Count < 7 ? "EP Sized" : songs.Count < 20 ? "LP Sized" : "Mixtape Sized";
        var sizeStat = new TextStat("Playlist Scale", sizeLabel, $"{songs.Count} tracks");

        // 8. Size-to-Time Ratio (Are these short punk tracks or long epics?)
        var ratioStat = new TextStat("Avg Track Size", songs.Count > 0 ? TimeSpan.FromSeconds(songs.Sum(s => s.DurationInSeconds) / songs.Count).ToString(@"mm\:ss") : "00:00", "Duration per track");

        return new PlaylistSnapshot(staleStat,anchorStat,disruptorStat,ratioStat,sizeStat,decadeStat,
            CommonStatsHelper.GetTotalPlayTime(events),bpmStat,instStat, CommonStatsHelper.GetPlaySkipRatio(events), CommonStatsHelper.GetTimeOfDayHeatmap(events), CommonStatsHelper.GetDayOfWeekHeatmap(events), CommonStatsHelper.GetRollingMonthlyTrend(events), CommonStatsHelper.GetDiscoveryLifespan(events),
            mood, velocity, weakLink, acoustic, diversity, avgDur, tql, loyalty, era, new List<InsightStat>());
    }

    private record PlaylistSnapshot(TextStat StaleStat, TextStat AnchorStat,TextStat DisruptorStat, TextStat RadioStat,TextStat SizeStat,TextStat DecadeStat,TextStat TotalTime,TextStat BpmStat,TextStat InstStat, IReadOnlyList<ChartPoint> PlaySkipRatio, IReadOnlyList<ChartPoint> TimeOfDayHeatmap, IReadOnlyList<ChartPoint> DayOfWeekHeatmap, IReadOnlyList<TrendStat> MonthlyTrend, TextStat Lifespan, TextStat Mood, TextStat Velocity, TextStat WeakLink, TextStat AcousticSplit, TextStat Diversity, TextStat AvgDur, TextStat TqlAcc, TextStat Loyalty, IReadOnlyList<ChartPoint> EraCurve, IReadOnlyList<InsightStat> Insights)
    { 
        public static PlaylistSnapshot Empty() => new PlaylistSnapshot(StaleStat: new("", ""), AnchorStat: new("", ""), DisruptorStat: new("", ""), RadioStat: new("", ""), SizeStat: new("", ""), DecadeStat: new("", ""), TotalTime: new("", ""), BpmStat: new("", ""), InstStat: new("", ""), PlaySkipRatio: new List<ChartPoint>(), TimeOfDayHeatmap: new List<ChartPoint>(), DayOfWeekHeatmap: new List<ChartPoint>(), MonthlyTrend: new List<TrendStat>(), Lifespan: new("", ""), Mood: new("", ""), Velocity: new("", ""), WeakLink: new("", ""), AcousticSplit: new("", ""), Diversity: new("", ""), AvgDur: new("", ""), TqlAcc: new("", ""), Loyalty: new("", ""), EraCurve: new List<ChartPoint>(), Insights: new List<InsightStat>()); }
}