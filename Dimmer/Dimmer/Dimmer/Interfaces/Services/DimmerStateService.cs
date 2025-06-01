using System.Reactive.Disposables;
// Assuming Dimmer.Data.Models and Dimmer.Utilities.Enums are accessible
using Dimmer.Interfaces.Services.Interfaces;
// using Dimmer.Platform; // For Window

namespace Dimmer.Interfaces.Services;

public partial class DimmerStateService : IDimmerStateService
{
    // --- BehaviorSubjects (Source of Truth) ---
    private readonly BehaviorSubject<SongModelView?> _currentSong = new(null);
    private readonly BehaviorSubject<PlaybackStateInfo> _playbackState; // Initialized in constructor
    private readonly BehaviorSubject<bool> _isPlaying = new(false);
    private readonly BehaviorSubject<PlaylistModel?> _currentPlaylistModel = new(null);
    private readonly BehaviorSubject<bool> _isShuffleActive = new(false);
    private readonly BehaviorSubject<RepeatMode> _currentRepeatMode = new(RepeatMode.Off); // Default
    private readonly BehaviorSubject<TimeSpan> _currentSongPosition = new(TimeSpan.Zero);
    private readonly BehaviorSubject<TimeSpan> _currentSongDuration = new(TimeSpan.Zero);

    private readonly BehaviorSubject<IReadOnlyList<SongModel>> _allSongsInLibrary = new(Array.Empty<SongModel>());

    private readonly BehaviorSubject<IReadOnlyList<DimmerPlayEvent>> _allEventsInLibrary = new(Array.Empty<DimmerPlayEvent>());
    private readonly BehaviorSubject<UserModelView?> _currentUser = new(null);
    private readonly BehaviorSubject<AppStateModelView?> _applicationSettingsState = new(null);
    private readonly BehaviorSubject<CurrentPage> _currentPage = new(Utilities.Enums.CurrentPage.HomePage);
    private readonly BehaviorSubject<IReadOnlyList<Window>> _currentlyOpenWindows = new(Array.Empty<Window>());

    private readonly BehaviorSubject<AppLogModel> _latestDeviceLog; // Initialized in constructor
    private readonly BehaviorSubject<IReadOnlyList<AppLogModel>> _dailyLatestDeviceLogs = new(Array.Empty<AppLogModel>());
    private readonly BehaviorSubject<LyricPhraseModel?> _currentLyric = new(null);
    private readonly BehaviorSubject<IReadOnlyList<LyricPhraseModel>> _syncLyrics = new(Array.Empty<LyricPhraseModel>());
    private readonly BehaviorSubject<double> _deviceVolume = new(1.0);

    private readonly IMapper _mapper;
    private readonly CompositeDisposable _disposables = new();

    private readonly IDimmerAudioService _audioService;

    public DimmerStateService(IMapper mapper, IDimmerAudioService audioService)
    {
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

        // Initialize with default/empty values where appropriate
        _latestDeviceLog = new BehaviorSubject<AppLogModel>(new AppLogModel { Log = $"StateService Initialized. + {DateTimeOffset.UtcNow}" });
        _playbackState = new BehaviorSubject<PlaybackStateInfo>(new PlaybackStateInfo(DimmerPlaybackState.Opening, null, null, null));

        // Derived state: Reset position and duration when song changes to null (or a new song)
        _disposables.Add(
             _currentSong
                 .Where(songView => songView == null)
                 .Subscribe(_ =>
                 {
                     _currentSongPosition.OnNext(TimeSpan.Zero);
                     _currentSongDuration.OnNext(TimeSpan.Zero);
                 },
                 ex => { /* Log error for _currentSong reset subscription if needed */ })
         );
        _audioService= audioService ?? throw new ArgumentNullException(nameof(audioService));
    }

    // --- Observables (Implementing IDimmerStateService) ---
    public IObservable<SongModelView?> CurrentSong => _currentSong.AsObservable();
    public IObservable<PlaybackStateInfo> CurrentPlayBackState => _playbackState.AsObservable();
    public IObservable<bool> IsPlaying => _isPlaying.AsObservable();
    public IObservable<PlaylistModel?> CurrentPlaylist => _currentPlaylistModel.AsObservable();
    public IObservable<bool> IsShuffleActive => _isShuffleActive.AsObservable();
    public IObservable<RepeatMode> CurrentRepeatMode => _currentRepeatMode.AsObservable();
    public IObservable<TimeSpan> CurrentSongPosition => _currentSongPosition.AsObservable();
    public IObservable<TimeSpan> CurrentSongDuration => _currentSongDuration.AsObservable();

