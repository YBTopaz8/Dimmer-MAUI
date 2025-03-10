

namespace Dimmer_MAUI.Utilities.OtherUtils;
public static class GeneralStaticUtilities
{
    public static string GenerateLocalDeviceID(string CallerClass)
    {
        if (string.IsNullOrEmpty(CallerClass))
        {
            throw new ArgumentNullException(nameof(CallerClass));
        }

        char[] stringChars = new char[3];
        stringChars[0] = CallerClass[0];
        stringChars[1] = CallerClass[1];
        stringChars[2] = CallerClass[2];


        return stringChars[0]+stringChars[1]+stringChars[2]+ Guid.NewGuid().ToString();
    }

    public static bool IsValidFile(string file)
    {
        FileInfo fileInfo = new(file);
        return fileInfo.Length > 1000;
    }
    public static SongModel? ProcessFile(
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
        ATL.Track track = new(file);
        if (track == null)
        {
            return null;
        }

        string title = track.Title.Contains(';') ? track.Title.Split(';')[0].Trim() : track.Title;
        string albumName = string.IsNullOrEmpty(track.Album.Trim()) ? track.Title : track.Album.Trim();

        AlbumModel album = GetOrCreateAlbum(albumName, existingAlbums, albumDict, newAlbums);

        // Extract artist names
        var artistNames = GetArtistNames(track.Artist, track.AlbumArtist);
        string mainArtistName = string.Join(", ", artistNames);

        var song = CreateSongModel(track, title, albumName, mainArtistName, file);

        if (!string.IsNullOrEmpty(albumName))
        {
            album.ImagePath= song.CoverImagePath;
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



    public static T MapFromParseObjectToClassObject<T>(ParseObject parseObject) where T : new()
    {
        var model = new T();
        var properties = typeof(T).GetProperties();

        foreach (var property in properties)
        {
            try
            {
                // Skip Realm-specific properties
                if (IsRealmSpecificType(property.PropertyType))
                {
                    continue;
                }

                // Check if the ParseObject contains the property name
                if (parseObject.ContainsKey(property.Name))
                {
                    var value = parseObject[property.Name];

                    if (value != null)
                    {
                        // Handle special types like DateTimeOffset
                        if (property.PropertyType == typeof(DateTimeOffset) && value is DateTime dateTime)
                        {
                            property.SetValue(model, new DateTimeOffset(dateTime));
                            continue;
                        }

                        // Handle string as string
                        if (property.PropertyType == typeof(string) && value is string objectIdStr)
                        {
                            property.SetValue(model, new string(objectIdStr));
                            continue;
                        }

                        // For other types, directly set the value if the property has a setter
                        //if (property.CanWrite && property.PropertyType.IsAssignableFrom(GetType()))
                        //{
                        //    property.SetValue(model, value);
                        //}
                    }
                }
            }
            catch (Exception ex)
            {
                // Log and skip the property
                Debug.WriteLine($"Error mapping property '{property.Name}': {ex.Message}");
            }
        }

        return model;
    }


    public static bool IsRealmSpecificType(Type type)
    {

        return type.IsSubclassOf(typeof(RealmObject)) || type.IsGenericType && type.GetGenericTypeDefinition() == typeof(RealmList<>) || type == typeof(DynamicObjectApi);
    }
    public static ParseObject MapToParseObject<T>(T model, string className)
    {
        var parseObject = new ParseObject(className);

        // Get the properties of the class
        var properties = typeof(T).GetProperties();

        foreach (var property in properties)
        {
            try
            {
                var value = property.GetValue(model);

                // Skip null values or Realm-specific/unsupported types
                if (value == null || IsRealmSpecificType(property.PropertyType))
                {
                    continue;
                }

                // Handle special types like DateTimeOffset
                if (property.PropertyType == typeof(DateTimeOffset))
                {
                    var val = (DateTimeOffset)value;
                    parseObject[property.Name] = val.Date;
                    continue;
                }

                // Handle string as string (required for Parse compatibility)
                if (property.PropertyType == typeof(string))
                {
                    parseObject[property.Name] = value.ToString();
                    continue;
                }

                // For other types, directly set the value
                parseObject[property.Name] = value;
            }
            catch (Exception ex)
            {
                // Log the exception for this particular property, but continue with the next one
                Debug.WriteLine($"Error when mapping property '{property.Name}': {ex.Message}");
            }
        }

        return parseObject;
    }


    public static List<string> GetAllFiles(List<string> folderPaths)
    {
        List<string> allFiles = [];
        int countOfScanningErrors = 0;

        foreach (var path in folderPaths.Distinct())
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                continue;
            }

            try
            {
                if (Directory.Exists(path))
                {
                    // It's a valid directory, get files recursively
                    var files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories)
                                         .Where(s => s.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) ||
                                                     s.EndsWith(".flac", StringComparison.OrdinalIgnoreCase) ||
                                                     s.EndsWith(".wav", StringComparison.OrdinalIgnoreCase) ||
                                                     s.EndsWith(".m4a", StringComparison.OrdinalIgnoreCase))
                                         .AsParallel()
                                         .ToList();
                    allFiles.AddRange(files);
                }
                else if (File.Exists(path))
                {
                    // It's a file, check the extension and add if it matches
                    if (path.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) ||
                        path.EndsWith(".flac", StringComparison.OrdinalIgnoreCase) ||
                        path.EndsWith(".wav", StringComparison.OrdinalIgnoreCase) ||
                        path.EndsWith(".m4a", StringComparison.OrdinalIgnoreCase))
                    {
                        allFiles.Add(path);
                    }
                }
                else
                {
                    Debug.WriteLine($"Invalid path: '{path}'"); // Log invalid paths
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
        return [.. artistNamesSet];
    }

