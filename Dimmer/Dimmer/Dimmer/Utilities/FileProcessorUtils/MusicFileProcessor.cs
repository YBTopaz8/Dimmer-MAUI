using ATL;

namespace Dimmer.Utilities.FileProcessorUtils;

public static class MusicFileProcessor
{
    // Generates a local device ID using the first three characters of CallerClass and a new GUID.
    public static string GenerateLocalDeviceID(string callerClass)
    {
        if (string.IsNullOrWhiteSpace(callerClass) || callerClass.Length < 3)
            throw new ArgumentException("CallerClass must be at least 3 characters long", nameof(callerClass));
        return $"{callerClass[..3]}{Guid.NewGuid()}";
    }

    // Check if the file exists and its size is over 1000 bytes.
    public static bool IsValidFile(string file)
    {
        if (!File.Exists(file))
            return false;
        FileInfo fileInfo = new FileInfo(file);
        return fileInfo.Length > 1000;
    }

    // Global method to sanitize and extract unique artist names.
    public static List<string> SanitizeArtistNames(string? artist, string? albumArtist)
    {
        HashSet<string> names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        names.UnionWith(ParseNames(artist, removeAmpersand: true));
        names.UnionWith(ParseNames(albumArtist));
        return [.. names];
    }

    // Helper to replace common delimiters and split the names.
    private static IEnumerable<string> ParseNames(string? input, bool removeAmpersand = false)
    {
        if (string.IsNullOrWhiteSpace(input))
            return [];

        if (removeAmpersand)
            input = input.Replace("&", string.Empty);

        // Replace common delimiters to standardize splits.
        input = input.Replace(" x ", ",")
                     .Replace(" X ", ",")
                     .Replace(" feat. ", ",")
                     .Replace(" featuring ", ",");

        return input.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(name => name.Trim())
                    .Where(name => !string.IsNullOrWhiteSpace(name));
    }

    // Use the global method in your processing.
    public static List<string> GetArtistNames(string? artist, string? albumArtist)
    {
        return SanitizeArtistNames(artist, albumArtist);
    }

    // Process a file and generate a SongModel from it.
    public static (SongModel? song, string message) ProcessFile(
        string file,
        List<AlbumModel> existingAlbums,
        Dictionary<string, AlbumModel> albumDict,
        List<AlbumModel> newAlbums,
        List<SongModel> oldSongs,
        List<ArtistModel> newArtists,
        Dictionary<string, ArtistModel> artistDict,
        List<AlbumArtistGenreSongLink> newLinks,
        List<AlbumArtistGenreSongLink> existingLinks,
        List<ArtistModel> existingArtists,
        List<GenreModel> newGenres,
        Dictionary<string, GenreModel> genreDict,
        List<GenreModel> existingGenres)
    {
        // Load the track metadata.
        Track track;
        try
        {
            track = new Track(file);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error creating Track for file {file}: {ex.Message}");
            return (null, $"Error creating Track for file {file}: {ex.Message}");
        }

        // Determine title and album.
        string title = track.Title.Contains(';') ? track.Title.Split(';')[0].Trim() : track.Title;
        string albumName = string.IsNullOrWhiteSpace(track.Album) ? track.Title : track.Album.Trim();

        // Get or create the album.
        AlbumModel album = GetOrCreateAlbum(albumName, existingAlbums, albumDict, newAlbums);

        // Extract artist names using the global method.
        List<string> artistNames = GetArtistNames(track.Artist, track.AlbumArtist);
        string mainArtistName = string.Join(", ", artistNames);

        // Create the song model.
        SongModel song = CreateSongModel(track, title, albumName, mainArtistName, file);

        // Update album image.
        if (!string.IsNullOrWhiteSpace(albumName))
            album.ImagePath = song.CoverImagePath;

        // Skip if the song already exists.
        if (oldSongs.Any(s => s.Title.Equals(title, StringComparison.OrdinalIgnoreCase) &&
                              s.DurationInSeconds == track.Duration))
        {
            Debug.WriteLine($"Skipping existing song for artist: {song.ArtistName}");
            return (null, $"Skipping existing song for artist: {song.ArtistName}");
        }

        // Process genre.
        string genreName = string.IsNullOrWhiteSpace(track.Genre) ? "Unknown Genre" : track.Genre.Trim();
        GenreModel genre = GetOrCreateGenre(genreName, genreDict, newGenres, existingGenres);

        // Process artists and create links.
        foreach (string artistName in artistNames)
        {
            ArtistModel artist = GetOrCreateArtist(artistName, artistDict, newArtists, existingArtists);
            CreateLinks(artist, album, song, genre, newLinks, existingLinks);
        }

        return (song, $"Track is {song.Title}");
    }

