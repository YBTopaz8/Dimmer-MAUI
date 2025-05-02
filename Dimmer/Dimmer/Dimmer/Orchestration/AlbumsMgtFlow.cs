using Dimmer.Services;
using System.Text;
using System.Text.Json;

namespace Dimmer.Orchestration;
public class AlbumsMgtFlow : BaseAppFlow, IDisposable
{
    private readonly IRepository<AlbumModel> _albumRepo;
    private readonly IRepository<AlbumArtistGenreSongLink> _linkRepo;
    private readonly IRepository<PlayDateAndCompletionStateSongLink> _pdlRepo;
    private readonly IMapper _mapper;
    private readonly SubscriptionManager _subs;

    private readonly BehaviorSubject<List<AlbumModel>> _specificAlbums = new(new());
    public IObservable<List<AlbumModel>> SpecificAlbums => _specificAlbums.AsObservable();

    private readonly BehaviorSubject<double> _syncProgress = new(0);
    public IObservable<double> SyncProgress => _syncProgress.AsObservable();

    public AlbumsMgtFlow(
        IPlayerStateService state,
        IRepository<SongModel> songRepo,
        IRepository<GenreModel> genreRepo,
        IRepository<AlbumArtistGenreSongLink> aagslRepo,
        IRepository<PlayDateAndCompletionStateSongLink> pdlRepo,
        IRepository<PlaylistModel> playlistRepo,
        IRepository<ArtistModel> artistRepo,
        IRepository<AlbumModel> albumRepo,
        IRepository<AlbumArtistGenreSongLink> linkRepo,
        ISettingsService settings,
        IFolderMgtService folderMonitor,
        IMapper mapper,
        SubscriptionManager subs
    ) : base(state,  songRepo, genreRepo, aagslRepo, pdlRepo, playlistRepo, artistRepo, albumRepo, settings, folderMonitor, mapper)
    {
        _albumRepo     = albumRepo;
        _linkRepo      = linkRepo;
        _pdlRepo       = pdlRepo;
        _mapper        = mapper;
        _subs          = subs;
    }

    // 1. Filtering & Searching
    public void GetAlbumsByReleaseYearRange(int startYear, int endYear)
    {
        var list = _albumRepo.GetAll().AsEnumerable()
            .Where(a => a.ReleaseYear >= startYear && a.ReleaseYear <= endYear)
            .ToList();
        _specificAlbums.OnNext(list);
    }

    public void GetAlbumsByArtistId(string artistId)
    {
        var albumIds = _linkRepo.GetAll().AsEnumerable()
            .Where(l => l.ArtistId == artistId)
            .Select(l => l.AlbumId)
            .Distinct();
        var list = _albumRepo.GetAll().AsEnumerable()
            .Where(a => albumIds.Contains(a.LocalDeviceId))
            .ToList();
        _specificAlbums.OnNext(list);
    }
    private string? currentLocalSongId;
    public void GetAlbumsBySongId(string songId)
    {
        if(currentLocalSongId is not null && currentLocalSongId ==songId)
        {
            return;
        }
        var albumIds = _linkRepo.GetAll().AsEnumerable()
            .Where(l => l.SongId == songId)
            .Select(l => l.AlbumId)
            .Distinct();
        var list = _albumRepo.GetAll().AsEnumerable()
            .Where(a => albumIds.Contains(a.LocalDeviceId))
            .ToList();
        currentLocalSongId = songId;
        _specificAlbums.OnNext(list);
    }

    public void GetAlbumsByGenreId(string genreId)
    {
        var albumIds = _linkRepo.GetAll().AsEnumerable()
            .Where(l => l.GenreId == genreId)
            .Select(l => l.AlbumId)
            .Distinct();
        var list = _albumRepo.GetAll().AsEnumerable()
            .Where(a => albumIds.Contains(a.LocalDeviceId))
            .ToList();
        _specificAlbums.OnNext(list);
    }

