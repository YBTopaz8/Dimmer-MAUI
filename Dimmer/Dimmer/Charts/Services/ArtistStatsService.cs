
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Realms;
namespace Dimmer.Charts.Services;


public class ArtistStatsService
{
    private readonly IRealmFactory _realmF;
    private readonly Realm _mainThreadRealm;
    private readonly BehaviorSubject<ObjectId?> _currentArtistId = new(null);
    private readonly BehaviorSubject<bool> _isLoading = new(false);

    public void SetArtistId(ObjectId id) => _currentArtistId.OnNext(id);
    public IObservable<bool> IsLoading => _isLoading.AsObservable();

    // 6 Common
    public IObservable<TextStat> TotalTime { get; }
    public IObservable<IReadOnlyList<ChartPoint>> PlaySkipRatio { get; }
    public IObservable<IReadOnlyList<ChartPoint>> TimeOfDayHeatmap { get; }
    public IObservable<IReadOnlyList<ChartPoint>> DayOfWeekHeatmap { get; }
    public IObservable<IReadOnlyList<TrendStat>> MonthlyTrend { get; }
    public IObservable<TextStat> Lifespan { get; }

    // 10 Distinct
    public IObservable<TextStat> LoyaltyIndex { get; }
    public IObservable<TextStat> CatalogCompletion { get; }
    public IObservable<TextStat> OneHitWonderPct { get; }
    public IObservable<TextStat> BingeRecord { get; }
    public IObservable<TextStat> YoYGrowth { get; }
    public IObservable<TextStat> ParetoPrinciple { get; }
    public IObservable<IReadOnlyList<LeaderboardItem>> TopSongs { get; }
    public IObservable<IReadOnlyList<LeaderboardItem>> TopAlbums { get; }
    public IObservable<IReadOnlyList<LeaderboardItem>> DeepCuts { get; }
    public IObservable<IReadOnlyList<ChartPoint>> EraPreference { get; }

    public ArtistStatsService(IRealmFactory realmF)
    {
        _realmF = realmF;
        _mainThreadRealm = _realmF.GetRealmInstance();

        var snapshotStream = _currentArtistId.Where(id => id.HasValue).Select(id => id.Value)
           .Select(artId => _mainThreadRealm.All<DimmerPlayEvent>().AsObservableChangeSet().Throttle(TimeSpan.FromMilliseconds(250)).Select(_ => artId)
               .Select(id => Observable.FromAsync(() =>
               {
                   _isLoading.OnNext(true);
                   return Task.Run(() =>
                   {
                       using var bgRealm = _realmF.GetRealmInstance();
                       var bgArtist = bgRealm.Find<ArtistModel>(id);
                       var artistSongs = bgArtist?.Songs.ToList() ?? new List<SongModel>();
                       var songIds = artistSongs.Select(s => s.Id).ToHashSet();
                       var artEvents = bgRealm.All<DimmerPlayEvent>().ToList().Where(e => e.SongId.HasValue && songIds.Contains(e.SongId.Value)).OrderBy(e => e.DatePlayed).ToList();
                       int totPlays = bgRealm.All<DimmerPlayEvent>().Count();
                       return CalculateSnapshot(bgArtist, artistSongs, artEvents, totPlays);
                   });
               })).Switch()).Switch().Do(_ => _isLoading.OnNext(false)).Publish().RefCount();

        TotalTime = snapshotStream.Select(s => s.TotalTime).ObserveOn(RxSchedulers.UI);
        PlaySkipRatio = snapshotStream.Select(s => s.PlaySkipRatio).ObserveOn(RxSchedulers.UI);
        TimeOfDayHeatmap = snapshotStream.Select(s => s.TimeOfDayHeatmap).ObserveOn(RxSchedulers.UI);
        DayOfWeekHeatmap = snapshotStream.Select(s => s.DayOfWeekHeatmap).ObserveOn(RxSchedulers.UI);
        MonthlyTrend = snapshotStream.Select(s => s.MonthlyTrend).ObserveOn(RxSchedulers.UI);
        Lifespan = snapshotStream.Select(s => s.Lifespan).ObserveOn(RxSchedulers.UI);

        LoyaltyIndex = snapshotStream.Select(s => s.Loyalty).ObserveOn(RxSchedulers.UI);
        CatalogCompletion = snapshotStream.Select(s => s.Catalog).ObserveOn(RxSchedulers.UI);
        OneHitWonderPct = snapshotStream.Select(s => s.OneHit).ObserveOn(RxSchedulers.UI);
        BingeRecord = snapshotStream.Select(s => s.Binge).ObserveOn(RxSchedulers.UI);
        YoYGrowth = snapshotStream.Select(s => s.YoY).ObserveOn(RxSchedulers.UI);
        ParetoPrinciple = snapshotStream.Select(s => s.Pareto).ObserveOn(RxSchedulers.UI);
        TopSongs = snapshotStream.Select(s => s.TopSongs).ObserveOn(RxSchedulers.UI);
        TopAlbums = snapshotStream.Select(s => s.TopAlbums).ObserveOn(RxSchedulers.UI);
        DeepCuts = snapshotStream.Select(s => s.DeepCuts).ObserveOn(RxSchedulers.UI);
        EraPreference = snapshotStream.Select(s => s.Era).ObserveOn(RxSchedulers.UI);
    }