    // Get all audio files from a list of folder paths.
    public static List<string> GetAllFiles(List<string> folderPaths)
    {
        List<string> allFiles = new List<string>();
        int countOfScanningErrors = 0;
        HashSet<string> supportedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".mp3", ".flac", ".wav", ".m4a"
        };

        foreach (string? path in folderPaths.Distinct())
        {
            if (string.IsNullOrWhiteSpace(path))
                continue;

            try
            {
                if (Directory.Exists(path))
                {
                    //List<string> files = [.. Directory.GetFiles(path, "*.*", SearchOption.AllDirectories)];
                    List<string> files = [.. Directory.GetFiles(path, "*.*", SearchOption.AllDirectories).Where(s => supportedExtensions.Contains(Path.GetExtension(s)))];
                    allFiles.AddRange(files);
                }
                else if (File.Exists(path))
                {
                    if (supportedExtensions.Contains(Path.GetExtension(path)))
                        allFiles.Add(path);
                }
                else
                {
                    Debug.WriteLine($"Invalid path: '{path}'");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error processing path '{path}': {ex.Message}");
                countOfScanningErrors++;
            }
        }
        Debug.WriteLine($"Total folders with errors: {countOfScanningErrors}");
        return allFiles;
    }

    // Get or create a GenreModel.
    public static GenreModel GetOrCreateGenre(
        string genreName,
        Dictionary<string, GenreModel> genreDict,
        List<GenreModel> newGenres,
        List<GenreModel> existingGenres)
    {
        if (!genreDict.TryGetValue(genreName, out GenreModel? genre))
        {
            genre = existingGenres.FirstOrDefault(g =>
                string.Equals(g.Name, genreName, StringComparison.OrdinalIgnoreCase));
            if (genre == null)
            {
                genre = new GenreModel
                {
                    LocalDeviceId = GenerateLocalDeviceID("Genre"),
                    Name = genreName,
                };
                newGenres.Add(genre);
            }
            genreDict[genreName] = genre;
        }
        if (string.IsNullOrWhiteSpace(genre.Name))
            genre.Name = "Unknown Genre";
        return genre;
    }

    // Get or create an AlbumModel.
    public static AlbumModel GetOrCreateAlbum(
        string albumName,
        List<AlbumModel> existingAlbums,
        Dictionary<string, AlbumModel> albumDict,
        List<AlbumModel> newAlbums)
    {
        albumName = string.IsNullOrWhiteSpace(albumName) ? "Unknown Album" : albumName;
        if (!albumDict.TryGetValue(albumName, out AlbumModel? album))
        {
            album = new AlbumModel
            {
                LocalDeviceId = GenerateLocalDeviceID("Album"),
                Name = albumName,
                ImagePath = null,
            };
            albumDict[albumName] = album;
            if (!existingAlbums.Any(a =>
                a.Name.Equals(albumName, StringComparison.OrdinalIgnoreCase)))
            {
                newAlbums.Add(album);
            }
        }
        return album;
    }

    // Get or create an ArtistModel.
    public static ArtistModel GetOrCreateArtist(
        string artistName,
        Dictionary<string, ArtistModel> artistDict,
        List<ArtistModel> newArtists,
        List<ArtistModel> existingArtists)
    {
        if (!artistDict.TryGetValue(artistName, out ArtistModel? artist))
        {
            artist = existingArtists.FirstOrDefault(a =>
                a.Name.Equals(artistName, StringComparison.OrdinalIgnoreCase));
            if (artist == null)
            {
                artist = new ArtistModel
                {
                    LocalDeviceId = GenerateLocalDeviceID("Artist"),
                    Name = artistName,
                    ImagePath = null,
                };
                newArtists.Add(artist);
            }
            artistDict[artistName] = artist;
        }
        return artist;
    }

