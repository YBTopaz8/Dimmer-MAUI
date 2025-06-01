using System.Diagnostics;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.Input;

using Dimmer.Interfaces.Services.Interfaces;
using Dimmer.Utilities.Events;
using Dimmer.Utilities.Extensions;
using Dimmer.Utilities.StatsUtils;

using Microsoft.Extensions.Logging.Abstractions;


//using Dimmer.Utilities.FileProcessorUtils; // If needed
//using Dimmer.Utilities.StatsUtils; // If CollectionStats is here

namespace Dimmer.ViewModel;

public partial class BaseViewModel : ObservableObject, IDisposable
{
    // Injected Services
    public readonly IMapper _mapper;
    private readonly IAppInitializerService appInitializerService;
    private readonly IDimmerLiveStateService _dimmerLiveStateService;
    private readonly AlbumsMgtFlow _albumsMgtFlow;
    private readonly PlayListMgtFlow _playlistsMgtFlow;
    private readonly SongsMgtFlow _songsMgtFlow;
    protected readonly IDimmerStateService _stateService;
    protected readonly ISettingsService _settingsService;
    protected readonly SubscriptionManager _subsManager; // Renamed from _subs to avoid conflict, and made protected
    protected readonly IFolderMgtService _folderMgtService;
    private readonly IRepository<SongModel> songRepo;
    private readonly IRepository<ArtistModel> artistRepo;
    private readonly IRepository<AlbumModel> albumRepo;
    private readonly IRepository<GenreModel> genreRepo;
    private readonly IRepository<DimmerPlayEvent> dimmerPlayEventRepo;
    private readonly LyricsMgtFlow _lyricsMgtFlow;
    protected readonly ILogger<BaseViewModel> _logger; // Added logger
    private readonly IDimmerAudioService audioService;


    [ObservableProperty]
    public partial ObservableCollection<DimmerPlayEvent> DimmerPlayEventList { get; set; } = new();


    //public 
    public IDimmerLiveStateService DimmerLiveStateService { get; }


    // --- UI-Bound Properties driven by _stateService or local logic ---
    [ObservableProperty]
    public partial CollectionStatsSummary? SummaryStatsForAllSongs { get; set; }
    [ObservableProperty]
    public partial SongModelView? CurrentPlayingSongView { get; set; }
    [ObservableProperty] public partial int? CurrentTotalSongsOnDisplay { get; set; }
    [ObservableProperty]
    public partial SongModelView? SelectedSongForContext { get; set; }
    [ObservableProperty]
    public partial SongModelView? ActivePlaylistModel { get; set; } // Renamed from ActivePlaylist

    [ObservableProperty]
    public partial bool IsPlaying { get; set; }

    [ObservableProperty]
    public partial bool IsShuffleActive { get; set; } // Renamed from IsShuffle

    [ObservableProperty]
    public partial RepeatMode CurrentRepeatMode { get; set; } // Renamed from RepeatMode

    [ObservableProperty]
    public partial double CurrentTrackPositionSeconds { get; set; } // Renamed

    [ObservableProperty]
    public partial double CurrentTrackDurationSeconds { get; set; } = 1; // Default to 1 to avoid divide by zero

    [ObservableProperty]
    public partial double CurrentTrackPositionPercentage { get; set; }

    [ObservableProperty]
    public partial double DeviceVolumeLevel { get; set; } // Renamed

    [ObservableProperty]
    public partial string AppTitle { get; set; }


    [ObservableProperty]
    public partial ObservableCollection<SongModelView> NowPlayingDisplayQueue { get; set; } = new(); // Renamed from NowPlayingQueue & PlaylistSongs

    [ObservableProperty]
    public partial ObservableCollection<LyricPhraseModelView>? CurrentSynchronizedLyrics { get; set; }

    [ObservableProperty]
    public partial LyricPhraseModelView? ActiveCurrentLyricPhrase { get; set; }

    [ObservableProperty]
    public partial bool IsMainViewVisible { get; set; } = true;

