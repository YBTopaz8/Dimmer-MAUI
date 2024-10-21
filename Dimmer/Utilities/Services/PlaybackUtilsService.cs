namespace Dimmer_MAUI.Utilities.Services;
public partial class PlaybackUtilsService : ObservableObject, IPlaybackUtilsService
{

    INativeAudioService audioService;
    public IObservable<ObservableCollection<SongsModelView>> NowPlayingSongs => _nowPlayingSubject.AsObservable();
    BehaviorSubject<ObservableCollection<SongsModelView>> _nowPlayingSubject = new([]);
    public IObservable<ObservableCollection<SongsModelView>> SecondaryQueue => _secondaryQueueSubject.AsObservable();
    BehaviorSubject<ObservableCollection<SongsModelView>> _secondaryQueueSubject = new([]);
    public IObservable<ObservableCollection<SongsModelView>> TertiaryQueue => _tertiaryQueueSubject.AsObservable();
    BehaviorSubject<ObservableCollection<SongsModelView>> _tertiaryQueueSubject = new([]);

    public IObservable<MediaPlayerState> PlayerState => _playerStateSubject.AsObservable();
    BehaviorSubject<MediaPlayerState> _playerStateSubject = new(MediaPlayerState.Stopped);
    public IObservable<PlaybackInfo> CurrentPosition => _currentPositionSubject.AsObservable();
    BehaviorSubject<PlaybackInfo> _currentPositionSubject = new(new());
    System.Timers.Timer _positionTimer;

