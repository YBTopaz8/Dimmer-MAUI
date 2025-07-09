using ATL;

namespace Dimmer.Utilities.FileProcessorUtils;

public class MusicMetadataService : IMusicMetadataService
{
    private readonly Dictionary<string, ArtistModelView> _artistsByName = new(System.StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, AlbumModelView> _albumsByName = new(System.StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, GenreModelView> _genresByName = new(System.StringComparer.OrdinalIgnoreCase);
    private readonly List<SongModelView> _songs = new();


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
        _songs.AddRange(existingSongs);
    }


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
        _songs.Add(song);
    }

    public bool DoesSongExist(string title, int durationInSeconds)
    {
        if (string.IsNullOrWhiteSpace(title))
            return false;

        // Create the key exactly as the model would.
        string keyToCheck = $"{title.ToLowerInvariant().Trim()}|{durationInSeconds}";

        // This check is now extremely fast because TitleDurationKey is indexed.
        // We are checking against the in-memory list of songs processed *so far in this scan*.
        return _songs.Any(s => s.TitleDurationKey == keyToCheck);
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

    public IReadOnlyList<SongModelView> GetAllSongs()
    {
        return [.. _songs];
    }
}