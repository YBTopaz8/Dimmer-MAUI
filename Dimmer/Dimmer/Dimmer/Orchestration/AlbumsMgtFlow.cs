

namespace Dimmer.Orchestration;
public class AlbumsMgtFlow : BaseAppFlow, IDisposable
{
    private readonly IDimmerStateService state;
    private readonly IRepository<SongModel> _songRepo;
    private readonly IRepository<UserModel> userRepo;
    private readonly IRepository<GenreModel> genreRepo;
    private readonly IRepository<GenreModel> _genreRepo ;
    private readonly IRepository<AlbumModel> _albumRepo;
    private readonly IRepository<AppStateModel> appstateRepo;
    private readonly ISettingsService settings;
    private readonly IFolderMgtService folderMonitor;
    private readonly IRepository<DimmerPlayEvent> _pdlRepo;
    private readonly IRepository<PlaylistModel> playlistRepo;
    private readonly IRepository<ArtistModel> _artistRepo;
    private readonly IMapper _mapper;
    private readonly SubscriptionManager _subs;

    private readonly BehaviorSubject<List<ArtistModel>> _specificArtists = new(new());
    private readonly BehaviorSubject<List<SongModel>> _specificSongs = new(new());
    private readonly BehaviorSubject<List<AlbumModel>> _specificAlbums = new(new());
    private readonly BehaviorSubject<List<GenreModel>> _specificGenres= new(new());
    public IObservable<List<AlbumModel>> SpecificArtists => _specificAlbums.AsObservable();
    public IObservable<List<SongModel>> SpecificSongs => _specificSongs.AsObservable();
    public IObservable<List<GenreModel>> SpecificGenre => _specificGenres.AsObservable();
    public IObservable<List<AlbumModel>> SpecificAlbums => _specificAlbums.AsObservable();

    private readonly BehaviorSubject<double> _syncProgress = new(0);
    public IObservable<double> SyncProgress => _syncProgress.AsObservable();
    private readonly BehaviorSubject<IReadOnlyList<AlbumModel>> _queriedAlbums = new(Array.Empty<AlbumModel>());
    public IObservable<IReadOnlyList<AlbumModel>> QueriedAlbums => _queriedAlbums.AsObservable();
    private readonly BehaviorSubject<IReadOnlyList<SongModel>> _queriedSongs = new(Array.Empty<SongModel>());
    public IObservable<IReadOnlyList<SongModel>> QueriedSongs => _queriedSongs.AsObservable();

    public AlbumsMgtFlow(
        IDimmerStateService state,
        IRepository<SongModel> songRepo,
        IRepository<UserModel> userRepo,
        IRepository<GenreModel> genreRepo,
        IRepository<DimmerPlayEvent> pdlRepo,
        IRepository<PlaylistModel> playlistRepo,
        IRepository<ArtistModel> artistRepo,
        IRepository<AlbumModel> albumRepo,
        IRepository<AppStateModel> appstateRepo,
        ISettingsService settings,
        IFolderMgtService folderMonitor,        
        IMapper mapper,
        SubscriptionManager subs
        
    ) : base(state, songRepo, genreRepo, userRepo,  pdlRepo, playlistRepo, artistRepo, albumRepo, appstateRepo, settings, folderMonitor, subs, mapper)
    {
        this.state=state;
        _songRepo=songRepo;
        this.userRepo=userRepo;
        this.genreRepo=genreRepo;
        _albumRepo     = albumRepo;
        this.appstateRepo=appstateRepo;
        this.settings=settings;
        this.folderMonitor=folderMonitor;
        _pdlRepo       = pdlRepo;
        this.playlistRepo=playlistRepo;
        this._artistRepo=artistRepo;
        _mapper        = mapper;
        _subs          = subs;
    }
    public void GetAlbumsByArtistName(string artistName) // More user-friendly than ID
    {
        // This is a multi-step query if going from Artist Name -> Songs -> Albums
        // Step 1: Find the artist(s) by name (could be multiple if names aren't unique, handle that)
        var artists = _artistRepo.Query(ar => ar.Name == artistName);
        if (!artists.Any())
        {
            _queriedAlbums.OnNext(Array.Empty<AlbumModel>());
            return;
        }
        var artistIds = artists.Select(ar => ar.Id).ToList();

        // Step 2: Find songs by these artists
        // Realm doesn't directly support ANY artist.Id IN artistIds in a single sub-query like EF.
        // So, we query songs and filter in memory for this part OR iterate.
        // A better model might have direct links from Artist to Album if this is very common.
        // For now, leveraging SongModel.Artists:
        var songsByArtists = _songRepo.Query(s => s.ArtistIds.Any(a => artistIds.Contains(a.Id)));

        // Step 3: Get distinct albums from these songs
        var albums = songsByArtists.Select(s => s.Album)
                                   .Where(al => al != null)
                                   .DistinctBy(al => al!.Id) // Requires .NET 6+ for DistinctBy
                                   .ToList();
        _queriedAlbums.OnNext(albums!);
    }