    [ObservableProperty]
    public partial CurrentPage CurrentPageContext { get; set; } // Renamed

    // --- Properties for selection and page context (as before) ---
    [ObservableProperty]
    public partial SongModelView? SelectedSong { get; set; }

    [ObservableProperty]
    public partial bool IsLoadingSongs { get; set; }

    [ObservableProperty]
    public partial int SettingsPageIndex { get; set; } = 0;

    [ObservableProperty]
    public partial ObservableCollection<string> FolderPaths { get; set; } = new(); // Keep this if populated from _settingsService
    private BaseAppFlow? _baseAppFlow; // This can be used to access common flows if needed, but not directly in BaseViewModel
    public const string CurrentAppVersion = "Dimmer v1.9";

    // User and Online state (as before, but interaction with 
    //public ParseUser? UserOnline { get; set; } 
    [ObservableProperty]
    public partial UserModelView UserLocal { get; set; } // Should be driven by a user service or state

    [ObservableProperty]
    public partial string? LatestScanningLog { get; set; }

    [ObservableProperty]
    public partial AppLogModel? LatestAppLog { get; set; } // Nullable

    [ObservableProperty]
    public partial ObservableCollection<AppLogModel> ScanningLogs { get; set; } = new();

    [ObservableProperty]
    public partial bool IsStickToTop { get; set; } // Should come from _settingsService

    [ObservableProperty]
    public partial bool IsConnected { get; set; } // From DimmerLiveStateService

    // Selection context properties (as before)
    [ObservableProperty] public partial AlbumModelView? SelectedAlbum { get; set; }
    [ObservableProperty] public partial ObservableCollection<ArtistModelView>? SelectedAlbumArtists { get; set; }
    [ObservableProperty] public partial ArtistModelView? SelectedArtist { get; set; }

    [ObservableProperty] public partial PlaylistModelView? SelectedPlaylist { get; set; }
    [ObservableProperty] public partial ObservableCollection<AlbumModelView>? SelectedAlbumsCol { get; set; }
    [ObservableProperty] public partial ObservableCollection<SongModelView>? SelectedAlbumSongs { get; set; }
    [ObservableProperty] public partial ObservableCollection<SongModelView>? SelectedArtistSongs { get; set; }
    [ObservableProperty] public partial ObservableCollection<SongModelView>? SelectedPlaylistSongs { get; set; }
    [ObservableProperty] public partial ObservableCollection<ArtistModelView>? SelectedSongArtists { get; set; }
    [ObservableProperty] public partial ObservableCollection<AlbumModelView>? SelectedArtistAlbums { get; set; }
    [ObservableProperty] public partial CollectionStatsSummary? ArtistCurrentColStats { get; private set; }
    [ObservableProperty] public partial CollectionStatsSummary? AlbumCurrentColStats { get; private set; }


    // To fix the S1699 diagnostic, we need to avoid calling an overridable method from the constructor.
    // The solution is to move the call to `InitializeViewModelSubscriptions` outside the constructor
    // and ensure it is invoked explicitly after the object is constructed.

