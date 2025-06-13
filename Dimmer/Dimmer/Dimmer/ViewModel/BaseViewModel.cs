﻿using System.Diagnostics;
using System.Threading.Tasks;

using ATL;

using CommunityToolkit.Mvvm.Input;

using Dimmer.Data.ModelView.NewFolder;
using Dimmer.Interfaces.Services;
using Dimmer.Interfaces.Services.Interfaces;
using Dimmer.Utilities.Events;
using Dimmer.Utilities.Extensions;
using Dimmer.Utilities.StatsUtils;

using Microsoft.Extensions.Logging.Abstractions;


using static Dimmer.Utilities.StatsUtils.SongStatTwop;





namespace Dimmer.ViewModel;

public partial class BaseViewModel : ObservableObject, IDisposable
{

    public readonly IMapper _mapper;
    private readonly IAppInitializerService appInitializerService;
    private readonly IDimmerLiveStateService _dimmerLiveStateService;
    private readonly AlbumsMgtFlow _albumsMgtFlow;
    private readonly PlayListMgtFlow _playlistsMgtFlow;
    private readonly SongsMgtFlow _songsMgtFlow;
    protected readonly IDimmerStateService _stateService;
    protected readonly ISettingsService _settingsService;
    protected readonly SubscriptionManager _subsManager;
    protected readonly IFolderMgtService _folderMgtService;
    private readonly IRepository<SongModel> songRepo;
    private readonly IRepository<ArtistModel> artistRepo;
    private readonly IRepository<PlaylistModel> playlistRepo;
    private readonly IRepository<AlbumModel> albumRepo;
    private readonly IRepository<GenreModel> genreRepo;
    private readonly IRepository<DimmerPlayEvent> dimmerPlayEventRepo;
    private readonly LyricsMgtFlow _lyricsMgtFlow;
    protected readonly ILogger<BaseViewModel> _logger;
    private readonly IDimmerAudioService audioService;
    private readonly ILibraryScannerService libService;
    [ObservableProperty]
    public partial ObservableCollection<LyricPhraseModelView> AllLines { get; set; }
    [ObservableProperty]
    public partial LyricPhraseModelView? PreviousLine { get; set; }
    [ObservableProperty]
    public partial LyricPhraseModelView? CurrentLine { get; set; }
    [ObservableProperty]
    public partial LyricPhraseModelView? NextLine { get; set; }
    [ObservableProperty]
    public partial ObservableCollection<DimmerPlayEventView> DimmerPlayEventList { get; set; } = new();

    [ObservableProperty]
    public partial ObservableCollection<PlaylistModelView> AllPlaylists { get; set; } = new();



    public IDimmerLiveStateService DimmerLiveStateService { get; }



    [ObservableProperty]
    public partial CollectionStatsSummary? SummaryStatsForAllSongs { get; set; }

    [ObservableProperty] public partial int? CurrentTotalSongsOnDisplay { get; set; }
    [ObservableProperty] public partial int? CurrentSortOrderInt { get; set; }
    [ObservableProperty] public partial string? CurrentSortProperty { get; set; } = "Title";
    [ObservableProperty] public partial SortOrder CurrentSortOrder { get; set; } = SortOrder.Ascending;

    [ObservableProperty] public partial ObservableCollection<AudioOutputDevice>? AudioDevices { get; set; }
    [ObservableProperty] public partial List<string>? SortingModes { get; set; } = new List<string> { "Title", "Artist", "Album", "Duration", "Year" };
    [ObservableProperty] public partial AudioOutputDevice? SelectedAudioDevice { get; set; }
    [ObservableProperty] public partial string? SelectedSortingMode { get; set; }
    [ObservableProperty] public partial bool? IsAscending { get; set; }
    [ObservableProperty]
    public partial SongModelView? SelectedSongForContext { get; set; }
    [ObservableProperty]
    public partial SongModelView? ActivePlaylistModel { get; set; }

    [ObservableProperty]
    public partial bool IsPlaying { get; set; }

    [ObservableProperty]
    public partial bool IsShuffleActive { get; set; }

    [ObservableProperty]
    public partial RepeatMode CurrentRepeatMode { get; set; }

    [ObservableProperty]
    public partial double CurrentTrackPositionSeconds { get; set; }

    [ObservableProperty]
    public partial double CurrentTrackDurationSeconds { get; set; } = 1;

    [ObservableProperty]
    public partial double CurrentTrackPositionPercentage { get; set; }

    [ObservableProperty]
    public partial double DeviceVolumeLevel { get; set; }
    partial void OnDeviceVolumeLevelChanged(double oldValue, double newValue)
    {
        _songsMgtFlow.RequestSetVolume(newValue);
    }

    [ObservableProperty]
    public partial string AppTitle { get; set; }

    [ObservableProperty]
    public partial SongModelView? CurrentPlayingSongView { get; set; }
    [ObservableProperty]
    public partial ObservableCollection<SongModelView> NowPlayingDisplayQueue { get; set; } = new();

    [ObservableProperty]
    public partial ObservableCollection<LyricPhraseModelView>? CurrentSynchronizedLyrics { get; set; }

    [ObservableProperty]
    public partial LyricPhraseModelView? ActiveCurrentLyricPhrase { get; set; }