    // Create a link between artist, album, song, and genre.
    public static void CreateLinks(
        ArtistModel artist,
        AlbumModel album,
        SongModel song,
        GenreModel genre,
        List<AlbumArtistGenreSongLink> newLinks,
        List<AlbumArtistGenreSongLink> existingLinks)
    {
        if (existingLinks == null)
            return;

        bool linkExists = existingLinks.Any(l =>
            l.ArtistId == artist.LocalDeviceId &&
            l.AlbumId == album.LocalDeviceId &&
            l.SongId == song.LocalDeviceId);

        if (!linkExists)
        {
            AlbumArtistGenreSongLink newLink = new AlbumArtistGenreSongLink
            {
                LocalDeviceId = GenerateLocalDeviceID("Lnk"),
                ArtistId = artist.LocalDeviceId,
                AlbumId = album.LocalDeviceId,
                SongId = song.LocalDeviceId,
                GenreId = genre.LocalDeviceId,
            };
            newLinks.Add(newLink);
            Debug.WriteLine("Added Artist-Song Link");
        }
    }

    // Create a SongModel from track metadata.
    public static SongModel CreateSongModel(Track track, string title, string albumName, string artistName, string filePath)
    {
        FileInfo fileInfo = new FileInfo(filePath);
        SongModel song = new SongModel
        {
            LocalDeviceId = Guid.NewGuid().ToString(),
            Title = title,
            AlbumName = albumName,
            ArtistName = artistName,
            Genre = track.Genre,
            FilePath = track.Path,
            DurationInSeconds = track.Duration,
            BitRate = track.Bitrate,
            FileSize = fileInfo.Exists ? fileInfo.Length : 0,
            FileFormat = Path.GetExtension(filePath).TrimStart('.'),
            CoverImagePath = GetCoverImagePath(filePath)
        };

        if ((track.Lyrics.SynchronizedLyrics?.Count > 0) || File.Exists(Path.ChangeExtension(filePath, ".lrc")))
        {
            song.HasSyncedLyrics = true;
            song.SyncLyrics = track.Lyrics.SynchronizedLyrics?.ToString()!;
        }
        if (track.Lyrics.UnsynchronizedLyrics?.Length > 0)
        {
            song.HasLyrics = true;
            song.UnSyncLyrics = track.Lyrics.UnsynchronizedLyrics?.ToString()!;
        }
        if (track.Year.HasValue)
            song.ReleaseYear = track.Year.Value;

        return song;
    }

    // Update cover image paths for all songs.
    public static ObservableCollection<SongModel> CheckCoverImage(ObservableCollection<SongModel> songs)
    {
        foreach (SongModel song in songs)
        {
            if (!string.IsNullOrWhiteSpace(song.FilePath))
                song.CoverImagePath = GetCoverImagePath(song.FilePath);
        }
        return songs;
    }

    // Retrieve the cover image path for a file.
    public static string GetCoverImagePath(string filePath)
    {
        string defaultCover = "musicnoteslider.png";
        Track track;
        try
        {
            track = new Track(filePath);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading track for cover image: {ex.Message}");
            return defaultCover;
        }

        // Check for embedded pictures.
        if (track.EmbeddedPictures != null && track.EmbeddedPictures.Count <1)
        {
            ATL.PictureInfo? picture = track.EmbeddedPictures.FirstOrDefault();
            if (picture != null)
            {
                return FileCoverImageProcessor.SaveOrGetCoverImageToFilePath(filePath, picture.PictureData);
            }
        }

        // Fallback: Look for an image file in a predefined folder.
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
        string folderPath = string.Empty;
        //folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments), "DimmerDB", "CoverImagesDimmer");

        folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CoverImagesDimmer");

        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        List<string> imageFiles = [.. Directory.GetFiles(folderPath, $"{fileNameWithoutExtension}.*", SearchOption.TopDirectoryOnly)
                                  .Where(f => new[] { ".jpg", ".jpeg", ".png" }
                                  .Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))];
        if (imageFiles.Count<1)
            return imageFiles.FirstOrDefault() is null ? defaultCover : imageFiles[0];

        return defaultCover;
    }

    // Return text before the first comma.
    public static string GetTextBeforeComma(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;
        int commaIndex = input.IndexOf(',');
        return commaIndex == -1 ? input : input[..commaIndex];
    }
}
