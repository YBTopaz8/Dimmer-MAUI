using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Realms;
namespace Dimmer.Charts.Services;




public class AlbumStatsService
{
    private readonly IRealmFactory _realmF;
    private readonly Realm _mainThreadRealm;
    private readonly BehaviorSubject<ObjectId?> _currentAlbumId = new(null);
    private readonly BehaviorSubject<bool> _isLoading = new(false);

    public void SetAlbumId(ObjectId id) => _currentAlbumId.OnNext(id);
    public IObservable<bool> IsLoading => _isLoading.AsObservable();

    // 6 Common
    public IObservable<TextStat> TotalTime { get; }
    public IObservable<IReadOnlyList<ChartPoint>> PlaySkipRatio { get; }
    public IObservable<IReadOnlyList<ChartPoint>> TimeOfDayHeatmap { get; }
    public IObservable<IReadOnlyList<ChartPoint>> DayOfWeekHeatmap { get; }
    public IObservable<IReadOnlyList<TrendStat>> MonthlyTrend { get; }
    public IObservable<TextStat> Lifespan { get; }

    // 10 Distinct
    public IObservable<TextStat> GoldenRatioTrack { get; }
    public IObservable<TextStat> AlbumCompletionRate { get; }
    public IObservable<TextStat> FrontToBackIndex { get; }
    public IObservable<TextStat> SinglesVsFiller { get; }
    public IObservable<TextStat> ListenThroughRate { get; }
    public IObservable<TextStat> SkippedTrackId { get; }
    public IObservable<TextStat> PeakDiscoveryDay { get; }
    public IObservable<IReadOnlyList<ChartPoint>> PlaysPerTrack { get; }
    public IObservable<IReadOnlyList<ChartPoint>> BpmFlow { get; }
    public IObservable<IReadOnlyList<InsightStat>> Insights { get; }

    public AlbumStatsService(IRealmFactory realmF)
    {
        _realmF = realmF;
        _mainThreadRealm = _realmF.GetRealmInstance();

        var snapshotStream = _currentAlbumId.Where(id => id.HasValue).Select(id => id.Value)
           .Select(albId => _mainThreadRealm.All<DimmerPlayEvent>().AsObservableChangeSet().Throttle(TimeSpan.FromMilliseconds(250)).Select(_ => albId)
               .Select(id => Observable.FromAsync(() => Task.Run(() => {
                   using var bgRealm = _realmF.GetRealmInstance();
                   var album = bgRealm.Find<AlbumModel>(id);
                   var songs = album?.SongsInAlbum?.ToList() ?? new List<SongModel>();
                   var songIds = songs.Select(s => s.Id).ToHashSet();
                   var evs = bgRealm.All<DimmerPlayEvent>().ToList().Where(e => e.SongId.HasValue && songIds.Contains(e.SongId.Value)).ToList();
                   return CalculateSnapshot(album, songs, evs);
               }))).Switch()).Switch().Do(s => _isLoading.OnNext(false)).Publish().RefCount();

        TotalTime = snapshotStream.Select(s => s.TotalTime).ObserveOn(RxSchedulers.UI);
        PlaySkipRatio = snapshotStream.Select(s => s.PlaySkipRatio).ObserveOn(RxSchedulers.UI);
        TimeOfDayHeatmap = snapshotStream.Select(s => s.TimeOfDayHeatmap).ObserveOn(RxSchedulers.UI);
        DayOfWeekHeatmap = snapshotStream.Select(s => s.DayOfWeekHeatmap).ObserveOn(RxSchedulers.UI);
        MonthlyTrend = snapshotStream.Select(s => s.MonthlyTrend).ObserveOn(RxSchedulers.UI);
        Lifespan = snapshotStream.Select(s => s.Lifespan).ObserveOn(RxSchedulers.UI);

        GoldenRatioTrack = snapshotStream.Select(s => s.Golden).ObserveOn(RxSchedulers.UI);
        AlbumCompletionRate = snapshotStream.Select(s => s.CompRate).ObserveOn(RxSchedulers.UI);
        FrontToBackIndex = snapshotStream.Select(s => s.FrontToBack).ObserveOn(RxSchedulers.UI);
        SinglesVsFiller = snapshotStream.Select(s => s.SinglesFiller).ObserveOn(RxSchedulers.UI);
        ListenThroughRate = snapshotStream.Select(s => s.LTR).ObserveOn(RxSchedulers.UI);
        SkippedTrackId = snapshotStream.Select(s => s.Skipped).ObserveOn(RxSchedulers.UI);
        PeakDiscoveryDay = snapshotStream.Select(s => s.PeakDay).ObserveOn(RxSchedulers.UI);
        PlaysPerTrack = snapshotStream.Select(s => s.PlaysCurve).ObserveOn(RxSchedulers.UI);
        BpmFlow = snapshotStream.Select(s => s.BpmFlow).ObserveOn(RxSchedulers.UI);
        Insights = snapshotStream.Select(s => s.Insights).ObserveOn(RxSchedulers.UI);
    }

