using System;
using System.Collections.Generic;
using System.Text;

namespace Dimmer.Charts.Services;

public class SongStatsService
{
    private readonly IRealmFactory _realmF;
    private readonly Realm _mainThreadRealm;
    private readonly BehaviorSubject<ObjectId?> _currentSongId = new(null);
    public void SetSongId(ObjectId id)
    {
        _currentSongId.OnNext(id);
    }

    // --- OUTPUTS ---
    public IObservable<TextStat> TextCompletionRate { get; }
    public IObservable<TextStat> TextAvgListenDuration { get; }
    public IObservable<TextStat> TextBingeFactor { get; }
    public IObservable<TextStat> TextPredictability { get; }

    public IObservable<IReadOnlyList<ChartPoint>> ListActionRadar { get; }
    public IObservable<IReadOnlyList<ChartPoint>> ListDropOffHeatmap { get; }
    public IObservable<IReadOnlyList<TrendStat>> ListWeeklyTrend { get; }
    public IObservable<IReadOnlyList<TrendStat>> ListMonthlyTrend { get; }

    public IObservable<IReadOnlyList<SongPairing>> ListPerfectPairings { get; }
    public IObservable<IReadOnlyList<PlaySession>> ListWalkthrough { get; }
    public IObservable<IReadOnlyList<InsightStat>> ListInsights { get; }

    public SongStatsService(IRealmFactory realmF)
    {
        _realmF = realmF;
        _mainThreadRealm = _realmF.GetRealmInstance();

        var snapshotStream = _currentSongId.Where(id =>
        {
            return id.HasValue;
        }).Select(id =>
        {
            return id.Value;
        })
           .Select(songId => _mainThreadRealm.All<DimmerPlayEvent>().Where(e => e.SongId == songId).AsObservableChangeSet()
               .Throttle(TimeSpan.FromMilliseconds(250)).Select(_ => songId)
               .Select(id => Observable.FromAsync(() =>
               {
                   return Task.Run(() =>
                                  {
                                      using var bgRealm = _realmF.GetRealmInstance();
                                      var bgSong = bgRealm.Find<SongModel>(id);
                                      var songEvents = bgRealm.All<DimmerPlayEvent>().Filter("SongId == $0", (QueryArgument)id).OrderBy(e => e.DatePlayed).ToList();
                                      var allEvents = bgRealm.All<DimmerPlayEvent>().OrderBy(e => e.DatePlayed).ToList();
                                      var res = CalculateSongSnapshot(bgSong, songEvents, allEvents, bgRealm);

                                      if (res is null)
                                      {

                                      }

                                      return res;
                                  });
               })).Switch()).Switch().Publish().RefCount();

        TextCompletionRate = snapshotStream.Select(s => s.CompRate).ObserveOn(RxSchedulers.UI);
        TextAvgListenDuration = snapshotStream.Select(s => s.AvgDuration).ObserveOn(RxSchedulers.UI);
        TextBingeFactor = snapshotStream.Select(s => s.Binge).ObserveOn(RxSchedulers.UI);
        TextPredictability = snapshotStream.Select(s => s.Predict).ObserveOn(RxSchedulers.UI);

        ListActionRadar = snapshotStream.Select(s => s.Radar).ObserveOn(RxSchedulers.UI);
        ListDropOffHeatmap = snapshotStream.Select(s => s.DropOff).ObserveOn(RxSchedulers.UI);
        ListWeeklyTrend = snapshotStream.Select(s => s.WTrend).ObserveOn(RxSchedulers.UI);
        ListMonthlyTrend = snapshotStream.Select(s => s.MTrend).ObserveOn(RxSchedulers.UI);

        ListPerfectPairings = snapshotStream.Select(s => s.Pairings).ObserveOn(RxSchedulers.UI);
        ListWalkthrough = snapshotStream.Select(s => s.Walkthrough).ObserveOn(RxSchedulers.UI);
        ListInsights = snapshotStream.Select(s => s.Insights).ObserveOn(RxSchedulers.UI);
    }