    public BaseViewModel(
       IMapper mapper,
       IAppInitializerService appInitializerService,
       IDimmerLiveStateService dimmerLiveStateService,
       IDimmerAudioService _audioService,
       AlbumsMgtFlow albumsMgtFlow,
       PlayListMgtFlow playlistsMgtFlow,
       SongsMgtFlow songsMgtFlow,
       IDimmerStateService stateService,
       ISettingsService settingsService,
       SubscriptionManager subsManager,
       LyricsMgtFlow lyricsMgtFlow,
       IFolderMgtService folderMgtService,
       IRepository<SongModel> songRepo,
       IRepository<ArtistModel> artistRepo,
       IRepository<AlbumModel> albumModel,
       IRepository<GenreModel> genreModel,
       ILogger<BaseViewModel> logger)
    {
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        this.appInitializerService=appInitializerService;
        _dimmerLiveStateService = dimmerLiveStateService;

        _albumsMgtFlow = albumsMgtFlow;
        _playlistsMgtFlow = playlistsMgtFlow;
        _songsMgtFlow = songsMgtFlow;
        _stateService = stateService ?? throw new ArgumentNullException(nameof(stateService));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _subsManager = subsManager ?? new SubscriptionManager();
        _folderMgtService = folderMgtService;
        this.songRepo=songRepo;
        this.artistRepo=artistRepo;
        this.albumRepo=albumModel;
        this.genreRepo=genreModel;
        _lyricsMgtFlow = lyricsMgtFlow;
        _logger = logger ?? NullLogger<BaseViewModel>.Instance;
        audioService= _audioService   ?? throw new ArgumentNullException(nameof(audioService));
        UserLocal = new UserModelView();
        dimmerPlayEventRepo ??= IPlatformApplication.Current!.Services.GetService<IRepository<DimmerPlayEvent>>()!;
        Initialize();
    }
    [ObservableProperty]
    public partial PlaylistModelView CurrentlyPlayingPlaylistContext { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<DimmerPlayEvent> AllPlayEvents { get; private set; }

    public void Initialize()
    {
        InitializeViewModelSubscriptions();
    }

    protected virtual void InitializeViewModelSubscriptions() // Changed from Initialize to avoid name clash if derived
    {
        _logger.LogInformation("BaseViewModel: Initializing subscriptions.");

        // --- Subscribe to IDimmerStateService for UI updates ---
        _subsManager.Add(
            _stateService.CurrentSong
                // Ensure UI thread for UI properties
                .Subscribe(songView =>
                {
                    _logger.LogTrace("BaseViewModel: _stateService.CurrentSong emitted: {SongTitle}", songView?.Title ?? "None");
                    CurrentPlayingSongView = songView;
                    CurrentTrackDurationSeconds = songView?.DurationInSeconds ?? 1;
                    AppTitle = songView != null
                        ? $"{songView.Title} - {songView.ArtistName} [{songView.AlbumName}] | {CurrentAppVersion}"
                        : CurrentAppVersion;
                    // Update other song-dependent UI if necessary
                }, ex => _logger.LogError(ex, "Error in CurrentSong subscription"))
        );
        _subsManager.Add(
    _stateService.CurrentPlaylist

        .Subscribe(pm => CurrentlyPlayingPlaylistContext = _mapper.Map<PlaylistModelView>(pm))
);
        _subsManager.Add(
            Observable.FromEventPattern<PlaybackEventArgs>(h => audioService.MediaKeyNextPressed += h, h => audioService.MediaKeyNextPressed -= h)
                .Subscribe(async evt =>
                {

                    await NextTrack();
                    _logger.LogInformation($"Next song is {evt.EventArgs.MediaSong}");
                },
                           ex => _logger.LogError(ex, "Error in play next subscription."))
        );
        _subsManager.Add(
            Observable.FromEventPattern<PlaybackEventArgs>(h => audioService.MediaKeyPreviousPressed += h, h => audioService.MediaKeyPreviousPressed -= h)
                .Subscribe(async evt =>
                {
                    await PreviousTrack();
                    _logger.LogInformation($"Previous song is {evt.EventArgs.MediaSong}");
                },
                           ex => _logger.LogError(ex, "Error in play next subscription."))
        );
        _subsManager.Add(
            Observable.FromEventPattern<PlaybackEventArgs>(h => audioService.IsPlayingChanged += h, h => audioService.IsPlayingChanged -= h)
                .Subscribe(evt =>
                {
                    IsPlaying= evt.EventArgs.IsPlaying;
                },
                           ex => _logger.LogError(ex, "Error in IsPlayingChanged subscription."))
        );
        _subsManager.Add(
            _stateService.IsShuffleActive

                .Subscribe(isShuffle =>
                {
                    _logger.LogTrace("BaseViewModel: _stateService.IsShuffleActive emitted: {IsShuffleState}", isShuffle);
                    IsShuffleActive = isShuffle;
                }, ex => _logger.LogError(ex, "Error in IsShuffleActive subscription"))
        );

        CurrentRepeatMode = _settingsService.RepeatMode; // Initial value

        _subsManager.Add(
            _stateService.DeviceVolume

                .Subscribe(volume =>
                {
                    _logger.LogTrace("BaseViewModel: _stateService.DeviceVolume emitted: {Volume}", volume);
                    DeviceVolumeLevel = volume;
                }, ex => _logger.LogError(ex, "Error in DeviceVolume subscription"))
        );

        // Subscribe to actual audio engine position for the progress bar
        _subsManager.Add(
            _songsMgtFlow.AudioEnginePositionObservable // Use the direct observable from the audio bridge

                .Subscribe(positionSeconds =>
                {
                    CurrentTrackPositionSeconds = positionSeconds;
                    CurrentTrackPositionPercentage = CurrentTrackDurationSeconds > 0 ? (positionSeconds / CurrentTrackDurationSeconds) : 0;

                }, ex => _logger.LogError(ex, "Error in AudioEnginePositionObservable subscription"))
        );


        // This populates the "queue" display if needed. It might be different from PlaylistSongs
        // if PlayListMgtFlow is playing from "All Songs" or a mixed queue.
        _subsManager.Add(
             _stateService.AllCurrentSongs // Or a more specific "current effective queue" observable if PlayListMgtFlow provides it
                .Subscribe(songList =>
                {
                    _logger.LogTrace("BaseViewModel: _stateService.AllCurrentSongs (for NowPlayingDisplayQueue) emitted count: {Count}", songList?.Count ?? 0);
                    NowPlayingDisplayQueue = _mapper.Map<ObservableCollection<SongModelView>>(songList);
                    CurrentPlayingSongView = NowPlayingDisplayQueue.FirstOrDefault();

                }, ex => _logger.LogError(ex, "Error in AllCurrentSongs for NowPlayingDisplayQueue subscription"))
        );

        _subsManager.Add(
             _stateService.AllPlayHistory // Or a more specific "current effective queue" observable if PlayListMgtFlow provides it
                .Subscribe(playEvents =>
                {
                    _logger.LogTrace("BaseViewModel: events (for all) emitted count: {Count}", playEvents?.Count ?? 0);
                    AllPlayEvents = playEvents.ToObservableCollection();
                }, ex => _logger.LogError(ex, "Error in AllCurrentSongs for NowPlayingDisplayQueue subscription"))
        );


        // Subscriptions for lyrics (assuming LyricsMgtFlow updates _stateService)
        _subsManager.Add(_stateService.CurrentLyric

            .DistinctUntilChanged()
            .Subscribe(l => ActiveCurrentLyricPhrase = _mapper.Map<LyricPhraseModelView>(l),
                       ex => _logger.LogError(ex, "Error in CurrentLyric subscription"))
        );

        _subsManager.Add(_stateService.SyncLyrics

             .Subscribe(l => CurrentSynchronizedLyrics = _mapper.Map<ObservableCollection<LyricPhraseModelView>>(l),
                        ex => _logger.LogError(ex, "Error in SyncLyrics subscription"))
        );

        // App Logs
        _subsManager.Add(_stateService.LatestDeviceLog

            .DistinctUntilChanged()
            .Subscribe(log =>
            {
                if (log == null || string.IsNullOrEmpty(log.Log))
                    return;
                LatestScanningLog = log.Log; // If it's a scanning log
                LatestAppLog = log;

                if (log.ViewSongModel != null && CurrentPlayingSongView?.Id != log.ViewSongModel.Id)
                {
                    // If the log has a song context different from current, maybe highlight it briefly
                    // This logic for TemporarilyPickedSong needs review based on its exact purpose.
                }

                ScanningLogs ??= new ObservableCollection<AppLogModel>();
                if (ScanningLogs.Count > 20)
                    ScanningLogs.RemoveAt(0); // Keep it bounded
                ScanningLogs.Add(log);
            }, ex => _logger.LogError(ex, "Error in LatestDeviceLog subscription"))
        );

        // Folder Paths from settings (assuming _folderMgtService updates _settingsService or an observable)
        // This needs a proper observable source if it's dynamic. For now, load once.
        FolderPaths = new ObservableCollection<string>(_settingsService.UserMusicFoldersPreference ?? Enumerable.Empty<string>());

        // Initial log
        LatestAppLog = new AppLogModel { Log = "Dimmer ViewModel Initialized" };
        _logger.LogInformation("BaseViewModel: Subscriptions initialized.");
    }


    // --- Commands that Initiate Playback ---

    public async Task PlaySongFromListAsync(SongModelView songToPlay, IEnumerable<SongModelView> songs) // Renamed, async void is discouraged for RelayCommands unless truly fire-and-forget with no downstream await
    {
        if (songToPlay == null)
        {
            _logger.LogWarning("PlaySongFromList: songToPlay is null.");
            return;
        }

        _baseAppFlow ??= IPlatformApplication.Current?.Services.GetService<BaseAppFlow>();


        _logger.LogInformation("PlaySongFromList: Requesting to play '{SongTitle}'.", songToPlay.Title);

        if (audioService.IsPlaying)
        {
            _baseAppFlow.UpdateDatabaseWithPlayEvent(CurrentPlayingSongView, StatesMapper.Map(DimmerPlaybackState.Skipped), CurrentTrackPositionSeconds);

            await audioService.StopAsync();
        }


        await audioService.InitializeAsync(songToPlay);
        await audioService.PlayAsync();

        var songToPlayModel = songToPlay.ToModel(_mapper);
        if (songToPlayModel == null)
        {
            _logger.LogWarning("PlaySongFromList: Could not map songToPlay to SongModel.");
            return;
        }

        // Determine context: Is there an active playlist model in the global state?
        var activePlaylistContextFromState = this.CurrentlyPlayingPlaylistContext;
        var activePlaylistModel = _mapper.Map<PlaylistModel>(activePlaylistContextFromState);
        if (activePlaylistModel != null && activePlaylistModel.SongsInPlaylist.Any(s => s.Id == songToPlayModel.Id))
        {
            // Song is part of the globally active playlist context
            _logger.LogDebug("PlaySongFromList: Playing from active playlist context '{PlaylistName}'.", activePlaylistModel.PlaylistName);
            int startIndex = activePlaylistModel.SongsInPlaylist.ToList().FindIndex(s => s.Id == songToPlayModel.Id);
            _playlistsMgtFlow.PlayPlaylist(activePlaylistModel, Math.Max(0, startIndex));
        }
        else
        {
            // Song is likely from a generic list (e.g., "All Songs" view, search results)
            // NowPlayingDisplayQueue should represent this generic list if that's what the UI shows.
            // Ensure NowPlayingDisplayQueue accurately reflects the list the user clicked from.
            _logger.LogDebug("PlaySongFromList: Playing from generic list/current display queue.");

            var songListModels = songs
                .Select(svm => svm.ToModel(_mapper))
                .Where(sm => sm != null) // Filter out any nulls after mapping
                .ToList();
            int startIndex = songListModels.FindIndex(s => s?.Id == songToPlayModel.Id);

            _playlistsMgtFlow.PlayGenericSongList(songListModels, Math.Max(0, startIndex), "Custom Context List");

        }

        if (IsShuffleActive)
        {
            _stateService.SetCurrentState(new PlaybackStateInfo(DimmerPlaybackState.ShuffleRequested, null, CurrentPlayingSongView, CurrentPlayingSongView?.ToModel(_mapper)));
        }
    }
    // This method is more high-level, usually called when context is broader
    // than just the currently displayed flat list.
    public void RequestPlayGenericList(IEnumerable<SongModelView> songs, SongModelView? startWithSong, string listName = "Custom List")
    {
        if (songs == null || !songs.Any())
        {
            _logger.LogWarning("RequestPlayGenericList: Provided song list is empty.");
            return;
        }
        var songModels = songs.Select(svm => svm.ToModel(_mapper)).Where(sm => sm != null).ToList()!;
        int startIndex = 0;
        if (startWithSong != null)
        {
            var startWithModel = startWithSong.ToModel(_mapper);
            if (startWithModel != null)
            {
                startIndex = songModels.FindIndex(s => s.Id == startWithModel.Id);
                if (startIndex < 0)
                    startIndex = 0;
            }
        }
        _playlistsMgtFlow.PlayGenericSongList(songModels, startIndex, listName);
    }


    // --- Playback Control Commands (Interacting with _stateService or _songsMgtFlow) ---
    [RelayCommand]
    public async Task PlayPauseToggleAsync()
    {
        _baseAppFlow ??= IPlatformApplication.Current?.Services.GetService<BaseAppFlow>();

        _logger.LogDebug("PlayPauseToggleAsync called. Current IsPlaying state: {IsPlayingState}", IsPlaying);
        SongModelView? currentSongVm = CurrentPlayingSongView; // From _stateService subscription
        if (currentSongVm == null)
        {
            _logger.LogInformation("PlayPauseToggleAsync: No current song. Attempting to play from 'All Songs'.");
            // If no current song, try to play "All Songs" from PlayListMgtFlow
            _playlistsMgtFlow.PlayAllSongsFromLibrary(); // PlayListMgtFlow handles empty library
            return;
        }

        SongModel? currentSongModel = currentSongVm.ToModel(_mapper);
        if (IsPlaying)
        {
            await audioService.PauseAsync(); // Call the audio service to pause playback
            _baseAppFlow.UpdateDatabaseWithPlayEvent(CurrentPlayingSongView, StatesMapper.Map(DimmerPlaybackState.PausedUser), CurrentTrackPositionSeconds);
            _stateService.SetCurrentState(new PlaybackStateInfo(DimmerPlaybackState.PausedDimmer, null, currentSongVm, currentSongModel));
        }
        else
        {
            await audioService.PlayAsync(); // Call the audio service to pause playback

        }
    }

    [RelayCommand]
    public async Task NextTrack()
    {
        _baseAppFlow ??= IPlatformApplication.Current?.Services.GetService<BaseAppFlow>();

        var nextSong = _playlistsMgtFlow.MultiPlayer.Next();

        if (IsPlaying)
        {
            await audioService.StopAsync(); // Call the audio service to pause playback
            _baseAppFlow.UpdateDatabaseWithPlayEvent(CurrentPlayingSongView, StatesMapper.Map(DimmerPlaybackState.Skipped), CurrentTrackPositionSeconds);
        }

        _stateService.SetCurrentState(new PlaybackStateInfo(DimmerPlaybackState.PlayNextUser, null, CurrentPlayingSongView, _mapper.Map<SongModel>(CurrentPlayingSongView)));



        await audioService.InitializeAsync(CurrentPlayingSongView);
        await audioService.PlayAsync();
    }

    [RelayCommand]
    public async Task PreviousTrack()
    {
        _baseAppFlow ??= IPlatformApplication.Current?.Services.GetService<BaseAppFlow>();


        var nextSong = _playlistsMgtFlow.MultiPlayer.Previous();

        if (IsPlaying)
        {
            await audioService.StopAsync(); // Call the audio service to pause playback
        }
        _baseAppFlow.UpdateDatabaseWithPlayEvent(CurrentPlayingSongView, StatesMapper.Map(DimmerPlaybackState.Skipped), CurrentTrackPositionSeconds);
        _stateService.SetCurrentState(new PlaybackStateInfo(DimmerPlaybackState.Skipped, null, CurrentPlayingSongView, _mapper.Map<SongModel>(CurrentPlayingSongView)));

        _stateService.SetCurrentState(new PlaybackStateInfo(DimmerPlaybackState.PlayPreviousUI, null, CurrentPlayingSongView, _mapper.Map<SongModel>(CurrentPlayingSongView)));


        await audioService.InitializeAsync(CurrentPlayingSongView);
        await audioService.PlayAsync();
    }

    [RelayCommand]
    public void ToggleShuffleMode()
    {
        bool newShuffleState = !IsShuffleActive; // IsShuffleActive is bound to 
        // If immediate re-shuffling of the current queue is desired when turning ON:
        if (newShuffleState)
        {
            _stateService.SetCurrentState(new PlaybackStateInfo(DimmerPlaybackState.ShuffleRequested, null, CurrentPlayingSongView, CurrentPlayingSongView?.ToModel(_mapper)));
        }
    }

    [RelayCommand]
    public void ToggleRepeatPlaybackMode() // Renamed
    {
        var currentMode = _settingsService.RepeatMode; // Get from settings
        var nextMode = (RepeatMode)(((int)currentMode + 1) % Enum.GetNames(typeof(RepeatMode)).Length);
        _settingsService.RepeatMode = nextMode; // Update settings
        CurrentRepeatMode = nextMode; // Update UI property
        _logger.LogInformation("Repeat mode toggled to: {RepeatMode}", nextMode);
        // PlayListMgtFlow would need to subscribe to an observable for RepeatMode
        // from _stateService if it's to change its queue behavior.
        _stateService.SetRepeatMode(nextMode); // If _stateService manages this
    }

    [RelayCommand]
    public async Task SeekTrackPosition(double positionSeconds) // Parameter is seconds
    {
        _baseAppFlow ??= IPlatformApplication.Current?.Services.GetService<BaseAppFlow>();

        _baseAppFlow.UpdateDatabaseWithPlayEvent(CurrentPlayingSongView, StatesMapper.Map(DimmerPlaybackState.Seeked), positionSeconds);

        _logger.LogDebug("SeekTrackPosition called by UI to: {PositionSeconds}s", positionSeconds);
        await _songsMgtFlow.RequestSeekAsync(positionSeconds); // Call the method on the refactored _songsMgtFlow
    }


    public async Task RequestSeekPercentage(double percentage)
    {
        if (CurrentTrackDurationSeconds > 0)
        {
            double targetSeconds = percentage * CurrentTrackDurationSeconds;
            await SeekTrackPosition(targetSeconds);
        }
    }


    public void SetVolumeLevel(double newVolume) // Assumes volume is 0.0 to 1.0
    {
        newVolume = Math.Clamp(newVolume, 0.0, 1.0);
        _logger.LogDebug("SetVolumeLevel called by UI to: {Volume}", newVolume);
        _songsMgtFlow.RequestSetVolume(newVolume); // Call the method on the refactored _songsMgtFlow
    }

    [RelayCommand]
    public void IncreaseVolumeLevel()
    {
        SetVolumeLevel(DeviceVolumeLevel + 0.05);
    }

    [RelayCommand]
    public void DecreaseVolumeLevel()
    {
        SetVolumeLevel(DeviceVolumeLevel - 0.05);
    }


    // --- Methods that interact with legacy 
    // These need careful review to ensure they align or are refactored.

    public void SetCurrentlyPickedSongForContext(SongModelView? song) // Renamed from SetCurrentlyPickedSong
    {
        _logger.LogTrace("SetCurrentlyPickedSongForContext called with: {SongTitle}", song?.Title ?? "None");
        SelectedSongForContext = song; // This is a local UI selection context
                                       // The old logic for _stateService.SetSecondSelectdSong is removed as that's not in refactored _stateService.
                                       // If "second selected song" is purely a UI concept for context, this is fine.
                                       // If it was meant to influence playback, that needs a different mechanism.
    }


    public void ViewAlbumDetails(AlbumModelView? albumView) // Can also pass just albumId (Guid/ObjectId)
    {
        var albumDb = albumRepo.GetById(albumView.Id);

        if (albumDb == null)
        {
            _logger.LogWarning("ViewArtistDetails: Album not found in repository for ID: {ArtistId}", albumView?.Id);
            return;
        }

        SelectedAlbum = _mapper.Map<AlbumModelView>(albumDb); // Update the selected album context
        SelectedAlbumSongs = new ObservableCollection<SongModelView>(albumDb.SongsInAlbum.Select(s => _mapper.Map<SongModelView>(s)));


    }


    public void ViewArtistDetails(ArtistModelView? artView) // Can also pass just artistId
    {
        var art = artistRepo.GetById(artView.Id);

        SelectedArtist = _mapper.Map<ArtistModelView>(art); // Update the selected artist context
        SelectedArtistSongs = new ObservableCollection<SongModelView>(art.Songs.AsEnumerable().Select(s => _mapper.Map<SongModelView>(s.Freeze())));

        if (art == null || art.Id == default)
        {
            _logger.LogWarning("ViewArtistDetails: art or its ID is null/default.");
            return;
        }
        SelectedArtist.ImagePath = SelectedArtistSongs[0].CoverImagePath;
        _logger.LogInformation("Requesting to navigate to artist details for ID: {ArtistId}", art.Id);
        // _navigationService.NavigateTo(nameof(ArtistDetailPageViewModel), art.Id); // Example
    }

    // Wrapper for legacy 
    // This should be phased out by moving logic into proper services/repositories.
    public void SaveUserNoteToDbLegacy(UserNoteModelView userNote, SongModelView songWithNote)
    {
        if (userNote == null || songWithNote == null)
            return;
        _logger.LogInformation("Saving user note for song: {SongTitle}", songWithNote.Title);
        var songDb = songWithNote.ToModel(_mapper);
        var userNoteDb = _mapper.Map<UserNoteModel>(userNote);
        if (songDb != null && userNoteDb != null)
        {

        }
    }

    public void UpdateAppStickToTop(bool isStick)
    {
        IsStickToTop = isStick; // Update UI property
        //_settingsService.?? = isStick; // Persist setting
        _logger.LogInformation("StickToTop setting changed to: {IsStickToTop}", isStick);
        // The actual window behavior would be handled by platform-specific code observing this setting.
    }


    // [RelayCommand] public async Task StartChatWithUser(string userId) { ... }
    // [RelayCommand] public async Task SendChatMessage(string message) { ... }
    // [RelayCommand] public async Task ShareSongDataWithUser(SongModelView song, string userId) { ... }


    // --- Folder Management (delegates to _folderMgtService or 
    [RelayCommand]
    public void DeleteFolderPath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return;
        _logger.LogInformation("Requesting to delete folder path: {Path}", path);
        FolderPaths.Remove(path); // Update UI
        _folderMgtService.RemoveFolderFromWatchListAsync(path); // Tell service
                                                                // Optionally trigger a rescan if needed, or let FolderMgtService handle that via state.
    }