    [ObservableProperty]
    public partial bool IsMainViewVisible { get; set; } = true;

    [ObservableProperty]
    public partial CurrentPage CurrentPageContext { get; set; }


    [ObservableProperty]
    public partial SongModelView? SelectedSong { get; set; }

    [ObservableProperty]
    public partial bool IsLoadingSongs { get; set; }

    [ObservableProperty]
    public partial int SettingsPageIndex { get; set; } = 0;

    [ObservableProperty]
    public partial ObservableCollection<string> FolderPaths { get; set; } = new();

    private BaseAppFlow? _baseAppFlow;
    public const string CurrentAppVersion = "Dimmer v1.9";



    [ObservableProperty]
    public partial UserModelView UserLocal { get; set; }

    [ObservableProperty]
    public partial string? LatestScanningLog { get; set; }

    [ObservableProperty]
    public partial AppLogModel? LatestAppLog { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<AppLogModel> ScanningLogs { get; set; } = new();

    [ObservableProperty]
    public partial bool IsStickToTop { get; set; }

    [ObservableProperty]
    public partial bool IsConnected { get; set; }


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
        audioService= _audioService;
        UserLocal = new UserModelView();
        dimmerPlayEventRepo ??= IPlatformApplication.Current!.Services.GetService<IRepository<DimmerPlayEvent>>()!;
        playlistRepo ??= IPlatformApplication.Current!.Services.GetService<IRepository<PlaylistModel>>()!;
        libService ??= IPlatformApplication.Current!.Services.GetService<ILibraryScannerService>()!;
    }

    [ObservableProperty]
    public partial PlaylistModelView CurrentlyPlayingPlaylistContext { get; set; }