    [ObservableProperty]
    private SongsModelView observableCurrentlyPlayingSong;
    public SongsModelView CurrentlyPlayingSong => ObservableCurrentlyPlayingSong;
    [ObservableProperty]
    private SongsModelView observablePreviouslyPlayingSong;
    public SongsModelView PreviouslyPlayingSong => ObservablePreviouslyPlayingSong;
    [ObservableProperty]
    private SongsModelView observableNextPlayingSong;
    public SongsModelView NextPlayingSong => ObservableNextPlayingSong;

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
    public IDiscordRPC DiscordRPC { get; }
    [ObservableProperty]
    ObservableCollection<PlaylistModelView> allPlaylists;
    [ObservableProperty]
    ObservableCollection<ArtistModelView> allArtists;
    [ObservableProperty]
    ObservableCollection<AlbumModelView> allAlbums;

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
        IArtistsManagementService artistsMgtService, IDiscordRPC discordRPC)
    {
        this.SongsMgtService = SongsMgtService;
        StatsMgtService = statsMgtService;
        PlaylistManagementService = playlistManagementService;
        ArtistsMgtService = artistsMgtService;
        audioService = AudioService;
        DiscordRPC = discordRPC;

        audioService.PlayPrevious += AudioService_PlayPrevious;
        audioService.PlayNext += AudioService_PlayNext;
        audioService.IsPlayingChanged += AudioService_PlayingChanged;
        audioService.PlayEnded += AudioService_PlayEnded;
        audioService.IsSeekedFromNotificationBar += AudioService_IsSeekedFromNotificationBar;

        IsShuffleOn = AppSettingsService.ShuffleStatePreference.GetShuffleState();
        CurrentRepeatMode = AppSettingsService.RepeatModePreference.GetRepeatState();

        CurrentQueue = 0; //0 = main queue, 1 = playlistQ, 2 = externallyloadedsongs Queue
        LoadSongsWithSorting();

        LoadLastPlayedSong();
        LoadFirstPlaylist();
        AllPlaylists = PlaylistManagementService.AllPlaylists?.ToObservableCollection();
        AllArtists = ArtistsMgtService.AllArtists?.ToObservableCollection();
    }

    private void AudioService_IsSeekedFromNotificationBar(object? sender, long e)
    {
        currentPositionInSec = e/1000;
    }


    #region Setups/Loadings Region

    ObservableCollection<SongsModelView> nowPlayingShuffledOrNot = new ObservableCollection<SongsModelView>(); //the collection used to index next song without changing on ui

    private Dictionary<string, ArtistModelView> artistDict = new Dictionary<string, ArtistModelView>();
    HomePageVM? ViewModel { get; set; }
    private (List<ArtistModelView>?, List<AlbumModelView>?, List<AlbumArtistSongLink>?, List<SongsModelView>?, 
        List<GenreModelView>?, List<AlbumArtistGenreSongLink>?) LoadSongs(List<string> folderPaths)
    {
        try
        {
            var allFiles = GetAllFiles(folderPaths);

            if (allFiles.Count == 0)
            {
                return (null, null, null, null, null, null);
            }

            // Fetch existing data from services
            var existingArtists = ArtistsMgtService.AllArtists;
            var existingLinks = ArtistsMgtService.AlbumsArtistsSongLink;
            var existingAlbums = SongsMgtService.AllAlbums;
            var existingGenres = SongsMgtService.AllGenres is null? [] : SongsMgtService.AllGenres;
            var oldSongs = SongsMgtService.AllSongs ?? new List<SongsModelView>();

            // Initialize collections and dictionaries
            var newArtists = new List<ArtistModelView>();
            var newAlbums = new List<AlbumModelView>();
            var newLinks = new List<AlbumArtistSongLink>();
            var newGenres = new List<GenreModelView>();
            var genreLinks = new List<AlbumArtistGenreSongLink>(); // New genre link collection
            var allSongs = new List<SongsModelView>();

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
                if (processedFiles == 50)
                {
                    _nowPlayingSubject.OnNext(allSongs.ToObservableCollection());
                }
                ObservableLoadingSongsProgress = processedFiles * 100 / totalFiles;
            }

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
    private SongsModelView? ProcessFile(
    string file,
    List<AlbumModelView> existingAlbums,
    Dictionary<string, AlbumModelView> albumDict,
    List<AlbumModelView> newAlbums,
    List<SongsModelView> oldSongs,
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
        Track track = new(file);

        string title = track.Title.Contains(';') ? track.Title.Split(';')[0].Trim() : track.Title;
        string albumName = string.IsNullOrEmpty(track.Album?.Trim()) ? track.Title : track.Album?.Trim();
        
        AlbumModelView album = GetOrCreateAlbum(albumName, existingAlbums, albumDict, newAlbums);

        // Extract artist names
        var artistNames = GetArtistNames(track.Artist, track.AlbumArtist);
        string mainArtistName = string.Join(", ", artistNames);

        var song = CreateSongModel(track, title, albumName, mainArtistName, file);

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
        if (!string.IsNullOrEmpty(genreName))
        {
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


    private void CreateGenreLink(GenreModelView genre, SongsModelView song)
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
        if (string.IsNullOrEmpty(albumName))
        {
            return null;
        }

        if (!albumDict.TryGetValue(albumName, out var album))
        {
            album = new AlbumModelView
            {
                Id = ObjectId.GenerateNewId(),
                Name = albumName,
                AlbumImagePath = null // Default value, will be updated later
            };
            albumDict[albumName] = album;

            // Check if the album already exists in the database
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
      SongsModelView song,
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

    private SongsModelView CreateSongModel(
    Track track,
    string title,
    string albumName,
    string artistName,
    string filePath)
    {
        FileInfo fileInfo = new(filePath);

        var song = new SongsModelView
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



    public async Task<bool> LoadSongsFromFolder(List<string> folderPaths)
    {
        isLoadingSongs=true;
        ViewModel = IPlatformApplication.Current.Services.GetService<HomePageVM>();

        // Load songs from the folders asynchronously
        (var allArtists, var allAlbums, var allLinks, var songs
            , var allGenres, var genreLinks) = await Task.Run(() => LoadSongs(folderPaths));

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
    List<AlbumArtistSongLink> allLinks, List<SongsModelView> songs, List<GenreModelView> allGenres, List<AlbumArtistGenreSongLink> genreLinks)
    {
        List<SongsModel> dbSongs = songs?.Select(song => new SongsModel(song)).ToList();
        ArtistsMgtService.AddSongToArtistWithArtistIDAndAlbumAndGenre(allArtists, allAlbums, 
            allLinks, dbSongs, allGenres, genreLinks);

    }


    private void LoadSongsWithSorting(ObservableCollection<SongsModelView>? songss = null)
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

    public SongsModelView lastPlayedSong { get; set; }

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
            _currentSongIndex = _nowPlayingSubject.Value.IndexOf(ObservableCurrentlyPlayingSong);
        }
        GetPrevAndNextSongs();
        _playerStateSubject.OnNext(MediaPlayerState.Initialized);
    }
    static string? GetCoverImagePath(string filePath)
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
            return imageFiles[0];
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
    public async Task<bool> PlaySelectedSongsOutsideAppAsync(string[] filePaths)
    {
        // Filter the array to include only specific file extensions
        var filteredFiles = filePaths.Where(path => path.EndsWith(".mp3") || path.EndsWith(".flac") || path.EndsWith(".wav") || path.EndsWith(".m4a")).ToArray();
        var existingSongDictionary = _tertiaryQueueSubject.Value.ToDictionary(song => song.FilePath, song => song, StringComparer.OrdinalIgnoreCase);

        var allSongs = new ObservableCollection<SongsModelView>();
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
                var newSong = new SongsModelView
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
        await PlaySongAsync(allSongs[0], 2, _tertiaryQueueSubject.Value);

        return true;
    }
    //private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);  // Initialize with a count of 1
    bool isLoadingSongs;
    public async Task<bool> PlaySongAsync(SongsModelView? song = null, int currentQueue = 0,
        ObservableCollection<SongsModelView>? currentList = null, double positionInSec = 0,
        int repeatMode = 0, int repeatMaxCount = 0,
        bool IsFromPreviousOrNext = false)
    {
        ViewModel ??= IPlatformApplication.Current!.Services.GetService<HomePageVM>();
        CurrentQueue = currentQueue;

        if (_positionTimer != null)
        {
            _positionTimer.Stop();
            _positionTimer.Elapsed -= OnPositionTimerElapsed;
            _positionTimer.Dispose();
            _positionTimer = null;
        }
        switch (CurrentQueue)
        {
            case 1:
                _secondaryQueueSubject.OnNext(currentList);

                break;
            case 2:
                _tertiaryQueueSubject.OnNext(currentList);
                break;
            default:
                break;
        }

        if (ObservableCurrentlyPlayingSong != null)
        {
            ObservableCurrentlyPlayingSong.IsPlaying = false;
        }
        else
        {
            ObservableCurrentlyPlayingSong = _nowPlayingSubject.Value.FirstOrDefault();
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
                }
                song.IsPlaying = true;
                ObservableCurrentlyPlayingSong = song!;
                if (currentList == null)
                {
                    int songIndex = _nowPlayingSubject.Value.IndexOf(song);
                    if (songIndex != -1)
                    {
                        _currentSongIndex = songIndex;
                    }
                }
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

            await audioService.InitializeAsync(ObservableCurrentlyPlayingSong, coverImage);

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

            // Add current time with 'false' for incomplete play
            ObservableCurrentlyPlayingSong.DatesPlayedAndWasPlayCompleted ??= new();
            ObservableCurrentlyPlayingSong.DatesPlayedAndWasPlayCompleted.Add(new PlayDateAndIsPlayCompletedModelView
            {
                DatePlayed = DateTimeOffset.Now,
                WasPlayCompleted = false // Mark as incomplete
            });
            // Add the current song to the history before moving to the next song
            if (!playedSongsHistoryIndexes.Contains(_currentSongIndex))
            {
                playedSongsHistoryIndexes.Add(_currentSongIndex);
            }

            ViewModel.SetPlayerState(MediaPlayerState.Playing);
            _playerStateSubject.OnNext(MediaPlayerState.Playing);
            ViewModel.SetPlayerState(MediaPlayerState.RefreshStats);

            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Error message!!:  " + ex.Message);

            await Shell.Current.DisplayAlert("Error message", ex.Message, "Ok");
            return false;
        }
        finally
        {
            if (File.Exists(ObservableCurrentlyPlayingSong.FilePath) && ObservableCurrentlyPlayingSong != null && currentQueue != 2)
            {
                DiscordRPC.UpdatePresence(ObservableCurrentlyPlayingSong,
                TimeSpan.FromSeconds(ObservableCurrentlyPlayingSong.DurationInSeconds),
                TimeSpan.Zero);
                ObservableCurrentlyPlayingSong.IsPlaying = true;
                SongsMgtService.UpdateSongDetails(ObservableCurrentlyPlayingSong);
                _currentPositionSubject.OnNext(new());
            }
#if WINDOWS
            MiniPlayBackControlNotif.ShowUpdateMiniView(ObservableCurrentlyPlayingSong);
#endif
        }
    }

    //private void GetPrevAndNextSongs(bool IsNext = false, bool IsPrevious = false)
    //{


    //    var currentList = nowPlayingShuffledOrNot;
    //    if (currentList == null || currentList.Count == 0)
    //        return;


    //    ObservablePreviouslyPlayingSong = ObservableCurrentlyPlayingSong;

    //    int prevIndex = (_currentSongIndex > 0) ? _currentSongIndex - 1 : currentList.Count - 1;
    //    int nextIndex = (_currentSongIndex < currentList.Count - 1) ? _currentSongIndex + 1 : 0;

    //    if (IsNext)
    //    {
    //        ObservablePreviouslyPlayingSong = ObservableCurrentlyPlayingSong;
    //        ObservableNextPlayingSong = currentList[nextIndex];
    //    }
    //    else if (IsPrevious)
    //    {
    //        ObservableNextPlayingSong = ObservableCurrentlyPlayingSong;
    //        ObservablePreviouslyPlayingSong = currentList[prevIndex];
    //    }

    //}
    // List to store the order of played song indexes
    private List<int> playedSongsHistoryIndexes = new();

    private void GetPrevAndNextSongs(bool IsNext = false, bool IsPrevious = false)
    {
        bool isTrueShuffle = false;
        var currentList = nowPlayingShuffledOrNot;
        if (currentList == null || currentList.Count == 0)
            return;

        int prevIndex = (_currentSongIndex > 0) ? _currentSongIndex - 1 : currentList.Count - 1;
        int nextIndex = (_currentSongIndex < currentList.Count - 1) ? _currentSongIndex + 1 : 0;

        if (IsNext)
        {
            if (isTrueShuffle)
            {
                // Shuffle on: Pick a random next song
                _currentSongIndex = new Random().Next(currentList.Count);
            }
            else
            {
                // Add the current song to the history before moving to the next song
                if (!playedSongsHistoryIndexes.Contains(_currentSongIndex))
                {
                    playedSongsHistoryIndexes.Add(_currentSongIndex);
                }

                // Regular next song logic
                _currentSongIndex = nextIndex;
            }

            ObservablePreviouslyPlayingSong = ObservableCurrentlyPlayingSong;
            ObservableNextPlayingSong = currentList[_currentSongIndex];
        }
        else if (IsPrevious)
        {
            if (isTrueShuffle)
            {
                // Shuffle on: Pick a random previous song
                _currentSongIndex = new Random().Next(currentList.Count);
            }
            else
            {
                // Shuffle off: Use the played song history to go back
                if (playedSongsHistoryIndexes.Count > 0)
                {
                    // Get the last played song index from the history
                    _currentSongIndex = playedSongsHistoryIndexes.Last();
                    playedSongsHistoryIndexes.RemoveAt(playedSongsHistoryIndexes.Count - 1); // Remove it from history
                }
                else
                {
                    // If no history, fallback to the regular previous song logic
                    _currentSongIndex = prevIndex;
                }
            }

            ObservableNextPlayingSong = ObservableCurrentlyPlayingSong;
            ObservablePreviouslyPlayingSong = currentList[_currentSongIndex];
        }
    }



    public async Task<bool> PauseResumeSongAsync(double currentPosition)
    {
        ViewModel ??= IPlatformApplication.Current.Services.GetService<HomePageVM>();
        currentPositionInSec = currentPosition;
        if (ObservableCurrentlyPlayingSong is null)
        {
            await PlaySongAsync();
            return true;
        }
        if (currentPositionInSec == 0 && !audioService.IsPlaying)
        {
            await PlaySongAsync(ObservableCurrentlyPlayingSong);

            return true;
        }
        if (audioService.IsPlaying)
        {
            await audioService.PauseAsync();
            ObservableCurrentlyPlayingSong.IsPlaying = false;
            _playerStateSubject.OnNext(MediaPlayerState.Paused);  // Update state to paused
            ViewModel.SetPlayerState(MediaPlayerState.Paused);

            _positionTimer?.Stop();
        }
        else
        {
            if (!File.Exists(ObservableCurrentlyPlayingSong.FilePath))
            {
                return false;
            }
            var coverImage = GetCoverImage(ObservableCurrentlyPlayingSong.FilePath, true);
            
            await audioService.InitializeAsync(ObservableCurrentlyPlayingSong, coverImage);


            await audioService.ResumeAsync(currentPosition);
            DiscordRPC.UpdatePresence(ObservableCurrentlyPlayingSong, 
                TimeSpan.FromSeconds(ObservableCurrentlyPlayingSong.DurationInSeconds),
                TimeSpan.FromSeconds(currentPosition));
#if WINDOWS
            MiniPlayBackControlNotif.ShowUpdateMiniView(ObservableCurrentlyPlayingSong);
#endif
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
            ViewModel.SetPlayerState(MediaPlayerState.Stopped);
            _positionTimer.Stop();
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
            AudioService_PlayEnded(null, null); // Force re-triggering if still stuck
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
            DiscordRPC.ClearPresence();
        }
    }
    #endregion
    public async Task<bool> PlayNextSongAsync()
    {
        if (CurrentRepeatMode == 0)
        {
            await PlaySongAsync(currentQueue: CurrentQueue, IsFromPreviousOrNext: true);
            return true;
        }

        GetPrevAndNextSongs(IsNext: true);
        return await PlaySongAsync(ObservableNextPlayingSong, CurrentQueue, IsFromPreviousOrNext: true);
    }
    public async Task<bool> PlayPreviousSongAsync()
    {
        GetPrevAndNextSongs(IsPrevious: true);
        return await PlaySongAsync(ObservablePreviouslyPlayingSong, CurrentQueue, IsFromPreviousOrNext: true);
    }
    private ObservableCollection<SongsModelView>? GetCurrentList(int currentQueue, ObservableCollection<SongsModelView>? secQueueSongs = null)
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

        DiscordRPC.UpdatePresence(ObservableCurrentlyPlayingSong,
                TimeSpan.FromSeconds(ObservableCurrentlyPlayingSong.DurationInSeconds),
                TimeSpan.FromSeconds(positionInSec));

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

        switch (CurrentQueue)
        {
            case 0:
                nowPlayingShuffledOrNot = IsShuffleOn is true ? _nowPlayingSubject.Value
                    .OrderBy(x => Guid.NewGuid())
                    .ToObservableCollection() : _nowPlayingSubject.Value.ToObservableCollection();
                break;
            case 1:
                nowPlayingShuffledOrNot = IsShuffleOn is true ? _secondaryQueueSubject.Value
                    .OrderBy(x => Guid.NewGuid())
                    .ToObservableCollection() : _nowPlayingSubject.Value.ToObservableCollection();
                break;
            case 2:
                nowPlayingShuffledOrNot = IsShuffleOn is true ? _tertiaryQueueSubject.Value
                    .OrderBy(x => Guid.NewGuid())
                    .ToObservableCollection() : _nowPlayingSubject.Value.ToObservableCollection();
                break;
            default:
                break;
        }
            
        
        
        AppSettingsService.ShuffleStatePreference.ToggleShuffleState(isShuffleOn);
        GetPrevAndNextSongs();
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

    public void UpdateCurrentQueue(IList<SongsModelView> songs, int QueueNumber = 1) //0 = main queue, 1 = playlistQ, 2 = externallyloadedsongs Queue
    {
        CurrentQueue = QueueNumber;
        _secondaryQueueSubject.OnNext(songs.ToObservableCollection());
    }
    public void UpdateSongToFavoritesPlayList(SongsModelView song)
    {
        if (song is not null)
        {
            SongsMgtService.UpdateSongDetails(song);
        }
    }
    public void AddSongToQueue(SongsModelView song)
    {
        var list = _nowPlayingSubject.Value;
        list.Add(song);
        _nowPlayingSubject.OnNext(list);
    }
    public void RemoveSongFromQueue(SongsModelView song)
    {
        var list = _nowPlayingSubject.Value;
        list.Remove(song);
        _nowPlayingSubject.OnNext(list);
    }

    #region Region Search

    Dictionary<string, string> normalizationCache = new();
    List<SongsModelView> SearchedSongsList;
    public void SearchSong(string songTitleOrArtistName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(songTitleOrArtistName))
            {
                ResetSearch();
                return;
            }

            // Normalize the search term
            string normalizedSearchTerm = NormalizeAndCache(songTitleOrArtistName).ToLowerInvariant();

            // Clear the search result list
            SearchedSongsList?.Clear();

            // Perform the search with proper normalization and comparison
            SearchedSongsList = SongsMgtService.AllSongs
            .Where(s => NormalizeAndCache(s.Title).ToLowerInvariant().Contains(normalizedSearchTerm)
                        || (s.ArtistName != null && NormalizeAndCache(s.ArtistName).ToLowerInvariant().Contains(normalizedSearchTerm))
                        || (s.AlbumName != null && NormalizeAndCache(s.AlbumName).ToLowerInvariant().Contains(normalizedSearchTerm)))
            .ToList();

            LoadSongsWithSorting(SearchedSongsList.ToObservableCollection());

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

    public void AddSongToPlayListWithPlayListName(SongsModelView song, string playlistName)
    {
        PlaylistManagementService.AddSongToPlayListWithPlayListName(song, playlistName);
        //GetAllPlaylists(); it's called in HomePageVM but let's see
    }

    public void RemoveSongFromPlayListWithPlayListID(SongsModelView song, ObjectId playlistID)
    {

        //TODO: DO THIS
    }

    public void GetSongsFromPlaylistID(ObjectId playlistID)
    {
        try
        {
            var specificPlaylist = PlaylistManagementService.AllPlaylists.FirstOrDefault(x => x.Id == playlistID);
            var songsIdsFromPL = new HashSet<ObjectId>(PlaylistManagementService.GetSongsIDsFromPlaylistID(specificPlaylist.Id));
            var songsFromPlaylist = SongsMgtService.AllSongs;
            ObservableCollection<SongsModelView> songsinpl = new();
            songsinpl?.Clear();
            foreach (var song in songsFromPlaylist)
            {
                if (songsIdsFromPL.Contains(song.Id))
                {
                    songsinpl.Add(song);
                }
            }
            SelectedPlaylistName = specificPlaylist.Name;
            _secondaryQueueSubject.OnNext(songsinpl);

        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting songs from playlist: {ex.Message}");
        }

    }
    public void AddSongToPlayListWithPlayListID(SongsModelView song, ObjectId playlistID)
    {
        var specificPlaylist = AllPlaylists.FirstOrDefault(x => x.Id == playlistID);
        if (specificPlaylist is null)
        {
            return;
        }
        //specificPlaylist?.SongsIDs.Add(song.Id);        
        specificPlaylist.TotalSongsCount += 1;
    }

    public void RemoveSongFromPlayListWithPlayListName(SongsModelView song, string playlistName)
    {
        var specificPlaylist = AllPlaylists.FirstOrDefault(x => x.Name == playlistName);
        if (specificPlaylist is not null)
        {
            var songsInPlaylist = _secondaryQueueSubject.Value;
            songsInPlaylist.Remove(song);
            _secondaryQueueSubject.OnNext(songsInPlaylist);
            specificPlaylist.TotalSongsCount -= 1;
        }
        PlaylistManagementService.RemoveSongFromPlayListWithPlayListName(song, playlistName);
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
    public IList<SongsModelView> GetSongsFromArtistID(ObjectId artistID)
    {
        try
        {
            var specificArtist = ArtistsMgtService.AllArtists.FirstOrDefault(x => x.Id == artistID);
            var songsIDsFromArtists = new HashSet<ObjectId>(ArtistsMgtService.GetSongsIDsFromArtistID(specificArtist.Id));
            var songsFromArtist = SongsMgtService.AllSongs;
            List<SongsModelView> songsfromartist = new();
            songsfromartist?.Clear();
            foreach (var song in songsFromArtist)
            {
                if (songsIDsFromArtists.Contains(song.Id))
                {
                    songsfromartist.Add(song);
                }
            }
            return songsfromartist ?? Enumerable.Empty<SongsModelView>().ToList();
            ;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting songs from playlist: {ex.Message}");
            return Enumerable.Empty<SongsModelView>().ToList();
        }
    }

    public ObservableCollection<SongsModelView> GetallArtistsSongsByAlbumID(ObjectId albumID)
    {
        try
        {
            var specificAlbum = SongsMgtService.AllAlbums.FirstOrDefault(x => x.Id == albumID);
            if (specificAlbum == null)
            {
                Debug.WriteLine($"Album with ID {albumID} not found.");
                return (Enumerable.Empty<SongsModelView>().ToObservableCollection());
            }
            var AllAlbumsForSpecificArtist = SongsMgtService.AllAlbums;
            var songsIDsFromAlbum = new HashSet<ObjectId>(SongsMgtService.GetSongsIDsFromAlbumID(albumID));

            ObservableCollection<SongsModelView> songsFromArtistAndAlbum = new();

            foreach (var songId in songsIDsFromAlbum)
            {
                songsFromArtistAndAlbum.Add(SongsMgtService.AllSongs.FirstOrDefault(song => song.Id == songId));
            }


            return songsFromArtistAndAlbum;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting songs from album: {ex.Message}");
            return (Enumerable.Empty<SongsModelView>().ToObservableCollection());
        }
    }

    public ObservableCollection<SongsModelView> GetallArtistsSongsByArtistId(ObjectId artistID)
    {
        try
        {
            ObservableCollection<SongsModelView> songsFromArtist = new();

            // Get all song IDs associated with the artist
            var songsIDsFromArtist = new HashSet<ObjectId>(SongsMgtService.GetSongsIDsFromArtistID(artistID));

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
            return (Enumerable.Empty<SongsModelView>().ToObservableCollection());
        }
    }


    public bool DeletePlaylistThroughID(ObjectId playlistID)
    {
        //TODO: DO THIS TOO
        throw new NotImplementedException();
    }

    public async Task DeleteSongFromHomePage(ObjectId songID)
    {
        await SongsMgtService.DeleteSongFromDB(songID);
    }
    public async Task MultiDeleteSongFromHomePage(ObservableCollection<SongsModelView> songs)
    {
        await SongsMgtService.MultiDeleteSongFromDB(songs);
        
    }
}