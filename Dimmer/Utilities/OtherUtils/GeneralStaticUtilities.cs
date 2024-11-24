namespace Dimmer_MAUI.Utilities.OtherUtils;
public static class GeneralStaticUtilities
{
    public static string GenerateRandomString(string CallerClass, int length = 12)
    {
        if (string.IsNullOrEmpty(CallerClass))
        {
            throw new ArgumentNullException(nameof(CallerClass));
        }

        Random random = Random.Shared;
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        char[] stringChars = new char[length];
        stringChars[0] = CallerClass[0];
        stringChars[1] = CallerClass[1];

        for (int i = 2; i < length; i++)
        {
            stringChars[i] = chars[random.Next(chars.Length)];
        }

        return new string(stringChars);
    }

    public static bool IsValidFile(string file)
    {
        FileInfo fileInfo = new(file);
        return fileInfo.Length > 1000;
    }
    public static SongModelView? ProcessFile(
    string file,
    List<AlbumModelView> existingAlbums,
    Dictionary<string, AlbumModelView> albumDict,
    List<AlbumModelView> newAlbums,
    List<SongModelView> oldSongs,
    List<ArtistModelView> newArtists,
    Dictionary<string, ArtistModelView> artistDict,
    List<AlbumArtistGenreSongLinkView> newLinks,
    List<AlbumArtistGenreSongLinkView> existingLinks,
    List<ArtistModelView> existingArtists,
    List<GenreModelView> newGenres,
    Dictionary<string, GenreModelView> genreDict,
    List<GenreModelView> existingGenres)
    {
        ATL.Track track = new(file);
        if (track == null)
        {
            return null;
        }
        Debug.WriteLine($"Track Name {track.Title}");
        string title = track.Title.Contains(';') ? track.Title.Split(';')[0].Trim() : track.Title;
        string albumName = string.IsNullOrEmpty(track.Album.Trim()) ? track.Title : track.Album.Trim();

        AlbumModelView album = GetOrCreateAlbum(albumName, existingAlbums, albumDict, newAlbums);
        Debug.WriteLine("Got Or Created Album");
        // Extract artist names
        var artistNames = GetArtistNames(track.Artist, track.AlbumArtist);
        string mainArtistName = string.Join(", ", artistNames);
        Debug.WriteLine("Got Artist");
        var song = CreateSongModel(track, title, albumName, mainArtistName, file);
        Debug.WriteLine("Got Song");
        if (!string.IsNullOrEmpty(albumName))
        {
            album.AlbumImagePath = song.CoverImagePath;
        }

        // Check if song already exists
        if (oldSongs.Any(s => s.Title == title && s.DurationInSeconds == track.Duration))
        {

            Debug.WriteLine("Skipping existing song for artist: " + song.ArtistName);
            return null;
        }

        // Process genre and links
        var genreName = track.Genre?.Trim();
        if (string.IsNullOrEmpty(genreName))
        {
            genreName = "Unknown Genre";
        }
        var genre = GetOrCreateGenre(genreName, genreDict, newGenres, existingGenres);

        // Process artists and links
        foreach (var artistName in artistNames)
        {
            var artist = GetOrCreateArtist(artistName, artistDict, newArtists, existingArtists);
            CreateLinks(artist, album, song, genre, newLinks, existingLinks); // Create the artist-song link

        }

        return song;
    }



    public static List<string> GetAllFiles(List<string> folderPaths)
    {
        List<string> allFiles = new();
        int countOfScanningErrors = 0;

        foreach (var path in folderPaths.Distinct())
        {
            if (path is null)
            {
                continue;
            }

            try
            {
                var files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories)
                                     .Where(s => s.EndsWith(".mp3") || s.EndsWith(".flac") || s.EndsWith(".wav") || s.EndsWith(".m4a"))
                                     .AsParallel()
                                     .ToList();
                allFiles.AddRange(files);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error scanning folder '{path}': {ex.Message}"); // TODO: Log error
                countOfScanningErrors++;
            }
        }

