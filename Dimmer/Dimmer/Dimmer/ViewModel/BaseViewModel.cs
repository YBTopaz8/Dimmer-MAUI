using ATL;

using CommunityToolkit.Mvvm.Input;

using Dimmer.Charts;
using Dimmer.Data.ModelView.NewFolder;
using Dimmer.Data.RealmStaticFilters;
using Dimmer.DimmerSearch;
using Dimmer.DimmerSearch.AbstractQueryTree;
using Dimmer.DimmerSearch.AbstractQueryTree.NL;
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
    public readonly IMapper _mapper;
    private readonly IAppInitializerService appInitializerService;
    private readonly IDimmerLiveStateService _dimmerLiveStateService;
    private readonly AlbumsMgtFlow _albumsMgtFlow;
    protected readonly IDimmerStateService _stateService;
    protected readonly ISettingsService _settingsService;
    protected readonly SubscriptionManager _subsManager;
    protected readonly IFolderMgtService _folderMgtService;
    private readonly IRepository<SongModel> songRepo;
    private readonly IRepository<ArtistModel> artistRepo;
    private readonly IRepository<PlaylistModel> _playlistRepo;
    private readonly IRepository<AlbumModel> albumRepo;
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
        _songsMgtFlow.RequestSetVolume(newValue);
        _settingsService.LastVolume = newValue;
    }
    private SongsMgtFlow _songsMgtFlow;
    [ObservableProperty]
    public partial string AppTitle { get; set; }

    [ObservableProperty]
    public partial SongModelView? CurrentPlayingSongView { get; set; }

    [ObservableProperty]
    public partial string? CurrentNoteToSave { get; set; }
    partial void OnCurrentPlayingSongViewChanged(SongModelView? value)
    {

        audioService.Volume=DeviceVolumeLevel;
        if (value is not null)
        {
            Track track = new(value.FilePath);

            var imgg = track.EmbeddedPictures?.FirstOrDefault()?.PictureData;
            if (imgg is null)
            {
                value.CoverImageBytes=null;
                return;
            }
            value.CoverImageBytes = ImageResizer.ResizeImage(imgg);

        }

    }
    partial void OnSelectedSongChanged(SongModelView? value)
    {

        if (value is not null)
        {
            Track track = new(value.FilePath);

            var imgg = track.EmbeddedPictures?.FirstOrDefault()?.PictureData;
            if (imgg is null)
            {
                value.CoverImageBytes=null;
                return;
            }

            value.CoverImageBytes = ImageResizer.ResizeImage(imgg);

        }

    }

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

                        var songHasArtist = songDb.ArtistIds.FirstOrDefault(a => a.Name == artistName);
                        if (songHasArtist is not null)
                        {
                            continue;
                        }


                        if (artistsFromDb.TryGetValue(artistName, out var artistModel))
                        {

                            songDb.ArtistIds.Add(artistModel);


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

                    bool songHasArtist = freshSongDb.ArtistIds.Any(a => a.Name == artistName);
                    if (songHasArtist)
                    {
                        continue;
                    }


                    if (artistsFromDb.TryGetValue(artistName, out var artistModel))
                    {

                        freshSongDb.ArtistIds.Add(artistModel);
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
    [ObservableProperty] public partial ObservableCollection<AlbumModelView?>? SelectedAlbumsCol { get; set; }
    [ObservableProperty] public partial ObservableCollection<SongModelView?>? SelectedAlbumSongs { get; set; }
    [ObservableProperty] public partial ObservableCollection<SongModelView?>? SelectedArtistSongs { get; set; }
    [ObservableProperty] public partial ObservableCollection<SongModelView?>? SelectedPlaylistSongs { get; set; }
    [ObservableProperty] public partial ObservableCollection<PlaylistModelView>? AllPlaylistsFromDBView { get; set; }
    [ObservableProperty] public partial ObservableCollection<ArtistModelView>? SelectedSongArtists { get; set; }
    [ObservableProperty] public partial ObservableCollection<AlbumModelView>? SelectedArtistAlbums { get; set; }
    [ObservableProperty] public partial CollectionStatsSummary? ArtistCurrentColStats { get; private set; }
    [ObservableProperty] public partial CollectionStatsSummary? AlbumCurrentColStats { get; private set; }





    readonly IFolderMonitorService folderMonitorService;
    public BaseViewModel(
       IMapper mapper,
       IAppInitializerService appInitializerService,
       IDimmerLiveStateService dimmerLiveStateService,
       IDimmerAudioService _audioService,
       AlbumsMgtFlow albumsMgtFlow,
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
        _playlistRepo ??= IPlatformApplication.Current!.Services.GetService<IRepository<PlaylistModel>>()!;
        libService ??= IPlatformApplication.Current!.Services.GetService<ILibraryScannerService>()!;


        AudioEnginePositionObservable = Observable.FromEventPattern<double>(
                                             h => audioService.PositionChanged += h,
                                             h => audioService.PositionChanged -= h)
                                         .Select(evt => evt.EventArgs)
                                         .StartWith(audioService.CurrentPosition)
                                         .Replay(1).RefCount();


        _baseAppFlow = IPlatformApplication.Current!.Services.GetService<BaseAppFlow>();




        folderMonitorService = IPlatformApplication.Current!.Services.GetService<IFolderMonitorService>()!;
        realmFactory = IPlatformApplication.Current!.Services.GetService<IRealmFactory>()!;

        var realm = realmFactory.GetRealmInstance();



        this.musicRelationshipService=new(realmFactory);
        this.musicArtistryService=new(realmFactory);

        this.musicStatsService=new(realmFactory);

        _filterPredicate = new BehaviorSubject<Func<SongModelView, bool>>(song => true);
        _sortComparer = new BehaviorSubject<IComparer<SongModelView>>(new SongModelViewComparer(null));
        _limiterClause = new BehaviorSubject<LimiterClause?>(null);





        var songsStream = realm.All<SongModel>().Shuffle().AsObservableChangeSet<SongModel>();

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
    }
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



    private readonly ObservableCollectionExtended<DimmerPlayEventView> _allLivePlayEventsBackingList = new();


    public ReadOnlyObservableCollection<DimmerPlayEventView> AllLivePlayEvents { get; }


    private readonly ObservableAsPropertyHelper<IReadOnlyCollection<SongModelView>> _searchResultsHelper;

    private readonly SourceList<SongModelView> _finalLimitedDataSource = new SourceList<SongModelView>();

    public ReadOnlyObservableCollection<SongModelView> SearchResults => _searchResults;

    private readonly ReadOnlyObservableCollection<SongModelView> _fullFilteredResults;
    protected CompositeDisposable Disposables { get; } = new CompositeDisposable();

    private IReadOnlyCollection<SongModelView> ApplyLimiter(IReadOnlyCollection<SongModelView> items, LimiterClause? limiter)
    {
        if (limiter == null)
        {
            return items;
        }

        switch (limiter.Type)
        {
            case LimiterType.First:
                return [.. items.Take(limiter.Count)];

            case LimiterType.Last:

                return [.. items.TakeLast(limiter.Count)];

            case LimiterType.Random:
                var random = new Random();

                return [.. items.OrderBy(x => random.Next()).Take(limiter.Count)];

            default:
                return items;
        }
    }

    [ObservableProperty]
    public partial Label SongsCountLabel { get; set; }
    [ObservableProperty]
    public partial Label TranslatedSearch { get; set; }

    private ReadOnlyObservableCollection<SongModelView> _searchResults;
    private readonly BehaviorSubject<LimiterClause?> _limiterClause;


    private readonly BehaviorSubject<Func<SongModelView, bool>> _filterPredicate;
    private readonly BehaviorSubject<IComparer<SongModelView>> _sortComparer;


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


        }
        catch (Exception ex)
        {




            _filterPredicate.OnNext(song => false);


            _sortComparer.OnNext(new SongModelViewComparer(null));


            _limiterClause.OnNext(null);



        }


        WireUpLiveStats();

    }

    private void OnRealmPlayEventsChanged(IRealmCollection<DimmerPlayEvent> sender, ChangeSet? changes)
    {

        if (changes is null)
        {
            var initialItems = _mapper.Map<IEnumerable<DimmerPlayEventView>>(sender);
            _playEventSource.Edit(innerList =>
            {
                innerList.Clear();
                innerList.AddRange(initialItems);
            });
            return;
        }

        _playEventSource.Edit(innerList =>
        {

            for (int i = changes.DeletedIndices.Length - 1; i >= 0; i--)
            {
                var indexToDelete = changes.DeletedIndices[i];
                innerList.RemoveAt(indexToDelete);
            }


            foreach (var i in changes.InsertedIndices)
            {
                var newEventView = _mapper.Map<DimmerPlayEventView>(sender[i]);
                innerList.Insert(i, newEventView);
            }

            foreach (var i in changes.NewModifiedIndices)
            {
                var updatedEventView = _mapper.Map<DimmerPlayEventView>(sender[i]);
                innerList[i] = updatedEventView;
            }
        });

    }
    private void WireUpLiveStats()
    {

        _playEventSource.Connect()


            .Throttle(TimeSpan.FromSeconds(1), RxApp.MainThreadScheduler)

            .ToCollection()

            .ObserveOn(TaskPoolScheduler.Default)
             .Select(allEvents =>
             {

                 if (!allEvents.Any())
                     return null;


                 return allEvents
                     .Where(e => !string.IsNullOrEmpty(e.SongName))

                     .GroupBy(e => e.SongName)
                     .Select(g => new ArtistStat(new ArtistModel { Name = g.Key }, g.Count()))
                     .OrderByDescending(a => a.PlayCount)
                     .FirstOrDefault();
             })
        .ObserveOn(RxApp.MainThreadScheduler)
        .Subscribe(artistStat => AllTimeTopArtist = artistStat)

            .DisposeWith(Disposables);
    }

    readonly IRealmFactory realmFactory;
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
        InitializeViewModelSubscriptions();

    }

    public void InitializeApp()
    {
        if (_songSource.Count <1 && _settingsService.UserMusicFoldersPreference.Count >0)
        {
            var listofFOlders = _settingsService.UserMusicFoldersPreference.ToList();

        }
    }
    protected virtual void InitializeViewModelSubscriptions()
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
            .SubscribeOn(RxApp.MainThreadScheduler)
                .Subscribe(songView =>
                {
                    if (songView is null)
                    {
                        return;
                    }


                    CurrentPlayingSongView = songView;





                    _logger.LogTrace("BaseViewModel: _stateService.CurrentSong emitted: {SongTitle}", songView?.Title ?? "None");

                    CurrentTrackDurationSeconds = songView?.DurationInSeconds ?? 1;
                    AppTitle = songView != null
                        ? $"{songView.Title} - {songView.ArtistName} [{songView.AlbumName}] | {CurrentAppVersion}"
                        : CurrentAppVersion;
                    DimmerPlayEventList=_mapper.Map<ObservableCollection<DimmerPlayEventView>>(dimmerPlayEventRepo.GetAll());
                    if (DimmerPlayEventList is not null && DimmerPlayEventList.Count<1)
                    {
                        return;
                    }
                    CurrentPlayingSongView.PlayEvents = DimmerPlayEventList.Where(x => x.SongId==CurrentPlayingSongView.Id).ToObservableCollection();
                },
                ex => _logger.LogError(ex, "Error in CurrentSong subscription"))
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

                 IRepository<AppStateModel> appState = IPlatformApplication.Current!.Services.GetService<IRepository<AppStateModel>>()!;
                 var modell = appState.GetAll().FirstOrDefault();
                 if (modell is null)
                 {
                     _logger.LogWarning("AppStateModel not found, cannot set FolderPaths.");
                     return;
                 }
                 FolderPaths = new ObservableCollection<string>(modell.UserMusicFoldersPreference ?? Enumerable.Empty<string>());
                 folderMonitorService.Start(FolderPaths);
                 IsAppScanning = false;
             }, ex => _logger.LogError(ex, "Error processing FolderRemoved state."))
     );

        _subsManager.Add(
         _stateService.CurrentPlayBackState
             .Where(psi => psi.State == DimmerPlaybackState.PlaySongFrommOutsideApp)
             .Subscribe(async folderPath =>
             {
                 if (folderPath.ExtraParameter is null)
                 {
                     return;
                 }

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
                        Track track = new(CurrentPlayingSongView.FilePath);
                        PictureInfo? firstPicture = track.EmbeddedPictures?.FirstOrDefault(p => p.PictureData?.Length > 0);
                        CurrentPlayingSongView.CoverImageBytes = ImageResizer.ResizeImage(firstPicture?.PictureData);


                        CurrentPlayingSongView.IsCurrentPlayingHighlight = true;
                        AudioDevices = audioService.GetAllAudioDevices()?.ToObservableCollection();



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
            AudioEnginePositionObservable
                .Subscribe(positionSeconds =>
                {
                    CurrentTrackPositionSeconds = positionSeconds;
                    CurrentTrackPositionPercentage = CurrentTrackDurationSeconds > 0 ? (positionSeconds / CurrentTrackDurationSeconds) : 0;

                    var percentageConverter = new PercentageInverterConverter();
                    if (CurrentTrackPositionPercentage >=0.40)
                    {
                        ProgressOpacity = percentageConverter.Convert(CurrentTrackPositionPercentage, typeof(double), null, CultureInfo.InvariantCulture) as double?;


                    }

                }, ex => _logger.LogError(ex, "Error in AudioEnginePositionObservable subscription"))
        );



        _subsManager.Add(
     _stateService.AllCurrentSongs
         .Subscribe(songList =>
         {
             if (songList is null || songList.Count < 1)
             {
                 _songSource.Clear();
                 return;
             }

             _logger.LogTrace("BaseViewModel: Received {Count} songs. Updating SourceList.", songList.Count);

             var newSongListViews = _mapper.Map<IEnumerable<SongModelView>>(songList.Shuffle());



             _songSource.Edit(innerList =>
             {
                 innerList.Clear();
                 innerList.AddRange(newSongListViews);
             });


             if (audioService.IsPlaying)
                 return;


         }, ex => _logger.LogError(ex, "Error in AllCurrentSongs for _songSource subscription"))
 );

        _subsManager.Add(
             _stateService.AllPlayHistory
                .Subscribe(playEvents =>
                {
                    _logger.LogTrace("BaseViewModel: events (for all) emitted count: {Count}", playEvents?.Count ?? 0);
                    AllPlayEvents = playEvents.ToObservableCollection();
                }, ex => _logger.LogError(ex, "Error in AllCurrentSongs for _songSource subscription"))
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

                if (log.ViewSongModel is null || log.AppSongModel is null)
                {
                    return;
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

    [ObservableProperty]
    public partial bool IsSafeKeyboardAreaViewOpened { get; set; }

    [ObservableProperty]
    public partial PlaylistModel ActivePlaybackContext { get; set; }


    [RelayCommand]
    public void SetPreferredAudioDevice(AudioOutputDevice dev)
    {
        audioService.SetPreferredOutputDevice(dev);
    }
    // Add this field to your BaseViewModel class
    private readonly Random _random = new();

    #region Play Song Section

    /// <summary>
    /// The definitive method to play a song. It handles stopping the current track,
    /// logging events, setting the playback context, and starting the new track.
    /// </summary>
    /// <param name="songToPlay">The song to play from the current search results.</param>
    public async Task PlaySongFromListAsync(SongModelView? songToPlay, string? PlaylistQuery = null)
    {
        if (songToPlay == null)
        {
            _logger.LogWarning("PlaySongFromListAsync: songToPlay is null. Stopping playback.");
            audioService.Stop();
            return;
        }

        _logger.LogInformation("Requesting to play '{SongTitle}'.", songToPlay.Title);

        // --- CONTEXT LOCK & PLAYLIST MANAGEMENT ---
        // This is the core of your new logic.
        string queryContext = string.IsNullOrWhiteSpace(CurrentQuery) ? "Default Queue" : CurrentQuery;

        // Find a playlist that matches the current query context.
        var matchingPlaylists = _playlistRepo.Query(p => p.QueryText == queryContext);
        var playlistForPlayback = matchingPlaylists.FirstOrDefault();

        if (playlistForPlayback == null)
        {
            _logger.LogInformation("No playlist found for query '{Query}'. Creating new smart playlist.", queryContext);
            // Create a new smart playlist because one doesn't exist.
            var newPlaylist = new PlaylistModel
            {
                PlaylistName = queryContext, // Name it after the query by default
                QueryText = queryContext,
                IsSmartPlaylist = true,
                // You can add more properties here, like a DateCreated
            };
            playlistForPlayback = _playlistRepo.Create(newPlaylist);
        }
        else
        {
            // A playlist already exists, so we just update its 'last used' date.
            _playlistRepo.Update(playlistForPlayback.Id, livePlaylist =>
            {
                // You might want to add a 'DateLastAccessed' property to PlaylistModel
                // livePlaylist.DateLastAccessed = DateTimeOffset.UtcNow;
            });
            _logger.LogDebug("Found existing playlist '{Name}' for playback context.", playlistForPlayback.PlaylistName);
        }

        // Set the active playback context for the ViewModel.
        ActivePlaybackContext = playlistForPlayback;
        // ------------------------------------------

        // Stop current track if necessary
        if (audioService.IsPlaying && CurrentPlayingSongView != null && CurrentPlayingSongView.Id != songToPlay.Id)
        {
            _baseAppFlow.UpdateDatabaseWithPlayEvent(CurrentPlayingSongView, StatesMapper.Map(DimmerPlaybackState.Skipped), CurrentTrackPositionSeconds);
        }
        audioService.Stop();

        // Initialize and play the new song
        await audioService.InitializeAsync(songToPlay);
        audioService.Play();
        _baseAppFlow.UpdateDatabaseWithPlayEvent(songToPlay, StatesMapper.Map(DimmerPlaybackState.Playing), 0);
    }

    [RelayCommand]
    public async Task PlayPauseToggleAsync()
    {
        _logger.LogDebug("PlayPauseToggleAsync called. IsPlaying: {IsPlayingState}", IsPlaying);
        SongModelView? currentSongVm = CurrentPlayingSongView;
        if (currentSongVm == null)
        {
            _logger.LogInformation("PlayPauseToggleAsync: No current song. Attempting to play first song from results.");
            // If there's no song, play the first one from the current list
            await PlaySongFromListAsync(_searchResults.FirstOrDefault());
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
            // If resuming from a stopped state, we might need to re-initialize
            if (audioService.CurrentPosition == 0)
            {
                await audioService.InitializeAsync(CurrentPlayingSongView);
            }
            audioService.Play();
            _stateService.SetCurrentState(new PlaybackStateInfo(DimmerPlaybackState.Playing, null, currentSongVm, currentSongModel));
            _baseAppFlow.UpdateDatabaseWithPlayEvent(CurrentPlayingSongView, StatesMapper.Map(DimmerPlaybackState.Resumed), CurrentTrackPositionSeconds);
        }
    }

    [RelayCommand]
    public async Task NextTrack()
    {
        _logger.LogDebug("NextTrack requested.");
        // Because your reactive pipeline is so good, _searchResults is always the correct context.
        // We don't need to re-run the query.
        var nextSong = GetNextSongInSequence(1);
        await PlaySongFromListAsync(nextSong);
    }

    [RelayCommand]
    public async Task PreviousTrack()
    {
        _logger.LogDebug("PreviousTrack requested.");

        // Standard media player behavior: restart song if it has been playing for a bit.
        if (audioService.CurrentPosition > 3)
        {
            _logger.LogInformation("Restarting current track.");
            audioService.Seek(0);
            //audioService.Play(); // Ensure it plays if it was paused
            return;
        }

        var previousSong = GetNextSongInSequence(-1);
        await PlaySongFromListAsync(previousSong);
    }
    int currentIndex = 0;
    int nextIndex = 0;
    /// <summary>
    /// Determines the next song to play based on shuffle and repeat modes.
    /// </summary>
    /// <param name="direction">1 for next, -1 for previous.</param>
    /// <returns>The SongModelView to play next, or null if playback should stop.</returns>
    private SongModelView? GetNextSongInSequence(int direction)
    {
        if (!_searchResults.Any())
        {
            _logger.LogWarning("GetNextSongInSequence: Search results are empty.");
            return null;
        }

        var currentSong = CurrentPlayingSongView;
        if (currentSong == null)
        {
            _logger.LogInformation("GetNextSongInSequence: No current song, returning first in list.");
            return _searchResults[0];
        }

        // --- Handle Repeat One ---
        if (CurrentRepeatMode == RepeatMode.One)
        {
            _logger.LogDebug("RepeatMode.One active. Returning current song.");
            return currentSong;
        }

        // --- Handle Shuffle ---
        if (IsShuffleActive)
        {
            _logger.LogDebug("Shuffle is active. Finding random song.");
            if (_searchResults.Count <= 1)
            {
                return _searchResults.FirstOrDefault();
            }

            currentIndex = _searchResults.IndexOf(currentSong);

            do
            {
                nextIndex = _random.Next(_searchResults.Count);
            } while (nextIndex == currentIndex); // Avoid playing the same song twice in a row

            return _searchResults[nextIndex];
        }

        // --- Handle Normal Sequence (Next/Previous) ---
        currentIndex = _searchResults.IndexOf(currentSong);
        if (currentIndex == -1)
        {
            _logger.LogWarning("Current song not found in search results. Returning first song.");
            return _searchResults[0]; // Failsafe
        }

        nextIndex = currentIndex + direction;

        // Check bounds
        if (nextIndex >= _searchResults.Count)
        {
            return (CurrentRepeatMode == RepeatMode.All) ? _searchResults[0] : null;
        }

        if (nextIndex < 0)
        {
            return (CurrentRepeatMode == RepeatMode.All) ? _searchResults.Last() : null;
        }

        return _searchResults[nextIndex];
    }


    [RelayCommand]
    public void ToggleShuffleMode()
    {
        IsShuffleActive = !IsShuffleActive;
        _logger.LogInformation("Shuffle mode toggled to: {IsShuffleActive}", IsShuffleActive);

        var state = IsShuffleActive ? DimmerPlaybackState.ShuffleRequested : DimmerPlaybackState.ShuffleDisabled;
        _stateService.SetCurrentState(new PlaybackStateInfo(state, null, CurrentPlayingSongView, CurrentPlayingSongView?.ToModel(_mapper)));
    }

    [RelayCommand]
    public void ToggleRepeatPlaybackMode()
    {
        var currentMode = _settingsService.RepeatMode;
        // Cycle through the enum values: None -> One -> All -> None
        var nextMode = (RepeatMode)(((int)currentMode + 1) % Enum.GetNames(typeof(RepeatMode)).Length);
        _settingsService.RepeatMode = nextMode;
        CurrentRepeatMode = nextMode; // Assuming this is an [ObservableProperty]
        _logger.LogInformation("Repeat mode toggled to: {RepeatMode}", nextMode);

        _stateService.SetRepeatMode(nextMode);
    }

    #endregion
    [RelayCommand]
    public void SeekTrackPosition(double positionSeconds)
    {
        if (_songSource is null ||  _songSource?.Count <1)
        {
            return;
        }

        _logger.LogDebug("SeekTrackPosition called by UI to: {PositionSeconds}s", positionSeconds);


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
        _stateService.SetCurrentState(new PlaybackStateInfo(DimmerPlaybackState.FolderAdded, folderPath, null, null));
    }
    public void AddMusicFoldersByPassingToService(List<string> folderPath)
    {
        _logger.LogInformation("User requested to add music folder.");
        _stateService.SetCurrentState(new PlaybackStateInfo(DimmerPlaybackState.PlaySongFrommOutsideApp, folderPath, null, null));
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
            var songsToFix = realm.All<SongModel>().Filter("ArtistIds.@count == 0").ToList();

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


                    song.ArtistIds.Clear();

                    foreach (var artistName in namesForThisSong)
                    {

                        if (artistsFromDb.TryGetValue(artistName, out var artistModel))
                        {

                            song.ArtistIds.Add(artistModel);
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
        SelectedAlbumSongs ??=new();
        SelectedArtistAlbums ??=new();
        SelectedAlbumSongs.Clear();
        SelectedArtistAlbums.Clear();




        var db = realmFactory.GetRealmInstance();


        var artistIdToFind = artView.Id;


        var songsByArtist = db.All<SongModel>()
                              .Filter("Artist.Id == $0 OR ANY ArtistIds.Id == $0", artistIdToFind)
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
            SelectedAlbumSongs.Add(song.ToModelView(_mapper));
        }
        foreach (var album in albumsByArtist)
        {
            SelectedArtistAlbums.Add(album.ToModelView(_mapper));
        }































        _logger.LogInformation("Successfully prepared details for artist: {ArtistName}", SelectedArtist.Name);


    }
    partial void OnSelectedArtistSongsChanging(ObservableCollection<SongModelView>? oldValue, ObservableCollection<SongModelView>? newValue)
    {

    }
    partial void OnSelectedAlbumSongsChanging(ObservableCollection<SongModelView>? oldValue, ObservableCollection<SongModelView>? newValue)
    {

    }

    private void RefreshSongsCover(IEnumerable<SongModelView> songsToRefresh)
    {
        var OgSongs = _mapper.Map<ObservableCollection<SongModelView>>((songRepo.GetAll()));

        _ = Task.Run(async () =>
        {
            const int thumbnailSize = 96;



            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 4 };

            foreach (var songView in songsToRefresh)
            {


                if (songView.CoverImageBytes?.Length > 0 || string.IsNullOrEmpty(songView.FilePath))
                {
                    return;
                }

                try
                {

                    var track = new ATL.Track(songView.FilePath);
                    var originalImageData = track.EmbeddedPictures.FirstOrDefault()?.PictureData;

                    if (originalImageData != null)
                    {

                        byte[] thumbnailData = ImageResizer.ResizeImage(originalImageData, 650, thumbnailSize);



                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            songView.CoverImageBytes = thumbnailData;
                        });
                    }
                }
                catch (Exception ex)
                {

                    _logger.LogError(ex, "Failed to load cover for {FilePath}", songView.FilePath);
                }

                await Task.Delay(1);
            }
        });

    }

    public void GetStatsGeneral()
    {



        var endDate = DateTimeOffset.UtcNow;
        var startDate = endDate.AddMonths(-1);

        TopSkippedArtists = TopStats.GetTopSkippedArtists(songRepo.GetAll(), dimmerPlayEventRepo.GetAll(), 20).ToObservableCollection();
        TopSongsByEventType = TopStats.GetTopSongsByEventType(songRepo.GetAll(), dimmerPlayEventRepo.GetAll(), 20, 3).ToObservableCollection();
        TopSongsLastMonth = TopStats.GetTopCompletedSongs(songRepo.GetAll(), dimmerPlayEventRepo.GetAll(), 20, startDate, endDate).ToObservableCollection();




        MostSkipped = TopStats.GetTopSkippedSongs(songRepo.GetAll(), dimmerPlayEventRepo.GetAll(), 20).ToObservableCollection();


        MostListened = TopStats.GetTopSongsByListeningTime(songRepo.GetAll(), dimmerPlayEventRepo.GetAll(), 10, startDate, endDate).ToObservableCollection();
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
        if (string.IsNullOrEmpty(playlistName) || songsToAdd == null || !songsToAdd.Any())
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
                if (!livePlaylist.ManualSongIds.Contains(songId))
                {
                    livePlaylist.ManualSongIds.Add(songId);
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


}