    [ObservableProperty]
    public partial int MaxDeviceVolumeLevel { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<DimmerPlayEvent> AllPlayEvents { get; private set; }

    public async Task Initialize()
    {
        InitializeApp();
        await InitializeViewModelSubscriptions();
        _stateService.LoadAllSongs(songRepo.GetAll());
    }

    public void InitializeApp()
    {
        if (NowPlayingDisplayQueue.Count <1 && _settingsService.UserMusicFoldersPreference.Count >0)
        {
            var listofFOlders = _settingsService.UserMusicFoldersPreference.ToList();

        }
    }
    protected virtual async Task InitializeViewModelSubscriptions()
    {
        _logger.LogInformation("BaseViewModel: Initializing subscriptions.");

        _subsManager.Add(_lyricsMgtFlow.AllSyncLyrics
            .Subscribe(lines => AllLines = lines.ToObservableCollection()));

        _subsManager.Add(_lyricsMgtFlow.CurrentLyric
            .Subscribe(line => CurrentLine = line));

        _subsManager.Add(_lyricsMgtFlow.PreviousLyric
            .Subscribe(line => PreviousLine = line));

        _subsManager.Add(_lyricsMgtFlow.NextLyric
            .Subscribe(line =>
            {
                NextLine = line;
            }));
        _subsManager.Add(
            _stateService.CurrentSong

                .Subscribe(songView =>
                {
                    if (songView is null)
                    {
                        return;
                    }
                    CurrentPlayingSongView = null;
                    //Track trck = new Track(songView.FilePath);
                    CurrentPlayingSongView = songView;
                    //var e = trck.EmbeddedPictures.FirstOrDefault();
                    //if (e is not null && CurrentPlayingSongView.CoverImageBytes is null)
                    //{
                    //CurrentPlayingSongView.CoverImageBytes = e.PictureData;
                    //}
                    _logger.LogTrace("BaseViewModel: _stateService.CurrentSong emitted: {SongTitle}", songView?.Title ?? "None");

                    CurrentTrackDurationSeconds = songView?.DurationInSeconds ?? 1;
                    AppTitle = songView != null
                        ? $"{songView.Title} - {songView.ArtistName} [{songView.AlbumName}] | {CurrentAppVersion}"
                        : CurrentAppVersion;
                    CurrentPlayingSongView.PlayEvents = DimmerPlayEventList.Where(x => x.SongId==CurrentPlayingSongView.Id).ToObservableCollection();
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
            Observable.FromEventPattern<double>(h => audioService.SeekCompleted += h, h => audioService.SeekCompleted -= h)
                .Subscribe(evt =>
                {

                    _baseAppFlow ??= IPlatformApplication.Current?.Services.GetService<BaseAppFlow>();
                    _baseAppFlow.UpdateDatabaseWithPlayEvent(CurrentPlayingSongView, StatesMapper.Map(DimmerPlaybackState.Seeked), evt.EventArgs);
                    _logger.LogInformation($"Seeked to ");
                },
                           ex => _logger.LogError(ex, "Error in play next subscription."))
        );

        _subsManager.Add(
         _stateService.CurrentPlayBackState
             .Where(psi => psi.State == DimmerPlaybackState.FolderScanCompleted)
             .Subscribe(folderPath =>
             {

                 IRepository<AppStateModel> appState = IPlatformApplication.Current.Services.GetService<IRepository<AppStateModel>>();
                 var modell = appState.GetAll().FirstOrDefault();
                 FolderPaths = new ObservableCollection<string>(modell.UserMusicFoldersPreference ?? Enumerable.Empty<string>());

                 NowPlayingDisplayQueue = _mapper.Map<ObservableCollection<SongModelView>>(songRepo.GetAll(true));

                 QueueOfSongsLive = NowPlayingDisplayQueue.Take(50).ToObservableCollection();

                 IsAppScanning=false;
             }, ex => _logger.LogError(ex, "Error processing FolderRemoved state."))
     );

        _subsManager.Add(
            Observable.FromEventPattern<PlaybackEventArgs>(h => audioService.IsPlayingChanged += h, h => audioService.IsPlayingChanged -= h)
                .Subscribe(evt =>
                {
                    IsPlaying= evt.EventArgs.IsPlaying;
                    if (evt.EventArgs.MediaSong is null)
                    {
                        return;
                    }
                    CurrentPlayingSongView =evt.EventArgs.MediaSong;
                    if (IsPlaying)
                    {

                        CurrentPlayingSongView.IsCurrentPlayingHighlight = true;
                        AudioDevices = audioService.GetAllAudioDevices()?.ToObservableCollection();
                        var ll = _playlistsMgtFlow.MultiPlayer.Playlists[0].CurrentItems.Take(50);
                        QueueOfSongsLive = _mapper.Map<ObservableCollection<SongModelView>>(ll);
                    }
                    else
                    {
                        CurrentPlayingSongView.IsCurrentPlayingHighlight = false;
                    }
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

        CurrentRepeatMode = _settingsService.RepeatMode;

        _subsManager.Add(
            _stateService.DeviceVolume

                .Subscribe(volume =>
                {
                    _logger.LogTrace("BaseViewModel: _stateService.DeviceVolume emitted: {Volume}", volume);
                    DeviceVolumeLevel = volume;
                }, ex => _logger.LogError(ex, "Error in DeviceVolume subscription"))
        );


        _subsManager.Add(
            _songsMgtFlow.AudioEnginePositionObservable

                .Subscribe(positionSeconds =>
                {
                    CurrentTrackPositionSeconds = positionSeconds;
                    CurrentTrackPositionPercentage = CurrentTrackDurationSeconds > 0 ? (positionSeconds / CurrentTrackDurationSeconds) : 0;

                }, ex => _logger.LogError(ex, "Error in AudioEnginePositionObservable subscription"))
        );




        _subsManager.Add(
             _stateService.AllCurrentSongs
             .SubscribeOn(await MainThread.GetMainThreadSynchronizationContextAsync())
                .Subscribe(songList =>
                {
                    if (songList.Count < 1)
                    {
                        return;
                    }
                    _logger.LogTrace("BaseViewModel: _stateService.AllCurrentSongs (for NowPlayingDisplayQueue) emitted count: {Count}", songList?.Count ?? 0);
                    NowPlayingDisplayQueue = _mapper.Map<ObservableCollection<SongModelView>>(songList);
                    CurrentPlayingSongView =audioService.CurrentTrackMetadata;

                }, ex => _logger.LogError(ex, "Error in AllCurrentSongs for NowPlayingDisplayQueue subscription"))
        );

        _subsManager.Add(
             _stateService.AllPlayHistory
                .Subscribe(playEvents =>
                {
                    _logger.LogTrace("BaseViewModel: events (for all) emitted count: {Count}", playEvents?.Count ?? 0);
                    AllPlayEvents = playEvents.ToObservableCollection();
                }, ex => _logger.LogError(ex, "Error in AllCurrentSongs for NowPlayingDisplayQueue subscription"))
        );



        _subsManager.Add(_stateService.CurrentLyric

            .DistinctUntilChanged()
            .Subscribe(l => ActiveCurrentLyricPhrase = _mapper.Map<LyricPhraseModelView>(l),
                       ex => _logger.LogError(ex, "Error in CurrentLyric subscription"))
        );

        _subsManager.Add(_stateService.SyncLyrics

             .Subscribe(l => CurrentSynchronizedLyrics = _mapper.Map<ObservableCollection<LyricPhraseModelView>>(l),
                        ex => _logger.LogError(ex, "Error in SyncLyrics subscription"))
        );


        _subsManager.Add(_stateService.LatestDeviceLog

            .DistinctUntilChanged()
            .Subscribe(log =>
            {
                if (log == null || string.IsNullOrEmpty(log.Log))
                    return;
                LatestScanningLog = log.Log;
                LatestAppLog = log;

                if (log.ViewSongModel != null && CurrentPlayingSongView?.Id != log.ViewSongModel.Id)
                {


                }

                if (log.ViewSongModel is null || log.AppSongModel is null)
                {
                    return;
                }
                if (log.ViewSongModel is null || log.AppSongModel is not null)
                {
                    var vSong = _mapper.Map<SongModelView>(log.AppSongModel);
                    if (NowPlayingDisplayQueue.Count <1 || !NowPlayingDisplayQueue.Contains(vSong))
                    {
                        if (NowPlayingDisplayQueue.Count>50)
                        {
                            IsAppScanning=false;
                            return;
                        }
                        IsAppScanning=true;
                        NowPlayingDisplayQueue.Add(vSong);
                    }

                }
                ScanningLogs ??= new ObservableCollection<AppLogModel>();
                if (ScanningLogs.Count > 20)
                    ScanningLogs.RemoveAt(0);
                ScanningLogs.Add(log);
            }, ex => _logger.LogError(ex, "Error in LatestDeviceLog subscription"))
        );



        FolderPaths = new ObservableCollection<string>(_settingsService.UserMusicFoldersPreference ?? Enumerable.Empty<string>());


        LatestAppLog = new AppLogModel { Log = "Dimmer ViewModel Initialized" };
        _logger.LogInformation("BaseViewModel: Subscriptions initialized.");
    }


    [ObservableProperty]
    public partial bool IsAppScanning { get; set; }

    [RelayCommand]
    public void SetPreferredAudioDevice(AudioOutputDevice dev)
    {
        audioService.SetPreferredOutputDevice(dev);
    }
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


    public async Task PlaySongFromListAsync(SongModelView? songToPlay, IEnumerable<SongModelView> songs)
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

            audioService.Stop();
        }




        var songToPlayModel = songToPlay.ToModel(_mapper);
        if (songToPlayModel == null)
        {
            _logger.LogWarning("PlaySongFromList: Could not map songToPlay to SongModel.");
            return;
        }


        var activePlaylistContextFromState = this.CurrentlyPlayingPlaylistContext;
        var activePlaylistModel = _mapper.Map<PlaylistModel>(activePlaylistContextFromState);
        if (activePlaylistModel != null && activePlaylistModel.SongsInPlaylist.Any(s => s.Id == songToPlayModel.Id))
        {

            _logger.LogDebug("PlaySongFromList: Playing from active playlist context '{PlaylistName}'.", activePlaylistModel.PlaylistName);
            int startIndex = activePlaylistModel.SongsInPlaylist.ToList().FindIndex(s => s.Id == songToPlayModel.Id);
            _playlistsMgtFlow.PlayPlaylist(activePlaylistModel, Math.Max(0, startIndex));
            songToPlay = _playlistsMgtFlow.MultiPlayer.Next().ToModelView(_mapper);

        }
        else
        {



            _logger.LogDebug("PlaySongFromList: Playing from generic list/current display queue.");

            var songListModels = songs
                .Select(svm => svm.ToModel(_mapper))
                .Where(sm => sm != null)
                .ToList();
            int startIndex = songListModels.FindIndex(s => s?.Id == songToPlayModel.Id);

            _playlistsMgtFlow.PlayGenericSongList(songListModels, Math.Max(0, startIndex), "Custom Context List");


        }

        if (IsShuffleActive)
        {
            _stateService.SetCurrentState(new PlaybackStateInfo(DimmerPlaybackState.ShuffleRequested, null, CurrentPlayingSongView, CurrentPlayingSongView?.ToModel(_mapper)));
        }
        await audioService.InitializeAsync(songToPlay);
        audioService.Play();
        _baseAppFlow.UpdateDatabaseWithPlayEvent(songToPlay, StatesMapper.Map(DimmerPlaybackState.Playing), CurrentTrackPositionSeconds);

    }

    [RelayCommand]
    public async Task PlayPauseToggleAsync()
    {
        _baseAppFlow ??= IPlatformApplication.Current?.Services.GetService<BaseAppFlow>();

        _logger.LogDebug("PlayPauseToggleAsync called. Current IsPlaying state: {IsPlayingState}", IsPlaying);
        SongModelView? currentSongVm = CurrentPlayingSongView;
        if (currentSongVm == null)
        {
            _logger.LogInformation("PlayPauseToggleAsync: No current song. Attempting to play from 'All Songs'.");

            _playlistsMgtFlow.PlayAllSongsFromLibrary();
            return;
        }

        SongModel? currentSongModel = currentSongVm.ToModel(_mapper);
        if (IsPlaying)
        {
            audioService.Pause();

            _stateService.SetCurrentState(new PlaybackStateInfo(DimmerPlaybackState.PausedDimmer, null, currentSongVm, currentSongModel));
            _baseAppFlow.UpdateDatabaseWithPlayEvent(CurrentPlayingSongView, StatesMapper.Map(DimmerPlaybackState.PausedUser), CurrentTrackPositionSeconds);

        }
        else
        {
            if (audioService.CurrentPosition == 0)
            {
                await audioService.InitializeAsync(CurrentPlayingSongView);
            }
            audioService.Play();
            _baseAppFlow.UpdateDatabaseWithPlayEvent(CurrentPlayingSongView, StatesMapper.Map(DimmerPlaybackState.Resumed), CurrentTrackPositionSeconds);
        }
        var songToPlay = CurrentPlayingSongView;
        var songToPlayModel = songToPlay.ToModel(_mapper);
        if (songToPlayModel == null)
        {
            _logger.LogWarning("PlaySongFromList: Could not map songToPlay to SongModel.");
            return;
        }


        var activePlaylistContextFromState = this.CurrentlyPlayingPlaylistContext;
        var activePlaylistModel = _mapper.Map<PlaylistModel>(activePlaylistContextFromState);
        if (activePlaylistModel != null && activePlaylistModel.SongsInPlaylist.Any(s => s.Id == songToPlayModel.Id))
        {

            _logger.LogDebug("PlaySongFromList: Playing from active playlist context '{PlaylistName}'.", activePlaylistModel.PlaylistName);
            int startIndex = activePlaylistModel.SongsInPlaylist.ToList().FindIndex(s => s.Id == songToPlayModel.Id);
            _playlistsMgtFlow.PlayPlaylist(activePlaylistModel, Math.Max(0, startIndex));
        }
        else
        {



            _logger.LogDebug("PlaySongFromList: Playing from generic list/current display queue.");

            var songListModels = NowPlayingDisplayQueue
                .Select(svm => svm.ToModel(_mapper))
                .Where(sm => sm != null)
                .ToList();
            int startIndex = songListModels.FindIndex(s => s?.Id == songToPlayModel.Id);

            _playlistsMgtFlow.PlayGenericSongList(songListModels, Math.Max(0, startIndex), "Custom Context List");

        }

        if (IsShuffleActive)
        {
            _stateService.SetCurrentState(new PlaybackStateInfo(DimmerPlaybackState.ShuffleRequested, null, CurrentPlayingSongView, CurrentPlayingSongView?.ToModel(_mapper)));
        }
    }

    partial void OnCurrentPlayingSongViewChanging(SongModelView? oldValue, SongModelView? newValue)
    {
        if (newValue != audioService.CurrentTrackMetadata)
        {
            CurrentPlayingSongView =audioService.CurrentTrackMetadata;

        }
    }

    [RelayCommand]
    public async Task NextTrack()
    {
        if (NowPlayingDisplayQueue is null ||  NowPlayingDisplayQueue?.Count <1)
        {
            return;
        }
        if (CurrentPlayingSongView is null)
        {

            await PlaySongFromListAsync(NowPlayingDisplayQueue.FirstOrDefault()!, NowPlayingDisplayQueue);

            return;

        }
        _baseAppFlow ??= IPlatformApplication.Current?.Services.GetService<BaseAppFlow>();

        var nextSong = _playlistsMgtFlow.MultiPlayer.Next();

        if (audioService.IsPlaying)
        {
            audioService.Stop();
            _baseAppFlow.UpdateDatabaseWithPlayEvent(CurrentPlayingSongView, StatesMapper.Map(DimmerPlaybackState.Skipped), CurrentTrackPositionSeconds);
        }


        if (nextSong is not null)
        {

            await audioService.InitializeAsync(nextSong.ToModelView(_mapper)!, nextSong.CoverImageBytes);
            audioService.Play();
        }
        else
        {

            await PlaySongFromListAsync(NowPlayingDisplayQueue.FirstOrDefault()!, NowPlayingDisplayQueue);


        }
    }
    [ObservableProperty] public partial ObservableCollection<SongModelView> QueueOfSongsLive { get; set; }
    public void GetPlaylistsQueue()
    {

    }

    [RelayCommand]
    public async Task PreviousTrack()
    {

        if (NowPlayingDisplayQueue is null ||  NowPlayingDisplayQueue?.Count <1)
        {
            return;
        }
        if (CurrentPlayingSongView is null)
        {

            await PlaySongFromListAsync(NowPlayingDisplayQueue.FirstOrDefault()!, NowPlayingDisplayQueue);

            return;
        }
        _baseAppFlow ??= IPlatformApplication.Current?.Services.GetService<BaseAppFlow>();


        var previousSong = _playlistsMgtFlow.MultiPlayer.Previous();

        if (IsPlaying)
        {
            audioService.Stop();
        }
        _baseAppFlow.UpdateDatabaseWithPlayEvent(CurrentPlayingSongView, StatesMapper.Map(DimmerPlaybackState.Skipped), CurrentTrackPositionSeconds);
        _stateService.SetCurrentState(new PlaybackStateInfo(DimmerPlaybackState.Skipped, null, CurrentPlayingSongView, _mapper.Map<SongModel>(CurrentPlayingSongView)));


        if (previousSong is not null)
        {
            await audioService.InitializeAsync(previousSong.ToModelView(_mapper)!, previousSong.CoverImageBytes);
            audioService.Play();
        }
        else
        {
            await PlaySongFromListAsync(NowPlayingDisplayQueue.FirstOrDefault()!, NowPlayingDisplayQueue);


        }
    }

    [RelayCommand]
    public void ToggleShuffleMode()
    {
        if (NowPlayingDisplayQueue is null ||  NowPlayingDisplayQueue?.Count <1)
        {
            return;
        }
        bool newShuffleState = !IsShuffleActive;

        if (newShuffleState)
        {
            _stateService.SetCurrentState(new PlaybackStateInfo(DimmerPlaybackState.ShuffleRequested, null, CurrentPlayingSongView, CurrentPlayingSongView?.ToModel(_mapper)));
        }
    }

    [RelayCommand]
    public void ToggleRepeatPlaybackMode()
    {
        var currentMode = _settingsService.RepeatMode;
        var nextMode = (RepeatMode)(((int)currentMode + 1) % Enum.GetNames(typeof(RepeatMode)).Length);
        _settingsService.RepeatMode = nextMode;
        CurrentRepeatMode = nextMode;
        _logger.LogInformation("Repeat mode toggled to: {RepeatMode}", nextMode);


        _stateService.SetRepeatMode(nextMode);
    }

    [RelayCommand]
    public void SeekTrackPosition(double positionSeconds)
    {
        if (NowPlayingDisplayQueue is null ||  NowPlayingDisplayQueue?.Count <1)
        {
            return;
        }

        _logger.LogDebug("SeekTrackPosition called by UI to: {PositionSeconds}s", positionSeconds);
        _songsMgtFlow.RequestSeek(positionSeconds);


    }


    public void RequestSeekPercentage(double percentage)
    {
        if (CurrentTrackDurationSeconds > 0)
        {
            double targetSeconds = percentage * CurrentTrackDurationSeconds;
            SeekTrackPosition(targetSeconds);
        }
    }


    public void SetVolumeLevel(double newVolume)
    {
        newVolume = Math.Clamp(newVolume, 0.0, 1.0);
        _logger.LogDebug("SetVolumeLevel called by UI to: {Volume}", newVolume);
        _songsMgtFlow.RequestSetVolume(newVolume);
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





    public async Task<bool> SelectedArtistAndNavtoPage(SongModelView? song)
    {
        if (song is null)
        {
            return false;
        }

        SongModel? db = songRepo.GetById(song.Id);
        if (db == null)
        {
            return false;
        }
        string[] AllArtists = db.OtherArtistsName.Split(", ").ToArray();
        _logger.LogTrace("SelectedArtistAndNavtoPage called with song: {SongTitle}", song.Title);
        var result = await Shell.Current.DisplayActionSheet("Select Action", "Cancel", null, AllArtists);
        if (result == "Cancel" || string.IsNullOrEmpty(result))
            return false;

        var art = artistRepo.GetAll().FirstOrDefault(x => x.Name==result);
        var songss = art.Songs.ToList();
        DeviceStaticUtils.SelectedArtistOne = art.ToModelView(_mapper);
        _logger.LogInformation("Selected artist set to: {Artist}", art?.Name);
        return true;
    }
    public void SetCurrentlyPickedSongForContext(SongModelView? song)
    {
        _logger.LogTrace("SetCurrentlyPickedSongForContext called with: {SongTitle}", song?.Title ?? "None");


        song.PlayEvents = DimmerPlayEventList.Where(x => x.SongId==song.Id).ToObservableCollection();
        SelectedSongForContext = song;



    }
    [RelayCommand]
    public void RescanSongs()
    {
        Task.Run(() => libService.ScanLibrary(null));

    }
    [RelayCommand]
    public void LoadInSongsAndEvents()
    {
        Task.Run(() => libService.LoadInSongsAndEvents());

    }
    public void AddMusicFolderByPassingToService(string folderPath)
    {
        _logger.LogInformation("User requested to add music folder.");
        _stateService.SetCurrentState(new PlaybackStateInfo(DimmerPlaybackState.FolderAdded, folderPath, null, null));
    }

    public void ViewAlbumDetails(AlbumModelView? albumView)
    {
        var albumDb = albumRepo.GetById(albumView.Id);

        if (albumDb == null)
        {
            _logger.LogWarning("ViewArtistDetails: Album not found in repository for ID: {ArtistId}", albumView?.Id);
            return;
        }

        SelectedAlbum = _mapper.Map<AlbumModelView>(albumDb);
        SelectedAlbumSongs = new ObservableCollection<SongModelView>(albumDb.SongsInAlbum.Select(s => _mapper.Map<SongModelView>(s)));


    }


    public void ViewArtistDetails(ArtistModelView? artView)
    {
        if (artView?.Id == null)
        {
            _logger.LogWarning("ViewArtistDetails called with a null artist view or ID.");
            return;
        }

        // ====================================================================
        // 1. Fetch ALL Data in a Single, Efficient Operation
        // ====================================================================
        // Get the live artist object. This should be on the correct thread (e.g., UI thread).
        var artist = artistRepo.GetById(artView.Id);
        if (artist == null)
        {
            _logger.LogWarning("Artist with ID {ArtistId} not found in the database.", artView.Id);
            return;
        }

        // Get all songs for this artist. This is a single, efficient query thanks to the backlink.
        // We immediately "freeze" them and map them to ViewModels. Now they are thread-safe
        // and detached from the live Realm instance, perfect for a ViewModel.
        var allArtistSongs = artist.Songs
                                   .AsEnumerable() // Move from IQueryable to IEnumerable
                                   .DistinctBy(s => s.Title)
                                   .Select(s => _mapper.Map<SongModelView>(s.Freeze()))
                                   .ToList(); // Materialize the list in memory

        if (!allArtistSongs.Any()) // Use MoreLINQ's replacement for .Count() > 0
        {
            _logger.LogInformation("Artist {ArtistName} has no songs to display.", artist.Name);
            // We can still show the artist, just with empty lists.
            SelectedArtist = _mapper.Map<ArtistModelView>(artist.Freeze());
            SelectedArtistSongs = new ObservableCollection<SongModelView>();
            SelectedArtistAlbums = new ObservableCollection<AlbumModelView>();
            return;
        }

        // ====================================================================
        // 2. Process Data In-Memory to Fulfill Requirements
        // ====================================================================

        // Requirement 1: Get the artist's image from their FIRST song with cover art.
        // We use FirstOrDefault to be safe against lists where no song has cover art.
        var artistImageBytes = allArtistSongs
            .Select(s => s.CoverImageBytes)
            .FirstOrDefault(bytes => bytes != null && bytes.Length > 0);

        // Requirement 2: Get all unique albums and their cover images.
        // This is the perfect use case for GroupBy!
        var albumsWithCovers = allArtistSongs
            .Where(s => s.Album != null) // Ensure song has an album
            .GroupBy(s => s.Album!.Id)  // Group all songs by their Album's ID
            .Select(albumGroup =>
            {
                // The "Key" of the group is the AlbumId.
                // The group itself (albumGroup) is a collection of all songs for that album.

                // Get the AlbumViewModel from the first song in the group.
                var albumViewModel = albumGroup.First().Album;

                // Find the first song IN THIS GROUP that has cover art.
                albumViewModel.ImageBytes = albumGroup
                    .Select(s => s.CoverImageBytes)
                    .FirstOrDefault(bytes => bytes != null && bytes.Length > 0);

                return albumViewModel;
            })
            .ToList();


        // ====================================================================
        // 3. Atomically Update the ViewModel Properties
        // ====================================================================
        // This ensures the UI updates smoothly all at once.

        SelectedArtist = _mapper.Map<ArtistModelView>(artist.Freeze());
        SelectedArtist.ImageBytes = artistImageBytes;

        SelectedArtistSongs = new ObservableCollection<SongModelView>(allArtistSongs);
        SelectedArtistAlbums = new ObservableCollection<AlbumModelView>(albumsWithCovers);

        var allArtistSongsDb = artist.Songs
                                 .AsEnumerable() // Move from IQueryable to IEnumerable
                                 .DistinctBy(s => s.Title)
                                 .ToList(); // Materialize the list in memory
        var topSongs = TopStats.GetTopCompletedSongs(allArtistSongsDb, dimmerPlayEventRepo.GetAll(), 10);
        foreach (var item in topSongs)
        {
            Console.WriteLine($"#{topSongs.IndexOf(item) + 1}: '{item.Song.Title}' with {item.Count} completions.");
        }

        // Get Top 5 most completed artists of all time
        //var topArtists = TopStats.GetTopCompletedArtists(allArtistSongsDb, allEvents, 5);

        // --- ADVANCED USAGE: Filtering by DATE ---

        // Define a date range for "last month"
        var endDate = DateTimeOffset.UtcNow;
        var startDate = endDate.AddMonths(-1);

        // Get the Top 10 most completed songs in the last month
        TopSongsLastMonth = TopStats.GetTopCompletedSongs(allArtistSongsDb, dimmerPlayEventRepo.GetAll(), 10, startDate, endDate);

        // --- OTHER "TOPS" ---

        // Get the 5 most SKIPPED songs of all time
        MostSkipped = TopStats.GetTopSkippedSongs(allArtistSongsDb, dimmerPlayEventRepo.GetAll(), 5);

        // Get the 10 songs with the most TOTAL LISTENING TIME in the last month
        MostListened = TopStats.GetTopSongsByListeningTime(allArtistSongsDb, dimmerPlayEventRepo.GetAll(), 10, startDate, endDate);

        _logger.LogInformation("Successfully prepared details for artist: {ArtistName}", SelectedArtist.Name);
    }

    public void GetStatsGeneral()
    {
        // Get Top 5 most completed artists of all time
        //var topArtists = TopStats.GetTopCompletedArtists(allArtistSongsDb, allEvents, 5);

        // --- ADVANCED USAGE: Filtering by DATE ---

        // Define a date range for "last month"
        var endDate = DateTimeOffset.UtcNow;
        var startDate = endDate.AddMonths(-1);

        // Get the Top 10 most completed songs in the last month
        TopSongsLastMonth = TopStats.GetTopCompletedSongs(songRepo.GetAll(), dimmerPlayEventRepo.GetAll(), 10, startDate, endDate);

        // --- OTHER "TOPS" ---

        // Get the 5 most SKIPPED songs of all time
        MostSkipped = TopStats.GetTopSkippedSongs(songRepo.GetAll(), dimmerPlayEventRepo.GetAll(), 5);

        // Get the 10 songs with the most TOTAL LISTENING TIME in the last month
        MostListened = TopStats.GetTopSongsByListeningTime(songRepo.GetAll(), dimmerPlayEventRepo.GetAll(), 10, startDate, endDate);
    }
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
        IsStickToTop = isStick;

        _logger.LogInformation("StickToTop setting changed to: {IsStickToTop}", isStick);

    }


    [ObservableProperty]
    public partial List<(SongModel Song, int Count)>? TopSongsLastMonth { get; set; }

    [ObservableProperty]
    public partial List<(SongModel Song, int Count)>? MostSkipped { get; set; }

    [ObservableProperty]
    public partial List<(SongModel Song, double TotalSeconds)>? MostListened { get; set; }





    [RelayCommand]
    public void DeleteFolderPath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return;
        _logger.LogInformation("Requesting to delete folder path: {Path}", path);
        FolderPaths.Remove(path);
        _folderMgtService.RemoveFolderFromWatchListAsync(path);

    }


    [ObservableProperty] public partial ObservableCollection<DimmerPlayEventView>? SongEvts { get; set; }
    [ObservableProperty] public partial SongSingleStatsSummary SingleSongStatsSumm { get; set; }
    [ObservableProperty] public partial int CurrSongCompletedTimes { get; set; }
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
               g.OrderByDescending(e => e.DatePlayed).ToList()
           ))
           .OrderBy(group => group.Name)
           .ToObservableCollection();


    }
    public void LoadStatsForSong(SongModelView? song)
    {
        //return;
        if (song is null && CurrentPlayingSongView is null)
        {
            return;
        }
        SongEvts ??= new();
        var s = songRepo.GetById(song.Id);
        var evts = s.PlayHistory.ToList();
        //SongEvts= _mapper.Map<ObservableCollection<DimmerPlayEventView>>(evts);

        GroupedPlayEvents?.Clear();

        CurrSongCompletedTimes = SongStats.GetCompletedPlayCount(s, evts);

        SingleSongStatsSumm = SongStatTwop.GetSingleSongSummary(s, evts);

    }

