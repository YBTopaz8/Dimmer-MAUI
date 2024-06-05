


namespace Dimmer.Utilities.IServices;
public partial class PlaybackManagerService : ObservableObject, IPlayBackService
{
    
    INativeAudioService audioService;    
    public IObservable<IList<SongsModelView>> NowPlayingSongs => _nowPlayingSubject.AsObservable();
    BehaviorSubject<IList<SongsModelView>> _nowPlayingSubject = new([]);
    
    public IObservable<MediaPlayerState> PlayerState => _playerStateSubject.AsObservable();
    BehaviorSubject<MediaPlayerState> _playerStateSubject = new(MediaPlayerState.Stopped);

    public IObservable<PlaybackInfo> CurrentPosition => _currentPositionSubject.AsObservable();
    BehaviorSubject<PlaybackInfo> _currentPositionSubject = new(new() );
    System.Timers.Timer _positionTimer;

    [ObservableProperty]
    private SongsModelView observableCurrentlyPlayingSong;
    [ObservableProperty]
    string totalSongsSizes;
    [ObservableProperty]
    string totalSongsDuration;
    public SongsModelView CurrentlyPlayingSong => ObservableCurrentlyPlayingSong;

    ISongsManagementService SongsMgtService { get; }
    IStatsManagementService StatsMgtService { get; }
    IPlaylistManagementService PlaylistMgtService { get; }

    int _currentSongIndex = 0;

    public PlaybackManagerService(INativeAudioService AudioService, ISongsManagementService SongsMgtService,
        IStatsManagementService statsMgtService, IPlaylistManagementService playlistMgtService)
    {
        this.SongsMgtService = SongsMgtService;
        StatsMgtService = statsMgtService;
        PlaylistMgtService = playlistMgtService;

#if WINDOWS
        this.audioService = AudioService;
#elif ANDROID

        this.audioService = NativeAudioService.Current;
#endif
        audioService.PlayNext -= AudioService_PlayNext;
        audioService.PlayNext += AudioService_PlayNext;
        audioService.IsPlayingChanged += AudioService_PlayingChanged;
        audioService.IsPlayingChanged -= AudioService_PlayingChanged;
        
        _positionTimer = new (1000);
        _positionTimer.Elapsed += OnPositionTimerElapsed;
        _positionTimer.AutoReset = true;
        _nowPlayingSubject.OnNext(SongsMgtService.AllSongs);

        GetReadableFileSize();
        GetReadableDuration();
    }

    private void AudioService_PlayingChanged(object? sender, bool e)
    {
        Debug.WriteLine("Pause Play changed");
    }

