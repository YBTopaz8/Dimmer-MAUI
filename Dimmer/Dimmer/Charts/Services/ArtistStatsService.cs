
using System;
using System.Collections.Generic;
using System.Text;

namespace Dimmer.Charts.Services;




public class ArtistStatsService
{
    private readonly IRealmFactory _realmF;
    private readonly Realm _mainThreadRealm;
    private readonly BehaviorSubject<ObjectId?> _currentArtistId = new(null);
    public void SetArtistId(ObjectId id) => _currentArtistId.OnNext(id);

    // --- OUTPUTS ---
    public IObservable<TextStat> TextLoyaltyIndex { get; }
    public IObservable<TextStat> TextBingeScore { get; }
    public IObservable<TextStat> TextDiscoveryComparison { get; }

    public IObservable<IReadOnlyList<TrendStat>> ListMonthlyTrend { get; }
    public IObservable<IReadOnlyList<LeaderboardItem>> ListDeepCuts { get; }
    public IObservable<IReadOnlyList<LeaderboardItem>> ListTopSongs { get; }
    public IObservable<IReadOnlyList<LeaderboardItem>> ListTopAlbums { get; }
    public IObservable<IReadOnlyList<InsightStat>> ListInsights { get; }

    public ArtistStatsService(IRealmFactory realmF)
    {
        _realmF = realmF;
        _mainThreadRealm = _realmF.GetRealmInstance();

        var snapshotStream = _currentArtistId.Where(id => id.HasValue).Select(id => id.Value)
           .Select(artId => _mainThreadRealm.All<DimmerPlayEvent>().AsObservableChangeSet().Throttle(TimeSpan.FromMilliseconds(250)).Select(_ => artId)
               .Select(id => Observable.FromAsync(() => Task.Run(() => {
                   using var bgRealm = _realmF.GetRealmInstance();
                   var bgArtist = bgRealm.Find<ArtistModel>(id);
                   var artistSongs = bgArtist?.Songs.ToList();
                   var songIds = artistSongs.Select(s => s.Id).ToHashSet();
                   var artEvents = bgRealm.All<DimmerPlayEvent>().ToList().Where(e => e.SongId.HasValue && songIds.Contains(e.SongId.Value)).OrderBy(e => e.DatePlayed).ToList();
                   int totPlays = bgRealm.All<DimmerPlayEvent>().Count();
                   return CalculateArtistSnapshot(bgArtist, artistSongs, artEvents, totPlays);
               }))).Switch()).Switch().Publish().RefCount();

        TextLoyaltyIndex = snapshotStream.Select(s => s.Loyalty).ObserveOn(RxSchedulers.UI);
        TextBingeScore = snapshotStream.Select(s => s.Binge).ObserveOn(RxSchedulers.UI);
        TextDiscoveryComparison = snapshotStream.Select(s => s.Disco).ObserveOn(RxSchedulers.UI);
        ListMonthlyTrend = snapshotStream.Select(s => s.MTrend).ObserveOn(RxSchedulers.UI);
        ListDeepCuts = snapshotStream.Select(s => s.DeepCuts).ObserveOn(RxSchedulers.UI);
        ListTopSongs = snapshotStream.Select(s => s.TopSongs).ObserveOn(RxSchedulers.UI);
        ListTopAlbums = snapshotStream.Select(s => s.TopAlbums).ObserveOn(RxSchedulers.UI);
        ListInsights = snapshotStream.Select(s => s.Insights).ObserveOn(RxSchedulers.UI);
    }