    public static GenreModel GetOrCreateGenre(
    string genreName,
    Dictionary<string, GenreModel> genreDict,
    List<GenreModel> newGenres,
    List<GenreModel> existingGenres)
    {
        if (!genreDict.TryGetValue(genreName, out var genre))
        {
            genre = existingGenres?.FirstOrDefault(g => g.Name!.Equals(genreName, StringComparison.OrdinalIgnoreCase));
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
        if (string.IsNullOrEmpty(genre.Name))
        {
            genre.Name = "Unknown Genre";
        }

        return genre;
    }

    public static AlbumModel GetOrCreateAlbum(
      string albumName,
      List<AlbumModel> existingAlbums,
      Dictionary<string, AlbumModel> albumDict,
      List<AlbumModel> newAlbums)
    {
        // Assign "Unknown Album" if albumName is empty or null
        if (string.IsNullOrEmpty(albumName))
        {
            albumName = "Unknown Album";
        }

        // Check if album already exists in dictionary
        if (!albumDict.TryGetValue(albumName, out var album))
        {
            album = new AlbumModel
            {
                LocalDeviceId = GenerateLocalDeviceID("Album"),
                Name = albumName,
                ImagePath = null,
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
    public static ArtistModel GetOrCreateArtist(
    string artistName,
    Dictionary<string, ArtistModel> artistDict,
    List<ArtistModel> newArtists,
    List<ArtistModel> existingArtists)
    {
        if (!artistDict.TryGetValue(artistName, out var artist))
        {
            artist = existingArtists?.FirstOrDefault(a => a.Name.Equals(artistName, StringComparison.OrdinalIgnoreCase));
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

    public static void CreateLinks(
      ArtistModel artist,
      AlbumModel album,
      SongModel song,
      GenreModel genre,
      List<AlbumArtistGenreSongLink> newLinks,
      List<AlbumArtistGenreSongLink> existingLinks)
    {
        var newLink = new AlbumArtistGenreSongLink
        {
            LocalDeviceId = GenerateLocalDeviceID("Lnk"),
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

    public static SongModel CreateSongModel(
    Track track,
    string title,
    string albumName,
    string artistName,
    string filePath)
    {
        FileInfo fileInfo = new(filePath);

        var song = new SongModel
        {
            LocalDeviceId = Guid.NewGuid().ToString(),
            Title = title,
            AlbumName = albumName,
            ArtistName = artistName,
            Genre = track.Genre,

            FilePath = track.Path,
            DurationInSeconds = track.Duration,
            BitRate = track.Bitrate,
            FileSize = fileInfo.Length,

            FileFormat = Path.GetExtension(filePath).TrimStart('.'),
            HasLyrics = track.Lyrics.SynchronizedLyrics?.Count > 0 || File.Exists(filePath.Replace(Path.GetExtension(filePath), ".lrc")),

            CoverImagePath = GetCoverImagePath(track.Path),
        };

        if (track.Year is not null)
        {
            song.ReleaseYear = (int)track.Year;
        }
        return song;
    }

    public static ObservableCollection<SongModel> CheckCoverImage(ObservableCollection<SongModel> col)
    {
        foreach (var item in col)
        {
            if (item.FilePath is not null)
            {
                item.CoverImagePath = GetCoverImagePath(item.FilePath);
            }
        }

        return col;
    }

    public static string GetCoverImagePath(string filePath)
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

#if ANDROID && NET9_0
            string folderPath = Path.Combine(FileSystem.AppDataDirectory, "CoverImagesDimmer"); // Use AppDataDirectory for Android compatibility
#elif WINDOWS && NET9_0
            string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments), "DimmerDB", "CoverImagesDimmer");
#endif


            // Ensure the directory exists
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }


            string[] imageFiles =
            [
                .. Directory.GetFiles(folderPath, $"{fileNameWithoutExtension}.jpg", SearchOption.TopDirectoryOnly)
,
                .. Directory.GetFiles(folderPath, $"{fileNameWithoutExtension}.jpeg", SearchOption.TopDirectoryOnly),
                .. Directory.GetFiles(folderPath, $"{fileNameWithoutExtension}.png", SearchOption.TopDirectoryOnly),
            ];

            if (imageFiles.Length > 0)
            {
                return imageFiles.ToString();
            }
        }

        if (coverImage is null)
        {
            
            return "musicnoteslider.png";
        }

        return "musicnoteslider.png";
    }

#if WINDOWS
    public static void UpdateTaskBarProgress(double progress)
    {
        return;
        //// wait for now.
        //int maxProgressbarValue = 100;
        //var taskbarInstance = Microsoft.WindowsAPICodePack.Taskbar.TaskbarManager.Instance;
        //taskbarInstance.SetProgressState(Microsoft.WindowsAPICodePack.Taskbar.TaskbarProgressBarState.Normal);
        //taskbarInstance.SetProgressValue((int)progress, maxProgressbarValue);

        //if (progress >= maxProgressbarValue)
        //{
        //    taskbarInstance.SetProgressState(Microsoft.WindowsAPICodePack.Taskbar.TaskbarProgressBarState.NoProgress);
        //}

    }
#endif


    public static string GetTextBeforeComma(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        int commaIndex = input.IndexOf(',');
        if (commaIndex == -1) // No comma found
        {
            return input;
        }
        else
        {
            return input.Substring(0, commaIndex);
        }
    }


    public static void RunFireAndForget(Task task, Action<Exception>? onException = null)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await task;
            }
            catch (Exception ex)
            {
                onException?.Invoke(ex);
            }
        });
    }

    public static void ClearUp()
    {
        var DimmerAudioService = IPlatformApplication.Current!.Services.GetService<IDimmerAudioService>() as DimmerAudioService;
        DimmerAudioService?.Dispose();
    }

}


