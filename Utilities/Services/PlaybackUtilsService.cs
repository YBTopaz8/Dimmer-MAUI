namespace Dimmer_MAUI.Utilities.Services;
public partial class PlaybackUtilsService : ObservableObject, IPlaybackUtilsService
{

    INativeAudioService audioService;
    public IObservable<ObservableCollection<SongModelView>> NowPlayingSongs => _nowPlayingSubject.AsObservable();
    BehaviorSubject<ObservableCollection<SongModelView>> _nowPlayingSubject = new([]);
   
    ObservableCollection<SongModelView> nowPlayingShuffledOrNotSubject = new([]);
    
    public IObservable<ObservableCollection<SongModelView>> SecondaryQueue => _secondaryQueueSubject.AsObservable();
    BehaviorSubject<ObservableCollection<SongModelView>> _secondaryQueueSubject = new([]);
    public IObservable<ObservableCollection<SongModelView>> TertiaryQueue => _tertiaryQueueSubject.AsObservable();
    BehaviorSubject<ObservableCollection<SongModelView>> _tertiaryQueueSubject = new([]);

    public IObservable<MediaPlayerState> PlayerState => _playerStateSubject.AsObservable();
    BehaviorSubject<MediaPlayerState> _playerStateSubject = new(MediaPlayerState.Stopped);
    public IObservable<PlaybackInfo> CurrentPosition => _currentPositionSubject.AsObservable();
    BehaviorSubject<PlaybackInfo> _currentPositionSubject = new(new());
    System.Timers.Timer? _positionTimer;

    [ObservableProperty]
    private SongModelView? observableCurrentlyPlayingSong;
    public SongModelView? CurrentlyPlayingSong => ObservableCurrentlyPlayingSong;
    [ObservableProperty]
    private SongModelView observablePreviouslyPlayingSong;
    public SongModelView PreviouslyPlayingSong => ObservablePreviouslyPlayingSong;
    [ObservableProperty]
    private SongModelView observableNextPlayingSong;
    public SongModelView NextPlayingSong => ObservableNextPlayingSong;

    [ObservableProperty]
    int observableLoadingSongsProgress;
    public int LoadingSongsProgressPercentage => ObservableLoadingSongsProgress;

    [ObservableProperty]
    string totalSongsSizes;
    [ObservableProperty]
    string totalSongsDuration;
    ISongsManagementService SongsMgtService { get; }
    IStatsManagementService StatsMgtService { get; }
    public IPlaylistManagementService PlaylistManagementService { get; }
    public IArtistsManagementService ArtistsMgtService { get; }
    [ObservableProperty]
    ObservableCollection<PlaylistModelView>? allPlaylists;
    [ObservableProperty]
    ObservableCollection<ArtistModelView>? allArtists;
    [ObservableProperty]
    ObservableCollection<AlbumModelView>? allAlbums;

    [ObservableProperty]
    string selectedPlaylistName;

    int _currentSongIndex = 0;

    [ObservableProperty]
    bool isShuffleOn;
    [ObservableProperty]
    int currentRepeatMode;
    public int CurrentRepeatCount { get; set; } = 1;
    bool isSongPlaying;

    List<ObjectId> playedSongsIDs = [];
    Random _shuffleRandomizer = new Random();
    SortingEnum CurrentSorting;
    public PlaybackUtilsService(INativeAudioService AudioService, ISongsManagementService SongsMgtService,
        IStatsManagementService statsMgtService, IPlaylistManagementService playlistManagementService,
        IArtistsManagementService artistsMgtService)
    {
        this.SongsMgtService = SongsMgtService;
        StatsMgtService = statsMgtService;
        PlaylistManagementService = playlistManagementService;
        ArtistsMgtService = artistsMgtService;
        audioService = AudioService;

        audioService.PlayPrevious += AudioService_PlayPrevious;
        audioService.PlayNext += AudioService_PlayNext;
        audioService.IsPlayingChanged += AudioService_PlayingChanged;
        audioService.PlayEnded += AudioService_PlayEnded;
        audioService.IsSeekedFromNotificationBar += AudioService_IsSeekedFromNotificationBar;

        IsShuffleOn = AppSettingsService.ShuffleStatePreference.GetShuffleState();
        CurrentRepeatMode = AppSettingsService.RepeatModePreference.GetRepeatState();

        CurrentQueue = 0; //0 = main queue, 1 = playlistQ, 2 = externallyloadedsongs Queue

        LoadLastPlayedSong();
        LoadSongsWithSorting();

        LoadFirstPlaylist();
        AllPlaylists = PlaylistManagementService.AllPlaylists?.ToObservableCollection();
        AllArtists = ArtistsMgtService.AllArtists?.ToObservableCollection();
    }

    private void AudioService_IsSeekedFromNotificationBar(object? sender, long e)
    {
        currentPositionInSec = e/1000;
    }


    #region Setups/Loadings Region


