using ATL;

namespace Dimmer.Utilities.FileProcessorUtils;

public class MusicMetadataService : IMusicMetadataService
{
    private readonly Dictionary<string, ArtistModel> _artistsByName = new(System.StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, AlbumModel> _albumsByName = new(System.StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, GenreModel> _genresByName = new(System.StringComparer.OrdinalIgnoreCase);
    private readonly List<SongModel> _songs = new();


    public List<ArtistModel> NewArtists { get; } = new();
    public List<AlbumModel> NewAlbums { get; } = new();
    public List<GenreModel> NewGenres { get; } = new();


    public MusicMetadataService() { }


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


    public ArtistModel GetOrCreateArtist(Track track, string name)
    {
        name = string.IsNullOrWhiteSpace(name) ? "Unknown Artist" : name.Trim();
        if (!_artistsByName.TryGetValue(name, out var artist))
        {
            artist = new ArtistModel { Name = name, Id=ObjectId.GenerateNewId(), IsNew=true };
            _artistsByName[name] = artist;
            NewArtists.Add(artist);
        }


        return artist;
    }

    public AlbumModel GetOrCreateAlbum(Track track, string name, string? initialCoverPath = null)
    {
        name = string.IsNullOrWhiteSpace(name) ? "Unknown Album" : name.Trim();
        if (!_albumsByName.TryGetValue(name, out var album))
        {
            album = new AlbumModel
            {
                Name = name,
                Id=ObjectId.GenerateNewId(),
                //ImagePath = initialCoverPath,
                IsNew=true,


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

    public GenreModel GetOrCreateGenre(Track track, string name)
    {
        name = string.IsNullOrWhiteSpace(name) ? "Unknown Genre" : name.Trim();
        if (!_genresByName.TryGetValue(name, out var genre))
        {
            genre = new GenreModel
            {
                Id=ObjectId.GenerateNewId(),
                Name = name,
                IsNew = true,

            };
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

        if (_songs == null)
        {

            System.Diagnostics.Debug.WriteLine("Error: _songs collection is null in DoesSongExist.");
            return false;
        }

        title??=string.Empty;

        bool exists = _songs.Any(s =>
            s != null &&
            s.Title != null &&
            s.Title.Equals(title, System.StringComparison.OrdinalIgnoreCase) &&
            s.DurationInSeconds == durationInSeconds);

        return exists;
    }

    public IReadOnlyList<ArtistModel> GetAllArtists()
    {
        return [.. _artistsByName.Values];
    }

    public IReadOnlyList<AlbumModel> GetAllAlbums()
    {
        return [.. _albumsByName.Values];
    }

    public IReadOnlyList<GenreModel> GetAllGenres()
    {
        return [.. _genresByName.Values];
    }

    public IReadOnlyList<SongModel> GetAllSongs()
    {
        return [.. _songs];
    }
}