    public IObservable<IReadOnlyList<DimmerPlayEvent>> AllPlayHistory => _allEventsInLibrary.AsObservable();

    public IObservable<IReadOnlyList<SongModel>> AllCurrentSongs => _allSongsInLibrary.AsObservable();
    public IObservable<UserModelView?> CurrentUser => _currentUser.AsObservable();
    public IObservable<AppStateModelView?> ApplicationSettingsState => _applicationSettingsState.AsObservable();
    public IObservable<CurrentPage> CurrentPage => _currentPage.AsObservable();
    public IObservable<IReadOnlyList<Window>> CurrentlyOpenWindows => _currentlyOpenWindows.AsObservable();

    public IObservable<AppLogModel> LatestDeviceLog => _latestDeviceLog.AsObservable();
    public IObservable<IReadOnlyList<AppLogModel>> DailyLatestDeviceLogs => _dailyLatestDeviceLogs.AsObservable();
    public IObservable<LyricPhraseModel?> CurrentLyric => _currentLyric.AsObservable();
    public IObservable<IReadOnlyList<LyricPhraseModel>> SyncLyrics => _syncLyrics.AsObservable();
    public IObservable<double> DeviceVolume => _deviceVolume.AsObservable();

    // --- Setters (Implementing IDimmerStateService) ---
    public void LoadAllSongs(IEnumerable<SongModel> songs)
    {
        var songList = songs?.ToList() ?? new List<SongModel>();
        // Consider if a deep equality check is needed or if reference change is enough
        _allSongsInLibrary.OnNext(songList.AsReadOnly());
    }
    public void LoadAllPlayHistory(IEnumerable<DimmerPlayEvent> events)
    {
        var eventsList = events?.ToList() ?? new List<DimmerPlayEvent>();
        // Consider if a deep equality check is needed or if reference change is enough
        _allEventsInLibrary.OnNext(eventsList.AsReadOnly());
    }

    public void SetCurrentSong(SongModel? songModel)
    {
        SongModelView? newSongView = null;
        if (songModel != null)
        {
            newSongView = _mapper.Map<SongModelView>(songModel);
        }

        // Check if the ID has changed to avoid redundant notifications for the same song
        if (_currentSong.Value?.Id == newSongView?.Id)
        {
            // If it's the same song ID, but perhaps other details in SongModelView changed,
            // still push if the objects are not reference-equal (or value-equal if SongModelView is a struct).
            if (ReferenceEquals(_currentSong.Value, newSongView) || Equals(_currentSong.Value, newSongView))
                return;
        }

        _currentSong.OnNext(newSongView);

        // When a new song is set (or cleared), reset position and duration
        if (newSongView != null)
        {
            // Duration might be set later by SongsMgtFlow once audio is loaded
            SetCurrentSongDuration(TimeSpan.FromSeconds(newSongView.DurationInSeconds > 0 ? newSongView.DurationInSeconds : 0));
            SetCurrentSongPosition(TimeSpan.Zero);
        }
        else // Song cleared
        {
            SetCurrentSongDuration(TimeSpan.Zero);
            SetCurrentSongPosition(TimeSpan.Zero);
        }
    }

    public void SetCurrentState(PlaybackStateInfo state)
    {
        // Consider adding a proper Equals to PlaybackStateInfo for DistinctUntilChanged to work effectively
        // or compare key properties here if PlaybackStateInfo doesn't have a good Equals override.
        if (_playbackState.Value.Equals(state)) // Assumes PlaybackStateInfo has a proper Equals
            return;
        _playbackState.OnNext(state);
    }

    public void SetCurrentPlaylist(PlaylistModel? playlist)
    {
        if (_currentPlaylistModel.Value?.Id == playlist?.Id) // Basic check by ID
            return;
        _currentPlaylistModel.OnNext(playlist);
    }