    private Dictionary<string, ArtistModelView> artistDict = new Dictionary<string, ArtistModelView>();
    HomePageVM? ViewModel { get; set; }
    private (List<ArtistModelView>?, List<AlbumModelView>?, List<AlbumArtistSongLink>?, List<SongModelView>?,
    List<GenreModelView>?, List<AlbumArtistGenreSongLink>?) LoadSongsAsync(List<string> folderPaths, IProgress<int> progress)
    {
        try
        {
            var allFiles = GetAllFiles(folderPaths);
            Debug.WriteLine("Got All Files");
            if (allFiles.Count == 0)
            {
                return (null, null, null, null, null, null);
            }

            // Fetch existing data from services
            var existingArtists = ArtistsMgtService.AllArtists;
            var existingLinks = ArtistsMgtService.AlbumsArtistsSongLink;
            var existingAlbums = SongsMgtService.AllAlbums;
            var existingGenres = SongsMgtService.AllGenres is null? [] : SongsMgtService.AllGenres;
            var oldSongs = SongsMgtService.AllSongs ?? new List<SongModelView>();

            // Initialize collections and dictionaries
            var newArtists = new List<ArtistModelView>();
            var newAlbums = new List<AlbumModelView>();
            var newLinks = new List<AlbumArtistSongLink>();
            var newGenres = new List<GenreModelView>();
            var genreLinks = new List<AlbumArtistGenreSongLink>(); // New genre link collection
            var allSongs = new List<SongModelView>();

            var artistDict = new Dictionary<string, ArtistModelView>(StringComparer.OrdinalIgnoreCase);
            var albumDict = new Dictionary<string, AlbumModelView>();
            var genreDict = new Dictionary<string, GenreModelView>();

            int processedFiles = 0;
            int totalFiles = allFiles.Count;

            foreach (var file in allFiles)
            {
                if (IsValidFile(file))
                {
                    var songData = ProcessFile(file, existingAlbums.ToList(), albumDict, newAlbums, oldSongs.ToList(),
                        newArtists, artistDict, newLinks, existingLinks.ToList(), existingArtists.ToList(),
                        newGenres, genreDict, existingGenres.ToList(), genreLinks); // Pass genreLinks

                    if (songData != null)
                    {
                        allSongs.Add(songData);
                    }
                }

                processedFiles++;
                if (processedFiles % 10 == 0) // Report progress every 10 files
                {
                    int percentComplete = processedFiles * 100 / totalFiles;
                    progress.Report(percentComplete);
                }
            }
            Debug.WriteLine("All Processed");
            ViewModel?.SetPlayerState(MediaPlayerState.LoadingSongs);

            return (newArtists, newAlbums, newLinks, allSongs.ToList(), newGenres, genreLinks); // Return genreLinks
        }
        catch (Exception ex)
        {
            MainThread.BeginInvokeOnMainThread(() =>
                Shell.Current.DisplayAlert("Error while scanning files", ex.Message, "OK")
            );
            return (null, null, null, null, null, null);
        }
    }

    private List<string> GetAllFiles(List<string> folderPaths)
    {
        List<string> allFiles = new();

        foreach (var path in folderPaths.Distinct())
        {
            if (path is null)
            {
                continue;
            }
            allFiles.AddRange(Directory.GetFiles(path, "*.*", SearchOption.AllDirectories)
                                .Where(s => s.EndsWith(".mp3") || s.EndsWith(".flac") || s.EndsWith(".wav") || s.EndsWith(".m4a"))
                                .AsParallel()
                                .ToList());
        }

        return allFiles;
    }
    private bool IsValidFile(string file)
    {
        FileInfo fileInfo = new(file);
        return fileInfo.Length > 1000;
    }
    private SongModelView? ProcessFile(
    string file,
    List<AlbumModelView> existingAlbums,
    Dictionary<string, AlbumModelView> albumDict,
    List<AlbumModelView> newAlbums,
    List<SongModelView> oldSongs,
    List<ArtistModelView> newArtists,
    Dictionary<string, ArtistModelView> artistDict,
    List<AlbumArtistSongLink> newLinks,
    List<AlbumArtistSongLink> existingLinks,
    List<ArtistModelView> existingArtists,
    List<GenreModelView> newGenres,
    Dictionary<string, GenreModelView> genreDict,
    List<GenreModelView> existingGenres,
    List<AlbumArtistGenreSongLink> genreLinks)
    {
        ATL.Track track = new(file);
        Debug.WriteLine($"Track Name {track.Title}");
        string title = track.Title.Contains(';') ? track.Title.Split(';')[0].Trim() : track.Title;
        string albumName = string.IsNullOrEmpty(track.Album?.Trim()) ? track.Title : track.Album?.Trim();
        
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

        // Process artists and links
        foreach (var artistName in artistNames)
        {
            var artist = GetOrCreateArtist(artistName, artistDict, newArtists, existingArtists);
            
            CreateLinks(artist, album, song, newLinks, existingLinks); // Create the artist-song link
        }

        // Process genre and links
        var genreName = track.Genre?.Trim();
        if (string.IsNullOrEmpty(genreName))
        {
            genreName = "Unknown Genre";
        }
        var genre = GetOrCreateGenre(genreName, genreDict, newGenres, existingGenres);

        // Find the artist-song link to retrieve the ArtistId
        var artistSongLink = newLinks.FirstOrDefault(l => l.SongId == song.Id);

        if (artistSongLink != null)
        {
            var genreLink = new AlbumArtistGenreSongLink
            {
                SongId = song.Id,
                ArtistId = artistSongLink.ArtistId, // Use ArtistId from artist-song link
                AlbumId = album.Id,
                GenreId = genre.Id
            };
            genreLinks.Add(genreLink);
        }
        else
        {
            Debug.WriteLine("Artist-Song link not found for song: " + song.Title);
        }
        

        return song;
    }



    private GenreModelView GetOrCreateGenre(
    string genreName,
    Dictionary<string, GenreModelView> genreDict,
    List<GenreModelView> newGenres,
    List<GenreModelView> existingGenres)
    {
        if (!genreDict.TryGetValue(genreName, out var genre))
        {
            genre = existingGenres.FirstOrDefault(g => g.Name.Equals(genreName, StringComparison.OrdinalIgnoreCase));
            if (genre == null)
            {
                genre = new GenreModelView
                {
                    Id = ObjectId.GenerateNewId(),
                    Name = genreName
                };
                newGenres.Add(genre);
            }
            genreDict[genreName] = genre;
        }

        return genre;
    }


    private void CreateGenreLink(GenreModelView genre, SongModelView song)
    {
        var genreLink = new AlbumArtistGenreSongLink
        {
            SongId = song.Id,
            GenreId = genre.Id
        };

        // Add this genre-song link to the database or in-memory collection (implement your logic here)
        // e.g., add to a list or directly add to Realm DB
    }

