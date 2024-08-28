#if ANDROID
using Dimmer_MAUI.Platforms.Android.MAudioLib;
#endif

//using static Android.Icu.Text.CaseMap;


namespace Dimmer.Utilities.Services;
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
    int observableLoadingSongsProgress;
    public int LoadingSongsProgressPercentage => ObservableLoadingSongsProgress;

    [ObservableProperty]
    string totalSongsSizes;
    [ObservableProperty]
    string totalSongsDuration;
    ISongsManagementService SongsMgtService { get; }
    IStatsManagementService StatsMgtService { get; }
    public IPlaylistManagementService PlaylistManagementService { get; }
    IPlaylistManagementService PlaylistMgtService { get; }

    [ObservableProperty]
    public ObservableCollection<PlaylistModelView> allPlaylists;

    [ObservableProperty]
    string selectedPlaylistName;

    int _currentSongIndex = 0;

    [ObservableProperty]
    bool isShuffleOn;
    [ObservableProperty]
    int currentRepeatMode;

    bool isSongPlaying;

    List<ObjectId> playedSongsIDs = [];
    Random _shuffleRandomizer = new Random();
    public PlaybackUtilsService(INativeAudioService AudioService, ISongsManagementService SongsMgtService,
        IStatsManagementService statsMgtService, IPlaylistManagementService playlistManagementService)
    {
        this.SongsMgtService = SongsMgtService;
        StatsMgtService = statsMgtService;
        PlaylistManagementService = playlistManagementService;
        audioService = AudioService;


        audioService.PlayPrevious += AudioService_PlayPrevious;
        audioService.PlayNext += AudioService_PlayNext;
        audioService.IsPlayingChanged += AudioService_PlayingChanged;
        audioService.PlayEnded += AudioService_PlayEnded;
        _positionTimer = new(1000);
        _positionTimer.Elapsed += OnPositionTimerElapsed;
        _positionTimer.AutoReset = true;
        _nowPlayingSubject.OnNext(SongsMgtService.AllSongs.ToObservableCollection());

        LoadLastPlayedSong(SongsMgtService);
        LoadFirstPlaylist();
        GetReadableFileSize();
        GetReadableDuration();
        CurrentRepeatMode = AppSettingsService.RepeatModePreference.GetRepeatState();
        IsShuffleOn = AppSettingsService.ShuffleStatePreference.GetShuffleState();

        AllPlaylists = PlaylistManagementService.AllPlaylists.ToObservableCollection();
        CurrentQueue = 0;
    }

    #region Audio Service Events Region
    private void AudioService_PlayPrevious(object? sender, EventArgs e)
    {
        //throw new NotImplementedException();
    }
    SemaphoreSlim _playLock = new SemaphoreSlim(1, 1);
    private async void AudioService_PlayNext(object? sender, EventArgs e)
    {
        bool isLocked = await _playLock.WaitAsync(0);
        if (!isLocked)
            return;

        Console.WriteLine("Step0");
        try
        {
            if (CurrentRepeatMode == 2) //repeat the same song
            {
                await PlaySongAsync();
                return;
            }

            await PlayNextSongAsync();

            await Task.Delay(500);
            if (!audioService.IsPlaying)
            {

                if (CurrentRepeatMode == 2) //repeat the same song
                {
                    await PlaySongAsync();
                    return;
                }

                await PlayNextSongAsync();
            }
        }
        finally
        {
            _playLock.Release();
        }
    }
    private void AudioService_PlayEnded(object? sender, EventArgs e)
    {
        Debug.WriteLine("Ended");
    }
    private void AudioService_PlayingChanged(object? sender, bool e)
    {
        if (isSongPlaying == e)
        {
            return;
        }
        isSongPlaying = e;

        if (isSongPlaying)
        {
            _playerStateSubject.OnNext(MediaPlayerState.Playing);  // Update state to playing
        }
        else
        {
            _playerStateSubject.OnNext(MediaPlayerState.Paused);
        }
        Debug.WriteLine("Play state " + e);
        Debug.WriteLine("Pause Play changed");
    }
    #endregion

    #region Setups/Loadings Region
    private (List<SongsModelView> songs, Dictionary<string, ArtistModelView>) LoadSongs(List<string> folderPaths)
    {
        try
        {
            List<string> allFiles = new();
            foreach (var folder in folderPaths)
            {

                allFiles.AddRange(Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories)
                                    .Where(s => s.EndsWith(".mp3") || s.EndsWith(".flac") || s.EndsWith(".wav") || s.EndsWith(".m4a"))
                                    .AsParallel() 
                                    .ToList());
            }


            if (allFiles.Count == 0)
            {
                return (Enumerable.Empty<SongsModelView>().ToList(), new Dictionary<string, ArtistModelView>());
            }

            var allSongs = new List<SongsModelView>();
            //allSongs = SongsMgtService.AllSongs.ToList();
            var artistDict = new Dictionary<string, ArtistModelView>();
            int totalFiles = allFiles.Count;
            int processedFiles = 0;

            int updateThreshold = Math.Max(1, totalFiles / 100);  // update progress every 1%

            if (allSongs.Count > 0)
            {
                return (allSongs, artistDict);
            }
            Debug.WriteLine("Begin Scanning");
            int skipCounter=0;
            foreach (var file in allFiles)
            {
                FileInfo fileInfo = new(file);
                if (fileInfo.Length < 1000)
                {
                    skipCounter++;
                    
                    continue;
                }


                Track track = new(file);
                Debug.WriteLine($"Now on file: {track.Title}");
                if (allSongs.Any(s => s.Title == track.Title && s.DurationInSeconds == track.Duration && s.ArtistName == track.Artist))
                {
                    Debug.WriteLine("Skip " + track.Path);
                    continue;
                }
                string title = track.Title;
                if (title.Contains(';'))
                {
                    title = title.Split(';')[0].Trim();
                }

                // Process the artist names and add each to the artistDict
                var artists = track.Artist 
                    .Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries) // Split by semicolon and comma
                    .Select(artist => artist.Trim()) // Trim whitespace from each artist name
                    .Distinct() // Remove duplicates
                    .ToList(); // Convert to list for easy iteration

                // Create the song model
                var song = new SongsModelView
                {
                    Title = title,
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
                    CreationTime = fileInfo.CreationTime
                };

                // Handle cover image
                string mimeType = track.EmbeddedPictures?.FirstOrDefault()?.MimeType;
                if (mimeType == "image/jpg" || mimeType == "image/jpeg" || mimeType == "image/png")
                {
                    song.CoverImagePath = LyricsService.SaveCoverImageToFile(track.Path, track.EmbeddedPictures?.FirstOrDefault()?.PictureData);
                }
                if (string.IsNullOrEmpty(song.CoverImagePath))
                {
                    song.CoverImagePath = GetCoverImagePath(track.Path);
                }

                foreach (var artistName in artists)
                {
                    // Check if the artist already exists in the dictionary
                    if (!artistDict.TryGetValue(artistName, out var artist))
                    {
                        artist = new ArtistModelView
                        {
                            Name = artistName,
                            ImagePath = null,
                        };
                        artistDict[artistName] = artist;
                    }

                    // Link the song ID to the artist
                    artist.SongsIDs ??= new();
                    artist.SongsIDs.Add(song.Id);

                    // Set the ArtistID and ArtistName in the song model
                    song.ArtistID = artist.Id;
                    song.ArtistName = artist.Name;
                }

                allSongs.Add(song);
                processedFiles++;
            }

            ObservableLoadingSongsProgress = processedFiles * 100 / totalFiles; //TODO : YOU WANTED TO LET USER KNOW OF PROGRESS %age
            _playerStateSubject.OnNext(MediaPlayerState.LoadingSongs);
            
            Debug.WriteLine($"TotalFiles {allFiles.Count} files");
            Debug.WriteLine($"Skipped {skipCounter} files");
            return (allSongs, artistDict);
        }
        catch (Exception ex)
        {
            MainThread.BeginInvokeOnMainThread(() =>
                Shell.Current.DisplayAlert("Error while scanning files ", ex.Message, "OK")
                );
            return (Enumerable.Empty<SongsModelView>().ToList(), new Dictionary<string, ArtistModelView>());
        }
    }

    public async Task<bool> LoadSongsFromFolder(List<string> folderPaths)
    {
        var (songs, artists) = await Task.Run( () => LoadSongs(folderPaths));
        GetReadableFileSize();
        GetReadableDuration();
        if (songs.Count != 0)
        {
            //save songs to db to songs table
            await SongsMgtService.AddSongBatchAsync(songs);
        }
        _nowPlayingSubject.OnNext(songs.ToObservableCollection());
        //_nowPlayingSubject.OnNext(SongsMgtService.AllSongs);
        ObservableLoadingSongsProgress = 100;
        _playerStateSubject.OnNext(MediaPlayerState.LoadingSongs);
        return true;
    }

    private void LoadLastPlayedSong(ISongsManagementService SongsMgtService)
    {
        var lastPlayedSongID = AppSettingsService.LastPlayedSongSettingPreference.GetLastPlayedSong();
        if (lastPlayedSongID is not null)
        {
            var lastPlayedSong = SongsMgtService.AllSongs.FirstOrDefault(x => x.Id == (ObjectId)lastPlayedSongID);
            if (lastPlayedSong is null)
                return;
            ObservableCurrentlyPlayingSong = lastPlayedSong!;
            //_nowPlayingSubject.OnNext(ObservableCurrentlyPlayingSong);
        }
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
            string mimeType = LoadTrack.EmbeddedPictures?.FirstOrDefault()?.MimeType;
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
        return null;


        //if (coverImage is null || coverImage.Length < 1) TODO : Do code that will scan the image from the folder found in song if it's an album folder 
        //{
        //    string directoryPath = Path.GetDirectoryName(filePath);
        //    string[] imageFiles = Directory.GetFiles(directoryPath, "*.jpg", SearchOption.TopDirectoryOnly)
        //        .Concat(Directory.GetFiles(directoryPath, "*.jpeg", SearchOption.TopDirectoryOnly))
        //        .Concat(Directory.GetFiles(directoryPath, "*.png", SearchOption.TopDirectoryOnly))
        //        .ToArray();

        //    if (imageFiles.Length > 0)
        //    {
        //        coverImage = File.ReadAllBytes(imageFiles[0]);
        //    }
        //}
    }


    #endregion
    private void OnPositionTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        double currentPositionInSeconds = audioService.CurrentPosition;
        double totalDurationInSeconds = audioService.Duration;

        if (totalDurationInSeconds > 0)
        {
            double percentagePlayed = currentPositionInSeconds / totalDurationInSeconds;
            _currentPositionSubject.OnNext(new PlaybackInfo
            {
                TimeElapsed = percentagePlayed, //this is the value of that CurrentPosition then takes
                CurrentTimeInSeconds = currentPositionInSeconds
            });
        }
        else
        {
            _currentPositionSubject.OnNext(new());
        }
    }

    double currentPosition = 0;

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
                    ArtistID = ObjectId.Empty,
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
                    CoverImagePath = LyricsService.SaveCoverImageToFile(track.Path, track.EmbeddedPictures?.FirstOrDefault()?.PictureData),
                    CreationTime = fileInfo.CreationTime
                };
                allSongs.Add(newSong);
            }

        }
        _tertiaryQueueSubject.OnNext(allSongs);
        await PlaySongAsync(allSongs[0], 2);
        
        return true;
    }

    public async Task<bool> PlaySongAsync(SongsModelView? song = null, int currentQueue = 0)
    {
        if (ObservableCurrentlyPlayingSong != null)
        {
            ObservableCurrentlyPlayingSong.IsPlaying = false;
        }
        
        try
        {
            if (song != null)
            {
                ObservableCollection<SongsModelView>? currentList = null;

                // Determine which queue to use based on currentQueue
                currentList = currentQueue switch
                {
                    0 => _nowPlayingSubject.Value,
                    1 => _secondaryQueueSubject.Value,
                    2 => _tertiaryQueueSubject.Value,
                    _ => _nowPlayingSubject.Value,// Fallback to the primary queue
                };
                
                int songIndex = currentList.IndexOf(song);
                if (songIndex != -1)
                {
                    _currentSongIndex = songIndex;
                }
                else
                {
                    currentList.Add(song);
                    _nowPlayingSubject.OnNext(currentList);
                    _currentSongIndex = currentList.Count - 1;
                }
                song.IsPlaying = true;
                ObservableCurrentlyPlayingSong = song!;
            }
            
            CurrentQueue = currentQueue;
            _playerStateSubject.OnNext(MediaPlayerState.LyricsLoad);

            var coverImage = GetCoverImage(ObservableCurrentlyPlayingSong!.FilePath, true);

            await audioService.InitializeAsync(new MediaPlay()
            {
                Name = ObservableCurrentlyPlayingSong.Title,
                Author = ObservableCurrentlyPlayingSong!.ArtistName!,
                URL = ObservableCurrentlyPlayingSong.FilePath,
                ImageBytes = coverImage,
                DurationInMs = (long)(ObservableCurrentlyPlayingSong.DurationInSeconds * 1000),
            });

            _currentPositionSubject.OnNext(new());
            await audioService.PlayAsync();

            ObservableCurrentlyPlayingSong.IsPlaying = true;
            ObservableCurrentlyPlayingSong.PlayCount++;

            Debug.WriteLine("Play " + CurrentlyPlayingSong.Title);
            _positionTimer.Start();
            _playerStateSubject.OnNext(MediaPlayerState.Playing);

            AppSettingsService.LastPlayedSongSettingPreference.SetLastPlayedSong(ObservableCurrentlyPlayingSong.Id);

#if WINDOWS
            MiniPlayBackControlNotif.ShowUpdateMiniView(ObservableCurrentlyPlayingSong);
#endif
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Error message:  " + ex.Message);

            await Shell.Current.DisplayAlert("Error message", ex.Message, "Ok");
            return false;
        }
        finally
        {
            if (ObservableCurrentlyPlayingSong != null && currentQueue != 2)
            {                
                await SongsMgtService.UpdateSongDetailsAsync(ObservableCurrentlyPlayingSong);
            }
        }
    }

    public async Task<bool> PauseResumeSongAsync()
    {
        if (ObservableCurrentlyPlayingSong is null)
        {
            await PlaySongAsync();
            return true;
        }
        if (audioService.CurrentPosition == 0 && !audioService.IsPlaying)
        {
            await PlaySongAsync(ObservableCurrentlyPlayingSong);
            ObservableCurrentlyPlayingSong.IsPlaying = true;
            _playerStateSubject.OnNext(MediaPlayerState.Playing);  // Update state to playing
            _positionTimer.Start();
            return true;
        }
        if (audioService.IsPlaying)
        {
            currentPosition = audioService.CurrentPosition;
            await audioService.PauseAsync();
            ObservableCurrentlyPlayingSong.IsPlaying = false;
            _playerStateSubject.OnNext(MediaPlayerState.Paused);  // Update state to paused
            _positionTimer.Stop();

        }
        else
        {
            await audioService.PlayAsync(currentPosition);
            ObservableCurrentlyPlayingSong.IsPlaying = true;
            _playerStateSubject.OnNext(MediaPlayerState.Playing);  // Update state to playing
            _positionTimer.Start();
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
    bool IsTrueShuffleEnabled;
    public async Task<bool> PlayNextSongAsync()
    {
        IList<SongsModelView>? currentList = null;

        switch (CurrentQueue)
        {
            case 0:
                currentList = _nowPlayingSubject.Value;
                break;
            case 1:
                currentList = _secondaryQueueSubject.Value;
                break;
            case 2:
                currentList = _tertiaryQueueSubject.Value;
                break;
            default:
                return false;
        }

        if (currentList == null || currentList.Count == 0)
            return false;

        UpdateCurrentSongIndex(currentList, isNext: true);
        return await PlaySongAsync(currentList[_currentSongIndex], CurrentQueue);
    }


    public async Task<bool> PlayPreviousSongAsync()
    {
        IList<SongsModelView>? currentList = null;

        switch (CurrentQueue)
        {
            case 0:
                currentList = _nowPlayingSubject.Value;
                break;
            case 1:
                currentList = _secondaryQueueSubject.Value;
                break;
            case 2:
                currentList = _tertiaryQueueSubject.Value;
                break;
            default:
                return false;
        }

        if (currentList == null || currentList.Count == 0)
            return false;

        UpdateCurrentSongIndex(currentList, isNext: false);
        return await PlaySongAsync(currentList[_currentSongIndex], CurrentQueue);
    }

    private void UpdateCurrentSongIndex(IList<SongsModelView>? currentList, bool isNext)
    {
        if (currentList == null || currentList.Count == 0)
            return;

        if (IsShuffleOn)
        {
            if (IsTrueShuffleEnabled)
            {
                if (shuffleHistory.Count > 1)
                {
                    if (!isNext)
                    {
                        shuffleHistory.Pop(); // Remove current song from history if going back
                    }
                    _currentSongIndex = shuffleHistory.Peek();
                }
                else
                {
                    _currentSongIndex = _shuffleRandomizer.Next(currentList.Count);
                }
            }
            else
            {
                if (!isNext && shuffleHistory.Count > 1)
                {
                    shuffleHistory.Pop();
                    _currentSongIndex = shuffleHistory.Peek();
                }
                else
                {
                    do
                    {
                        _currentSongIndex = _shuffleRandomizer.Next(currentList.Count);
                    } while (playedSongsIDs.Contains(currentList[_currentSongIndex].Id));

                    playedSongsIDs.Add(currentList[_currentSongIndex].Id);
                    shuffleHistory.Push(_currentSongIndex);
                }
            }
        }
        else
        {
            _currentSongIndex = isNext
                ? (_currentSongIndex + 1) % currentList.Count
                : (_currentSongIndex - 1 + currentList.Count) % currentList.Count;
        }
    }


    public async Task SetSongPosition(double positionFraction)
    {
        if (ObservableCurrentlyPlayingSong is null)
        {
            return;
        }
        // Convert the fraction to actual seconds
        double positionInSeconds = positionFraction * audioService.Duration;

        var coverImage = GetCoverImage(ObservableCurrentlyPlayingSong.FilePath, true);
        // Set the current time in the audio service
        if (!await audioService.SetCurrentTime(positionInSeconds))
        {
            await audioService.InitializeAsync(new MediaPlay()
            {
                Name = ObservableCurrentlyPlayingSong.Title,
                Author = ObservableCurrentlyPlayingSong.ArtistName,
                URL = ObservableCurrentlyPlayingSong.FilePath,
                ImageBytes = coverImage,
                DurationInMs = (long)(ObservableCurrentlyPlayingSong.DurationInSeconds * 1000),
            });
            
            await SetSongPosition(positionInSeconds);
        }
        await audioService.PlayAsync(positionInSeconds);
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
    }
    public int ToggleRepeatMode()
    {

        switch (CurrentRepeatMode)
        {
            case 0:
                CurrentRepeatMode = 1;
                break;
            case 1:
                CurrentRepeatMode = 2;
                break;
            case 2:
                CurrentRepeatMode = 0;
                break;
            default:
                break;
        }
        AppSettingsService.RepeatModePreference.ToggleRepeatState();
        return CurrentRepeatMode;
    }
    #endregion

    //ObjectId PreviouslyLoadedPlaylist;
    public int CurrentQueue { get; set; } = 0;
    public void UpdateCurrentQueue(IList<SongsModelView> songs,int QueueNumber = 1) //0 = main queue, 1 = playlistQ, 2 = externallyloadedsongs Queue
    {
        CurrentQueue = QueueNumber;
        _secondaryQueueSubject.OnNext(songs.ToObservableCollection());        
    }
    public async Task UpdateSongToFavoritesPlayList(SongsModelView song)
    {

        if (song is not null)
        {
            await SongsMgtService.UpdateSongDetailsAsync(song);
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
        if (string.IsNullOrWhiteSpace(songTitleOrArtistName))
        {
            ResetSearch();
            return;
        }

        string normalizedSearchTerm = NormalizeAndCache(songTitleOrArtistName).ToLower();

        SearchedSongsList?.Clear();
        SearchedSongsList = SongsMgtService.AllSongs
        .Where(s => NormalizeAndCache(s.Title).ToLower().Contains(normalizedSearchTerm) ||
                    (s.ArtistName != null && NormalizeAndCache(s.ArtistName).Contains(normalizedSearchTerm, StringComparison.CurrentCultureIgnoreCase)))
        .ToList();

        _nowPlayingSubject.OnNext(SearchedSongsList.ToObservableCollection());
        GetReadableFileSize(SearchedSongsList);
        GetReadableDuration(SearchedSongsList);
    }

    private string NormalizeAndCache(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        if (normalizationCache.TryGetValue(text, out string? value))
        {
            return value;
        }

        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new System.Text.StringBuilder();

        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        var result = stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        normalizationCache[text] = result;
        return result;
    }

    void ResetSearch()
    {
        Debug.WriteLine("Resetting");
        SearchedSongsList?.Clear();
        _nowPlayingSubject.OnNext(SongsMgtService.AllSongs.ToObservableCollection());
        GetReadableFileSize();
        GetReadableDuration();
    }

    #endregion
    void GetReadableFileSize(List<SongsModelView>? songsList = null)
    {
        long totalBytes;
        if (songsList is null)
        {
            totalBytes = _nowPlayingSubject.Value.Sum(s => s.FileSize);
        }
        else
        {
            totalBytes = songsList.Sum(s => s.FileSize);
        }

        const long MB = 1024 * 1024;
        const long GB = 1024 * MB;

        if (totalBytes < GB)
        {
            double totalMB = totalBytes / (double)MB;
            TotalSongsSizes = $"{totalMB:F2} MB";
        }
        else
        {
            double totalGB = totalBytes / (double)GB;
            TotalSongsSizes = $"{totalGB:F2} GB";
        }
        Debug.WriteLine($"Total Sizes: {TotalSongsSizes}");
    }
    void GetReadableDuration(List<SongsModelView>? songsList = null)
    {
        double totalSeconds;
        if (songsList is null)
        {
            totalSeconds = _nowPlayingSubject.Value.Sum(s => s.DurationInSeconds);            
        }
        else
        {
            totalSeconds = songsList.Sum(s => s.DurationInSeconds);
        }

        const double minutes = 60;
        const double hours = 60 * minutes;
        const double days = 24 * hours;

        if (totalSeconds < hours)
        {
            double totalMinutes = totalSeconds / minutes;
            TotalSongsDuration = $"{totalMinutes:F2} minutes";
        }
        else if (totalSeconds < days)
        {
            double totalHours = totalSeconds / hours;
            TotalSongsDuration = $"{totalHours:F2} hours";
        }
        else
        {
            double totalDays = totalSeconds / days;
            TotalSongsDuration = $"{totalDays:F2} days";
        }

        Debug.WriteLine($"Total Duration: {TotalSongsDuration}");
    }

    public void LoadFirstPlaylist()
    {
        var firstPlaylist = PlaylistManagementService.AllPlaylists.FirstOrDefault();
        if (firstPlaylist is not null)
        {
            SelectedPlaylistName = firstPlaylist.Name;
            GetSongsFromPlaylistID(firstPlaylist.Id);            
        }
    }

    public void AddSongToPlayListWithPlayListName(SongsModelView song, string playlistName)
    {        
        PlaylistManagementService.AddSongToPlayListWithPlayListName(song, playlistName);
        GetAllPlaylists();
    }

    public void RemoveSongFromPlayListWithPlayListID(SongsModelView song, ObjectId playlistID)
    {
        //var specificPlaylist = AllPlaylists.FirstOrDefault(x => x.Name == playListName);
        //if (specificPlaylist is not null)
        //{
        //    specificPlaylist?.SongsIDs.Remove(song.Id);
        //    var songsInPlaylist = _secondaryQueueSubject.Value;
        //    songsInPlaylist.Remove(song);
        //    _secondaryQueueSubject.OnNext(songsInPlaylist);
        //    specificPlaylist.TotalSongsCount -= 1;

        //}

        //PlaylistManagementService.RemoveSongFromPlayListWithPlayListName(song, playListName);

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
    public bool DeletePlaylistThroughID(ObjectId playlistID)
    {
        //TODO: DO THIS TOO
        throw new NotImplementedException();
    }

   
}