public static class UserActivityLogger
{
    public static async Task LogUserActivity(
        ParseUser sender,        
        PlayType activityType, 
        ParseUser? recipient = null,
        Message? chatMessage = null,
        SharedPlaylist? sharedPlaylist = null,
        SongModelView? nowPlaying = null,
        bool? isRead = null,
        Dictionary<string, object>? additionalData = null,
        ChatRoom? chatRoomm =null,
        ParseUser? CurrentUserOnline=null)
    {
        // --- Input Validation (Crucial for Robustness) ---
        
        if (sender == null)
        {
            throw new ArgumentNullException(nameof(sender), "Sender cannot be null.");
        }
        
        
        if (CurrentUserOnline is null)
        {
            return;
        }
        // --- Create the UserActivity Object ---

        try
        {
            

            UserActivity newActivity = new UserActivity();

            // --- Set Core Fields ---

            newActivity["sender"] = sender;
            newActivity["activityType"] = (int)activityType;          
            newActivity["devicePlatform"] = DeviceInfo.Current.Platform.ToString();
            newActivity["deviceIdiom"] = DeviceInfo.Current.Idiom.ToString();
            newActivity["deviceVersion"] = DeviceInfo.Current.VersionString;

            // --- Set Optional Related Objects (Pointers) ---
            // Use null-conditional operator and null-coalescing operator for brevity and safety.

            newActivity["chatMessage"] = chatMessage ?? null; //if chatMessage is not null, set newActivity["chatMessage"] = chatMessage , else set to null.
            

            // --- Set isRead (if provided) ---

            if (isRead.HasValue)
            {
                newActivity["isRead"] = isRead.Value;
            }

            // --- Add Additional Data (Flexibility) ---

            if (additionalData != null)
            {
                foreach (var kvp in additionalData)
                {
                    newActivity[kvp.Key] = kvp.Value;
                }
            }

            // --- Save the UserActivity ---

            await newActivity.SaveAsync(); //thrown on this line
        }
        catch (Exception ex)
        {
            // CRITICAL:  Handle errors!  In a real app, you'd log this properly.
            Console.WriteLine($"Error logging user activity: {ex.Message}");
            // Consider re-throwing the exception, or returning a result indicating failure,
            // depending on how you want to handle errors in the calling code.
            throw; // Re-throw for now, so the caller knows it failed.
        }
    }
}