    private List<string> GetArtistNames(string? artist, string? albumArtist)
    {
        var artistNamesSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Split and process the main artist names
        if (!string.IsNullOrEmpty(artist))
        {
            var artistNames = artist
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


    private AlbumModelView GetOrCreateAlbum(
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
                Id = ObjectId.GenerateNewId(),
                Name = albumName,
                AlbumImagePath = null // Default value, will be updated later if needed
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

    private ArtistModelView GetOrCreateArtist(
        string artistName,
        Dictionary<string, ArtistModelView> artistDict,
        List<ArtistModelView> newArtists,
        List<ArtistModelView> existingArtists)
    {
        if (!artistDict.TryGetValue(artistName, out var artist))
        {
            artist = existingArtists.FirstOrDefault(a => a.Name.Equals(artistName, StringComparison.OrdinalIgnoreCase));
            if (artist == null)
            {
                artist = new ArtistModelView
                {
                    Id = ObjectId.GenerateNewId(),
                    Name = artistName,
                    ImagePath = null
                };
                newArtists.Add(artist);
            }
            artistDict[artistName] = artist;
        }

        return artist;
    }

    private void CreateLinks(
      ArtistModelView artist,
      AlbumModelView album,
      SongModelView song,
      List<AlbumArtistSongLink> newLinks,
      List<AlbumArtistSongLink> existingLinks)
    {
        var newLink = new AlbumArtistSongLink
        {
            ArtistId = artist.Id,
            AlbumId = album.Id,
            SongId = song.Id
        };

        if (!existingLinks.Any(l => l.ArtistId == artist.Id && l.AlbumId == album.Id && l.SongId == song.Id))
        {
            newLinks.Add(newLink);
            Debug.WriteLine("Added Artist-Song Link");
        }
    }

    private SongModelView CreateSongModel(
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
            DateAdded = fileInfo.CreationTime,
            CoverImagePath = GetCoverImagePath(track.Path)
        };

        return song;
    }



    public async Task<bool> LoadSongsFromFolderAsync(List<string> folderPaths)
    {
        isLoadingSongs=true;
        ViewModel = IPlatformApplication.Current!.Services.GetService<HomePageVM>();

        // Create a progress reporter
        var progress = new Progress<int>(percent =>
        {
            ObservableLoadingSongsProgress = percent;  // Update UI with the current progress
        });
        Debug.WriteLine($"Percentage {ObservableLoadingSongsProgress}");
        // Load songs from folders asynchronously without blocking the UI
        (var allArtists, var allAlbums, var allLinks, var songs, var allGenres, var genreLinks) =
            await Task.Run(() => LoadSongsAsync(folderPaths, progress));

        if (songs is null || allArtists is null || allAlbums is null || allGenres is null || genreLinks is null)
        {
            await Shell.Current.DisplayAlert("Error during Scan", "No Songs to Scan", "OK");
            return false;
        }

        songs = songs.DistinctBy(x => new { x.Title, x.DurationInSeconds, x.AlbumName, x.ArtistName }).ToList();
        allArtists = allArtists.DistinctBy(x => x.Name).ToList();
        allAlbums = allAlbums.DistinctBy(x => x.Name).ToList();
        allGenres = allGenres.DistinctBy(x => x.Name).ToList();
        allLinks = allLinks.DistinctBy(x => new { x.ArtistId, x.AlbumId, x.SongId }).ToList();
        genreLinks = genreLinks.DistinctBy(x => new { x.GenreId, x.SongId }).ToList(); // Ensure unique genre-song links

        // Add songs, artists, albums, and genres to the database
        AddSongsToDatabase(allArtists, allAlbums, allLinks, songs, allGenres, genreLinks);

        var allSongss = SongsMgtService.AllSongs.Concat(songs).ToObservableCollection();
        LoadSongsWithSorting(allSongss);

        SongsMgtService.GetSongs();

        ObservableLoadingSongsProgress = 100;
        ViewModel?.SetPlayerState(MediaPlayerState.LoadingSongs);
        return true;
    }

    private void AddSongsToDatabase(List<ArtistModelView> allArtists, List<AlbumModelView> allAlbums,
    List<AlbumArtistSongLink> allLinks, List<SongModelView> songs, List<GenreModelView> allGenres, List<AlbumArtistGenreSongLink> genreLinks)
    {
        List<SongsModel> dbSongs = songs?.Select(song => new SongsModel(song)).ToList()!;
        ArtistsMgtService.AddSongToArtistWithArtistIDAndAlbumAndGenre(allArtists, allAlbums, 
            allLinks, dbSongs, allGenres, genreLinks);

    }


    private void LoadSongsWithSorting(ObservableCollection<SongModelView>? songss = null, bool isFromSearch = false)
    {
        if (songss == null || songss.Count < 1)
        {
            songss = SongsMgtService.AllSongs.ToObservableCollection();
        }
        CurrentSorting = AppSettingsService.SortingModePreference.GetSortingPref();
        var sortedSongs = AppSettingsService.ApplySorting(songss, CurrentSorting);
        
        _nowPlayingSubject.OnNext(sortedSongs);
        ToggleShuffle(IsShuffleOn);
    }

    public SongModelView? lastPlayedSong { get; set; }

    private void LoadLastPlayedSong()
    {
        var lastPlayedSongID = AppSettingsService.LastPlayedSongSettingPreference.GetLastPlayedSong();
        if (lastPlayedSongID is not null)
        {
            lastPlayedSong = SongsMgtService.AllSongs.FirstOrDefault(x => x.Id == (ObjectId)lastPlayedSongID);

            if (lastPlayedSong is null)
            {
                return;
            }
            ObservableCurrentlyPlayingSong = lastPlayedSong!;
            //audioService.InitializeAsync(ObservableCurrentlyPlayingSong);
            //_nowPlayingSubject.OnNext(ObservableCurrentlyPlayingSong);
            _currentSongIndex = SongsMgtService.AllSongs.IndexOf(ObservableCurrentlyPlayingSong);
        }

        _playerStateSubject.OnNext(MediaPlayerState.Initialized);
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

        if(coverImage is not null)
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
    byte[]? GetCoverImage(string filePath, bool isToGetByteArrayImages)
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


        if (coverImage is null || coverImage.Length < 1)
        {
            string fileNameWithoutExtension = Path.GetFileName(filePath);
            string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "DimmerDB", "CoverImagesDimmer");
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
                coverImage = File.ReadAllBytes(imageFiles[0]);

                if (!string.IsNullOrEmpty(ObservableCurrentlyPlayingSong.CoverImagePath))
                {
                    ObservableCurrentlyPlayingSong.CoverImagePath = imageFiles[0];

                }
                return coverImage;
            }
        }

        if (coverImage is null)
        {
            _playerStateSubject.OnNext(MediaPlayerState.CoverImageDownload);
        }

        return coverImage;
    }
    #endregion


