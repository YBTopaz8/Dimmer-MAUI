using Dimmer.Interfaces.Services.Interfaces;

namespace Dimmer.Orchestration;
public class AlbumsMgtFlow : IDisposable
{
    private readonly IDimmerStateService state;
    private readonly IRepository<SongModel> _songRepo;
    private readonly IRepository<UserModel> userRepo;
    private readonly IRepository<GenreModel> genreRepo;
    private readonly IRepository<AlbumModel> _albumRepo;
    private readonly IRepository<AppStateModel> appstateRepo;
    private readonly ISettingsService settings;
    private readonly IFolderMgtService folderMonitor;
    private readonly IRepository<DimmerPlayEvent> _pdlRepo;
    private readonly IRepository<PlaylistModel> playlistRepo;
    private readonly IRepository<ArtistModel> _artistRepo;
    private readonly SubscriptionManager _subs;


    //public IObservable<List<AlbumModel>> SpecificAlbums => _queriedAlbums.AsObservable();
    public IObservable<IReadOnlyList<SongModel>> SpecificSongs => _queriedSongs.AsObservable();
    //public IObservable<List<GenreModel>> SpecificGenre => _specificGenres.AsObservable();
    public IObservable<IReadOnlyList<AlbumModel>> SpecificAlbums => _queriedAlbums.AsObservable();

