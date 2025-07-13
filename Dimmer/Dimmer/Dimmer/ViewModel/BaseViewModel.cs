﻿using ATL;

using CommunityToolkit.Mvvm.Input;

using Dimmer.Charts;
using Dimmer.Data.ModelView.NewFolder;
using Dimmer.Data.RealmStaticFilters;
using Dimmer.DimmerSearch;
using Dimmer.DimmerSearch.AbstractQueryTree;
using Dimmer.DimmerSearch.AbstractQueryTree.NL;
using Dimmer.Interfaces.Services;
using Dimmer.Interfaces.Services.Interfaces;
using Dimmer.Orchestration;
using Dimmer.Utilities.Events;
using Dimmer.Utilities.Extensions;
using Dimmer.Utilities.StatsUtils;
using Dimmer.Utilities.TypeConverters;
using Dimmer.Utilities.ViewsUtils;

using DynamicData;
using DynamicData.Binding;

using Microsoft.Extensions.Logging.Abstractions;

using MoreLinq;

using ReactiveUI;

using Realms;

using Syncfusion.Maui.Toolkit.TextInputLayout;

using System.ComponentModel;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;

using static Dimmer.Data.RealmStaticFilters.MusicPowerUserService;
using static Dimmer.Utilities.AppUtils;





namespace Dimmer.ViewModel;

public partial class BaseViewModel : ObservableObject, IReactiveObject, IDisposable
{
    public BaseViewModel(
       IMapper mapper,
       IAppInitializerService appInitializerService,
       IDimmerLiveStateService dimmerLiveStateService,
       IDimmerAudioService audioServ,
       IDimmerStateService stateService,
       ISettingsService settingsService,
       SubscriptionManager subsManager,
       LyricsMgtFlow lyricsMgtFlow,
       ICoverArtService coverArtService,
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
        _baseAppFlow= IPlatformApplication.Current?.Services.GetService<BaseAppFlow>() ?? throw new ArgumentNullException(nameof(BaseAppFlow));
        _stateService = stateService ?? throw new ArgumentNullException(nameof(stateService));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _subsManager = subsManager ?? new SubscriptionManager();
        _folderMgtService = folderMgtService;
        this.songRepo=songRepo;
        this.artistRepo=artistRepo;
        _coverArtService = coverArtService ?? throw new ArgumentNullException(nameof(coverArtService));
        this.albumRepo=albumModel;
        this.genreRepo=genreModel;
        _lyricsMgtFlow = lyricsMgtFlow;
        _logger = logger ?? NullLogger<BaseViewModel>.Instance;
        this.audioService= audioServ;
        UserLocal = new UserModelView();
        dimmerPlayEventRepo ??= IPlatformApplication.Current!.Services.GetService<IRepository<DimmerPlayEvent>>()!;
        _playlistRepo ??= IPlatformApplication.Current!.Services.GetService<IRepository<PlaylistModel>>()!;
        libService ??= IPlatformApplication.Current!.Services.GetService<ILibraryScannerService>()!;
        AudioEnginePositionObservable = Observable.FromEventPattern<double>(
                                             h => audioServ.PositionChanged += h,
                                             h => audioServ.PositionChanged -= h)
                                         .Select(evt => evt.EventArgs)
                                         .StartWith(audioServ.CurrentPosition)
                                         .Replay(1).RefCount();

        CurrentPlayingSongView=new();
        _baseAppFlow = IPlatformApplication.Current!.Services.GetService<BaseAppFlow>()!;




        folderMonitorService = IPlatformApplication.Current!.Services.GetService<IFolderMonitorService>()!;
        realmFactory = IPlatformApplication.Current!.Services.GetService<IRealmFactory>()!;

        var realm = realmFactory.GetRealmInstance();


        this.musicRelationshipService=new(realmFactory);
        this.musicArtistryService=new(realmFactory);

        this.musicStatsService=new(realmFactory);

        _filterPredicate = new BehaviorSubject<Func<SongModelView, bool>>(song => true);
        _sortComparer = new BehaviorSubject<IComparer<SongModelView>>(new SongModelViewComparer(null));
        _limiterClause = new BehaviorSubject<LimiterClause?>(null);





        var songsStream = realm.All<SongModel>().AsObservableChangeSet<SongModel>();

        songsStream.Throttle(TimeSpan.FromMilliseconds(250), RxApp.MainThreadScheduler)
            .Transform(songModel => _mapper.Map<SongModelView>(songModel))
            .Filter(_filterPredicate)
            .Sort(_sortComparer)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out _searchResults)
            .Subscribe()
            .DisposeWith(Disposables);


        _searchResults.ToObservableChangeSet()
            .Select(_ => _searchResults.Count)
            .StartWith(0)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(count =>
            {
                if (TranslatedSearch is not null && SongsCountLabel is not null)
                {

                    TranslatedSearch.Text = $"{count} Songs";
                    SongsCountLabel.IsVisible = true;
                }
            })
            .DisposeWith(Disposables);


        var allPlayEvents = dimmerPlayEventRepo.GetAll();

        // Step 2: Map them to the ViewModel type.
        var allPlayEventViews = _mapper.Map<IEnumerable<DimmerPlayEventView>>(allPlayEvents);

        // Step 3: Load them into our source list. This fires one big "add" change,
        // which is exactly what our stats pipeline needs to get its initial data.
        _playEventSource.AddRange(allPlayEventViews);