    #region Playback Control Region
    public async Task<bool> PlaySelectedSongsOutsideAppAsync(List<string> filePaths)
    {
        if(filePaths.Count < 1)
            return false;
        // Filter the array to include only specific file extensions
        var filteredFiles = filePaths.Where(path => path.EndsWith(".mp3") || path.EndsWith(".flac") || path.EndsWith(".wav") || path.EndsWith(".m4a")).ToList();
        Dictionary<string, SongModelView>? existingSongDictionary = new();
        if (_tertiaryQueueSubject.Value is not null)
        {
            existingSongDictionary = _tertiaryQueueSubject.Value.ToDictionary(song => song.FilePath, song => song, StringComparer.OrdinalIgnoreCase);
        }
        else
        {
        }
        var allSongs = new ObservableCollection<SongModelView>();
        foreach (var file in filteredFiles)
        {
            FileInfo fileInfo = new(file);
            if (fileInfo.Length < 1000)
            {
                continue;
            }
            if (existingSongDictionary.TryGetValue(file, out var existingSong))
            {
                // If the song already exists, use the existing song
                allSongs.Add(existingSong);
            }
            else
            {
                // Otherwise, create a new song object and add it to the collection
                Track track = new Track(file);
                var newSong = new SongModelView
                {
                    Title = track.Title,
                    GenreName = track.Genre,
                    ArtistName = track.Artist,
                    AlbumName = track.Album,
                    ReleaseYear = track.Year,
                    SampleRate = track.SampleRate,
                    FilePath = track.Path,
                    DurationInSeconds = track.Duration,
                    BitRate = track.Bitrate,
                    FileSize = fileInfo.Length,
                    TrackNumber = track.TrackNumber,
                    FileFormat = Path.GetExtension(file).TrimStart('.'),
                    HasLyrics = track.Lyrics.SynchronizedLyrics?.Count > 0 || File.Exists(file.Replace(Path.GetExtension(file), ".lrc")),
                    CoverImagePath = LyricsService.SaveOrGetCoverImageToFilePath(track.Path, track.EmbeddedPictures?.FirstOrDefault()?.PictureData),
                };
                allSongs.Add(newSong);
            }

        }
        _tertiaryQueueSubject.OnNext(allSongs);
        await PlaySongAsync(allSongs[0], 2, allSongs);

        return true;
    }
    //private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);  // Initialize with a count of 1
    bool isLoadingSongs;
    public async Task<bool> PlaySongAsync(SongModelView? song = null, int currentQueue = 0,
        ObservableCollection<SongModelView>? currentList = null, double positionInSec = 0,
        int repeatMode = 0, int repeatMaxCount = 0,
        bool IsFromPreviousOrNext = false, AppState CurrentAppStatee = AppState.OnForeGround)
    {
        ViewModel ??= IPlatformApplication.Current!.Services.GetService<HomePageVM>();
        CurrentQueue = currentQueue;
        if (currentList is not null && CurrentQueue == 1)
        {
            _secondaryQueueSubject.OnNext(currentList);
        }
        if (currentList is not null && CurrentQueue == 2)
        {
            _tertiaryQueueSubject.OnNext(currentList);
        }
        if (CurrentAppState != CurrentAppStatee)
        {
            CurrentAppState = CurrentAppStatee;
        }
        if (_positionTimer != null)
        {
            _positionTimer.Stop();
            _positionTimer.Elapsed -= OnPositionTimerElapsed;
            _positionTimer.Dispose();
            _positionTimer = null;
        }
        if (ObservableCurrentlyPlayingSong != null)
        {
            ObservableCurrentlyPlayingSong.IsPlaying = false;
        }
        else
        {
            ObservableCurrentlyPlayingSong = _nowPlayingSubject.Value.FirstOrDefault()!;
            if (ObservableCurrentlyPlayingSong is null)
            {
                if (!isLoadingSongs)
                {
                    await Shell.Current.DisplayAlert("Error when trying to play song", "Song Not Ready to play yet!", "OK");
                }
            }
        }
        if (repeatMode == 4)
        {
            CurrentRepeatCount = 1;
            CurrentRepeatMode = 4;
            this.repeatCountMax = repeatMaxCount;
        }
        try
        {
            if (song != null)
            {
                if (!File.Exists(song.FilePath))
                {
                    await SongsMgtService.DeleteSongFromDB(song.Id);                    
                    _nowPlayingSubject.OnNext(SongsMgtService.AllSongs.ToObservableCollection());                    
                    return false;
                }
                //song.IsPlaying = true;
                ObservableCurrentlyPlayingSong = song!;
                //if (nowPlayingShuffledOrNotSubject != null)
                //{
                //    int songIndex = nowPlayingShuffledOrNotSubject.IndexOf(song);
                //    if (songIndex != -1)
                //    {
                //        _currentSongIndex = songIndex;
                //    }
                //}
            }

            var coverImage = GetCoverImage(ObservableCurrentlyPlayingSong!.FilePath, true);
            
            if (!File.Exists(ObservableCurrentlyPlayingSong.FilePath))
            {
                return false;
            }
            currentPositionInSec = 0;
            _positionTimer = new System.Timers.Timer(1000);
            _positionTimer.Elapsed += OnPositionTimerElapsed;
            _positionTimer.AutoReset = true;

            CurrentQueue = currentQueue;
            _playerStateSubject.OnNext(MediaPlayerState.LyricsLoad);

            audioService.Initialize(ObservableCurrentlyPlayingSong, coverImage);

            // Now, play the audio after initialization has completed
            

            if (positionInSec > 0)
            {
                await audioService.PlayAsync(IsFromPreviousOrNext);
                currentPositionInSec = positionInSec;
                await audioService.SetCurrentTime(currentPositionInSec);
            }
            else
            {
                await audioService.PlayAsync(true);
            }
            _positionTimer.Start();
            ObservableCurrentlyPlayingSong.DatesPlayedAndWasPlayCompleted ??= new ObservableCollection<PlayDateAndIsPlayCompletedModelView>();
            ObservableCurrentlyPlayingSong.DatesPlayedAndWasPlayCompleted.Add(new PlayDateAndIsPlayCompletedModelView
            {
                DatePlayed = DateTimeOffset.Now,
                WasPlayCompleted = false // Mark as incomplete
            });

            ViewModel!.SetPlayerState(MediaPlayerState.Playing);
            _playerStateSubject.OnNext(MediaPlayerState.Playing);
            ViewModel.SetPlayerState(MediaPlayerState.RefreshStats);

            return true;
        }
        catch (Exception e)
        {
            Debug.WriteLine("Error message!!:  " + e.Message);

            await Shell.Current.DisplayAlert("Error message!!!!", $"********** UNHANDLED EXCEPTION! Details: {e.Message} | {e.Source} " +
            $"| {e.TargetSite} ", "Ok");
            return false;
        }
        finally
        {
            if (File.Exists(ObservableCurrentlyPlayingSong!.FilePath) && ObservableCurrentlyPlayingSong != null && currentQueue != 2)
            {
                SongsMgtService.UpdateSongDetails(ObservableCurrentlyPlayingSong);
                _currentPositionSubject.OnNext(new());
                
            }

            //ShowMiniPlayBackView();

        }
    }