    private readonly BehaviorSubject<double> _syncProgress = new(0);
    public IObservable<double> SyncProgress => _syncProgress.AsObservable();
    private readonly BehaviorSubject<IReadOnlyList<AlbumModel>> _queriedAlbums = new(Array.Empty<AlbumModel>());
    private readonly BehaviorSubject<IReadOnlyList<SongModel>> _queriedSongs = new(Array.Empty<SongModel>());

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

    )
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
        _subs          = subs;
    }
    public void GetAlbumsByArtistName(string artistName)
    {
        if (string.IsNullOrWhiteSpace(artistName))
        {
            _queriedAlbums.OnNext(Enumerable.Empty<AlbumModel>().ToList());
            return;
        }

        // Step 1: Find the artist(s) by name. Realm supports string equality.
        // Case-sensitive by default. If you need case-insensitive, Realm's RQL `CONTAINS[c]` or `==[c]`
        // is not directly exposed via simple LINQ equality.
        // For case-insensitive, you might need to fetch and filter or store normalized names.
        // Assuming case-sensitive for now:
        var artists = _artistRepo.Query(ar => ar.Name == artistName);
        if (artists.Count==0)
        {
            _queriedAlbums.OnNext(Enumerable.Empty<AlbumModel>().ToList());
            return;
        }
        var artistIds = artists.Select(ar => ar.Id).ToList();

        // Step 2: Find songs by these artist IDs.
        // If SongModel.ArtistIds is IList<ObjectId>:
        // This requires iterating through artistIds or fetching all songs and filtering in memory.
        // Realm LINQ does not directly support `artistIds.Contains(s.PrimaryArtistId)` or similar for sub-queries.
        // Let's fetch songs that have *any* of these artists.

        // Strategy: Fetch all songs, then filter in memory if SongModel.Artists is IList<ArtistModel>
        // If SongModel.ArtistIds is IList<ObjectId>, we can do better with multiple OR queries if needed,
        // or fetch all and filter.
        // For simplicity here, assuming SongModel.ArtistIds is a list of ObjectId that represent artist PKs.

        var allSongs = _songRepo.GetAll(); // Potentially large, use with caution.
                                           // A better model or more complex querying strategy might be needed for performance.

        // In-memory filtering
        var songsByArtists = allSongs
            .Where(s => s.ArtistIds != null && s.ArtistIds.Any(songArtistId => artistIds.Contains(songArtistId.Id)))
            .ToList();

        if (songsByArtists.Count==0)
        {
            _queriedAlbums.OnNext(Enumerable.Empty<AlbumModel>().ToList());
            return;
        }

        // Step 3: Get distinct albums from these songs
        var albums = songsByArtists
            .Where(s => s.Album != null) // Ensure Album object is linked
            .Select(s => s.Album)
            .DistinctBy(al => al!.Id) // Requires .NET 6+ for DistinctBy. Album is not null here.
            .ToList();

        _queriedAlbums.OnNext(albums!); // albums will not be null here
    }

    // 1. Filtering & Searching
    public void GetAlbumsByReleaseYearRange(int startYear, int endYear)
    {
        // Use the Query method from your repository
        var list = _albumRepo.Query(a => a.ReleaseYear >= startYear && a.ReleaseYear <= endYear);
        _queriedAlbums.OnNext(list);
    }
    public void GetAlbumsByArtistId(ObjectId artistId) // Changed parameter to ObjectId for directness
    {
        // This LINQ query should be translatable by Realm if ArtistIds is a list of primitives (ObjectId)
        // and Realm supports .Any() on primitive lists with an equality check.
        // This is a common pattern that Realm *often* supports.
        List<SongModel> songsOfArtist;

        songsOfArtist = _songRepo.Query(s => s.ArtistIds.Any(id => id.Id == artistId));


        if (songsOfArtist.Count==0)
        {
            _queriedAlbums.OnNext(Enumerable.Empty<AlbumModel>().ToList());
            return;
        }

        var albums = songsOfArtist
            .Where(s => s.Album != null)
            .Select(s => s.Album)
            .DistinctBy(al => al!.Id)
            .ToList();
        _queriedAlbums.OnNext(albums!);

    }
    public void GetSongsByGenreId(ObjectId genreId) // Changed to ID for directness
    {
        var songs = _songRepo.Query(s => s.Genre != null && s.Genre.Id == genreId);
        _queriedSongs.OnNext(songs);
    }
    public void GetAlbumsByArtistName_Alternative(string artistName)
    {
        var artist = _artistRepo.Query(ar => ar.Name == artistName).FirstOrDefault();
        if (artist == null)
        {
            _queriedAlbums.OnNext(Array.Empty<AlbumModel>());
            return;
        }


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
        var genre = genreRepo.Query(g => g.Name == genreName).FirstOrDefault();
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
        if (string.IsNullOrWhiteSpace(query))
        {
            _queriedAlbums.OnNext(_albumRepo.GetAll().ToList()); // Or Array.Empty if that's preferred for empty query
            return;
        }

        try
        {
            var list = _albumRepo.Query(a => a.Name != null && a.Name.Contains(query));
            _queriedAlbums.OnNext(list);
        }
        catch (NotSupportedException ex)
        {
            Debug.WriteLine($"Realm Query for Name.Contains was not supported: {ex.Message}. Falling back to in-memory filter.");
            var allAlbums = _albumRepo.GetAll();
            var filteredList = allAlbums
                .Where(a => a.Name != null && a.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                .ToList();
            _queriedAlbums.OnNext(filteredList);
        }
    }

    public void GetAlbumsWithNoCoverArt()
    {
        var list = _albumRepo.Query(a => string.IsNullOrEmpty(a.ImagePath));
        _queriedAlbums.OnNext(list);
    }

    public void GetAlbumsWithoutAnySongs()
    {
        var list = _albumRepo.Query(a => !a.SongsInAlbum.Any());
        _queriedAlbums.OnNext(list);
    }

    public void GetAlbumsAddedInLastDays(int days)
    {
        var cutoff = DateTimeOffset.UtcNow.AddDays(-days);
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
            : [.. allAlbums.OrderByDescending(a => a.Name, StringComparer.OrdinalIgnoreCase)];
        _queriedAlbums.OnNext(sortedList);
    }



    public void GetAlbumsBySongId(ObjectId songId)
    {
        throw new NotImplementedException("This method is not implemented yet.");
    }

    public int GetTotalAlbumCount()
    {
        // This is efficient if your repo's GetAll() is smart or you have a Count() method.
        // If GetAll() loads everything, this is bad.
        // IDEAL: _albumRepo.Count(a => true); or _albumRepo.Count();
        return _albumRepo.Count(a => true); // Assumes GetAll() is returning a list of frozen objects.
                                            // A dedicated Count method in the repo is better.
    }


    public async Task ExportAlbumsToJsonAsync(string filePath)
    {
        // GetAll() returns frozen objects, safe for serialization.
        var albums = _albumRepo.GetAll();
        // Ensure your AlbumModel (and any nested RealmObjects it references) are serializable by System.Text.Json.
        // You might need [JsonIgnore] for backlinks or Realm-specific properties if they cause issues.
        var json = JsonSerializer.Serialize(albums, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(filePath, json);
        _syncProgress.OnNext(1.0); // Indicate completion
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
    public new void Dispose()
    {
        _subs.Dispose(); // Assuming SubscriptionManager is IDisposable and disposes all its subs
        _queriedAlbums.Dispose();
        _queriedAlbums.Dispose();
        _queriedSongs.Dispose();
        _syncProgress.Dispose();
    }
}