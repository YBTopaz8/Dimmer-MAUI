namespace Dimmer.Interfaces.Services.Interfaces.FileProcessing;

public class MusicMetadataService : IMusicMetadataService
{
    private readonly Dictionary<string, ArtistModelView> _artistsByName = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, AlbumModelView> _albumsByName = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, GenreModelView> _genresByName = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _existingFilePaths = new(StringComparer.OrdinalIgnoreCase);


    public List<ArtistModelView> NewArtists { get; } = new();
    public List<AlbumModelView> NewAlbums { get; } = new();
    public List<GenreModelView> NewGenres { get; } = new();


    public MusicMetadataService() { }


    public void LoadExistingData(
        IEnumerable<ArtistModelView> existingArtists,
        IEnumerable<AlbumModelView> existingAlbums,
        IEnumerable<GenreModelView> existingGenres,
        IEnumerable<SongModelView> existingSongs)
    {
        foreach (var artist in existingArtists)
        {

            if (!string.IsNullOrEmpty(artist.Name))
            {
                _artistsByName.TryAdd(artist.Name, artist);
            }
        }
        foreach (var album in existingAlbums)
        {
            if (!string.IsNullOrEmpty(album.Name))
            {
                _albumsByName.TryAdd(album.Name, album);
            }
        }
        foreach (var genre in existingGenres)
        {
            if (!string.IsNullOrEmpty(genre.Name))
            {
                _genresByName.TryAdd(genre.Name, genre);
            }
        }
        foreach (var song in existingSongs)
        {
            _existingSongKeys.Add(song.TitleDurationKey);
        }
        _processedSongsInThisScan.AddRange(existingSongs);

        foreach (var song in existingSongs)
        {
            if (!string.IsNullOrWhiteSpace(song.FilePath))
            {
                _existingFilePaths.Add(song.FilePath);
            }
        }
    }
    private readonly List<SongModelView> _processedSongsInThisScan = new();

    private readonly HashSet<string> _existingSongKeys = new();

    public ArtistModelView GetOrCreateArtist(Track track, string name)
    {
        name = string.IsNullOrWhiteSpace(name) ? "Unknown Artist" : name.Trim();
        if (!_artistsByName.TryGetValue(name, out var artist))
        {
            artist = new ArtistModelView { Name = name, Id=ObjectId.GenerateNewId() };
            _artistsByName[name] = artist;
            NewArtists.Add(artist);
        }


        return artist;
    }

    public AlbumModelView GetOrCreateAlbum(Track track, string name, string? initialCoverPath = null)
    {
        name = string.IsNullOrWhiteSpace(name) ? "Unknown Album" : name.Trim();
        if (!_albumsByName.TryGetValue(name, out var album))
        {
            album = new AlbumModelView
            {
                Name = name,
                Id=ObjectId.GenerateNewId(),
                //ImagePath = initialCoverPath,


            };

            _albumsByName[name] = album;
            NewAlbums.Add(album);
        }
        else if (album.ImagePath == null && initialCoverPath != null)
        {

            album.ImagePath = initialCoverPath;

        }

        return album;
    }

    public GenreModelView GetOrCreateGenre(Track track, string name)
    {
        name = string.IsNullOrWhiteSpace(name) ? "Unknown Genre" : name.Trim();
        if (!_genresByName.TryGetValue(name, out var genre))
        {
            genre = new GenreModelView
            {
                Id=ObjectId.GenerateNewId(),
                Name = name,

            };
            _genresByName[name] = genre;
            NewGenres.Add(genre);
        }
        return genre;
    }

    public void AddSong(SongModelView song)
    {
        _processedSongsInThisScan.Add(song);
    }


    // Add a way to track updates
    private readonly HashSet<ObjectId> _updatedEntityIds = new();
    public bool DoesSongExist(string title, double durationInSeconds)
    {
        // We now check against the set of keys from songs that existed BEFORE the scan started.
        string keyToCheck = $"{title.ToLowerInvariant().Trim()}|{durationInSeconds}";
        return _existingSongKeys.Contains(keyToCheck);
    }
    public bool DoesSongExist(string title, int durationInSeconds, out SongModelView? existingSong)
    {
        string keyToCheck = $"{title.ToLowerInvariant().Trim()}|{durationInSeconds}";
        existingSong = _processedSongsInThisScan.FirstOrDefault(s => s.TitleDurationKey == keyToCheck);
        return existingSong != null;
    }

    // Method to mark an entity as updated
    public void MarkAsUpdated(SongModelView song)
    {
        _updatedEntityIds.Add(song.Id);
    }


    public IReadOnlyList<ArtistModelView> GetAllArtists()
    {
        return [.. _artistsByName.Values];
    }

    public IReadOnlyList<AlbumModelView> GetAllAlbums()
    {
        return [.. _albumsByName.Values];
    }

    public IReadOnlyList<GenreModelView> GetAllGenres()
    {
        return [.. _genresByName.Values];
    }

    public IReadOnlyList<SongModelView> GetProcessedSongs()
    {
        return [.. _processedSongsInThisScan];
    }
    public bool HasFileBeenProcessed(string filePath)
    {
        return _existingFilePaths.Contains(filePath);
    }
}