    private void ShowMiniPlayBackView()
    {
#if WINDOWS
       
            MiniPlayBackControlNotif.ShowUpdateMiniView(ObservableCurrentlyPlayingSong!);
       
#endif
    }

    public AppState CurrentAppState = AppState.OnForeGround;


    private Random random = new Random();  // Reuse the same Random instance
    bool isTrueShuffle = false;  // Set this based on whether shuffle is enabled

    public ObservableCollection<SongModelView> mobileSongOrder;
    private void GetPrevAndNextSongs(bool IsNext = false, bool IsPrevious = false)
    {
        try
        {
            if (!nowPlayingShuffledOrNotSubject.Contains(ObservableCurrentlyPlayingSong))
            {
                nowPlayingShuffledOrNotSubject.Add(ObservableCurrentlyPlayingSong!); // Add to shuffled list
            }
            // Determine the list to pick from based on CurrentQueue
            ObservableCollection<SongModelView>? listToPickFrom = CurrentQueue switch
            {
                0 => SongsMgtService.AllSongs.ToObservableCollection(),
                1 => _secondaryQueueSubject.Value,
                2 => _tertiaryQueueSubject.Value,
                _ => null,
            };
            if (listToPickFrom == null || listToPickFrom.Count < 1)
                return;

            _currentSongIndex = listToPickFrom.IndexOf(ObservableCurrentlyPlayingSong!);

            // Next song requested
            if (IsNext)
            {
                if (IsShuffleOn)
                {
                    int newPosition;

                    // Ensure the random song picked is not the same as the current song
                    newPosition = random.Next(0, listToPickFrom.Count);
                    

                    // Update current song and add it to the shuffled playlist
                    _currentSongIndex = newPosition;
                    ObservableCurrentlyPlayingSong = listToPickFrom[_currentSongIndex];
                }
                else
                {
                    // Non-shuffle logic for next song
                    _currentSongIndex = (_currentSongIndex + 1) % listToPickFrom.Count;
                    ObservableCurrentlyPlayingSong = listToPickFrom[_currentSongIndex];
                }
            }

            // Previous song requested
            else if (IsPrevious)
            {
                _currentSongIndex--;
                if(_currentSongIndex < 0)
                    _currentSongIndex = listToPickFrom.Count - 1;

                ObservableCurrentlyPlayingSong = listToPickFrom[_currentSongIndex];
            }

            // Optional: Update observable properties or UI
            // UpdateObservableProperties();
        }
        catch (Exception e)
        {
            Debug.WriteLine($"Error message!!:  {e.Message}");
        }
    }
    public async Task<bool> PauseResumeSongAsync(double currentPosition, bool isPause=false)
    {
        ViewModel ??= IPlatformApplication.Current!.Services.GetService<HomePageVM>();
        currentPositionInSec = currentPosition;
        ObservableCurrentlyPlayingSong ??= _nowPlayingSubject.Value.First();
        if (isPause)
        {
            await audioService.PauseAsync();
            ObservableCurrentlyPlayingSong.IsPlaying = false;
            _playerStateSubject.OnNext(MediaPlayerState.Paused);  // Update state to paused
            ViewModel!.SetPlayerState(MediaPlayerState.Paused);

            _positionTimer?.Stop();
        }
        else
        {
            if (!File.Exists(ObservableCurrentlyPlayingSong.FilePath))
            {
                return false;
            }
            var coverImage = GetCoverImage(ObservableCurrentlyPlayingSong.FilePath, true);
            if (ObservableCurrentlyPlayingSong is null)
                return false;

            //ObservableCurrentlyPlayingSong.IsPlaying = true;
            _playerStateSubject.OnNext(MediaPlayerState.Playing);
            ViewModel!.SetPlayerState(MediaPlayerState.Playing);

            if (_positionTimer is null)
            {
                _positionTimer = new System.Timers.Timer(1000);
                _positionTimer.Elapsed += OnPositionTimerElapsed;
                _positionTimer.AutoReset = true;
            }
            _positionTimer?.Start();

            audioService.Initialize(ObservableCurrentlyPlayingSong, coverImage);
            await audioService.ResumeAsync(currentPosition);
            
            ShowMiniPlayBackView();
        }

        return true;
    }

    public async Task<bool> StopSongAsync()
    {
        try
        {
            await audioService.PauseAsync();
            currentPosition = 0;
            CurrentlyPlayingSong.IsPlaying = false;

            _playerStateSubject.OnNext(MediaPlayerState.Stopped);  // Update state to stopped
            ViewModel!.SetPlayerState(MediaPlayerState.Stopped);
            _positionTimer?.Stop();
            _currentPositionSubject.OnNext(new());

            return true;
        }
        catch (Exception ex)
        {

            throw new Exception(ex.Message);
        }
    }

    Stack<int> shuffleHistory = new();


    #region Audio Service Events Region
    private async void AudioService_PlayPrevious(object? sender, EventArgs e)
    {
        await PlayPreviousSongAsync();
    }
    SemaphoreSlim _playLock = new SemaphoreSlim(1, 1);
    private async void AudioService_PlayNext(object? sender, EventArgs e)
    {
        bool isLocked = await _playLock.WaitAsync(0);
        if (!isLocked)
            return;

        try
        {
            if (CurrentRepeatMode == 2) //repeat the same song
            {
                await PlaySongAsync(IsFromPreviousOrNext: true);
                return;
            }

            await PlayNextSongAsync();
        }
        finally
        {
            _playLock.Release();
        }
    }

