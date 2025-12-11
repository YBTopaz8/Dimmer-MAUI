using System.Collections.Concurrent;

namespace Dimmer.Interfaces.Services.Interfaces.FileProcessing;

public class MusicMetadataService : IMusicMetadataService
{
    private readonly ConcurrentDictionary<string, ArtistModelView> _artistsByName = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, AlbumModelView> _albumsByName = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, GenreModelView> _genresByName = new(StringComparer.OrdinalIgnoreCase); private readonly HashSet<string> _existingFilePaths = new(StringComparer.OrdinalIgnoreCase);


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

        // GetOrAdd is an atomic operation, making this method thread-safe.
        return _artistsByName.GetOrAdd(name, (key) => {
            var newArtist = new ArtistModelView { Name = key, Id = ObjectId.GenerateNewId() 
            };
            newArtist.TotalSongsByArtist++;
            NewArtists.Add(newArtist); // Note: NewArtists list might still need locking if accessed elsewhere during the scan.
            return newArtist;
        });
    }

    public AlbumModelView GetOrCreateAlbum(Track track, string name, string? artistForContext = null)
    {
        name = string.IsNullOrWhiteSpace(name) ? "Unknown Album" : name.Trim();

        // A unique key for an album is often its name + the primary artist's name
        string albumKey = $"{name}|{artistForContext ?? "Unknown"}";

        return _albumsByName.GetOrAdd(albumKey, (Func<string, AlbumModelView>)((key) => {
            var newAlbum = new AlbumModelView
            {
                Name = name,
                Id = ObjectId.GenerateNewId(),
            };
            NewAlbums.Add(newAlbum);
            return newAlbum;
        }));
    }

    public GenreModelView GetOrCreateGenre(Track track, string name)
    {
        name = string.IsNullOrWhiteSpace(name) ? "Unknown Genre" : name.Trim();

        return _genresByName.GetOrAdd(name, (key) => {
            var newGenre = new GenreModelView { Name = key, Id = ObjectId.GenerateNewId() };
            NewGenres.Add(newGenre);
            return newGenre;
        });
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

    public void ClearAll()
    {
       
            // Clear all dictionaries and lists to free up memory
            _artistsByName.Clear();
            _albumsByName.Clear();
            _genresByName.Clear();
            _existingFilePaths.Clear();
            NewArtists.Clear();
            NewAlbums.Clear();
            NewGenres.Clear();
            _processedSongsInThisScan.Clear();
            _existingSongKeys.Clear();
            _updatedEntityIds.Clear();


        }

    }