    private ArtistSnapshot CalculateSnapshot(ArtistModel? artist, List<SongModel> songs, List<DimmerPlayEvent> events, int totLibraryPlays)
    {
        if (artist == null || events.Count == 0) return ArtistSnapshot.Empty();

        int plays = events.Count(e => e.PlayType == 3);

        // 1. Loyalty
        var loyalty = new TextStat("Loyalty Index", totLibraryPlays > 0 ? $"{((double)plays / totLibraryPlays) * 100:F1}%" : "0%", "Share of total library plays");

        // 2. Catalog
        int uniquePlayed = events.Select(e => e.SongId).Distinct().Count();
        var catalog = new TextStat("Catalog Exploration", songs.Count > 0 ? $"{((double)uniquePlayed / songs.Count) * 100:F0}%" : "0%", $"{uniquePlayed}/{songs.Count} songs played");

        // 3. One Hit Wonder
        var topSongPlays = events.GroupBy(e => e.SongId).Select(g => g.Count()).OrderByDescending(c => c).FirstOrDefault();
        var oneHit = new TextStat("Top Song Weight", plays > 0 ? $"{((double)topSongPlays / plays) * 100:F1}%" : "0%", "Plays from their #1 song");

        // 4. Binge
        var bingeDay = events.Where(e => e.PlayType == 3).GroupBy(e => e.DatePlayed.Date).OrderByDescending(g => g.Count()).FirstOrDefault();
        var binge = new TextStat("Binge Record", bingeDay != null ? $"{bingeDay.Count()} plays" : "0", bingeDay != null ? bingeDay.Key.ToString("MMM d, yyyy") : "");

        // 5. YoY
        var tStart = new DateTimeOffset(DateTime.UtcNow.Year, 1, 1, 0, 0, 0, TimeSpan.Zero);
        int pThisYear = events.Count(e => e.DatePlayed >= tStart);
        int pLastYear = events.Count(e => e.DatePlayed >= tStart.AddYears(-1) && e.DatePlayed < tStart);
        var yoy = new TextStat("YoY Growth", $"{pThisYear} vs {pLastYear}", "Plays this year vs last");

        // 6. Pareto
        var songPlays = events.GroupBy(e => e.SongId).Select(g => g.Count()).OrderByDescending(c => c).ToList();
        double paretoPct = songPlays.Count != 0 ? (double)songPlays.Take(Math.Max(1, (int)(songPlays.Count * 0.2))).Sum() / songPlays.Sum() * 100 : 0;
        var pareto = new TextStat("Pareto (80/20)", $"{paretoPct:F1}%", "Plays from top 20% of their songs");

        // Leaderboards
        List<LeaderboardItem>? topSongsList = songs.Select(s => new { S = s, P = events.Count(e => e.SongId == s.Id && e.PlayType == 3) }).OrderByDescending(x => x.P).Take(10).Select((x, i) => new LeaderboardItem($"#{i + 1}", x.S.Title, $"{x.P} plays", x.S.CoverImagePath)).ToList();
        var deepCutsList = songs.Select(s => new { S = s, P = events.Count(e => e.SongId == s.Id && e.PlayType == 3) }).Where(x => x.P > 0).OrderBy(x => x.P).Take(10).Select((x, i) => new LeaderboardItem($"#{i + 1}", x.S.Title, $"{x.P} plays", x.S.CoverImagePath)).ToList();
        var topAlbumsList = events.Where(e => e.PlayType == 3).GroupBy(e => songs.FirstOrDefault(s => s.Id == e.SongId)?.Album).Where(g => g.Key != null).OrderByDescending(g => g.Count()).Take(10).Select((g, i) => new LeaderboardItem($"#{i + 1}", g.Key!.Name, $"{g.Count()} plays", g.Key.ImagePath)).ToList();

        // 10. Era Preference
        var era = events.Select(e => songs.FirstOrDefault(s => s.Id == e.SongId)?.ReleaseYear)
            .Where(y => y.HasValue).GroupBy(y => (y!.Value / 10) * 10)
            .Select(g => new ChartPoint($"{g.Key}s", g.Count())).OrderBy(c => c.Label).ToList();


        // 1. Catalog Focus (Gini-like: Are plays spread out or focused on 1-2 tracks?)
        int tracksMaking50Percent = songPlays.TakeWhile(c => { plays -= c; return plays > (events.Count / 2); }).Count() + 1;
        var focusStat = new TextStat("Catalog Focus", $"{tracksMaking50Percent} tracks", "Make up 50% of plays");

        // 2. Favorite Era (Which specific decade/5-year period of this artist do you prefer?)
        var favEra = events.Select(e => songs.FirstOrDefault(s => s.Id == e.SongId)?.ReleaseYear)
            .Where(y => y.HasValue).GroupBy(y => (y!.Value / 5) * 5).OrderByDescending(g => g.Count()).FirstOrDefault();
        var favEraStat = new TextStat("Favorite Era", favEra != null ? $"Late {favEra.Key}s" : "N/A", "Most played time period");

        // 3. Artist Velocity (How fast from 1st play to 50th play?)
        var first50 = events.OrderBy(e => e.DatePlayed).Take(50).ToList();
        var veloStat = new TextStat("Fandom Velocity", first50.Count == 50 ? $"{(first50.Last().DatePlayed - first50.First().DatePlayed).TotalDays:F0} days" : "N/A", "Time to reach 50 plays");

        var mTrend = CommonStatsHelper.GetRollingMonthlyTrend(events);
        // 4. Comeback Kid (Biggest jump in plays between consecutive months)
        int maxJump = 0; string jumpMonth = "";
        for (int i = 1; i < mTrend.Count; i++)
        { // From your existing mTrend list
            int jump = mTrend[i].PlayCount - mTrend[i - 1].PlayCount;
            if (jump > maxJump) { maxJump = jump; jumpMonth = mTrend[i].Period; }
        }
        var comebackStat = new TextStat("Comeback Month", maxJump > 0 ? jumpMonth : "N/A", $"Jumped by {maxJump} plays");

        // 5. Gateway Album (The first album you ever played from them)
        var gatewayEvent = events.OrderBy(e => e.DatePlayed).FirstOrDefault();
        var gatewayAlbum = songs.FirstOrDefault(s => s.Id == gatewayEvent?.SongId)?.AlbumName;
        var gatewayStat = new TextStat("Gateway Album", gatewayAlbum ?? "Unknown", "Your introduction to them");

        // 6. Anchor Album (The album with the most raw listening time)
        var anchor = events.GroupBy(e => songs.FirstOrDefault(s => s.Id == e.SongId)?.AlbumName)
            .OrderByDescending(g => g.Sum(e => e.PositionInSeconds)).FirstOrDefault();
        var anchorStat = new TextStat("Anchor Album", anchor?.Key ?? "Unknown", "Most total time spent");

        // 7. Weekend Bias (Do they fit your party vibe or chill vibe?)
        int weekendP = events.Count(e => e.DatePlayed.DayOfWeek == DayOfWeek.Saturday || e.DatePlayed.DayOfWeek == DayOfWeek.Sunday);
        var weekendStat = new TextStat("Weekend Bias", $"{((double)weekendP / events.Count) * 100:F0}%", "Plays on Sat/Sun");

        // 8. Sleeping Beauty (Were they dormant for >6 months and revived?)
        var sleepingStat = new TextStat("Status", (events.Last().DatePlayed - events.First().DatePlayed).TotalDays > 180 && events.Count(e => e.DatePlayed > DateTimeOffset.UtcNow.AddDays(-30)) > 5 ? "Revived!" : "Active", "Recent fandom activity");

        return new ArtistSnapshot(focusStat,favEraStat,veloStat,comebackStat,gatewayStat,anchorStat,weekendStat,sleepingStat,
            CommonStatsHelper.GetTotalPlayTime(events), CommonStatsHelper.GetPlaySkipRatio(events), CommonStatsHelper.GetTimeOfDayHeatmap(events), CommonStatsHelper.GetDayOfWeekHeatmap(events), CommonStatsHelper.GetRollingMonthlyTrend(events), CommonStatsHelper.GetDiscoveryLifespan(events),
            loyalty, catalog, oneHit, binge, yoy, pareto, topSongsList, topAlbumsList, deepCutsList, era);
    }

