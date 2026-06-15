using System.Collections.Concurrent;
using LTrack = ATL.Track;

namespace Dimmer.Interfaces.Services.Interfaces.FileProcessing;

public class MusicMetadataService : IMusicMetadataService
{
    private readonly ConcurrentDictionary<string, ArtistModelView> _artistsByName = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, AlbumModelView> _albumsByName = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, GenreModelView> _genresByName = new(StringComparer.OrdinalIgnoreCase);

    public ConcurrentBag<ArtistModelView> NewArtists { get; } = new();
    public ConcurrentBag<AlbumModelView> NewAlbums { get; } = new();
    public ConcurrentBag<GenreModelView> NewGenres { get; } = new();

    private readonly HashSet<string> _existingFilePaths = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _existingSongKeys = new();
    private readonly List<SongModelView> _processedSongsInThisScan = new();

    // CENTRALIZED PATH NORMALIZER
    public static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return string.Empty;
        try
        {
            return Path.GetFullPath(path).Trim().ToLowerInvariant().Replace('/', '\\');
        }
        catch
        {
            return path.Trim().ToLowerInvariant().Replace('/', '\\');
        }
    }

    public void LoadExistingData(
        IEnumerable<ArtistModelView> existingArtists,
        IEnumerable<AlbumModelView> existingAlbums,
        IEnumerable<GenreModelView> existingGenres,
        IEnumerable<SongModelView> existingSongs)
    {
        foreach (var artist in existingArtists)
        {
            if (!string.IsNullOrEmpty(artist.Name))
                _artistsByName.TryAdd(artist.Name, artist);
        }

        foreach (var album in existingAlbums)
        {
            if (!string.IsNullOrEmpty(album.Name))
            {
                var artistName = album.Artists?.FirstOrDefault()?.Name ?? "Unknown";
                string albumKey = $"{album.Name.Trim()}|{artistName.Trim()}";
                _albumsByName.TryAdd(albumKey, album);
            }
        }

        foreach (var genre in existingGenres)
        {
            if (!string.IsNullOrEmpty(genre.Name))
                _genresByName.TryAdd(genre.Name, genre);
        }

        foreach (var song in existingSongs)
        {
            _existingSongKeys.Add(song.TitleDurationKey);
            if (!string.IsNullOrWhiteSpace(song.FilePath))
                _existingFilePaths.Add(NormalizePath(song.FilePath)); // Normalize path!
        }
        _processedSongsInThisScan.AddRange(existingSongs);
    }

    public ArtistModelView GetOrCreateArtist(LTrack track, string name)
    {
        name = string.IsNullOrWhiteSpace(name) ? "Unknown Artist" : name.Trim();

        var artist = _artistsByName.GetOrAdd(name, (key) =>
        {
            var newArtist = new ArtistModelView
            {
                Name = key,
                Id = ObjectId.GenerateNewId()
            };
            NewArtists.Add(newArtist);
            return newArtist;
        });

        lock (artist)
        {
            artist.TotalSongsByArtist++;
        }
        return artist;
    }

    public AlbumModelView GetOrCreateAlbum(LTrack track, string name, string? artistForContext = null)
    {
        name = string.IsNullOrWhiteSpace(name) ? "Unknown Album" : name.Trim();
        string artistName = (artistForContext ?? "Unknown").Trim();
        string albumKey = $"{name}|{artistName}";

        return _albumsByName.GetOrAdd(albumKey, (key) =>
        {
            var newAlbum = new AlbumModelView
            {
                Name = name,
                Id = ObjectId.GenerateNewId(),
            };
            NewAlbums.Add(newAlbum);
            return newAlbum;
        });
    }

    public GenreModelView GetOrCreateGenre(LTrack track, string name)
    {
        name = string.IsNullOrWhiteSpace(name) ? "Unknown Genre" : name.Trim();

        return _genresByName.GetOrAdd(name, (key) => {
            var newGenre = new GenreModelView
            {
                Name = key,
                Id = ObjectId.GenerateNewId()
            };
            NewGenres.Add(newGenre);
            return newGenre;
        });
    }

    public void AddSong(SongModelView song)
    {
        _processedSongsInThisScan.Add(song);
    }

    private readonly HashSet<ObjectId> _updatedEntityIds = new();

    public bool DoesSongExist(string title, double durationInSeconds)
    {
        // FIX: Cast durationInSeconds to (int) to match TitleDurationKey rules!
        string keyToCheck = $"{title.ToLowerInvariant().Trim()}|{(int)durationInSeconds}";
        return _existingSongKeys.Contains(keyToCheck);
    }

    public bool DoesSongExist(string title, int durationInSeconds, out SongModelView? existingSong)
    {
        string keyToCheck = $"{title.ToLowerInvariant().Trim()}|{durationInSeconds}";
        existingSong = _processedSongsInThisScan.FirstOrDefault(s => s.TitleDurationKey == keyToCheck);
        return existingSong != null;
    }

    public void MarkAsUpdated(SongModelView song)
    {
        _updatedEntityIds.Add(song.Id);
    }

    public IReadOnlyList<ArtistModelView> GetAllArtists() => [.. _artistsByName.Values];
    public IReadOnlyList<AlbumModelView> GetAllAlbums() => [.. _albumsByName.Values];
    public IReadOnlyList<GenreModelView> GetAllGenres() => [.. _genresByName.Values];
    public IReadOnlyList<SongModelView> GetProcessedSongs() => [.. _processedSongsInThisScan];

    public bool HasFileBeenProcessed(string filePath)
    {
        // Normalize search path
        return _existingFilePaths.Contains(NormalizePath(filePath));
    }

    public void ClearAll()
    {
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
        while (NewArtists.TryTake(out _)) ;
        while (NewAlbums.TryTake(out _)) ;
        while (NewGenres.TryTake(out _)) ;
    }
}