    public void LoadStatsApp()
    {
        //return;

        //SongEvts ??= new();
        var s = dimmerPlayEventRepo.GetAll();
        var ss = songRepo.GetAll();
        DimmerPlayEventList= _mapper.Map<ObservableCollection<DimmerPlayEventView>>(s);
        //SongEvts= _mapper.Map<ObservableCollection<DimmerPlayEventView>>(evts);

        GroupedPlayEvents?.Clear();

        //CollectionStatsSummary? sss = CollectionStats.GetSummary(ss, s);

        GetStatsGeneral();
    }

    public void RateSong(int newRating)
    {
        if (CurrentPlayingSongView == null)
        {
            _logger.LogWarning("RateSong called but CurrentPlayingSongView is null.");
            return;
        }
        _logger.LogInformation("Rating song '{SongTitle}' with new rating: {NewRating}", CurrentPlayingSongView.Title, newRating);
        var songModel = CurrentPlayingSongView.ToModel(_mapper);
        if (songModel == null)
        {
            _logger.LogWarning("RateSong: Could not map CurrentPlayingSongView to SongModel.");
            return;
        }
        songModel.Rating = newRating;
        var song = songRepo.AddOrUpdate(songModel);
        _logger.LogInformation("Song '{SongTitle}' updated with new rating: {NewRating}", songModel.Title, newRating);

        _stateService.SetCurrentSong(song);
    }
    [RelayCommand]
    public void ToggleFavSong()
    {
        if (CurrentPlayingSongView == null)
        {
            _logger.LogWarning("RateSong called but CurrentPlayingSongView is null.");
            return;
        }
        var songModel = CurrentPlayingSongView.ToModel(_mapper);
        if (songModel == null)
        {
            _logger.LogWarning("ToggleFavSong: Could not map CurrentPlayingSongView to SongModel.");
            return;
        }
        songModel.IsFavorite = !songModel.IsFavorite;
        var song = songRepo.AddOrUpdate(songModel);

        _stateService.SetCurrentSong(song);
    }