    private AlbumSnapshot CalculateSnapshot(AlbumModel? album, List<SongModel> songs, List<DimmerPlayEvent> events)
    {
        if (album == null || songs.Count == 0) return AlbumSnapshot.Empty();

        int plays = events.Count(e => e.PlayType == 0);
        int comps = events.Count(e => e.PlayType == 3);

        // 1. Golden Ratio
        double totalDur = songs.Sum(s => s.DurationInSeconds);
        double grTime = totalDur / 1.61803398875;
        double cum = 0; string grTrack = "Unknown";
        foreach (var s in songs.OrderBy(x => x.TrackNumber)) { cum += s.DurationInSeconds; if (cum >= grTime) { grTrack = s.Title; break; } }
        var golden = new TextStat("Golden Ratio Track", grTrack, "The aesthetic center");

        // 2. Comp Rate
        var compRate = new TextStat("Album Completion", plays > 0 ? $"{((double)comps / plays) * 100:F0}%" : "0%");

        // 3. Front to back (Days where >80% of album was played)
        int f2b = events.GroupBy(e => e.DatePlayed.Date).Count(g => g.Select(e => e.SongId).Distinct().Count() >= songs.Count * 0.8);
        var front2Back = new TextStat("Front-To-Back Listens", f2b.ToString(), "Days played sequentially");

        // 4. Singles vs Filler
        var tp = songs.Select(s => new { Plays = events.Count(e => e.SongId == s.Id && e.PlayType == 0), Label=s.Title }).OrderByDescending(c => c.Plays).ToList();
        int top2 = tp.Take(2).Sum(x => x.Plays); int rest = tp.Skip(2).Sum(x => x.Plays);
        var singlesFill = new TextStat("Singles vs Rest", $"{top2} vs {rest}", "Top 2 tracks vs remainder");

        // 5. Listen Through Rate (Avg of tracks)
        double avgLtr = songs.Count != 0 ? songs.Average(s => s.ListenThroughRate) : 0;
        var ltr = new TextStat("Avg Listen-Through", $"{avgLtr:F1}%", "Track retention");

        // 6. Most Skipped
        var skipTrack = songs.OrderByDescending(s => events.Count(e => e.SongId == s.Id && e.PlayType == 5)).FirstOrDefault();
        var skipped = new TextStat("Most Skipped", skipTrack != null ? skipTrack.Title : "N/A", "Highest skip rate");

        // 7. Peak Day
        var peak = events.GroupBy(e => e.DatePlayed.Date).OrderByDescending(g => g.Count()).FirstOrDefault();
        var peakDay = new TextStat("Peak Discovery", peak != null ? peak.Key.ToString("MMM d, yyyy") : "N/A", peak != null ? $"{peak.Count()} plays" : "");

        // Charts
        var curve = songs.OrderBy(s => s.TrackNumber).Select(s => new ChartPoint($"Trk {s.TrackNumber ?? 0}", events.Count(e => e.SongId == s.Id && e.PlayType == 0))).ToList();
        var bpmFlow = songs.OrderBy(s => s.TrackNumber).Select(s => new ChartPoint(s.Title, s.BPM ?? 0)).ToList();

        // Insights
        var insights = new List<InsightStat>();
        if (album.TrackTotal > 0 && songs.Count != album.TrackTotal) insights.Add(new InsightStat("Incomplete", $"You have {songs.Count} tracks, but the official album has {album.TrackTotal}.", "⚠️"));


        // 1. Side A vs Side B (First half of tracklist vs Second half)
        int midPoint = (int)Math.Ceiling(songs.Count / 2.0);
        var sideAIds = songs.Where(s => s.TrackNumber <= midPoint).Select(s => s.Id).ToHashSet();
        int sideAPlays = events.Count(e => e.SongId.HasValue && sideAIds.Contains(e.SongId.Value));
        int sideBPlays = plays - sideAPlays; // 'plays' is your existing total play count
        var sideStat = new TextStat("Side A vs Side B", $"{sideAPlays} / {sideBPlays}", "Play distribution");

        // 2. The Closer (How often the final track is played compared to the first)
        var firstTrack = songs.OrderBy(s => s.TrackNumber).FirstOrDefault();
        var lastTrack = songs.OrderByDescending(s => s.TrackNumber).FirstOrDefault();
        int t1Plays = events.Count(e => e.SongId == firstTrack?.Id);
        int tNPlays = events.Count(e => e.SongId == lastTrack?.Id);
        var closerStat = new TextStat("The Closer Metric", t1Plays > 0 ? $"{((double)tNPlays / t1Plays) * 100:F0}%" : "0%", "Track N vs Track 1 plays");

        // 3. Patience Index (Average seconds into the album before a skip happens)
        var albumSkips = events.Where(e => e.PlayType == 5).ToList();
        var patienceStat = new TextStat("Album Patience", albumSkips.Count != 0 ? TimeSpan.FromSeconds(albumSkips.Average(e => e.PositionInSeconds)).ToString(@"mm\:ss") : "Infinite", "Avg time before a skip");

        // 4. Album Velocity (Time from discovering the album to finishing all tracks)
        var playedIds = events.Where(e => e.PlayType == 3).Select(e => e.SongId).Distinct().ToList();
        var veloStat = new TextStat("Consumption Velocity", playedIds.Count == songs.Count && songs.Count > 0 ? $"{(events.Last(e => playedIds.Contains(e.SongId)).DatePlayed - events.First().DatePlayed).TotalDays:F1} days" : "Incomplete", "To hear every track");

        // 5. The Interlude Filter (Are short songs skipped?)
        var shortSongs = songs.Where(s => s.DurationInSeconds < 120).Select(s => s.Id).ToHashSet();
        int shortSkips = events.Count(e => e.SongId.HasValue && shortSongs.Contains(e.SongId.Value) && e.PlayType == 5);
        var interludeStat = new TextStat("Interlude Survival", shortSongs.Count != 0 ? $"{shortSkips} skips" : "No short tracks", "Skips on tracks < 2m");

        // 6. Day of Rest (Most common day of week for full listens)
        var restDay = events.GroupBy(e => e.DatePlayed.DayOfWeek).OrderByDescending(g => g.Count()).FirstOrDefault();
        var restStat = new TextStat("Album Vibe", restDay != null ? restDay.Key.ToString() : "N/A", "Most popular day");

        // 7. Core Trio (The 3 tracks played together the most)
        // (Simplified: just the 3 most played tracks, as calculating sequences is expensive)
        var coreTrio = tp.Take(3).Select(c => c.Label).ToList(); // 'tp' is your existing ChartPoint list
        var trioStat = new TextStat("The Core Trio", string.Join(", ", coreTrio), "The pillars of this album");

        // 8. Skipped Intro (Is track 1 skipped often?)
        int t1Skips = events.Count(e => e.SongId == firstTrack?.Id && e.PlayType == 5);
        var introStat = new TextStat("Track 1 Skips", t1Plays > 0 ? $"{((double)t1Skips / t1Plays) * 100:F0}%" : "0%", "Skip rate of the opener");


        return new AlbumSnapshot(
            SideStat: sideStat,
            CloserStat: closerStat,
            PatienceStat: patienceStat,
            VeloStat: veloStat,
            InterludeStat: interludeStat,
            RestStat: restStat,
            TrioStat: trioStat,
            IntroStat: introStat,
            TotalTime: CommonStatsHelper.GetTotalPlayTime(events),
            PlaySkipRatio: CommonStatsHelper.GetPlaySkipRatio(events),
            TimeOfDayHeatmap: CommonStatsHelper.GetTimeOfDayHeatmap(events),
            DayOfWeekHeatmap: CommonStatsHelper.GetDayOfWeekHeatmap(events),
            MonthlyTrend: CommonStatsHelper.GetRollingMonthlyTrend(events),
            Lifespan: CommonStatsHelper.GetDiscoveryLifespan(events),
            Golden: golden,
            CompRate: compRate,
            FrontToBack: front2Back,
            SinglesFiller: singlesFill,
            LTR: ltr,
            Skipped: skipped,
            PeakDay: peakDay,
            PlaysCurve: curve,
            BpmFlow: bpmFlow,
            Insights: insights
        );
    }