    private record ArtistSnapshot(TextStat FocusStat, TextStat FavEraStat, TextStat VeloStat, TextStat ComebackStat, TextStat GatewayStat, TextStat AnchorStat, TextStat WeekendStat, TextStat SleepingStat, TextStat TotalTime, IReadOnlyList<ChartPoint> PlaySkipRatio, IReadOnlyList<ChartPoint> TimeOfDayHeatmap, IReadOnlyList<ChartPoint> DayOfWeekHeatmap, IReadOnlyList<TrendStat> MonthlyTrend, TextStat Lifespan, TextStat Loyalty, TextStat Catalog, TextStat OneHit, TextStat Binge, TextStat YoY, TextStat Pareto, IReadOnlyList<LeaderboardItem> TopSongs, IReadOnlyList<LeaderboardItem> TopAlbums, IReadOnlyList<LeaderboardItem> DeepCuts, IReadOnlyList<ChartPoint> Era)
    {
        public static ArtistSnapshot Empty()
        {
            return new ArtistSnapshot(FocusStat: new("", ""),FavEraStat: new("", ""),VeloStat: new("", ""),ComebackStat: new("", ""),GatewayStat: new("", ""),AnchorStat: new("", ""),WeekendStat: new("", ""),SleepingStat: new("", ""), TotalTime: new("", ""), PlaySkipRatio: new List<ChartPoint>(), TimeOfDayHeatmap: new List<ChartPoint>(), DayOfWeekHeatmap: new List<ChartPoint>(), MonthlyTrend: new List<TrendStat>(), Lifespan: new("", ""), Loyalty: new("", ""), Catalog: new("", ""), OneHit: new("", ""), Binge: new("", ""), YoY: new("", ""), Pareto: new("", ""), TopSongs: new List<LeaderboardItem>(), TopAlbums: new List<LeaderboardItem>(), DeepCuts: new List<LeaderboardItem>(), Era: new List<ChartPoint>());
        }
    }
}