    public void SetShuffleActive(bool isShuffleOn)
    {
        if (_isShuffleActive.Value == isShuffleOn)
            return;
        _isShuffleActive.OnNext(isShuffleOn);
    }

    public void SetRepeatMode(RepeatMode repeatMode)
    {
        if (_currentRepeatMode.Value == repeatMode)
            return;
        _currentRepeatMode.OnNext(repeatMode);
    }

    public void SetCurrentSongPosition(TimeSpan position)
    {
        // Add tolerance for frequent updates if needed
        if (_currentSongPosition.Value == position)
            return;
        _currentSongPosition.OnNext(position);
    }

    public void SetCurrentSongDuration(TimeSpan duration)
    {
        if (_currentSongDuration.Value == duration)
            return;
        _currentSongDuration.OnNext(duration);
    }

    public void SetCurrentUser(UserModelView? user)
    {
        if (_currentUser.Value?.Id == user?.Id)
            return; // Basic check
        _currentUser.OnNext(user);
    }

    public void SetApplicationSettingsState(AppStateModelView? appState)
    {
        // Add comparison logic if AppStateModelView is complex
        if (Equals(_applicationSettingsState.Value, appState))
            return;
        _applicationSettingsState.OnNext(appState);
    }

    public void SetCurrentLogMsg(AppLogModel logMessage)
    {
        if (logMessage == null)
            return;
        _latestDeviceLog.OnNext(logMessage);

        var updatedLogs = new List<AppLogModel>(_dailyLatestDeviceLogs.Value ?? Enumerable.Empty<AppLogModel>());
        updatedLogs.Add(logMessage);
        _dailyLatestDeviceLogs.OnNext(updatedLogs.AsReadOnly());
    }

    public void SetDeviceVolume(double volume)
    {
        double clampedVolume = Math.Clamp(volume, 0.0, 1.0);
        if (Math.Abs(_deviceVolume.Value - clampedVolume) < 0.001)
            return;
        _deviceVolume.OnNext(clampedVolume);
    }

    public void AddWindow(Window window)
    {
        if (window == null)
            return;
        var currentWindows = _currentlyOpenWindows.Value?.ToList() ?? new List<Window>();
        if (!currentWindows.Contains(window))
        {
            currentWindows.Add(window);
            _currentlyOpenWindows.OnNext(currentWindows.AsReadOnly());
        }
    }

    public void RemoveWindow(Window window)
    {
        if (window == null)
            return;
        var currentWindows = _currentlyOpenWindows.Value?.ToList() ?? new List<Window>();
        if (currentWindows.Remove(window))
        {
            _currentlyOpenWindows.OnNext(currentWindows.AsReadOnly());
        }
    }

    public void SetCurrentPage(CurrentPage page)
    {
        if (_currentPage.Value == page)
            return;
        _currentPage.OnNext(page);
    }

    public void SetSyncLyrics(IEnumerable<LyricPhraseModel>? lyrics)
    {
        var e = Array.Empty<LyricPhraseModel>();
        _syncLyrics.OnNext(lyrics?.ToList().AsReadOnly() ?? e.AsReadOnly());
    }

    public void SetCurrentLyric(LyricPhraseModel? lyric)
    {
        if (EqualityComparer<LyricPhraseModel?>.Default.Equals(_currentLyric.Value, lyric))
            return;
        _currentLyric.OnNext(lyric);
    }

    public void Dispose()
    {
        _disposables.Dispose();
        // Dispose all BehaviorSubjects
        _currentSong.Dispose();
        _playbackState.Dispose();
        _isPlaying.Dispose();
        _currentPlaylistModel.Dispose();
        _isShuffleActive.Dispose();
        _currentRepeatMode.Dispose();
        _currentSongPosition.Dispose();
        _currentSongDuration.Dispose();
        _allSongsInLibrary.Dispose();
        _allEventsInLibrary.Dispose();
        _currentUser.Dispose();
        _applicationSettingsState.Dispose();
        _currentPage.Dispose();
        _currentlyOpenWindows.Dispose();
        _latestDeviceLog.Dispose();
        _dailyLatestDeviceLogs.Dispose();
        _currentLyric.Dispose();
        _syncLyrics.Dispose();
        _deviceVolume.Dispose();
        GC.SuppressFinalize(this);
    }
}