    public async Task AddMusicFolderAsync(string folderPath) // Renamed from SelectSongFromFolder
    {
        _logger.LogInformation("User requested to add music folder.");

        await _folderMgtService.AddFolderToWatchListAndScanAsync(folderPath);

    }
    [ObservableProperty] public partial ObservableCollection<DimmerPlayEventView>? SongEvts { get; set; }
    [ObservableProperty] public partial ObservableCollection<PlayEventGroup>? GroupedPlayEvents { get; set; } = new();
    public void LoadStats()
    {

        SongEvts ??= new();
        SongEvts= _mapper.Map<ObservableCollection<DimmerPlayEventView>>(dimmerPlayEventRepo.GetAll().ToList());

        GroupedPlayEvents.Clear();

        GroupedPlayEvents = SongEvts
           .GroupBy(e => e.PlayTypeStr ?? "Unknown")
           .Select(g => new PlayEventGroup(
               g.Key,
               g.OrderByDescending(e => e.DatePlayed).ToList() // Now g contains DimmerPlayEventDisplay
           ))
           .OrderBy(group => group.Name)
           .ToObservableCollection();

        //SummaryStatsForAllSongs = plays
    }


    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _logger.LogInformation("Disposing BaseViewModel.");
            _subsManager.Dispose(); // Dispose all Rx subscriptions registered with it
        }
    }
}