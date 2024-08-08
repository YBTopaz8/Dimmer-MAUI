using ATL;

#if ANDROID
using Dimmer_MAUI.Platforms.Android.MAudioLib;
#endif

using System.Globalization;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Timers;
//using static Android.Icu.Text.CaseMap;

namespace Dimmer_MAUI.UtilitiesServices;
public partial class PlaybackManagerService : ObservableObject, IPlayBackService
{

    INativeAudioService audioService;
    public IObservable<IList<SongsModelView>> NowPlayingSongs => _nowPlayingSubject.AsObservable();
    BehaviorSubject<IList<SongsModelView>> _nowPlayingSubject = new([]);

    public IObservable<MediaPlayerState> PlayerState => _playerStateSubject.AsObservable();
    BehaviorSubject<MediaPlayerState> _playerStateSubject = new(MediaPlayerState.Stopped);

    public IObservable<PlaybackInfo> CurrentPosition => _currentPositionSubject.AsObservable();
    BehaviorSubject<PlaybackInfo> _currentPositionSubject = new(new());
    System.Timers.Timer _positionTimer;

    [ObservableProperty]
    private SongsModelView observableCurrentlyPlayingSong;
    public SongsModelView CurrentlyPlayingSong => ObservableCurrentlyPlayingSong;
    [ObservableProperty]
    int observableLoadProgressPercent;
    [ObservableProperty]
    string totalSongsSizes;
    [ObservableProperty]
    string totalSongsDuration;
    public int LoadProgressPercent => ObservableLoadProgressPercent;
    ISongsManagementService SongsMgtService { get; }
    IStatsManagementService StatsMgtService { get; }
    IPlaylistManagementService PlaylistMgtService { get; }
    public IPlayListService PlayListService { get; }

    int _currentSongIndex = 0;

    [ObservableProperty]
    bool isShuffleOn;
    [ObservableProperty]
    int currentRepeatMode;

    bool isSongPlaying;