        Debug.WriteLine($"Total folders with errors: {countOfScanningErrors}");
        return allFiles;
    }

    public static List<string> GetArtistNames(string? artist, string? albumArtist)
    {
        var artistNamesSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Split and process the main artist names
        if (!string.IsNullOrEmpty(artist))
        {
            var artistNames = artist
                .Replace("&", string.Empty, StringComparison.OrdinalIgnoreCase)  // Replace " x " with ","
                .Replace(" x ", ",", StringComparison.OrdinalIgnoreCase)  // Replace " x " with ","
                .Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(a => a.Trim());

            foreach (var name in artistNames)
            {
                artistNamesSet.Add(name);
            }
        }

        // Split and process the album artist names, if any
        if (!string.IsNullOrEmpty(albumArtist))
        {
            var albumArtists = albumArtist
                .Replace(" x ", ",", StringComparison.OrdinalIgnoreCase)  // Replace " x " with ","
                .Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(a => a.Trim());

            foreach (var name in albumArtists)
            {
                artistNamesSet.Add(name);  // Add to the set to ensure uniqueness
            }
        }

        // Return the artist names as a list
        return artistNamesSet.ToList();
    }

    public static GenreModelView GetOrCreateGenre(
    string genreName,
    Dictionary<string, GenreModelView> genreDict,
    List<GenreModelView> newGenres,
    List<GenreModelView> existingGenres)
    {
        if (!genreDict.TryGetValue(genreName, out var genre))
        {
            genre = existingGenres?.FirstOrDefault(g => g.Name!.Equals(genreName, StringComparison.OrdinalIgnoreCase));
            if (genre == null)
            {
                genre = new GenreModelView
                {
                    Name = genreName,
                };
                newGenres.Add(genre);
            }
            genreDict[genreName] = genre;
        }
        if (string.IsNullOrEmpty(genre.Name))
        {
            genre.Name = "Unknown Genre";
        }

        return genre;
    }

    public static AlbumModelView GetOrCreateAlbum(
      string albumName,
      List<AlbumModelView> existingAlbums,
      Dictionary<string, AlbumModelView> albumDict,
      List<AlbumModelView> newAlbums)
    {
        // Assign "Unknown Album" if albumName is empty or null
        if (string.IsNullOrEmpty(albumName))
        {
            albumName = "Unknown Album";
        }

        // Check if album already exists in dictionary
        if (!albumDict.TryGetValue(albumName, out var album))
        {
            album = new AlbumModelView
            {
                Name = albumName,
                AlbumImagePath = null,
            };
            albumDict[albumName] = album;

            // Check if the album already exists in the database list
            if (!existingAlbums.Any(a => a.Name == albumName))
            {
                newAlbums.Add(album);
            }
        }

        return album;
    }
    public static ArtistModelView GetOrCreateArtist(
    string artistName,
    Dictionary<string, ArtistModelView> artistDict,
    List<ArtistModelView> newArtists,
    List<ArtistModelView> existingArtists)
    {
        if (!artistDict.TryGetValue(artistName, out var artist))
        {
            artist = existingArtists?.FirstOrDefault(a => a.Name.Equals(artistName, StringComparison.OrdinalIgnoreCase));
            if (artist == null)
            {
                artist = new ArtistModelView
                {
                    Name = artistName,
                    ImagePath = null,
                };
                newArtists.Add(artist);
            }
            artistDict[artistName] = artist;
        }

        return artist;
    }

    public static void CreateLinks(
      ArtistModelView artist,
      AlbumModelView album,
      SongModelView song,
      GenreModelView genre,
      List<AlbumArtistGenreSongLinkView> newLinks,
      List<AlbumArtistGenreSongLinkView> existingLinks)
    {
        var newLink = new AlbumArtistGenreSongLinkView
        {
            ArtistId = artist.LocalDeviceId,
            AlbumId = album.LocalDeviceId,
            SongId = song.LocalDeviceId,
            GenreId = genre.LocalDeviceId,
        };
        if (existingLinks is null)
        {
            return;
        }
        if (!existingLinks.Any(l => l.ArtistId == artist.LocalDeviceId && l.AlbumId == album.LocalDeviceId && l.SongId == song.LocalDeviceId))
        {
            newLinks.Add(newLink);
            Debug.WriteLine("Added Artist-Song Link");
        }
    }

    public static SongModelView CreateSongModel(
    Track track,
    string title,
    string albumName,
    string artistName,
    string filePath)
    {
        FileInfo fileInfo = new(filePath);

        var song = new SongModelView
        {
            Title = title,
            AlbumName = albumName,
            ArtistName = artistName,
            GenreName = track.Genre,
            ReleaseYear = track.Year,
            SampleRate = track.SampleRate,
            FilePath = track.Path,
            DurationInSeconds = track.Duration,
            BitRate = track.Bitrate,
            FileSize = fileInfo.Length,
            TrackNumber = track.TrackNumber,
            FileFormat = Path.GetExtension(filePath).TrimStart('.'),
            HasLyrics = track.Lyrics.SynchronizedLyrics?.Count > 0 || File.Exists(filePath.Replace(Path.GetExtension(filePath), ".lrc")),

            CoverImagePath = GetCoverImagePath(track.Path),
        };

        return song;
    }

    public static ObservableCollection<SongModelView> CheckCoverImage(ObservableCollection<SongModelView> col)
    {
        foreach (var item in col)
        {
            item.CoverImagePath = GetCoverImagePath(item.FilePath);
        }

        return col;
    }

    public static string? GetCoverImagePath(string filePath)
    {
        var LoadTrack = new Track(filePath);
        byte[]? coverImage = null;

        if (LoadTrack.EmbeddedPictures?.Count > 0)
        {
            string? mimeType = LoadTrack.EmbeddedPictures?.FirstOrDefault()?.MimeType;
            if (mimeType == "image/jpg" || mimeType == "image/jpeg" || mimeType == "image/png")
            {
                coverImage = LoadTrack.EmbeddedPictures?.FirstOrDefault()?.PictureData;
            }
        }

        if (coverImage is not null)
        {
            return LyricsService.SaveOrGetCoverImageToFilePath(filePath, coverImage);
        }

        if (coverImage is null || coverImage.Length < 1)
        {
            string fileNameWithoutExtension = Path.GetFileName(filePath);

#if ANDROID
            string folderPath = Path.Combine(FileSystem.AppDataDirectory, "CoverImagesDimmer"); // Use AppDataDirectory for Android compatibility
#elif WINDOWS
            string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments), "DimmerDB", "CoverImagesDimmer");
#endif


            // Ensure the directory exists
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }


            string[] imageFiles = Directory.GetFiles(folderPath, $"{fileNameWithoutExtension}.jpg", SearchOption.TopDirectoryOnly)
                .Concat(Directory.GetFiles(folderPath, $"{fileNameWithoutExtension}.jpeg", SearchOption.TopDirectoryOnly))
                .Concat(Directory.GetFiles(folderPath, $"{fileNameWithoutExtension}.png", SearchOption.TopDirectoryOnly))
                .ToArray();

            if (imageFiles.Length > 0)
            {
                return imageFiles.ToString();
            }
        }

        if (coverImage is null)
        {
            return null;
        }

        return null;
    }
}