    public void AddToPlaylist(string PlName, List<SongModelView> songs)
    {
        if (string.IsNullOrEmpty(PlName) || songs == null || !songs.Any())
        {
            _logger.LogWarning("AddToPlaylist called with invalid parameters: PlName = '{PlName}', songs count = {Count}", PlName, songs?.Count ?? 0);
            return;
        }

        _logger.LogInformation("Adding songs to playlist '{PlName}'.", PlName);

        var playlistModel = playlistRepo.GetAll();
        PlaylistModel playlistt = new();
        if (playlistModel == null)
        {
            _logger.LogWarning("AddToPlaylist: No playlists found in repository.");
            playlistt.PlaylistName  =PlName;
        }

        if (playlistt.Songs is not null && playlistt.Songs.Count>0)
        {
            var allDistinctSongs = playlistt.Songs.ToList();
            var s = _mapper.Map<List<SongModel>>(songs);
            allDistinctSongs.AddRange(s.Where(ns => !allDistinctSongs.Any(os => os.Id == ns.Id)));
        }
        playlistRepo.AddOrUpdate(playlistt);
        var allPL = playlistRepo.GetAll();
        AllPlaylists = _mapper.Map<ObservableCollection<PlaylistModelView>>(allPL.ToList());

    }

    public void RemoveFromPlaylist(ObjectId Id, List<SongModelView> songs)
    {

        _logger.LogInformation("Removing songs from playlist '{PlName}'.", Id);

        var playlistModel = playlistRepo.GetById(Id);
        if (playlistModel == null)
        {
            _logger.LogWarning("RemoveFromPlaylist: Playlist '{PlName}' not found.", Id);
            return;
        }

        var songsToRemove = _mapper.Map<List<SongModel>>(songs);
        var songsInPlaylist = playlistModel.Songs.ToList();
        songsInPlaylist.RemoveAll(existingSong => songsToRemove.Any(song => song.Id == existingSong.Id));
        playlistModel.Songs.Clear();


        playlistRepo.AddOrUpdate(playlistModel);
        var allPL = playlistRepo.GetAll();
        AllPlaylists = _mapper.Map<ObservableCollection<PlaylistModelView>>(allPL.ToList());
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
            _subsManager.Dispose();
        }
    }
}