    List<ObjectId> playedSongsIDs = [];
    Random _shuffleRandomizer = new Random();
    public PlaybackManagerService(INativeAudioService AudioService, ISongsManagementService SongsMgtService,
        IStatsManagementService statsMgtService, IPlaylistManagementService playlistMgtService,
        IPlayListService playListService)
    {
        this.SongsMgtService = SongsMgtService;
        StatsMgtService = statsMgtService;
        PlaylistMgtService = playlistMgtService;
        PlayListService = playListService;

        audioService = AudioService;


        audioService.PlayPrevious += AudioService_PlayPrevious;
        audioService.PlayNext += AudioService_PlayNext;
        audioService.IsPlayingChanged += AudioService_PlayingChanged;
        audioService.PlayEnded += AudioService_PlayEnded;
        _positionTimer = new(1000);
        _positionTimer.Elapsed += OnPositionTimerElapsed;
        _positionTimer.AutoReset = true;
        _nowPlayingSubject.OnNext(SongsMgtService.AllSongs);

        LoadLastPlayedSong(SongsMgtService);
        GetReadableFileSize();
        GetReadableDuration();
        CurrentRepeatMode = AppSettingsService.RepeatModePreference.GetRepeatState();
        IsShuffleOn = AppSettingsService.ShuffleStatePreference.GetShuffleState();

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
    private (List<SongsModelView> songs, Dictionary<string, ArtistModel>) LoadSongs(List<string> folderPaths)
    {
        try
        {
            List<string> allFiles = new();
            foreach (var folder in folderPaths)
            {

                allFiles.AddRange(Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories)
                                    .Where(s => s.EndsWith(".mp3") || s.EndsWith(".flac") || s.EndsWith(".wav"))
                                    .AsParallel()
                                    .ToList());
            }


            if (allFiles.Count == 0)
            {
                return (Enumerable.Empty<SongsModelView>().ToList(), new Dictionary<string, ArtistModel>());
            }
            Debug.WriteLine($"Total files {allFiles.Count}");
            var allSongs = new List<SongsModelView>();
            //allSongs = SongsMgtService.AllSongs.ToList();
            var artistDict = new Dictionary<string, ArtistModel>();
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
                // Check if the artist already exists in the dictionary
                if (!artistDict.TryGetValue(track.Artist, out var artist))
                {
                    artist = new ArtistModel()
                    {
                        Name = track.Artist,
                        ImagePath = null,
                    };
                    artistDict[track.Artist] = artist;
                }

                //Debug.WriteLine("Creating songmodel");
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
                
                string mimeType = track.EmbeddedPictures?.FirstOrDefault()?.MimeType;
                if (mimeType == "image/jpg" || mimeType == "image/jpeg" || mimeType == "image/png")
                {
                    song.CoverImagePath = LyricsService.SaveCoverImageToFile(track.Path, track.EmbeddedPictures?.FirstOrDefault()?.PictureData);
                }

                song.CreationTime = fileInfo.CreationTime;
                allSongs.Add(song);

                artist.SongsIDs.Add(song.Id);
                //Debug.WriteLine("Added");
                processedFiles++;
                //Debug.WriteLine($"Progress: {processedFiles}");
                
                ObservableLoadProgressPercent = (processedFiles * 100 / totalFiles);
            }
            Debug.WriteLine($"TotalFiles {allFiles.Count} files");
            Debug.WriteLine($"Skipped {skipCounter} files");
            return (allSongs, artistDict);
        }
        catch (Exception ex)
        {
            MainThread.BeginInvokeOnMainThread(() =>
                Shell.Current.DisplayAlert("Error while scanning files ", ex.Message, "OK")
                );
            return (Enumerable.Empty<SongsModelView>().ToList(), new Dictionary<string, ArtistModel>());
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
        _nowPlayingSubject.OnNext(songs);
        //_nowPlayingSubject.OnNext(SongsMgtService.AllSongs);
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
        }
        _playerStateSubject.OnNext(MediaPlayerState.Initialized);
    }

    byte[]? GetCoverImage(string filePath, bool isToGetByteArrayImages)
    {
        var LoadTrack = new Track(filePath);

        if (LoadTrack.EmbeddedPictures.Count != 0)
        {
            string mimeType = LoadTrack.EmbeddedPictures?.FirstOrDefault()?.MimeType;
            if (mimeType != "image/jpg" || mimeType != "image/jpeg" || mimeType != "image/png")
            {
                _playerStateSubject.OnNext(MediaPlayerState.CoverImageDownload);
                return Array.Empty<byte>();
            }
        }
        else
        {
            string[] pngFiles = Array.Empty<string>();
            string directoryPath = Path.GetDirectoryName(filePath);
            var jpgFiles = Directory.GetFiles(directoryPath, "*.jpg", SearchOption.TopDirectoryOnly);
            if (jpgFiles.Length < 1)
            {
                pngFiles = Directory.GetFiles(directoryPath, "*.png", SearchOption.TopDirectoryOnly);
            }
            if (jpgFiles.Length > 0 || pngFiles.Length > 0)
            {
                return File.ReadAllBytes(jpgFiles[0]);
            }
        }
        return null;
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

    public bool PlaySelectedSongsOutsideApp(string[] filePaths)
    {
        // Filter the array to include only specific file extensions
        var filteredFiles = filePaths.Where(path => path.EndsWith(".mp3") || path.EndsWith(".flac") || path.EndsWith(".wav")).ToArray();

        var allSongs = new List<SongsModelView>();
        foreach (var file in filteredFiles)
        {
            FileInfo fileInfo = new(file);
            if (fileInfo.Length < 1000)
            {
                continue;
            }
            Track track = new Track(file);
            var song = new SongsModelView
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
                HasLyrics = track.Lyrics.SynchronizedLyrics?.Count > 0 || File.Exists(file.Replace(Path.GetExtension(file), ".lrc"))
            };
            song.CoverImagePath = LyricsService.SaveCoverImageToFile(track.Path, track.EmbeddedPictures?.FirstOrDefault()?.PictureData);
            song.CreationTime = fileInfo.CreationTime;
            allSongs.Add(song);
        }
        _nowPlayingSubject.OnNext(allSongs);
        return true;
    }

    #region Playback Control Region
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
            _playerStateSubject.OnNext(MediaPlayerState.LyricsLoad);

            var coverImage = GetCoverImage(ObservableCurrentlyPlayingSong.FilePath, true);
            if (coverImage is null || coverImage.Length < 1)
            {
                _playerStateSubject.OnNext(MediaPlayerState.CoverImageDownload);
            }
            await audioService.InitializeAsync(new MediaPlay()
            {
                Name = ObservableCurrentlyPlayingSong.Title,
                Author = ObservableCurrentlyPlayingSong.ArtistName,
                URL = ObservableCurrentlyPlayingSong.FilePath,
                ImageBytes = coverImage,
                DurationInMs = (long)(ObservableCurrentlyPlayingSong.DurationInSeconds * 1000),
            });

            _currentPositionSubject.OnNext(new());
            await audioService.PlayAsync();

            ObservableCurrentlyPlayingSong.IsPlaying = true;
            ObservableCurrentlyPlayingSong.PlayCount++;

            StatsMgtService.IncrementPlayCount(ObservableCurrentlyPlayingSong.Title, ObservableCurrentlyPlayingSong.DurationInSeconds);
            Debug.WriteLine("Play " + CurrentlyPlayingSong.Title);
            _positionTimer.Start();
            _playerStateSubject.OnNext(MediaPlayerState.Playing);
            
            AppSettingsService.LastPlayedSongSettingPreference.SetLastPlayedSong(ObservableCurrentlyPlayingSong.Id);
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Erromessage:  " + ex.Message);

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

    public async Task<bool> PlayNextSongAsync()
    {
        var currentList = _nowPlayingSubject.Value;
        if (currentList.Count == 0)
            return false;

        SongsModelView nextSong;
        if (IsShuffleOn)
        {
            if (playedSongsIDs.Count == currentList.Count)
            {
                playedSongsIDs.Clear();
            }
            do
            {
                _currentSongIndex = _shuffleRandomizer.Next(currentList.Count);
                nextSong = currentList[_currentSongIndex];
            } while (playedSongsIDs.Contains(nextSong.Id));

            playedSongsIDs.Add(nextSong.Id);
        }
        else
        {
            _currentSongIndex = (_currentSongIndex + 1) % currentList.Count;
            nextSong = currentList[_currentSongIndex];
        }

        return await PlaySongAsync(nextSong);

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


    public async Task SetSongPosition(double positionFraction)
    {

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
            await audioService.PlayAsync(positionInSeconds);
            await SetSongPosition(positionInSeconds);
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

    ObjectId PreviouslyLoadedPlaylist;
    public void UpdateCurrentQueue()
    {
        _nowPlayingSubject.OnNext(PlayListService.SongsFromPlaylist);
        Debug.WriteLine("Called");
    }
    public void UpdateSongToFavoritesPlayList(SongsModelView song)
    {

        if (song is not null)
        {
            if (!song.IsFavorite)
            {
                song.IsFavorite = true;

                if (PlaylistMgtService.AddSongToPlayListWithPlayListName(song, "Favorites"))
                {
                    PlayListService.AddSongToPlayListWithPlayListName(song, "Favorites");
                }
            }
            else
            {
                song.IsFavorite = false;
                PlaylistMgtService.RemoveSongFromPlayListWithPlayListName(song, "Favorites");
                PlayListService.RemoveFromPlayListWithPlayListName(song, "Favorites");
            }

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
        Debug.WriteLine("Resetting");
        SearchedSongsList?.Clear();
        _nowPlayingSubject.OnNext(SongsMgtService.AllSongs);
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

}

