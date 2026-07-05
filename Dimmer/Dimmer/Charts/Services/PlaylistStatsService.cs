namespace Dimmer.Charts.Services;


public class PlaylistStatsService
{
    private readonly IRealmFactory _realmF;
    private readonly Realm _mainThreadRealm;
    private readonly BehaviorSubject<ObjectId?> _currentPlId = new(null);
    public void SetPlaylistId(ObjectId id) => _currentPlId.OnNext(id);

    public IObservable<TextStat> LoyaltyScore { get; }
    public IObservable<TextStat> DominantMood { get; }
    public IObservable<IReadOnlyList<ChartPoint>> PlaySkipRatio { get; }

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
                   return CalculatePlaylistSnapshot(songs, evs);
               }))).Switch()).Switch().Publish().RefCount();

        LoyaltyScore = snapshotStream.Select(s => s.Loyalty).ObserveOn(RxSchedulers.UI);
        DominantMood = snapshotStream.Select(s => s.Mood).ObserveOn(RxSchedulers.UI);
        PlaySkipRatio = snapshotStream.Select(s => s.PlaySkip).ObserveOn(RxSchedulers.UI);
    }

    private PlaylistSnapshot CalculatePlaylistSnapshot(List<SongModel> songs, List<DimmerPlayEvent> events)
    {
        if (songs.Count == 0) return new PlaylistSnapshot(new("", ""), new("", ""), new List<ChartPoint>());

        int plays = events.Count(e => e.PlayType == 0);
        int comps = events.Count(e => e.PlayType == 3);
        int skips = events.Count(e => e.PlayType == 5);

        var loyalty = new TextStat("Loyalty Score", plays > 0 ? $"{((double)comps / plays) * 100:F1}%" : "0%", "Plays that reach the end");

        var tags = songs.SelectMany(s => s.Tags).GroupBy(t => t.Name).OrderByDescending(g => g.Count()).FirstOrDefault();
        var mood = new TextStat("Dominant Mood", tags != null ? tags.Key : "Mixed", "Based on song tags");

        var ps = new List<ChartPoint> { new("Completed", comps), new("Skipped", skips) };

        return new PlaylistSnapshot(loyalty, mood, ps);
    }
    private record PlaylistSnapshot(TextStat Loyalty, TextStat Mood, IReadOnlyList<ChartPoint> PlaySkip);
}