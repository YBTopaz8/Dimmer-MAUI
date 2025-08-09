using ATL;

using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.Input;

using Dimmer.Data.ModelView.DimmerSearch;
using Dimmer.Data.ModelView.LibSanityModels;
using Dimmer.Data.ModelView.NewFolder;
using Dimmer.Data.RealmStaticFilters;
using Dimmer.DimmerSearch;
using Dimmer.DimmerSearch.Exceptions;
using Dimmer.DimmerSearch.TQL;
using Dimmer.DimmerSearch.TQL.TQLCommands;
using Dimmer.Interfaces.IDatabase;
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

using Parse.LiveQuery;
//using MoreLinq;
//using MoreLinq.Extensions;

using ReactiveUI;

using Realms;

using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text.RegularExpressions;

using static Dimmer.Data.RealmStaticFilters.MusicPowerUserService;




namespace Dimmer.ViewModel;

public partial class BaseViewModel : ObservableObject, IReactiveObject, IDisposable
{
    private IDuplicateFinderService _duplicateFinderService;
    public BaseViewModel(
       IMapper mapper,
       CommandEvaluator commandEvaluator,
       IAppInitializerService appInitializerService,
       IDimmerLiveStateService dimmerLiveStateService,
       IDimmerAudioService audioServ,
       IDimmerStateService stateService,
       ISettingsService settingsService,
       ILyricsMetadataService lyricsMetadataService,
       SubscriptionManager subsManager,
       LyricsMgtFlow lyricsMgtFlow,
       ICoverArtService coverArtService,
       IFolderMgtService folderMgtService,
       IRepository<SongModel> _songRepo,
       IDeviceConnectivityService deviceConnectivityService,
       IDuplicateFinderService duplicateFinderService,
        ILastfmService _lastfmService,
       IRepository<ArtistModel> artistRepo,
       IRepository<AlbumModel> albumModel,
       IRepository<GenreModel> genreModel,
       IDialogueService dialogueService,
       
       ILogger<BaseViewModel> logger)
    {
        _dialogueService = dialogueService ?? throw new ArgumentNullException(nameof(dialogueService));
        this.lastfmService = _lastfmService ?? throw new ArgumentNullException(nameof(lastfmService));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        this.commandEvaluator=commandEvaluator;
        this.appInitializerService=appInitializerService;
        _dimmerLiveStateService = dimmerLiveStateService;
        _baseAppFlow= IPlatformApplication.Current?.Services.GetService<BaseAppFlow>() ?? throw new ArgumentNullException(nameof(BaseAppFlow));
        _stateService = stateService ?? throw new ArgumentNullException(nameof(stateService));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _subsManager = subsManager ?? new SubscriptionManager();
        _folderMgtService = folderMgtService;
        this.songRepo=_songRepo;
        this.deviceConnectivityService=deviceConnectivityService;
        _lyricsMetadataService= lyricsMetadataService ?? throw new ArgumentNullException(nameof(lyricsMetadataService));
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
        PlaybackManager = new RuleBasedPlaybackManager();


    }


    public async Task InitializeAllVMCoreComponentsAsync()
    {

        var songRep = songRepo;

        var realm = realmFactory.GetRealmInstance();
        var initialSongs = realm.All<SongModel>().ToList().Select(song => song.ToViewModel());
        _songSource.AddRange(initialSongs);
        Debug.WriteLine($"[LOAD] Manually loaded {_songSource.Count} songs into SourceList.");

        _playbackQueueSource.Connect()
    .ObserveOn(RxApp.MainThreadScheduler) // Ensure UI updates are on the main thread
    .Bind(out _playbackQueue) // Bind the results to our public property
    .Subscribe()
    .DisposeWith(Disposables);


        _searchQuerySubject
            .Throttle(TimeSpan.FromMilliseconds(380), RxApp.TaskpoolScheduler)
           .Select(query =>
           {
               if (string.IsNullOrWhiteSpace(query))
               {
                   return (Components: new QueryComponents(p => true, new SongModelViewComparer(null), null), Command: (IQueryNode?)null, ErrorMessage: (string?)null);
               }

               try
               {
                   var tqlQuery = NaturalLanguageProcessor.Process(query);
                   var orchestrator = new MetaParser(tqlQuery);

                   var components = new QueryComponents(
                      orchestrator.CreateMasterPredicate(),
                      orchestrator.CreateSortComparer(),
                      orchestrator.CreateLimiterClause()
                   );

                   return (Components: components, Command: orchestrator.ParsedCommand, ErrorMessage: (string?)null);
               }
               catch (ParsingException ex)
               {
                   TQLUserSearchErrorMessage = ex.Message;
                   // Example: ex.Message might be "Unknown field 'artst'."
                   var match = Regex.Match(ex.Message, @"Unknown field '(\w+)'");
                   if (match.Success)
                   {
                       InvalidField = match.Groups[1].Value;
                       NewFieldSuggestion = QueryValidator.SuggestCorrectField(InvalidField);
                       if (NewFieldSuggestion != null)
                       {
                           TQLUserSearchErrorMessage += $"\nDid you mean '{NewFieldSuggestion}'?";
                       }
                   }

                   _logger.LogWarning(ex, "User search query failed to parse: {Query}", query);
                   return (Components: (QueryComponents?)null, Command: (IQueryNode?)null, ErrorMessage: ex.Message);
               }
               catch (Exception ex)
               {
                   _logger.LogError(ex, "An unexpected error occurred during search parsing for query: {Query}", query);
                   return (Components: (QueryComponents?)null, Command: (IQueryNode?)null, ErrorMessage: "An unexpected error occurred.");
               }
           })
    .ObserveOn(RxApp.MainThreadScheduler)
    .Subscribe(result =>
    {

        DebugMessage = result.ErrorMessage ?? string.Empty;

        if (result.Components is not null)
        {
            _filterPredicate.OnNext(result.Components.Predicate ?? (song => true));
            _sortComparer.OnNext(result.Components.Comparer ?? new SongModelViewComparer(null));
            _limiterClause.OnNext(result.Components.Limiter);
        }

        if (result.Command is not null)
        {
            this.commandEvaluator.Execute(result.Command, searchResultsHolder.Items);

            DebugMessage = $"Executed command.";
        }
    },
    ex => _logger.LogError(ex, "FATAL: Search control pipeline has crashed."))
    .DisposeWith(Disposables);

        var controlPipeline = 
            Observable.CombineLatest(
      _filterPredicate,
      _sortComparer,
      _limiterClause,
      (predicate, comparer, limiter) => new { predicate, comparer, limiter }
  );






        // 2. The pipeline now reads from the master list and populates the results holder.
        _songSource.Connect()
            .Filter(song => !song.IsHidden) // Filter out hidden songs
            .ToCollection()
            .ObserveOn(RxApp.TaskpoolScheduler) // Heavy lifting on background thread
            .CombineLatest(controlPipeline, (songs, controls) => new { songs, controls })
            .Select(data =>
            {
                var predicate = data.controls.predicate;
                var comparer = data.controls.comparer;
                var limiter = data.controls.limiter;

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
                    searchResultsHolder.Edit(updater =>
                    {

                        updater.Clear();
                        updater.AddRange(newList);
                    });
                },
                ex => _logger.LogError(ex, "FATAL: Data calculation pipeline crashed!"))
            .DisposeWith(Disposables);

        searchResultsHolder.Connect()
            .ObserveOn(RxApp.MainThreadScheduler) // Binding should always be on the UI thread
            .Bind(out _searchResults)
            .Subscribe(
                cs =>
                {

                },
                ex => _logger.LogError(ex, "FATAL: Data binding pipeline crashed!"))
            .DisposeWith(Disposables);




    //    _duplicateSource.Connect()
    //.Sort(SortExpressionComparer<DuplicateSetViewModel>.Ascending(d => d.Title)) // Keep the list sorted
    //.ObserveOn(RxApp.MainThreadScheduler) // Ensure UI updates are on the main thread
    //.Bind(out _duplicateSets) // Bind the results to our public property
    //.Subscribe() // Activate the pipeline
    //.DisposeWith(Disposables);




        string rql = "TRUEPREDICATE SORT(EventDate DESC)";

        // 2. Execute the query and use the LINQ FirstOrDefault() to get just the top one.
        var evtt = realm.All<DimmerPlayEvent>().Filter(rql).ToList();
        if (evtt is null || evtt.Count == 0)
        {
            // No events found, set default values
            CurrentPlayingSongView = new SongModelView();
        }
        var evt = evtt?.FirstOrDefault();
        if (evt?.SongId is ObjectId songId && songId != ObjectId.Empty)
        {

            var song = songRepo.GetById(songId);
            if (song is not null)
            {
            CurrentPlayingSongView = song.ToModelView();

            }

        }
        else
        {
            // Handle the case where there's no valid event or the event has no valid song.
            CurrentPlayingSongView = new();
        }

        SearchSongSB_TextChanged("random");

        FolderPaths = _settingsService.UserMusicFoldersPreference.ToObservableCollection();

        lastfmService.IsAuthenticatedChanged
           .ObserveOn(RxApp.MainThreadScheduler) // Ensure UI updates on the main thread
           .Subscribe(isAuthenticated =>
           {
               IsLastfmAuthenticated = isAuthenticated;
               LastfmUsername = lastfmService.AuthenticatedUser ?? "Not Logged In";
               if (isAuthenticated)
               {


                   if (string.IsNullOrEmpty(UserLocal.Username))
                   {
                       if ((!string.IsNullOrEmpty(lastfmService.AuthenticatedUser)))
                       {
                           UserLocal.Username=lastfmService.AuthenticatedUser;
                           var db = realmFactory.GetRealmInstance();
                           db.Write(() =>
                           {

                               var usrs = db.All<UserModel>().ToList();
                               if (usrs is not null && usrs.Count>0)
                               {
                                   UserModel usr = usrs.First();
                                   usr.UserName=lastfmService.AuthenticatedUser;




                                   db.Add(usr, true);
                               }

                           });
                       }
                   }
               }
           })
           .DisposeWith(Disposables); // Assuming > have a reactive disposables manager
        this.lastfmService.Start();



        MyDeviceId = LoadOrGenerateDeviceId();


        

        SubscribeToStateServiceEvents();
        SubscribeToAudioServiceEvents();
        SubscribeToLyricsFlow();
        await Task.WhenAll( EnsureAllCoverArtCachedForSongsAsync(),LoadAllSongsEventsASync());
        return;
    }

    public RuleBasedPlaybackManager PlaybackManager { get; }
    // You'll want a property to bind a loading indicator to in >r UI
    // [ObservableProperty]
    // private bool _isLoadingSongs;