    private int repeatCountMax;

    private async void AudioService_PlayEnded(object? sender, EventArgs e)
    {
        if (_positionTimer != null)
        {
            _positionTimer.Stop();
            _positionTimer.Elapsed -= OnPositionTimerElapsed;
            _positionTimer.Dispose();
            _positionTimer = null;
        }
        _currentPositionSubject.OnNext(new());
        ObservableCurrentlyPlayingSong.IsCurrentPlayingHighlight = false;
        if (ObservableCurrentlyPlayingSong.DatesPlayedAndWasPlayCompleted != null && ObservableCurrentlyPlayingSong.DatesPlayedAndWasPlayCompleted.Count > 0)
        {
            // Find the most recent entry with `WasPlayCompleted == false` (meaning not completed yet)
            var lastPlayEntry = ObservableCurrentlyPlayingSong.DatesPlayedAndWasPlayCompleted
                .Last();

            if (lastPlayEntry != null)
            {
                // Update the play completion flag to `true`
                lastPlayEntry.WasPlayCompleted = true;  // Mark the song as fully played
            }
            SongsMgtService.UpdateSongDetails(ObservableCurrentlyPlayingSong);
        }


        if (CurrentRepeatMode == 2) // Repeat the same song
        {
            await PlaySongAsync();
        }

        else if (CurrentRepeatMode == 4) // Custom repeat mode
        {
            if (CurrentRepeatCount < repeatCountMax)
            {
                CurrentRepeatCount++;
                Debug.WriteLine($"Repeating song {CurrentRepeatCount}/{repeatCountMax}");
                await PlaySongAsync();
            }
            else
            {
                CurrentRepeatMode = 1;
                CurrentRepeatCount = 1;
                Debug.WriteLine("Finished repeating the song, moving to next song.");
                await PlayNextSongAsync();
            }
        }
        else
        {
            await PlayNextSongAsync();
        }


        // Additional logic to force re-triggering if the app is stuck
        await Task.Delay(2000); // Wait for 2 seconds
        if (!audioService.IsPlaying)
        {
            Debug.WriteLine("Audio service seems stuck, forcing play event.");
            AudioService_PlayEnded(null, EventArgs.Empty); // Force re-triggering if still stuck
        }

    }
    private void AudioService_PlayingChanged(object? sender, bool e)
    {
        if (isSongPlaying != e)
        {
            isSongPlaying = e;
        }

        if (isSongPlaying)
        {
            _positionTimer?.Start();
            _playerStateSubject.OnNext(MediaPlayerState.ShowPauseBtn);  // Update state to playing
        }
        else
        {
            _positionTimer?.Stop();
            _playerStateSubject.OnNext(MediaPlayerState.ShowPlayBtn);
        }
    }
    #endregion
    public async Task<bool> PlayNextSongAsync()
    {
        ObservableCurrentlyPlayingSong.IsCurrentPlayingHighlight = false;
        if (CurrentRepeatMode == 0)
        {
            await PlaySongAsync(currentQueue: CurrentQueue, IsFromPreviousOrNext: true);
            return true;
        }

        GetPrevAndNextSongs(IsNext: true);
        return await PlaySongAsync(ObservableCurrentlyPlayingSong, CurrentQueue, IsFromPreviousOrNext: true);
    }
    public async Task<bool> PlayPreviousSongAsync()
    {
        ObservableCurrentlyPlayingSong.IsCurrentPlayingHighlight = false;

        GetPrevAndNextSongs(IsPrevious: true);
        return await PlaySongAsync(ObservableCurrentlyPlayingSong, CurrentQueue, IsFromPreviousOrNext: true);
    }
    private ObservableCollection<SongModelView>? GetCurrentList(int currentQueue, ObservableCollection<SongModelView>? secQueueSongs = null)
    {

        return currentQueue switch
        {
            0 => _nowPlayingSubject.Value,
            1 => secQueueSongs ?? _secondaryQueueSubject.Value,
            2 => _tertiaryQueueSubject.Value,
            _ => _nowPlayingSubject.Value,
        };
    }

    public async Task SetSongPosition(double positionInSec)
    {
        if (ObservableCurrentlyPlayingSong is null)
        {
            return;
        }

        currentPositionInSec = positionInSec;

        if (audioService.IsPlaying)
        {
            await audioService.SetCurrentTime(positionInSec);
        }

        

    }
    public void ChangeVolume(double newPercentageValue)
    {
        try
        {
            if (CurrentlyPlayingSong is null)
            {
                return;
            }
            audioService.Volume = newPercentageValue;

            AppSettingsService.VolumeSettingsPreference.SetVolumeLevel(newPercentageValue);
        }
        catch (Exception ex)
        {
            Debug.WriteLine("No Volume modified. Possible null exception ", ex.Message);
        }
    }

    public void DecreaseVolume()
    {
        audioService.Volume -= 0.1;
    }

    public void IncreaseVolume()
    {
        audioService.Volume += 0.1;
    }


    public void ToggleShuffle(bool isShuffleOn)
    {
        IsShuffleOn = isShuffleOn;
        AppSettingsService.ShuffleStatePreference.ToggleShuffleState(isShuffleOn);
        //GetPrevAndNextSongs();
    }
    public int ToggleRepeatMode()
    {
        CurrentRepeatMode = (CurrentRepeatMode + 1) % 3;
        switch (CurrentRepeatMode)
        {
            case 0:
                CurrentRepeatMode = 0;
                break;
            case 1:
                CurrentRepeatMode = 1;
                break;
            case 2:
                CurrentRepeatMode = 2;
                break;
            default:
                break;
        }
        AppSettingsService.RepeatModePreference.ToggleRepeatState();
        return CurrentRepeatMode;
    }

    double currentPositionInSec = 0;
    PlaybackInfo CurrentPlayBackInfo;
    private void OnPositionTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        currentPositionInSec++;

        double totalDurationInSeconds = ObservableCurrentlyPlayingSong.DurationInSeconds;
        double percentagePlayed = currentPositionInSec / totalDurationInSeconds;

