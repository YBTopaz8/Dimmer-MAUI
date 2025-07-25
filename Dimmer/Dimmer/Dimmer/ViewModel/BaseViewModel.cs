using ATL;

using CommunityToolkit.Mvvm.Input;

using Dimmer.Data.ModelView.DimmerSearch;
using Dimmer.Data.ModelView.LibSanityModels;
using Dimmer.Data.ModelView.NewFolder;
using Dimmer.Data.RealmStaticFilters;
using Dimmer.DimmerSearch;
using Dimmer.DimmerSearch.Exceptions;
using Dimmer.DimmerSearch.TQL;
using Dimmer.Interfaces.Services;
using Dimmer.Interfaces.Services.Interfaces;
using Dimmer.Interfaces.Services.Interfaces.FileProcessing;
using Dimmer.LastFM;
using Dimmer.Utilities.Events;
using Dimmer.Utilities.Extensions;
using Dimmer.Utilities.StatsUtils;
using Dimmer.Utilities.ViewsUtils;

using DynamicData;
using DynamicData.Binding;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Maui.Graphics;


//using MoreLinq;
//using MoreLinq.Extensions;

using ReactiveUI;

using System.Buffers.Text;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using static Dimmer.Data.Models.PlaylistEvent;
using static Dimmer.Data.RealmStaticFilters.MusicPowerUserService;




namespace Dimmer.ViewModel;