    private ArtistSnapshot CalculateArtistSnapshot(ArtistModel? artist, List<SongModel> songs, List<DimmerPlayEvent> events, int totEvts)
    {
        if (artist == null || events.Count == 0) return ArtistSnapshot.Empty();

        int plays = events.Count(e => e.PlayType == 3);
        var loyalty = new TextStat("Loyalty Index", totEvts > 0 ? $"{((double)plays / totEvts) * 100:F1}%" : "0%", "Share of total library plays");

        var bingeDay = events.Where(e => e.PlayType == 3).GroupBy(e => e.DatePlayed.Date).OrderByDescending(g => g.Count()).FirstOrDefault();
        var binge = new TextStat("Binge Record", bingeDay != null ? $"{bingeDay.Count()} plays" : "0", bingeDay != null ? bingeDay.Key.ToString("MMM d, yyyy") : "");

        var tYearStart = new DateTimeOffset(DateTime.UtcNow.Year, 1, 1, 0, 0, 0, TimeSpan.Zero);
        int playsThisYear = events.Count(e => e.DatePlayed >= tYearStart);
        int playsLastYear = events.Count(e => e.DatePlayed >= tYearStart.AddYears(-1) && e.DatePlayed < tYearStart);
        var disco = new TextStat("YoY Growth", $"{playsThisYear} vs {playsLastYear}", "Plays this year vs last year");

        var mTrend = new List<TrendStat>();
        for (int i = 0; i < 12; i++)
        {
            var start = DateTimeOffset.UtcNow.AddMonths(-(i + 1));
            var end = start.AddMonths(1);
            int cur = events.Count(e => e.DatePlayed > start && e.DatePlayed <= end);
            int prev = events.Count(e => e.DatePlayed > start.AddMonths(-1) && e.DatePlayed <= start);
            mTrend.Add(new TrendStat($"{start:MMM yyyy}", cur, cur - prev));
        }

        var topSongs = songs.Select(s => new { S = s, P = events.Count(e => e.SongId == s.Id && e.PlayType == 3) }).OrderByDescending(x => x.P).Take(10).Select((x, i) => new LeaderboardItem(Rank: $"#{i + 1}", Name: x.S.Title, SubValue: $"{x.P} plays", ImagePath: x.S.CoverImagePath)).ToList();
        var deepCuts = songs.Select(s => new { S = s, P = events.Count(e => e.SongId == s.Id && e.PlayType == 3) }).Where(x => x.P > 0).OrderBy(x => x.P).Take(10).Select((x, i) => new LeaderboardItem($"#{i + 1}", x.S.Title, $"{x.P} plays", x.S.CoverImagePath)).ToList();
        var topAlbums = events.Where(e => e.PlayType == 3   ).GroupBy(e => songs.FirstOrDefault(s => s.Id == e.SongId)?.Album).Where(g => g.Key != null).OrderByDescending(g => g.Count()).Take(10).Select((g, i) => new LeaderboardItem($"#{i + 1}", g.Key!.Name, $"{g.Count()} plays", g.Key.ImagePath)).ToList();

        var insights = new List<InsightStat>();
        var topSongGrp = events.GroupBy(e => e.SongId).OrderByDescending(g => g.Count()).FirstOrDefault();
        if (topSongGrp != null && plays > 10 && ((double)topSongGrp.Count() / plays) > 0.8)
        {
            insights.Add(new InsightStat("One-Hit Wonder", $"'{songs.FirstOrDefault(s => s.Id == topSongGrp.Key)?.Title}' accounts for over 80% of your plays for this artist.", "⭐"));
        }
        if (plays < 5 && (DateTimeOffset.UtcNow - events.Min(e => e.DatePlayed)).TotalDays > 90)
        {
            insights.Add(new InsightStat("Abandoned", "Discovered over 3 months ago but rarely played.", "👻"));
        }

        return new ArtistSnapshot(loyalty, binge, disco, mTrend, deepCuts, topSongs, topAlbums, insights);
    }
    private record ArtistSnapshot(TextStat Loyalty, TextStat Binge, TextStat Disco, IReadOnlyList<TrendStat> MTrend, IReadOnlyList<LeaderboardItem> DeepCuts, IReadOnlyList<LeaderboardItem> TopSongs, IReadOnlyList<LeaderboardItem> TopAlbums, IReadOnlyList<InsightStat> Insights) 
    {
        public static ArtistSnapshot Empty()
        {
            return new(new("", ""), new("", ""), new("", ""), new List<TrendStat>(), new List<LeaderboardItem>(), new List<LeaderboardItem>(), new List<LeaderboardItem>(), new List<InsightStat>());
        }
    }
}