    // 1. Filtering & Searching
    public void GetAlbumsByReleaseYearRange(int startYear, int endYear)
    {
        // Use the Query method from your repository
        var list = _albumRepo.Query(a => a.ReleaseYear >= startYear && a.ReleaseYear <= endYear);
        _queriedAlbums.OnNext(list); // Assuming Query returns List<T>
    }

    public void GetAlbumsByArtistName_Alternative(string artistName)
    {
        var artist = _artistRepo.Query(ar => ar.Name == artistName).FirstOrDefault();
        if (artist == null)
        {
            _queriedAlbums.OnNext(Array.Empty<AlbumModel>());
            return;
        }

        // Requires ArtistModel to have:
        // [Backlink(nameof(SongModel.Artists))]
        // public IQueryable<SongModel> Songs { get; }
        // And your repo's Query method needs to handle IQueryable sources for such traversals if it doesn't already.
        // If _artistRepo.Query returns List<T>, this won't work directly with artist.Songs.
        // This highlights the need for repo methods that can handle more complex queries or return IQueryable.

        // Assuming you can get songs for an artist efficiently:
        var songsOfArtist = _songRepo.Query(s => s.ArtistIds.Any(a => a.Id == artist.Id));
        var albums = songsOfArtist.Select(s => s.Album)
                                  .Where(al => al != null)
                                  .DistinctBy(al => al!.Id)
                                  .ToList();
        _queriedAlbums.OnNext(albums!);
    }


    public void GetSongsByGenreName(string genreName)
    {
        var genre = _genreRepo.Query(g => g.Name == genreName).FirstOrDefault();
        if (genre == null)
        {
            _queriedSongs.OnNext(Array.Empty<SongModel>());
            return;
        }
        // Assumes SongModel.Genre links to GenreModel
        var songs = _songRepo.Query(s => s.Genre != null && s.Genre.Id == genre.Id);
        _queriedSongs.OnNext(songs);
    }