public partial class BaseViewModel : ObservableObject, IReactiveObject, IDisposable
{
    private readonly IDuplicateFinderService _duplicateFinderService;
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
       IDuplicateFinderService duplicateFinderService,
        ILastfmService lastfmService,
       IRepository<ArtistModel> artistRepo,
       IRepository<AlbumModel> albumModel,
       IRepository<GenreModel> genreModel,
       ILogger<BaseViewModel> logger)
    {

        _lastfmService = lastfmService ?? throw new ArgumentNullException(nameof(lastfmService));
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

        _duplicateFinderService = duplicateFinderService;
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


        _searchQuerySubject = new BehaviorSubject<string>("");
        _filterPredicate = new BehaviorSubject<Func<SongModelView, bool>>(song => true);
        _sortComparer = new BehaviorSubject<IComparer<SongModelView>>(new SongModelViewComparer(null));
        _limiterClause = new BehaviorSubject<LimiterClause?>(null); // We DO need this to distinguish between limiter types.


        var initialSongs = realm.All<SongModel>().ToList().Select(song => song.ToViewModel());
        _songSource.AddRange(initialSongs);
        Debug.WriteLine($"[LOAD] Manually loaded {_songSource.Count} songs into SourceList.");


        _searchQuerySubject
            .Throttle(TimeSpan.FromMilliseconds(380), RxApp.TaskpoolScheduler)
           .Select(query =>
           {
               // If the query is empty, clear any previous error and return a "match all" state.
               if (string.IsNullOrWhiteSpace(query))
               {
                   // IMPORTANT: Return a tuple to provide both components and status.
                   return (Components: new QueryComponents(p => true, new SongModelViewComparer(null), null), ErrorMessage: (string?)null);
               }

               try
               {
                   var orchestrator = new MetaParser (query);
                   var components = new QueryComponents(
                      orchestrator.CreateMasterPredicate(),
                      orchestrator.CreateSortComparer(),
                      orchestrator.CreateLimiterClause()
                   );
                   // Success! Return the components and a null error message.
                   return (Components: components, ErrorMessage: (string?)null);
               }
               catch (ParsingException ex)
               {
                   // THIS IS THE FIX! We caught the error.
                   _logger.LogWarning(ex, "User search query failed to parse: {Query}", query);

                   // Return a null for components and the user-friendly error message.
                   return (Components: (QueryComponents?)null, ErrorMessage: ex.Message);
               }
               // You could add a catch-all for unexpected errors too
               catch (Exception ex)
               {
                   _logger.LogError(ex, "An unexpected error occurred during search parsing for query: {Query}", query);
                   return (Components: (QueryComponents?)null, ErrorMessage: "An unexpected error occurred.");
               }
           })

            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(result =>
            {
                if (result.ErrorMessage is null)
                {
                    DebugMessage = string.Empty;
                }
                else
                {
                    DebugMessage = result.ErrorMessage;
                }
                // ONLY update the pipeline if the parse was successful.
                if (result.Components is not null)
                {
                    var predicate = result.Components.Predicate ?? (song => true);
                    var comparer = result.Components.Comparer ?? new SongModelViewComparer(null);

                    _filterPredicate.OnNext(predicate);
                    _sortComparer.OnNext(comparer);
                    _limiterClause.OnNext(result.Components.Limiter);
                }
                // If result.Components is null, we do nothing. The last valid filter/sort remains active.
            },
            ex => _logger.LogError(ex, "FATAL: Search control pipeline has crashed."))
            .DisposeWith(Disposables);

        var controlPipeline = Observable.CombineLatest(
      _filterPredicate,
      _sortComparer,
      _limiterClause,
      (predicate, comparer, limiter) => new { predicate, comparer, limiter }
  );

        var searchResultsHolder = new SourceList<SongModelView>();

        // 2. The pipeline now reads from the master list and populates the results holder.
        _songSource.Connect()
            .ToCollection()
            .ObserveOn(RxApp.TaskpoolScheduler) // Heavy lifting on background thread
            .CombineLatest(controlPipeline, (songs, controls) => new { songs, controls })
            .Select(data =>
            {
                var predicate = data.controls.predicate;
                var comparer = data.controls.comparer;
                var limiter = data.controls.limiter;

                // The calculation logic you had is perfectly fine.
                var filtered = data.songs.Where(predicate);

                IOrderedEnumerable<SongModelView> sorted;
                if (limiter?.Type == LimiterType.Random)
                {
                    var random = new Random();
                    sorted = filtered.OrderBy(x => random.Next());
                }
                else if (limiter?.Type == LimiterType.Last)
                {
                    var invertedComparer = (comparer as SongModelViewComparer)?.Inverted() ?? comparer;
                    sorted = filtered.OrderBy(x => x, invertedComparer);
                }
                else
                {
                    sorted = filtered.OrderBy(x => x, comparer);
                }

                var limited = sorted.Take(limiter?.Count ?? int.MaxValue);
                return limited.ToList();
            })
            .ObserveOn(RxApp.MainThreadScheduler) // Switch to UI thread before editing the list
            .Subscribe(
                newList =>
                {
                    // IMPORTANT: We are editing the NEW holder, NOT the original _songSource.
                    searchResultsHolder.Edit(updater =>
                    {
                        updater.Clear();
                        updater.AddRange(newList);
                    });
                },
                ex => _logger.LogError(ex, "FATAL: Data calculation pipeline crashed!"))
            .DisposeWith(Disposables);

        // 3. Bind your public UI property (_searchResults) to the dedicated results holder.
        searchResultsHolder.Connect()
            .ObserveOn(RxApp.MainThreadScheduler) // Binding should always be on the UI thread
            .Bind(out _searchResults)
            .Subscribe(
                cs =>
                {

                },
                ex => _logger.LogError(ex, "FATAL: Data binding pipeline crashed!"))
            .DisposeWith(Disposables);




        _duplicateSource.Connect()
    .Sort(SortExpressionComparer<DuplicateSetViewModel>.Ascending(d => d.Title)) // Keep the list sorted
    .ObserveOn(RxApp.MainThreadScheduler) // Ensure UI updates are on the main thread
    .Bind(out _duplicateSets) // Bind the results to our public property
    .Subscribe() // Activate the pipeline
    .DisposeWith(Disposables);





        IReadOnlyCollection<DimmerPlayEvent>? allPlayEvents = dimmerPlayEventRepo.GetAll();
        DimmerPlayEvent? evt = allPlayEvents?.MaxBy(x => x.EventDate);


        ActiveFilters.CollectionChanged += (s, e) => RebuildAndExecuteQuery();



        if (evt?.SongId is ObjectId songId && songId != ObjectId.Empty)
        {

            var song = songRepo.GetById(songId);
            if (song is null)
            {
                return;
            }
            CurrentPlayingSongView = song.ToModelView();


        }
        else
        {
            // Handle the case where there's no valid event or the event has no valid song.
            CurrentPlayingSongView = new();
        }
        //// Step 2: Map them to the ViewModel type.
        //var allPlayEventViews = _mapper.Map<IEnumerable<DimmerPlayEventView>>(allPlayEvents);

        //// Step 3: Load them into our source list. This fires one big "add" change,
        //// which is exactly what our stats pipeline needs to get its initial data.
        //_playEventSource.AddRange(allPlayEventViews);


        //SearchSongSB_TextChanged("random");

        FolderPaths = _settingsService.UserMusicFoldersPreference.ToObservableCollection();
        _lastfmService.IsAuthenticatedChanged
           .ObserveOn(RxApp.MainThreadScheduler) // Ensure UI updates on the main thread
           .Subscribe(isAuthenticated =>
           {
               IsLastfmAuthenticated = isAuthenticated;
               LastfmUsername = _lastfmService.AuthenticatedUser ?? "Not Logged In";
           })
           .DisposeWith(Disposables); // Assuming you have a reactive disposables manager
        _lastfmService.Start();


        UIQueryComponents.CollectionChanged += (s, e) => RebuildAndExecuteQuery();
    }
    private readonly ReadOnlyObservableCollection<string> _liveArtists;
    private readonly ReadOnlyObservableCollection<string> _liveAlbums;
    private readonly ReadOnlyObservableCollection<string> _liveGenres;

    private readonly ReadOnlyObservableCollection<string> _masterArtists;
    private readonly ReadOnlyObservableCollection<string> _masterAlbums;
    private readonly ReadOnlyObservableCollection<string> _masterGenres;

    private readonly AutocompleteEngine _autocompleteEngine;
    public ObservableCollection<IQueryComponentViewModel> UIQueryComponents { get; } = new();
    public void SearchSongSB_TextChanged(string searchText)
    {

        _searchQuerySubject.OnNext(searchText);

        CurrentQuery= searchText;

    }
    private readonly BehaviorSubject<string> _searchQuerySubject;

    private readonly BehaviorSubject<Func<SongModelView, bool>> _filterPredicate;
    private readonly BehaviorSubject<IComparer<SongModelView>> _sortComparer;
    //private readonly BehaviorSubject<LimiterClause?> _limiterClause;

    [ObservableProperty]
    public partial string DebugMessage { get; set; } = string.Empty;
    [RelayCommand]
    public void SmolHold()
    {
        SearchSongSB_TextChanged("Len:<=2:00");
    }

    [RelayCommand]
    public void Randomize()
    {
        SearchSongSB_TextChanged("random");
    }

    [RelayCommand]
    public void BigHold()
    {
        SearchSongSB_TextChanged("Len:<=3:00");
    }

    [RelayCommand]
    public void ResetSearch()
    {
        _searchQuerySubject.OnNext("random");
        CurrentQuery= "random";
    }

    private readonly SourceList<DimmerPlayEventView> _playEventSource = new();
    private readonly CompositeDisposable _disposables = new();
    private IDisposable? _realmSubscription;
    private bool _isDisposed;

    [ObservableProperty]
    public partial SongViewMode CurrentSongViewMode { get; set; } = SongViewMode.DetailedGrid;
    private readonly BehaviorSubject<ValidationResult> _validationResultSubject = new(new(true));
    public IObservable<ValidationResult> ValidationResult => _validationResultSubject;



    public ReadOnlyObservableCollection<SongModelView> SearchResults => _searchResults;
    private ReadOnlyObservableCollection<SongModelView> _searchResults;

    protected CompositeDisposable Disposables { get; } = new CompositeDisposable();


    [ObservableProperty]
    public partial Label SongsCountLabel { get; set; }
    [ObservableProperty]
    public partial Label TranslatedSearch { get; set; }




    #region privte fields
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

    #endregion
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
    public partial string AppTitle { get; set; } = "Dimmer";

    public const string CurrentAppVersion = "Dimmer v1.8heta";

    [ObservableProperty]
    public partial SongModelView CurrentPlayingSongView { get; set; }

    [ObservableProperty]
    public partial SongModelView EditableSongView { get; set; }

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
    protected readonly ILastfmService _lastfmService;

    [RelayCommand]
    public async Task LoadUserLastFMInfo()
    {
        if (!_lastfmService.IsAuthenticated)
        {
            return;
        }
        var usr= await _lastfmService.GetUserInfoAsync();
        if (usr is null)
        {
            _logger.LogWarning("Failed to load Last.fm user info.");
            return;
        }
        UserLocal.LastFMAccountInfo.Name = usr.Name;
        UserLocal.LastFMAccountInfo.RealName = usr.RealName;
        UserLocal.LastFMAccountInfo.Url = usr.Url;
        UserLocal.LastFMAccountInfo.Country = usr.Country;
        UserLocal.LastFMAccountInfo.Age = usr.Age;
        UserLocal.LastFMAccountInfo.Playcount= usr.Playcount;
        UserLocal.LastFMAccountInfo.Playlists = usr.Playlists;
        UserLocal.LastFMAccountInfo.Registered = usr.Registered;
        UserLocal.LastFMAccountInfo.Gender = usr.Gender;
        UserLocal.LastFMAccountInfo.Image = new LastFMUserView.LastImageView();
        UserLocal.LastFMAccountInfo.Image.Url = usr.Images.LastOrDefault()?.Url;
        UserLocal.LastFMAccountInfo.Image.Size = usr.Images.LastOrDefault()?.Size;
        var rlm= realmFactory.GetRealmInstance();
        rlm.Write(() =>
        {
            var usre= rlm.All<UserModel>().ToList();
            if (usre is not null)
            {
                var usrr = usre.FirstOrDefault();
                if (usrr is not null )
                {
                    usrr.LastFMAccountInfo=new();
                    usrr.LastFMAccountInfo.Name = usr.Name;
                    usrr.LastFMAccountInfo.RealName = usr.RealName;
                    usrr.LastFMAccountInfo.Url = usr.Url;
                    usrr.LastFMAccountInfo.Country = usr.Country;
                    usrr.LastFMAccountInfo.Age = usr.Age;
                    usrr.LastFMAccountInfo.Playcount= usr.Playcount;
                    usrr.LastFMAccountInfo.Playlists = usr.Playlists;
                    usrr.LastFMAccountInfo.Registered = usr.Registered;
                    usrr.LastFMAccountInfo.Gender = usr.Gender;

                    usrr.LastFMAccountInfo.Image =new LastFMUser.LastImage();
                    usrr.LastFMAccountInfo.Image.Url=usr.Images.LastOrDefault().Url;
                    usrr.LastFMAccountInfo.Image.Size=usr.Images.LastOrDefault().Size;
                    rlm.Add(usrr, update: true);
                }
                else
                {
                    usrr = new UserModel();
                    usrr.LastFMAccountInfo=new();
                    usrr.Id=new();
                    usrr.UserName= usr.Name;
                    usrr.LastFMAccountInfo.Name = usr.Name;
                    usrr.LastFMAccountInfo.RealName = usr.RealName;
                    usrr.LastFMAccountInfo.Url = usr.Url;
                    usrr.LastFMAccountInfo.Country = usr.Country;
                    usrr.LastFMAccountInfo.Age = usr.Age;
                    usrr.LastFMAccountInfo.Playcount= usr.Playcount;
                    usrr.LastFMAccountInfo.Playlists = usr.Playlists;
                    usrr.LastFMAccountInfo.Registered = usr.Registered;
                    usrr.LastFMAccountInfo.Gender = usr.Gender;
                    usrr.LastFMAccountInfo.Image =new LastFMUser.LastImage();
                    usrr.LastFMAccountInfo.Image.Url=usr.Images.LastOrDefault().Url;
                    usrr.LastFMAccountInfo.Image.Size=usr.Images.LastOrDefault().Size;

                    rlm.Add(usrr, update: true);
                }
            }
        });
    }

    // Properties for UI binding
    [ObservableProperty]
    public partial bool IsLastfmAuthenticated { get; set; }
    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string LastfmUsername { get; set; }


    [RelayCommand]
    public async Task LoginToLastfm() 
    {
        if (string.IsNullOrEmpty(UserLocal.LastFMAccountInfo.Name))
        {
            await Shell.Current.DisplayAlert("One More Step", "Please Put in Your Account's UserName", "OK");
            return;
        }
        IsBusy = true;
        try
        {
            // 1. Get the URL from our service
            string url = await _lastfmService.GetAuthenticationUrlAsync();
            await Shell.Current.DisplayAlert(
               "Authorize in Browser",
               "Please authorize Dimmer in the browser window that will open, then return here and press 'Complete Login'.",
               "OK");
            // 2. Open it in the browser
            await Launcher.Default.OpenAsync(new Uri(url));

            // 3. Update UI to prompt user to finish
            // e.g., Show a message: "Please authorize in your browser, then click 'Finish Login'."
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Last.fm authentication URL.");
            IsBusy = false;
            return;
        }

    }
    [RelayCommand]
    public async Task CompleteLoginAsync()
    {
        IsBusy = true;
        try
        {
            // Call the second-step method in your service
            bool success = await _lastfmService.CompleteAuthenticationAsync(UserLocal.LastFMAccountInfo.Name);

            if (success)
            {
                await Shell.Current.DisplayAlert("Success!", $"Successfully logged in as {_lastfmService.AuthenticatedUser}.", "Awesome!");
            }
            else
            {
                await Shell.Current.DisplayAlert("Login Failed", "Could not complete the login process. Please try again.", "OK");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while completing Last.fm login.");
            await Shell.Current.DisplayAlert("Error", "An unexpected error occurred. Please try again.", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }
    // Example command for logging out
    [RelayCommand]
    private void LogoutFromLastfm()
    {
        _lastfmService.Logout();
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
                        _logger.LogWarning("evt with ID {SongId} not found in DB, skipping.", songViewModel.Id);
                        continue;
                    }


                    if (songDb.Album == null)
                    {
                        _logger.LogWarning("evt '{Title}' has no associated album, cannot update album artists.", songDb.Title);
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
                _logger.LogWarning("evt with ID {SongId} not found in DB. Cannot refresh metadata.", songViewModel.Id);
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
                    _logger.LogWarning("evt '{Title}' has no associated album, cannot update album artists.", freshSongDb.Title);
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





    [ObservableProperty]
    public partial string CurrentQuery { get; set; }
    public string QueryBeforePlay { get; private set; }

    readonly IRealmFactory realmFactory;
    private readonly Realm realm;

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
    public async Task Initialize()
    {
        InitializeApp();

        SubscribeToStateServiceEvents();
        SubscribeToAudioServiceEvents();
        SubscribeToLyricsFlow();
        await EnsureAllCoverArtCachedForSongsAsync();
    }

    public void InitializeApp()
    {

    }
    #region Subscription Event Handlers (The Reactive Logic)

    [ObservableProperty]
    public partial string CurrentCoverImagePath { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<ArtistModelView> AllAvailableArtists { get; set; }
    private void OnPlaybackStarted(PlaybackEventArgs args)
    {
        if (args.MediaSong is null)
        {
            _logger.LogWarning("OnPlaybackPaused was called but the event had no song context.");
            return;
        }

        CurrentPlayingSongView = args.MediaSong;
        _songToScrobble = CurrentPlayingSongView; // This is the next candidate.

 

        _logger.LogInformation("AudioService confirmed: Playback started for '{Title}'", args.MediaSong.Title);
        _baseAppFlow.UpdateDatabaseWithPlayEvent(realmFactory, args.MediaSong, StatesMapper.Map(DimmerPlaybackState.Playing), 0);
        UpdateSongSpecificUi(CurrentPlayingSongView);


    }
    private void UpdateSongSpecificUi(SongModelView? song)
    {
        if (song is null)
        {
            // What should the UI show when nothing is playing?
            AppTitle = "Dimmer - 1.3Theta"; // Reset the title
            CurrentTrackDurationSeconds = 1; // Prevent division by zero
            return;
        }

        AppTitle = $"{song.Title} - {song.OtherArtistsName} | {song.AlbumName} ({song.ReleaseYear}) | {CurrentAppVersion}";
        CurrentTrackDurationSeconds = song.DurationInSeconds > 0 ? song.DurationInSeconds : 1;
        // Trigger the new, evolved cover art loading process

      _=  LoadAndCacheCoverArtAsync(song);
    }



    async partial void OnSelectedSongChanged(SongModelView? oldValue, SongModelView? newValue)
    {
        if (newValue is not null)

        {
            PrepareForEditing(newValue);

            await LoadAndCacheCoverArtAsync(newValue);
            // Efficiently load related data
            newValue.PlayEvents = _mapper.Map<ObservableCollection<DimmerPlayEventView>>(
                songRepo.GetById(newValue.Id)?.PlayHistory);
        }
    }
    /// <summary>
    /// Creates a deep, unmanaged copy of the selected song for safe editing.
    /// </summary>
    private void PrepareForEditing(SongModelView song)
    {
        // Use your mapper to create a clean copy. This assumes you have a
        // SongModelView -> SongModelView mapping configured in AutoMapper.
        // If not, you can manually create a new SongModelView and copy properties.
        EditableSongView = _mapper.Map<SongModelView>(song);
    }

    /// <summary>
    /// Loads all artists from the database into a collection for the UI to bind to.
    /// </summary>
    private async Task LoadAllArtistsAsync()
    {
        // Run on a background thread to not block UI
        var artists = await Task.Run(() => artistRepo.GetAll());
        var artistViews = _mapper.Map<List<ArtistModelView>>(artists);

        AllAvailableArtists.Clear();
        foreach (var artist in artistViews.OrderBy(a => a.Name))
        {
            AllAvailableArtists.Add(artist);
        }
    }


    /// <summary>
    /// A robust, multi-stage process to load cover art. It prioritizes existing paths,
    /// checks for cached files, and only extracts from the audio file as a last resort,
    /// caching the result for future use.
    /// </summary>
    public async Task LoadAndCacheCoverArtAsync(SongModelView song)
    {
        // Don't start the process if the image is already loaded in the UI object.
        if (song.CoverImageBytes != null && song.CoverImageBytes.Length>1 || !string.IsNullOrEmpty(song.CoverImagePath))
        {
            CurrentCoverImagePath= song.CoverImagePath;
            return;
        }
            
            // --- Stage 2: Extract picture info from the audio file using ATL ---
            PictureInfo? embeddedPicture = null;
            try
            {
            if (!File.Exists(song.FilePath))
            {
                return;
            }
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
            CurrentCoverImagePath= finalImagePath;
            // Load the image bytes for the UI
            song.CoverImageBytes = await File.ReadAllBytesAsync(finalImagePath);
                _logger.LogTrace("Loaded cover art from new/cached path: {ImagePath}", finalImagePath);

                // If the path is new, update our song model and save it to the database.
                if (song.CoverImagePath != finalImagePath)
                {
                    song.CoverImagePath= finalImagePath;
                    using var realm = realmFactory.GetRealmInstance();
                    if (realm is null)
                    {
                        _logger.LogError("Failed to get Realm instance from RealmFactory.");
                        return;
                    }
                    // Update the song in the database with the new cover image path.
                    await realm.WriteAsync(() =>
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
        
    }

   
    public async Task EnsureCoverArtCachedForSongsAsync(IEnumerable<SongModelView> songsToProcess)
    {
        // Get a copy of the current list to avoid issues if it changes during the process.

        _logger.LogInformation("Starting to pre-cache cover art for {Count} visible songs.", songsToProcess.Count());

        // This is a great use case for parallel processing.
        await Parallel.ForEachAsync(songsToProcess, async (song, cancellationToken) =>
        {
            // We only need to process songs that don't already have a valid path.
            if (string.IsNullOrEmpty(song.CoverImagePath) || !File.Exists(song.CoverImagePath))
            {
                // We re-use the same core logic, but we don't need to load the bytes into the UI here.
                await LoadAndCacheCoverArtAsync(song);

            }
        });

        _logger.LogInformation("Finished pre-caching cover art process.");
    }

    public async Task EnsureAllCoverArtCachedForSongsAsync()
    {
        // Get a copy of the current list to avoid issues if it changes during the process.
        IEnumerable<SongModelView> songsToProcess = _songSource.Items.AsEnumerable();

        // This is a great use case for parallel processing.
        await Parallel.ForEachAsync(songsToProcess, async (song, cancellationToken) =>
        {
            // We only need to process songs that don't already have a valid path.
            if (string.IsNullOrEmpty(song.CoverImagePath) || !File.Exists(song.CoverImagePath))
            {
                // We re-use the same core logic, but we don't need to load the bytes into the UI here.
               await  LoadAndCacheCoverArtAsync(song);
               
            }
        });

        _logger.LogInformation("Finished pre-caching ALL cover art process.");
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

    private async Task OnPlaybackEnded()
    {
        _logger.LogInformation("AudioService confirmed: Playback ended for '{Title}'", CurrentPlayingSongView?.Title ?? "N/A");
        if (CurrentPlayingSongView == null)
        {

            return;
        }

        _baseAppFlow.UpdateDatabaseWithPlayEvent(realmFactory, CurrentPlayingSongView, StatesMapper.Map(DimmerPlaybackState.PlayCompleted), CurrentTrackDurationSeconds);

       
        // Automatically play the next song in the queue.
      await  NextTrack();
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
        AppTitle = $"{CurrentAppVersion} | {songView.Title} - {songView.ArtistName} ";

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
        var newSongs = stateInfo.ExtraParameter as List<SongModelView>;
        if(newSongs != null && newSongs.Count > 0)
        { 
            _logger.LogInformation("Adding {Count} new songs to the UI.", newSongs.Count);

        _songSource.AddRange(newSongs);

        _ = EnsureCoverArtCachedForSongsAsync(newSongs);

         var   _lyricsCts = new CancellationTokenSource();
            _ = LoadSongDataAsync(null, _lyricsCts);
        }
    else
    {
        _logger.LogInformation("Scan completed, but no new songs were passed to the UI.");
    }




        var realmm = realmFactory.GetRealmInstance();

        var appModel = realmm.All<AppStateModel>().ToList();
        if (appModel is not null && appModel.Count>0)
        {
            var appmodel = appModel[0];

            FolderPaths = appmodel.UserMusicFoldersPreference.ToObservableCollection();
   
        }
    }

    #endregion

    #region Playback Commands (User Intent)

    #region Playback Commands (User Intent)

    [RelayCommand]
    public async Task PlaySong(SongModelView? songToPlay)
    {
        if (songToPlay == null)
            return;

        // --- Step 1: Get the current UI results and FREEZE them into a new list ---
        // .ToList() creates a brand new list in memory, a perfect snapshot.
        var baseQueue = _searchResults.ToList();
        int startIndex = baseQueue.IndexOf(songToPlay);

        if (startIndex == -1)
        {
            _logger.LogError("Could not find song '{Title}' to start playback.", songToPlay.Title);
            return;
        }

        // --- Step 2: Set the private _playbackQueue to this frozen snapshot ---
        if (IsShuffleActive)
        {
            var shuffledQueue = baseQueue.OrderBy(x => _random.Next()).ToList();
            shuffledQueue.Remove(songToPlay);
            shuffledQueue.Insert(0, songToPlay);
            _playbackQueue = shuffledQueue; // _playbackQueue is now the shuffled snapshot
            startIndex = 0;
        }
        else
        {
            _playbackQueue = baseQueue; // _playbackQueue is now the ordered snapshot
        }

        // --- Step 3: Save the context for the FUTURE (e.g., if the app restarts) ---
        CurrentPlaybackQuery = CurrentQuery;
        SavePlaybackContext(CurrentPlaybackQuery); // Your smart save context method

        // --- Step 4: Start playback using the now-independent queue ---
        await StartAudioForSongAtIndex(startIndex);
    }   

    [RelayCommand]
    public async Task PlayPauseToggle()
    {
        if (CurrentPlayingSongView.Title == null)
        {
           await PlaySong(_searchResults.FirstOrDefault());
            return;
        }
        if (audioService.CurrentTrackMetadata is null)
        {
          await  PlaySong(CurrentPlayingSongView);
            return;
        }
        if (IsPlaying)
            audioService.Pause();
        else
            audioService.Play();
    }

    [RelayCommand]
    public async Task NextTrack()
    {
        if (IsPlaying && CurrentPlayingSongView != null)
        {
            _baseAppFlow.UpdateDatabaseWithPlayEvent(realmFactory, CurrentPlayingSongView, StatesMapper.Map(DimmerPlaybackState.Skipped), CurrentTrackPositionSeconds);
        }
        var nextIndex = GetNextIndexInQueue(1);
        await StartAudioForSongAtIndex(nextIndex);

        if (IsPlaying && _songToScrobble != null && IsLastfmAuthenticated)
        {
            await _lastfmService.ScrobbleAsync(_songToScrobble);
        }
    }

    [RelayCommand]
    public async Task PreviousTrack()
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
        await StartAudioForSongAtIndex(prevIndex);
        if (IsPlaying && _songToScrobble != null && IsLastfmAuthenticated)
        {
            await _lastfmService.ScrobbleAsync(_songToScrobble);
        }
    }

    #endregion

    #region Private Playback Helper Methods

    private async Task StartAudioForSongAtIndex(int index)
    {
        _playbackQueueIndex = index;

        if (_playbackQueueIndex == -1)
        {
            _logger.LogInformation("Playback queue finished. Stopping playback.");
            audioService.Stop();
            UpdateSongSpecificUi(null); // Clear the UI
            return;
        }

        
        // Get the song DIRECTLY from our private, frozen queue.
        // No need to search, no need to touch SearchResults, no UI flickering.
        var songToPlay = _playbackQueue[_playbackQueueIndex];

      

        if (songToPlay.FilePath == null || !File.Exists(songToPlay.FilePath))
        {
            _logger.LogError("Song file not found for '{Title}'. Skipping to next track.", songToPlay.Title);
            await NextTrack(); // This will recursively call until a valid file is found or the queue ends.
            await ValidateSongAsync(songToPlay);
            return;
        }

        audioService.Stop();
        await audioService.InitializeAsync(songToPlay);
    }

    private int _playbackQueueIndex = -1;
    private IReadOnlyList<SongModelView> _playbackQueue = new List<SongModelView>();

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
    // This is much more reliable than parsing names like "Playback Session: ..."
    private const string LastSessionPlaylistName = "__LastPlaybackSession";

    private void SavePlaybackContext(string query)
    {
        // --- Step 1: Find the existing "Last Session" playlist using RQL ---
        var existingPlaylist = _playlistRepo.FirstOrDefaultWithRQL("PlaylistName == $0", LastSessionPlaylistName);

        // --- Step 2: Check if the query is the same ---
        if (existingPlaylist != null && existingPlaylist.QueryText == query)
        {
            // --- PATH A: THE QUERIES MATCH ---
            // The user is re-playing the same queue. Don't create a new playlist.
            // Just update the timestamp of the existing one.
            _logger.LogInformation("Same query detected. Updating existing session playlist.");

            _playlistRepo.Update(existingPlaylist.Id, playlistInDb =>
            {
                playlistInDb.LastPlayedDate = DateTimeOffset.UtcNow;
                playlistInDb.PlayHistory.Add(new PlaylistEvent());
            });
        }
        else
        {
            // --- PATH B: NEW QUERY OR NO EXISTING SESSION ---
            // We need to create or overwrite the "Last Session" playlist.
            _logger.LogInformation("New query detected. Overwriting session playlist.");

            // Prepare the new playlist object.
            var contextPlaylist = new PlaylistModel
            {
                // If an old session playlist exists, we RE-USE its ID to ensure we overwrite it.
                // If not, we generate a new ID to create it for the first time.
                Id = existingPlaylist?.Id ?? ObjectId.GenerateNewId(),
                PlaylistName = LastSessionPlaylistName, // Use our constant name
                IsSmartPlaylist = !string.IsNullOrEmpty(query),
                QueryText = query,
                DateCreated = DateTimeOffset.UtcNow,
                LastPlayedDate = DateTimeOffset.UtcNow,
            };

            // Add the first play event to its history.
            contextPlaylist.PlayHistory.Add(new PlaylistEvent());

            // Populate the song list.
            foreach (var song in _playbackQueue)
            {
                contextPlaylist.SongsIdsInPlaylist.Add(song.Id);
            }

            // Use Upsert. This will CREATE the playlist if the ID is new,
            // or UPDATE/OVERWRITE it if the ID already exists. Perfect for our needs.
            _playlistRepo.Upsert(contextPlaylist);
        }

        // This part remains the same.
        QueryBeforePlay = query;
        _logger.LogInformation("Saved playback context for query: \"{query}\"", query);
    }
    /// <summary>
    /// Plays an entire playlist from the beginning.
    /// </summary>
    [RelayCommand]
    private async Task PlayPlaylist(PlaylistModelView? playlist)
    {
        if (playlist == null || !playlist.SongsIdsInPlaylist.Any())
        {
            _logger.LogWarning("PlayPlaylist called with a null or empty playlist.");
            return;
        }

        // The playlist already defines the queue. No need for shuffle logic here,
        // as we want to respect the saved order of the playlist.
        //_playbackQueue = playlist.SongsIdsInPlaylist.ToList();
        CurrentPlaybackQuery = $"playlist:\"{playlist.PlaylistName}\""; // Set context for UI

        _logger.LogInformation("Playback queue established from playlist '{PlaylistName}' with {Count} songs.", playlist.PlaylistName, _playbackQueue.Count);

        // No need to call SavePlaybackContext, as this isn't a temporary search session.
        // Start playback from the first song (index 0).
       await StartAudioForSongAtIndex(0);
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
            var remainingQueue = _playbackQueue.Where(song => song.Id != currentSongId).ToList();

            if (IsShuffleActive)
            {
                // If shuffle was just turned ON, shuffle the remaining songs.
                var shuffledRemaining = remainingQueue.OrderBy(x => _random.Next()).ToList();

                // Rebuild the queue with the current song at the front, followed by the new shuffled part.
                var newQueue = new List<SongModelView> { CurrentPlayingSongView };
                newQueue.AddRange(shuffledRemaining);
                _playbackQueue = newQueue;
                _playbackQueueIndex = 0; // We are at the start of this new conceptual queue.
            }
            else
            {
                // If shuffle was just turned OFF, we should revert the queue to the original,
                // sorted order from the search results, but keep our current position.
                _playbackQueue = _searchResults.ToList();
                // Find the new index of our current song in the un-shuffled list.
                _playbackQueueIndex = _playbackQueue.IndexOf(CurrentPlayingSongView);
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
        return;
        if (context?.Playlist == null || context.SongToPlay == null || !context.Playlist.SongsIdsInPlaylist.Any())
        {
            _logger.LogWarning("PlaySongFromPlaylist called with invalid context.");
            return;
        }

        var playlist = context.Playlist;
        var songToPlay = context.SongToPlay;

        //// The playlist's song list becomes our new frozen queue.
        //_playbackQueue = playlist.SongsIdsInPlaylist.ToList();

        //// Find the starting index of the song the user clicked.
        //var startIndex = _playbackQueue.IndexOf(songToPlay.Id);
        //if (startIndex == -1)
        //{
        //    _logger.LogError("Could not find song '{SongTitle}' in playlist '{PlaylistName}'. Starting from the beginning.", songToPlay.Title, playlist.PlaylistName);
        //    startIndex = 0;
        //}

        //CurrentPlaybackQuery = $"playlist:\"{playlist.PlaylistName}\""; // Set context for UI

        //_logger.LogInformation("Playback queue established from playlist '{PlaylistName}', starting with '{SongTitle}'.", playlist.PlaylistName, songToPlay.Title);

        //StartAudioForSongAtIndex(startIndex);
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
            .Subscribe( HandlePlaybackStateChange, ex => _logger.LogError(ex, "Error in PlaybackStateChanged subscription")));

        // --- Simple Property Updates ---
        // IsPlayingChanged is a simple boolean event, so we handle it directly.
        // The compiler error was about the generic type. We just need to get the bool from the args.
        _subsManager.Add(Observable.FromEventPattern<PlaybackEventArgs>(
                h => audioService.IsPlayingChanged += h,
                h => audioService.IsPlayingChanged -= h)
            .Select(evt => evt.EventArgs.IsPlaying) // Get the boolean value from the event args
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(isPlaying =>
            {
                IsPlaying = isPlaying;

                }
            , ex => _logger.LogError(ex, "Error in IsPlayingChanged subscription")));

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
            .Subscribe(async _ => await OnPlaybackEnded(), ex => _logger.LogError(ex, "Error in PlayEnded subscription")));

        // --- Media Key Integration (These were already correct) ---
        _subsManager.Add(Observable.FromEventPattern<PlaybackEventArgs>(
                h => audioService.MediaKeyNextPressed += h,
                h => audioService.MediaKeyNextPressed -= h)
            .Subscribe(async _ => await NextTrack(), ex => _logger.LogError(ex, "Error in MediaKeyNextPressed subscription")));

        _subsManager.Add(Observable.FromEventPattern<PlaybackEventArgs>(
                h => audioService.MediaKeyPreviousPressed += h,
                h => audioService.MediaKeyPreviousPressed -= h)
            .Subscribe(async _ => await PreviousTrack(), ex => _logger.LogError(ex, "Error in MediaKeyPreviousPressed subscription")));
    }

    /// <summary>
    /// A new helper method to route playback state changes to the correct handler.
    /// This is the target of our main PlaybackStateChanged subscription.
    /// </summary>
    private SongModelView? _songToScrobble;
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
            .Subscribe(LatestDeviceLog, ex => _logger.LogError(ex, "Error on FolderScanCompleted.")));

        // PlaylistExhausted is now handled by the PlaybackEnded event from the audio service, making this obsolete.
    }

    private void LatestDeviceLog(AppLogModel model)
    {
        LatestAppLog=model;
        LatestScanningLog = model.Log;
    }

    [ObservableProperty] public partial string CurrentPlaybackQuery { get; set; }

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
        //Task.Run(() => libService.LoadInSongsAndEvents());

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



    [ObservableProperty]
    public partial DimmerStats? SongListeningStreak { get; set; }

    [ObservableProperty]
    public partial DimmerStats? SongEvergreenScore {get;set;}

    [ObservableProperty]
    public partial ObservableCollection<DimmerStats>? SongWeekdayVsWeekend {get;set;}


    /// <summary>
    /// Loads ALL global, library-wide statistics. Call this once when the page loads.
    /// </summary>
    public void GetStatsGeneral()
    {
        // It's more efficient to get these once and pass them around.
        var allSongs = songRepo.GetAll();
        var allEvents = dimmerPlayEventRepo.GetAll();
        int topCount = 10;

        // --- Time-based Filters ---
        var endDate = DateTimeOffset.UtcNow;
        var monthStartDate = endDate.AddMonths(-1);
        var yearStartDate = endDate.AddYears(-1);

        // --- Basic Rankings (from TopStats) ---
        TopSongsLastMonth = TopStats.GetTopCompletedSongs(allSongs, allEvents, topCount, monthStartDate, endDate).ToObservableCollection();
        MostSkippedSongs = TopStats.GetTopSkippedSongs(allSongs, allEvents, topCount).ToObservableCollection();
        ArtistsByHighestSkipRate = TopStats.GetArtistsByHighestSkipRate(allEvents, allSongs, topCount).ToObservableCollection();
        TopBurnoutSongs = TopStats.GetTopBurnoutSongs(allEvents, allSongs, topCount).ToObservableCollection();
        TopRediscoveredSongs = TopStats.GetTopRediscoveredSongs(allEvents, allSongs, topCount).ToObservableCollection();
        TopArtistsByVariety = TopStats.GetTopArtistsBySongVariety(allEvents, allSongs, topCount).ToObservableCollection();
        TopGenresByListeningTime = TopStats.GetTopGenresByListeningTime(allEvents, allSongs, topCount).ToObservableCollection();

        // --- Populating Chart-Specific Properties ---
        OverallListeningByDayOfWeek = ChartSpecificStats.GetOverallListeningByDayOfWeek(allEvents, allSongs).ToObservableCollection();
        DeviceUsageByTopArtists = TopStats.GetDeviceUsageByTopArtists(allEvents, allSongs, 5).ToObservableCollection(); // Using TopStats method
        GenrePopularityOverTime = ChartSpecificStats.GetGenrePopularityOverTime(allEvents, allSongs).ToObservableCollection();
        DailyListeningTimeRange = ChartSpecificStats.GetDailyListeningTimeRange(allEvents, allSongs, monthStartDate, endDate).ToObservableCollection();
        SongProfileBubbleChart = ChartSpecificStats.GetSongProfileBubbleChartData(allEvents, allSongs).ToObservableCollection();
        DailyListeningRoutineOHLC = ChartSpecificStats.GetDailyListeningRoutineOHLC(allEvents, allSongs, monthStartDate, endDate).ToObservableCollection();

        // Note: Waterfall, Histogram, BoxPlot might require more specific logic or different data shaping
        // depending on the exact chart component API. These are placeholders for that future implementation.
    }

    /// <summary>
    /// Loads ALL statistics for a single song. Call this when a song is selected.
    /// </summary>
    public void LoadStatsForSelectedSong(SongModelView? song)
    {
        song ??= SelectedSong;

        if (song == null)
        {
            ClearSingleSongStats();
            return;
        }

        // It's much more efficient to get the full song model once with its relations.
        var songDb = songRepo.GetById(song.Id);
        if (songDb == null)
        {
            ClearSingleSongStats();
            return;
        }

        // Use the song's own PlayHistory if it's reliably populated.
        // This is VASTLY more performant than scanning all events in the database.
        var songEvents = songDb.PlayHistory.ToList().AsReadOnly();

        if (songEvents.Count==0)
        {
            ClearSingleSongStats();
            return;
        }

        // --- Calling ALL Single-Song Stat Methods ---

        // FROM TopStats (or original methods)
        SongPlayTypeDistribution = TopStats.GetPlayTypeDistribution(songEvents).ToObservableCollection();
        SongPlayDistributionByHour = TopStats.GetPlayDistributionByHour(songEvents).ToObservableCollection();
        SongBingeFactor = TopStats.GetBingeFactor(songEvents, song.Id);
        SongAverageListenThrough = TopStats.GetAverageListenThroughPercent(songEvents, song.DurationInSeconds);

        // FROM ChartSpecificStats
        SongPlayHistoryOverTime = ChartSpecificStats.GetSongPlayHistoryOverTime(songEvents).ToObservableCollection();
        SongDropOffPoints = ChartSpecificStats.GetSongDropOffPoints(songEvents).ToObservableCollection();
        SongWeeklyOHLC = ChartSpecificStats.GetSongWeeklyOHLC(songEvents).ToObservableCollection();

        // FROM NEW METHODS in Part 1 (previous response)
        // Note: You'll need to add these methods to either TopStats or ChartSpecificStats
        SongListeningStreak = TopStats.GetListeningStreak(songEvents);
        //SongEvergreenScore = TopStats.GetEvergreenScore(songEvents);
        //SongWeekdayVsWeekend = TopStats.GetWeekdayVsWeekendDistribution(songEvents).ToObservableCollection();
    }

    /// <summary>
    /// Helper method to clear all properties related to a single song's stats.
    /// </summary>
    private void ClearSingleSongStats()
    {
        SongPlayTypeDistribution = null;
        SongPlayDistributionByHour = null;
        SongPlayHistoryOverTime = null;
        SongDropOffPoints = null;
        SongWeeklyOHLC = null;
        SongBingeFactor = null;
        SongAverageListenThrough = null;

        // Clear the new properties too
        SongListeningStreak = null;
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






    [RelayCommand]
    public void DeleteFolderPath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return;
        _logger.LogInformation("Requesting to delete folder path: {Path}", path);
        FolderPaths.Remove(path);
        _folderMgtService.RemoveFolderFromWatchListAsync(path);
        _settingsService.UserMusicFoldersPreference.Remove(path);
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
        _logger.LogInformation("evt '{SongTitle}' updated with new rating: {NewRating}", songModel.Title, newRating);

        _stateService.SetCurrentSong(song);
    }
    [RelayCommand]
    public async Task ToggleFavSong(SongModelView songModel)
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

        if (songModel.IsFavorite)
        {
            _= await _lastfmService.LoveTrackAsync(songModel);
        }
        else
        {
            _= await _lastfmService.UnloveTrackAsync(songModel);

        }
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
            realm.Dispose();

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

    private readonly ObservableCollectionExtended<InteractiveChartPoint> _topSkipsList = new();
    private readonly BehaviorSubject<LimiterClause?> _limiterClause;

    public ReadOnlyObservableCollection<InteractiveChartPoint> TopSkipsChartData { get; }

    [ObservableProperty]
    public partial ObservableCollection<DimmerStats> SongPlayTypeDistribution { get; set; }
    
    [ObservableProperty]
    public partial ObservableCollection<DimmerStats> SongPlayDistributionByHour { get; set; }
    
    [ObservableProperty]
    public partial DimmerStats SongBingeFactor { get; set; }
    
    [ObservableProperty]
    public partial DimmerStats SongAverageListenThrough { get; set; }
    
    [ObservableProperty]
    public partial ObservableCollection<DimmerStats> SongPlayHistoryOverTime { get; set; }
    
    [ObservableProperty]
    public partial ObservableCollection<DimmerStats> SongDropOffPoints { get; set; }
    
    [ObservableProperty]
    public partial ObservableCollection<DimmerStats> SongWeeklyOHLC { get; set; }
    
    [ObservableProperty]
    public partial ObservableCollection<DimmerStats> TopSongsLastMonth { get; set; }
    
    [ObservableProperty]
    public partial ObservableCollection<DimmerStats> MostSkippedSongs { get; set; }
    
    [ObservableProperty]
    public partial ObservableCollection<DimmerStats> ArtistsByHighestSkipRate { get; set; }
    
    [ObservableProperty]
    public partial ObservableCollection<DimmerStats> TopBurnoutSongs { get; set; }
    
    [ObservableProperty]
    public partial ObservableCollection<DimmerStats> TopRediscoveredSongs { get; set; }
    
    [ObservableProperty]
    public partial ObservableCollection<DimmerStats> TopArtistsByVariety { get; set; }
    
    [ObservableProperty]
    public partial ObservableCollection<DimmerStats> TopGenresByListeningTime { get; set; }
    
    [ObservableProperty]
    public partial ObservableCollection<DimmerStats> OverallListeningByDayOfWeek { get; set; }
    
    [ObservableProperty]
    public partial ObservableCollection<DimmerStats> DailyListeningVolume { get; set; }
    
    [ObservableProperty]
    public partial ObservableCollection<DimmerStats> DeviceUsageByTopArtists { get; set; }
    
    [ObservableProperty]
    public partial ObservableCollection<DimmerStats> GenrePopularityOverTime { get; set; }
    
    [ObservableProperty]
    public partial ObservableCollection<DimmerStats> DailyListeningTimeRange { get; set; }
    
    [ObservableProperty]
    public partial ObservableCollection<DimmerStats> SongProfileBubbleChart { get; set; }
    
    [ObservableProperty]
    public partial ObservableCollection<DimmerStats> DailyListeningRoutineOHLC { get; set; }


    public async Task LoadSongDataAsync(Progress<LyricsProcessingProgress>? progressReporter, CancellationTokenSource _lyricsCts)
    {
        // Get the list of songs you want to process
        var songsToRefresh = _songSource.Items.AsEnumerable(); // Or your full master list
        var lryServ = IPlatformApplication.Current.Services.GetService<ILyricsMetadataService>();
        // --- Call our static, background-safe method ---
        await SongDataProcessor.ProcessLyricsAsync(songsToRefresh, lryServ, progressReporter, _lyricsCts.Token);
    }

    public record QueryComponents(
        Func<SongModelView, bool> Predicate,
        IComparer<SongModelView> Comparer,
        LimiterClause? Limiter
    );


    private readonly SourceList<DuplicateSetViewModel> _duplicateSource = new();

    // 2. THE DESTINATION: This is the read-only collection for the UI.
    private readonly ReadOnlyObservableCollection<DuplicateSetViewModel> _duplicateSets;

    // 3. THE PUBLIC PROPERTY: The UI will bind to this.
    public ReadOnlyObservableCollection<DuplicateSetViewModel> DuplicateSets => _duplicateSets;

    public bool HasDuplicates => _duplicateSets.Any();

    [ObservableProperty]
    public partial bool IsFindingDuplicates { get; set; }
    [RelayCommand]
    private async Task FindDuplicatesAsync()
    {
        IsFindingDuplicates = true;

        // We can clear the source directly. DynamicData will update the UI.
        _duplicateSource.Clear();

        try
        {
            // Offload the finding of data to a background thread so the UI never freezes.
            var results = await Task.Run(() => _duplicateFinderService.FindDuplicates());

            if (results.Any())
            {
                // THIS IS THE DYNAMIC DATA FIX:
                // Use .Edit() to perform a highly efficient bulk update.
                // This replaces the slow foreach loop and prevents the UI from freezing.
                // It calculates all the changes and sends ONE notification to the UI.
                _duplicateSource.Edit(updater =>
                {
                    updater.Clear();
                    updater.AddRange(results);
                });
            }
            else
            {
                // Optionally show a "No duplicates found" message
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while finding duplicates.");
        }
        finally
        {
            IsFindingDuplicates = false;
        }
    }
    [RelayCommand]
    private async Task ApplyDuplicateActionsAsync()
    {
        var itemsToDelete = DuplicateSets
            .SelectMany(set => set.Items)
            .Where(item => item.Action == DuplicateAction.Delete)
            .ToList();

        if (!itemsToDelete.Any())
            return;

        // ... (Confirmation dialog logic here) ...

        var deletedCount = await _duplicateFinderService.ResolveDuplicatesAsync(itemsToDelete);

        // --- Clean up the UI using Dynamic Data ---

        // 1. Remove the deleted songs from the main song source list
        var idsToDelete = itemsToDelete.Select(i => i.Song.Id).ToHashSet();
        _songSource.RemoveMany(_songSource.Items.Where(s => idsToDelete.Contains(s.Id)));

        // 2. Identify which duplicate sets are now fully resolved
        var resolvedSets = DuplicateSets
            .Where(set => set.Items.All(item => item.Action == DuplicateAction.Keep || item.Action == DuplicateAction.Ignore))
            .ToList();

        // 3. Remove these resolved sets from the duplicate source list.
        //    DynamicData will automatically remove them from the UI.
        _duplicateSource.RemoveMany(resolvedSets);

        _logger.LogInformation("Successfully resolved duplicates, deleting {Count} items.", deletedCount);
    }


    [ObservableProperty]
    public partial bool IsCheckingFilePresence { get; set; }

    // --- Add the new command ---
    [RelayCommand]
    private async Task ValidateLibraryAsync()
    {
        if (IsCheckingFilePresence)
            return;

        IsCheckingFilePresence = true;
        _logger.LogInformation("Starting library validation...");
        // Optionally show a status message to the user

        try
        {
            // 1. Run the service on a background thread to keep the UI responsive.
            var validationResult = await Task.Run(() => _duplicateFinderService.ValidateFilePresenceAsync(_mapper.Map<List<SongModelView>>( songRepo.GetAll())));

            if (validationResult.MissingCount == 0)
            {
                _logger.LogInformation("Library validation complete. No missing files found.");
                // Show a "Library is clean!" message
                return;
            }

            _logger.LogInformation("Found {Count} songs with missing files. Removing from UI and database.", validationResult.MissingCount);

            // 2. Get the IDs of the songs to remove. A HashSet is fastest for lookups.
            var missingIds = validationResult.MissingSongs.Select(s => s.Id).ToHashSet();

            // 3. Find the corresponding items currently in our UI's SourceList.
            var itemsInUiToRemove = _songSource.Items.Where(s => missingIds.Contains(s.Id)).ToList();

            // 4. Use the high-performance RemoveMany to update the UI just once.
            _songSource.RemoveMany(itemsInUiToRemove);

            // 5. CRITICAL: Clean up the database as well.
            await _duplicateFinderService.RemoveSongsFromDbAsync(missingIds);

            // Show a final "Cleanup complete" message
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during library validation.");
            // Show an error message to the user
        }
        finally
        {
            IsCheckingFilePresence = false;
        }
    }

    [RelayCommand]
    private async Task ValidateSongAsync(SongModelView song)
    {
        if (IsCheckingFilePresence)
            return;

        IsCheckingFilePresence = true;
        _logger.LogInformation("Starting library validation...");
        // Optionally show a status message to the user

        try
        {
            // 1. Run the service on a background thread to keep the UI responsive.
            var listSong = new List<SongModelView>();
            listSong.Add(song);
            var validationResult = await Task.Run(() => _duplicateFinderService.ValidateFilePresenceAsync(
                listSong));
            if (validationResult.MissingCount == 0)
            {
                _logger.LogInformation("Library validation complete. No missing files found.");
                // Show a "Library is clean!" message
                return;
            }

            _logger.LogInformation("Found {Count} songs with missing files. Removing from UI and database.", validationResult.MissingCount);

            // 2. Get the IDs of the songs to remove. A HashSet is fastest for lookups.
            var missingIds = validationResult.MissingSongs.Select(s => s.Id).ToHashSet();

            // 3. Find the corresponding items currently in our UI's SourceList.
            var itemsInUiToRemove = _songSource.Items.Where(s => missingIds.Contains(s.Id)).ToList();

            // 4. Use the high-performance RemoveMany to update the UI just once.
            _songSource.RemoveMany(itemsInUiToRemove);

            // 5. CRITICAL: Clean up the database as well.
            await _duplicateFinderService.RemoveSongsFromDbAsync(missingIds);

            // Show a final "Cleanup complete" message
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during library validation.");
            // Show an error message to the user
        }
        finally
        {
            IsCheckingFilePresence = false;
        }
    }






    [ObservableProperty]
    public partial string LyricsSearchQuery { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasLyricsSearchResults))]
    public partial ObservableCollection<LrcLibSearchResult> LyricsSearchResults { get; set; } = new();

    public bool HasLyricsSearchResults => LyricsSearchResults.Any();

    [ObservableProperty]
    public partial bool IsLyricsSearchBusy {get; set;}

    [ObservableProperty]
    public partial bool IsReconcilingLibrary { get; set;}


    [RelayCommand]
    private async Task SearchLyricsAsync()
    {
        if (SelectedSong == null)
            return;

        IsLyricsSearchBusy = true;
        LyricsSearchResults.Clear(); // Clear old results

        try
        {

            ILyricsMetadataService _lyricsMetadataService = IPlatformApplication.Current!.Services.GetService<ILyricsMetadataService>()!;
            // Use the manual search from the service. Use the query if provided, otherwise the song's tags.
            string query = string.IsNullOrWhiteSpace(LyricsSearchQuery) ? SelectedSong.Title : LyricsSearchQuery;
            var results = await _lyricsMetadataService.SearchOnlineManualParamsAsync(query, SelectedSong.ArtistName, SelectedSong.AlbumName);

            foreach (var result in results)
            {
                LyricsSearchResults.Add(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search for lyrics online.");
            // Optionally show a user-facing error message

            await Shell.Current.DisplayAlert("Error", "Failed to search for lyrics. Please try again later.", "OK");
        }
        finally
        {
            IsLyricsSearchBusy = false;
        }
    }

    [RelayCommand]
    private async Task SelectLyricsAsync(LrcLibSearchResult? selectedResult)
    {
        if (SelectedSong == null || selectedResult == null || string.IsNullOrWhiteSpace(selectedResult.SyncedLyrics))
        {
            return;
        }

        try
        {
            // Step 1: Tell the real-time flow to use these lyrics NOW.
            _lyricsMgtFlow.LoadLyrics(selectedResult.SyncedLyrics);

            // Step 2: Tell the metadata service to SAVE these lyrics for the future.
            // The service needs a LyricsInfo object, so we create one.
            var lyricsInfo = new LyricsInfo();
            lyricsInfo.Parse(selectedResult.SyncedLyrics);
            var _lyricsMetadataService = IPlatformApplication.Current.Services.GetService<ILyricsMetadataService>();

            await _lyricsMetadataService.SaveLyricsForSongAsync(SelectedSong, selectedResult.SyncedLyrics, lyricsInfo);

            // Step 3: Update the local ViewModel state so the UI reacts instantly.
            SelectedSong.SyncLyrics = selectedResult.SyncedLyrics;
            SelectedSong.HasLyrics = true;
            SelectedSong.HasSyncedLyrics = true;

            // Step 4: Clean up the UI
            LyricsSearchResults.Clear(); // Hide the search results
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to select and save lyrics.");
        }
    }

    [RelayCommand]
    private async Task UnlinkLyricsAsync()
    {
        if (SelectedSong == null)
            return;

        // Optional: Add a confirmation dialog here!

        bool confirm = await Shell.Current.DisplayAlert(
            "Unlink Lyrics",
            "Are you sure you want to unlink the lyrics from this song? This will remove all synced lyrics.",
            "Yes, Unlink",
            "Cancel"
        );

        if (!confirm)
            return; // User cancelled the unlinking

        var _lyricsMetadataService = IPlatformApplication.Current.Services.GetService<ILyricsMetadataService>();

        // Clear the lyrics from the song object
        var emptyLyricsInfo = new LyricsInfo(); // An empty object to clear data
        await  _lyricsMetadataService.SaveLyricsForSongAsync(SelectedSong, string.Empty, emptyLyricsInfo);

        // Update the local ViewModel state
        SelectedSong.SyncLyrics = string.Empty;
        SelectedSong.UnSyncLyrics = string.Empty;
        SelectedSong.HasLyrics = false;
        SelectedSong.HasSyncedLyrics = false;

        // Tell the flow to clear its current state
        _lyricsMgtFlow.LoadLyrics(string.Empty);
    }

    [RelayCommand]
    private async Task ReconcileLibraryAsync()
    {
        if (IsReconcilingLibrary)
            return;

        IsReconcilingLibrary = true;
        _logger.LogInformation("Starting library reconciliation...");
        // Show a status message to the user

        try
        {
            // 1. Run the service on a background thread. Pass the current UI song list.
            var result = await Task.Run(() => _duplicateFinderService.ReconcileLibraryAsync(_songSource.Items.ToList()));

            if (result.MigratedCount == 0 && result.UnresolvedCount == 0)
            {
                _logger.LogInformation("Reconciliation complete. Library is already in a perfect state.");
                return;
            }

            // 2. Prepare the changeset for the UI update.
            // We need a list of all items to remove from the source list.
            var songsToRemove = new List<SongModelView>();

            // Add all the truly missing songs.
            songsToRemove.AddRange(result.UnresolvedMissingSongs);

            // Add the OLD version of the migrated songs.
            songsToRemove.AddRange(result.MigratedSongs.Select(m => m.From));

            // We also need to remove the UN-UPDATED version of the replacement songs,
            // because we will add the UPDATED version back in.
            songsToRemove.AddRange(result.MigratedSongs.Select(m => m.To));

            // This is the list of songs with fresh, migrated data to add back.
            var songsToAdd = result.MigratedSongs.Select(m => m.To).ToList();

            // 3. Use Dynamic Data's .Edit() for a single, high-performance UI update.
            _songSource.Edit(updater =>
            {
                updater.Remove(songsToRemove);
                updater.AddRange(songsToAdd); // Add the updated versions back.
            });

            // Show a final "Cleanup complete" message to the user
            _logger.LogInformation("UI updated. Removed {RemoveCount} entries, added back {AddCount} updated entries.", songsToRemove.Count, songsToAdd.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during library reconciliation.");
            // Show an error message to the user
        }
        finally
        {
            IsReconcilingLibrary = false;
        }
    }


    /// <summary>
    /// Quickly assigns a single song to an existing artist.
    /// This is a lightweight "move" operation.
    /// </summary>
    /// <param name="context">A tuple containing the Song to change and the target Artist.</param>
    [RelayCommand]
    private async Task AssignSongToArtistAsync((SongModelView Song, ArtistModelView TargetArtist) context)
    {
        if (context.Song == null || context.TargetArtist == null)
            return;

        _logger.LogInformation("Assigning song '{SongTitle}' to artist '{ArtistName}'", context.Song.Title, context.TargetArtist.Name);

        // Update the database
        songRepo.Update(context.Song.Id, songInDb =>
        {
            var artistInDb = artistRepo.GetById(context.TargetArtist.Id);
            if (artistInDb == null)
                return;

            // This is a full replacement of the artist list
            songInDb.ArtistToSong.Clear();
            songInDb.ArtistToSong.Add(artistInDb);
            songInDb.Artist = artistInDb;
            songInDb.ArtistName = artistInDb.Name;
        });

        // --- Refresh UI ---
        // Fetch the updated song and replace it in the main source list
        var updatedSong = _mapper.Map<SongModelView>(songRepo.GetById(context.Song.Id));
        _songSource.Edit(updater =>
        {
            updater.Remove(context.Song);
            updater.Add(updatedSong);
        });
    }

    /// <summary>
    /// Creates a new artist in the database and assigns the selected song(s) to it.
    /// Useful for quickly categorizing untagged files.
    /// </summary>
    /// <param name="songsToAssign">The list of songs to assign to the new artist.</param>
    [RelayCommand]
    private async Task CreateArtistAndAssignSongsAsync(IList<SongModelView> songsToAssign)
    {
        if (songsToAssign == null || !songsToAssign.Any())
            return;

        // --- Step 1: Get the new artist name from the user ---
        // This is a perfect use case for a simple popup input dialog.
        string? newArtistName = await Shell.Current.DisplayPromptAsync(
            "Create New Artist",
            "Enter the name for the new artist:");

        if (string.IsNullOrWhiteSpace(newArtistName))
            return;

        _logger.LogInformation("Creating new artist '{ArtistName}' and assigning {Count} songs.", newArtistName, songsToAssign.Count);

        // --- Step 2: Create the new artist in the database ---
        var newArtist = new ArtistModel { Name = newArtistName };
        var createdArtist = artistRepo.Create(newArtist); // This returns the managed object with a new ID

        // --- Step 3: Loop through songs and update them ---
        var songIds = songsToAssign.Select(s => s.Id).ToList();
        foreach (var songId in songIds)
        {
            songRepo.Update(songId, songInDb =>
            {
                songInDb.ArtistToSong.Clear();
                songInDb.ArtistToSong.Add(createdArtist);
                songInDb.Artist = createdArtist;
                songInDb.ArtistName = createdArtist.Name;
            });
        }

        // --- Step 4: Refresh UI (High-performance version) ---
        var updatedSongs = _mapper.Map<List<SongModelView>>(songRepo.Query(s => songIds.Contains(s.Id)));
        _songSource.Edit(updater =>
        {
            updater.RemoveMany(songsToAssign);
            updater.AddRange(updatedSongs);
        });
    }

    // =================================================================
    // ALBUM LINKING COMMANDS
    // =================================================================

    /// <summary>
    /// Merges multiple songs into a single album, creating the album if it doesn't exist.
    /// This is the core command for "compiling" an album from loose tracks.
    /// </summary>
    /// <param name="songsToAlbumize">The list of songs to group into an album.</param>
    [RelayCommand]
    private async Task GroupSongsIntoAlbumAsync(IList<SongModelView> songsToAlbumize)
    {
        if (songsToAlbumize == null || !songsToAlbumize.Any())
            return;

        // --- Step 1: Get Album Name and Album Artist from User ---
        string? albumName = await Shell.Current.DisplayPromptAsync("Group into Album", "Enter the album name:");
        if (string.IsNullOrWhiteSpace(albumName))
            return;

        // Use the artist of the first song as a default suggestion
        string? albumArtistName = await Shell.Current.DisplayPromptAsync("Group into Album", "Enter the album artist name:", initialValue: songsToAlbumize.First().ArtistName);
        if (string.IsNullOrWhiteSpace(albumArtistName))
            return;

        // --- Step 2: Find or Create the Album and Album Artist ---
        var albumArtist = artistRepo.Query(a => a.Name == albumArtistName).FirstOrDefault() ?? artistRepo.Create(new ArtistModel { Name = albumArtistName });
        var album = albumRepo.Query(a => a.Name == albumName).FirstOrDefault() ?? albumRepo.Create(new AlbumModel { Name = albumName, Artist = albumArtist });

        // --- Step 3: Update all selected songs ---
        var songIds = songsToAlbumize.Select(s => s.Id).ToList();
        songRepo.UpdateMany(songIds, songInDb => // Assuming IRepository has an UpdateMany
        {
            songInDb.Album = album;
            songInDb.AlbumName = album.Name;
            songInDb.OtherArtistsName = albumArtist.Name; // Set Album Artist
        });

        // --- Step 4: Refresh UI ---
        var updatedSongs = _mapper.Map<List<SongModelView>>(songRepo.Query(s => songIds.Contains(s.Id)));
        _songSource.Edit(updater =>
        {
            updater.RemoveMany(songsToAlbumize);
            updater.AddRange(updatedSongs);
        });
    }

    // =================================================================
    // GENRE AND TAGGING COMMANDS
    // =================================================================

    /// <summary>
    /// Applies a single genre to a batch of selected songs.
    /// </summary>
    /// <param name="songsToGenre">The songs to apply the genre to.</param>
    [RelayCommand]
    private async Task ApplyGenreToSongsAsync(IList<SongModelView> songsToGenre)
    {
        if (songsToGenre == null || songsToGenre.Count<0)
            return;

        string? genreName = await Shell.Current.DisplayPromptAsync("Apply Genre", "Enter the genre to apply:");
        if (string.IsNullOrWhiteSpace(genreName))
            return;

        var genre = genreRepo.Query(g => g.Name == genreName).FirstOrDefault() ?? genreRepo.Create(new GenreModel { Name = genreName });

        var songIds = songsToGenre.Select(s => s.Id).ToList();
        songRepo.UpdateMany(songIds, songInDb =>
        {
            songInDb.Genre = genre;
            songInDb.GenreName = genre.Name;
        });

        // Refresh UI
        var updatedSongs = _mapper.Map<List<SongModelView>>(songRepo.Query(s => songIds.Contains(s.Id)));
        _songSource.Edit(updater =>
        {
            updater.RemoveMany(songsToGenre);
            updater.AddRange(updatedSongs);
        });
    }

    /// <summary>
    /// Applies one or more tags (comma-separated) to a batch of selected songs.
    /// </summary>
    /// <param name="songsToTag">The songs to apply tags to.</param>
    [RelayCommand]
    private async Task ApplyTagsToSongsAsync(IList<SongModelView> songsToTag)
    {
        if (songsToTag == null || !songsToTag.Any())
            return;

        string? tagsInput = await Shell.Current.DisplayPromptAsync("Apply Tags", "Enter tags, separated by commas:");
        if (string.IsNullOrWhiteSpace(tagsInput))
            return;

        var tagNames = tagsInput.Split(',', ';').Select(t => t.Trim()).Where(t => !string.IsNullOrEmpty(t)).ToList();
        var songIds = songsToTag.Select(s => s.Id).ToList();

        songRepo.UpdateMany(songIds, songInDb =>
        {
            foreach (var tagName in tagNames)
            {
                // Avoid adding duplicate tags
                if (!songInDb.Tags.Any(t => t.Name.Equals(tagName, StringComparison.OrdinalIgnoreCase)))
                {
                    // For simplicity, we assume tags are not shared objects.
                    // If they were, you would find-or-create them like genres.
                    songInDb.Tags.Add(new TagModel { Name = tagName });
                }
            }
        });

        // Refresh UI
        var updatedSongs = _mapper.Map<List<SongModelView>>(songRepo.Query(s => songIds.Contains(s.Id)));
        _songSource.Edit(updater =>
        {
            updater.RemoveMany(songsToTag);
            updater.AddRange(updatedSongs);
        });
    }



    /// <summary>
    /// A "power method" that adds a new filter clause to the current search query.
    /// </summary>
    /// <param name="clause">The TQL clause to add, e.g., "fav:true" or "year:>2000".</param>
    public void AddFilterToSearch(string clause)
    {
        var currentQuery = _searchQuerySubject.Value.Trim();
        if (string.IsNullOrWhiteSpace(currentQuery))
        {
            // If the search is empty, the new clause is the whole query.
            _searchQuerySubject.OnNext(clause);
        }
        else
        {
            // Otherwise, intelligently add it with an "and".
            _searchQuerySubject.OnNext($"{currentQuery} and {clause}");
        }
    }

    /// <summary>
    /// A "power method" that completely replaces the sort directives in the current query.
    /// </summary>
    /// <param name="sortClause">The TQL sort clause, e.g., "asc artist" or "desc year".</param>
    public void SetSortForSearch(string sortClause)
    {
        var currentQuery = _searchQuerySubject.Value;
        // This is a simplified regex to remove existing sort/limit directives.
        // Your MetaParser is the source of truth, but this is good for a quick UI-driven change.
        var queryWithoutDirectives = Regex.Replace(currentQuery, @"(asc|desc|random|shuffle|first|last)\s*\w*\s*", "", RegexOptions.IgnoreCase).Trim();

        _searchQuerySubject.OnNext($"{queryWithoutDirectives} {sortClause}");
    }


    public ObservableCollection<ActiveFilterViewModel> ActiveFilters { get; } = new();

    private void RebuildAndExecuteQuery()
    {
        var clauses = new List<string>();
        LogicalOperator nextJoiner = LogicalOperator.And; // Default joiner

        foreach (var component in UIQueryComponents)
        {
            if (component is ActiveFilterViewModel filter)
            {
                // If the last thing added was a filter, we need to add the joiner first.
                if (clauses.Any())
                {
                    clauses.Add(nextJoiner.ToString().ToLower());
                }
                clauses.Add(filter.TqlClause);
            }
            else if (component is LogicalJoinerViewModel joiner)
            {
                // Store the joiner for the NEXT filter.
                nextJoiner = joiner.Operator;
            }
        }

        var fullQueryString = string.Join(" ", clauses);

        // Push the newly built string into your existing TQL engine pipeline.
        // The rest of your app (parser, evaluator) works exactly as before!
        _searchQuerySubject.OnNext(fullQueryString);
    }
    [ObservableProperty]
    public partial bool IsFirmSearchEnabled { get; set; }
    
    /// <summary>
    /// The main command for adding a new filter. This is the heart of the Lego system.
    /// It's smart and knows how to ask the user for input based on the field type.
    /// </summary>
    /// <param name="tqlField">The TQL field to add (e.g., "title", "fav", "year").</param>
    [RelayCommand]
    private async Task AddFilterAsync(string tqlField)
    {
        if (string.IsNullOrWhiteSpace(tqlField) || !FieldRegistry.FieldsByAlias.TryGetValue(tqlField, out var fieldDef))
        {
            return;
        }

        // Uniqueness Check: If a field can only appear once (like 'fav'),
        // and it's already in our active list, do nothing.
        if (fieldDef.Type == FieldType.Boolean && ActiveFilters.Any(f => f.Field == tqlField))
        {
            _logger.LogWarning("Cannot add duplicate unique filter: {Field}", tqlField);
            return;
        }

        string? tqlClause = null;
        string? displayText = null;

        // Ask for user input based on the field type
        switch (fieldDef.Type)
        {
            case FieldType.Text:
                string? value = await Shell.Current.DisplayPromptAsync($"Filter by {fieldDef.PrimaryName}", "Enter the text to search for:");
                if (!string.IsNullOrWhiteSpace(value))
                {
                    // Quote the value if it contains spaces
                    string formattedValue = value.Contains(' ') ? $"\"{value}\"" : value;
                    tqlClause = $"{tqlField}:{formattedValue}";
                    displayText = $"{fieldDef.PrimaryName}: {value}";
                }
                break;

            case FieldType.Boolean:
                // For booleans, we just add the "true" state. The UI can have a toggle for negation.
                tqlClause = $"{tqlField}:true";
                displayText = fieldDef.Description;
                break;

            case FieldType.Numeric:
            case FieldType.Duration:
                // This could be expanded with a more complex UI for operators (>, <, etc.)
                string? numValue = await Shell.Current.DisplayPromptAsync($"Filter by {fieldDef.PrimaryName}", "Enter the value (e.g., >2000 or 3:30):");
                if (!string.IsNullOrWhiteSpace(numValue))
                {
                    tqlClause = $"{tqlField}:{numValue}";
                    displayText = $"{fieldDef.PrimaryName} {numValue}";
                }
                break;

            case FieldType.Date:
                // Here you would show a calendar control or a set of predefined ranges.
                // For simplicity, we'll use a prompt.
                string? dateValue = await Shell.Current.DisplayPromptAsync($"Filter by {fieldDef.PrimaryName}", "Enter a date or range (e.g., today, last month, 2023-12-25):");
                if (!string.IsNullOrWhiteSpace(dateValue))
                {
                    tqlClause = $"{tqlField}:{dateValue}";
                    displayText = $"{fieldDef.PrimaryName}: {dateValue}";
                }
                break;
        }

        // If the user provided input and we created a clause, add the new Lego brick!
        if (tqlClause != null && displayText != null)
        {
            ActiveFilters.Add(new ActiveFilterViewModel(tqlField, displayText, tqlClause, RemoveFilter));
        }
    }

    /// <summary>
    /// This is the callback method that the RemoveCommand on each chip will call.
    /// </summary>
    private void RemoveFilter(ActiveFilterViewModel filterToRemove)
    {
        if (filterToRemove != null)
        {
            ActiveFilters.Remove(filterToRemove);
        }
    }

    [ObservableProperty] public partial bool IsCreatingSegment { get; set; }
    [ObservableProperty] public partial double NewSegmentStart {get;set;}
    [ObservableProperty] public partial double NewSegmentEnd {get;set;}
    [ObservableProperty] public partial string? NewSegmentName {get;set;}

    [RelayCommand]
    private void BeginCreateSegment()
    {
        if (CurrentPlayingSongView == null)
            return;
        NewSegmentStart = CurrentTrackPositionSeconds;
        NewSegmentEnd = NewSegmentStart + 30;
        NewSegmentName = $"{CurrentPlayingSongView.Title} (Clip)";
        IsCreatingSegment = true;
    }

    [RelayCommand] private void SetSegmentStartFromCurrent() => NewSegmentStart = CurrentTrackPositionSeconds;
    [RelayCommand] private void SetSegmentEndFromCurrent() => NewSegmentEnd = CurrentTrackPositionSeconds;
    [RelayCommand] private void CancelCreateSegment() => IsCreatingSegment = false;

    [RelayCommand]
    private void SaveNewSegment()
    {
        if (CurrentPlayingSongView == null || string.IsNullOrWhiteSpace(NewSegmentName))
            return;

        var segmentModel = new SongModel
        {
            Id = ObjectId.GenerateNewId(),
            SongType = SongType.Segment,
            Title = NewSegmentName,
            ParentSongId = CurrentPlayingSongView.Id,
            FilePath = CurrentPlayingSongView.FilePath,
            ArtistName = CurrentPlayingSongView.ArtistName,
            AlbumName = CurrentPlayingSongView.AlbumName,
            SegmentStartTime = NewSegmentStart,
            SegmentEndTime = NewSegmentEnd,
            SegmentEndBehavior = SegmentEndBehavior.LoopSegment, // Default to loop
            DurationInSeconds = NewSegmentEnd - NewSegmentStart,
        };

        var createdSegmentModel = songRepo.Create(segmentModel);
        var createdSegmentView = _mapper.Map<SongModelView>(createdSegmentModel);

        _songSource.Add(createdSegmentView);
        IsCreatingSegment = false;
    }
}