    private SongSnapshot CalculateSongSnapshot(SongModel? song, List<DimmerPlayEvent> events, List<DimmerPlayEvent> allEvents, Realm bgRealm)
    {
        if (song == null || events.Count == 0) return SongSnapshot.Empty();

        int plays = events.Count(e => e.PlayType == 0);
        int skips = events.Count(e => e.PlayType == 5);
        int completes = events.Count(e => e.WasPlayCompleted || e.PlayType == 3);

        var compRate = new TextStat("Completion Rate", plays > 3 ? $"{((double)completes / plays) * 100:F1}%" : "0%");
        var avgDur = new TextStat("Avg Listen", TimeSpan.FromSeconds(events.Where(e => e.PlayType == 5 || e.PlayType == 3).Select(e => e.PositionInSeconds).DefaultIfEmpty(0).Average()).ToString(@"mm\:ss"));

        var bingeGroup = events.Where(e => e.PlayType == 3).GroupBy(e => e.DatePlayed.Date).OrderByDescending(g => g.Count()).FirstOrDefault();
        var binge = new TextStat("Binge Factor", bingeGroup != null ? $"{bingeGroup.Count()} plays in 1 day" : "N/A");

        var radar = new List<ChartPoint> { new("Plays", plays), new("Skips", skips), new("Completions", completes), new("Repeats", events.Count(e => e.PlayType == 6 || e.PlayType == 8)) };

        var dropOff = events.Where(e => e.PlayType == (int)PlayEventType.Skipped && e.PositionInSeconds > 0).GroupBy(e => Math.Floor(e.PositionInSeconds / 10) * 10).Select(g => new ChartPoint($"{g.Key}s", g.Count(), g.Key)).OrderBy(c => c.XValue).ToList();

        var wTrend = new List<TrendStat>();
        for (int i = 0; i < 12; i++)
        {
            var start = DateTimeOffset.UtcNow.AddDays(-7 * (i + 1));
            var end = start.AddDays(7);
            int cur = events.Count(e => e.DatePlayed > start && e.DatePlayed <= end);
            int prev = events.Count(e => e.DatePlayed > start.AddDays(-7) && e.DatePlayed <= start);
            wTrend.Add(new TrendStat(Period: $"{start:MMM d}", PlayCount: cur, ChangeVsPrevious: cur - prev));
        }

        var mTrend = new List<TrendStat>();
        for (int i = 0; i < 6; i++)
        {
            var start = DateTimeOffset.UtcNow.AddMonths(-(i + 1));
            var end = start.AddMonths(1);
            int cur = events.Count(e => e.DatePlayed > start && e.DatePlayed <= end);
            int prev = events.Count(e => e.DatePlayed > start.AddMonths(-1) && e.DatePlayed <= start);
            mTrend.Add(new TrendStat($"{start:MMM yyyy}", cur, cur - prev));
        }

        Dictionary<ObjectId, int> pairingsDict = new Dictionary<ObjectId, int>();
        int totalFollowUps = 0;
        for (int i = 0; i < allEvents.Count - 1; i++)
        {
            if (allEvents[i].SongId == song.Id && allEvents[i + 1].SongId.HasValue && allEvents[i + 1].SongId != song.Id && (allEvents[i + 1].DatePlayed - allEvents[i].DatePlayed).TotalMinutes < 15)
            {

                var vall = pairingsDict.GetValueOrDefault(allEvents[i + 1].SongId.Value);
                vall = vall + 1;
                //var songVal = pairingsDict.GetValueOrDefault(concernedSong.Id).Item1 ;
                
                pairingsDict[allEvents[i + 1].SongId.Value] = vall;
                totalFollowUps++;
            }
        }
        var pairings = pairingsDict.OrderByDescending(kvp => kvp.Value).Take(10).Select(kvp =>
        {
            var songKey = kvp.Key;
            DimmerPlayEvent DimmerEvent = allEvents.First(e => e.SongId == songKey);

            var intVal = kvp.Value;


            SongModelView pairedSong = new SongModelView();

            pairedSong.Title = DimmerEvent.SongName;
            pairedSong.ArtistName = DimmerEvent.ArtistName;
            pairedSong.AlbumName = DimmerEvent.AlbumName;
            pairedSong.CoverImagePath = DimmerEvent.CoverImagePath;
            


            var query = "AlbumName == $0 AND Title == $1";
           
            //bgRealm.Find<SongModel>()
            var res = bgRealm.All<SongModel>().Filter<SongModel>(query, pairedSong.AlbumName, pairedSong.Title);

            if (res.Any())
            {
                var resSong = res.First();
                if (resSong is not null)
                {
                    return new SongPairing(PairedSongTitle: resSong.Title ?? "Unknown", TimesPlayedTogether: intVal, Context: "Played next", CoverImagePath: resSong.CoverImagePath, resSong.TitleDurationKey, songId: resSong.Id, isPresentOnDevice: true);
                }
            }
            else
            {
                var albQuery = "Name == $0";
                var albRes = bgRealm.All<AlbumModel>().Filter(albQuery, pairedSong.AlbumName);
                if (albRes.Any())
                {
                    var songsWithCover = albRes.First().SongsInAlbum?.Filter("CoverImagePath != ''");
                    if (songsWithCover is not null && songsWithCover.Any())
                    {
                        SongModel fSong = songsWithCover.First()!;
                        pairedSong.CoverImagePath = fSong.CoverImagePath;
                    }
                }
             }
            return new SongPairing(PairedSongTitle: pairedSong.Title, TimesPlayedTogether: intVal, Context: "Played next", CoverImagePath: pairedSong.CoverImagePath, songTitleDurationKey: null);


        }).ToList();

        var predict = new TextStat("Predictability", totalFollowUps > 0 && pairings.Count != 0 ? $"{((double)pairings.First().TimesPlayedTogether / totalFollowUps) * 100:F0}%" : "N/A", "Chance of playing top pair next");

        var walkthrough = new List<PlaySession>();
        var curStart = events[0].DatePlayed; int evCnt = 0; double dur = 0;
        foreach (var ev in events)
        {
            if ((ev.DatePlayed - curStart).TotalHours > 2 && evCnt > 0)
            {
                walkthrough.Add(new PlaySession(curStart, evCnt, dur, $"Session"));
                curStart = ev.DatePlayed; evCnt = 0; dur = 0;
            }
            evCnt++; dur += ev.PositionInSeconds;
        }
        if (evCnt > 0) walkthrough.Add(new PlaySession(curStart, evCnt, dur, $"Latest"));

        var insights = new List<InsightStat>();
        if (skips < plays * 0.1) insights.Add(new InsightStat("Personal Anthem", "High rating and extremely low skip rate. A true favorite.", "🏆"));
        if (song.Title.Replace(" ", "").ToLower() == new string(song.Title.Replace(" ", "").ToLower().Reverse().ToArray())) insights.Add(new InsightStat("Palindrome", "This song's title is a palindrome!", "🔁"));

        return new SongSnapshot(compRate, avgDur, binge, predict, radar, dropOff, wTrend, mTrend, pairings, walkthrough, insights);
    }

    private record SongSnapshot(TextStat CompRate, TextStat AvgDuration, TextStat Binge, TextStat Predict, IReadOnlyList<ChartPoint> Radar, IReadOnlyList<ChartPoint> DropOff, IReadOnlyList<TrendStat> WTrend, IReadOnlyList<TrendStat> MTrend, IReadOnlyList<SongPairing> Pairings, IReadOnlyList<PlaySession> Walkthrough, IReadOnlyList<InsightStat> Insights) { public static SongSnapshot Empty() => new(new("", ""), new("", ""), new("", ""), new("", ""), new List<ChartPoint>(), new List<ChartPoint>(), new List<TrendStat>(), new List<TrendStat>(), new List<SongPairing>(), new List<PlaySession>(), new List<InsightStat>()); }
}