    private record AlbumSnapshot(TextStat SideStat, TextStat CloserStat, TextStat PatienceStat, TextStat VeloStat, TextStat InterludeStat, TextStat RestStat, TextStat TrioStat, TextStat IntroStat , TextStat TotalTime, IReadOnlyList<ChartPoint> PlaySkipRatio, IReadOnlyList<ChartPoint> TimeOfDayHeatmap, IReadOnlyList<ChartPoint> DayOfWeekHeatmap, IReadOnlyList<TrendStat> MonthlyTrend, TextStat Lifespan, TextStat Golden, TextStat CompRate, TextStat FrontToBack, TextStat SinglesFiller, TextStat LTR, TextStat Skipped, TextStat PeakDay, IReadOnlyList<ChartPoint> PlaysCurve, IReadOnlyList<ChartPoint> BpmFlow, IReadOnlyList<InsightStat> Insights)
    { public static AlbumSnapshot Empty() => new AlbumSnapshot(SideStat: new("", ""), CloserStat: new("", ""), PatienceStat: new("", ""), VeloStat: new("", ""), InterludeStat: new("", ""), RestStat: new("", ""), TrioStat: new("", ""), IntroStat: new("", ""), TotalTime: new("", ""), PlaySkipRatio: new List<ChartPoint>(), TimeOfDayHeatmap: new List<ChartPoint>(), DayOfWeekHeatmap: new List<ChartPoint>(), MonthlyTrend: new List<TrendStat>(), Lifespan: new("", ""), Golden: new("", ""), CompRate: new("", ""), FrontToBack: new("", ""), SinglesFiller: new("", ""), LTR: new("", ""), Skipped: new("", ""), PeakDay: new("", ""), PlaysCurve: new List<ChartPoint>(), BpmFlow: new List<ChartPoint>(), Insights: new List<InsightStat>()); }
}