        if (CurrentPlayBackInfo is null)
        {
            CurrentPlayBackInfo = new PlaybackInfo
            {
                CurrentPercentagePlayed = percentagePlayed, //this is the value of that CurrentPosition then takes
                CurrentTimeInSeconds = currentPositionInSec
            };
        }
        else
        {
            CurrentPlayBackInfo.CurrentPercentagePlayed = percentagePlayed;
            CurrentPlayBackInfo.CurrentTimeInSeconds = currentPositionInSec;
        }

        _currentPositionSubject.OnNext(CurrentPlayBackInfo);
    }

    double currentPosition = 0;
    #endregion

    //ObjectId PreviouslyLoadedPlaylist;
    public int CurrentQueue { get; set; } = 0; //0 = main queue, 1 = playlistQ, 2 = externallyloadedsongs Queue

    public void UpdateCurrentQueue(IList<SongModelView> songs, int QueueNumber = 1) //0 = main queue, 1 = playlistQ, 2 = externallyloadedsongs Queue
    {
        CurrentQueue = QueueNumber;
        _secondaryQueueSubject.OnNext(value: songs.ToObservableCollection());
    }
    public void UpdateSongToFavoritesPlayList(SongModelView song)
    {
        if (song is not null)
        {
            SongsMgtService.UpdateSongDetails(song);
        }
    }
    public void AddSongToQueue(SongModelView song)
    {
        var list = _nowPlayingSubject.Value;
        list.Add(song);
        _nowPlayingSubject.OnNext(list);
    }
    public void RemoveSongFromQueue(SongModelView song)
    {
        var list = _nowPlayingSubject.Value;
        list.Remove(song);
        _nowPlayingSubject.OnNext(list);
    }

    #region Region Search

    Dictionary<string, string> normalizationCache = new();
    List<SongModelView> SearchedSongsList;

    public void SearchSong(string songTitleOrArtistName, List<string>? selectedFilters, int Rating)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(songTitleOrArtistName) && selectedFilters?.Count == 0)
            {
                ResetSearch();
                return;
            }

            // Normalize the search term
            string normalizedSearchTerm = NormalizeAndCache(songTitleOrArtistName ?? string.Empty).ToLowerInvariant();

            // Clear the search result list
            SearchedSongsList?.Clear();

            // Step 1: Start with all songs and apply the rating filter
            var filteredSongs = SongsMgtService.AllSongs
                .Where(s => s.Rating >= Rating);

            // Step 2: Apply additional filters from selectedFilters if any
            if (selectedFilters?.Count > 0)
            {
                filteredSongs = filteredSongs.Where(s => selectedFilters.Contains("Artist")
                                                        || selectedFilters.Contains("Album")
                                                        || selectedFilters.Contains("Genre"));
            }

            // Step 3: Perform the search with normalization and comparison on the filtered list
            SearchedSongsList = filteredSongs
                .Where(s => NormalizeAndCache(s.Title).ToLowerInvariant().Contains(normalizedSearchTerm)
                            || (s.ArtistName != null && NormalizeAndCache(s.ArtistName).ToLowerInvariant().Contains(normalizedSearchTerm))
                            || (s.AlbumName != null && NormalizeAndCache(s.AlbumName).ToLowerInvariant().Contains(normalizedSearchTerm)))
                .ToList();

            // Step 4: Load the results with sorting
            Debug.WriteLine(SearchedSongsList.Count + "Search Count");
            LoadSongsWithSorting(SearchedSongsList.ToObservableCollection(), true);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }


    private string NormalizeAndCache(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        // Retrieve from cache if already normalized
        if (normalizationCache.TryGetValue(text, out string? cachedValue))
        {
            return cachedValue;
        }

        // Normalize the string (spaces are preserved)
        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in normalizedString)
        {
            // Retain characters that are not non-spacing marks
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        // Convert back to Form C and cache the result
        var result = stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        normalizationCache[text] = result;
        return result;
    }

    void ResetSearch()
    {
        SearchedSongsList?.Clear();
        LoadSongsWithSorting();
    }


    #endregion

    public void LoadFirstPlaylist()
    {
        var firstPlaylist = PlaylistManagementService?.AllPlaylists?.ToList();
        if (firstPlaylist is not null && firstPlaylist.Count > 0)
        {           
            SelectedPlaylistName = firstPlaylist.First().Name;
            GetSongsFromPlaylistID(firstPlaylist.FirstOrDefault().Id);
        }
    }

    public void AddSongToPlayListWithPlayListID(SongModelView song, PlaylistModelView playlistModel)
    {
        var anyExistingPlaylist = PlaylistManagementService.AllPlaylists.FirstOrDefault(x=>x.Name == playlistModel.Name);
        if (anyExistingPlaylist is not null)
        {
            playlistModel = anyExistingPlaylist;
        }
        else
        {
            playlistModel.Id = ObjectId.GenerateNewId();
            playlistModel.Name = playlistModel.Name;
                playlistModel.DateCreated = DateTimeOffset.Now;
            playlistModel.TotalSongsCount = 1;
            playlistModel.TotalDuration = song.DurationInSeconds;
            playlistModel.TotalSize = song.FileSize;
        
        }
        var newPlaylistSongLinkByUserManual = new PlaylistSongLink()
        {
            Id = ObjectId.GenerateNewId(),
            PlaylistId = playlistModel.Id,
            SongId = song.Id,
        };

        PlaylistManagementService.UpdatePlayList(playlistModel, newPlaylistSongLinkByUserManual, true);
        
        //GetAllPlaylists(); it's called in HomePageVM but let's see
    }

    public void RemoveSongFromPlayListWithPlayListID(SongModelView song, ObjectId playlistID)
    {
        var playlists = PlaylistManagementService.GetPlaylists();
        var specificPlaylist = playlists.FirstOrDefault(x => x.Id == playlistID);
        if (specificPlaylist is not null)
        {
            var songsInPlaylist = _secondaryQueueSubject.Value;
            songsInPlaylist.Remove(song);
            _secondaryQueueSubject.OnNext(songsInPlaylist);
            specificPlaylist.TotalSongsCount -= 1;
            specificPlaylist.TotalDuration -= song.DurationInSeconds;
            specificPlaylist.TotalSize -= song.FileSize;
        }

        PlaylistManagementService.UpdatePlayList(specificPlaylist, IsRemoveSong: true);
    }

    public List<SongModelView> GetSongsFromPlaylistID(ObjectId playlistID)
    {
        try
        {
            var specificPlaylist = PlaylistManagementService.AllPlaylists.FirstOrDefault(x => x.Id == playlistID);
            var songsIdsFromPL = new HashSet<ObjectId>(PlaylistManagementService.GetSongsIDsFromPlaylistID(specificPlaylist.Id));
            var songsFromPlaylist = SongsMgtService.AllSongs;
            List<SongModelView> songsinpl = new();
            songsinpl?.Clear();
            foreach (var song in songsFromPlaylist)
            {
                if (songsIdsFromPL.Contains(song.Id))
                {
                    songsinpl.Add(song);
                }
            }
            SelectedPlaylistName = specificPlaylist.Name;
            
            if (songsinpl is null)
            {
                return Enumerable.Empty<SongModelView>().ToList();
            }
            _secondaryQueueSubject.OnNext(songsinpl.ToObservableCollection());
            return songsinpl;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting songs from playlist: {ex.Message}");
            return Enumerable.Empty<SongModelView>().ToList();
        }

    }

    public ObservableCollection<PlaylistModelView> GetAllPlaylists()
    {
        PlaylistManagementService.GetPlaylists();
        AllPlaylists = new ObservableCollection<PlaylistModelView>(PlaylistManagementService.AllPlaylists);
        return AllPlaylists;
    }

    public ObservableCollection<ArtistModelView> GetAllArtists()
    {
        ArtistsMgtService.GetArtists();
        if (ArtistsMgtService.AllArtists is null)
            return Enumerable.Empty<ArtistModelView>().ToObservableCollection();

        AllArtists = new ObservableCollection<ArtistModelView>(ArtistsMgtService.AllArtists);
        return AllArtists;
    }
    public ObservableCollection<AlbumModelView> GetAllAlbums()
    {
        SongsMgtService.GetAlbums();
        AllAlbums = new ObservableCollection<AlbumModelView>(SongsMgtService.AllAlbums);
        return AllAlbums;
    }
    public IList<SongModelView> GetSongsFromArtistID(ObjectId artistID)
    {
        try
        {
            var specificArtist = ArtistsMgtService.AllArtists.FirstOrDefault(x => x.Id == artistID);
            var songsIDsFromArtists = new HashSet<ObjectId>(ArtistsMgtService.GetSongsIDsFromArtistID(specificArtist.Id));
            var songsFromArtist = SongsMgtService.AllSongs;
            List<SongModelView> songsfromartist = new();
            songsfromartist?.Clear();
            foreach (var song in songsFromArtist)
            {
                if (songsIDsFromArtists.Contains(song.Id))
                {
                    songsfromartist.Add(song);
                }
            }
            return songsfromartist ?? Enumerable.Empty<SongModelView>().ToList();
            ;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting songs from playlist: {ex.Message}");
            return Enumerable.Empty<SongModelView>().ToList();
        }
    }

    public ObservableCollection<SongModelView> GetAllArtistsAlbumSongsAlbumID(ObjectId albumID)
    {
        try
        {
            var artistID = SongsMgtService.AllLinks.FirstOrDefault(x=>x.AlbumId == albumID).ArtistId;

            var allSongIDsLinkedToArtistID = SongsMgtService.AllLinks.
                Where(x => x.ArtistId == artistID)
                .Select(x => x.SongId)
                .ToList();

            var allSongsLinkedToArtist = SongsMgtService.AllSongs
                .Where(song => allSongIDsLinkedToArtistID.Contains(song.Id))
                .ToObservableCollection();

            return allSongsLinkedToArtist;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting songs from album: {ex.Message}");
            return (Enumerable.Empty<SongModelView>().ToObservableCollection());
        }
    }

    public ObservableCollection<SongModelView> GetallArtistsSongsByArtistId(ObjectId artistID)
    {
        try
        {
            ObservableCollection<SongModelView> songsFromArtist = new();

            // Get all song IDs associated with the artist
            
            var songsIDsFromArtist = ArtistsMgtService.GetSongsIDsFromArtistID(artistID);

            // Add each song found by its ID
            foreach (var songId in songsIDsFromArtist)
            {
                var song = SongsMgtService.AllSongs.FirstOrDefault(s => s.Id == songId);
                if (song != null)
                {
                    songsFromArtist.Add(song);  // Assuming AllSongs returns SongsModelView objects
                }
            }

            return songsFromArtist;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting songs by artist ID: {ex.Message}");
            return (Enumerable.Empty<SongModelView>().ToObservableCollection());
        }
    }


    public bool DeletePlaylistThroughID(ObjectId playlistID)
    {
        //TODO: DO THIS TOO
        throw new NotImplementedException();
    }

    public async Task DeleteSongFromHomePage(SongModelView song)
    {
        // Get the current list from the subject
        var list = nowPlayingShuffledOrNotSubject.ToList();

        // Find the index of the song using its unique ID
        var index = list.FindIndex(x => x.Id == song.Id); // Assuming each song has a unique Id

        if (index != -1) // If the song was found
        {
            list.RemoveAt(index); // Remove the song at the found index

            // Push the updated list back into the subject
            _nowPlayingSubject.OnNext(list.ToObservableCollection());
            nowPlayingShuffledOrNotSubject = list.ToObservableCollection();
        }


        await SongsMgtService.DeleteSongFromDB(song.Id);
        _nowPlayingSubject.OnNext(SongsMgtService.AllSongs.ToObservableCollection());
    }
    public async Task MultiDeleteSongFromHomePage(ObservableCollection<SongModelView> songs)
    {
        // Get the current list from the subject
        var list = _nowPlayingSubject.Value;

        // Filter out all songs in the passed collection
        list = list.Where(x => !songs.Any(s => s.Id == x.Id)).ToObservableCollection();
        
        _nowPlayingSubject.OnNext(list.ToObservableCollection());        

        // Delete the songs from the database
        await SongsMgtService.MultiDeleteSongFromDB(songs);

        // Update the _nowPlayingSubject with the latest list of songs
        _nowPlayingSubject.OnNext(SongsMgtService.AllSongs.ToObservableCollection());
    }

}