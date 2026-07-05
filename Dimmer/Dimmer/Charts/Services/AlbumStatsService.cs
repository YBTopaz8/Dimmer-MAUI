using System;
using System.Collections.Generic;
using System.Text;

namespace Dimmer.Charts.Services;

public class AlbumStatsService
{
    private readonly IRealmFactory _realmF;
    private readonly Realm _mainThreadRealm;
    private readonly BehaviorSubject<ObjectId?> _currentAlbumId = new(null);
    public void SetAlbumId(ObjectId id) => _currentAlbumId.OnNext(id);

    // Outputs
    public IObservable<TextStat> GoldenRatioTrack { get; }
    public IObservable<TextStat> CompletionRate { get; }
    public IObservable<IReadOnlyList<ChartPoint>> PlaysPerTrack { get; }
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
                   var songs = album?.SongsInAlbum.ToList() ?? new List<SongModel>();
                   var songIds = songs.Select(s => s.Id).ToHashSet();
                   var evs = bgRealm.All<DimmerPlayEvent>().ToList().Where(e => e.SongId.HasValue && songIds.Contains(e.SongId.Value)).ToList();
                   return CalculateAlbumSnapshot(album, songs, evs);
               }))).Switch()).Switch().Publish().RefCount();

        GoldenRatioTrack = snapshotStream.Select(s => s.Golden).ObserveOn(RxSchedulers.UI);
        CompletionRate = snapshotStream.Select(s => s.CompRate).ObserveOn(RxSchedulers.UI);
        PlaysPerTrack = snapshotStream.Select(s => s.TrackPlays).ObserveOn(RxSchedulers.UI);
        Insights = snapshotStream.Select(s => s.Insights).ObserveOn(RxSchedulers.UI);
    }

    private AlbumSnapshot CalculateAlbumSnapshot(AlbumModel? album, List<SongModel> songs, List<DimmerPlayEvent> events)
    {
        if (album == null || songs.Count == 0) return AlbumSnapshot.Empty();

        var insights = new List<InsightStat>();
        if (album.TrackTotal > 0 && songs.Count != album.TrackTotal) insights.Add(new InsightStat("Incomplete", $"You have {songs.Count} tracks, but the official album has {album.TrackTotal}.", "⚠️"));

        double totalDur = songs.Sum(s => s.DurationInSeconds);
        double grTime = totalDur / 1.61803398875;
        double cum = 0; string grTrack = "Unknown";
        foreach (var s in songs.OrderBy(x => x.TrackNumber)) { cum += s.DurationInSeconds; if (cum >= grTime) { grTrack = s.Title; break; } }
        var golden = new TextStat("Golden Ratio Track", grTrack, "The aesthetic center of the album");

        int plays = events.Count(e => e.PlayType == 0);
        int comps = events.Count(e => e.PlayType == 3);
        var compRate = new TextStat("Album Completion", plays > 0 ? $"{((double)comps / plays) * 100:F0}%" : "0%");

        var tp = songs.Select(s => new ChartPoint($"Trk {s.TrackNumber ?? 0}", events.Count(e => e.SongId == s.Id && e.PlayType == 0))).ToList();

        return new AlbumSnapshot(golden, compRate, tp, insights);
    }
    private record AlbumSnapshot(TextStat Golden, TextStat CompRate, IReadOnlyList<ChartPoint> TrackPlays, IReadOnlyList<InsightStat> Insights) { public static AlbumSnapshot Empty() => new(new("", ""), new("", ""), new List<ChartPoint>(), new List<InsightStat>()); }
}