    public void SearchAlbumsByName(string query)
    {
        // Realm's default string comparisons are case-sensitive.
        // For case-insensitive, you might need to store a normalized version or rely on .NET filtering after a broader query.
        // Or use Realm Full-Text Search if available and configured.
        // A common approach for simple case-insensitivity if not using FTS:
        // Fetch and filter, or make sure your repo Query can translate ToLower()
        // For this example, let's assume your Query method can handle ToLower or you accept case-sensitivity.
        // If your IRepository<T>.Query method uses LINQ to Realm IQueryable:
        // var list = _albumRepo.Query(a => a.Name.Contains(query)); // Case-sensitive by default in RQL
        // For case-insensitive with LINQ-to-Objects on frozen results:
        var allAlbums = _albumRepo.GetAll(); // Get all frozen albums (use with caution for large sets)
        var list = allAlbums.Where(a => a.Name != null && a.Name.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
        _queriedAlbums.OnNext(list);
        // IDEAL: _albumRepo.Query(a => a.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
        //        but this depends on the Realm LINQ provider's capabilities for string methods.
        //        Often, `string.Contains(value, StringComparison)` is not translated.
        //        `CONTAINS[c]` is the RQL equivalent. Your repo needs to build such queries.
    }

    public void GetAlbumsWithNoCoverArt()
    {
        var list = _albumRepo.Query(a => string.IsNullOrEmpty(a.ImagePath));
        _queriedAlbums.OnNext(list);
    }

    public void GetAlbumsWithoutAnySongs() // Renamed for clarity
    {
        // Assumes AlbumModel has Songs backlink: [Backlink("Album")] IQueryable<SongModel> Songs { get; }
        var list = _albumRepo.Query(a => !a.Songs.Any());
        _queriedAlbums.OnNext(list);
    }

    public void GetAlbumsAddedInLastDays(int days)
    {
        var cutoff = DateTimeOffset.UtcNow.AddDays(-days);
        // Assumes AlbumModel has a DateAddedToLibrary property of type DateTimeOffset
        var list = _albumRepo.Query(a => a.DateCreated >= cutoff);
        _queriedAlbums.OnNext(list);
    }

    // --- 2. Sorting & Grouping ---
    // Sorting should ideally be done by Realm if possible.
    // Your IRepository<T> would need methods like:
    // QueryOrdered<TKey>(Expression<Func<T, bool>> predicate, Expression<Func<T, TKey>> keySelector, bool ascending)

    public void GetAlbumsSortedByName(bool ascending = true)
    {
        // If repo can't do it, sort the frozen list.
        var allAlbums = _albumRepo.GetAll(); // Potentially inefficient
        var sortedList = ascending
            ? allAlbums.OrderBy(a => a.Name, StringComparer.OrdinalIgnoreCase).ToList()
            : allAlbums.OrderByDescending(a => a.Name, StringComparer.OrdinalIgnoreCase).ToList();
        _queriedAlbums.OnNext(sortedList);
    }




    //public void GetAlbumsByArtistId(string artistId)
    //{
    //    var albumIds = _linkRepo.GetAll().AsEnumerable()
    //        .Where(l => l.ArtistId == artistId)
    //        .Select(l => l.AlbumId)
    //        .Distinct();
    //    var list = _albumRepo.GetAll().AsEnumerable()
    //        .Where(a => albumIds.Contains(a.Id))
    //        .ToList();
    //    _specificAlbums.OnNext(list);
    //}

    //// Get Genres by SongId
    //public void GetGenresBySongId(string songId)
    //{
    //    var genreIds = _linkRepo.GetAll().AsEnumerable()
    //        .Where(l => l.SongId == songId)
    //        .Select(l => l.GenreId)
    //        .Distinct();

    //    var list = _genreRepo.GetAll().AsEnumerable()
    //        .Where(g => genreIds.Contains(g.Id))
    //        .ToList();

    //    _specificGenres.OnNext(list);
    //}

    //// Get Songs by ArtistId
    //public void GetSongsByArtistId(string artistId)
    //{
    //    var songIds = _linkRepo.GetAll().AsEnumerable()
    //        .Where(l => l.ArtistId == artistId)
    //        .Select(l => l.SongId)
    //        .Distinct();

    //    var list = _songRepo.GetAll().AsEnumerable()
    //        .Where(s => songIds.Contains(s.Id))
    //        .ToList();

    //    _specificSongs.OnNext(list);
    //}


    //private string? currentLocalSongId;
    public void GetAlbumsBySongId(ObjectId songId)
    {
        throw new NotImplementedException("This method is not implemented yet.");
    }

    //public void GetAlbumsByGenreId(string genreId)
    //{
    //    var albumIds = _linkRepo.GetAll().AsEnumerable()
    //        .Where(l => l.GenreId == genreId)
    //        .Select(l => l.AlbumId)
    //        .Distinct();
    //    var list = _albumRepo.GetAll().AsEnumerable()
    //        .Where(a => albumIds.Contains(a.Id))
    //        .ToList();
    //    _specificAlbums.OnNext(list);
    //}

    //public void SearchAlbumsByName(string query)
    //{
    //    var list = _albumRepo.GetAll().AsEnumerable()
    //        .Where(a => a.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
    //        .ToList();
    //    _specificAlbums.OnNext(list);
    //}

    //public void SearchAlbumsByKeyword(string keyword)
    //{
    //    var list = _albumRepo.GetAll().AsEnumerable()
    //        .Where(a => a.Name!.Contains(keyword, StringComparison.OrdinalIgnoreCase)
    //                 || (a.Description?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false))
    //        .ToList();
    //    _specificAlbums.OnNext(list);
    //}

    //public void GetAlbumsWithNoCoverArt()
    //{
    //    var list = _albumRepo.GetAll().AsEnumerable()
    //        .Where(a => string.IsNullOrEmpty(a.ImagePath))
    //        .ToList();
    //    _specificAlbums.OnNext(list);
    //}

    //public void GetAlbumsWithoutTracks()
    //{
    //    var list = _albumRepo.GetAll().AsEnumerable()
    //        .Where(a => a.NumberOfTracks != 0)
    //        .ToList();
    //    _specificAlbums.OnNext(list);
    //}

    //public void GetAlbumsAddedInLastDays(int days)
    //{
    //    var cutoff = DateTime.Now.AddDays(-days);
    //    var list = _albumRepo.GetAll().AsEnumerable()
    //    .Where(a =>
    //    {
    //        // assuming DateCreated is stored as a string
    //        if (DateTime.TryParse(a.DateCreated, out var dt))
    //            return dt >= cutoff;
    //        return false;
    //    })
    //    .ToList();
    //    _specificAlbums.OnNext(list);
    //}

    //// 2. Sorting & Grouping
    //public List<AlbumModel> SortAlbumsByName(bool ascending = true)
    //{
    //    return ascending
    //            ? [.. _albumRepo.GetAll().AsEnumerable().OrderBy(a => a.Name)]
    //            : [.. _albumRepo.GetAll().AsEnumerable().OrderByDescending(a => a.Name)];
    //}

    //public List<AlbumModel> SortAlbumsByDateAdded(bool ascending = false)
    //{
    //    return ascending
    //            ? [.. _albumRepo.GetAll().AsEnumerable().OrderBy(a => a.DateCreated)]
    //            : [.. _albumRepo.GetAll().AsEnumerable().OrderByDescending(a => a.DateCreated)];
    //}

    //public Dictionary<string, List<AlbumModel>> GroupAlbumsByArtist()
    //{
    //    return _linkRepo.GetAll().AsEnumerable()
    //            .GroupBy(l => l.ArtistId!)
    //            .ToDictionary(
    //                g => g.Key,
    //                g => _albumRepo.GetAll().AsEnumerable()
    //                     .Where(a => g.Select(l => l.AlbumId).Contains(a.Id))
    //                     .ToList()
    //            );
    //}

    //public Dictionary<string, List<AlbumModel>> GroupAlbumsByGenre()
    //{
    //    return _linkRepo.GetAll().AsEnumerable()
    //            .GroupBy(l => l.GenreId!)
    //            .ToDictionary(
    //                g => g.Key,
    //                g => _albumRepo.GetAll().AsEnumerable()
    //                     .Where(a => g.Select(l => l.AlbumId).Contains(a.Id))
    //                     .ToList()
    //            );
    //}

    //public List<AlbumModel> GetAlbumsOrderedByTrackCount(bool ascending = false)
    //{
    //    return ascending
    //            ? [.. _albumRepo.GetAll().AsEnumerable().OrderBy(a => a.NumberOfTracks)]
    //            : [.. _albumRepo.GetAll().AsEnumerable().OrderByDescending(a => a.NumberOfTracks)];
    //}

    //public List<AlbumModel> GetAlbumsOrderedByTotalDuration(bool ascending = false)
    //{
    //    return ascending
    //            ? [.. _albumRepo.GetAll().AsEnumerable().OrderBy(a => a.TotalDuration)]
    //            : [.. _albumRepo.GetAll().AsEnumerable().OrderByDescending(a => a.TotalDuration)];
    //}
    public int GetTotalAlbumCount()
    {
        // This is efficient if your repo's GetAll() is smart or you have a Count() method.
        // If GetAll() loads everything, this is bad.
        // IDEAL: _albumRepo.Count(a => true); or _albumRepo.Count();
        return _albumRepo.GetAll().Count; // Assumes GetAll() is returning a list of frozen objects.
                                          // A dedicated Count method in the repo is better.
    }
    //// 3. Statistics & Insights
    //public int GetTotalAlbumCount()
    //{
    //    return _albumRepo.GetAll().Count;
    //}

    //public Dictionary<string, int> GetAlbumPlayCounts()
    //{
    //    return _pdlRepo.GetAll().AsEnumerable()
    //            .Where(p => p.PlayType == (int)PlayType.Play)
    //            .GroupBy(p => _linkRepo.GetAll().AsEnumerable()
    //                .FirstOrDefault(l => l.SongId == p.SongId)
    //                ?.AlbumId)
    //            .Where(g => g.Key != null)
    //            .ToDictionary(g => g.Key!, g => g.Count());
    //}

    //public Dictionary<string, TimeSpan> GetAlbumTotalListenTime()
    //{
    //    return _pdlRepo.GetAll().AsEnumerable()
    //            .GroupBy(p => _linkRepo.GetAll().AsEnumerable()
    //                .FirstOrDefault(l => l.SongId == p.SongId)
    //                ?.AlbumId)
    //            .Where(g => g.Key != null)
    //            .ToDictionary(
    //                g => g.Key!,
    //                g => TimeSpan.FromSeconds(g.Sum(p => p.PositionInSeconds))
    //            );
    //}

    //public TimeSpan GetTotalLibraryDuration()
    //{
    //    return _albumRepo
    //        .GetAll().AsEnumerable()
    //        .Select(a =>
    //        {
    //            if (double.TryParse(a.TotalDuration, out var secs))
    //                return TimeSpan.FromSeconds(secs);
    //            return TimeSpan.Zero;
    //        })
    //        .Aggregate(TimeSpan.Zero, (sum, span) => sum + span);
    //}

    //public Dictionary<string, int> GetAlbumSkipCounts()
    //{
    //    return _pdlRepo.GetAll().AsEnumerable()
    //            .Where(p => p.PlayType == (int)PlayType.Skipped)
    //            .GroupBy(p => _linkRepo.GetAll().AsEnumerable()
    //                .FirstOrDefault(l => l.SongId == p.SongId)
    //                ?.AlbumId)
    //            .Where(g => g.Key != null)
    //            .ToDictionary(g => g.Key!, g => g.Count());
    //}

    //public Dictionary<string, DateTimeOffset> GetLastPlayedTimestamps()
    //{
    //    return _pdlRepo.GetAll().AsEnumerable()
    //            .GroupBy(p => _linkRepo.GetAll().AsEnumerable()
    //                .FirstOrDefault(l => l.SongId == p.SongId)
    //                ?.AlbumId)
    //            .Where(g => g.Key != null)
    //            .ToDictionary(g => g.Key!, g => g.Max(p => p.DatePlayed));
    //}

    //public List<string> GetMostSkewedAlbums(int topN)
    //{
    //    return [.. GetAlbumSkipCounts()
    //        .OrderByDescending(kv =>
    //            (double)kv.Value /
    //            (GetAlbumPlayCounts().GetValueOrDefault(kv.Key, 1)))
    //        .Take(topN)
    //        .Select(kv => kv.Key)];
    //}

    //// 4. Recommendations & Smart Picks
    //public List<AlbumModel> RecommendSimilarAlbums(string albumId, int count = 5)
    //{
    //    var targetGenres = _linkRepo.GetAll().AsEnumerable()
    //        .Where(l => l.AlbumId == albumId)
    //        .Select(l => l.GenreId)
    //        .Distinct();
    //    var targetArtists = _linkRepo.GetAll().AsEnumerable()
    //        .Where(l => l.AlbumId == albumId)
    //        .Select(l => l.ArtistId)
    //        .Distinct();

    //    return [.. _albumRepo.GetAll().AsEnumerable()
    //        .Where(a => a.Id != albumId)
    //        .Select(a => new
    //        {
    //            Album = a,
    //            Score = _linkRepo.GetAll().AsEnumerable().Count(l =>
    //                l.AlbumId == a.Id
    //                && (targetGenres.Contains(l.GenreId)
    //                 || targetArtists.Contains(l.ArtistId)))
    //        })
    //        .Where(x => x.Score > 0)
    //        .OrderByDescending(x => x.Score)
    //        .Take(count)
    //        .Select(x => x.Album)];
    //}

    //public List<AlbumModel> GetRecentlyPlayedAlbums(int count)
    //{
    //    return GetLastPlayedTimestamps()
    //            .OrderByDescending(kv => kv.Value)
    //            .Take(count)
    //            .Select(kv => _albumRepo.GetById(kv.Key))
    //            .Where(a => a != null)
    //            .ToList()!;
    //}

    //public List<AlbumModel> GetAlbumsNotPlayedSince(DateTime cutoff)
    //{
    //    return GetLastPlayedTimestamps()
    //            .Where(kv => kv.Value < cutoff)
    //            .Select(kv => _albumRepo.GetById(kv.Key))
    //            .Where(a => a != null)
    //            .ToList()!;
    //}

    //public List<AlbumModel> GetTopAlbumsByPlayCount(int count)
    //{
    //    return GetAlbumPlayCounts()
    //            .OrderByDescending(kv => kv.Value)
    //            .Take(count)
    //            .Select(kv => _albumRepo.GetById(kv.Key))
    //            .Where(a => a != null)
    //            .ToList()!;
    //}

    //public List<AlbumModel> GetUnderratedAlbums(int minPlays, int maxPlays)
    //{
    //    return GetAlbumPlayCounts()
    //            .Where(kv => kv.Value >= minPlays && kv.Value <= maxPlays)
    //            .Select(kv => _albumRepo.GetById(kv.Key))
    //            .Where(a => a != null)
    //            .ToList()!;
    //}

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

    //public bool ValidateAlbumMetadata(string albumId)
    //{
    //    var album = _albumRepo.GetById(albumId);
    //    return album != null
    //        && !string.IsNullOrEmpty(album.Name)
    //        && _linkRepo.GetAll().AsEnumerable().Any(l => l.AlbumId == albumId);
    //}

    ////public async Task EnrichAlbumWithWebInfoAsync(string albumId)
    ////{
    ////    var info = await _remoteService.GetAlbumInfoAsync(albumId);
    ////    var album = _albumRepo.GetById(albumId);
    ////    if (album != null)
    ////    {
    ////        album.Description = info.Description;
    ////        album.TotalDuration = info.TotalDuration;
    ////        _albumRepo.AddOrUpdate(album);
    ////    }
    ////}

    //// 6. Import/Export & Sync
    //public async Task ExportAlbumsToCsvAsync(string filePath)
    //{
    //    var sb = new StringBuilder();
    //    sb.AppendLine("Id,Name,ArtistCount,TrackCount,Duration");
    //    foreach (var a in _albumRepo.GetAll().AsEnumerable())
    //        sb.AppendLine($"{a.Id},{a.Name},"
    //            + $"{_linkRepo.GetAll().AsEnumerable().Count(l => l.AlbumId==a.Id)},"
    //            + $"{a.NumberOfTracks},{a.TotalDuration}");
    //    await File.WriteAllTextAsync(filePath, sb.ToString());
    //}

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
                Name = parts[1],
                NumberOfTracks = int.Parse(parts[3]),
                TotalDuration = TimeSpan.FromSeconds(double.Parse(parts[4])).ToString()
            };
            _albumRepo.AddOrUpdate(album);
        }
    }
}