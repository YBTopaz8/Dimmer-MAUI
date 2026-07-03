namespace Dimmer.Charts.Services;

public class PlaylistStatsService
{
    private readonly BehaviorSubject<ObjectId?> _currentPlaylistId = new(null);

    // TEXT
    public IObservable<TextStat> PlaylistLoyalty { get; }
    public IObservable<TextStat> AverageBpm { get; }

    // CHARTS
    public IObservable<IReadOnlyList<ChartPoint>> PlaySkipRatio { get; }
    public IObservable<IReadOnlyList<ChartPoint>> ArtistConcentration { get; }

    // COLLECTIONS
    public IObservable<IReadOnlyList<LeaderboardItem>> MostSkippedTracks { get; }

    public PlaylistStatsService(IRealmFactory realmFactory)
    {
        var playlistSnapshotStream = _currentPlaylistId
            .Where(id => id.HasValue).Select(id => id.Value)
            .Select(playlistId =>
            {
                var realm = realmFactory.GetRealmInstance();
                var playlist = realm.Find<PlaylistModel>(playlistId);
                var songIds = playlist.SongsIdsInPlaylist.Select(s => (QueryArgument)s).ToArray();
                var songsList = playlist.SongsInPlaylist.ToList(); // In memory

                return realm.All<DimmerPlayEvent>()
                    .Filter("SongId IN $0", songIds)
                    .AsObservableChangeSet()
                    .Throttle(TimeSpan.FromMilliseconds(250))
                    .ToCollection()
                    .Select(events => CalculatePlaylistSnapshot(songsList, events));
            })
            .Switch().Publish().RefCount();

        // Bindings
        PlaylistLoyalty = playlistSnapshotStream.Select(s => s.Loyalty).ObserveOn(RxSchedulers.UI);
        AverageBpm = playlistSnapshotStream.Select(s => s.AvgBpm).ObserveOn(RxSchedulers.UI);
        PlaySkipRatio = playlistSnapshotStream.Select(s => s.PlaySkipChart).ObserveOn(RxSchedulers.UI);
        ArtistConcentration = playlistSnapshotStream.Select(s => s.ArtistChart).ObserveOn(RxSchedulers.UI);
        MostSkippedTracks = playlistSnapshotStream.Select(s => s.MostSkipped).ObserveOn(RxSchedulers.UI);
    }

    public void SetPlaylistId(ObjectId id) => _currentPlaylistId.OnNext(id);

    private PlaylistSnapshot CalculatePlaylistSnapshot(List<SongModel> songs, IReadOnlyCollection<DimmerPlayEvent> events)
    {
        // Loyalty: How often a play is actually completed in this playlist?
        int starts = events.Count(e => e.PlayType == 0);
        int completes = events.Count(e => e.PlayType == 3);
        double loyalty = starts > 0 ? ((double)completes / starts) * 100 : 0;

        double avgBpm = songs.Where(s => s.BPM > 0).Average(s => (double?)s.BPM) ?? 0;

        var playSkip = new List<ChartPoint>
        {
            new("Completed", completes),
            new("Skipped", events.Count(e => e.PlayType == 5))
        };

        var artistConc = songs.GroupBy(s => s.ArtistName)
            .Select(g => new ChartPoint(g.Key ?? "Unknown", g.Count()))
            .OrderByDescending(c => c.YValue).Take(5).ToList();

        var mostSkipped = songs
            .Select(s => new { Song = s, Skips = events.Count(e => e.SongId == s.Id && e.PlayType == 5) })
            .Where(x => x.Skips > 0).OrderByDescending(x => x.Skips).Take(10)
            .Select((x, i) => new LeaderboardItem($"#{i + 1}", x.Song.Title, $"{x.Skips} skips", x.Song.CoverImagePath))
            .ToList();

        return new PlaylistSnapshot(
            new TextStat("Loyalty Score", $"{loyalty:F1}%", "Starts that finish"),
            new TextStat("Vibe", $"{avgBpm:F0} BPM", "Average Playlist Tempo"),
            playSkip, artistConc, mostSkipped
        );
    }

    private record PlaylistSnapshot(
        TextStat Loyalty, TextStat AvgBpm,
        IReadOnlyList<ChartPoint> PlaySkipChart, IReadOnlyList<ChartPoint> ArtistChart,
        IReadOnlyList<LeaderboardItem> MostSkipped);
}