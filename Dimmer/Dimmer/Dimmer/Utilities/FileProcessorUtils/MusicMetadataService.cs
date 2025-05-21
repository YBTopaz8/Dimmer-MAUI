namespace Dimmer.Utilities.FileProcessorUtils;

public class MusicMetadataService : IMusicMetadataService
{
    private readonly Dictionary<string, ArtistModel> _artistsByName = new(System.StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, AlbumModel> _albumsByName = new(System.StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, GenreModel> _genresByName = new(System.StringComparer.OrdinalIgnoreCase);
    private readonly List<SongModel> _songs = new(); // Or a dictionary for faster lookups if needed

    // For tracking newly created entities during a session, if needed for persistence
    public List<ArtistModel> NewArtists { get; } = new();
    public List<AlbumModel> NewAlbums { get; } = new();
    public List<GenreModel> NewGenres { get; } = new();


    public MusicMetadataService() { }

    // Call this if you have pre-existing data to load (e.g., from a database)
    public void LoadExistingData(
        IEnumerable<ArtistModel> existingArtists,
        IEnumerable<AlbumModel> existingAlbums,
        IEnumerable<GenreModel> existingGenres,
        IEnumerable<SongModel> existingSongs)
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
        _songs.AddRange(existingSongs);
    }


    public ArtistModel GetOrCreateArtist(string name)
    {
        name = string.IsNullOrWhiteSpace(name) ? "Unknown Artist" : name.Trim();
        if (!_artistsByName.TryGetValue(name, out var artist))
        {
            artist = new ArtistModel { Name = name, };
            _artistsByName[name] = artist;
            NewArtists.Add(artist);
        }
        return artist;
    }

    public AlbumModel GetOrCreateAlbum(string name, string? initialCoverPath = null)
    {
        name = string.IsNullOrWhiteSpace(name) ? "Unknown Album" : name.Trim();
        if (!_albumsByName.TryGetValue(name, out var album))
        {
            album = new AlbumModel { 
                Name = name, 
                ImagePath = initialCoverPath,
            };
            _albumsByName[name] = album;
            NewAlbums.Add(album);
        }
        else if (album.ImagePath == null && initialCoverPath != null)
        {
            // Update existing album if it was missing a cover and now we have one
            album.ImagePath = initialCoverPath;
            
        }
        
        return album;
    }

    public GenreModel GetOrCreateGenre(string name)
    {
        name = string.IsNullOrWhiteSpace(name) ? "Unknown Genre" : name.Trim();
        if (!_genresByName.TryGetValue(name, out var genre))
        {
            genre = new GenreModel { Name = name };
            _genresByName[name] = genre;
            NewGenres.Add(genre);
        }
        return genre;
    }

    public void AddSong(SongModel song)
    {
        _songs.Add(song);
    }

    public bool DoesSongExist(string title, int durationInSeconds)
    {
        // Simple duplicate check, can be made more robust
        return _songs.Any(s =>
            s.Title.Equals(title, System.StringComparison.OrdinalIgnoreCase) &&
            s.DurationInSeconds == durationInSeconds);
    }

    public IReadOnlyList<ArtistModel> GetAllArtists() => _artistsByName.Values.ToList();
    public IReadOnlyList<AlbumModel> GetAllAlbums() => _albumsByName.Values.ToList();
    public IReadOnlyList<GenreModel> GetAllGenres() => _genresByName.Values.ToList();
    public IReadOnlyList<SongModel> GetAllSongs() => _songs.ToList();
}