    public void SearchAlbumsByName(string query)
    {
        var list = _albumRepo.GetAll().AsEnumerable()
            .Where(a => a.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToList();
        _specificAlbums.OnNext(list);
    }

    public void SearchAlbumsByKeyword(string keyword)
    {
        var list = _albumRepo.GetAll().AsEnumerable()
            .Where(a => a.Name!.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                     || (a.Description?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false))
            .ToList();
        _specificAlbums.OnNext(list);
    }

    public void GetAlbumsWithNoCoverArt()
    {
        var list = _albumRepo.GetAll().AsEnumerable()
            .Where(a => string.IsNullOrEmpty(a.ImagePath))
            .ToList();
        _specificAlbums.OnNext(list);
    }

    public void GetAlbumsWithoutTracks()
    {
        var list = _albumRepo.GetAll().AsEnumerable()
            .Where(a => a.NumberOfTracks != 0)
            .ToList();
        _specificAlbums.OnNext(list);
    }

    public void GetAlbumsAddedInLastDays(int days)
    {
        var cutoff = DateTime.Now.AddDays(-days);
        var list = _albumRepo.GetAll().AsEnumerable()
        .Where(a =>
        {
            // assuming DateCreated is stored as a string
            if (DateTime.TryParse(a.DateCreated, out var dt))
                return dt >= cutoff;
            return false;
        })
        .ToList();
        _specificAlbums.OnNext(list);
    }

    // 2. Sorting & Grouping
    public List<AlbumModel> SortAlbumsByName(bool ascending = true)
    {
        return ascending
                ? [.. _albumRepo.GetAll().AsEnumerable().OrderBy(a => a.Name)]
                : [.. _albumRepo.GetAll().AsEnumerable().OrderByDescending(a => a.Name)];
    }

    public List<AlbumModel> SortAlbumsByDateAdded(bool ascending = false)
    {
        return ascending
                ? [.. _albumRepo.GetAll().AsEnumerable().OrderBy(a => a.DateCreated)]
                : [.. _albumRepo.GetAll().AsEnumerable().OrderByDescending(a => a.DateCreated)];
    }

    public Dictionary<string, List<AlbumModel>> GroupAlbumsByArtist()
    {
        return _linkRepo.GetAll().AsEnumerable()
                .GroupBy(l => l.ArtistId!)
                .ToDictionary(
                    g => g.Key,
                    g => _albumRepo.GetAll().AsEnumerable()
                         .Where(a => g.Select(l => l.AlbumId).Contains(a.LocalDeviceId))
                         .ToList()
                );
    }

    public Dictionary<string, List<AlbumModel>> GroupAlbumsByGenre()
    {
        return _linkRepo.GetAll().AsEnumerable()
                .GroupBy(l => l.GenreId!)
                .ToDictionary(
                    g => g.Key,
                    g => _albumRepo.GetAll().AsEnumerable()
                         .Where(a => g.Select(l => l.AlbumId).Contains(a.LocalDeviceId))
                         .ToList()
                );
    }

    public List<AlbumModel> GetAlbumsOrderedByTrackCount(bool ascending = false)
    {
        return ascending
                ? [.. _albumRepo.GetAll().AsEnumerable().OrderBy(a => a.NumberOfTracks)]
                : [.. _albumRepo.GetAll().AsEnumerable().OrderByDescending(a => a.NumberOfTracks)];
    }

    public List<AlbumModel> GetAlbumsOrderedByTotalDuration(bool ascending = false)
    {
        return ascending
                ? [.. _albumRepo.GetAll().AsEnumerable().OrderBy(a => a.TotalDuration)]
                : [.. _albumRepo.GetAll().AsEnumerable().OrderByDescending(a => a.TotalDuration)];
    }

    // 3. Statistics & Insights
    public int GetTotalAlbumCount()
    {
        return _albumRepo.GetAll().Count;
    }

    public Dictionary<string, int> GetAlbumPlayCounts()
    {
        return _pdlRepo.GetAll().AsEnumerable()
                .Where(p => p.PlayType == (int)PlayType.Play)
                .GroupBy(p => _linkRepo.GetAll().AsEnumerable()
                    .FirstOrDefault(l => l.SongId == p.SongId)
                    ?.AlbumId)
                .Where(g => g.Key != null)
                .ToDictionary(g => g.Key!, g => g.Count());
    }

    public Dictionary<string, TimeSpan> GetAlbumTotalListenTime()
    {
        return _pdlRepo.GetAll().AsEnumerable()
                .GroupBy(p => _linkRepo.GetAll().AsEnumerable()
                    .FirstOrDefault(l => l.SongId == p.SongId)
                    ?.AlbumId)
                .Where(g => g.Key != null)
                .ToDictionary(
                    g => g.Key!,
                    g => TimeSpan.FromSeconds(g.Sum(p => p.PositionInSeconds))
                );
    }

    public TimeSpan GetTotalLibraryDuration()
    {
        return _albumRepo
            .GetAll().AsEnumerable()
            .Select(a =>
            {
                if (double.TryParse(a.TotalDuration, out var secs))
                    return TimeSpan.FromSeconds(secs);
                return TimeSpan.Zero;
            })
            .Aggregate(TimeSpan.Zero, (sum, span) => sum + span);
    }

    public Dictionary<string, int> GetAlbumSkipCounts()
    {
        return _pdlRepo.GetAll().AsEnumerable()
                .Where(p => p.PlayType == (int)PlayType.Skipped)
                .GroupBy(p => _linkRepo.GetAll().AsEnumerable()
                    .FirstOrDefault(l => l.SongId == p.SongId)
                    ?.AlbumId)
                .Where(g => g.Key != null)
                .ToDictionary(g => g.Key!, g => g.Count());
    }

    public Dictionary<string, DateTimeOffset> GetLastPlayedTimestamps()
    {
        return _pdlRepo.GetAll().AsEnumerable()
                .GroupBy(p => _linkRepo.GetAll().AsEnumerable()
                    .FirstOrDefault(l => l.SongId == p.SongId)
                    ?.AlbumId)
                .Where(g => g.Key != null)
                .ToDictionary(g => g.Key!, g => g.Max(p => p.DatePlayed));
    }

    public List<string> GetMostSkewedAlbums(int topN)
    {
        return [.. GetAlbumSkipCounts()
            .OrderByDescending(kv =>
                (double)kv.Value /
                (GetAlbumPlayCounts().GetValueOrDefault(kv.Key, 1)))
            .Take(topN)
            .Select(kv => kv.Key)];
    }

    // 4. Recommendations & Smart Picks
    public List<AlbumModel> RecommendSimilarAlbums(string albumId, int count = 5)
    {
        var targetGenres = _linkRepo.GetAll().AsEnumerable()
            .Where(l => l.AlbumId == albumId)
            .Select(l => l.GenreId)
            .Distinct();
        var targetArtists = _linkRepo.GetAll().AsEnumerable()
            .Where(l => l.AlbumId == albumId)
            .Select(l => l.ArtistId)
            .Distinct();

        return [.. _albumRepo.GetAll().AsEnumerable()
            .Where(a => a.LocalDeviceId != albumId)
            .Select(a => new
            {
                Album = a,
                Score = _linkRepo.GetAll().AsEnumerable().Count(l =>
                    l.AlbumId == a.LocalDeviceId
                    && (targetGenres.Contains(l.GenreId)
                     || targetArtists.Contains(l.ArtistId)))
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Take(count)
            .Select(x => x.Album)];
    }

    public List<AlbumModel> GetRecentlyPlayedAlbums(int count)
    {
        return GetLastPlayedTimestamps()
                .OrderByDescending(kv => kv.Value)
                .Take(count)
                .Select(kv => _albumRepo.GetById(kv.Key))
                .Where(a => a != null)
                .ToList()!;
    }

    public List<AlbumModel> GetAlbumsNotPlayedSince(DateTime cutoff)
    {
        return GetLastPlayedTimestamps()
                .Where(kv => kv.Value < cutoff)
                .Select(kv => _albumRepo.GetById(kv.Key))
                .Where(a => a != null)
                .ToList()!;
    }

    public List<AlbumModel> GetTopAlbumsByPlayCount(int count)
    {
        return GetAlbumPlayCounts()
                .OrderByDescending(kv => kv.Value)
                .Take(count)
                .Select(kv => _albumRepo.GetById(kv.Key))
                .Where(a => a != null)
                .ToList()!;
    }

    public List<AlbumModel> GetUnderratedAlbums(int minPlays, int maxPlays)
    {
        return GetAlbumPlayCounts()
                .Where(kv => kv.Value >= minPlays && kv.Value <= maxPlays)
                .Select(kv => _albumRepo.GetById(kv.Key))
                .Where(a => a != null)
                .ToList()!;
    }

    //public List<AlbumModel> GetUserFavoriteAlbums()
    //    => GetAlbumAverageListenPercentage()
    //        .OrderByDescending(kv => kv.Value)
    //        .Take(10)
    //        .Select(kv => _albumRepo.GetById(kv.Key))
    //        .Where(a => a != null)
    //        .ToList()!;

    // 5. Metadata & Enrichment
    //public async Task FetchAndCacheCoverArtAsync(string albumId)
    //{
    //    //var url = await _remoteService.GetCoverArtUrlAsync(albumId);
    //    var album = _albumRepo.GetById(albumId);
    //    if (album != null)
    //    {
    //        album.ImagePath = url;
    //        _albumRepo.AddOrUpdate(album);
    //    }
    //}

    //public async Task<List<string>> GetAllAlbumTagsAsync(string albumId)
    //    => await  _remoteService.GetTagsAsync(albumId);

    //public async Task TagAlbumAsync(string albumId, string tag)
    //{
    //    var album = _albumRepo.GetById(albumId);
    //    if (album != null)
    //    {
    //        await _remoteService.AddTagAsync(albumId, tag);
    //    }
    //}

    //public void GetAlbumsByTag(string tag)
    //{
    //    var list = _albumRepo.GetAll().AsEnumerable()
    //        .Where(a => a.Tags.Contains(tag))
    //        .ToList();
    //    _specificAlbums.OnNext(list);
    //}

    public bool ValidateAlbumMetadata(string albumId)
    {
        var album = _albumRepo.GetById(albumId);
        return album != null
            && !string.IsNullOrEmpty(album.Name)
            && _linkRepo.GetAll().AsEnumerable().Any(l => l.AlbumId == albumId);
    }

    //public async Task EnrichAlbumWithWebInfoAsync(string albumId)
    //{
    //    var info = await _remoteService.GetAlbumInfoAsync(albumId);
    //    var album = _albumRepo.GetById(albumId);
    //    if (album != null)
    //    {
    //        album.Description = info.Description;
    //        album.TotalDuration = info.TotalDuration;
    //        _albumRepo.AddOrUpdate(album);
    //    }
    //}

    // 6. Import/Export & Sync
    public async Task ExportAlbumsToCsvAsync(string filePath)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Id,Name,ArtistCount,TrackCount,Duration");
        foreach (var a in _albumRepo.GetAll().AsEnumerable())
            sb.AppendLine($"{a.LocalDeviceId},{a.Name},"
                + $"{_linkRepo.GetAll().AsEnumerable().Count(l => l.AlbumId==a.LocalDeviceId)},"
                + $"{a.NumberOfTracks},{a.TotalDuration}");
        await File.WriteAllTextAsync(filePath, sb.ToString());
    }

    public async Task ExportAlbumsToJsonAsync(string filePath)
    {
        var json = JsonSerializer.Serialize(_albumRepo.GetAll().AsEnumerable());
        await File.WriteAllTextAsync(filePath, json);
    }

    public async Task ImportAlbumsFromCsvAsync(string filePath)
    {
        var lines = await File.ReadAllLinesAsync(filePath);
        foreach (var ln in lines.Skip(1))
        {
            var parts = ln.Split(',');
            var album = new AlbumModel
            {
                LocalDeviceId = parts[0],
                Name = parts[1],
                NumberOfTracks = int.Parse(parts[3]),
                TotalDuration = TimeSpan.FromSeconds(double.Parse(parts[4])).ToString()
            };
            _albumRepo.AddOrUpdate(album);
        }
    }
}