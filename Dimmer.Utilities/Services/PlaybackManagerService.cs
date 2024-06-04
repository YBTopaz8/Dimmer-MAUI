


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
    public SongsModelView CurrentlyPlayingSong => ObservableCurrentlyPlayingSong;

    public ISongsManagementService SongsMgtService { get; }
    public IStatsManagementService StatsMgtService { get; }
    public IPlaylistManagementService PlaylistMgtService { get; }

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
        

        _positionTimer = new (1000);
        _positionTimer.Elapsed += OnPositionTimerElapsed;
        _positionTimer.AutoReset = true;
        var allSongs = SongsMgtService.AllSongs;
        _nowPlayingSubject.OnNext(allSongs);
    }

    
    private List<SongsModelView> LoadSongs(string folderPath, IProgress<int> loadingSongsProgress)
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
                return Enumerable.Empty<SongsModelView>().ToList();
            }

            var allSongs = new List<SongsModelView>();
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

                var song = new SongsModelView
                {
                    Title = track.Title,
                    Artist = new ArtistModel { Name = track.Artist },
                    Album = new AlbumModel { Name = track.Album },
                    ReleaseYear = track.Year,
                    SampleRate = track.SampleRate,
                    FilePath = track.Path,
                    DurationInSeconds = track.Duration,
                    BitRate = track.Bitrate,
                    FileSize = fileInfo.Length,
                    FileFormat = Path.GetExtension(file).TrimStart('.'),
                    HasLyrics = track.Lyrics.SynchronizedLyrics?.Count > 0 || File.Exists(file.Replace(Path.GetExtension(file), ".lrc"))
                };
                
                allSongs.Add(song);

                processedFiles++;
                if (processedFiles % updateThreshold == 0 || processedFiles == totalFiles)
                {
                    loadingSongsProgress.Report((processedFiles * 100) / totalFiles);
                }
            }
            _nowPlayingSubject.OnNext(allSongs);

            return allSongs;
        }
        catch (Exception ex)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            Shell.Current.DisplayAlert("Error while scanning files", ex.Message, "OK"));
            return Enumerable.Empty<SongsModelView>().ToList();
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

        
        var songss = await Task.Run(() => LoadSongs(folderPath, progress));

        if (songss.Count != 0)
        {

            await SongsMgtService.AddSongBatchAsync(songss);
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


    void LoadSongs()
    {
        IList<SongsModelView> currentList = _nowPlayingSubject.Value.ToList();

        string songPath1 = "F:\\Nico\\Bastille - No Angels.flac";
        SongsModelView song = new SongsModelView();// { Title = "No Angels", Artist = new ArtistModel { Name = "Bastille" }, FilePath = songPath1 };
        string songPath2 = "F:\\Nico\\Sev - River.flac";
        SongsModelView song2 = new();// { Title = "River", Artist = new ArtistModel { Name = "Sev" }, FilePath = songPath2};

        currentList.Add(song);
        currentList.Add(song2);

        _nowPlayingSubject.OnNext(currentList);

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
                    //var clonedSong = song.Clone();
                    currentList.Add(song);
                    _nowPlayingSubject.OnNext(new List<SongsModelView>(currentList));
                    _currentSongIndex = currentList.Count - 1;
                }
            }

            ObservableCurrentlyPlayingSong = _nowPlayingSubject.Value[_currentSongIndex];//.Clone();
            
            ObservableCurrentlyPlayingSong.CoverImage = GetCoverImage(ObservableCurrentlyPlayingSong.FilePath);
            //string artName = new(ObservableCurrentlyPlayingSong.Artist?.Name);
            //var cover = Path.ChangeExtension(ObservableCurrentlyPlayingSong.FilePath, ".jpg");
            await audioService.InitializeAsync(new MediaPlay() 
            { 
                Name = ObservableCurrentlyPlayingSong.Title, 
                //Author = artName,
                //Image = cover,
                //Author = ObservableCurrentlyPlayingSong.Artist?.Name, 
                URL = ObservableCurrentlyPlayingSong.FilePath
            });

            _currentPositionSubject.OnNext(new());
            await audioService.PlayAsync();

            ObservableCurrentlyPlayingSong.IsPlaying = true;
            ObservableCurrentlyPlayingSong.PlayCount++;


            StatsMgtService.IncrementPlayCount(song.Title, song.DurationInSeconds);
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
    public async Task<bool> PauseResumeSongAsync()
    {
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
        await PlaySongAsync(currentList[_currentSongIndex]);
        return true;
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

    public void AddSongToFavoritesPlayList(SongsModelView song)
    {
        
        if (song is not null)
        {
           song.IsFavorite = !song.IsFavorite;
        }
        //PlaylistMgtService.AddSongToPlayListWithPlayListName(song, "Favorites");
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
                    (s.Artist.Name != null && NormalizeAndCache(s.Artist.Name).Contains(normalizedSearchTerm, StringComparison.CurrentCultureIgnoreCase)))
        .ToList();

        _nowPlayingSubject.OnNext(SearchedSongsList);

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

    private void ResetSearch()
    {
        SearchedSongsList?.Clear();
        _nowPlayingSubject.OnNext(SongsMgtService.AllSongs);
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