        WireUpLiveStats();
    }


    private readonly ICoverArtService _coverArtService;

    public readonly IMapper _mapper;
    private readonly IAppInitializerService appInitializerService;
    private readonly IDimmerLiveStateService _dimmerLiveStateService;
    protected readonly IDimmerStateService _stateService;
    protected readonly ISettingsService _settingsService;
    protected readonly SubscriptionManager _subsManager;
    protected readonly IFolderMgtService _folderMgtService;
    private readonly IRepository<SongModel> songRepo;
    private readonly IRepository<ArtistModel> artistRepo;
    private readonly IRepository<PlaylistModel> _playlistRepo;
    private readonly IRepository<AlbumModel> albumRepo;
    private readonly IFolderMonitorService folderMonitorService;
    private readonly IRepository<GenreModel> genreRepo;
    private readonly IRepository<DimmerPlayEvent> dimmerPlayEventRepo;
    public readonly LyricsMgtFlow _lyricsMgtFlow;
    private readonly MusicRelationshipService musicRelationshipService;
    private readonly MusicArtistryService musicArtistryService;
    private readonly MusicStatsService musicStatsService;
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
    public partial double? ProgressOpacity { get; set; } = 0.6;
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
    [ObservableProperty] public partial SortOrder CurrentSortOrder { get; set; } = SortOrder.Asc;

    [ObservableProperty] public partial ObservableCollection<AudioOutputDevice>? AudioDevices { get; set; }
    [ObservableProperty] public partial List<string>? SortingModes { get; set; } = new List<string> { "Title", "Artist", "Album", "Duration", "Year" };
    [ObservableProperty] public partial AudioOutputDevice? SelectedAudioDevice { get; set; }
    [ObservableProperty] public partial string? SelectedSortingMode { get; set; }
    [ObservableProperty] public partial bool? IsAscending { get; set; }
    [ObservableProperty]
    public partial SongModelView? SelectedSongForContext { get; set; }


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
        double newVolume = Math.Clamp(newValue, 0.0, 1.0);
        _logger.LogDebug("AudioEngine: UI Requesting SetVolume to {Volume}", newVolume);

        audioService.Volume = newVolume; // Update audio service volume

    }
    [ObservableProperty]
    public partial string AppTitle { get; set; }

    [ObservableProperty]
    public partial SongModelView CurrentPlayingSongView { get; set; }

    [ObservableProperty]
    public partial string? CurrentNoteToSave { get; set; }

    partial void OnSelectedSongForContextChanged(SongModelView? oldValue, SongModelView? newValue)
    {

        if (newValue is not null)
        {
            Track track = new(newValue.FilePath);

            var imgg = track.EmbeddedPictures?.FirstOrDefault()?.PictureData;
            if (imgg is null)
            {
                newValue.CoverImageBytes=null;
                return;
            }

            newValue.CoverImageBytes = ImageResizer.ResizeImage(imgg);

        }

    }
    [RelayCommand]
    public void RefreshSongsMetadata()
    {

        Task.Run(() =>
        {
            if (_songSource == null || !_songSource.Items.Any())
            {
                return;
            }


            if (realmFactory is null)
            {
                _logger.LogError("RealmFactory service is not registered.");
                return;
            }
            var realm = realmFactory.GetRealmInstance();
            if (realm is null)
            {
                _logger.LogError("Failed to get Realm instance from RealmFactory.");
                return;
            }



            var songIdsToUpdate = _songSource.Items.Select(s => s.Id).ToList();


            var allArtistNames = _songSource.Items
                .SelectMany(s => s.OtherArtistsName.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries))
                .Distinct()
                .ToList();




            var songClauses = Enumerable.Range(0, songIdsToUpdate.Count)
                                     .Select(i => $"Id == ${i}");

            var songQueryString = string.Join(" OR ", songClauses);

            var songQueryArgs = songIdsToUpdate.Select(id => (QueryArgument)id).ToArray();


            var songsFromDb = realm.All<SongModel>()
                                   .Filter(songQueryString, songQueryArgs)
                                   .ToDictionary(s => s.Id);



            var artistClauses = Enumerable.Range(0, allArtistNames.Count)
                                          .Select(i => $"Name == ${i}");

            var artistQueryString = string.Join(" OR ", artistClauses);

            QueryArgument[]? artistQueryArgs = [.. allArtistNames.Select(name => (QueryArgument)name)];


            var artistsFromDb = realm.All<ArtistModel>()
                                       .Filter(artistQueryString, artistQueryArgs)
                                       .ToDictionary(a => a.Name);


            var songsss = _songSource.Items.ToList();
            realm.Write(() =>
            {

                foreach (var songViewModel in songsss)
                {

                    if (!songsFromDb.TryGetValue(songViewModel.Id, out var songDb))
                    {
                        _logger.LogWarning("Song with ID {SongId} not found in DB, skipping.", songViewModel.Id);
                        continue;
                    }


                    if (songDb.Album == null)
                    {
                        _logger.LogWarning("Song '{Title}' has no associated album, cannot update album artists.", songDb.Title);
                        continue;
                    }

                    var artistNamesForThisSong = songViewModel.OtherArtistsName.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (var artistName in artistNamesForThisSong)
                    {

                        var songHasArtist = songDb.ArtistToSong.FirstOrDefault(a => a.Name == artistName);
                        if (songHasArtist is not null)
                        {
                            continue;
                        }


                        if (artistsFromDb.TryGetValue(artistName, out var artistModel))
                        {

                            songDb.ArtistToSong.Add(artistModel);


                            if (songDb.Album.ArtistIds != null && songDb.Album.ArtistIds.FirstOrDefault(a => a.Id == artistModel.Id) is null)
                            {
                                songDb.Album.ArtistIds.Add(artistModel);
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Artist '{ArtistName}' not found in DB. Cannot link to song '{Title}'.", artistName, songDb.Title);
                        }
                    }
                }
            });


            var songss = _mapper.Map<ObservableCollection<SongModelView>>(songRepo.GetAll(true));




        });
    }

    [RelayCommand]
    public void RefreshSongMetadata(SongModelView songViewModel)
    {
        if (songViewModel == null)
            return;

        Task.Run(() =>
        {

            if (realmFactory is null)
            {
                _logger.LogError("RealmFactory service is not registered.");
                return;
            }
            var realm = realmFactory.GetRealmInstance();
            if (realm is null)
            {
                _logger.LogError("Failed to get Realm instance from RealmFactory.");
                return;
            }


            var songDb = realm.Find<SongModel>(songViewModel.Id);
            if (songDb == null)
            {
                _logger.LogWarning("Song with ID {SongId} not found in DB. Cannot refresh metadata.", songViewModel.Id);
                return;
            }



            var artistNamesToLink = songDb.OtherArtistsName
                .Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries)
                .ToList();

            if (artistNamesToLink.Count==0)
            {
                _logger.LogInformation("No 'OtherArtists' found for song '{Title}'. Nothing to link.", songDb.Title);
                return;
            }



            var artistClauses = Enumerable.Range(0, artistNamesToLink.Count)
                                          .Select(i => $"Name == ${i}");


            var artistQueryString = string.Join(" OR ", artistClauses);


            var artistQueryArgs = artistNamesToLink.Select(name => (QueryArgument)name).ToArray();


            var artistsFromDb = realm.All<ArtistModel>()
                                       .Filter(artistQueryString, artistQueryArgs)
                                       .ToDictionary(a => a.Name);


            realm.Write(() =>
            {


                var freshSongDb = realm.Find<SongModel>(songViewModel.Id);
                if (freshSongDb == null)
                    return;


                if (freshSongDb.Album == null)
                {
                    _logger.LogWarning("Song '{Title}' has no associated album, cannot update album artists.", freshSongDb.Title);
                    return;
                }

                foreach (var artistName in artistNamesToLink)
                {

                    bool songHasArtist = freshSongDb.ArtistToSong.Any(a => a.Name == artistName);
                    if (songHasArtist)
                    {
                        continue;
                    }


                    if (artistsFromDb.TryGetValue(artistName, out var artistModel))
                    {

                        freshSongDb.ArtistToSong.Add(artistModel);
                        _logger.LogInformation("Linked artist '{ArtistName}' to song '{Title}'.", artistName, freshSongDb.Title);


                        bool albumHasArtist = freshSongDb.Album.ArtistIds.Any(a => a.Id == artistModel.Id);
                        if (!albumHasArtist)
                        {
                            freshSongDb.Album.ArtistIds.Add(artistModel);
                            _logger.LogInformation("Linked artist '{ArtistName}' to album '{AlbumTitle}'.", artistName, freshSongDb.Album.Name);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Artist '{ArtistName}' not found in DB. Cannot link to song '{Title}'.", artistName, freshSongDb.Title);
                    }
                }

            });













            _logger.LogInformation("Successfully finished refreshing metadata for song ID {SongId}", songViewModel.Id);
        });
    }

    private readonly SourceList<SongModelView> _songSource = new SourceList<SongModelView>();
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

    private readonly BaseAppFlow _baseAppFlow;
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
    [ObservableProperty] public partial ObservableCollection<PlaylistModelView>? AllPlaylistsFromDBView { get; set; }
    [ObservableProperty] public partial ObservableCollection<ArtistModelView>? SelectedSongArtists { get; set; }
    [ObservableProperty] public partial ObservableCollection<AlbumModelView>? SelectedArtistAlbums { get; set; }
    [ObservableProperty] public partial CollectionStatsSummary? ArtistCurrentColStats { get; private set; }
    [ObservableProperty] public partial CollectionStatsSummary? AlbumCurrentColStats { get; private set; }






    public string CurrentQuery { get; private set; }
    public string QueryBeforePlay { get; private set; }


    public void LoadStatsApp()
    {


        var s = dimmerPlayEventRepo.GetAll();
        DimmerPlayEventList= _mapper.Map<ObservableCollection<DimmerPlayEventView>>(s);



        GetStatsGeneral();
    }
    private readonly SourceList<DimmerPlayEventView> _playEventSource = new();
    private readonly CompositeDisposable _disposables = new();
    private IDisposable? _realmSubscription;
    private bool _isDisposed;

    [ObservableProperty]
    public partial SongViewMode CurrentSongViewMode { get; set; } = SongViewMode.DetailedGrid;









    public ReadOnlyObservableCollection<SongModelView> SearchResults => _searchResults;

    private readonly ReadOnlyObservableCollection<SongModelView> _fullFilteredResults;
    protected CompositeDisposable Disposables { get; } = new CompositeDisposable();


    [ObservableProperty]
    public partial Label SongsCountLabel { get; set; }
    [ObservableProperty]
    public partial Label TranslatedSearch { get; set; }

    private ReadOnlyObservableCollection<SongModelView> _searchResults;
    private readonly BehaviorSubject<LimiterClause?> _limiterClause;


    private readonly BehaviorSubject<Func<SongModelView, bool>> _filterPredicate;
    private readonly BehaviorSubject<IComparer<SongModelView>> _sortComparer;


    [RelayCommand]
    public void SmolHold()
    {
        SearchSongSB_TextChanged("Len:<=2:00");
    }

    [RelayCommand]
    public void BigHold()
    {
        SearchSongSB_TextChanged("Len:<=3:00");
    }

    [RelayCommand]
    public void ResetSearch()
    {
        _filterPredicate.OnNext(song => true);
        _sortComparer.OnNext(new SongModelViewComparer(null));
        _limiterClause.OnNext(null);
        SearchSongSB_TextChanged(string.Empty);
    }
    public void SearchSongSB_TextChanged(string searchText)
    {


        CurrentQuery= searchText;
        if (string.IsNullOrWhiteSpace(searchText))
        {
            _filterPredicate.OnNext(song => true);
            _sortComparer.OnNext(new SongModelViewComparer(null));
            _limiterClause.OnNext(null);

            return;
        }

        try
        {

            var orchestrator = new MetaParser(searchText);


            var filterPredicate = orchestrator.CreateMasterPredicate();
            var sortComparer = orchestrator.CreateSortComparer();
            var limiterClause = orchestrator.CreateLimiterClause();



            _filterPredicate.OnNext(filterPredicate);
            _sortComparer.OnNext(sortComparer);
            _limiterClause.OnNext(limiterClause);

            CurrentPlaybackQuery=searchText;
        }
        catch (Exception ex)
        {
            _filterPredicate.OnNext(song => false);


            _sortComparer.OnNext(new SongModelViewComparer(null));


            _limiterClause.OnNext(null);

            _logger.Log(LogLevel.Debug, ex, "Error while parsing search text: {SearchText}", searchText);

        }



    }

    private void WireUpLiveStats()
    {
        var filteredSongsStream = _searchResults.ToObservableChangeSet()
    .Throttle(TimeSpan.FromMilliseconds(500), RxApp.MainThreadScheduler)
    .ToCollection()
    .StartWith(_searchResults); // Start with the initial collection

        // Stream 2: A stream that fires whenever the full list of play events changes.
        // This is your existing _playEventSource.
        var allPlayEventsStream = _playEventSource.Connect()
            .ToCollection()
            .StartWith(new List<DimmerPlayEventView>()); // Start with an empty list

        filteredSongsStream.CombineLatest(allPlayEventsStream, (filteredSongs, allEvents) =>
        {
            // This is the "combiner" function. It runs on a background thread
            // thanks to ObserveOn(TaskPoolScheduler.Default) below.
            // We pass both the current songs and all events to our calculation method.
            return CalculateStats(filteredSongs, allEvents);
        })
       .ObserveOn(TaskPoolScheduler.Default) // Perform the calculation on a background thread
       .ObserveOn(RxApp.MainThreadScheduler)   // Switch back to the UI thread to update properties
       .Subscribe(stats =>
       {
           // The 'stats' object is the result from CalculateStats.
           // Now we can update all our UI properties.
           if (stats != null)
           {
               AllTimeTopSong = stats.TopSong;
               // You can add more properties to the stats result object
               // e.g., TopSongFromFilter = stats.TopSongInFilter;
               //      TotalPlaysInFilter = stats.TotalPlaysInFilter;
           }
       })
       .DisposeWith(Disposables); // Add this combined subscription to our main disposable.
    }


    readonly IRealmFactory realmFactory;
    [ObservableProperty]
    public partial SongStat AllTimeTopSong { get; set; }
    [ObservableProperty]
    public partial double SkipToCompletionRatio { get; set; }
    [ObservableProperty]
    public partial ArtistStat AllTimeTopArtist { get; set; }
    [ObservableProperty]
    public partial PlaylistModelView CurrentlyPlayingPlaylistContext { get; set; }

    [ObservableProperty]
    public partial int MaxDeviceVolumeLevel { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<DimmerPlayEvent> AllPlayEvents { get; private set; }

    public IObservable<bool> AudioEngineIsPlayingObservable { get; }
    public IObservable<double> AudioEnginePositionObservable { get; }
    public IObservable<double> AudioEngineVolumeObservable { get; }
    public void Initialize()
    {
        InitializeApp();

        SubscribeToStateServiceEvents();
        SubscribeToAudioServiceEvents();
        SubscribeToLyricsFlow();
    }

    public void InitializeApp()
    {
        if (_songSource.Count <1 && _settingsService.UserMusicFoldersPreference.Count >0)
        {
            var listofFOlders = _settingsService.UserMusicFoldersPreference.ToList();

        }
    }
    #region Subscription Event Handlers (The Reactive Logic)

    private void OnPlaybackStarted(PlaybackEventArgs args)
    {
        if (args.MediaSong is null)
        {
            _logger.LogWarning("OnPlaybackPaused was called but the event had no song context.");
            return;
        }
        CurrentPlayingSongView = args.MediaSong;


        _logger.LogInformation("AudioService confirmed: Playback started for '{Title}'", args.MediaSong.Title);
        _baseAppFlow.UpdateDatabaseWithPlayEvent(realmFactory, args.MediaSong, StatesMapper.Map(DimmerPlaybackState.Playing), 0);
        UpdateSongSpecificUi(CurrentPlayingSongView);
    }
    private void UpdateSongSpecificUi(SongModelView? song)
    {
        if (song is null)
        {
            // What should the UI show when nothing is playing?
            AppTitle = "Dimmer"; // Reset the title
            CurrentTrackDurationSeconds = 1; // Prevent division by zero
            return;
        }

        AppTitle = $"{song.Title} - {song.ArtistName} | {CurrentAppVersion}";
        CurrentTrackDurationSeconds = song.DurationInSeconds > 0 ? song.DurationInSeconds : 1;
        // Trigger the new, evolved cover art loading process
        LoadAndCacheCoverArtAsync(song);
    }

    /// <summary>
    /// A robust, multi-stage process to load cover art. It prioritizes existing paths,
    /// checks for cached files, and only extracts from the audio file as a last resort,
    /// caching the result for future use.
    /// </summary>
    private void LoadAndCacheCoverArtAsync(SongModelView song)
    {
        // Don't start the process if the image is already loaded in the UI object.
        if (song.CoverImageBytes != null && song.CoverImageBytes.Length>1)
            return;

        Task.Run(async () =>
        {
            // --- Stage 1: Check for an existing path in our data model ---
            if (!string.IsNullOrEmpty(song.CoverImagePath) && File.Exists(song.CoverImagePath))
            {
                try
                {
                    song.CoverImageBytes = await File.ReadAllBytesAsync(song.CoverImagePath);
                    // No DB update needed, the path was already correct.
                    _logger.LogTrace("Loaded cover art from existing path: {CoverImagePath}", song.CoverImagePath);
                    return; // We're done!
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load cover art from existing path {CoverImagePath}", song.CoverImagePath);
                    // The path might be invalid, so we continue to the next stage.
                }
            }

            // --- Stage 2: Extract picture info from the audio file using ATL ---
            PictureInfo? embeddedPicture = null;
            try
            {
                var track = new Track(song.FilePath);
                embeddedPicture = track.EmbeddedPictures?.FirstOrDefault(p => p.PictureData?.Length > 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read audio file with ATL: {FilePath}", song.FilePath);
                return; // Can't proceed without reading the file.
            }

            // --- Stage 3: Use the CoverArtService to save or get the image path ---
            // This will either return an existing cached path or save the new one.
            string? finalImagePath = await _coverArtService.SaveOrGetCoverImageAsync(song.FilePath, embeddedPicture);

            if (finalImagePath == null)
            {
                _logger.LogTrace("No cover art found or could be saved for {FilePath}", song.FilePath);
                return; // No cover art available.
            }

            // --- Stage 4: Update the UI and the Database ---
            try
            {
                // Load the image bytes for the UI
                song.CoverImageBytes = await File.ReadAllBytesAsync(finalImagePath);
                _logger.LogTrace("Loaded cover art from new/cached path: {ImagePath}", finalImagePath);

                // If the path is new, update our song model and save it to the database.
                if (song.CoverImagePath != finalImagePath)
                {
                    song.CoverImagePath= finalImagePath;
                    var realm = realmFactory.GetRealmInstance();
                    if (realm is null)
                    {
                        _logger.LogError("Failed to get Realm instance from RealmFactory.");
                        return;
                    }
                    // Update the song in the database with the new cover image path.
                    realm.Write(() =>
                    {
                        var songToUpdate = realm.Find<SongModel>(song.Id);
                        if (songToUpdate != null)
                        {
                            songToUpdate.CoverImagePath = finalImagePath;
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load or update cover art from final path: {ImagePath}", finalImagePath);
            }
        });
    }

    [RelayCommand]
    private async Task PreCacheArtForVisibleSongsAsync()
    {
        // Get a copy of the current list to avoid issues if it changes during the process.
        var songsToProcess = SearchResults.ToList();

        _logger.LogInformation("Starting to pre-cache cover art for {Count} visible songs.", songsToProcess.Count);

        // This is a great use case for parallel processing.
        await Parallel.ForEachAsync(songsToProcess, async (song, cancellationToken) =>
        {
            // We only need to process songs that don't already have a valid path.
            if (string.IsNullOrEmpty(song.CoverImagePath) || !File.Exists(song.CoverImagePath))
            {
                // We re-use the same core logic, but we don't need to load the bytes into the UI here.
                try
                {
                    var track = new Track(song.FilePath);
                    var embeddedPicture = track.EmbeddedPictures?.FirstOrDefault(p => p.PictureData?.Length > 0);

                    string? finalCoverImagePath = await _coverArtService.SaveOrGetCoverImageAsync(song.FilePath, embeddedPicture);

                    if (finalCoverImagePath != null && song.CoverImagePath != finalCoverImagePath)
                    {
                        song.CoverImagePath = finalCoverImagePath;
                        var realm = realmFactory.GetRealmInstance();
                        if (realm is null)
                        {
                            _logger.LogError("Failed to get Realm instance from RealmFactory.");
                            return;
                        }
                        // Update the song in the database with the new cover image path.
                        realm.Write(() =>
                        {
                            var songToUpdate = realm.Find<SongModel>(song.Id);
                            if (songToUpdate != null)
                            {
                                songToUpdate.CoverImagePath = finalCoverImagePath;
                            }
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to pre-cache art for {FilePath}", song.FilePath);
                }
            }
        });

        _logger.LogInformation("Finished pre-caching cover art process.");
    }
    private void OnPlaybackPaused(PlaybackEventArgs args)
    {
        if (args.MediaSong is null)
        {
            _logger.LogWarning("OnPlaybackPaused was called but the event had no song context.");
            return;
        }
        var isAtEnd = Math.Abs(CurrentTrackDurationSeconds - CurrentTrackPositionSeconds) < 0.5; // Within 0.5s of the end
        if (isAtEnd && CurrentTrackDurationSeconds > 0)
        {
            _logger.LogTrace("Ignoring Paused event at the end of the track, waiting for Completed event.");
            return; // Do not log the pause
        }

        _logger.LogInformation("AudioService confirmed: Playback paused for '{Title}'", args.MediaSong.Title);
        _baseAppFlow.UpdateDatabaseWithPlayEvent(realmFactory, args.MediaSong, StatesMapper.Map(DimmerPlaybackState.PausedUser), CurrentTrackPositionSeconds);
    }

    private void OnPlaybackResumed(PlaybackEventArgs args)
    {
        if (args.MediaSong is null)
        {
            _logger.LogWarning("OnPlaybackPaused was called but the event had no song context.");
            return;
        }

        _logger.LogInformation("AudioService confirmed: Playback resumed for '{Title}'", args.MediaSong.Title);
        _baseAppFlow.UpdateDatabaseWithPlayEvent(realmFactory, args.MediaSong, StatesMapper.Map(DimmerPlaybackState.Resumed), CurrentTrackPositionSeconds);
    }

    private void OnPlaybackEnded()
    {
        _logger.LogInformation("AudioService confirmed: Playback ended for '{Title}'", CurrentPlayingSongView?.Title ?? "N/A");
        if (CurrentPlayingSongView != null)
        {
            // Only log as 'Completed' if it played to the end naturally.
            // 'Skipped' is handled in the StartAudioForSongAtIndex method.
            _baseAppFlow.UpdateDatabaseWithPlayEvent(realmFactory, CurrentPlayingSongView, StatesMapper.Map(DimmerPlaybackState.PlayCompleted), CurrentTrackDurationSeconds);
        }
        // Automatically play the next song in the queue.
        NextTrack();
    }

    private void OnSeekCompleted(double newPosition)
    {

        _logger.LogInformation("AudioService confirmed: Seek completed to {Position}s.", newPosition);
        _baseAppFlow.UpdateDatabaseWithPlayEvent(realmFactory, CurrentPlayingSongView, StatesMapper.Map(DimmerPlaybackState.Seeked), newPosition);
    }

    private void OnPositionChanged(double positionSeconds)
    {
        CurrentTrackPositionSeconds = positionSeconds;
        CurrentTrackPositionPercentage = CurrentTrackDurationSeconds > 0 ? (positionSeconds / CurrentTrackDurationSeconds) : 0;
    }

    private void OnCurrentSongChanged(SongModelView? songView)
    {
        if (songView is null)
            return;

        CurrentPlayingSongView = songView;
        CurrentTrackDurationSeconds = songView.DurationInSeconds > 0 ? songView.DurationInSeconds : 1;
        AppTitle = $"{songView.Title} - {songView.ArtistName} | {CurrentAppVersion}";

        // Efficiently load related data
        CurrentPlayingSongView.PlayEvents = _mapper.Map<ObservableCollection<DimmerPlayEventView>>(
            songRepo.GetById(songView.Id)?.PlayHistory
        );
    }

    private void OnFolderScanCompleted(PlaybackStateInfo stateInfo)
    {
        // This logic was okay, just moved to a dedicated handler.
        _logger.LogInformation("Folder scan completed. Refreshing UI.");
        // ... your existing logic to refresh FolderPaths and trigger metadata scan ...
        IsAppScanning = false;
    }

    #endregion

    #region Playback Commands (User Intent)

    #region Playback Commands (User Intent)

    [RelayCommand]
    public void PlaySong(SongModelView? songToPlay)
    {
        if (songToPlay == null)
        {
            _logger.LogWarning("PlaySong command called with a null song.");
            return;
        }

        // --- Step 1: Establish the Playback Context ---
        var baseQueue = _searchResults.Select(s => s.Id).ToList();
        int startIndex = baseQueue.IndexOf(songToPlay.Id);

        if (startIndex == -1)
        {
            _logger.LogError("Could not find song '{Title}' in search results to start playback.", songToPlay.Title);
            return;
        }

        // --- Step 2: Handle Shuffle Mode Correctly on Start ---
        if (IsShuffleActive)
        {
            // When shuffle is on, we randomize the *entire queue* but ensure
            // the song the user clicked on is moved to the very beginning.
            var shuffledQueue = baseQueue.OrderBy(x => _random.Next()).ToList();
            shuffledQueue.Remove(songToPlay.Id);
            shuffledQueue.Insert(0, songToPlay.Id);
            _playbackQueue = shuffledQueue;
            // The starting index is now always 0.
            startIndex = 0;
        }
        else
        {
            // If not shuffling, the queue is just the search results in order.
            _playbackQueue = baseQueue;
        }

        CurrentPlaybackQuery = CurrentQuery;
        _logger.LogInformation("Playback queue established. Shuffle={IsShuffle}. Songs={Count}.", IsShuffleActive, _playbackQueue.Count);

        // --- Step 3: Save context and start playback ---
        SavePlaybackContext(CurrentPlaybackQuery);
        StartAudioForSongAtIndex(startIndex);
    }


    [RelayCommand]
    public void PlayPauseToggle()
    {
        if (CurrentPlayingSongView.Title == null)
        {
            PlaySong(_searchResults.FirstOrDefault());
            return;
        }
        if (IsPlaying)
            audioService.Pause();
        else
            audioService.Play();
    }

    [RelayCommand]
    public void NextTrack()
    {
        if (IsPlaying && CurrentPlayingSongView != null)
        {
            _baseAppFlow.UpdateDatabaseWithPlayEvent(realmFactory, CurrentPlayingSongView, StatesMapper.Map(DimmerPlaybackState.Skipped), CurrentTrackPositionSeconds);
        }
        var nextIndex = GetNextIndexInQueue(1);
        StartAudioForSongAtIndex(nextIndex);
    }

    [RelayCommand]
    public void PreviousTrack()
    {
        if (audioService.CurrentPosition > 3)
        {
            audioService.Seek(0);
            return;
        }
        if (IsPlaying && CurrentPlayingSongView != null)
        {
            _baseAppFlow.UpdateDatabaseWithPlayEvent(realmFactory, CurrentPlayingSongView, StatesMapper.Map(DimmerPlaybackState.Skipped), CurrentTrackPositionSeconds);
        }
        var prevIndex = GetNextIndexInQueue(-1);
        StartAudioForSongAtIndex(prevIndex);
    }

    #endregion

    #region Private Playback Helper Methods

    private void StartAudioForSongAtIndex(int index)
    {
        _playbackQueueIndex = index;

        if (_playbackQueueIndex == -1)
        {
            _logger.LogInformation("Playback queue finished. Stopping playback.");
            audioService.Stop();
            UpdateSongSpecificUi(null); // Clear the UI
            return;
        }

        var nextSongId = _playbackQueue[_playbackQueueIndex];
        var songToPlay = _searchResults.FirstOrDefault(s => s.Id == nextSongId);

        if (songToPlay == null)
        {
            _logger.LogError("Could not find song ID {SongId} in search results. Trying next.", nextSongId);
            NextTrack();
            return;
        }

        audioService.Stop();
        audioService.InitializeAsync(songToPlay);
    }

    /// <summary>
    /// Calculates the next valid index in the queue, correctly handling all Repeat and Shuffle modes.
    /// </summary>
    /// <param name="direction">1 for next, -1 for previous.</param>
    /// <returns>The next index to play, or -1 to stop.</returns>
    private int GetNextIndexInQueue(int direction)
    {
        if (_playbackQueue.Count == 0)
            return -1;

        // --- Mode 1: Repeat One ---
        // If user presses next/prev, we still give them the same song.
        if (CurrentRepeatMode == RepeatMode.One)
        {
            return _playbackQueueIndex;
        }

        // --- Mode 2: Shuffle ---
        // In shuffle mode, we just move to the next item in our pre-shuffled list.
        // The "randomness" was already decided when playback started.
        // Next/Prev simply moves linearly through this shuffled queue.
        int nextIndex = _playbackQueueIndex + direction;

        // --- Boundary and Repeat Logic ---
        if (nextIndex >= _playbackQueue.Count)
        {
            // Reached the end of the queue
            if (CurrentRepeatMode == RepeatMode.All)
            {
                if (IsShuffleActive)
                {
                    // Re-shuffle the queue for the next playthrough, but don't repeat the last song.
                    var lastSongId = _playbackQueue.Last();
                    var tempQueue = _playbackQueue.ToList();
                    tempQueue.Remove(lastSongId);
                    var newShuffledPart = tempQueue.OrderBy(x => _random.Next()).ToList();
                    newShuffledPart.Insert(0, lastSongId); // Put last song at start to avoid repeat
                    _playbackQueue = newShuffledPart.Distinct().ToList(); // Ensure no duplicates
                }
                return 0; // Loop back to the beginning
            }
            else
            {
                return -1; // Stop playback
            }
        }

        if (nextIndex < 0)
        {
            // Reached the beginning of the queue
            if (CurrentRepeatMode == RepeatMode.All)
            {
                return _playbackQueue.Count - 1; // Loop back to the end
            }
            else
            {
                // Can't go back further if not repeating
                return 0; // Or -1 if you want it to stop. Restarting at 0 is common.
            }
        }

        return nextIndex;
    }
    private static readonly ObjectId LastSessionPlaylistId = new("65f0c1a9c3b8a0b3f8e3c1a9"); // Example fixed ID

    private void SavePlaybackContext(string query)
    {
        // --- Step 1: Create the new playlist object ---
        var contextPlaylist = new PlaylistModel
        {
            Id = ObjectId.GenerateNewId(), // Always generate a new ID for a new session playlist
            PlaylistName = $"Playback Session: {DateTime.Now:g}",
            IsSmartPlaylist = !string.IsNullOrEmpty(query),
            QueryText = query,
            DateCreated = DateTimeOffset.UtcNow
        };

        // --- Step 2: Add the song IDs to the managed list ---
        // The _playbackQueue holds the correct, shuffled (or unshuffled) list of song IDs.
        foreach (var songId in _playbackQueue)
        {
            contextPlaylist.SongsIdsInPlaylist.Add(songId);
        }

        // --- Step 3: Save the new object to the database ---
        // We use 'Add' here because it's a new session playlist each time.
        // If you wanted to have a single, overwriting "Last Session" playlist,
        // you would find it by a fixed ID first and then use AddOrUpdate.
        _playlistRepo.Create(contextPlaylist);

        _logger.LogInformation("Saved playback context with query: \"{query}\"", query);
    }

    /// <summary>
    /// Plays an entire playlist from the beginning.
    /// </summary>
    [RelayCommand]
    private void PlayPlaylist(PlaylistModelView? playlist)
    {
        if (playlist == null || !playlist.SongsIdsInPlaylist.Any())
        {
            _logger.LogWarning("PlayPlaylist called with a null or empty playlist.");
            return;
        }

        // The playlist already defines the queue. No need for shuffle logic here,
        // as we want to respect the saved order of the playlist.
        _playbackQueue = playlist.SongsIdsInPlaylist.ToList();
        CurrentPlaybackQuery = $"playlist:\"{playlist.PlaylistName}\""; // Set context for UI

        _logger.LogInformation("Playback queue established from playlist '{PlaylistName}' with {Count} songs.", playlist.PlaylistName, _playbackQueue.Count);

        // No need to call SavePlaybackContext, as this isn't a temporary search session.
        // Start playback from the first song (index 0).
        StartAudioForSongAtIndex(0);
    }
    [RelayCommand]
    public void ToggleShuffleMode()
    {
        // Simply flip the boolean state.
        IsShuffleActive = !IsShuffleActive;
        _logger.LogInformation("Shuffle mode toggled to: {IsShuffleActive}", IsShuffleActive);

        // Optional but recommended:
        // If a song is currently playing, we can reshuffle the rest of the queue
        // to reflect the new shuffle state immediately for the upcoming tracks.
        if (CurrentPlayingSongView != null && _playbackQueue.Any())
        {
            var currentSongId = CurrentPlayingSongView.Id;

            // Get all songs in the queue EXCEPT the current one.
            var remainingQueue = _playbackQueue.Where(id => id != currentSongId).ToList();

            if (IsShuffleActive)
            {
                // If shuffle was just turned ON, shuffle the remaining songs.
                var shuffledRemaining = remainingQueue.OrderBy(x => _random.Next()).ToList();

                // Rebuild the queue with the current song at the front, followed by the new shuffled part.
                var newQueue = new List<ObjectId> { currentSongId };
                newQueue.AddRange(shuffledRemaining);
                _playbackQueue = newQueue;
                _playbackQueueIndex = 0; // We are at the start of this new conceptual queue.
            }
            else
            {
                // If shuffle was just turned OFF, we should revert the queue to the original,
                // sorted order from the search results, but keep our current position.
                _playbackQueue = _searchResults.Select(s => s.Id).ToList();
                // Find the new index of our current song in the un-shuffled list.
                _playbackQueueIndex = _playbackQueue.IndexOf(currentSongId);
            }
            _logger.LogInformation("Playback queue has been updated to reflect new shuffle state.");
        }
    }

    [RelayCommand]
    public void ToggleRepeatMode()
    {
        // This logic cycles through the enum values: None -> One -> All -> None ...
        CurrentRepeatMode = (RepeatMode)(((int)CurrentRepeatMode + 1) % Enum.GetNames(typeof(RepeatMode)).Length);

        // Persist the setting for the next app launch.
        _settingsService.RepeatMode = CurrentRepeatMode;

        _logger.LogInformation("Repeat mode toggled to: {RepeatMode}", CurrentRepeatMode);

        // You can also notify a global state service if other parts of the app need to know.
        // _stateService.SetRepeatMode(CurrentRepeatMode);
    }
    /// <summary>
    /// Plays a specific song within the context of its playlist.
    /// The playlist becomes the new playback queue.
    /// </summary>
    [RelayCommand]
    private void PlaySongFromPlaylist(PlaylistSongContext context)
    {
        if (context?.Playlist == null || context.SongToPlay == null || !context.Playlist.SongsIdsInPlaylist.Any())
        {
            _logger.LogWarning("PlaySongFromPlaylist called with invalid context.");
            return;
        }

        var playlist = context.Playlist;
        var songToPlay = context.SongToPlay;

        // The playlist's song list becomes our new frozen queue.
        _playbackQueue = playlist.SongsIdsInPlaylist.ToList();

        // Find the starting index of the song the user clicked.
        var startIndex = _playbackQueue.IndexOf(songToPlay.Id);
        if (startIndex == -1)
        {
            _logger.LogError("Could not find song '{SongTitle}' in playlist '{PlaylistName}'. Starting from the beginning.", songToPlay.Title, playlist.PlaylistName);
            startIndex = 0;
        }

        CurrentPlaybackQuery = $"playlist:\"{playlist.PlaylistName}\""; // Set context for UI

        _logger.LogInformation("Playback queue established from playlist '{PlaylistName}', starting with '{SongTitle}'.", playlist.PlaylistName, songToPlay.Title);

        StartAudioForSongAtIndex(startIndex);
    }

    // We need a small helper class to pass both the song and playlist to the command.
    // You can define this at the bottom of your BaseViewModel.cs file or in a separate file.
    public record PlaylistSongContext(PlaylistModelView Playlist, SongModelView SongToPlay);
    #endregion
    #endregion
    private void SubscribeToLyricsFlow()
    {
        _subsManager.Add(_lyricsMgtFlow.CurrentLyric.Subscribe(line => CurrentLine = line));
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
    }
    private void SubscribeToAudioServiceEvents()
    {
        // --- Centralized Playback State Handling ---
        // We subscribe to the ONE event that tells us about all state changes.
        _subsManager.Add(Observable.FromEventPattern<PlaybackEventArgs>(
                h => audioService.PlaybackStateChanged += h,
                h => audioService.PlaybackStateChanged -= h)
            .Select(evt => evt.EventArgs)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(HandlePlaybackStateChange, ex => _logger.LogError(ex, "Error in PlaybackStateChanged subscription")));

        // --- Simple Property Updates ---
        // IsPlayingChanged is a simple boolean event, so we handle it directly.
        // The compiler error was about the generic type. We just need to get the bool from the args.
        _subsManager.Add(Observable.FromEventPattern<PlaybackEventArgs>(
                h => audioService.IsPlayingChanged += h,
                h => audioService.IsPlayingChanged -= h)
            .Select(evt => evt.EventArgs.IsPlaying) // Get the boolean value from the event args
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(isPlaying => IsPlaying = isPlaying, ex => _logger.LogError(ex, "Error in IsPlayingChanged subscription")));

        // --- Position and Seeking (These were already correct based on your interface) ---
        _subsManager.Add(Observable.FromEventPattern<double>(
                h => audioService.PositionChanged += h,
                h => audioService.PositionChanged -= h)
            .Select(evt => evt.EventArgs)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(OnPositionChanged, ex => _logger.LogError(ex, "Error in PositionChanged subscription")));

        _subsManager.Add(Observable.FromEventPattern<double>(
                h => audioService.SeekCompleted += h,
                h => audioService.SeekCompleted -= h)
            .Select(evt => evt.EventArgs)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(OnSeekCompleted, ex => _logger.LogError(ex, "Error in SeekCompleted subscription")));

        // --- Playback Completion ---
        // Your interface has 'PlayEnded' not 'PlaybackEnded'.
        _subsManager.Add(Observable.FromEventPattern<PlaybackEventArgs>(
                h => audioService.PlayEnded += h,
                h => audioService.PlayEnded -= h)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ => OnPlaybackEnded(), ex => _logger.LogError(ex, "Error in PlayEnded subscription")));

        // --- Media Key Integration (These were already correct) ---
        _subsManager.Add(Observable.FromEventPattern<PlaybackEventArgs>(
                h => audioService.MediaKeyNextPressed += h,
                h => audioService.MediaKeyNextPressed -= h)
            .Subscribe(_ => NextTrack(), ex => _logger.LogError(ex, "Error in MediaKeyNextPressed subscription")));

        _subsManager.Add(Observable.FromEventPattern<PlaybackEventArgs>(
                h => audioService.MediaKeyPreviousPressed += h,
                h => audioService.MediaKeyPreviousPressed -= h)
            .Subscribe(_ => PreviousTrack(), ex => _logger.LogError(ex, "Error in MediaKeyPreviousPressed subscription")));
    }

    /// <summary>
    /// A new helper method to route playback state changes to the correct handler.
    /// This is the target of our main PlaybackStateChanged subscription.
    /// </summary>
    private void HandlePlaybackStateChange(PlaybackEventArgs args)
    {
        // Assuming PlaybackEventArgs has a property like 'State' of type 'DimmerPlaybackState'
        // If not, we'll need to see the definition of PlaybackEventArgs.
        // Let's assume it exists for this example.

        // You might need to adjust 'args.State' to whatever property holds the enum.
        // e.g., if PlaybackEventArgs holds a DimmerPlayEvent, it might be args.PlayEvent.PlayType
        PlayType? state = StatesMapper.Map(args.EventType); // Assuming you have a way to get the enum state

        switch (state)
        {
            case PlayType.Play:
                // This case might be handled by PlayEnded -> NextTrack -> StartAudioForSongAtIndex
                // which then calls InitializeAsync and Play. The audio service might raise
                // a 'Playing' state change at that point. We can simply log it.
                OnPlaybackStarted(args);
                break;

            case PlayType.Resume:
                OnPlaybackResumed(args);
                break;

            case PlayType.Pause:
                OnPlaybackPaused(args);
                break;

                // No need to handle Completed/Skipped here, as they are determined by
                // our ViewModel logic (OnPlaybackEnded and StartAudioForSongAtIndex).

                // You can add other cases if your audio service reports them
                // case DimmerPlaybackState.Buffering:
                // case DimmerPlaybackState.Error:
                //    HandleErrors(args);
                //    break;
        }
    }
    /// <summary>
    /// Subscribes to general state changes from the IStateService.
    /// </summary>
    private void SubscribeToStateServiceEvents()
    {
        _subsManager.Add(_stateService.CurrentSong
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(OnCurrentSongChanged, ex => _logger.LogError(ex, "Error in CurrentSong subscription")));

        _subsManager.Add(_stateService.IsShuffleActive
            .Subscribe(isShuffle => IsShuffleActive = isShuffle, ex => _logger.LogError(ex, "Error in IsShuffleActive subscription")));

        _subsManager.Add(_stateService.DeviceVolume
            .Subscribe(volume => DeviceVolumeLevel = volume, ex => _logger.LogError(ex, "Error in DeviceVolume subscription")));

        // This handles special cases like when a scan completes.
        var playbackStateObservable = _stateService.CurrentPlayBackState.Publish().RefCount();

        _subsManager.Add(playbackStateObservable
            .Where(s => s.State == DimmerPlaybackState.FolderScanCompleted)
            .Subscribe(OnFolderScanCompleted, ex => _logger.LogError(ex, "Error on FolderScanCompleted.")));

        _subsManager.Add(_stateService.LatestDeviceLog
            .Where(s => s.Log is not null)
            .Subscribe(OnSongAdded, ex => _logger.LogError(ex, "Error on FolderScanCompleted.")));

        // PlaylistExhausted is now handled by the PlaybackEnded event from the audio service, making this obsolete.
    }

    private void OnSongAdded(AppLogModel model)
    {
        LatestAppLog=model;
        LatestScanningLog = model.Log;
    }

    [ObservableProperty] public partial string CurrentPlaybackQuery { get; set; }
    private int _playbackQueueIndex = -1;
    private IReadOnlyList<ObjectId> _playbackQueue = new List<ObjectId>();



    [ObservableProperty]
    public partial bool IsAppScanning { get; set; }

    [ObservableProperty]
    public partial bool IsSafeKeyboardAreaViewOpened { get; set; }



    [RelayCommand]
    public void SetPreferredAudioDevice(AudioOutputDevice dev)
    {
        audioService.SetPreferredOutputDevice(dev);
    }


    private readonly Random _random = new();


    [RelayCommand]
    public void SeekTrackPosition(double positionSeconds)
    {


        _logger.LogDebug("SeekTrackPosition called by UI to: {PositionSeconds}s", positionSeconds);
        audioService.Seek(positionSeconds);
        //_baseAppFlow.UpdateDatabaseWithPlayEvent( realmFactory,CurrentPlayingSongView, StatesMapper.Map(DimmerPlaybackState.Seeked), positionSeconds);

        // If you want to log this as a play event, you can do so here.

        //_songsMgtFlow.RequestSeek(positionSeconds);


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
        song ??=CurrentPlayingSongView;
        if (song is null)
        {
            return false;
        }

        var allArts = song.OtherArtistsName.Split(", ");

        _logger.LogTrace("SelectedArtistAndNavtoPage called with song: {SongTitle}", song.Title);
        var result = await Shell.Current.DisplayActionSheet("Select Action", "Cancel", null, allArts);
        if (result == "Cancel" || string.IsNullOrEmpty(result))
            return false;


        var realm = realmFactory.GetRealmInstance();
        var artDb = realm.All<ArtistModel>().FirstOrDefault(x => x.Name == result);

        DeviceStaticUtils.SelectedArtistOne = artDb.ToModelView(_mapper);


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
        _folderMgtService.AddFolderToWatchListAndScan(folderPath);
        _stateService.SetCurrentState(new PlaybackStateInfo(DimmerPlaybackState.FolderAdded, folderPath, null, null));
    }
    public void AddMusicFoldersByPassingToService(List<string> folderPath)
    {
        _logger.LogInformation("User requested to add music folder.");
        _stateService.SetCurrentState(new PlaybackStateInfo(DimmerPlaybackState.PlaySongFrommOutsideApp, folderPath, null, null));
    }

    public void ViewAlbumDetails(AlbumModelView albumView)
    {

        SelectedAlbum =albumView;
        SearchSongSB_TextChanged(StaticMethods.SetQuotedSearch("album", albumView.Name));


    }



    [RelayCommand]
    public async Task RetroactivelyLinkArtists()
    {

        await Shell.Current.DisplayAlert("Process Started", "Starting to link artists for all songs. This may take a moment. The app might be a bit slow.", "OK");

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        await Task.Run(() =>
        {

            if (realmFactory is null)
            {
                _logger.LogError("RealmFactory is not available.");
                return;
            }
            var realm = realmFactory.GetRealmInstance();
            if (realm is null)
            {
                _logger.LogError("Failed to get Realm instance.");
                return;
            }



            _logger.LogInformation("Searching for songs with unlinked artists...");
            var songsToFix = realm.All<SongModel>().Filter("ArtistToSong.@count == 0").ToList();

            if (songsToFix.Count==0)
            {
                _logger.LogInformation("No songs found that require artist linking. Database is already up-to-date!");

                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Shell.Current.DisplayAlert("All Done!", "No songs needed fixing. Everything is already linked correctly.", "OK");
                });
                return;
            }

            _logger.LogInformation("Found {SongCount} songs to process.", songsToFix.Count);



            var allArtistNames = songsToFix
                .Select(s => s.ArtistName)
                .Concat(songsToFix.SelectMany(s => (s.OtherArtistsName ?? "").Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries)))
                .Where(name => !string.IsNullOrEmpty(name))
                .Distinct()
                .ToList();

            _logger.LogInformation("Found {ArtistCount} unique artist names to look up in the database.", allArtistNames.Count);



            var artistClauses = Enumerable.Range(0, allArtistNames.Count).Select(i => $"Name == ${i}");
            var artistQueryString = string.Join(" OR ", artistClauses);
            var artistQueryArgs = allArtistNames.Select(name => (QueryArgument)name).ToArray();

            var artistsFromDb = realm.All<ArtistModel>()
                                       .Filter(artistQueryString, artistQueryArgs)
                                       .ToDictionary(a => a.Name);

            _logger.LogInformation("Successfully fetched {ArtistCount} matching Artist objects from the database.", artistsFromDb.Count);



            _logger.LogInformation("Beginning database write transaction to link artists...");
            realm.Write(() =>
            {
                foreach (var song in songsToFix)
                {

                    var namesForThisSong = new List<string> { song.ArtistName }
                        .Concat((song.OtherArtistsName ?? "").Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries))
                        .Where(name => !string.IsNullOrEmpty(name))
                        .Distinct();


                    song.ArtistToSong.Clear();

                    foreach (var artistName in namesForThisSong)
                    {

                        if (artistsFromDb.TryGetValue(artistName, out var artistModel))
                        {

                            song.ArtistToSong.Add(artistModel);
                        }
                        else
                        {
                            _logger.LogWarning("Could not find artist '{ArtistName}' in the database to link to song '{SongTitle}'.", artistName, song.Title);
                        }
                    }
                }
            });

            stopwatch.Stop();
            _logger.LogInformation("Successfully linked artists for {SongCount} songs in {ElapsedMilliseconds} ms.", songsToFix.Count, stopwatch.ElapsedMilliseconds);


            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Shell.Current.DisplayAlert("Success!", $"Successfully updated {songsToFix.Count} songs in {stopwatch.Elapsed.TotalSeconds:F2} seconds.", "Awesome!");
            });
        });
    }
    public void ViewArtistDetails(ArtistModelView? artView)
    {

        if (artView?.Id == null)
        {
            _logger.LogWarning("ViewArtistDetails called with a null artist view or ID.");

        }

        SelectedArtistAlbums ??=new();

        SelectedArtistAlbums.Clear();




        var db = realmFactory.GetRealmInstance();


        var artistIdToFind = artView.Id;


        var songsByArtist = db.All<SongModel>()
                              .Filter("Artist.Id == $0 OR ANY ArtistToSong.Id == $0", artistIdToFind)
                              .ToList();

        if (songsByArtist.Count==0)
        {
            _logger.LogWarning("No songs found for artist with ID {ArtistId}", artistIdToFind);

            SelectedArtist = artView;
            return;
        }



        var albumsByArtist = songsByArtist
            .Where(s => s.Album != null)
            .Select(s => s.Album)
            .Distinct()
            .ToList();


        SelectedArtist = artView;


        var firstSong = songsByArtist[0];
        Track tt = new(firstSong.FilePath);
        SelectedArtist.ImageBytes = tt.EmbeddedPictures.FirstOrDefault()?.PictureData;


        foreach (var song in songsByArtist)
        {

        }
        foreach (var album in albumsByArtist)
        {
            SelectedArtistAlbums.Add(album.ToModelView(_mapper));
        }








        _logger.LogInformation("Successfully prepared details for artist: {ArtistName}", SelectedArtist.Name);


    }

    public void GetStatsGeneral()
    {



        var endDate = DateTimeOffset.UtcNow;
        var startDate = endDate.AddMonths(-1);

        TopSkippedArtists = TopStats.GetTopSkippedArtists(songRepo.GetAll(), dimmerPlayEventRepo.GetAll(), 50).ToObservableCollection();
        TopSongsByEventType = TopStats.GetTopSongsByEventType(songRepo.GetAll(), dimmerPlayEventRepo.GetAll(), 50, 3).ToObservableCollection();
        TopSongsLastMonth = TopStats.GetTopCompletedSongs(songRepo.GetAll(), dimmerPlayEventRepo.GetAll(), 50, startDate, endDate).ToObservableCollection();




        MostSkipped = TopStats.GetTopSkippedSongs(songRepo.GetAll(), dimmerPlayEventRepo.GetAll(), 50).ToObservableCollection();


        MostListened = TopStats.GetTopSongsByListeningTime(songRepo.GetAll(), dimmerPlayEventRepo.GetAll(), 50, startDate, endDate).ToObservableCollection();
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



    [ObservableProperty]
    public partial ObservableCollection<DimmerStats>? TopSongsLastMonth { get; set; }
    [ObservableProperty]
    public partial ObservableCollection<DimmerStats>? TopSkippedArtists { get; set; }
    [ObservableProperty]
    public partial ObservableCollection<DimmerStats>? TopSongsByEventType { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<DimmerStats>? MostSkipped { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<DimmerStats>? MostListened { get; set; }





    [RelayCommand]
    public void DeleteFolderPath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return;
        _logger.LogInformation("Requesting to delete folder path: {Path}", path);
        FolderPaths.Remove(path);
        _folderMgtService.RemoveFolderFromWatchListAsync(path);

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
        var song = songRepo.Upsert(songModel);
        _logger.LogInformation("Song '{SongTitle}' updated with new rating: {NewRating}", songModel.Title, newRating);

        _stateService.SetCurrentSong(song);
    }
    [RelayCommand]
    public void ToggleFavSong(SongModelView songModel)
    {


        if (CurrentPlayingSongView == null)
        {
            _logger.LogWarning("RateSong called but CurrentPlayingSongView is null.");
            return;
        }

        if (songModel == null)
        {
            _logger.LogWarning("ToggleFavSong: Could not map CurrentPlayingSongView to SongModel.");
            return;
        }
        songModel.IsFavorite = !songModel.IsFavorite;
        var song = songRepo.Upsert(songModel.ToModel(_mapper));


    }


    public void AddToPlaylist(string playlistName, List<SongModelView> songsToAdd)
    {
        if (string.IsNullOrEmpty(playlistName) || songsToAdd == null || songsToAdd.Count==0)
        {
            _logger.LogWarning("AddToPlaylist called with invalid parameters.");
            return;
        }

        _logger.LogInformation("Attempting to add {Count} songs to playlist '{PlaylistName}'.", songsToAdd.Count, playlistName);

        // Step 1: Find the playlist by name. We need its ID.
        // The Query method returns a frozen list.
        var matchingPlaylists = _playlistRepo.Query(p => p.PlaylistName == playlistName);
        var targetPlaylist = matchingPlaylists.FirstOrDefault();

        // If the playlist doesn't exist, create it as a NEW MANUAL playlist.
        if (targetPlaylist == null)
        {
            _logger.LogInformation("Playlist '{PlaylistName}' not found. Creating it as a new manual playlist.", playlistName);
            var newPlaylistModel = new PlaylistModel
            {
                PlaylistName = playlistName,
                IsSmartPlaylist = false // Crucial! It's a manual playlist.
            };
            targetPlaylist = _playlistRepo.Create(newPlaylistModel);
        }

        // If we found an existing playlist but it's a SMART playlist, we can't add songs manually.
        if (targetPlaylist.IsSmartPlaylist)
        {
            _logger.LogWarning("Cannot manually add songs to the smart playlist '{PlaylistName}'. Change its query instead.", playlistName);
            // Optionally, show a message to the user here.
            return;
        }

        // Step 2: Use the safe Update method to modify the playlist.
        var songIdsToAdd = songsToAdd.Select(s => s.Id).ToHashSet();

        _playlistRepo.Update(targetPlaylist.Id, livePlaylist =>
        {
            // 'livePlaylist' is the managed object inside the transaction.
            int songsAddedCount = 0;
            foreach (var songId in songIdsToAdd)
            {
                // Check if the song is already in the list to avoid duplicates.
                if (!livePlaylist.SongsIdsInPlaylist.Contains(songId))
                {
                    livePlaylist.SongsIdsInPlaylist.Add(songId);
                    songsAddedCount++;
                }
            }
            _logger.LogInformation("Successfully added {Count} new songs to manual playlist '{PlaylistName}'.", songsAddedCount, playlistName);
        });
    }
    private bool _disposed = false;


    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _realmSubscription?.Dispose();
            _playEventSource.Dispose();


            Disposables.Dispose();
        }

        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    private void LoadMusicArtistServiceMethods(ArtistModelView? artist)
    {
#if RELEASE
        return;
#endif
        if (artist is null)
        {
            return;
        }
        var artId = artist.Id;
        var first = musicArtistryService.GetPrimeNumberSongs();
        var snd = musicArtistryService.GetParetoPrincipleCheck();
        var thrd = musicArtistryService.GetNextSongPredictability();
        var fth = musicArtistryService.FindHiddenConceptAlbum();
        var fth2 = musicArtistryService.GetGoldenRatioTrackOnFavoriteAlbum();

        var ss = musicStatsService.GetAllTimeMostPlayedSong();
        var sss = musicStatsService.GetAllTimeTopAlbum();
        var ssss = musicStatsService.GetAllTimeTopArtist();
        var topnar = musicStatsService.GetAllTimeTopNArtists(8);
        var topalb = musicStatsService.GetArtistRankByPlayCount(ssss.Artist.Id);
        var ssa = musicStatsService.GetTotalListeningHours();
        var sss2 = musicStatsService.GetTotalUniqueArtistsPlayed();
        var eqe = musicStatsService.GetTotalUniqueSongsPlayed();
        ArtistLoyaltyIndex = musicRelationshipService.GetArtistLoyaltyIndex(artId);
        MyCoreArtists = _mapper.Map<ObservableCollection<ArtistModelView>>(musicRelationshipService.GetMyCoreArtists(10));
        ArtistBingeScore = musicRelationshipService.GetArtistBingeScore(artId);
        SongModelView? step4 = musicRelationshipService.GetSongThatHookedMeOnAnArtist(artId).ToModelView(_mapper);

    }

    public void RaisePropertyChanging(System.ComponentModel.PropertyChangingEventArgs args)
    {
        OnPropertyChanging(args);
    }

    public void RaisePropertyChanged(PropertyChangedEventArgs args)
    {
        OnPropertyChanged(args);
    }

    [ObservableProperty]
    public partial double ArtistLoyaltyIndex { get; set; }
    [ObservableProperty]
    public partial ObservableCollection<ArtistModelView> MyCoreArtists { get; set; }
    [ObservableProperty]
    public partial (DateTimeOffset Date, int PlayCount) ArtistBingeScore { get; set; }
    [ObservableProperty]
    public partial SongModelView SongThatHookedMeOnAnArtist { get; set; }



    public ReadOnlyObservableCollection<DimmerPlayEventView> PlayEventsByTimeChartData { get; private set; }
    public ReadOnlyObservableCollection<InteractiveChartPoint> PlayEventsByHourChartData { get; private set; }
    public ReadOnlyObservableCollection<DimmerPlayEventView> PlaysByDurationChartData { get; private set; }

    /*
    private void OnRealmPlayEventsChanged(IRealmCollection<DimmerPlayEvent> sender, ChangeSet? changes)
    {
        if (changes is null)
        {
            var initialItems = _mapper.Map<IEnumerable<DimmerPlayEventView>>(sender);
            _playEventSource.Edit(innerList => { innerList.Clear(); innerList.AddRange(initialItems); });
            return;
        }
        _playEventSource.Edit(innerList =>
        {
            for (int i = changes.DeletedIndices.Length - 1; i >= 0; i--)
            { var indexToDelete = changes.DeletedIndices[i]; innerList.RemoveAt(indexToDelete); }
            foreach (var i in changes.InsertedIndices)
            { innerList.Insert(i, _mapper.Map<DimmerPlayEventView>(sender[i])); }
            foreach (var i in changes.NewModifiedIndices)
            { innerList[i] = _mapper.Map<DimmerPlayEventView>(sender[i]); }
        });
    }
    */
    private Dictionary<ObjectId, SongModelView> _allSongsLookup = new();
    private void BuildRawDataChartPipelines()
    {
        _playEventSource.Connect()
      .ToCollection()
      .Select(events => events




          .Where(evt => evt.PlayTypeStr == "Skipped")
          .GroupBy(evt => evt.SongId)
          .Select(group => new
          {
              SongId = group.Key,
              SkipCount = group.Count()
          })
          .OrderByDescending(x => x.SkipCount)
          .Take(10)
          .Select(x =>
          {

              if (_allSongsLookup.TryGetValue(x.SongId.Value, out var fullSong))
              {

                  return new InteractiveChartPoint(fullSong.Title, x.SkipCount, fullSong);
              }
              return null;
          })
          .Where(x => x != null)
          .ToList())
      .ObserveOn(RxApp.MainThreadScheduler)
      .Subscribe(newData =>
      {
          _topSkipsList.Clear();
          _topSkipsList.AddRange(newData!);
      })
      .DisposeWith(_disposables);





        _playEventSource.Connect()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(_scatterChartList)
            .Subscribe()
            .DisposeWith(_disposables);


        _playEventSource.Connect()
                  .ToCollection()
                  .Select(events => events
                      .GroupBy(evt => evt.DatePlayed.Hour)
                      .Select(group => new InteractiveChartPoint(group.Key.ToString("00") + ":00", group.Count()))
                      .OrderBy(dp => dp.XValue.ToString())
                      .ToList())
                  .ObserveOn(RxApp.MainThreadScheduler)


                  .Subscribe(newChartData =>
                  {
                      _barChartList.Clear();
                      _barChartList.AddRange(newChartData);
                  })
                  .DisposeWith(_disposables);


        _playEventSource.Connect()
    .ToCollection()
    .Select(events => events
        .Where(evt => evt.PlayTypeStr == "Skipped")
        .GroupBy(evt => evt.SongName)
        .Select(group => new InteractiveChartPoint(group.Key, group.Count()))
        .OrderByDescending(dp => dp.YValue)
        .Take(10)
        .ToList())
    .ObserveOn(RxApp.MainThreadScheduler)
    .Subscribe(newData =>
    {
        _skippedSongsList.Clear();
        _skippedSongsList.AddRange(newData);
    })
    .DisposeWith(_disposables);




        _playEventSource.Connect()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(_bubbleChartList)
            .Subscribe()
            .DisposeWith(_disposables);
    }


    private readonly ObservableCollectionExtended<DimmerPlayEventView> _scatterChartList = new();
    private readonly ObservableCollectionExtended<InteractiveChartPoint> _barChartList = new();
    private readonly ObservableCollectionExtended<DimmerPlayEventView> _bubbleChartList = new();
    public ReadOnlyObservableCollection<InteractiveChartPoint> SkippedSongsChart { get; }
    private readonly ObservableCollectionExtended<InteractiveChartPoint> _skippedSongsList = new();

    public ReadOnlyObservableCollection<DimmerPlayEventView> PlayEventsForScatterChart { get; }
    public ReadOnlyObservableCollection<InteractiveChartPoint> PlayEventsByHourForBarChart { get; }
    public ReadOnlyObservableCollection<DimmerPlayEventView> PlayEventsForBubbleChart { get; }

    private readonly ObservableCollectionExtended<InteractiveChartPoint> _topSkipsList = new();
    public ReadOnlyObservableCollection<InteractiveChartPoint> TopSkipsChartData { get; }


    public class LiveStatsResult
    {
        public SongStat? TopSong { get; init; }
        // You can add more results here later, e.g.:
        // public SongStat? TopSongInFilter { get; init; }
        // public int TotalPlaysInFilter { get; init; }
    }

    /// <summary>
    /// Calculates statistics based on a collection of songs and play events.
    /// This method is designed to be called from a background thread.
    /// </summary>
    /// <param name="filteredSongs">The list of songs currently visible in the UI.</param>
    /// <param name="allPlayEvents">The complete list of all historical play events.</param>
    /// <returns>A LiveStatsResult object containing the calculated stats.</returns>
    private LiveStatsResult? CalculateStats(IReadOnlyCollection<SongModelView> filteredSongs, IReadOnlyCollection<DimmerPlayEventView> allPlayEvents)
    {
        // --- STUBBED IMPLEMENTATION ---
        // This is where your complex logic will go. For now, we'll implement
        // the "AllTimeTopArtist" calculation you already had.

        if (allPlayEvents.Count==0)
        {
            return null; // No events, no stats.
        }

        // This calculation does not depend on the filteredSongs, so it's an "All Time" stat.
        var topSong = allPlayEvents
            .Where(e => !string.IsNullOrEmpty(e.SongName))
            .GroupBy(e => e.SongName) // Group by SongName for better accuracy
            .Select(g => new SongStat(new SongModel { Title = g.Key }, g.Count()))
            .OrderByDescending(a => a.PlayCount)
            .FirstOrDefault();

        // --- EXAMPLE: Stat that USES the filter ---
        // Let's say you wanted to find the most played song *within the current search results*.
        /*
        var filteredSongIds = new HashSet<ObjectId>(filteredSongs.Select(s => s.Id));

        var topSongInFilter = allPlayEvents
            .Where(e => e.SongId.HasValue && filteredSongIds.Contains(e.SongId.Value))
            .GroupBy(e => e.SongId)
            .Select(g => new { SongId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .FirstOrDefault();
        */

        return new LiveStatsResult
        {
            TopSong = topSong,
            // TopSongInFilter = ... // you would assign the result here
        };
    }


}