    SourceList<SongModelView> searchResultsHolder = new SourceList<SongModelView>();
    public async Task LoadAllSongsEventsASync()
    {
        // Consider having a loading indicator for the UI
        // IsLoadingSongs = true; 
        _logger.LogInformation("Starting to load play events for all songs...");

        try
        {
            // --- 1. Asynchronously fetch data ---
            // Assume >r repository has an async method. If not, > should create one.
            var allEvents = await dimmerPlayEventRepo.GetAllAsync();
            var allSongs = _songSource.Items;

            if (allEvents is null || allEvents.Count == 0 || allSongs is null || allSongs.Count == 0)
            {
                _logger.LogWarning("No events or songs found to process.");
                return;
            }

            // --- 2. Offload the heavy processing to a background thread ---
            // This prevents the UI from freezing while we group and assign events.
            var eventsBySongId = await Task.Run(() =>
            {
                // --- 3. Group events by SongId ONCE. This is the key performance gain. ---
                // This creates a lookup where the key is the SongId and the value is a list of its events.
                // This is dramatically faster than >r original nested loop approach.
                return allEvents
                    .Where(e => e.SongId.HasValue)
                    .GroupBy(e => e.SongId.Value)
                    .ToDictionary(g => g.Key, g => g.Select(ev => new DimmerPlayEventView(ev)).ToObservableCollection());
            });

            // --- 4. Now that the hard work is done, update the songs on the UI thread ---
            foreach (var song in allSongs)
            {
                // This is now a super-fast dictionary lookup
                if (eventsBySongId.TryGetValue(song.Id, out var songEvents))
                {
                    song.PlayEvents = songEvents;
                }
                else
                {
                    song.PlayEvents ??=new();
                    // Ensure the collection is empty if no events were found, avoiding old data
                    song.PlayEvents.Clear();
                }
            }
            _logger.LogInformation("Finished loading play events for {Count} songs.", allSongs.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while loading song events.");
        }
        finally
        {
            // IsLoadingSongs = false;
        }
    }

    private Subject<List<SongModelView>> _searchResultsChangedSubject = new();



    /// <summary>
    /// Sanitizes the play events in the database by removing duplicate "skipped" events
    /// that were logged erroneously within a short time frame for the same song.
    /// </summary>
    /// <returns>A Task representing the asynchronous operation.</returns>
    public async Task SanitizeSkippedEventsAsync()
    {
        _logger.LogInformation("Starting sanitization of skipped play events...");

        try
        {
            var _realm = realmFactory.GetRealmInstance();
            // --- 1. Fetch all "skipped" events ---
            // PlayType 5 corresponds to "Skipped"
            var allSkippedEvents = 
                _realm.All<DimmerPlayEvent>().Where(e => e.PlayType == 5).ToList();

            if (allSkippedEvents is null || allSkippedEvents.Count==0)
            {
                _logger.LogInformation("No skipped events found to sanitize.");
                return;
            }

            // --- 2. Group events by the song they belong to ---
            var skippedEventsBySong = allSkippedEvents
                .Where(e => e.SongId.HasValue )
                .GroupBy(e => e.SongId.Value)
                .ToDictionary(g => g.Key, g => g.OrderBy(e => e.DatePlayed).ToList());

            var eventsToRemove = new List<DimmerPlayEvent>();

            // --- 3. Identify duplicate events within a time threshold ---
            foreach (var songGroup in skippedEventsBySong.Values)
            {
                for (int i = 0; i < songGroup.Count - 1; i++)
                {
                    var currentEvent = songGroup[i];
                    var nextEvent = songGroup[i + 1];

                    // Check if the next event is a duplicate within a 3-minute window
                    if ((nextEvent.DatePlayed - currentEvent.DatePlayed).TotalMinutes < 5)
                    {
                        // Mark the subsequent event for removal
                        eventsToRemove.Add(nextEvent);
                    }
                }
            }

            // --- 4. Remove the identified duplicate events ---
            if (eventsToRemove.Count!=0)
            {
                _logger.LogInformation("Found {Count} duplicate skipped events to remove.", eventsToRemove.Count);
                await _realm.WriteAsync(() =>
                {
                    foreach (var eventToRemove in eventsToRemove)
                    {
                        _realm.Remove(eventToRemove);
                    }
                });
                _logger.LogInformation("Successfully removed duplicate skipped events.");
            }
            else
            {
                _logger.LogInformation("No duplicate skipped events found to remove.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during the sanitization of skipped events.");
        }
    }
    [RelayCommand]
    private async Task RemoteNextTrackAsync()
    {
        if (ControlledDeviceState == null)
            return;
        
    }

    private string _myDeviceId; // A unique ID stored in app settings
    private DeviceState _myDeviceState; // The ParseObject representing this device's state


    private ParseClient ParseClient { get; set; }
    private ParseLiveQueryClient LiveClient { get; set; }
    public string MyDeviceId { get; set; }

    // ==========================================================
    // REMOTE MODE LOGIC (Sends commands, listens for state)
    // ==========================================================
    private void SetupRemoteModeListeners()
    {
        // For simplicity, let's just watch all devices for now.
        // In a real app, the user would select a device to control.
        var stateQuery = new ParseQuery<DeviceState>(ParseClient);
        var stateSub = LiveClient.Subscribe(stateQuery);


    }
    private void SetupPlayerModeListeners()
    {
        // Subscribe to commands targeted at THIS device
        var commandQuery = new ParseQuery<DeviceCommand>(ParseClient)
            .WhereEqualTo("targetDeviceId", _myDeviceId)
            .WhereEqualTo("isHandled", false);

        var commandSub = LiveClient.Subscribe(commandQuery);

        // When a new command is CREATED for us...
        commandSub.Events
            .Where(e => e.EventType == Subscription.Event.Create)
            .Subscribe(e => HandleIncomingDeviceCommand(e.Object));
    }

    private async void HandleIncomingDeviceCommand(DeviceCommand command)
    {
        _logger.LogInformation("Received remote command: {Command}", command.CommandName);

        // Execute the command by calling our existing RelayCommands
        switch (command.CommandName)
        {
            case "PLAY_PAUSE":
                await PlayPauseToggle();
                break;
            case "NEXT":
                await NextTrackAsync();
                break;
            case "PREVIOUS":
                await PreviousTrack();
                break;
                // ... add cases for SEEK, SET_VOLUME, etc. ...
        }

        // Mark the command as handled so we don't process it again
        command.IsHandled = true;
        await command.SaveAsync();
    }

    [ObservableProperty]
    public partial DeviceState? ControlledDeviceState { get; set; }


    private ReadOnlyObservableCollection<DeviceState> _availablePlayers;
    public ReadOnlyObservableCollection<DeviceState> AvailablePlayers => _availablePlayers;

    


    [RelayCommand]
    public async Task OpenFieInFolder()
    {
        var songToView = SelectedSong;
        
        if (songToView is null || string.IsNullOrEmpty(songToView.FilePath))
        {
            await Shell.Current.DisplayAlert("Error", "No song selected or file path is empty.", "OK");
            return;
        }
        try
        {
            var fileUri = new Uri(songToView.FilePath);
            if (fileUri.IsFile)
            {
                // Use Launcher to open the file in its default application
                await Launcher.Default.OpenAsync(fileUri);
            }
            else
            {
                await Shell.Current.DisplayAlert("Error", "The selected song's file path is not valid.", "OK");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open file in folder for song: {SongTitle}", songToView.Title);
            await Shell.Current.DisplayAlert("Error", "Failed to open file in folder. Please check the file path.", "OK");
        }
    }

    private string LoadOrGenerateDeviceId()
    {
        return DeviceInfo.Name + "-" +
               DeviceInfo.Manufacturer + "-" +
               DeviceInfo.Model + "-" +
               DeviceInfo.VersionString + "-" +
               DeviceInfo.Platform.ToString() + "-" +
               DeviceInfo.Idiom.ToString() + "-" +
               DeviceInfo.DeviceType.ToString();    
    }



    public ObservableCollection<IQueryComponentViewModel> UIQueryComponents { get; } = new();
    public void SearchSongSB_TextChanged(string searchText)
    {
        string currentText = CurrentTqlQuery;

        string processedNewText = NaturalLanguageProcessor.Process(searchText);

        // Check for refinement keywords
        if ((searchText.StartsWith("and ", StringComparison.OrdinalIgnoreCase) ||
             searchText.StartsWith("with ", StringComparison.OrdinalIgnoreCase)) &&
             !string.IsNullOrWhiteSpace(currentText))
        {
            // Append to the existing query
            CurrentTqlQuery = $"{currentText} {processedNewText}";
        }
        else
        {
            // Replace the query
            CurrentTqlQuery = processedNewText;
        }


        _searchQuerySubject.OnNext(searchText);


    }
    private BehaviorSubject<string> _searchQuerySubject;

    private BehaviorSubject<Func<SongModelView, bool>> _filterPredicate;
    private BehaviorSubject<IComparer<SongModelView>> _sortComparer;
    //private BehaviorSubject<LimiterClause?> _limiterClause;

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

    private int _playbackQueueIndex = -1;
    [RelayCommand]
    public void BigHold()
    {
        SearchSongSB_TextChanged("Len:<=3:00");
    }

    [RelayCommand]
    public void ResetSearch()
    {
        _searchQuerySubject.OnNext("random");
        CurrentTqlQuery= "random";
    }

    private SourceList<DimmerPlayEventView> _playEventSource = new();
    private CompositeDisposable _disposables = new();
    private IDisposable? _realmSubscription;
    private bool _isDisposed;

    [ObservableProperty]
    public partial SongViewMode CurrentSongViewMode { get; set; } = SongViewMode.DetailedGrid;
    private BehaviorSubject<ValidationResult> _validationResultSubject = new(new(true));
    public IObservable<ValidationResult> ValidationResult => _validationResultSubject;

    private ILyricsMetadataService _lyricsMetadataService;

    public ReadOnlyObservableCollection<SongModelView> SearchResults => _searchResults;
    
    private ReadOnlyObservableCollection<SongModelView> _searchResults;

    private readonly SourceList<SongModelView> _playbackQueueSource = new();
    private ReadOnlyObservableCollection<SongModelView> _playbackQueue; 
    public ReadOnlyObservableCollection<SongModelView> PlaybackQueue => _playbackQueue; // This is the public property for binding
    protected CompositeDisposable Disposables { get; } = new CompositeDisposable();


    [ObservableProperty]
    public partial Label SongsCountLabel { get; set; }
    [ObservableProperty]
    public partial Label TranslatedSearch { get; set; }




    #region privte fields
    private ICoverArtService _coverArtService;

    public IMapper _mapper;
    private readonly CommandEvaluator commandEvaluator;
    private IAppInitializerService appInitializerService;
    private IDimmerLiveStateService _dimmerLiveStateService;
    protected IDimmerStateService _stateService;
    protected ISettingsService _settingsService;
    protected SubscriptionManager _subsManager;
    protected IFolderMgtService _folderMgtService;
    private IRepository<SongModel> songRepo;
    private readonly IDeviceConnectivityService deviceConnectivityService;
    private IRepository<ArtistModel> artistRepo;
    private IRepository<PlaylistModel> _playlistRepo;
    private IRepository<AlbumModel> albumRepo;
    private IFolderMonitorService folderMonitorService;
    private IRepository<GenreModel> genreRepo;
    private IRepository<DimmerPlayEvent> dimmerPlayEventRepo;
    public LyricsMgtFlow _lyricsMgtFlow;
    private MusicRelationshipService musicRelationshipService;
    private MusicArtistryService musicArtistryService;
    private MusicStatsService musicStatsService;
    protected ILogger<BaseViewModel> _logger;
    private IDimmerAudioService audioService;
    private ILibraryScannerService libService;

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
    public partial string AppTitle { get; set; } = "Dimmer v1.96 Theta";

    public const string CurrentAppVersion = "Dimmer v1.96 Theta";

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

    private IDialogueService _dialogueService;
    protected ILastfmService lastfmService;

    [RelayCommand]
    public async Task LoadUserLastFMInfo()
    {
        if (!lastfmService.IsAuthenticated)
        {
            return;
        }
        var usr = await lastfmService.GetUserInfoAsync();
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
        var rlm = realmFactory.GetRealmInstance();
        rlm.Write(() =>
        {
            var usre = rlm.All<UserModel>().ToList();
            if (usre is not null)
            {
                var usrr = usre.FirstOrDefault();
                if (usrr is not null)
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
            string url = await lastfmService.GetAuthenticationUrlAsync();
            await Shell.Current.DisplayAlert(
               "Authorize in Browser",
               "Please authorize Dimmer in the browser window that will open, then return here and press 'Complete Login'.",
               "OK");
            // 2. Open it in the browser
            await Launcher.Default.OpenAsync(new Uri(url));

            // 3. Update UI to prompt user to finish
            // e.g., Show a message: "Please authorize in >r browser, then click 'Finish Login'."
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
            // Call the second-step method in >r service
            bool success = await lastfmService.CompleteAuthenticationAsync(UserLocal.LastFMAccountInfo.Name);

            if (success)
            {
                await Shell.Current.DisplayAlert("Success!", $"Successfully logged in as {lastfmService.AuthenticatedUser}.", "Awesome!");
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
        lastfmService.Logout();
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

    private SourceList<SongModelView> _songSource = new SourceList<SongModelView>();
    [ObservableProperty]
    public partial ObservableCollection<LyricPhraseModelView>? CurrentSynchronizedLyrics { get; set; }

    [ObservableProperty]
    public partial LyricPhraseModelView? ActiveCurrentLyricPhrase { get; set; }

    [ObservableProperty]
    public partial bool IsMainViewVisible { get; set; } = true;

    [ObservableProperty]
    public partial CurrentPage CurrentPageContext { get; set; }

    [ObservableProperty]
    public partial Hqub.Lastfm.Entities.Track? SelectedSongLastFMData { get; set; }
    [ObservableProperty]
    public partial Hqub.Lastfm.Entities.Track? CorrectedSelectedSongLastFMData { get; set; }

    [ObservableProperty]
    public partial SongModelView? SelectedSong { get; set; }

     async partial  void OnSelectedSongChanged(SongModelView? oldValue, SongModelView? newValue)
    {

        if (newValue is not null)
        {
            SelectedSecondDomColor = await ImageResizer.GetDomminantMauiColorAsync(newValue.CoverImagePath);


        }

        //LoadSongLastFMData().ConfigureAwait(false);
        //LoadSongLastFMMoreData().ConfigureAwait(false);

    }
    public async Task LoadSelectedSongLastFMData()
    {
        if (SelectedSong is not null)
        {
            SelectedSongLastFMData = null;
            CorrectedSelectedSongLastFMData = null;

            var res = dimmerPlayEventRepo.GetAll().Where(x => x.SongName == SelectedSong.Title);


            var playEvents = _mapper.Map<ObservableCollection<DimmerPlayEventView>>(res);
            SelectedSong.PlayEvents=playEvents;
            _ = Task.Run(() => LoadStatsForSelectedSong(SelectedSong));



            SelectedSecondDomColor = await ImageResizer.GetDomminantMauiColorAsync(SelectedSong.CoverImagePath);
            await LoadSongLastFMData();

        }
    }
    [ObservableProperty]
    public partial Color? SelectedSecondDomColor { get; set; } 
    [ObservableProperty]
    public partial byte[]? SelectedSecondSongCoverBytes { get; set; } = Array.Empty<byte>();
    public async Task LoadSongLastFMData()
    {
        return;
        if (SelectedSong is null || SelectedSong.ArtistName=="Unknown Artist")
        {
            return;
        }
      


            var artistName = SelectedSong.ArtistName.Split("| ", StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        artistName ??=string.Empty;
            SelectedSongLastFMData= await lastfmService.GetTrackInfoAsync(artistName, SelectedSong.Title);
        if (SelectedSongLastFMData is null)
        {
            return;
        }
            SelectedSongLastFMData.Artist = await lastfmService.GetArtistInfoAsync(artistName);
            SelectedSongLastFMData.Album = await lastfmService.GetAlbumInfoAsync(artistName, SelectedSong.AlbumName);

    }
 
    public async Task LoadSongLastFMMoreData()
    {
        if (SelectedSong is null)
        {
            return;
        }
        //SimilarTracks=   await lastfmService.GetSimilarAsync(SelectedSong.ArtistName, SelectedSong.Title);
        //var LyricsMetadataService = IPlatformApplication.Current.Services.GetService<ILyricsMetadataService>();
        //IEnumerable<LrcLibSearchResult>? s = await LyricsMetadataService.SearchOnlineManualParamsAsync(SelectedSong.Title, SelectedSong.ArtistName, SelectedSong.AlbumName);
        //AllLyricsResultsLrcLib = s.ToObservableCollection();

    }

    [RelayCommand]
    public async Task PickSongImageFromFolderAsync()
    {
        var result = await FilePicker.Default.PickAsync(new PickOptions
        {
            PickerTitle = "Select an image for the song",
            FileTypes = FilePickerFileType.Images,
        });
        if (result is null)
        {
            _logger.LogWarning("No image file was selected.");
            return;
        }
        var file = result.FullPath;
        if (string.IsNullOrEmpty(file))
        {
            _logger.LogWarning("Selected file path is empty.");
            return;
        }
        // now save to realm db async

        if (SelectedSong is null)
        {
            _logger.LogWarning("No song is currently selected to update the image.");
            return;
        }
        try
        {
            
            SelectedSong.CoverImagePath = file;
            // Save changes to Realm
            var realm = realmFactory.GetRealmInstance();
            await realm.WriteAsync(() =>
            {

                var existingSong = realm.Find<SongModel>(SelectedSong.Id);
                if (existingSong is null)
                {
                    _logger.LogWarning("Selected song with ID {SongId} not found in Realm database.", SelectedSong.Id);
                    return;
                }
                // Update the cover image path
                existingSong.CoverImagePath = file;
                // save song to realm

                realm.Add(existingSong, update: true);
            });
            _logger.LogInformation("Successfully updated cover image for song '{Title}'", SelectedSong.Title);

            SelectedSong.CoverImageBytes = ImageResizer.ResizeImage(File.ReadAllBytes(file));

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update cover image for song '{Title}'", SelectedSong?.Title);
        }
    }
    public async Task FetchAndLoadSelectedSongFromLastFMToSelectedSongLastFMObject()
    {
        if (SelectedSong is null)
        {
            _logger.LogWarning("No song is currently selected to fetch Last.fm data.");
            return;
        }
        var artistName = SelectedSong.ArtistName.Split("| ", StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();

        Hqub.Lastfm.Entities.Track? trackInfo = await lastfmService.GetTrackInfoAsync(artistName, SelectedSong.Title);
        if (trackInfo is not null)
        {
            SelectedSongLastFMData = trackInfo;
            CorrectedSelectedSongLastFMData = await lastfmService.GetCorrectionAsync(artistName, SelectedSong.Title);
            SimilarTracks = await lastfmService.GetSimilarAsync(artistName, SelectedSong.Title);
            if (SimilarTracks is not null)
            {
                SimilarSongs = SimilarTracks.ToObservableCollection();
            }
        }
        else
        {
            _logger.LogWarning("Failed to load Last.fm track info for song '{Title}' by '{Artist}'", SelectedSong.Title, SelectedSong.ArtistName);
        }
    }

    [ObservableProperty]
    public partial ObservableCollection<Hqub.Lastfm.Entities.Track> SimilarSongs { get; set; } = new ObservableCollection<Hqub.Lastfm.Entities.Track>();
    [ObservableProperty]
    public partial ObservableCollection<Hqub.Lastfm.Entities.Track>? SimilarTracks { get; set; }
    [ObservableProperty]
    public partial bool IsSearching { get; set; }
    [ObservableProperty]
    public partial ObservableCollection<LrcLibSearchResult>? AllLyricsResultsLrcLib { get; set; }
    [ObservableProperty]
    public partial SongModelView SelectedSongOnPage { get; set; }


    [ObservableProperty]
    public partial bool IsLoadingSongs { get; set; }

    [ObservableProperty]
    public partial int SettingsPageIndex { get; set; } = 0;

    [ObservableProperty]
    public partial ObservableCollection<string> FolderPaths { get; set; } = new();

    private BaseAppFlow _baseAppFlow;



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





    public string QueryBeforePlay { get; private set; }

    IRealmFactory realmFactory;
    private Realm realm;

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
        CurrentPlayingSongView.IsCurrentPlayingHighlight=false;

        CurrentPlayingSongView = args.MediaSong;
        _songToScrobble = CurrentPlayingSongView; // This is the next candidate.
        CurrentPlayingSongView.IsCurrentPlayingHighlight=true;


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



    /// <summary>
    /// Creates a deep, unmanaged copy of the selected song for safe editing.
    /// </summary>
    private void PrepareForEditing(SongModelView song)
    {
        // Use >r mapper to create a clean copy. This assumes > have a
        // SongModelView -> SongModelView mapping configured in AutoMapper.
        // If not, > can manually create a new SongModelView and copy properties.
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
        if (song.CoverImagePath=="musicnote.png")
        {
            song.CoverImagePath=string.Empty;
        }
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
            song.CoverImageBytes = ImageResizer.ResizeImage( await File.ReadAllBytesAsync(finalImagePath), 1200);
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
                await LoadAndCacheCoverArtAsync(song);

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

        CurrentPlayingSongView.IsCurrentPlayingHighlight=false;
        
    }

    private void OnPlaybackResumed(PlaybackEventArgs args)
    {
        if (args.MediaSong is null)
        {
            _logger.LogWarning("OnPlaybackPaused was called but the event had no song context.");
            return;
        }

        CurrentPlayingSongView.IsCurrentPlayingHighlight=true;
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

        CurrentPlayingSongView.IsCurrentPlayingHighlight=false;

        _baseAppFlow.UpdateDatabaseWithPlayEvent(realmFactory, CurrentPlayingSongView, StatesMapper.Map(DimmerPlaybackState.PlayCompleted), CurrentTrackDurationSeconds);

        CurrentTrackPositionSeconds = 0;
        CurrentTrackPositionPercentage = 0;
        // Automatically play the next song in the queue.
        await NextTrackAsync();
    }

    private void OnSeekCompleted(double newPosition)
    {

        _logger.LogInformation("AudioService confirmed: Seek completed to {Position}s.", newPosition);
        _baseAppFlow.UpdateDatabaseWithPlayEvent(realmFactory, CurrentPlayingSongView, StatesMapper.Map(DimmerPlaybackState.Seeked), newPosition);
    }

    private void OnPositionChanged(double positionSeconds)
    {
        CurrentTrackPositionSeconds = positionSeconds;
        CurrentTrackPositionPercentage = (CurrentTrackDurationSeconds > 0 ? (positionSeconds / CurrentTrackDurationSeconds) : 0)*100;

    }
    [ObservableProperty]
    public partial Color? CurrentPlaySongDominantColor { get; set; }
    async partial void OnCurrentPlayingSongViewChanged(SongModelView value)
    {
        if (value.Title is null)
            return;

        if (audioService.IsPlaying)
        {
            value.IsCurrentPlayingHighlight = true;
        
        AppTitle = $"{CurrentAppVersion} | {value.Title} - {value.ArtistName} ";
        value.CurrentPlaySongDominantColor = await ImageResizer.GetDomminantMauiColorAsync(value.CoverImagePath,1f);
        CurrentPlaySongDominantColor = value.CurrentPlaySongDominantColor;
        // Efficiently load related data
        value.PlayEvents = _mapper.Map<ObservableCollection<DimmerPlayEventView>>(
            dimmerPlayEventRepo.GetAll().Where(x=>x.Id == value.Id)
        );
        }
        else
        {
            value.IsCurrentPlayingHighlight=false;
        }

    }
    private void OnFolderScanCompleted(PlaybackStateInfo stateInfo)
    {
       
        _logger.LogInformation("Folder scan completed. Refreshing UI.");
       
        IsAppScanning = false;
        var newSongs = stateInfo.ExtraParameter as List<SongModelView>;
        if (newSongs != null && newSongs.Count > 0)
        {
            _logger.LogInformation("Adding {Count} new songs to the UI.", newSongs.Count);

            _songSource.AddRange(newSongs);
            SearchSongSB_TextChanged("desc added");
            _ = EnsureCoverArtCachedForSongsAsync(newSongs);

            var _lyricsCts = new CancellationTokenSource();
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

    public void OnAppOpening()
    {
        var realmm = realmFactory.GetRealmInstance();
        var appModel = realmm.All<AppStateModel>().ToList();
        if (appModel is not null && appModel.Count>0)
        {
            var appmodel = appModel[0];
           
                //appmodel.LastKnownQuery = CurrentQuery;
                //appmodel.LastKnownPlaybackQuery = CurrentPlaybackQuery;
                //appmodel.LastKnownPlaybackQueueIndex = _playbackQueueIndex;
                //appmodel.LastKnownPlaybackQueue = _playbackQueue.ToList();
                //appmodel.LastKnownShuffleState = IsShuffleActive;
                //appmodel.LastKnownRepeatState = IsRepeatActive;
            
            CurrentTrackPositionSeconds= appmodel.LastKnownPosition;

            var song=_songSource.Items.FirstOrDefault(x=> x.Id.ToString() == appmodel.CurrentSongId);
                if (song is not null)
                {
                    CurrentPlayingSongView = song;
                    //OnCurrentSongChanged(song);
                }
                else
                {
                    CurrentPlayingSongView = new();
                }

                DeviceVolumeLevel=appmodel.VolumeLevelPreference;


        }

    }

    public  void OnAppClosing()
    {
        var realmm = realmFactory.GetRealmInstance();
        var appModel = realmm.All<AppStateModel>().ToList();
        if (appModel is not null && appModel.Count>0)
        {
            var appmodel = appModel[0];
            realmm.Write(() =>
            {
                //appmodel.LastKnownQuery = CurrentQuery;
                //appmodel.LastKnownPlaybackQuery = CurrentPlaybackQuery;
                //appmodel.LastKnownPlaybackQueueIndex = _playbackQueueIndex;
                //appmodel.LastKnownPlaybackQueue = _playbackQueue.ToList();
                //appmodel.LastKnownShuffleState = IsShuffleActive;
                //appmodel.LastKnownRepeatState = IsRepeatActive;
                appmodel.LastKnownPosition=CurrentTrackPositionSeconds;
                appmodel.CurrentSongId = CurrentPlayingSongView?.Id.ToString();
                appmodel.VolumeLevelPreference=audioService.Volume;

            });
           //FolderPaths = appmodel.UserMusicFoldersPreference.ToObservableCollection();

        }
    }

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


        _subsManager.Add(Observable.FromEventPattern<PlaybackEventArgs>(
                h => audioService.PlaybackStateChanged += h,
                h => audioService.PlaybackStateChanged -= h)
            .Select(evt => evt.EventArgs)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(HandlePlaybackStateChange, ex => _logger.LogError(ex, "Error in PlaybackStateChanged subscription")));




        _subsManager.Add(Observable.FromEventPattern<PlaybackEventArgs>(
                h => audioService.IsPlayingChanged += h,
                h => audioService.IsPlayingChanged -= h)
            .Select(evt => evt.EventArgs.IsPlaying)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(isPlaying =>
            {
                IsPlaying = isPlaying;

            }
            , ex => _logger.LogError(ex, "Error in IsPlayingChanged subscription")));


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



        _subsManager.Add(Observable.FromEventPattern<PlaybackEventArgs>(
                h => audioService.PlayEnded += h,
                h => audioService.PlayEnded -= h)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(async _ => await OnPlaybackEnded(), ex => _logger.LogError(ex, "Error in PlayEnded subscription")));


        _subsManager.Add(Observable.FromEventPattern<PlaybackEventArgs>(
                h => audioService.MediaKeyNextPressed += h,
                h => audioService.MediaKeyNextPressed -= h)
            .Subscribe(async _ => await NextTrackAsync(), ex => _logger.LogError(ex, "Error in MediaKeyNextPressed subscription")));

        _subsManager.Add(Observable.FromEventPattern<PlaybackEventArgs>(
                h => audioService.MediaKeyPreviousPressed += h,
                h => audioService.MediaKeyPreviousPressed -= h)
            .Subscribe(async _ => await PreviousTrack(), ex => _logger.LogError(ex, "Error in MediaKeyPreviousPressed subscription")));
    }

    #region Playback Commands (User Intent)

    #region Playback Commands (User Intent)


    [RelayCommand]
    public async Task PlaySong(SongModelView? songToPlay)
    {
        if (songToPlay == null)
            return;

        
        
        var newQueue = _searchResults.ToList();
        int startIndex = newQueue.IndexOf(songToPlay);

        if (startIndex == -1)
        {
            
            
            _logger.LogWarning("Song '{Title}' not in current search results. Playing it alone.", songToPlay.Title);
            newQueue = new List<SongModelView> { songToPlay };
            startIndex = 0;
        }

        
        
        await StartNewPlaybackQueue(newQueue, startIndex, CurrentTqlQuery);
    }

    private async Task PlaySongAtIndexAsync(int index)
    {
        if (_playbackQueue == null || index < 0 || index >= _playbackQueue.Count)
        {
            _logger.LogInformation("Playback stopped: Index {Index} is out of bounds for the current queue.", index);
            if (audioService.IsPlaying)
                audioService.Stop();
            UpdateSongSpecificUi(null);
            return;
        }

        _playbackQueueIndex = index;
        var songToPlay = _playbackQueue[_playbackQueueIndex];

        if (_playbackQueueIndex == -1)
     
        PlaybackManager.AddSongToHistory(songToPlay);
        if (songToPlay.TitleDurationKey != CurrentPlayingSongView.TitleDurationKey)
        {
            CurrentTrackPositionSeconds=0;
        }


        if (songToPlay.FilePath == null || !File.Exists(songToPlay.FilePath))
        {
            _logger.LogError("Song file not found for '{Title}'. Skipping to next track.", songToPlay.Title);

            await ValidateSongAsync(songToPlay);
            await NextTrackAsync();
            return;
        }
        if (audioService.IsPlaying)
        {
            audioService.Stop();

        }
        await audioService.InitializeAsync(songToPlay, CurrentTrackPositionSeconds);
    }


    private int GetNextIndexInQueue(int direction)
    {
        if (_playbackQueue.Count == 0)
            return -1;

        if (CurrentRepeatMode == RepeatMode.One)
        {
            return _playbackQueueIndex;
        }

        int nextIndex = _playbackQueueIndex + direction;



        if (nextIndex >= _playbackQueue.Count)
        {


            return CurrentRepeatMode == RepeatMode.All ? 0 : -1;
        }

        if (nextIndex < 0)
        {


            return CurrentRepeatMode == RepeatMode.All ? _playbackQueue.Count - 1 : -1;
        }


        return nextIndex;
    }
    private async Task StartNewPlaybackQueue(IEnumerable<SongModelView> songs, int startIndex, string contextQuery)
    {
        List<SongModelView> finalQueue;
        int finalStartIndex;

        if (IsShuffleActive)
        {
            var songToStartWith = songs.ElementAt(startIndex);
            var otherSongs = songs.Where(s => s.Id != songToStartWith.Id).ToList();
            var shuffledSongs = otherSongs.OrderBy(x => _random.Next()).ToList();

            finalQueue = new List<SongModelView> { songToStartWith };
            finalQueue.AddRange(shuffledSongs);
            finalStartIndex = 0;
        }
        else
        {
            finalQueue = songs.ToList();
            finalStartIndex = startIndex;
        }

        if (audioService.IsPlaying)
        {
            audioService.Stop();
        }

        

        
        _playbackQueueSource.Edit(updater =>
        {
            updater.Clear();
            updater.AddRange(finalQueue);
        });

        
        CurrentPlaybackQuery = contextQuery;
        SavePlaybackContext(CurrentPlaybackQuery);
        PlaybackManager.ClearSessionHistory();

        

        
        if (finalQueue == null || finalStartIndex < 0 || finalStartIndex >= finalQueue.Count)
        {
            _logger.LogError("Could not start playback. Invalid start index for the new queue.");
            UpdateSongSpecificUi(null);
            return;
        }

        
        _playbackQueueIndex = finalStartIndex;
        var songToPlay = finalQueue[_playbackQueueIndex];

        
        PlaybackManager.AddSongToHistory(songToPlay);
        CurrentTrackPositionSeconds = 0; 

        
        if (string.IsNullOrEmpty(songToPlay.FilePath) || !File.Exists(songToPlay.FilePath))
        {
            _logger.LogError("Song file not found for '{Title}'. Skipping...", songToPlay.Title);
            await ValidateSongAsync(songToPlay);
            
            
            return;
        }

        
        await audioService.InitializeAsync(songToPlay, CurrentTrackPositionSeconds);
    }
    [RelayCommand]
    private void ToggleShuffle()
    {
        IsShuffleActive = !IsShuffleActive;
        _logger.LogInformation("Shuffle mode toggled to: {ShuffleState}", IsShuffleActive);

        if (IsShuffleActive && _playbackQueue.Any() && _playbackQueueIndex >= 0)
        {
           
            var playedPart = _playbackQueue.Take(_playbackQueueIndex + 1).ToList();

            
            var upcomingPart = _playbackQueue.Skip(_playbackQueueIndex + 1).ToList();

            
            var shuffledUpcoming = upcomingPart.OrderBy(x => _random.Next()).ToList();

            
            var newQueue = new List<SongModelView>();
            newQueue.AddRange(playedPart);
            newQueue.AddRange(shuffledUpcoming);

            
            _playbackQueueSource.Edit(updater =>
            {
                updater.Clear();
                updater.AddRange(newQueue);
            });
        }
    }

    #endregion
    [RelayCommand]
    public async Task PlayPauseToggle()
    {
        if (!audioService.IsPlaying && CurrentPlayingSongView?.Title == null)
        {
            var firstSong = _searchResults.FirstOrDefault();
            if (firstSong != null)
            {
                await PlaySong(firstSong); 
            }
            return;
        }

        
        if (audioService.IsPlaying)
        {
            audioService.Pause();
        }
        else
        {
            audioService.Play(CurrentTrackPositionSeconds); 
        }
    }

    [RelayCommand]
    public async Task NextTrackAsync()
    {
        if (IsPlaying && CurrentPlayingSongView != null)
        {
            _baseAppFlow.UpdateDatabaseWithPlayEvent(realmFactory, CurrentPlayingSongView, StatesMapper.Map(DimmerPlaybackState.Skipped), CurrentTrackPositionSeconds);
        }


        if (IsPlaying && _songToScrobble != null && IsLastfmAuthenticated)
        {
            await lastfmService.ScrobbleAsync(_songToScrobble);
        }


        int nextIndex = GetNextIndexInQueue(1);

        
        bool hasLooped = (_playbackQueueIndex == _playbackQueue.Count - 1) && nextIndex == 0;

        
        if (hasLooped && IsShuffleActive && CurrentRepeatMode == RepeatMode.All)
        {
            _logger.LogInformation("End of shuffled queue reached. Reshuffling for Repeat All.");

            var songsToReshuffle = _playbackQueue.ToList();

            await StartNewPlaybackQueue(songsToReshuffle, 0, CurrentPlaybackQuery);
            return;



        }

        await PlaySongAtIndexAsync(nextIndex);
    }
    [RelayCommand]
    private void AddNewPlaybackRule()
    {
        
        PlaybackManager.Rules.Add(new PlaybackRule { Priority = PlaybackManager.Rules.Count + 1, Query = "" });
    }

    [RelayCommand]
    private void RemovePlaybackRule(PlaybackRule rule)
    {
        if (rule != null)
        {
            PlaybackManager.Rules.Remove(rule);
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
        if (IsPlaying && _songToScrobble != null && IsLastfmAuthenticated)
        {
            await lastfmService.ScrobbleAsync(_songToScrobble);
        }
        var prevIndex = GetNextIndexInQueue(-1);
        await PlaySongAtIndexAsync(prevIndex);
    }

    #endregion

    #region Private Playback Helper Methods

    
    private const string LastSessionPlaylistName = "__LastPlaybackSession";

    [RelayCommand]
    public void LoadLastPlaybackSession()
    {
        
        var existingPlaylist = _playlistRepo.FirstOrDefaultWithRQL("PlaylistName == $0", LastSessionPlaylistName);
        if (existingPlaylist != null)
        {
            
            QueryBeforePlay = existingPlaylist.QueryText;
            var restoredQueue = existingPlaylist.SongsIdsInPlaylist
            .Select(songId => _songSource.Items.FirstOrDefault(song => song.Id == songId))
            .Where(song => song != null)
            .ToList();
            if (restoredQueue is null)
                return;
            _playbackQueueSource.Edit(updater =>
            {
                updater.Clear();
                updater.AddRange(restoredQueue);
            });
            
            CurrentPlaybackQuery = QueryBeforePlay;
            
            if (_playbackQueue.Any())
            {
                PlaySongAtIndexAsync(0).ConfigureAwait(false);
            }
        }
        else
        {
            _logger.LogInformation("No previous playback session found.");
        }
    }
    private void SavePlaybackContext(string query)
    {
        
        var existingPlaylist = _playlistRepo.FirstOrDefaultWithRQL("PlaylistName == $0", LastSessionPlaylistName);

        
        if (existingPlaylist != null && existingPlaylist.QueryText == query)
        {
            
            
            
            _logger.LogInformation("Same query detected. Updating existing session playlist.");

            _playlistRepo.Update(existingPlaylist.Id, playlistInDb =>
            {
                playlistInDb.LastPlayedDate = DateTimeOffset.UtcNow;
                playlistInDb.PlayHistory.Add(new PlaylistEvent());
            });
        }
        else
        {
            
            
            _logger.LogInformation("New query detected. Overwriting session playlist.");

            
            var contextPlaylist = new PlaylistModel
            {
                
                
                Id = existingPlaylist?.Id ?? ObjectId.GenerateNewId(),
                PlaylistName = LastSessionPlaylistName, 
                IsSmartPlaylist = !string.IsNullOrEmpty(query),
                QueryText = query,
                DateCreated = DateTimeOffset.UtcNow,
                LastPlayedDate = DateTimeOffset.UtcNow,
            };

            
            contextPlaylist.PlayHistory.Add(new PlaylistEvent());

            
            foreach (var song in _playbackQueue)
            {
                contextPlaylist.SongsIdsInPlaylist.Add(song.Id);
            }

            
            
            _playlistRepo.Upsert(contextPlaylist);
        }

        
        QueryBeforePlay = query;
        _logger.LogInformation("Saved playback context for query: \"{query}\"", query);
    }
    
    [RelayCommand]
    private async Task PlayPlaylist(PlaylistModelView? playlist)
    {
        if (playlist == null || playlist.SongsIdsInPlaylist?.Count <1)
        {
            _logger.LogWarning("PlayPlaylist called with a null or empty playlist.");
            return;
        }


        var songsInPlaylist = playlist.SongsIdsInPlaylist
      .Select(id => _songSource.Items.FirstOrDefault(s => s.Id == id))
      .Where(s => s != null)
      .ToList();

        if (!songsInPlaylist.Any())
            return;

        
        var contextQuery = $"playlist:\"{playlist.PlaylistName}\"";
        await StartNewPlaybackQueue(songsInPlaylist!, 0, contextQuery);
    
    }

    [RelayCommand]
    public void ToggleRepeatMode()
    {
        
        CurrentRepeatMode = (RepeatMode)(((int)CurrentRepeatMode + 1) % Enum.GetNames(typeof(RepeatMode)).Length);

        
        _settingsService.RepeatMode = CurrentRepeatMode;

        _logger.LogInformation("Repeat mode toggled to: {RepeatMode}", CurrentRepeatMode);

        
        
    }
    
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

    }
    public record PlaylistSongContext(PlaylistModelView Playlist, SongModelView SongToPlay);
    #endregion
   
    [RelayCommand]
    private async Task StartRadioStation(SongModelView? seedSong)
    {
        if (seedSong == null)
            return;

        var seedGenre = seedSong.GenreName;
        if (string.IsNullOrEmpty(seedGenre))
        {
            _logger.LogWarning("Cannot start radio for song without a genre.");
            return;
        }

        var radioSongs = _songSource.Items
            .Where(s => s.GenreName == seedGenre && s.Id != seedSong.Id)
            .OrderBy(x => _random.Next())
            .Take(50)
            .ToList();

        var newQueue = new List<SongModelView> { seedSong };
        newQueue.AddRange(radioSongs);

        
        var contextQuery = $"radio:\"{seedSong.Title}\"";
        _logger.LogInformation("Started radio station based on '{Title}' with {Count} songs.", seedSong.Title, newQueue.Count);
        await StartNewPlaybackQueue(newQueue, 0, contextQuery);
    }
    [RelayCommand]
    private void ClearUpcomingSongs()
    {
        if (!_playbackQueue.Any())
            return;

        var numberToKeep = _playbackQueueIndex + 1;
        if (numberToKeep < _playbackQueueSource.Count)
        {
            
            _playbackQueueSource.RemoveRange(numberToKeep, _playbackQueueSource.Count - numberToKeep);
        }

        _logger.LogInformation("Cleared all upcoming songs in the queue.");
    }
    
    public void MoveSongInQueue(int oldIndex, int newIndex)
    {
        
        if (oldIndex < 0 || oldIndex >= _playbackQueue.Count ||
            newIndex < 0 || newIndex >= _playbackQueue.Count || oldIndex == newIndex)
        {
            return;
        }

        _playbackQueueSource.Move(oldIndex, newIndex);




        _logger.LogInformation("Moved song from index {Old} to {New}", oldIndex, newIndex);
    }

    [ObservableProperty]
    public partial bool IsSleepTimerActive { get; set; }

    private IDisposable? _sleepTimerSubscription;

    [RelayCommand]
    private void SetSleepTimer(TimeSpan duration)
    {
        
        _sleepTimerSubscription?.Dispose();

        if (duration <= TimeSpan.Zero)
        {
            IsSleepTimerActive = false;
            _logger.LogInformation("Sleep timer cancelled.");
            return;
        }

        IsSleepTimerActive = true;
        _logger.LogInformation("Sleep timer set for {Duration}.", duration);

        _sleepTimerSubscription = Observable.Timer(duration, RxApp.MainThreadScheduler)
            .Subscribe(_ => {
                _logger.LogInformation("Sleep timer expired. Pausing playback.");
                if (IsPlaying)
                {
                    audioService.Pause();
                }
                IsSleepTimerActive = false;
            });
    }


    /// <summary>
    /// Inserts a single song to play immediately after the current one.
    /// If the queue is empty, it starts playback with this song.
    /// </summary>
    [RelayCommand]
    public void PlayNext(SongModelView? song)
    {
        if (song == null)
            return;

        _playbackQueueSource.Insert(_playbackQueueIndex + 1, song);
    }

    /// <summary>
    /// Inserts a list of songs to play immediately after the current one.
    /// If the queue is empty, it starts playback with this new list.
    /// </summary>
    public async Task PlayNextSongsImmediately(IEnumerable<SongModelView>? songs)
    {
        if (songs == null || !songs.Any())
            return;

        var distinctSongs = songs.Distinct().ToList();

       
        if (!_playbackQueue.Any() || CurrentPlayingSongView.Title == null)
        {
            if (audioService.IsPlaying)
                audioService.Stop();

            _playbackQueueSource.Edit(updater =>
            {
                updater.Clear();
                updater.AddRange(distinctSongs);
            });

            _playbackQueueIndex = 0;
            await PlaySongAtIndexAsync(_playbackQueueIndex);
            return;
        }

       
        var insertIndex = _playbackQueueIndex + 1;
        _playbackQueueSource.InsertRange(distinctSongs, insertIndex);

        _logger.LogInformation("Added {Count} song(s) to play next.", distinctSongs.Count());
    }
    /// <summary>
    /// Adds a single song to the end of the current playback queue.
    /// If the queue is empty, it starts playback with this song.
    /// </summary>
    [RelayCommand]
    public void AddToQueue(SongModelView? song)
    {
        if (song == null)
            return;

       
        AddToQueueEnd(new List<SongModelView> { song });
    }

    /// <summary>
    /// Adds a list of songs to the end of the current playback queue.
    /// If the queue is empty, it starts playback with this new list.
    /// </summary>
    
    public void AddToQueueEnd(IEnumerable<SongModelView>? songs)
    {
        if (songs == null || !songs.Any())
            return;

        _playbackQueueSource.AddRange(songs.Distinct());
    }



    /// <summary>
    /// A new helper method to route playback state changes to the correct handler.
    /// This is the target of our main PlaybackStateChanged subscription.
    /// </summary>
    private SongModelView? _songToScrobble;
    private void HandlePlaybackStateChange(PlaybackEventArgs args)
    {

       
       
        PlayType? state = StatesMapper.Map(args.EventType);

        switch (state)
        {
            case PlayType.Play:
               
               
               
                OnPlaybackStarted(args);
                break;

            case PlayType.Resume:
                OnPlaybackResumed(args);
                break;

            case PlayType.Pause:
                OnPlaybackPaused(args);
                break;

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

       
        var playbackStateObservable = _stateService.CurrentPlayBackState.Publish().RefCount();

        _subsManager.Add(playbackStateObservable
            .Where(s => s.State == DimmerPlaybackState.FolderScanCompleted)
            .Subscribe(OnFolderScanCompleted, ex => _logger.LogError(ex, "Error on FolderScanCompleted.")));

        _subsManager.Add(_stateService.LatestDeviceLog
            .Where(s => s.Log is not null)
            .Subscribe(LatestDeviceLog, ex => _logger.LogError(ex, "Error on FolderScanCompleted.")));

       
    }

    private void OnCurrentSongChanged(SongModelView? view)
    {
        //throw new NotImplementedException();
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


    private Random _random = new();


    [RelayCommand]
    public void SeekTrackPosition(double positionSeconds)
    {


        _logger.LogDebug("SeekTrackPosition called by UI to: {PositionSeconds}s", positionSeconds);
        audioService.Seek(positionSeconds);
        //_baseAppFlow.UpdateDatabaseWithPlayEvent( realmFactory,CurrentPlayingSongView, StatesMapper.Map(DimmerPlaybackState.Seeked), positionSeconds);

       

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
    public void AddMusicFoldersByPassingToService(List<string> folderPaths)
    {
        _logger.LogInformation("User requested to add music folder.");
        _folderMgtService.AddManyFoldersToWatchListAndScan(folderPaths);
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
    public partial DimmerStats? SongEvergreenScore { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<DimmerStats>? SongWeekdayVsWeekend { get; set; }


    /// <summary>
    /// Loads ALL global, library-wide statistics. Call this once when the page loads.
    /// </summary>
    public void GetStatsGeneral()
    {
       
        var allSongs = songRepo.GetAll();
        var allEvents = dimmerPlayEventRepo.GetAll();
        int topCount = 10;

       
        var endDate = DateTimeOffset.UtcNow;
        var monthStartDate = endDate.AddMonths(-1);
        var yearStartDate = endDate.AddYears(-1);

       
        TopSongsLastMonth = TopStats.GetTopCompletedSongs(allSongs, allEvents, topCount, monthStartDate, endDate).ToObservableCollection();
        MostSkippedSongs = TopStats.GetTopSkippedSongs(allSongs, allEvents, topCount).ToObservableCollection();
        ArtistsByHighestSkipRate = TopStats.GetArtistsByHighestSkipRate(allEvents, allSongs, topCount).ToObservableCollection();
        TopBurnoutSongs = TopStats.GetTopBurnoutSongs(allEvents, allSongs, topCount).ToObservableCollection();
        TopRediscoveredSongs = TopStats.GetTopRediscoveredSongs(allEvents, allSongs, topCount).ToObservableCollection();
        TopArtistsByVariety = TopStats.GetTopArtistsBySongVariety(allEvents, allSongs, topCount).ToObservableCollection();
        TopGenresByListeningTime = TopStats.GetTopGenresByListeningTime(allEvents, allSongs, topCount).ToObservableCollection();

       
        OverallListeningByDayOfWeek = ChartSpecificStats.GetOverallListeningByDayOfWeek(allEvents, allSongs).ToObservableCollection();
        DeviceUsageByTopArtists = TopStats.GetDeviceUsageByTopArtists(allEvents, allSongs, 5).ToObservableCollection();
        GenrePopularityOverTime = ChartSpecificStats.GetGenrePopularityOverTime(allEvents, allSongs).ToObservableCollection();
        DailyListeningTimeRange = ChartSpecificStats.GetDailyListeningTimeRange(allEvents, allSongs, monthStartDate, endDate).ToObservableCollection();
        SongProfileBubbleChart = ChartSpecificStats.GetSongProfileBubbleChartData(allEvents, allSongs).ToObservableCollection();
        DailyListeningRoutineOHLC = ChartSpecificStats.GetDailyListeningRoutineOHLC(allEvents, allSongs, monthStartDate, endDate).ToObservableCollection();

       
       
    }

    /// <summary>
    /// Loads ALL statistics for a single song. Call this when a song is selected.
    /// </summary>
    public async Task LoadStatsForSelectedSong(SongModelView? song)
    {
        song ??= SelectedSong;

        if (song == null)
        {
            ClearSingleSongStats();
            return;
        }

       
        var songDb = songRepo.GetById(song.Id);
        if (songDb == null)
        {
            ClearSingleSongStats();
            return;
        }

        
        
        //var songEvents = songDb.PlayHistory.ToList().AsReadOnly();
        var songEventsROCol = await dimmerPlayEventRepo.GetAllAsync();
        var songEvents = songEventsROCol.Where(x => x.SongName == song.Title).ToList();
        if (songEvents.Count()==0)
        {
            ClearSingleSongStats();
            return;
        }

        

        
        SongPlayTypeDistribution = TopStats.GetPlayTypeDistribution(songEvents).ToObservableCollection();
        SongPlayDistributionByHour = TopStats.GetPlayDistributionByHour(songEvents).ToObservableCollection();
        SongBingeFactor = TopStats.GetBingeFactor(songEvents, song.Id);
        SongAverageListenThrough = TopStats.GetAverageListenThroughPercent(songEvents, song.DurationInSeconds);
        SongsFirstImpression = TopStats.GetSongsFirstImpression(songEvents);

        //SongDropOffPoints = ChartSpecificStats.GetSongDropOffPoints(songEvents).ToObservableCollection();
        //SongWeeklyOHLC = ChartSpecificStats.GetSongWeeklyOHLC(songEvents).ToObservableCollection();

        SongListeningStreak = TopStats.GetListeningStreak(songEvents);
    }

   
    private void ClearSingleSongStats()
    {
        SongPlayTypeDistribution = null;
        SongPlayDistributionByHour = null;
        SongPlayHistoryOverTime = null;
        SongDropOffPoints = null;
        SongWeeklyOHLC = null;
        SongBingeFactor = null;
        SongAverageListenThrough = null;

        
        SongListeningStreak = null;
    }
    public async Task SaveNoteToListOfSongs(IEnumerable<SongModelView> songs)
    {
        foreach (var item in songs)
        {
            await SaveUserNoteToDbLegacy(item);
        }
        //TODO : make an error handling logic here
    }
    public async Task SaveUserNoteToDbLegacy(SongModelView songWithNote)
    {
        var result = await Shell.Current.DisplayPromptAsync("Note Text", $"Note for {Environment.NewLine}" +
            $"{songWithNote.Title} - {songWithNote.OtherArtistsName}",
                placeholder: "Tip: Just type this note to search this song through TQL :)",
                accept: "Done", keyboard: Keyboard.Text);
        if (result == null)
        {
            return;
        }
            UserNoteModelView userNote = new()
            {
                UserMessageText = result,
                CreatedAt = DateTime.Now,
            };


        songWithNote.UserNotes = new();
        songWithNote.UserNotes.Add(userNote);

        
        try
        {
            realm ??= realmFactory.GetRealmInstance();
            realm.Write(() =>
            {
                var existingSong = realm.Find<SongModel>(songWithNote.Id);
                if (existingSong != null)
                {
                    var userNoteDb = _mapper.Map<UserNoteModel>(userNote);
                    if (userNoteDb != null)
                    {
                        existingSong.UserNotes.Add(userNoteDb);

                        
                        realm.Add(existingSong, true);

                        _logger.LogInformation("Successfully persisted user note for song: {SongTitle}", existingSong.Title);
                    }
                }
                else
                {
                    _logger.LogWarning("Could not find song with ID {SongId} to save user note.", songWithNote.Id);
                }
            });
            Toast newToast = new()
            {
                Duration= CommunityToolkit.Maui.Core.ToastDuration.Long,
                Text = $"Added Note {userNote.UserMessageText} to {songWithNote.Title}"
            };
           await newToast.Show(CancellationToken.None);
        }

        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save user note for song {SongId}", songWithNote.Id);
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
            _= await lastfmService.LoveTrackAsync(songModel);
        }
        else
        {
            _= await lastfmService.UnloveTrackAsync(songModel);

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

       
       
        var matchingPlaylists = _playlistRepo.Query(p => p.PlaylistName == playlistName);
        var targetPlaylist = matchingPlaylists.FirstOrDefault();

       
        if (targetPlaylist == null)
        {
            _logger.LogInformation("Playlist '{PlaylistName}' not found. Creating it as a new manual playlist.", playlistName);
            var newPlaylistModel = new PlaylistModel
            {
                PlaylistName = playlistName,
                IsSmartPlaylist = false
            };
            targetPlaylist = _playlistRepo.Create(newPlaylistModel);
        }

       
        if (targetPlaylist.IsSmartPlaylist)
        {
            _logger.LogWarning("Cannot manually add songs to the smart playlist '{PlaylistName}'. Change its query instead.", playlistName);
           
            return;
        }

       
        var songIdsToAdd = songsToAdd.Select(s => s.Id).ToHashSet();

        _playlistRepo.Update(targetPlaylist.Id, livePlaylist =>
        {
           
            int songsAddedCount = 0;
            foreach (var songId in songIdsToAdd)
            {
               
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

    private ObservableCollectionExtended<InteractiveChartPoint> _topSkipsList = new();
    private BehaviorSubject<LimiterClause?> _limiterClause;

    public ReadOnlyObservableCollection<InteractiveChartPoint> TopSkipsChartData { get; }

    [ObservableProperty]
    public partial ObservableCollection<DimmerStats>? SongPlayTypeDistribution { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<DimmerStats>? SongPlayDistributionByHour { get; set; }

    [ObservableProperty]
    public partial DimmerStats SongBingeFactor { get; set; }

    [ObservableProperty]
    public partial DimmerStats SongAverageListenThrough { get; set; }

    [ObservableProperty]
    public partial DimmerStats SongsFirstImpression { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<DimmerStats>? SongPlayHistoryOverTime { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<DimmerStats>? SongDropOffPoints { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<DimmerStats>? SongWeeklyOHLC { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<DimmerStats>? TopSongsLastMonth { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<DimmerStats>? MostSkippedSongs { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<DimmerStats>? ArtistsByHighestSkipRate { get; set; }

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
       
        var songsToRefresh = _songSource.Items.AsEnumerable();
        ILyricsMetadataService lryServ = IPlatformApplication.Current!.Services.GetService<ILyricsMetadataService>()!;
       
        await SongDataProcessor.ProcessLyricsAsync(songsToRefresh, lryServ, progressReporter, _lyricsCts.Token);
    }

    public record QueryComponents(
        Func<SongModelView, bool> Predicate,
        IComparer<SongModelView> Comparer,
        LimiterClause? Limiter
    );


    private SourceList<DuplicateSetViewModel> _duplicateSource = new();

   
    private  ReadOnlyObservableCollection<DuplicateSetViewModel> _duplicateSets;

   
    public ReadOnlyObservableCollection<DuplicateSetViewModel> DuplicateSets => _duplicateSets;

    public bool HasDuplicates => _duplicateSets.Any();

    [ObservableProperty]
    public partial bool IsFindingDuplicates { get; set; }
    [RelayCommand]
    private async Task FindDuplicatesAsync()
    {
        IsFindingDuplicates = true;

       
        _duplicateSource.Clear();

        try
        {
           
            var results = await Task.Run(() => _duplicateFinderService.FindDuplicates());

            if (results.Count!=0)
            {
                
               
               
               
                _duplicateSource.Edit(updater =>
                {
                    updater.Clear();
                    updater.AddRange(results);
                });
            }
            else
            {
               
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

        if (itemsToDelete.Count==0)
            return;

       

        var deletedCount = await _duplicateFinderService.ResolveDuplicatesAsync(itemsToDelete);

       

       
        var idsToDelete = itemsToDelete.Select(i => i.Song.Id).ToHashSet();
        _songSource.RemoveMany(_songSource.Items.Where(s => idsToDelete.Contains(s.Id)));

       
        var resolvedSets = DuplicateSets
            .Where(set => set.Items.All(item => item.Action == DuplicateAction.Keep || item.Action == DuplicateAction.Ignore))
            .ToList();

       
       
        _duplicateSource.RemoveMany(resolvedSets);

        _logger.LogInformation("Successfully resolved duplicates, deleting {Count} items.", deletedCount);
    }


    [ObservableProperty]
    public partial bool IsCheckingFilePresence { get; set; }

   
    [RelayCommand]
    private async Task ValidateLibraryAsync()
    {
        if (IsCheckingFilePresence)
            return;

        IsCheckingFilePresence = true;
        _logger.LogInformation("Starting library validation...");
       

        try
        {
           
            var validationResult = await Task.Run(async () => await _duplicateFinderService.ValidateFilePresenceAsync(_mapper.Map<List<SongModelView>>(await songRepo.GetAllAsync())));

            if (validationResult.MissingCount == 0)
            {
                _logger.LogInformation("Library validation complete. No missing files found.");
               
                return;
            }

            _logger.LogInformation("Found {Count} songs with missing files. Removing from UI and database.", validationResult.MissingCount);

           
            var missingIds = validationResult.MissingSongs.Select(s => s.Id).ToHashSet();

           
            var itemsInUiToRemove = _songSource.Items.Where(s => missingIds.Contains(s.Id)).ToList();

           
            _songSource.RemoveMany(itemsInUiToRemove);

           
            await _duplicateFinderService.RemoveSongsFromDbAsync(missingIds);

           
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during library validation.");
           
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
       

        try
        {
           
            var listSong = new List<SongModelView>();
            listSong.Add(song);
            var validationResult = await Task.Run(() => _duplicateFinderService.ValidateFilePresenceAsync(
                listSong));
            if (validationResult.MissingCount == 0)
            {
                _logger.LogInformation("Library validation complete. No missing files found.");
               
                return;
            }

            _logger.LogInformation("Found {Count} songs with missing files. Removing from UI and database.", validationResult.MissingCount);

           
            var missingIds = validationResult.MissingSongs.Select(s => s.Id).ToHashSet();

           
            var itemsInUiToRemove = _songSource.Items.Where(s => missingIds.Contains(s.Id)).ToList();

           
            _songSource.RemoveMany(itemsInUiToRemove);

           
            await _duplicateFinderService.RemoveSongsFromDbAsync(missingIds);

           
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during library validation.");
           
        }
        finally
        {
            IsCheckingFilePresence = false;
        }
    }






    [ObservableProperty]
    public partial string LyricsAlbumNameSearch{ get; set; }

    [ObservableProperty]
    public partial string LyricsTrackNameSearch { get; set; }

    [ObservableProperty]
    public partial string LyricsArtistNameSearch { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasLyricsSearchResults))]
    public partial ObservableCollection<LrcLibSearchResult> LyricsSearchResults { get; set; } = new();

    public bool HasLyricsSearchResults => LyricsSearchResults.Any();

    [ObservableProperty]
    public partial bool IsLyricsSearchBusy { get; set; }

    [ObservableProperty]
    public partial bool IsReconcilingLibrary { get; set; }


    [RelayCommand]
    private async Task SearchLyricsAsync()
    {
        if (SelectedSong == null)
            return;

        IsLyricsSearchBusy = true;
        LyricsSearchResults.Clear();

        try
        {
            if (string.IsNullOrEmpty(LyricsTrackNameSearch))
            {
                LyricsTrackNameSearch = SelectedSong.Title;
            }
            if (string.IsNullOrEmpty(LyricsAlbumNameSearch))
            {
                LyricsAlbumNameSearch = SelectedSong.AlbumName;
            }
            if (string.IsNullOrEmpty(LyricsArtistNameSearch))
            {


                var artistName = SelectedSong.ArtistName.Split("| ", StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                if (artistName != null)
                {
                    LyricsArtistNameSearch = artistName;
                }
                else
                {
                    LyricsArtistNameSearch = SelectedSong.ArtistName;
                }
            }
            var query = $"{LyricsTrackNameSearch} {LyricsArtistNameSearch} {LyricsAlbumNameSearch}".Trim();
            ILyricsMetadataService _lyricsMetadataService = IPlatformApplication.Current!.Services.GetService<ILyricsMetadataService>()!;
           
            IEnumerable<LrcLibSearchResult>? results = await _lyricsMetadataService.SearchOnlineManualParamsAsync(LyricsTrackNameSearch, LyricsArtistNameSearch,LyricsAlbumNameSearch);

            foreach (var result in results)
            {
                LyricsSearchResults.Add(result);
            }
            _logger.LogInformation("Successfully fetched {Count} lyrics search results for '{Query}'", LyricsSearchResults.Count, query);
           
            if (LyricsSearchResults.Count == 0)
            {
                _logger.LogInformation("No lyrics found for the search query: {Query}", query);
                await Shell.Current.DisplayAlert("No Results", "No lyrics found for the specified search criteria.", "OK");
            }
            else
            {
                _logger.LogInformation("Found {Count} lyrics results for query: {Query}", LyricsSearchResults.Count, query);
            }

            await Shell.Current.DisplayAlert("Search Complete", $"Found {LyricsSearchResults.Count} results for '{query}'", "OK");


            LyricsArtistNameSearch = string.Empty;
            LyricsAlbumNameSearch = string.Empty;
            LyricsTrackNameSearch = string.Empty;
                


        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search for lyrics online.");
           

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
           
            _lyricsMgtFlow.LoadLyrics(selectedResult.SyncedLyrics);

           
           
            var lyricsInfo = new LyricsInfo();
            lyricsInfo.Parse(selectedResult.SyncedLyrics);
            var _lyricsMetadataService = IPlatformApplication.Current.Services.GetService<ILyricsMetadataService>();

            await _lyricsMetadataService.SaveLyricsForSongAsync(SelectedSong, selectedResult.SyncedLyrics, lyricsInfo);

           
            SelectedSong.SyncLyrics = selectedResult.SyncedLyrics;
            SelectedSong.HasLyrics = true;
            SelectedSong.HasSyncedLyrics = true;

           
            LyricsSearchResults.Clear();
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

       

        bool confirm = await Shell.Current.DisplayAlert(
            "Unlink Lyrics",
            "Are > sure > want to unlink the lyrics from this song? This will remove all synced lyrics.",
            "Yes, Unlink",
            "Cancel"
        );

        if (!confirm)
            return;

        var _lyricsMetadataService = IPlatformApplication.Current.Services.GetService<ILyricsMetadataService>();

       
        var emptyLyricsInfo = new LyricsInfo();
        await _lyricsMetadataService.SaveLyricsForSongAsync(SelectedSong, string.Empty, emptyLyricsInfo);

       
        SelectedSong.SyncLyrics = string.Empty;
        SelectedSong.UnSyncLyrics = string.Empty;
        SelectedSong.HasLyrics = false;
        SelectedSong.HasSyncedLyrics = false;

       
        _lyricsMgtFlow.LoadLyrics(string.Empty);
    }

    [RelayCommand]
    private async Task ReconcileLibraryAsync()
    {
        if (IsReconcilingLibrary)
            return;

        IsReconcilingLibrary = true;
        _logger.LogInformation("Starting library reconciliation...");
       

        try
        {
           
            var result = await Task.Run(() => _duplicateFinderService.ReconcileLibraryAsync(_songSource.Items.ToList()));

            if (result.MigratedCount == 0 && result.UnresolvedCount == 0)
            {
                _logger.LogInformation("Reconciliation complete. Library is already in a perfect state.");
                return;
            }

           
           
            var songsToRemove = new List<SongModelView>();

           
            songsToRemove.AddRange(result.UnresolvedMissingSongs);

           
            songsToRemove.AddRange(result.MigratedSongs.Select(m => m.From));

           
           
            songsToRemove.AddRange(result.MigratedSongs.Select(m => m.To));

           
            var songsToAdd = result.MigratedSongs.Select(m => m.To).ToList();

           
            _songSource.Edit(updater =>
            {
                updater.Remove(songsToRemove);
                updater.AddRange(songsToAdd);
            });

           
            _logger.LogInformation("UI updated. Removed {RemoveCount} entries, added back {AddCount} updated entries.", songsToRemove.Count, songsToAdd.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during library reconciliation.");
           
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

       
       await songRepo.UpdateAsync(context.Song.Id, songInDb =>
        {
            var artistInDb = artistRepo.GetById(context.TargetArtist.Id);
            if (artistInDb == null)
                return;

           
            songInDb.ArtistToSong.Clear();
            songInDb.ArtistToSong.Add(artistInDb);
            songInDb.Artist = artistInDb;
            songInDb.ArtistName = artistInDb.Name;
        });

       
       
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

       
       
        string? newArtistName = await Shell.Current.DisplayPromptAsync(
            "Create New Artist",
            "Enter the name for the new artist:");

        if (string.IsNullOrWhiteSpace(newArtistName))
            return;

        _logger.LogInformation("Creating new artist '{ArtistName}' and assigning {Count} songs.", newArtistName, songsToAssign.Count);

       
        var newArtist = new ArtistModel { Name = newArtistName };
        var createdArtist = artistRepo.Create(newArtist);

       
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

       
        var updatedSongs = _mapper.Map<List<SongModelView>>(songRepo.Query(s => songIds.Contains(s.Id)));
        _songSource.Edit(updater =>
        {
            updater.RemoveMany(songsToAssign);
            updater.AddRange(updatedSongs);
        });
    }

   
   
   

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

       
        string? albumName = await Shell.Current.DisplayPromptAsync("Group into Album", "Enter the album name:");
        if (string.IsNullOrWhiteSpace(albumName))
            return;

       
        string? albumArtistName = await Shell.Current.DisplayPromptAsync("Group into Album", "Enter the album artist name:", initialValue: songsToAlbumize.First().ArtistName);
        if (string.IsNullOrWhiteSpace(albumArtistName))
            return;

       
        var albumArtist = artistRepo.Query(a => a.Name == albumArtistName).FirstOrDefault() ?? artistRepo.Create(new ArtistModel { Name = albumArtistName });
        var album = albumRepo.Query(a => a.Name == albumName).FirstOrDefault() ?? albumRepo.Create(new AlbumModel { Name = albumName, Artist = albumArtist });

       
        var songIds = songsToAlbumize.Select(s => s.Id).ToList();
        songRepo.UpdateMany(songIds, songInDb =>
        {
            songInDb.Album = album;
            songInDb.AlbumName = album.Name;
            songInDb.OtherArtistsName = albumArtist.Name;
        });

       
        var updatedSongs = _mapper.Map<List<SongModelView>>(songRepo.Query(s => songIds.Contains(s.Id)));
        _songSource.Edit(updater =>
        {
            updater.RemoveMany(songsToAlbumize);
            updater.AddRange(updatedSongs);
        });
    }

   
   
   

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
               
                if (!songInDb.Tags.Any(t => t.Name.Equals(tagName, StringComparison.OrdinalIgnoreCase)))
                {
                   
                   
                    songInDb.Tags.Add(new TagModel { Name = tagName });
                }
            }
        });

       
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
           
            _searchQuerySubject.OnNext(clause);
        }
        else
        {
           
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
       
       
        var queryWithoutDirectives = Regex.Replace(currentQuery, @"(asc|desc|random|shuffle|first|last)\s*\w*\s*", "", RegexOptions.IgnoreCase).Trim();

        _searchQuerySubject.OnNext($"{queryWithoutDirectives} {sortClause}");
    }


    public ObservableCollection<ActiveFilterViewModel> ActiveFilters { get; } = new();

    private void RebuildAndExecuteQuery()
    {
        var clauses = new List<string>();
        LogicalOperator nextJoiner = LogicalOperator.And;

        foreach (var component in UIQueryComponents)
        {
            if (component is ActiveFilterViewModel filter)
            {
               
                if (clauses.Count!=0)
                {
                    clauses.Add(nextJoiner.ToString().ToLower());
                }
                clauses.Add(filter.TqlClause);
            }
            else if (component is LogicalJoinerViewModel joiner)
            {
               
                nextJoiner = joiner.Operator;
            }
        }

        var fullQueryString = string.Join(" ", clauses);

       
       
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

       
       
        if (fieldDef.Type == FieldType.Boolean && ActiveFilters.Any(f => f.Field == tqlField))
        {
            _logger.LogWarning("Cannot add duplicate unique filter: {Field}", tqlField);
            return;
        }

        string? tqlClause = null;
        string? displayText = null;

       
        switch (fieldDef.Type)
        {
            case FieldType.Text:
                string? value = await Shell.Current.DisplayPromptAsync($"Filter by {fieldDef.PrimaryName}", "Enter the text to search for:");
                if (!string.IsNullOrWhiteSpace(value))
                {
                   
                    string formattedValue = value.Contains(' ') ? $"\"{value}\"" : value;
                    tqlClause = $"{tqlField}:{formattedValue}";
                    displayText = $"{fieldDef.PrimaryName}: {value}";
                }
                break;

            case FieldType.Boolean:
               
                tqlClause = $"{tqlField}:true";
                displayText = fieldDef.Description;
                break;

            case FieldType.Numeric:
            case FieldType.Duration:
               
                string? numValue = await Shell.Current.DisplayPromptAsync($"Filter by {fieldDef.PrimaryName}", "Enter the value (e.g., >2000 or 3:30):");
                if (!string.IsNullOrWhiteSpace(numValue))
                {
                    tqlClause = $"{tqlField}:{numValue}";
                    displayText = $"{fieldDef.PrimaryName} {numValue}";
                }
                break;

            case FieldType.Date:
               
               
                string? dateValue = await Shell.Current.DisplayPromptAsync($"Filter by {fieldDef.PrimaryName}", "Enter a date or range (e.g., today, last month, 2023-12-25):");
                if (!string.IsNullOrWhiteSpace(dateValue))
                {
                    tqlClause = $"{tqlField}:{dateValue}";
                    displayText = $"{fieldDef.PrimaryName}: {dateValue}";
                }
                break;
        }

       
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
    [ObservableProperty] public partial double NewSegmentStart { get; set; }
    [ObservableProperty] public partial double NewSegmentEnd { get; set; }
    [ObservableProperty] public partial string? NewSegmentName { get; set; }

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
            SegmentEndBehavior = SegmentEndBehavior.LoopSegment,
            DurationInSeconds = NewSegmentEnd - NewSegmentStart,
        };

        var createdSegmentModel = songRepo.Create(segmentModel);
        var createdSegmentView = _mapper.Map<SongModelView>(createdSegmentModel);

        _songSource.Add(createdSegmentView);
        IsCreatingSegment = false;

    }

    [RelayCommand]
    public async Task UpdateSongToDB(SongModelView song)
    {
        //this is going to be a bit complex because 
       

        //i'll think of a system of it, i can only think of listening to onpropertychanged to save the latest state.

    }

    public async Task LoadUserLastFMDataAsync()
    {
        ListOfUserRecentTracks = await lastfmService.GetUserRecentTracksAsync(lastfmService.AuthenticatedUser, 50);

        LastFMUserInfo = await lastfmService.GetUserInfoAsync();

        CollectionUserTopTracks = await lastfmService.GetUserTopTracksAsync();

        ListOfUserLovedTracks = await lastfmService.GetLovedTracksAsync();
        ListOfGetTopArtistsChart = await lastfmService.GetTopArtistsChartAsync();
        ListOfSimilarTracks = await lastfmService.GetSimilarAsync(CurrentPlayingSongView.ArtistName, CurrentPlayingSongView.Title);
    }

    [ObservableProperty]
    public partial ObservableCollection<Hqub.Lastfm.Entities.Track>? ListOfUserRecentTracks { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<Hqub.Lastfm.Entities.Track>? ListOfSimilarTracks { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<Hqub.Lastfm.Entities.Artist>? ListOfGetTopArtistsChart { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<Hqub.Lastfm.Entities.Track>? ListOfUserLovedTracks { get; set; }

    [ObservableProperty]
    public partial Hqub.Lastfm.Entities.User? LastFMUserInfo { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<Hqub.Lastfm.Entities.Track>? CollectionUserTopTracks { get; set; }







   

    /// <summary>
    /// Deletes the source files and database entries for a list of selected songs after user confirmation.
    /// </summary>
    [RelayCommand]
    public async Task DeleteSongs(IEnumerable<SongModelView> songsToDelete)
    {
        if (songsToDelete == null || !songsToDelete.Any())
            return;

       
        var confirmed = await _dialogueService.ShowConfirmationAsync(
            "Confirm Deletion",
            $"Are > sure > want to permanently delete {songsToDelete.Count()} song(s)? This will remove the files from >r disk and cannot be undone.",
            "Yes, Delete Them",
            "Cancel");

        if (!confirmed)
            return;

       
        await PerformFileOperationAsync(songsToDelete, string.Empty, FileOperation.Delete);
    }

    /// <summary>
    /// Copies the source files for a list of songs to a new folder.
    /// </summary>
    
    public async Task CopySongs(IEnumerable<SongModelView> songsToCopy, string destinationPath)
    {
        if (songsToCopy == null || !songsToCopy.Any() || string.IsNullOrWhiteSpace(destinationPath))
            return;

       
        await PerformFileOperationAsync(songsToCopy, destinationPath, FileOperation.Copy);
    }

    /// <summary>
    /// Moves the source files for a list of songs to a new folder and updates their paths in the database.
    /// </summary>
    
    public async Task MoveSongs(IEnumerable<SongModelView> songsToMove, string destinationPath)
    {
        if (songsToMove == null || !songsToMove.Any() || string.IsNullOrWhiteSpace(destinationPath))
            return;

        await PerformFileOperationAsync(songsToMove, destinationPath, FileOperation.Move);
    }

   

    /// <summary>
    /// Updates a single song's artist. It intelligently handles whether the new artist
    /// already exists or needs to be created in the database.
    /// </summary>
    /// <param name="songToUpdate">The Song View Model from the UI.</param>
    /// <param name="newArtistName">The desired name of the new artist.</param>
    
    public async Task UpdateSongArtist(SongModelView songToUpdate, string newArtistName)
    {
        if (songToUpdate == null || string.IsNullOrWhiteSpace(newArtistName))
            return;

        using var realm = realmFactory.GetRealmInstance();

        await realm.WriteAsync(() =>
        {
           
            var songInDb = realm.Find<SongModel>(songToUpdate.Id);
            if (songInDb == null)
            {
                _logger.LogWarning("Could not find song with ID {SongId} to update artist.", songToUpdate.Id);
                return;
            }

           
            var existingArtist = realm.All<ArtistModel>().FirstOrDefault(a => a.Name == newArtistName);

            ArtistModel artistToAssign;
            if (existingArtist != null)
            {
               
                _logger.LogInformation("Assigning existing artist '{ArtistName}' to song '{SongTitle}'.", newArtistName, songInDb.Title);
                artistToAssign = existingArtist;
            }
            else
            {
               
                _logger.LogInformation("Creating new artist '{ArtistName}' and assigning to song '{SongTitle}'.", newArtistName, songInDb.Title);
                artistToAssign = new ArtistModel { Name = newArtistName };
                realm.Add(artistToAssign);
            }

           
            songInDb.ArtistToSong.Clear();
            songInDb.ArtistToSong.Add(artistToAssign);
            songInDb.Artist = artistToAssign;
        });

        _logger.LogInformation("Successfully updated artist for song ID {SongId}.", songToUpdate.Id);

       
       
        songToUpdate.ArtistToSong.Clear();
        songToUpdate.ArtistToSong.Add(_mapper.Map<ArtistModelView>(new ArtistModel { Name = newArtistName }));
        songToUpdate.OtherArtistsName = newArtistName;
    }


   

    /// <summary>
    /// Flags a song as "hidden". This requires a change to >r core filtering logic to be effective.
    /// </summary>
    /// <param name="songToBlacklist">The song to hide from the library.</param>
    [RelayCommand]
    public async Task BlacklistSong(SongModelView songToBlacklist)
    {
        if (songToBlacklist == null)
            return;

        using var realm = realmFactory.GetRealmInstance();

        await realm.WriteAsync(() =>
        {
            var songInDb = realm.Find<SongModel>(songToBlacklist.Id);
            if (songInDb != null)
            {
               
                songInDb.IsHidden = true;
            }
        });

       
       
        _logger.LogInformation("Blacklisted song '{Title}'. It will be hidden from view.", songToBlacklist.Title);
    }


    private enum FileOperation { Copy, Move, Delete }

    /// <summary>
    /// Private helper to handle the logic for copying, moving, or deleting song files and updating the database.
    /// </summary>
    private async Task PerformFileOperationAsync(IEnumerable<SongModelView> songs, string destinationPath, FileOperation operation)
    {
        if (operation != FileOperation.Delete && !Directory.Exists(destinationPath))
        {
            _logger.LogError("File operation failed: Destination path '{Path}' does not exist.", destinationPath);
            return;
        }

        List<ObjectId> processedSongIds = new();

        foreach (var song in songs)
        {
            try
            {
                string sourcePath = song.FilePath;
                if (!File.Exists(sourcePath))
                {
                    _logger.LogWarning("Skipping file operation for '{Title}' because source file was not found at '{Path}'.", song.Title, sourcePath);
                    continue;
                }

                if (operation == FileOperation.Delete)
                {
                    File.Delete(sourcePath);
                }
                else
                {
                    string destFileName = Path.Combine(destinationPath, Path.GetFileName(sourcePath));
                    if (operation == FileOperation.Copy)
                    {
                        File.Copy(sourcePath, destFileName, overwrite: true);
                    }
                    else
                    {
                        File.Move(sourcePath, destFileName, overwrite: true);
                    }
                }

                processedSongIds.Add(song.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to perform '{Operation}' for song '{Title}'.", operation, song.Title);
            }
        }

       
        if (processedSongIds.Count==0)
            return;

        using var realm = realmFactory.GetRealmInstance();
        await realm.WriteAsync(() =>
        {
            foreach (var songId in processedSongIds)
            {
                var songInDb = realm.Find<SongModel>(songId);
                if (songInDb == null)
                    continue;

                if (operation == FileOperation.Delete)
                {
                    realm.Remove(songInDb);
                }
                else if (operation == FileOperation.Move)
                {
                   
                    songInDb.FilePath = Path.Combine(destinationPath, Path.GetFileName(songInDb.FilePath));
                }
            }
        });

       
       
        if (operation == FileOperation.Delete)
        {
            _songSource.RemoveMany(songs.Where(s => processedSongIds.Contains(s.Id)));
        }
       
        else if (operation == FileOperation.Move)
        {
            foreach (var song in songs.Where(s => processedSongIds.Contains(s.Id)))
            {
                song.FilePath = Path.Combine(destinationPath, Path.GetFileName(song.FilePath));
            }
        }

        _logger.LogInformation("Successfully completed file operation '{Operation}' for {Count} songs.", operation, processedSongIds.Count);
    }

    [ObservableProperty]
    public partial ObservableCollection<LyricEditingLineViewModel> LyricsInEditor { get; set; }

    [ObservableProperty]
    public partial bool IsLyricEditorActive {get;set;}

    public int _currentLineIndexToTimestamp = 0;

   

    /// <summary>
    /// Takes plain text, splits it into lines, and prepares the editor UI.
    /// </summary>
    [RelayCommand]
    public void StartLyricsEditingSession(string plainText)
    {
        if (string.IsNullOrWhiteSpace(plainText))
            return;

        var lines = plainText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                             .Select(line => new LyricEditingLineViewModel { Text = line.Trim() })
                             .ToList();

        if (lines.Count==0)
            return;

        LyricsInEditor = new ObservableCollection<LyricEditingLineViewModel>(lines);
        _currentLineIndexToTimestamp = 0;
        LyricsInEditor[_currentLineIndexToTimestamp].IsCurrentLine = true;
        IsLyricEditorActive = true;
        _logger.LogInformation("Started lyrics editing session with {Count} lines.", lines.Count);
    }

    /// <summary>
    /// This is the main action button. It grabs the current playback time
    /// and applies it to the current lyric line.
    /// </summary>
    [RelayCommand]
    public void TimestampCurrentLyricLine()
    {
        if (!IsLyricEditorActive || _currentLineIndexToTimestamp >= LyricsInEditor.Count)
        {
            return;
        }

       
        var currentTime = TimeSpan.FromSeconds(audioService.CurrentPosition);
        var timestampString = currentTime.ToString(@"mm\:ss\.ff");

        // 2. Update the ViewModel for the current line
        var currentLine = LyricsInEditor[_currentLineIndexToTimestamp];
        currentLine.Timestamp = $"[{timestampString}]";
        currentLine.IsTimed = true;
        currentLine.IsCurrentLine = false;

        // 3. Move to the next line
        _currentLineIndexToTimestamp++;

        // 4. If there's a next line, mark it as the new current line for the UI
        if (_currentLineIndexToTimestamp < LyricsInEditor.Count)
        {
            LyricsInEditor[_currentLineIndexToTimestamp].IsCurrentLine = true;
        }
        else
        {
            _logger.LogInformation("All lyric lines have been timestamped.");
            // Optionally, > can automatically trigger the save command here.
        }
    }

    /// <summary>
    /// Builds the final LRC content from the editor and saves it using the existing service.
    /// </summary>
    [RelayCommand]
    public async Task SaveTimestampedLyrics()
    {
        if (!IsLyricEditorActive || !LyricsInEditor.Any())
            return;

        var songToUpdate = CurrentPlayingSongView;
        if (songToUpdate == null)
            return;

        var stringBuilder = new StringBuilder();
        foreach (var line in LyricsInEditor.Where(l => l.IsTimed))
        {
            stringBuilder.AppendLine($"{line.Timestamp}{line.Text}");
        }

        var finalLrcContent = stringBuilder.ToString();

        _logger.LogInformation("Saving newly timestamped lyrics for '{Title}'", songToUpdate.Title);

        // Reuse >r existing service!
        // We pass 'null' for the LyricsInfo because the service can parse it from the lrcContent.
        await _lyricsMetadataService.SaveLyricsForSongAsync(songToUpdate, finalLrcContent, null);

        // Clean up the session
        IsLyricEditorActive = false;
        LyricsInEditor.Clear();
    }

    [RelayCommand]
    private void CancelLyricsEditingSession()
    {
        IsLyricEditorActive = false;
        LyricsInEditor?.Clear();
        _logger.LogInformation("Lyrics editing session cancelled.");
    }
    public enum PlaylistEditMode { Add, Remove }

    [ObservableProperty]
    public partial PlaylistEditMode CurrentEditMode { get; set; } = PlaylistEditMode.Add;

    [ObservableProperty]
    public partial string CurrentTqlQuery {get;set;}= "";
    [ObservableProperty]
    public partial string TQLUserSearchErrorMessage { get; private set; }
    [ObservableProperty]
    public partial string InvalidField { get; private set; }
    [ObservableProperty]
    public partial string? NewFieldSuggestion { get; private set; }

    // 2. Create a command that the UI will call when a song is tapped
    [RelayCommand]
    private void HandleSongTap(SongModelView tappedSong)
    {
        if (tappedSong is null)
            return;
        string songTitleQueryPart = $"\"{tappedSong.Title}\"";
        // ---

        string newQuerySegment;
        if (CurrentEditMode == PlaylistEditMode.Add)
        {
            newQuerySegment = $" include title:{songTitleQueryPart}";
        }
        else
        {
            // Use 'exclude' (or 'remove')
            newQuerySegment = $" exclude title:{songTitleQueryPart}";
        }

        // Append to the existing query and update the subject
        var newFullQuery = $"{CurrentTqlQuery}{newQuerySegment}".Trim();

        // Update the property bound to the UI text box
        CurrentTqlQuery = newFullQuery;

        // And push the change into the reactive pipeline
        _searchQuerySubject.OnNext(newFullQuery);
    }

    // 4. A command to toggle the mode
    [RelayCommand]
    private void ToggleEditMode()
    {
        CurrentEditMode = (CurrentEditMode == PlaylistEditMode.Add)
            ? PlaylistEditMode.Remove
            : PlaylistEditMode.Add;
    }
}