    private (List<SongsModelView> songs, Dictionary<string, ArtistModel>) LoadSongs(string folderPath, IProgress<int> loadingSongsProgress)
    {
        try
        {
            
            var ss = Directory.GetFiles(folderPath);
            var allFiles = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories)
                                .Where(s => s.EndsWith(".mp3") || s.EndsWith(".flac") || s.EndsWith(".wav"))
                                .AsParallel()
                                .ToList();
            
            if (allFiles.Count == 0)
            {
                return (Enumerable.Empty<SongsModelView>().ToList(), new Dictionary<string, ArtistModel>());
            }

            var allSongs = new List<SongsModelView>();
            var artistDict = new Dictionary<string, ArtistModel>();
            int totalFiles = allFiles.Count;
            int processedFiles = 0;

            int updateThreshold = Math.Max(1, totalFiles / 100);  // update progress every 1%

            foreach (var file in allFiles)
            {
                FileInfo fileInfo = new(file);
                if (fileInfo.Length < 1000)
                {
                    continue;
                }

                Track track = new(file);

                // Check if the artist already exists in the dictionary
                if (!artistDict.TryGetValue(track.Artist, out var artist))
                {
                    artist = new ArtistModel ()
                    {
                        Name = track.Artist,
                        ImagePath = null,                        
                    };
                    artistDict[track.Artist] = artist;
                }


                var song = new SongsModelView
                {
                    Title = track.Title,
                    ArtistID = artist.Id,
                    ArtistName = artist.Name,
                    AlbumName = track.Album, 
                    ReleaseYear = track.Year,
                    SampleRate = track.SampleRate,
                    FilePath = track.Path,
                    DurationInSeconds = track.Duration,
                    BitRate = track.Bitrate,
                    FileSize = fileInfo.Length,
                    TrackNumber = track.TrackNumber,
                    FileFormat = Path.GetExtension(file).TrimStart('.'),
                    HasLyrics = track.Lyrics.SynchronizedLyrics?.Count > 0 || File.Exists(file.Replace(Path.GetExtension(file), ".lrc"))
                };
                
                allSongs.Add(song);

                artist.SongsIDs.Add(song.Id);

                processedFiles++;
                if (processedFiles % updateThreshold == 0 || processedFiles == totalFiles)
                {
                    loadingSongsProgress.Report((processedFiles * 100) / totalFiles);
                }
            }
            _nowPlayingSubject.OnNext(allSongs);

            return (allSongs, artistDict);
        }
        catch (Exception ex)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            Shell.Current.DisplayAlert("Error while scanning files", ex.Message, "OK"));
            return (Enumerable.Empty<SongsModelView>().ToList(), new Dictionary<string, ArtistModel>());

        }
    }
    public async Task<bool> LoadSongsFromFolder(string folderPath, IProgress<int> loadingSongsProgress)
    {
        var taskCompletionSource = new TaskCompletionSource<bool>();
        var progress = new Progress<int>(percent =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Debug.WriteLine($"Loading progress: {percent}%");
                
            });
        });

        
        var (songs, artists) = await Task.Run(() => LoadSongs(folderPath, progress));

        if (songs.Count != 0)
        {
            //save songs to db to songs table
            await SongsMgtService.AddSongBatchAsync(songs);
        }
        return true;
    }


    private void OnPositionTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        double currentPositionInSeconds = audioService.CurrentPosition;
        double totalDurationInSeconds = audioService.Duration;

        if (totalDurationInSeconds > 0)
        {
            double percentagePlayed = currentPositionInSeconds / totalDurationInSeconds;
            _currentPositionSubject.OnNext(new PlaybackInfo
            {
                TimeElapsed = percentagePlayed,
                CurrentTimeInSeconds = currentPositionInSeconds
            });
        }
        else
        {
            _currentPositionSubject.OnNext(new ());
        }
    }

    SemaphoreSlim _playLock = new SemaphoreSlim(1, 1);
    private async void AudioService_PlayNext(object? sender, EventArgs e)
    {
        bool isLocked = await _playLock.WaitAsync(0);
        if (!isLocked)
            return;
        try
        {
            await PlayNextSongAsync();
        }
        finally
        {
            _playLock.Release();            
        }
    }

      
    double currentPosition = 0;
    public async Task<bool> PlaySongAsync(SongsModelView? song = null)
    {
        if (ObservableCurrentlyPlayingSong != null)
        {
            ObservableCurrentlyPlayingSong.IsPlaying = false;
        }

        try
        {
            if (song is not null)
            {
                IList<SongsModelView>? currentList = _nowPlayingSubject.Value;
                int songIndex = currentList.IndexOf(song);
                if (songIndex != -1)
                {
                    _currentSongIndex = songIndex;
                }
                else
                {
                    currentList.Add(song);
                    _nowPlayingSubject.OnNext(new List<SongsModelView>(currentList));
                    _currentSongIndex = currentList.Count - 1;
                }
                song.IsPlaying = true;
            }

            ObservableCurrentlyPlayingSong = _nowPlayingSubject.Value[_currentSongIndex];
            
            ObservableCurrentlyPlayingSong.CoverImage = GetCoverImage(ObservableCurrentlyPlayingSong.FilePath);
            //string artName = new(ObservableCurrentlyPlayingSong.Artist?.Name);
            //var cover = Path.ChangeExtension(ObservableCurrentlyPlayingSong.FilePath, ".jpg");
            await audioService.InitializeAsync(new MediaPlay() 
            { 
                Name = ObservableCurrentlyPlayingSong.Title, 
                Author = ObservableCurrentlyPlayingSong.ArtistName,
                URL= ObservableCurrentlyPlayingSong.FilePath,
                ImageBytes = ObservableCurrentlyPlayingSong.CoverImage,
            });

            _currentPositionSubject.OnNext(new());
            await audioService.PlayAsync();

            ObservableCurrentlyPlayingSong.IsPlaying = true;
            ObservableCurrentlyPlayingSong.PlayCount++;


            StatsMgtService.IncrementPlayCount(ObservableCurrentlyPlayingSong.Title, ObservableCurrentlyPlayingSong.DurationInSeconds);
            Debug.WriteLine("Play " + CurrentlyPlayingSong.Title);
            _positionTimer.Start();
            _playerStateSubject.OnNext(MediaPlayerState.Playing);
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Erromessage:  "+ ex.Message);

            await Shell.Current.DisplayAlert("Erromessage: r", ex.Message, "Ok");
            return false;
            //throw new Exception(ex.Message);
        }
    }

    public async Task<bool> PauseResumeSongAsync()
    {
        if (ObservableCurrentlyPlayingSong is null)
        {
            await PlaySongAsync();
            return true;
        }
        if (audioService.IsPlaying)
        {
            currentPosition = audioService.CurrentPosition;
            await audioService.PauseAsync();
            //audioService.IsPlaying = false;
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
            _currentPositionSubject.OnNext(new ());

            return true;
        }
        catch (Exception ex)
        {

            throw new Exception(ex.Message);
        }
    }

    public async Task<bool> PlayNextSongAsync()
    {
        var currentList = _nowPlayingSubject.Value;
        if (currentList.Count == 0)
            return false;

        _currentSongIndex = (_currentSongIndex + 1) % currentList.Count;
        return await PlaySongAsync(currentList[_currentSongIndex]);
        
    }

    public async Task<bool> PlayPreviousSongAsync()
    {
        var currentList = _nowPlayingSubject.Value;
        if (currentList.Count == 0)
            return false;

        _currentSongIndex = (_currentSongIndex - 1 + currentList.Count) % currentList.Count;
        await PlaySongAsync(currentList[_currentSongIndex]);
        return true;
    }

    byte[]? GetCoverImage(string filePath)
    {
        var LoadTrack = new Track(filePath);
        if (LoadTrack.EmbeddedPictures.Count != 0)
        {
            return LoadTrack.EmbeddedPictures[0].PictureData;
        }
        else
        {
            string directoryPath = Path.GetDirectoryName(filePath);
            var jpgFiles = Directory.GetFiles(directoryPath, "*.jpg", SearchOption.TopDirectoryOnly);
            if (jpgFiles.Length > 0)
            {
                return File.ReadAllBytes(jpgFiles[0]);
            }            
        }
        return null;
    }
    public void UpdateSongToFavoritesPlayList(SongsModelView song)
    {
        
        if (song is not null)
        {
            if (!song.IsFavorite)
            {
                song.IsFavorite = true;
                PlaylistMgtService.AddSongToPlayListWithPlayListName(song, "Favorites");
                SongsMgtService.UpdateSongDetails(song);
            }
            else
            {
                song.IsFavorite = false;
                PlaylistMgtService.RemoveSongFromPlayListWithPlayListName(song, "Favorites");
                SongsMgtService.UpdateSongDetails(song);
            }
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

    public void SetSongPosition(double positionFraction)
    {
        if (audioService != null && positionFraction >= 0 && positionFraction <= 1)
        {
            // Convert the fraction to actual seconds
            double positionInSeconds = positionFraction * audioService.Duration;

            // Set the current time in the audio service
            audioService.SetCurrentTime(positionInSeconds);
        }
    }

    public void ChangeVolume(double newPercentageValue)
    {
        try
        {
            if(CurrentlyPlayingSong is null)
            {
                return;
            }
            audioService.Volume = newPercentageValue;
            AppSettingsService.VolumeSettings.SetVolumeLevel(newPercentageValue);
        }
        catch (Exception ex)
        {
            Debug.WriteLine("No Volume modified. Possible null exception ", ex.Message);
        }
    }

    public void DecreaseVolume()
    {
        audioService.Volume -= 0.2;
    }

    public void IncreaseVolume()
    {
        audioService.Volume += 0.2;
    }

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

        _nowPlayingSubject.OnNext(SearchedSongsList);
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
        SearchedSongsList?.Clear();
        _nowPlayingSubject.OnNext(SongsMgtService.AllSongs);
        GetReadableFileSize();
        GetReadableDuration();
    }

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

}


public enum MediaPlayerState
{
    Playing,
    Paused,
    Stopped
}
public class PlaybackInfo
{
    public double TimeElapsed { get; set; } = 0;
    public double CurrentTimeInSeconds { get; set; } = 0;
}