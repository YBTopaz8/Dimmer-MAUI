using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Dimmer.Data.Models;
using Dimmer.DimmerLive.ParseStatics;
using Dimmer.DimmerSearch.TQL.RealmSection;
using Dimmer.Interfaces;
using Dimmer.Interfaces.Services.Interfaces.FileProcessing.FileProcessorUtils;
using Dimmer.Resources.Localization;
using Dimmer.UIUtils;
using Dimmer.Utils;

using DynamicData;
using DynamicData.Binding;

using Hqub.Lastfm.Entities;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Maui.Controls.PlatformConfiguration;

using Parse.LiveQuery;
//using MoreLinq;
//using MoreLinq.Extensions;



using EventHandler = System.EventHandler;


namespace Dimmer.ViewModel;

public partial class BaseViewModel : ObservableObject,  IDisposable
{
    private IDuplicateFinderService _duplicateFinderService;

    

    public BaseViewModel(
        IMapper mapper,
        IDimmerStateService dimmerStateService,
        MusicDataService musicDataService,
        IAppInitializerService appInitializerService,
        IDimmerAudioService audioServ,
        ISettingsService settingsService,
        ILyricsMetadataService lyricsMetadataService,
        SubscriptionManager subsManager,
        LyricsMgtFlow lyricsMgtFlow,
        ICoverArtService coverArtService,
        IFolderMgtService folderMgtService,
        IRepository<SongModel> _songRepo,
        IDuplicateFinderService duplicateFinderService,
        ILastfmService _lastfmService,
        IRepository<ArtistModel> artistRepo,
        IRepository<AlbumModel> albumModel,
        IRepository<GenreModel> genreModel,
        IDialogueService dialogueService,

        ILogger<BaseViewModel> logger)
    {
        Dump();
        _stateService = dimmerStateService ?? throw new ArgumentNullException(nameof(dimmerStateService));
        _dialogueService = dialogueService ?? throw new ArgumentNullException(nameof(dialogueService));
        this.lastfmService = _lastfmService ?? throw new ArgumentNullException(nameof(lastfmService));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        this._musicDataService = musicDataService;
        this.appInitializerService = appInitializerService;
        _subsMgr = subsManager ?? new SubscriptionManager();
        _folderMgtService = folderMgtService;
        this.songRepo = _songRepo;
        _lyricsMetadataService = lyricsMetadataService ?? throw new ArgumentNullException(nameof(lyricsMetadataService));
        this.artistRepo = artistRepo;
        _coverArtService = coverArtService ?? throw new ArgumentNullException(nameof(coverArtService));
        this.albumRepo = albumModel;
        this.genreRepo = genreModel;
        LyricsMgtFlow = lyricsMgtFlow;
        _logger = logger ?? NullLogger<BaseViewModel>.Instance;
        this._audioService = audioServ;
        UserLocal = new UserModelView();
        dimmerPlayEventRepo ??= IPlatformApplication.Current!.Services.GetService<IRepository<DimmerPlayEvent>>()!;
        _playlistRepo ??= IPlatformApplication.Current!.Services.GetService<IRepository<PlaylistModel>>()!;

        _duplicateFinderService = duplicateFinderService;
        libScannerService ??= IPlatformApplication.Current!.Services.GetService<ILibraryScannerService>()!;
        AudioEnginePositionObservable = Observable.FromEventPattern<double>(
            h => audioServ.PositionChanged += h,
            h => audioServ.PositionChanged -= h)
            .Select(evt => evt.EventArgs)
            .StartWith(audioServ.CurrentPosition)
            .Replay(1)
            .RefCount();

        CurrentPlayingSongView = new();
        _baseAppFlow = IPlatformApplication.Current!.Services.GetService<BaseAppFlow>()!;

        folderMonitorService = IPlatformApplication.Current!.Services.GetService<IFolderMonitorService>()!;
        RealmFactory = IPlatformApplication.Current!.Services.GetService<IRealmFactory>()!;


        this.musicRelationshipService = new MusicRelationshipService(RealmFactory);
        this.musicArtistryService = new(RealmFactory);

        this.musicStatsService = new(RealmFactory);


        _searchQuerySubject = new BehaviorSubject<string>("");
        _limiterClause = new BehaviorSubject<LimiterClause?>(null);
        //PlaybackManager = new RuleBasedPlaybackManager();

        PlaybackManager = new RuleBasedPlaybackManager(RealmFactory, _mapper);

        searchResultsHolder.Connect()
            .ObserveOn(RxSchedulers.UI) // Important for UI updates
            .Bind(out _searchResults)
            .Subscribe(x =>
            {
                Debug.WriteLine(x.Count);
            })
            .DisposeWith(CompositeDisposables);


        // 4. Handle Logging. Remove the temporary `if (pos == 0)` check.
        Observable.FromEventPattern<double>(
            h => _audioService.SeekCompleted += h,
            h => _audioService.SeekCompleted -= h)
            .Select(evt => evt.EventArgs)
            .ObserveOn(RxSchedulers.UI)
            .Subscribe(OnSeekCompleted, ex => _logger.LogError(ex, "Error in SeekCompleted subscription"))
            .DisposeWith(_subsManager);
    }

    
    
    [RelayCommand] void Dump() => Debug.WriteLine($"VM: {InstanceId}");
    [ObservableProperty]
    public partial bool IsAppToDate { get; set; }

    private SourceList<DuplicateSetViewModel> _duplicateSource = new();


    private ReadOnlyObservableCollection<DuplicateSetViewModel> _duplicateSets;


    public ReadOnlyObservableCollection<DuplicateSetViewModel> DuplicateSets => _duplicateSets;

    Timer? _bootTimer;

    [ObservableProperty]
    public partial bool IsInitialized { get; set; }
    public void InitializeAllVMCoreComponents()
    {
        if (IsInitialized) return;

        var startTime = DateTime.Now;

        // subscribe to realm's directChanges for certain models to keep in sync

        ReloadFolderPaths();
        var realm = RealmFactory.GetRealmInstance();


        IsLibraryEmpty = !realm.All<SongModel>().Any();
        
        ShowWelcomeScreen = IsLibraryEmpty;
        if(!IsLibraryEmpty)
        {
            ShowAllSongsWindowActivate();
        }    


    

        var realmSub = realm.All<SongModel>()
            .AsObservableChangeSet()

            .Where(changes => changes.Any())
            .Subscribe(
                changes =>
                {
                    // Handle the changes here
                    foreach (var change in changes)
                    {
                        switch (change.Reason)
                        {
                            case ListChangeReason.Add:
                                // Handle addition
                                break;
                            case ListChangeReason.Remove:
                                // Handle removal
                                break;

                            case ListChangeReason.Refresh:
                                break;

                            case
                            ListChangeReason.Moved:
                                break;
                            case ListChangeReason.Clear:
                                break;
                            case ListChangeReason.RemoveRange:
                                break;
                            case ListChangeReason.AddRange:
                                break;
                        }
                    }
                },
                ex => _logger.LogError(ex, "Error observing SongModel changes"))
            .DisposeWith(CompositeDisposables);


        Debug.WriteLine($"{DateTime.Now}: Starting InitializeAllVMCoreComponentsAsync...");

        realm = RealmFactory.GetRealmInstance();
        var folders = realm.All<AppStateModel>().FirstOrDefault()?.Freeze().UserMusicFoldersPreference.ToList();

       
        Debug.WriteLine($"{DateTime.Now}: Folder monitoring started.");

        //SubscribeToCommandEvaluatorEvents();

        _duplicateSource.Connect()
            .ObserveOn(RxSchedulers.UI)
            .Bind(out _duplicateSets)
            .Subscribe(
                x =>
                {
                    OnPropertyChanged(nameof(DuplicateSets)); // Manually notify that the collection has changed
                })
            .DisposeWith(CompositeDisposables);
        Debug.WriteLine($"{DateTime.Now}: Duplicate sets subscription set up.");

        _playbackQueueSource.Connect()
            .ObserveOn(RxSchedulers.UI)
            .Bind(out _playbackQueue)
            .Subscribe(
                x =>
                {
                    OnPropertyChanged(nameof(PlaybackQueue)); // Manually notify that the collection has changed
                })

            .DisposeWith(CompositeDisposables);

        Debug.WriteLine($"{DateTime.Now}: Playback queue subscription set up.");


        _searchQuerySubject
            .Throttle(TimeSpan.FromMilliseconds(380), RxSchedulers.Background)
            .Select(
                query =>
                {
                    // --- DEBUG STEP 2: Is the query being processed? ---
                    Debug.WriteLine($"[DEBUG] Rx Pipeline: Processing query text: '{query}'");

                    var tqlQuery = string.IsNullOrWhiteSpace(query) ? "" : NaturalLanguageProcessor.Process(query);
                    var plan = MetaParser.Parse(tqlQuery);

                    // --- DEBUG STEP 3: What is the parser producing? ---
                    Debug.WriteLine(
                        $"[DEBUG] Parser Plan: RQL='{plan.RqlFilter}', Error='{plan.ErrorMessage ?? "None"}'");

                    return (Query: tqlQuery, Plan: plan);
                })
            .SubscribeOn(RxSchedulers.Background)
            .ObserveOn(RxSchedulers.UI)
            .Subscribe(
                payload =>
                {
                    var plan = payload.Plan;
                    

                        NLPQuery = payload.Query;

                        TQLUserSearchErrorMessage = plan.ErrorMessage ?? "";
                    

                    if (plan.ErrorMessage != null)
                    {
                        //searchResultsHolder.Edit(u => u.Clear());

                        // --- DEBUG STEP 4: Did we stop because of a parse error? ---
                        Debug.WriteLine($"[DEBUG] Halting due to parse error: {plan.ErrorMessage}");
                        return;
                    }

                    try
                    {
                        var realm = RealmFactory.GetRealmInstance();

                        IQueryable<SongModel> query = realm.All<SongModel>().Filter(plan.RqlFilter);

                        // --- DEBUG STEP 5: How many results did Realm return BEFORE sorting? ---
                        Debug.WriteLine(
                            $"[DEBUG] {DateTime.Now} Realm Query: Returned {query.Count()} songs for filter '{plan.RqlFilter}'.");

                        if (plan.SortDescriptions.Count > 0)
                        {
                            var orderByString = string.Join(
                                ", ",
                                plan.SortDescriptions
                                    .Select(
                                        desc => $"{desc.PropertyName} {(desc.Direction == SortDirection.Ascending ? "asc" : "desc")}"));
                            query = query.OrderBy(orderByString);
                        }

                        var resultsFromDb = query.ToList();
                        var mappedSongs = _mapper.Map<List<SongModelView>>(resultsFromDb);
                        var finalSongs = mappedSongs.Where(plan.InMemoryPredicate).ToList();
                        Debug.WriteLine(
                            $"[DEBUG] In-Memory Filter: Reduced result set from {mappedSongs.Count} to {finalSongs.Count} songs.");

                        // STAGE 3: Apply post-processing (Shuffle and Limiters)
                        List<SongModelView> processedSongs;
                        if (plan.Shuffle != null)
                        {
                            processedSongs = QueryResultProcessor.ApplyShuffle(finalSongs, plan.Shuffle);
                        }
                        else
                        {
                            processedSongs = QueryResultProcessor.ApplyLimiter(finalSongs, plan.Limiter);
                        }

                        searchResultsHolder.Edit(
                            updater =>
                            {
                                updater.Clear();
                                updater.AddRange(processedSongs);
                            });

                        if (plan.CommandNode is not null)
                        {
                            var commandAction = commandEvaluator.Evaluate(plan.CommandNode, processedSongs);

                            MainThread.BeginInvokeOnMainThread(
                                () =>
                                {
                                    HandleCommandAction(commandAction);
                                });
                            MainThread.BeginInvokeOnMainThread(() =>
                            {

                                if (CurrentTqlQuery != NLPQuery)
                                {
                                    CurrentTqlQuery = NLPQuery;
                                }
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to execute Realm query.");
                        TQLUserSearchErrorMessage = "Error executing query. Check syntax.";
                        // --- DEBUG STEP 8: Did an exception happen during the query? ---
                        Debug.WriteLine($"[DEBUG] EXCEPTION during Realm query execution: {ex.Message}");
                    }
                },
                ex =>
                {
                    _logger.LogError(ex, "FATAL: Search control pipeline has crashed.");
                    Debug.WriteLine($"[DEBUG] FATAL RX EXCEPTION: {ex.Message}");
                })
            .DisposeWith(CompositeDisposables);

        Debug.WriteLine($"{DateTime.Now}: Search query subscription set up.");

        // Initial search to populate the list
        _searchQuerySubject.OnNext(""); // Confirmed this should be empty string
        _logger.LogInformation(string.Format("{0}: Calculating ranks using RQL sorting...", DateTime.Now));
        // Use a single, large write transaction for performance.


        lastfmService.IsAuthenticatedChanged
            .ObserveOn(RxSchedulers.UI)
            .Subscribe(
                async isAuthenticated =>
                {
                    if (!isAuthenticated)
                        return;
                    LastFMUserInfo = await lastfmService.GetUserInfoAsync();
                    if (LastFMUserInfo is null)
                        return;

                    IsLastfmAuthenticated = isAuthenticated;
                    LastfmUsername = lastfmService.AuthenticatedUser ?? "Not Logged In";
                    if (isAuthenticated)
                    {
                        lastFMCOmpleteLoginBtnVisible = false;
                        LastFMLoginBtnVisible = false;

                        if ((!string.IsNullOrEmpty(lastfmService.AuthenticatedUser)))
                        {
                            UserLocal.Username = lastfmService.AuthenticatedUser;
                            var db = RealmFactory.GetRealmInstance();
                            await db.WriteAsync(
                                () =>
                                {
                                    var usrs = db.All<UserModel>().ToList();
                                    if (usrs is not null && usrs.Count > 0)
                                    {
                                        UserModel usr = usrs.First();
                                        usr.UserName = lastfmService.AuthenticatedUser;
                                        usr.LastFMAccountInfo ??= _mapper.Map<LastFMUser>(LastFMUserInfo);

                                        db.Add(usr, true);
                                    }
                                });
                        }
                    }
                })
            .DisposeWith(CompositeDisposables);


        MyDeviceId = LoadOrGenerateDeviceId();

        
        SubscribeToStateServiceEvents();
        SubscribeToAudioServiceEvents();
        SubscribeToLyricsFlow();


        Debug.WriteLine($"{DateTime.Now}: Subscriptions to services set up.");


        var endTime = DateTime.Now;
        var duration = endTime - startTime;
        Debug.WriteLine(
            $"{DateTime.Now}: Finished InitializeAllVMCoreComponentsAsync in {duration.TotalSeconds} seconds.");
        _ = Task.Run(async () =>
        {
            await OnAppOpening();
            await HeavierBackGroundLoadings(folders);
        });


        IsInitialized = true;
        return;
    }

    private async Task HeavierBackGroundLoadings(List<string>? folders)
    {
        try
        {            
            // 1. Set up watchers immediately (this is now fast)
            await _folderMgtService.StartWatchingConfiguredFoldersAsync();

            // 2. Perform the slow initial scan in the background

            if (folders is not null && folders.Count != 0)
            {
                //_ = await libScannerService.ScanLibrary(folders);
            }
            
            await PerformBackgroundInitializationAsync();
        }
        catch (Exception ex )
        {
            _logger.LogError(ex.Message, ex);
        }
    }

    /// <summary>
    /// Contains all long-running, non-essential initialization tasks that can be performed in the background without
    /// blocking the UI.
    /// </summary>
    public async Task PerformBackgroundInitializationAsync()
    {
        try
        {
            //await ValidateLibraryAsync();
            _logger.LogInformation("Starting background initialization tasks...");

            // Task 1: Check for App Updates (Network I/O)
            //var appUpdateObj = await ParseStatics.CheckForAppUpdatesAsync();
            //if (appUpdateObj is not null)
            //{
            //    // When the background task completes, update UI properties on the main thread.
            //    MainThread.BeginInvokeOnMainThread(() =>
            //    {
            //        AppUpdateObj = appUpdateObj;
            //        IsAppToDate = false;
            //        _stateService.SetCurrentLogMsg(new AppLogModel
            //        {
            //            Log = $"Update available: {appUpdateObj.title}",
            //        });
            //    });
            //}

            var backgroundRealm = RealmFactory.GetRealmInstance();
            //var tempListOfSongs = _mapper.Map<IEnumerable<SongModelView>>(backgroundRealm.All<SongModel>());
            //await EnsureCoverArtCachedForSongsAsync(tempListOfSongs);
            var redoStats = new StatsRecalculator(RealmFactory, _logger);
            redoStats.RecalculateAllStatistics();
            
            //await EnsureAllCoverArtCachedForSongsAsync();

            // Task 2: Update SearchableText for all songs (Heavy CPU/DB work)
            
            
         
            _logger.LogInformation("Finished calculating SearchableText.");

          
            lastfmService.Start();


            _ = Task.Run(LoadLastTenPlayedSongsFromDBToPlayBackQueue);

            
        }
        catch (Exception ex)
        {
            // CRITICAL: Always have a try-catch in a fire-and-forget method.
            // An unhandled exception here would crash your app silently.
            _logger.LogError(ex, "A fatal error occurred during background initialization.");
        }
    }

    public virtual void ShowAllSongsWindowActivate()
    {

    }

    [RelayCommand]
    public void SearchSongForSearchResultHolder(string? searchText)
    {
        if (string.IsNullOrEmpty(searchText))
        {
            CurrentTqlQuery = string.Empty;
            searchText = TQlStaticMethods.PresetQueries.DescAdded();
        }
        if (string.IsNullOrEmpty(searchText))
            return;

        string processedNewText = NaturalLanguageProcessor.Process(searchText);


        if ((searchText.StartsWith("and ", StringComparison.OrdinalIgnoreCase) ||
                searchText.StartsWith("with ", StringComparison.OrdinalIgnoreCase)) &&
            !string.IsNullOrWhiteSpace(searchText))
        {
            CurrentTqlQuery = $"{searchText} {processedNewText}";
        }
        else
        {
            CurrentTqlQuery = processedNewText;
        }

        
        _searchQuerySubject.OnNext(searchText);
    }

    partial void OnCurrentTqlQueryChanged(string oldValue, string newValue)
    {
        CurrentPlaybackQuery = newValue;

    }

    public BehaviorSubject<string> _searchQuerySubject;
    private BehaviorSubject<LimiterClause?> _limiterClause;

    public event EventHandler? AddNextEvent;


    public event EventHandler? ToggleNowPlayingUI;

    public event Action? MainWindowActivatedAction;

    public event EventHandler? MainWindowActivatedEventHandler;

    public event Action? MainWindowDeactivated;

    private void HandleCommandAction(ICommandAction action)
    {
        if (action is null)
            return;

        // Use pattern matching to execute the correct logic
        switch (action)
        {
            case SavePlaylistAction spa:
                // Your actual implementation here
                Debug.WriteLine($"Action: Save playlist '{spa.Name}' with {spa.Songs.Count} songs.");
                AddToPlaylist(spa.Name, spa.Songs.ToList(), CurrentTqlQuery);
                ShowNotification($"Playlist '{spa.Name}' saved.")
                    .FireAndForget(
                        ex =>
                        {
                            _logger.LogError(ex, "Failed to show notification");
                        });
                break;

            case AddToNextAction ana:
                Debug.WriteLine($"Action: Add {ana.Songs.Count} songs to next in queue.");

                AddToNext();


                //_= ShowNotification($"Added {ana.Songs.Count} songs to the queue.");
                break;

            case AddToEndAction aea:
                Debug.WriteLine($"Action: Add {aea.Songs.Count} songs to end of queue.");
                //Application.Current.Windows[0].

                _playbackQueueSource.AddRange(aea.Songs);
                //_= ShowNotification($"Added {aea.Songs.Count} songs to the end of the queue.");
                break;

            case DeleteAllAction daa:
                Debug.WriteLine($"Action: Delete all {daa.Songs.Count} songs in result set.");
                //_musicDataService.AddNoteToSong(daa.Songs);
                break;

            case DeleteDuplicateAction dda:
                Debug.WriteLine($"Action: Delete duplicates from {dda.Songs.Count} songs.");
                // var duplicates = _duplicateFinderService.FindDuplicates(dda.Songs);
                // await _musicDataService.DeleteSongsAsync(duplicates);
                break;

            case UnrecognizedCommandAction uca:
                TQLUserSearchErrorMessage = $"Unknown command: '{uca.CommandName}'";
                break;
            case AddToPositionAction apa:
                Debug.WriteLine($"Action: Add {apa.Songs.Count} songs to queue at position {apa.Position}.");
                int safeIndex = Math.Clamp(apa.Position, 0, _playbackQueueSource.Count);
                _playbackQueueSource.InsertRange(apa.Songs, safeIndex);

                //_= ShowNotification($"Added {apa.Songs.Count} songs to the queue.");
                break;

            case ViewAlbumAction vaa:
                // This requires more complex logic. You need to get the album from the search results.
                var distinctAlbums = searchResultsHolder.Items
                    .Where(s => !string.IsNullOrEmpty(s.AlbumName))
                    .GroupBy(s => s.AlbumName)
                    .Select(g => g.Key)
                    .ToList();

                if (vaa.AlbumIndex < distinctAlbums.Count)
                {
                    var albumToView = distinctAlbums[vaa.AlbumIndex];
                    Debug.WriteLine($"Action: View album '{albumToView}'.");
                    // Now, set the search query to show only this album
                    _searchQuerySubject.OnNext(TQlStaticMethods.PresetQueries.ByAlbum(albumToView));
                }
                break;

            case ScrollToPlayingAction:
                Debug.WriteLine("Action: Scroll to currently playing song.");
                // Your UI logic to find and scroll to the CurrentPlayingSongView would go here.
                // This is often done by telling the ListView/DataGrid to scroll an item into view.
                break;

            case AddIndexedToQueueAction aia:
                // Filter the search results based on the provided indices.
                var songsToAdd = aia.SearchResults.Where((song, index) => aia.Indices.Contains(index)).ToList();

                if (songsToAdd.Count == 0)
                    break;

                Debug.WriteLine($"Action: Add {songsToAdd.Count} indexed songs to '{aia.Position}'.");

                switch (aia.Position)
                {
                    case "next":
                        _playbackQueueSource.InsertRange(songsToAdd, _playbackQueueIndex + 1);
                        //_= ShowNotification($"Added {songsToAdd.Count} songs to next in queue.");
                        break;
                    case "end":
                        _playbackQueueSource.AddRange(songsToAdd);
                        //_= ShowNotification($"Added {songsToAdd.Count} songs to the end of the queue.");
                        break;
                    default:
                        // Try to parse a number for "addto" behavior.
                        if (int.TryParse(aia.Position, out int pos))
                        {
                            _playbackQueueSource.InsertRange(songsToAdd, Math.Max(0, pos - 1));
                            //_= ShowNotification($"Added {songsToAdd.Count} songs to queue at position {pos}.");
                        }
                        else
                        {
                            TQLUserSearchErrorMessage = $"Unknown position for addall: '{aia.Position}'";
                        }
                        break;
                }
                break;

            case NoAction:
                // Command was valid but resulted in no operation (e.g., save with no name).
                // You might want to show a subtle error message.
                TQLUserSearchErrorMessage = "Command requires additional parameters.";
                break;
        }
    }

    private async Task<List<SongModelView>> GetOrderedSongsFromIdsAsync(IEnumerable<ObjectId> songIdsToFetch)
    {
        var ids = songIdsToFetch?.ToList() ?? new List<ObjectId>();
        if (ids.Count == 0)
            return new List<SongModelView>();

        
        string rql = "Id IN {" + string.Join(", ", ids.Select((_, i) => $"${i}")) + "}";

        
        QueryArgument[] args = ids.Select(id => (QueryArgument)id).ToArray();

        
        var songsFromDb = await songRepo.QueryWithRQLAsync(rql, args);

        
        var songMap = songsFromDb.ToDictionary(s => s.Id);
        var orderedSongs = ids
            .Select(id => songMap.TryGetValue(id, out var song) ? song : null)
            .Where(s => s != null)
            .ToList();

        return _mapper.Map<List<SongModelView>>(orderedSongs);
    }






    #region appc cylce  
    public void OnAppClosing()
    {
        var realmm = RealmFactory.GetRealmInstance();
        var appModel = realmm.All<AppStateModel>().ToList();
        if (appModel is not null && appModel.Count > 0)
        {
            var appmodel = appModel[0];
            realmm.Write(
                () =>
                {
                    appmodel.LastKnownQuery = CurrentTqlQuery;
                    appmodel.LastKnownPlaybackQuery = CurrentPlaybackQuery;
                    appmodel.LastKnownPlaybackQueueIndex = _playbackQueueIndex;
                    appmodel.LastKnownShuffleState = IsShuffleActive;
                    appmodel.LastKnownRepeatState = (int)CurrentRepeatMode;
                    appmodel.LastKnownPosition = CurrentTrackPositionSeconds;
                    appmodel.CurrentSongId = CurrentPlayingSongView.Id.ToString();
                    appmodel.VolumeLevelPreference = _audioService.Volume;
                });
            realmm.Add(appmodel, true);
        }
        else
        {
            var newAppModel = new AppStateModel
            {
                LastKnownQuery = CurrentTqlQuery,
                LastKnownPlaybackQuery = CurrentPlaybackQuery,
                LastKnownPlaybackQueueIndex = _playbackQueueIndex,
                LastKnownShuffleState = IsShuffleActive,
                LastKnownRepeatState = (int)CurrentRepeatMode,
                LastKnownPosition = CurrentTrackPositionSeconds,
                CurrentSongId = CurrentPlayingSongView.Id.ToString(),
                VolumeLevelPreference = _audioService.Volume,
                IsDarkModePreference = Application.Current?.UserAppTheme == AppTheme.Dark,
                RepeatModePreference = (int)CurrentRepeatMode,
                ShuffleStatePreference = IsShuffleActive,
                IsStickToTop = IsStickToTop,
                CurrentTheme = Application.Current?.UserAppTheme.ToString() ?? "Unspecified",
                CurrentLanguage = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                CurrentCountry = RegionInfo.CurrentRegion.TwoLetterISORegionName,
            };
            realmm.Write(
                () =>
                {
                    realmm.Add(newAppModel);
                });

        }
    }

    public async Task OnAppOpening()
    {
        try
        {
            var realm = RealmFactory.GetRealmInstance();
            // Use FirstOrDefault() to be safer and slightly more efficient than .ToList()
            var appModel = realm.All<AppStateModel>().FirstOrDefault();

            // get last dimmerevent

            //await LoadLastPlaybackSession();
            var lastAppEvent = realm.All<DimmerPlayEvent>().LastOrDefault();
            if (appModel != null && appModel.Id != ObjectId.Empty)
            {
                
                if (appModel.LastKnownPlaybackQuery is not null)
                {
                    var removeCOmmandFromLastSaved = appModel.LastKnownPlaybackQuery.Replace(">>addnext!", "");

                    removeCOmmandFromLastSaved = Regex.Replace(
                        removeCOmmandFromLastSaved,
                        @">>addto:\d+!",
                        "",
                        RegexOptions.IgnoreCase);
                    removeCOmmandFromLastSaved = Regex.Replace(
                        removeCOmmandFromLastSaved,
                        @">>addto:end!",
                        "",
                        RegexOptions.IgnoreCase);
                    CurrentTqlQuery = removeCOmmandFromLastSaved;
                    removeCOmmandFromLastSaved = Regex.Replace(
                        removeCOmmandFromLastSaved,
                        @">>addnext!",
                        "",
                        RegexOptions.IgnoreCase);
                    CurrentPlaybackQuery = removeCOmmandFromLastSaved;
                }

                _playbackQueueIndex = appModel.LastKnownPlaybackQueueIndex;
                IsShuffleActive = appModel.LastKnownShuffleState;
                CurrentRepeatMode = (RepeatMode)appModel.LastKnownRepeatState;
                CurrentTrackPositionSeconds = appModel.LastKnownPosition;
                DeviceVolumeLevel = appModel.VolumeLevelPreference;
                IsDarkModeOn = appModel.IsDarkModePreference;

                // --- REPLACED: Logic to load the last played song ---
                if (!string.IsNullOrEmpty(appModel.CurrentSongId) &&
                    ObjectId.TryParse(appModel.CurrentSongId, out var songId))
                {
                    // Step 1: Query the database directly for the song by its ID.
                    var songModel = songRepo.GetById(songId); // Assuming GetById is synchronous. If not, make this method async.

                    if (songModel != null)
                    {
                        // Step 2: If found, map the database model to a view model for the UI.
                        CurrentPlayingSongView = songModel.ToModelView();
                    }
                    else
                    {
                        // Step 3: Handle the case where the song was deleted since the last session.
                        _logger.LogWarning(
                            "Last played song with ID {SongId} not found in the database. It may have been deleted.",
                            songId);
                        CurrentPlayingSongView = new();
                    }
                }
                else
                {
                    // Handles cases where no song was playing, or the ID was invalid.
                    CurrentPlayingSongView = new();
                }


                if (IsDarkModeOn)
                {
                    Application.Current?.UserAppTheme = AppTheme.Dark;
                }
                else
                {
                    Application.Current?.UserAppTheme = AppTheme.Light;
                }

                var songsLinked = _mapper.Map<List<SongModelView>>(lastAppEvent.SongsLinkingToThisEvent);
                _playbackQueueSource.Edit(
                    updater =>
                    {
                        updater.Clear();
                        updater.Add(songsLinked);
                    });
            }
            else
            {
                if (lastAppEvent is not null)
                {
                    var lastSong = lastAppEvent.SongsLinkingToThisEvent.FirstOrDefault();
                    if (lastSong != null)
                    {
                        CurrentPlayingSongView = lastSong.ToModelView();
                    }
                    else
                    {
                        CurrentPlayingSongView = new();
                    }
                    CurrentTrackPositionSeconds = lastAppEvent.PositionInSeconds;

                    IsDarkModeOn = Application.Current?.PlatformAppTheme == AppTheme.Dark;

                }
                
            }
        }

        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }


    [RelayCommand]
    public void ToggleAppTheme()
    {
        var currentAppTheme = Application.Current?.UserAppTheme;
        if (currentAppTheme == AppTheme.Dark)
        {
            Application.Current?.UserAppTheme = AppTheme.Light;
        }
        else if (currentAppTheme == AppTheme.Light)
        {
            Application.Current?.UserAppTheme = AppTheme.Dark;
        }
        else if (currentAppTheme == AppTheme.Unspecified)
        {
            Application.Current?.UserAppTheme = AppTheme.Dark;
        }

        //save to db for next boot
        using var realm = RealmFactory.GetRealmInstance();
        realm.Write(
            () =>
            {
                var existingSettings = realm.All<AppStateModel>().ToList();
                if (existingSettings != null)
                {
                    var setting = existingSettings.FirstOrDefault();
                    if (setting != null)
                    {
                        setting.IsDarkModePreference = Application.Current?.UserAppTheme == AppTheme.Dark;
                    }
                    else
                    {
                        setting = new AppStateModel
                        {
                            IsDarkModePreference = Application.Current?.UserAppTheme == AppTheme.Dark
                        };
                    }

                    realm.Add(setting);
                }
            });
        IsDarkModeOn = Application.Current?.UserAppTheme == AppTheme.Dark;
    }


    private async Task ShowNotification(string v) { await Shell.Current.DisplayAlert("Notification", v, "OK"); }
    #endregion



    [RelayCommand]
    private async Task LoadLastPlaybackSession()
    {
        var existingPlaylist = _playlistRepo.FirstOrDefaultWithRQL("PlaylistName == $0", LastSessionPlaylistName);
        if (existingPlaylist?.SongsIdsInPlaylist == null)
        {
            _logger.LogInformation("No previous playback session found.");
            return;
        }

        var restoredQueue = await GetOrderedSongsFromIdsAsync(existingPlaylist.SongsIdsInPlaylist);
        if (restoredQueue.Count == 0)
        {
            _logger.LogWarning(
                "Previous playback session playlist was found but its songs could not be located in the database.");
            return;
        }

        _playbackQueueSource.Edit(
            updater =>
            {
                updater.Clear();
                updater.AddRange(restoredQueue);
            });

        CurrentPlaybackQuery = existingPlaylist.QueryText;


    }

    [RelayCommand]
    public async Task PlayPlaylist(PlaylistModelView? playlist)
    {
        if (playlist?.SongsIdsInPlaylist == null || !playlist.SongsIdsInPlaylist.Any())
        {
            _logger.LogWarning("PlayPlaylist called with a null or empty playlist.");
            return;
        }

        var songsInPlaylist = await GetOrderedSongsFromIdsAsync(playlist.SongsIdsInPlaylist);
        if (songsInPlaylist.Count == 0)
        {
            _logger.LogWarning("Could not find any songs in the database for the selected playlist.");
            return;
        }

        var contextQuery = $"playlist:\"{playlist.PlaylistName}\"";
        await StartNewPlaybackQueue(songsInPlaylist, 0, contextQuery);
    }

    public ObservableCollection<PlaybackRule> Rules { get; } = new();

    public RuleBasedPlaybackManager PlaybackManager { get; }

    #region private fields
    SourceList<SongModelView> searchResultsHolder = new SourceList<SongModelView>();

    private readonly ReadOnlyObservableCollection<SongModelView> _searchResults;
    public ReadOnlyObservableCollection<SongModelView> SearchResults => _searchResults;


    private readonly CommandEvaluator commandEvaluator = new();
    #endregion

    #region private and protected fields
    protected CompositeDisposable CompositeDisposables { get; } = new CompositeDisposable();

    private ICoverArtService _coverArtService;

    public IMapper _mapper;
    private readonly MusicDataService _musicDataService;
    private IAppInitializerService appInitializerService;
    protected IDimmerStateService _stateService;
    protected SubscriptionManager _subsMgr;
    protected IFolderMgtService _folderMgtService;
    private IRepository<SongModel> songRepo;
    private IRepository<ArtistModel> artistRepo;
    private IRepository<PlaylistModel> _playlistRepo;
    private IRepository<AlbumModel> albumRepo;
    private IFolderMonitorService folderMonitorService;
    private IRepository<GenreModel> genreRepo;
    private IRepository<DimmerPlayEvent> dimmerPlayEventRepo;
    public LyricsMgtFlow LyricsMgtFlow;
    private MusicRelationshipService musicRelationshipService;
    private MusicArtistryService musicArtistryService;
    private MusicStatsService musicStatsService;
    protected ILogger<BaseViewModel> _logger;
    private IDimmerAudioService _audioService;
    private ILibraryScannerService libScannerService;


    private readonly CompositeDisposable _subsManager = new CompositeDisposable();


    #endregion


    /// <summary>
    /// Sanitizes the play events in the database by removing duplicate "skipped" events that were logged erroneously
    /// within a short time frame for the same song.
    /// </summary>
    /// <returns>A Task representing the asynchronous operation.</returns>
    public async Task SanitizeSkippedEventsAsync()
    {
        _logger.LogInformation("Starting sanitization of skipped play events...");

        try
        {
            var _realm = RealmFactory.GetRealmInstance();


            var allSkippedEvents =
                _realm.All<DimmerPlayEvent>().Where(e => e.PlayType == 5).ToList();

            if (allSkippedEvents is null || allSkippedEvents.Count == 0)
            {
                _logger.LogInformation("No skipped events found to sanitize.");
                return;
            }


            var skippedEventsBySong = allSkippedEvents
                .Where(e => e.SongId.HasValue)
                .GroupBy(e => e.SongId.Value)
                .ToDictionary(g => g.Key, g => g.OrderBy(e => e.DatePlayed).ToList());

            var eventsToRemove = new List<DimmerPlayEvent>();


            foreach (var songGroup in skippedEventsBySong.Values)
            {
                for (int i = 0; i < songGroup.Count - 1; i++)
                {
                    var currentEvent = songGroup[i];
                    var nextEvent = songGroup[i + 1];


                    if ((nextEvent.DatePlayed - currentEvent.DatePlayed).TotalMinutes < 5)
                    {
                        eventsToRemove.Add(nextEvent);
                    }
                }
            }


            if (eventsToRemove.Count != 0)
            {
                _logger.LogInformation("Found {Count} duplicate skipped events to remove.", eventsToRemove.Count);
                await _realm.WriteAsync(
                    () =>
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


    private ParseClient ParseClientInstance => ParseClient.Instance;

    private ParseLiveQueryClient LiveClient { get; set; }

    public string MyDeviceId { get; set; }


    [RelayCommand]
    public async Task OpenFileInOtherApp(SongModelView? songToView)
    {
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
            await Shell.Current
                .DisplayAlert("Error", "Failed to open file in folder. Please check the file path.", "OK");
        }
    }

    private string LoadOrGenerateDeviceId()
    {
        return DeviceInfo.Name +
            "-" +
            DeviceInfo.Manufacturer +
            "-" +
            DeviceInfo.Model +
            "-" +
            DeviceInfo.VersionString +
            "-" +
            DeviceInfo.Platform.ToString() +
            "-" +
            DeviceInfo.Idiom.ToString() +
            "-" +
            CurrentAppVersion +
            "-" +
            CurrentAppStage +
            "-" +
            DeviceInfo.DeviceType.ToString();
    }


    public ObservableCollection<IQueryComponentViewModel> UIQueryComponents { get; } = new();


    //private BehaviorSubject<LimiterClause?> _limiterClause;

    [RelayCommand]
    public void SmolHold() { SearchSongForSearchResultHolder("Len:<=2:00"); }

    [RelayCommand]
    public void Randomize() { SearchSongForSearchResultHolder("random"); }

    private int _playbackQueueIndex = -1;
    [RelayCommand]
    public void BigHold() { SearchSongForSearchResultHolder("Len:<=3:00"); }

    [RelayCommand]
    public void ResetSearch()
    {
        _searchQuerySubject.OnNext("random");
        CurrentTqlQuery = "random";
    }

    private SourceList<DimmerPlayEventView> _playEventSource = new();
    private CompositeDisposable _disposables = new();
    private IDisposable? _realmSubscription;
    private bool _isDisposed;

    private ILyricsMetadataService _lyricsMetadataService;


    private readonly SourceList<SongModelView> _playbackQueueSource = new();
    private ReadOnlyObservableCollection<SongModelView> _playbackQueue;

    public ReadOnlyObservableCollection<SongModelView> PlaybackQueue => _playbackQueue;

    [ObservableProperty]
    public partial ObservableCollection<LyricPhraseModelView>? AllLines { get; set; }

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


    [ObservableProperty]
    public partial CollectionStatsSummary? SummaryStatsForAllSongs { get; set; }

    [ObservableProperty] public partial int? CurrentTotalSongsOnDisplay { get; set; }

    [ObservableProperty] public partial int? CurrentSortOrderInt { get; set; }

    [ObservableProperty] public partial string? CurrentSortProperty { get; set; } = "Title";

    [ObservableProperty] public partial SortOrder CurrentSortOrder { get; set; } = SortOrder.Asc;

    [ObservableProperty] public partial ObservableCollection<AudioOutputDevice>? AudioDevices { get; set; }

    [ObservableProperty]
    public partial List<string>? SortingModes
    {
        get;
        set;
    } = new List<string> { "Title", "Artist", "Album", "Duration", "Year" };

    [ObservableProperty] public partial AudioOutputDevice? SelectedAudioDevice { get; set; }

    [ObservableProperty] public partial string? SelectedSortingMode { get; set; }

    [ObservableProperty] public partial bool IsAscending { get; set; }

    [ObservableProperty]
    public partial SongModelView? SelectedSongForContext { get; set; }


    [ObservableProperty]
    public partial bool IsDimmerPlaying { get; set; }

    
  

[ObservableProperty]
    public partial bool IsShuffleActive { get; set; }

    [ObservableProperty]
    public partial RepeatMode CurrentRepeatMode { get; set; }

    [ObservableProperty]
    public partial double CurrentTrackPositionSeconds { get; set; }

    [ObservableProperty]
    public partial double SliderPosition { get; set; }

    partial void OnCurrentTrackPositionSecondsChanged(double oldValue, double newValue)
    {
        // a
    }

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

        _audioService.Volume = newVolume;
    }

    [ObservableProperty]
    public partial string AppTitle { get; set; } = "Dimmer";

    public static string CurrentAppVersion = "1.03";
    public static string CurrentAppStage = "Beta";

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
            ATL.Track track = new(newValue.FilePath);

            var imgg = track.EmbeddedPictures?.FirstOrDefault()?.PictureData;
            if (imgg is null)
            {
                return;
            }

            //newValue.CoverImageBytes = ImageResizer.ResizeImage(imgg);
        }
    }

    private IDialogueService _dialogueService;
    protected ILastfmService lastfmService;
    public ILastfmService LastFMService => lastfmService;

    #region audio device management
    public void LoadAllAudioDevices()
    {
        var devices = _audioService.GetAllAudioDevices();
        AudioDevices = new ObservableCollection<AudioOutputDevice>(devices);
        //SelectedAudioDevice = AudioDevices.FirstOrDefault(d => d.IsSource) ?? AudioDevices.FirstOrDefault();
        //if (SelectedAudioDevice != null)
        //{
        //    _audioService.SetPreferredOutputDevice(SelectedAudioDevice);
        //}
    }

    [RelayCommand]
    public void SetPreferredAudioDevice(AudioOutputDevice device)
    {
        if (device == null)
            return;
        _audioService.SetPreferredOutputDevice(device);
        SelectedAudioDevice = device;
        LoadAllAudioDevices();
    }


    [RelayCommand]
    private void LogoutFromLastfm() { lastfmService.Logout(); }
    #endregion


    [RelayCommand]
    public void RefreshSongMetadata(SongModelView songViewModel)
    {
        if (songViewModel == null)
            return;

        Task.Run(
            () =>
            {
                if (RealmFactory is null)
                {
                    _logger.LogError("RealmFactory service is not registered.");
                    return;
                }
                var realm = RealmFactory.GetRealmInstance();
                if (realm is null)
                {
                    _logger.LogError("Failed to get Realm instance from RealmFactory.");
                    return;
                }


                var songDb = realm.Find<SongModel>(songViewModel.Id);
                if (songDb == null)
                {
                    _logger.LogWarning(
                        "evt with ID {SongId} not found in DB. Cannot refresh metadata.",
                        songViewModel.Id);
                    return;
                }


                var artistNamesToLink = songDb.OtherArtistsName
                    .Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries)
                    .ToList();

                if (artistNamesToLink.Count == 0)
                {
                    _logger.LogInformation("No 'OtherArtists' found for song '{Title}'. Nothing to link.", songDb.Title);
                    return;
                }


                var artistClauses = Enumerable.Range(0, artistNamesToLink.Count).Select(i => $"Name == ${i}");


                var artistQueryString = string.Join(" OR ", artistClauses);


                var artistQueryArgs = artistNamesToLink.Select(name => (QueryArgument)name).ToArray();


                var artistsFromDb = realm.All<ArtistModel>()
                    .Filter(artistQueryString, artistQueryArgs)
                    .ToDictionary(a => a.Name);


                realm.Write(
                    () =>
                    {
                        var freshSongDb = realm.Find<SongModel>(songViewModel.Id);
                        if (freshSongDb == null)
                            return;


                        if (freshSongDb.Album == null)
                        {
                            _logger.LogWarning(
                                "evt '{Title}' has no associated album, cannot update album artists.",
                                freshSongDb.Title);
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
                                _logger.LogInformation(
                                    "Linked artist '{ArtistName}' to song '{Title}'.",
                                    artistName,
                                    freshSongDb.Title);


                                bool albumHasArtist = freshSongDb.Album.Artists.Any(a => a.Id == artistModel.Id);
                                if (!albumHasArtist)
                                {
                                    freshSongDb.Album.Artists.Add(artistModel);
                                    _logger.LogInformation(
                                        "Linked artist '{ArtistName}' to album '{AlbumTitle}'.",
                                        artistName,
                                        freshSongDb.Album.Name);
                                }
                            }
                            else
                            {
                                _logger.LogWarning(
                                    "Artist '{ArtistName}' not found in DB. Cannot link to song '{Title}'.",
                                    artistName,
                                    freshSongDb.Title);
                            }
                        }
                    });


                _logger.LogInformation(
                    "Successfully finished refreshing metadata for song ID {SongId}",
                    songViewModel.Id);
            });
    }


    [ObservableProperty]
    public partial bool IsMainViewVisible { get; set; } = true;

    [ObservableProperty]
    public partial CurrentPage CurrentPageContext { get; set; }

    [ObservableProperty]
    public partial Page? CurrentMAUIPage { get; set; }
    partial void OnCurrentMAUIPageChanged(Page? oldValue, Page? newValue)
    {
        if(newValue is not null)
        {
            newValue.BindingContext = this;
        }
    }

    [ObservableProperty]
    public partial SongModelView? SelectedSong { get; set; }


    async partial void OnSelectedSongChanged(SongModelView? oldValue, SongModelView? newValue)
    {
        if (newValue is not null)
        {
            SelectedSecondDomColor = await ImageResizer.GetDomminantMauiColorAsync(newValue.CoverImagePath);
        }

        //LoadSongLastFMData().ConfigureAwait(false);
        //LoadSongLastFMMoreData().ConfigureAwait(false);
    }

    [ObservableProperty]
    public partial Color? SelectedSecondDomColor { get; set; }

    [ObservableProperty]
    public partial byte[]? SelectedSecondSongCoverBytes { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Updates a single song's artist. It intelligently handles whether the new artist already exists or needs to be
    /// created in the database.
    /// </summary>
    /// <param name="songToUpdate">The Song View Model from the UI.</param>
    /// <param name="newArtistName">The desired name of the new artist.</param>


    public async Task UpdateSongArtist(SongModelView songToUpdate, string newArtistName)
    {
        if (songToUpdate == null || string.IsNullOrWhiteSpace(newArtistName))
            return;

        // Let's assume the user can enter multiple artists separated by a semicolon
        var artistNames = newArtistName.Split(';', StringSplitOptions.RemoveEmptyEntries);

        await _musicDataService.UpdateSongArtists(songToUpdate.Id, artistNames);

        // Now, update the local ViewModel for immediate UI feedback
        songToUpdate.ArtistName = string.Join(" | ", artistNames);
    }

    public virtual async Task SaveUserNoteToSong(SongModelView songWithNote)
    {
        var result = await Shell.Current
            .DisplayPromptAsync(
                "Note Text",
                message: $"Note for {Environment.NewLine}" + $"{songWithNote.Title} - {songWithNote.OtherArtistsName}",
                placeholder: "Tip: Just type this note to search this song through TQL :)",
                accept: "Done",
                keyboard: Keyboard.Text);
        if (result == null)
        {
            return;
        }
        if (string.IsNullOrWhiteSpace(result))
        {
            return;
        }

        try
        {
            var addedNote = await _musicDataService.AddNoteToSong(songWithNote.Id, result);

            if (addedNote != null)
            {
                var addedNoteView = _mapper.Map<UserNoteModelView>(addedNote);
                songWithNote.UserNoteAggregatedCol.Add(addedNoteView);

                await Toast.Make($"Note added to {songWithNote.Title}").Show();
                _logger.LogInformation("Successfully added user note for song: {SongTitle}", songWithNote.Title);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save user note for song {SongId}", songWithNote.Id);
        }
    }

    [RelayCommand]
    public async Task PickSongImageFromFolderAsync()
    {
        var result = await FilePicker.Default
            .PickAsync(
                new PickOptions { PickerTitle = "Select an image for the song", FileTypes = FilePickerFileType.Images, });
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


        if (SelectedSong is null)
        {
            _logger.LogWarning("No song is currently selected to update the image.");
            return;
        }
        try
        {
            SelectedSong.CoverImagePath = file;

            var realm = RealmFactory.GetRealmInstance();
            await realm.WriteAsync(
                () =>
                {
                    var existingSong = realm.Find<SongModel>(SelectedSong.Id);
                    if (existingSong is null)
                    {
                        _logger.LogWarning(
                            "Selected song with ID {SongId} not found in Realm database.",
                            SelectedSong.Id);
                        return;
                    }

                    existingSong.CoverImagePath = file;


                    realm.Add(existingSong, update: true);
                });
            _logger.LogInformation("Successfully updated cover image for song '{Title}'", SelectedSong.Title);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update cover image for song '{Title}'", SelectedSong?.Title);
        }
    }

    [ObservableProperty]

    public partial bool IsSearching { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<LrcLibSearchResult>? AllLyricsResultsLrcLib { get; set; }

    [ObservableProperty]
    public partial SongModelView SelectedSongOnPage { get; set; }


    [ObservableProperty]
    public partial bool IsLoadingSongs { get; set; }

    [ObservableProperty]
    public partial bool IsLoadingDashoard { get; set; }

    [ObservableProperty]
    public partial bool IsLoadingArtistDashoard { get; set; }

    [ObservableProperty]
    public partial int SettingsPageIndex { get; set; } = 0;

    [ObservableProperty]
    public partial int ShellTabIndex { get; set; } = 0;

    partial void OnShellTabIndexChanged(int oldValue, int newValue)
    {
        switch (newValue)
        {
            case 0:

                break;

            case 1:
                LoadAllAudioDevices();

                break;
            default:
                break;
        }
    }


    [ObservableProperty]
    public partial ObservableCollection<string> FolderPaths { get; set; } = new();

    private BaseAppFlow _baseAppFlow;

    #region public partials
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
    #endregion


    public string QueryBeforePlay { get; private set; }

    public IRealmFactory RealmFactory;
    private Realm realm;


    [ObservableProperty]
    public partial int MaxDeviceVolumeLevel { get; set; }


    public IObservable<bool> AudioEngineIsPlayingObservable { get; }

    public IObservable<double> AudioEnginePositionObservable { get; }

    public IObservable<double> AudioEngineVolumeObservable { get; }


    [ObservableProperty]
    public partial ObservableCollection<ArtistModelView> AllAvailableArtists { get; set; }

    [ObservableProperty]
    public partial string CurrentCoverImagePath { get; set; }

    private async Task UpdateSongSpecificUi(SongModelView? song)
    {
        try
        {
            if (song is null)
            {
                CurrentTrackDurationSeconds = 1;
                return;
            }

            AppTitle = $"{CurrentAppVersion} - {CurrentAppStage}";
            CurrentTrackDurationSeconds = song.DurationInSeconds > 0 ? song.DurationInSeconds : 1;

            await LoadAndCacheCoverArtAsync(song);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
        }
    }

    private static readonly HttpClient httpClient = new();

    /// <summary>
    /// A robust, multi-stage process to load cover art. It prioritizes existing paths, checks for cached files, and
    /// only extracts from the audio file as a last resort, caching the result for future use.
    /// </summary>
    public async Task LoadAndCacheCoverArtAsync(SongModelView song)
    {
        
        if (song.CoverImagePath == "musicnotess.png")
        {
            song.CoverImagePath = string.Empty;
        }

        if (!string.IsNullOrEmpty(song.CoverImagePath))
        {
            if (File.Exists(song.CoverImagePath))
            {
                CurrentCoverImagePath = song.CoverImagePath;

                return;
            }
        }


        string? finalImagePath = null;
        PictureInfo? embeddedPicture = null;
        try
        {
            if (!File.Exists(song.FilePath)) return;

            embeddedPicture = EmbeddedArtValidator.GetValidEmbeddedPicture(song.FilePath);
            if (embeddedPicture != null)
            {
                finalImagePath = await _coverArtService.SaveOrGetCoverImageAsync(song.Id, song.FilePath, embeddedPicture);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed reading embedded art: {FilePath}", song.FilePath);
            return;
        }

        if (embeddedPicture is null && Connectivity.NetworkAccess == NetworkAccess.Internet)
        {

            _logger.LogTrace("No embedded cover art found in audio file: {FilePath}", song.FilePath);
            //Trying lastfm
            var cleanTitle = StaticUtils.CleanTitle(song.FilePath,song.Title,song.AlbumName,song.ArtistName);
            string cleanArtist = StaticUtils.CleanArtist(song.FilePath, song.ArtistName, song.Title);
            var lastfmTrack = await lastfmService.GetTrackInfoAsync(cleanArtist, cleanTitle);
            if (lastfmTrack.IsNull)
                return;
            if (lastfmTrack.Album is null || lastfmTrack.Album?.Images is null)
                return;
            var imgs = lastfmTrack.Album.Images;
            if (imgs is not null && imgs.Count > 0 && !string.IsNullOrEmpty(imgs.LastOrDefault()?.Url))
            {
                var imageUrl = imgs.LastOrDefault()?.Url;

                    try
                    {
                        
                        var imageBytes = await httpClient.GetByteArrayAsync(imageUrl);
                        embeddedPicture = PictureInfo.fromBinaryData(imageBytes);
                        _logger.LogTrace(
                            "Fetched cover art from Last.fm for {Title} by {Artist}",
                            song.Title,
                            song.ArtistName);
                        finalImagePath = await _coverArtService.SaveOrGetCoverImageAsync(song.Id, song.FilePath, embeddedPicture);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "Failed to fetch or save cover art from Last.fm for {Title} by {Artist}",
                            song.Title,
                            song.ArtistName);
                    }
             
            }
        }


        if (finalImagePath == null)
        {
            _logger.LogTrace("No cover art found or could be saved for {FilePath}", song.FilePath);
            finalImagePath = "musicnotess.png";
        }


        try
        {
            

            _logger.LogTrace("Loaded cover art from new/cached path: {ImagePath}", finalImagePath);


            if (song.CoverImagePath != finalImagePath)
            {                
                song.CoverImagePath = finalImagePath;                
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load or update cover art from final path: {ImagePath}", finalImagePath);
        }
    }


    public async Task EnsureCoverArtCachedForSongsAsync(IEnumerable<SongModelView> songsToProcess)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {

            ProgressCoverArtLoad = new Progress<(int current, int total, SongModelView song)>(p =>
            {
                var (current, total, song) = p;

                _stateService.SetCurrentLogMsg(new AppLogModel()
                {
                    ViewSongModel = song,
                    Log = $"[{current}/{total}] {song.Title} by {song.ArtistName}"
                });
            });
        });
        _logger.LogInformation("Starting to pre-cache cover art for {Count} visible songs.", songsToProcess.Count());

        using var semaphore = new SemaphoreSlim(8);
        int processed = 0;
        int total = songsToProcess.Count();
        var tasks = songsToProcess.Select(async song =>
        {
            await semaphore.WaitAsync();
            try
            {
                if (string.IsNullOrEmpty(song.CoverImagePath) || !File.Exists(song.CoverImagePath))
                {
                    await LoadAndCacheCoverArtAsync(song);
                    int current = Interlocked.Increment(ref processed);
                    ((IProgress<(int, int, SongModelView)>)ProgressCoverArtLoad)
                        .Report((current, total, song));
                }
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);

        _logger.LogInformation("Finished pre-caching cover art process.");
    }

    [ObservableProperty]
    public partial Progress<(int current, int total, SongModelView song)>? ProgressCoverArtLoad { get; set; }
    [RelayCommand]
    public async Task EnsureAllCoverArtCachedForSongsAsync()
    {

        MainThread.BeginInvokeOnMainThread(() =>
        {

            ProgressCoverArtLoad = new Progress<(int current, int total, SongModelView song)>(p =>
            {
                var (current, total, song) = p;

                _stateService.SetCurrentLogMsg(new AppLogModel()
                {
                    ViewSongModel = song,
                    Log = $"[{current}/{total}] {song.Title} by {song.ArtistName}"
                });
            });

        });
        realm = RealmFactory.GetRealmInstance();
        var allSongsFromDb = realm.All<SongModel>();
        var songsToProcess = _mapper.Map<List<SongModelView>>(allSongsFromDb);

        using var semaphore = new SemaphoreSlim(8); // Limit to 8 concurrent operations
        int processed = 0;
        int total = songsToProcess.Count;

        var tasks = songsToProcess.Select(
            async song =>
            {
                await semaphore.WaitAsync();
                try
                {
                    if (string.IsNullOrEmpty(song.CoverImagePath) || !File.Exists(song.CoverImagePath))
                    {
                        _stateService.SetCurrentLogMsg(new AppLogModel()
                        {
                            ViewSongModel = song,
                            Log = $"Cover art on song {song.Title} by {song.ArtistName}"
                        });
                        await LoadAndCacheCoverArtAsync(song);
                        int current = Interlocked.Increment(ref processed);
                        ((IProgress<(int, int, SongModelView)>)ProgressCoverArtLoad)
                            .Report((current, total, song));
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            });

        await Task.WhenAll(tasks);

        _logger.LogInformation("Finished pre-caching ALL cover art process.");
    }

    #region Playback Event Handlers
    private async void OnPlaybackPaused(PlaybackEventArgs args)
    {
        if (args.MediaSong is null)
        {
            _logger.LogWarning("OnPlaybackPaused was called but the event had no song context.");
            return;
        }
        var isAtEnd = Math.Abs(CurrentTrackDurationSeconds - CurrentTrackPositionSeconds) < 0.5;
        if (isAtEnd && CurrentTrackDurationSeconds > 0)
        {
            _logger.LogTrace("Ignoring Paused event at the end of the track, waiting for Completed event.");
            return;
        }

        _logger.LogInformation("AudioService confirmed: Playback paused for '{Title}'", args.MediaSong.Title);
        CurrentPlayingSongView.IsCurrentPlayingHighlight = false;
        await _baseAppFlow.UpdateDatabaseWithPlayEvent(
            RealmFactory,
            args.MediaSong,
            StatesMapper.Map(DimmerPlaybackState.PausedUser),
            CurrentTrackPositionSeconds);

    }

    private async Task OnPlaybackResumed(PlaybackEventArgs args)
    {
        if (args.MediaSong is null)
        {
            _logger.LogWarning("OnPlaybackPaused was called but the event had no song context.");
            return;
        }

        CurrentPlayingSongView.IsCurrentPlayingHighlight = true;
        _logger.LogInformation("AudioService confirmed: Playback resumed for '{Title}'", args.MediaSong.Title);
        await _baseAppFlow.UpdateDatabaseWithPlayEvent(
             RealmFactory,
             args.MediaSong,
             StatesMapper.Map(DimmerPlaybackState.Resumed),
             CurrentTrackPositionSeconds);
    }

    private async Task OnPlaybackEnded()
    {
        _logger.LogInformation(
            "AudioService confirmed: Playback ended for '{Title}'",
            CurrentPlayingSongView?.Title ?? "N/A");
        if (CurrentPlayingSongView == null)
        {
            return;
        }

        CurrentPlayingSongView.IsCurrentPlayingHighlight = false;

        await _baseAppFlow.UpdateDatabaseWithPlayEvent(
            RealmFactory,
            CurrentPlayingSongView,
            StatesMapper.Map(DimmerPlaybackState.PlayCompleted),
            CurrentTrackDurationSeconds);

        CurrentTrackPositionSeconds = 0;
        CurrentTrackPositionPercentage = 0;

        await NextTrackAsync();
    }

    [RelayCommand]
    public async Task CleanseDBOfDupeEvents()
    {
        var realm = RealmFactory.GetRealmInstance();

        await realm.WriteAsync(
            () =>
            {
                var allSongs = realm.All<SongModel>();
                var songEventPairs = allSongs.ToDictionary(
                    song => song.Id,
                    song => song.PlayHistory.OrderBy(e => e.EventDate).ToList());

                // now cleanse each song's events
                var eventsToRemove = new List<DimmerPlayEvent>();
                foreach (var eventList in songEventPairs.Values)
                {
                    for (int i = 0; i < eventList.Count - 1; i++)
                    {
                        var currentEvent = eventList[i];
                        var nextEvent = eventList[i + 1];
                        // Check for duplicate "Played" events within 10 minutes
                        if (currentEvent.PlayType == 1 &&
                            nextEvent.PlayType == 1 &&
                            (nextEvent.EventDate - currentEvent.EventDate).TotalMinutes < 10)
                        {
                            eventsToRemove.Add(nextEvent);
                        }
                        // Check for duplicate "Skipped" events within 5 minutes
                        if (currentEvent.PlayType == 5 &&
                            nextEvent.PlayType == 5 &&
                            (nextEvent.EventDate - currentEvent.EventDate).TotalMinutes < 5)
                        {
                            eventsToRemove.Add(nextEvent);
                        }
                        // Check for duplicate "Paused" events within 5 minutes
                        if (currentEvent.PlayType == 3 &&
                            nextEvent.PlayType == 3 &&
                            (nextEvent.EventDate - currentEvent.EventDate).TotalMinutes < 5)
                        {
                            eventsToRemove.Add(nextEvent);
                        }
                        // Check for duplicate "Resumed" events within 5 minutes
                        if (currentEvent.PlayType == 4 &&
                            nextEvent.PlayType == 4 &&
                            (nextEvent.EventDate - currentEvent.EventDate).TotalMinutes < 5)
                        {
                            eventsToRemove.Add(nextEvent);
                        }
                    }
                }

                foreach (var evt in eventsToRemove)
                {
                    realm.Remove(evt);
                }

                // handle case where events are not linked to any song
                var orphanedEvents = realm.All<DimmerPlayEvent>().Where(e => e.SongId == null);
                // Optionally log or inspect these orphaned events before deletion

                var orphanCount = orphanedEvents.Count();
                if (orphanCount > 0)
                {
                    _logger.LogInformation(
                        "Found {Count} orphaned play events not linked to any song. Removing them.",
                        orphanCount);
                }


                foreach (var orphan in orphanedEvents)
                {
                    realm.Remove(orphan);
                }
                eventsToRemove.AddRange(orphanedEvents);


                foreach (var eventToRemove in orphanedEvents)
                {
                    realm.Remove(eventToRemove);
                }

                _logger.LogInformation("Removed {Count} duplicate play events from the database.", eventsToRemove.Count);
            });

        LoadLastHundredPlayEvents();
    }


    [ObservableProperty]
    public partial bool IsUserDraggingSlider { get; set; }

    private async void OnSeekCompleted(double newPosition)
    {
        try
        {
            _logger.LogInformation("AudioService confirmed: Seek completed to {Position}s.", newPosition);
            await _baseAppFlow.UpdateDatabaseWithPlayEvent(
                RealmFactory,
                CurrentPlayingSongView,
                StatesMapper.Map(DimmerPlaybackState.Seeked),
                newPosition);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
        }
    }

    [RelayCommand]
    private void SliderValueChanged()
    {
        // This command is triggered by the user releasing the drag (DragCompleted)
        // or tapping on the slider track.
        if (!IsUserDraggingSlider) // This ensures it only seeks on tap, not during drag
        {
            SeekTrackPosition(CurrentTrackPositionSeconds);
        }
    }

    private void OnPositionChanged(double positionSeconds)
    {
        CurrentTrackPositionSeconds = positionSeconds;
        CurrentTrackPositionPercentage = (CurrentTrackDurationSeconds > 0
                ? (positionSeconds / CurrentTrackDurationSeconds)
                : 0) *
            100;
    }

    protected virtual async Task OnPlaybackStarted(PlaybackEventArgs args)
    {
        if (args.MediaSong is null)
        {
            _logger.LogWarning("OnPlaybackPaused was called but the event had no song context.");
            return;
        }
        CurrentPlayingSongView.IsCurrentPlayingHighlight = false;

        CurrentPlayingSongView = args.MediaSong;
        _songToScrobble = CurrentPlayingSongView;
        CurrentPlayingSongView.IsCurrentPlayingHighlight = true;


        _logger.LogInformation("AudioService confirmed: Playback started for '{Title}'", args.MediaSong.Title);
        await _baseAppFlow.UpdateDatabaseWithPlayEvent(
            RealmFactory,
            args.MediaSong,
            StatesMapper.Map(DimmerPlaybackState.Playing),
            0);
        await UpdateSongSpecificUi(CurrentPlayingSongView);
        await FindDuplicatesForSongAsync(CurrentPlayingSongView);
    }
    #endregion

    public async Task LogListenEvent()
    {
        SongModelView song = CurrentPlayingSongView;
        string deviceUName = string.Empty;
        if (ParseClientInstance is null)
        {
            return;
        }


        var listenEvent = new ParseObject("ListenEvent")
        {
            //["user"] = _authService.CurrentUser,
            ["deviceUName"] = deviceUName,
            ["songTitle"] = song.Title,
            ["artistName"] = song.ArtistName,
            ["albumName"] = song.AlbumName,
            ["genreName"] = song.GenreName
        };

        try
        {
            // Fire and forget. The client's job is done.
            await listenEvent.SaveAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log listen event to the server.");
            // The client can continue to function. The social feature just missed one event.
        }
    }

    #region Current Playing Song and Color Management
    [ObservableProperty]
    public partial Microsoft.Maui.Graphics.Color? CurrentPlaySongDominantColor { get; set; }

    partial void OnCurrentPlaySongDominantColorChanged(Color? oldValue, Color? newValue)
    {
        
    }

    public virtual void ResetCurrentPlaySongDominantColor()
    {
        
    }

    partial void OnCurrentPlayingSongViewChanging(SongModelView oldValue, SongModelView newValue)
    {
        if(oldValue is not null)
            oldValue.IsCurrentPlayingHighlight = false;
        
    }
    async partial void OnCurrentPlayingSongViewChanged(SongModelView value)
    {
        if (value.Title is null)
            return;

        await ProcessSongChangeAsync(value);
    }

    private void OnFolderScanCompleted(PlaybackStateInfo stateInfo)
    {
        _stateService.SetCurrentLogMsg(new AppLogModel() { Log = "Folder scan completed. Refreshing UI." });
        _logger.LogInformation("Folder scan completed. Refreshing UI.");

        IsAppScanning = false;
        if (stateInfo.ExtraParameter is List<SongModelView> newSongs && newSongs.Count > 0)
        {
            _logger.LogInformation("Adding {Count} new songs to the UI.", newSongs.Count);
            _stateService.SetCurrentLogMsg(new AppLogModel() { Log = $"Adding {newSongs.Count} new songs to the UI.", });
            SearchSongForSearchResultHolder("desc added");
            _ = EnsureCoverArtCachedForSongsAsync(newSongs);

            //var _lyricsCts = new CancellationTokenSource();
            //_ = LoadSongDataAsync(null, _lyricsCts);
        }
        else
        {
            _logger.LogInformation("Scan completed, but no new songs were passed to the UI.");
        }


        ReloadFolderPaths();
    }

    [RelayCommand]
    private void ReloadFolderPaths()
    {
        var realmm = RealmFactory.GetRealmInstance();

        var appModel = realmm.All<AppStateModel>().ToList();
        if (appModel is not null && appModel.Count > 0)
        {
            var appmodel = appModel[0];

            FolderPaths = appmodel.UserMusicFoldersPreference.ToObservableCollection();
        }
    }

    protected virtual async Task ProcessSongChangeAsync(SongModelView value)
    {
        value.IsCurrentPlayingHighlight = true;

        AppTitle = $"{CurrentAppVersion} | {value.Title} - {value.ArtistName}";
        //await LoadSongDominantColorIfNotYetDoneAsync(value);
    }

    #endregion

    #region Playback Commands (User Intent)


    public void LoadLastTenPlayedSongsFromDBToPlayBackQueue()
    {
        if (PlaybackQueue is not null && PlaybackQueue.Count > 0)
            return;

        using (var realm = RealmFactory.GetRealmInstance())
        {

            // Last 10 events that have at least one linked song
            var lastTenEvents = realm.All<DimmerPlayEvent>()
                .Where(e => e.PlayType == 3 && e.SongsLinkingToThisEvent.Any())
                .OrderByDescending(e => e.EventDate)
                .ToList()
                .DistinctBy(x => x.SongId)
                .Take(25);

            foreach (var evt in lastTenEvents)
            {
                var song = evt.SongsLinkingToThisEvent.FirstOrDefault();
                if (song is null) continue;

                var songView = _mapper.Map<SongModelView>(song);
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    _playbackQueueSource.Add(songView);
                });
            }
        }
    }

    #region DashBoard Region
    [RelayCommand]
    public async Task LoadDashboardAsync()
    {

        IsLoadingDashoard = true;
        var endDate = DateTimeOffset.UtcNow.Date.AddDays(1);
        var startDate = endDate.AddDays(-7);

        DashPeriod = $"From {startDate.ToShortDateString()} - {endDate.ToShortDateString()}";
        realm = RealmFactory.GetRealmInstance();
        // Phase 1: Data Preparation
        var scrobblesInPeriod = realm.All<DimmerPlayEvent>()

            .ToList();
        var allSongs = realm.All<SongModel>()

            .AsEnumerable()

            .Select(s => new SongModel
            {
                Id = s.Id,
                Title = s.Title,
                Artist = s.Artist,
                Album = s.Album,
                Genre = s.Genre,
                DurationInSeconds = s.DurationInSeconds,
                ReleaseYear = s.ReleaseYear,
                FirstPlayed = s.FirstPlayed,
                CoverImagePath = s.CoverImagePath,
                FileFormat = s.FileFormat,
                FilePath = s.FilePath,
                OtherArtistsName = s.OtherArtistsName,
                AlbumName = s.AlbumName,
                TrackNumber = s.TrackNumber,
                ArtistName = s.ArtistName,


            }).ToList();
        var allDimmsForSong = _mapper.Map<IEnumerable<DimmerPlayEventView>>(scrobblesInPeriod ?? new List<DimmerPlayEvent>());
        _reportGenerator = new ListeningReportGenerator(allSongs, allDimmsForSong, _logger, _mapper, RealmFactory);
        if (!await _reportGenerator.GenerateReportAsync(startDate, endDate))
            return;

        // --- Summary cards ---
        TotalSongs = _reportGenerator.GetUniqueTracks()?.Count ?? 0;
        TotalDimms = _reportGenerator.GetTotalScrobbles()?.Count ?? 0;
        TopArtist = _reportGenerator.GetTopArtists()?.FirstOrDefault()?.ArtistName ?? "-";
        TopAlbum = _reportGenerator.GetTopAlbums()?.FirstOrDefault()?.AlbumName ?? "-";
        TopGenre = _reportGenerator.GetVariance()?.Value.ToString() ?? "-"; // placeholder, use tags later
        TopPlayTime = $"{_reportGenerator.GetTotalListeningTime()?.Value:F1}h";

        // --- Collections ---
        TopTrackDashBoard = _reportGenerator.GetTopTracks()?.ToObservableCollection();

        IsLoadingArtistDashoard = true;
        var tArtist = _reportGenerator.GetTopArtists()?.ToObservableCollection();
        if (tArtist is not null)
        {
            foreach (var art in tArtist)
            {
                if (art.SongArtist is null) continue;
                art.SongArtist.ImagePath ??= await lastfmService.GetMaxResArtistImageLink(art.SongArtist.Name);
            }
            TopArtistsDash = tArtist;
        }
        IsLoadingArtistDashoard = false;
        //TopAlbumsDash = new ObservableCollection<AlbumModelView>(
        //    _reportGenerator.GetTopAlbums()?.Select(a => a.SongAlbum!) ?? []);

        IsLoadingDashoard = false;
    }



    [ObservableProperty] public partial int TotalSongs { get; set; }
    [ObservableProperty] public partial string DashPeriod { get; set; }
    [ObservableProperty] public partial int TotalDimms { get; set; }
    [ObservableProperty] public partial string TopArtist { get; set; } = string.Empty;
    [ObservableProperty] public partial string TopAlbum { get; set; } = string.Empty;
    [ObservableProperty] public partial string TopGenre { get; set; } = string.Empty;
    [ObservableProperty] public partial string TopPlayTime { get; set; } = string.Empty;

    [ObservableProperty] public partial ObservableCollection<DimmerStats?>? TopTrackDashBoard { get; set; } = new();
    [ObservableProperty] public partial ObservableCollection<DimmerStats?>? TopArtistsDash { get; set; } = new();
    [ObservableProperty] public partial ObservableCollection<DimmerStats?>? TopAlbumsDash { get; set; } = new();

    #endregion

    #region Queue Manipulation Commands

    /// <summary>
    /// Add list of songs to next playing
    /// </summary>
    /// <param name="songs"></param>
    [RelayCommand]
    public void AddToNext(IEnumerable<SongModelView>? songs = null)
    {

        songs ??= _searchResults;
        if (CurrentPlayingSongView.Title == null)
        {
            _playbackQueueSource.AddRange(songs);
            return;
        }

        var CurrentSongIndex = _playbackQueue.IndexOf(CurrentPlayingSongView);
        _playbackQueueSource.InsertRange(songs, CurrentSongIndex);
    }

    [RelayCommand]
    public void RemoveRemainingSongsFromQueue()
    {
        if (_playbackQueueIndex < 0 || _playbackQueueIndex >= _playbackQueueSource.Items.Count - 1)
            return;
        int itemsToRemove = _playbackQueueSource.Items.Count - (_playbackQueueIndex + 1);
        for (int i = 0; i < itemsToRemove; i++)
        {
            _playbackQueueSource.RemoveAt(_playbackQueueSource.Items.Count - 1);
        }
    }


    [RelayCommand]
    public void GroupSongsInQueueByArtist()
    {
        if (_playbackQueueSource.Items.Count < 2)
            return;
        var grouped = _playbackQueueSource.Items
            .OrderBy(s => s.ArtistName)
            .ThenBy(s => s.AlbumName)
            .ThenBy(s => s.DiscNumber)
            .ThenBy(s => s.TrackNumber)
            .ToList();
        _playbackQueueSource.Clear();
        _playbackQueueSource.AddRange(grouped);
    }


    [RelayCommand]
    public void GroupSongsInQueueByGenre()
    {
        if (_playbackQueueSource.Items.Count < 2)
            return;
        var grouped = _playbackQueueSource.Items
            .OrderBy(s => s.GenreName)
            .ThenBy(s => s.ArtistName)
            .ThenBy(s => s.AlbumName)
            .ThenBy(s => s.DiscNumber)
            .ThenBy(s => s.TrackNumber);
        _playbackQueueSource.Clear();
        _playbackQueueSource.AddRange(grouped);
    }

    [RelayCommand]
    public void GroupSongsInQueueByAlbum()
    {
        if (_playbackQueueSource.Items.Count < 2)
            return;
        var grouped = _playbackQueueSource.Items
            .OrderBy(s => s.AlbumName)
            .ThenBy(s => s.DiscNumber)
            .ThenBy(s => s.TrackNumber);
        _playbackQueueSource.Clear();
        _playbackQueueSource.AddRange(grouped);
    }

    [RelayCommand]
    public void OrderQueueByTitle()
    {
        var isCurrentAscending = IsAscending;
        if (_playbackQueueSource.Items.Count < 2)
            return;
        var ordered = IsAscending
            ? _playbackQueueSource.Items.OrderBy(s => s.Title)
            : _playbackQueueSource.Items.OrderByDescending(s => s.Title);
        _playbackQueueSource.Clear();
        _playbackQueueSource.AddRange(ordered);
        IsAscending = !IsAscending;
    }

    [RelayCommand]
    public void OrderQueueByArtist()
    {
        if (_playbackQueueSource.Items.Count < 2)
            return;
        var ordered = IsAscending
            ? _playbackQueueSource.Items.OrderBy(s => s.ArtistName)
            : _playbackQueueSource.Items.OrderByDescending(s => s.ArtistName);
        _playbackQueueSource.Clear();
        _playbackQueueSource.AddRange(ordered);
        IsAscending = !IsAscending;
    }

    [RelayCommand]
    public void OrderQueueByAlbum()
    {
        if (_playbackQueueSource.Items.Count < 2)
            return;
        var ordered = IsAscending
            ? _playbackQueueSource.Items.OrderBy(s => s.AlbumName)
            : _playbackQueueSource.Items.OrderByDescending(s => s.AlbumName);
        _playbackQueueSource.Clear();
        _playbackQueueSource.AddRange(ordered);
        IsAscending = !IsAscending;
    }


    [RelayCommand]
    public async Task RemoveFromQueue(SongModelView song)
    {
        if (song == null || !_playbackQueueSource.Items.Contains(song))
            return;
        int songIndex = _playbackQueueSource.Items.IndexOf(song);
        _playbackQueueSource.RemoveAt(songIndex);
        if (songIndex < _playbackQueueIndex)
        {
            _playbackQueueIndex--;
        }
        else if (songIndex == _playbackQueueIndex)
        {
            if (_audioService.IsPlaying)
            {
                _audioService.Stop();
            }
            if (_playbackQueueSource.Items.Count > 0)
            {
                if (_playbackQueueIndex >= _playbackQueueSource.Items.Count)
                {
                    _playbackQueueIndex = 0;
                }
                _ = PlaySongAtIndexAsync(_playbackQueueIndex);
            }
            else
            {
                _playbackQueueIndex = -1;
                await UpdateSongSpecificUi(null);
            }
        }
        //RemoveFromQueueEvent?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    public async Task ClearQueue()
    {
        if (_audioService.IsPlaying)
        {
            _audioService.Stop();
        }
        _playbackQueueSource.Clear();
        _playbackQueueIndex = -1;
        await UpdateSongSpecificUi(null);
        //ClearQueueEvent?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    public async Task RemoveManyFromQueue(IEnumerable<SongModelView> songs)
    {
        if (songs == null || !songs.Any())
            return;
        var songsList = songs;
        foreach (var song in songsList)
        {
            if (_playbackQueueSource.Items.Contains(song))
            {
                int songIndex = _playbackQueueSource.Items.IndexOf(song);
                _playbackQueueSource.RemoveAt(songIndex);
                if (songIndex < _playbackQueueIndex)
                {
                    _playbackQueueIndex--;
                }
                else if (songIndex == _playbackQueueIndex)
                {
                    if (_audioService.IsPlaying)
                    {
                        _audioService.Stop();
                    }
                    if (_playbackQueueSource.Items.Count > 0)
                    {
                        if (_playbackQueueIndex >= _playbackQueueSource.Items.Count)
                        {
                            _playbackQueueIndex = 0;
                        }
                        _ = PlaySongAtIndexAsync(_playbackQueueIndex);
                    }
                    else
                    {
                        _playbackQueueIndex = -1;
                        await UpdateSongSpecificUi(null);
                    }
                }
            }
        }
    }

    #endregion

    /// <summary>
    /// The single, core method responsible for playing a song. Handles all validation, audio service interaction, and
    /// UI updates.
    /// </summary>
    /// <param name="songToPlay">The song to attempt to play.</param>
    /// <returns>True if playback started successfully, otherwise false.</returns>
    private async Task<bool> PlayInternalAsync(SongModelView? songToPlay)
    {
        // A. Stop current playback and clear UI if the new song is null.
        if (songToPlay == null)
        {
            if (_audioService.IsPlaying)
                _audioService.Stop();
            await UpdateSongSpecificUi(null);
            return false;
        }

        // B. Validate the song file path.
        if (string.IsNullOrEmpty(songToPlay.FilePath) || !File.Exists(songToPlay.FilePath))
        {
            _logger.LogError("Song file not found for '{Title}'.", songToPlay.Title);
            await ValidateSongAsync(songToPlay);
            return false; // Playback failed
        }

        try
        {
            // C. Stop any currently playing audio.
            if (_audioService.IsPlaying)
            {
                _audioService.Stop();
            }

            // D. Determine the start position. Reset if it's a new song.
            double startPosition = 0;
            if (songToPlay.TitleDurationKey == CurrentPlayingSongView?.TitleDurationKey)
            {
                startPosition = CurrentTrackPositionSeconds;
            }

            // E. Initialize the audio service with the new track.
            await _audioService.InitializeAsync(songToPlay, startPosition);

            // F. Update history and UI state *after* successful initialization.
            PlaybackManager.AddSongToHistory(songToPlay);

            return true; // Playback started successfully
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to play song '{Title}'.", songToPlay.Title);
            return false; // Playback failed
        }
    }

    [ObservableProperty]
    public partial CurrentPage CurrentPagePlayingSong { get; set; }

    [RelayCommand]
    private async Task PlayFromSearchResultsAsync(SongModelView? songToPlay)
    {
        if (songToPlay == null)
            return;

        // We pass the full search results as the source for the new queue.
        await PlaySong(songToPlay, Utilities.Enums.CurrentPage.AllSongs, _searchResults);
    }

    public async Task PlaySong(
        SongModelView? songToPlay,
        CurrentPage curPage = CurrentPage.AllSongs,
        IEnumerable<SongModelView>? songs = null)
    {
        try
        {

            if (songToPlay == null)
                return;



            Debug.WriteLine("PlaySong invoked for: " + songToPlay.Title);
            // Quick exit check. The more detailed check is in PlayInternalAsync.
            if (string.IsNullOrEmpty(songToPlay.FilePath) || !File.Exists(songToPlay.FilePath))
            {
                _logger.LogError("Song file not found for '{Title}'.", songToPlay.Title);
                await ValidateSongAsync(songToPlay);
                return;
            }

            CurrentPagePlayingSong = curPage;

            var sourceList = new List<SongModelView>();
            int startIndex = -1;

            if (CurrentPagePlayingSong == Utilities.Enums.CurrentPage.AllSongs)
            {
                var songSource = (songs ?? _searchResults).ToList();
                startIndex = songSource.FindIndex(s => s.Id == songToPlay.Id);

                if (startIndex == -1)
                {
                    _logger.LogWarning("Song '{Title}' not in current source. Playing it alone.", songToPlay.Title);
                    sourceList.Add(songToPlay);
                    startIndex = 0;
                }
                else
                {
                    // *** SIMPLIFIED QUEUE SLICING LOGIC ***
                    // Take a window of 150 songs (50 before, 100 after) for performance.
                    const int songsToTakeBefore = 100;
                    const int songsToTakeAfter = 300;
                    const int totalQueueSize = songsToTakeBefore + 1 + songsToTakeAfter;

                    int sliceStart = Math.Max(0, startIndex - songsToTakeBefore);
                    sourceList = songSource.Skip(sliceStart).Take(totalQueueSize).ToList();

                    // The start index is now relative to this new, smaller list.
                    startIndex = sourceList.IndexOf(songToPlay);
                }
            }
            else if (CurrentPagePlayingSong == Utilities.Enums.CurrentPage.HomePage)
            {
                if (songs is null)
                {
                    sourceList = _playbackQueue.ToList();
                    startIndex = sourceList.IndexOf(songToPlay);

                    if (startIndex == -1)
                    {
                        _logger.LogWarning("Song '{Title}' not in current queue. Playing it alone.", songToPlay.Title);
                        sourceList = new List<SongModelView> { songToPlay };
                        startIndex = 0;
                    }
                }
                else
                {
                    sourceList = songs.ToList();
                    startIndex = sourceList.IndexOf(songToPlay);
                    if (startIndex == -1)
                    {
                        _logger.LogWarning("Song '{Title}' not in provided source. Playing it alone.", songToPlay.Title);
                        sourceList = new List<SongModelView> { songToPlay };
                        startIndex = 0;
                    }
                }


            }
            else
            {
                if (songs is null) return;
                sourceList = songs.ToList();
                startIndex = sourceList.IndexOf(songToPlay);
                if (startIndex == -1)
                {
                    _logger.LogWarning("Song '{Title}' not in provided source. Playing it alone.", songToPlay.Title);
                    sourceList = new List<SongModelView> { songToPlay };
                    startIndex = 0;
                }
            }


            Debug.WriteLine(
                    "Starting new playback queue with " + sourceList.Count + " songs, starting at index " + startIndex);
            if (startIndex == -1)
            {
                _logger.LogWarning("Song not found in current source. Playing standalone.");
                await PlayInternalAsync(songToPlay);
                return; // prevent invalid queue creation
            }

            await StartNewPlaybackQueue(sourceList, startIndex, CurrentTqlQuery);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message, ex);
        }
    }
    /// <summary>
    /// Plays a song from a specific context, like an album or a user-created playlist. This creates a new, smaller
    /// queue containing only the songs from that specific collection.
    /// </summary>
    [RelayCommand]
    private async Task PlayFromSpecificCollectionAsync(SongModelView? songToPlay)
    {
        try
        {


            if (songToPlay == null)
                return;

            // Example for playing from a specific album's song list.
            // You would have a similar property for a selected playlist's songs.
            await PlaySong(songToPlay, CurrentPage.SpecificAlbumPage, _searchResults);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message, ex);
        }
    }


    private async Task StartNewPlaybackQueue(IEnumerable<SongModelView> songs, int startIndex, string contextQuery)
    {
        List<SongModelView> initialSongList = songs.ToList();
        if (initialSongList.Count == 0 || startIndex < 0 || startIndex >= initialSongList.Count)
        {
            _logger.LogError("Could not start playback. Invalid songs list or start index.");

            _playbackQueueSource.Clear();

            await PlayInternalAsync(null); // Stop playback

            return;
        }


        List<SongModelView> finalQueue;
        int finalStartIndex;


        if (IsShuffleActive)
        {
            var songToStartWith = initialSongList[startIndex];
            var otherSongs = initialSongList.Where(s => s.Id != songToStartWith.Id);

            // The collection expression is fine for creating a temporary List<T>
            finalQueue = [songToStartWith, .. otherSongs.OrderBy(x => _random.Next())];
            finalStartIndex = 0;
        }
        else
        {
            finalQueue = initialSongList;
            finalStartIndex = startIndex;
        }
        if (CurrentPagePlayingSong != CurrentPage.HomePage)
        {

            _playbackQueueSource.Edit(
                updater =>
                {
                    updater.Clear();
                    updater.AddRange(finalQueue);
                });

        }
        // The index is a separate variable, we can set it directly.
        _playbackQueueIndex = finalStartIndex;

        // Update context and clear history for the new session
        CurrentPlaybackQuery = contextQuery;
        SavePlaybackContext(CurrentPlaybackQuery);
        PlaybackManager.ClearSessionHistory();

        // Now, attempt to play the first song in the newly created queue.
        // We can safely read from the public _playbackQueue now, as it has been updated by the source.
        var songToPlay = finalQueue[finalStartIndex];

        if (!await PlayInternalAsync(songToPlay))
        {
            await NextTrackAsync();
        }
    }

    private async Task PlaySongAtIndexAsync(int index)
    {
        if (_playbackQueue == null || index < 0 || index >= _playbackQueue.Count)
        {
            _logger.LogInformation("Playback stopped: Index {Index} is out of bounds.", index);
            await PlayInternalAsync(null); // Stop playback
            return;
        }

        _playbackQueueIndex = index;
        var songToPlay = _playbackQueue[_playbackQueueIndex];

        // If playback fails (e.g., file deleted), automatically skip to the next track.
        if (!await PlayInternalAsync(songToPlay))
        {
            var result = await UiDialogs.SafeDisplayActionSheetAsync(
                        "Failed to play the selected song. What would you like to do?",
                        "Cancel",
                        null,
                        "Remove from Queue",
                        "Skip to Next");

            if (result == "Remove from Queue")
            {
                await RemoveFromQueue(songToPlay);
            }
            else if (result == "Skip to Next")
            {
                await NextTrackAsync();
            }
            else
            {
                // User cancelled or closed the dialog
                _logger.LogInformation("User cancelled action after playback failure for '{Title}'.", songToPlay.Title);
            }

            await NextTrackAsync();
        }
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
    /// <summary>
    /// Jumps to a song that is already in the Now Playing queue. This does NOT create a new queue; it just changes the
    /// current track index.
    /// </summary>
    [RelayCommand]
    private async Task JumpToSongInQueueAsync(SongModelView? songToPlay)
    {
        if (songToPlay == null)
            return;

        int index = PlaybackQueue.IndexOf(songToPlay);
        if (index != -1)
        {
            await PlaySongAtIndexAsync(index);
        }
        else
        {
            // Fallback: If for some reason the song isn't in the queue,
            // add it next and play it.
            _logger.LogWarning(
                "Song '{Title}' was not found in the current queue. Adding it to play next.",
                songToPlay.Title);
            // You would need an "Add Next" method here.
            // For now, let's just play it as a single-song queue.
            await PlaySong(songToPlay, CurrentPage.NowPlayingPage, new List<SongModelView> { songToPlay });
        }
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


            _playbackQueueSource.Edit(
                updater =>
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
        if (!_audioService.IsPlaying && CurrentPlayingSongView?.Title == null)
        {
            var firstSong = _searchResults.FirstOrDefault();
            if (firstSong != null)
            {
                await PlaySong(firstSong);
            }
            return;
        }


        if (_audioService.IsPlaying)
        {
            _audioService.Pause();
        }
        else
        {
            if (_audioService.CurrentTrackMetadata is null)
            {
                await _audioService.InitializeAsync(CurrentPlayingSongView, CurrentTrackPositionSeconds);
                return;
            }
            _audioService.Play(CurrentTrackPositionSeconds);
        }
    }
    public Guid InstanceId { get; } = Guid.NewGuid();

    [ObservableProperty]
    public partial bool ScrobbleOnSkip { get; set; } = true;

    [ObservableProperty]
    public partial bool ScrobbleOnCompletion { get; set; } = true;

    [ObservableProperty]
    public partial bool ScrobbleOnStart { get; set; } = false;


    [RelayCommand]
    public async Task NextTrackAsync()
    {
        if (IsDimmerPlaying && CurrentPlayingSongView != null && CurrentTrackPositionPercentage < 90)
        {
            await _baseAppFlow.UpdateDatabaseWithPlayEvent(
                RealmFactory,
                CurrentPlayingSongView,
                StatesMapper.Map(DimmerPlaybackState.Skipped),
                CurrentTrackPositionSeconds);
            if (ScrobbleOnCompletion)
            {
                if (IsDimmerPlaying && _songToScrobble != null && IsLastfmAuthenticated)
                {
                    await lastfmService.ScrobbleAsync(_songToScrobble);
                }
                _stateService.SetCurrentLogMsg(new AppLogModel() { Log = "Ended manually", ViewSongModel = CurrentPlayingSongView });
            }
        }
        else if (IsDimmerPlaying && CurrentPlayingSongView != null && CurrentTrackPositionPercentage >= 90)
        {
            await _baseAppFlow.UpdateDatabaseWithPlayEvent(
                RealmFactory,
                CurrentPlayingSongView,
                StatesMapper.Map(DimmerPlaybackState.PlayCompleted),
                CurrentTrackDurationSeconds);
            if (ScrobbleOnCompletion)
            {
                if (IsDimmerPlaying && _songToScrobble != null && IsLastfmAuthenticated)
                {
                    await lastfmService.ScrobbleAsync(_songToScrobble);
                }
                _stateService.SetCurrentLogMsg(new AppLogModel() { Log = "Ended automatically" });
            }
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
    { this.Rules.Add(new PlaybackRule { Priority = this.Rules.Count + 1, Query = "any:" }); }

    [RelayCommand]
    private void RemovePlaybackRule(PlaybackRule rule)
    {
        if (rule != null)
        {
            this.Rules.Remove(rule);
        }
    }

    [RelayCommand]
    public async Task PreviousTrack()
    {
        if (_audioService.CurrentPosition > 3)
        {
            _audioService.Seek(0);
            return;
        }
        if (IsDimmerPlaying && CurrentPlayingSongView != null)
        {
            await _baseAppFlow.UpdateDatabaseWithPlayEvent(
                  RealmFactory,
                  CurrentPlayingSongView,
                  StatesMapper.Map(DimmerPlaybackState.Skipped),
                  CurrentTrackPositionSeconds);
        }
        if (IsDimmerPlaying && _songToScrobble != null && IsLastfmAuthenticated)
        {
            await lastfmService.ScrobbleAsync(_songToScrobble);
        }
        var prevIndex = GetNextIndexInQueue(-1);
        await PlaySongAtIndexAsync(prevIndex);
    }

    #region Private Playback Helper Methods

    private const string LastSessionPlaylistName = "__dimmerSessionPlayback";


    private void SavePlaybackContext(string query)
    {
        // --- FIX #1: Query by the CONSTANT name ---
        var existingPlaylist = _playlistRepo.FirstOrDefaultWithRQL("PlaylistName == $0", LastSessionPlaylistName);

        // Now, 'existingPlaylist' will correctly find the single "Last Session" playlist if it exists.

        if (existingPlaylist != null)
        {
            // This playlist already exists, so we update it.
            _logger.LogInformation("Found existing session playlist. Updating it.");

            _playlistRepo.Update(
                existingPlaylist.Id,
                playlistInDb =>
                {
                    // Update the properties
                    playlistInDb.LastPlayedDate = DateTimeOffset.UtcNow;
                    playlistInDb.QueryText = query; // Update the query text

                    // Clear the old song list and add the new one
                    playlistInDb.SongsIdsInPlaylist.Clear();
                    foreach (var song in _playbackQueue)
                    {
                        playlistInDb.SongsIdsInPlaylist.Add(song.Id);
                    }

                    // You might want to add a new event or clear and add one.
                    // Let's assume you add a new one each time context is saved.
                    playlistInDb.PlayHistory.Add(new PlaylistEvent());
                });
        }
        else
        {
            // The playlist doesn't exist, so we create it for the first time.
            _logger.LogInformation("No existing session playlist found. Creating a new one.");

            var contextPlaylist = new PlaylistModel
            {
                // Id will be generated by the repo if not provided
                PlaylistName = LastSessionPlaylistName, // The constant name
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

            // Use Create, not Upsert, as we've already established it's a new entity.
            _playlistRepo.Create(contextPlaylist);
        }

        QueryBeforePlay = query;
        _logger.LogInformation("Saved playback context for query: \"{query}\"", query);
    }


    [RelayCommand]
    public void ToggleRepeatMode()
    {
        CurrentRepeatMode = (RepeatMode)(((int)CurrentRepeatMode + 1) % Enum.GetNames(typeof(RepeatMode)).Length);


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

    [RelayCommand]
    public void MoveSongInQueue(Tuple<int, int> fromToIndices)
    {
        int fromIndex = fromToIndices.Item1;
        int toIndex = fromToIndices.Item2;

        if (fromIndex < 0 ||
            fromIndex >= _playbackQueueSource.Count ||
            toIndex < 0 ||
            toIndex >= _playbackQueueSource.Count)
            return;

        // Use the built-in Move method for efficient reordering
        var songToMove = _playbackQueueSource.Items[fromIndex];
        _playbackQueueSource.RemoveAt(fromIndex);
        _playbackQueueSource.Insert(toIndex, songToMove);

        // Important: Update the current playing index if it was affected by the move
        if (_playbackQueueIndex == fromIndex)
        {
            _playbackQueueIndex = toIndex;
        }
        else if (fromIndex < _playbackQueueIndex && toIndex >= _playbackQueueIndex)
        {
            _playbackQueueIndex--;
        }
        else if (fromIndex > _playbackQueueIndex && toIndex <= _playbackQueueIndex)
        {
            _playbackQueueIndex++;
        }
    }

    public void MoveSongInQueue(int oldIndex, int newIndex)
    {
        if (oldIndex < 0 ||
            oldIndex >= _playbackQueue.Count ||
            newIndex < 0 ||
            newIndex >= _playbackQueue.Count ||
            oldIndex == newIndex)
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

        _sleepTimerSubscription = Observable.Timer(duration, RxSchedulers.UI)
            .Subscribe(
                _ =>
                {
                    _logger.LogInformation("Sleep timer expired. Pausing playback.");
                    if (IsDimmerPlaying)
                    {
                        _audioService.Pause();
                    }
                    IsSleepTimerActive = false;
                });
    }


    /// <summary>
    /// Inserts a single song to play immediately after the current one. If the queue is empty, it starts playback with
    /// this song.
    /// </summary>
    [RelayCommand]
    public void PlayNext(SongModelView? song)
    {
        if (song == null)
            return;

        _playbackQueueSource.Insert(_playbackQueueIndex + 1, song);
    }

    /// <summary>
    /// Inserts a list of songs to play immediately after the current one. If the queue is empty, it starts playback
    /// with this new list.
    /// </summary>
    public async Task PlayNextSongsImmediately(IEnumerable<SongModelView>? songs)
    {
        if (songs == null || !songs.Any())
            return;

        var distinctSongs = songs.Distinct().ToList();


        if (!_playbackQueue.Any() || CurrentPlayingSongView.Title == null)
        {
            if (_audioService.IsPlaying)
                _audioService.Stop();

            _playbackQueueSource.Edit(
                updater =>
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
    /// Adds a single song to the end of the current playback queue. If the queue is empty, it starts playback with this
    /// song.
    /// </summary>
    [RelayCommand]
    public void AddToQueue(SongModelView? song)
    {
        if (song == null)
            return;


        AddListOfSongsToQueueEnd(new List<SongModelView> { song });
    }

    /// <summary>
    /// Adds a list of songs to the end of the current playback queue. If the queue is empty, it starts playback with
    /// this new list.
    /// </summary>

    [RelayCommand]
    public void AddListOfSongsToQueueEnd(IEnumerable<SongModelView>? songs)
    {
        if (songs == null || !songs.Any())
            return;

        _playbackQueueSource.AddRange(songs.Distinct());
    }


    /// <summary>
    /// A new helper method to route playback state changes to the correct handler. This is the target of our main
    /// PlaybackStateChanged subscription.
    /// </summary>
    private SongModelView? _songToScrobble;

    protected virtual async Task HandlePlaybackStateChange(PlaybackEventArgs args)
    {
        PlayType? state = StatesMapper.Map(args.EventType);

        switch (state)
        {
            case PlayType.Play:
                await OnPlaybackStarted(args);
                LoadAllAudioDevices();
                break;

            case PlayType.Resume:
                await OnPlaybackResumed(args);
                LoadAllAudioDevices();
                break;

            case PlayType.Pause:
                //OnPlaybackPaused(args);
                break;
        }
    }

    /// <summary>
    /// Subscribes to general state changes from the IStateService.
    /// </summary>
    /// 
    private void SubscribeToStateServiceEvents()
    {
        _subsMgr.Add(_stateService.CurrentSong
            .DistinctUntilChanged()
            .ObserveOn(RxSchedulers.UI)
            .Subscribe(
                    newSong =>
                    {
                        if (newSong is null || newSong.TitleDurationKey == null) return;
                        CurrentPlayingSongView = newSong;
                    }));

        _subsMgr.Add(
            _stateService.IsShuffleActive
                .Subscribe(
                    isShuffle => IsShuffleActive = isShuffle,
                    ex => _logger.LogError(ex, "Error in IsShuffleActive subscription")));

        _subsMgr.Add(
            _stateService.DeviceVolume
                .Subscribe(
                    volume => DeviceVolumeLevel = volume,
                    ex => _logger.LogError(ex, "Error in DeviceVolume subscription")));


        var playbackStateObservable = _stateService.CurrentPlayBackState.Publish().RefCount();

        _subsMgr.Add(
            playbackStateObservable
            .Where(s => s.State == DimmerUtilityEnum.FolderScanCompleted)
                .Subscribe(OnFolderScanCompleted, ex => _logger.LogError(ex, "Error on FolderScanCompleted.")));

        _subsMgr.Add(
            _stateService.LatestDeviceLog
                .Where(s => s.Log is not null)
                .Subscribe(
                    obv =>
                     {
                        LatestDeviceLog(obv);
                    }));
    }

    private void SubscribeToLyricsFlow()
    {
        _subsMgr.Add(
            LyricsMgtFlow.CurrentLyric.ObserveOn(RxSchedulers.UI)
            .Subscribe(line =>
            {
                CurrentPlayingSongView.HasSyncedLyrics = line is not null;
                CurrentLine = line;
            }));
        _subsMgr.Add(

            LyricsMgtFlow.IsLoadingLyrics
            .ObserveOn(RxSchedulers.UI)
                .Subscribe(isLoading =>
                {

                    IsLoadingLyrics = isLoading;
                    

                }));

        _subsMgr.Add(
            LyricsMgtFlow.IsSearchingLyrics
            .ObserveOn(RxSchedulers.UI)
            .Subscribe(isSearching =>
            {
                IsSearchingLyrics = isSearching;
            }));


        LyricsMgtFlow.AllSyncLyrics
                .ObserveOn(RxSchedulers.UI)
                .Subscribe(lines =>
                {
                    AllLines?.Clear();

                    if (lines.Count >= 1)
                    {
                        
                        AllLines = lines.ToObservableCollection();
                        return;
                    }
                    else
                    {
                        AllLines = new ObservableCollection<LyricPhraseModelView>();
                        LyricPhraseModelView defaultLyricForNoneInSong = new()
                        {
                            Text = "No Lyric Found For this song",
                            TimestampStart = 0,
                            TimeStampMs = 0,
                            IsLyricSynced = false
                        };
                        AllLines.Add(defaultLyricForNoneInSong);
                        

                    }
                });
                  CurrentPlayingSongView.HasSyncedLyrics = false;
                  
    

        _subsMgr.Add(
            LyricsMgtFlow.PreviousLyric
                .ObserveOn(RxSchedulers.UI)
                .Subscribe(
                    line =>
                    {
                        PreviousLine = line;
                        if (PreviousLine is not null)
                        {
                            PreviousLine.TextColor = Colors.DarkSlateBlue;
                            PreviousLine.NowPlayingLyricsFontSize = 12;
                        }
                    }));

        _subsMgr.Add(
            LyricsMgtFlow.NextLyric
                .ObserveOn(RxSchedulers.UI)
                .Subscribe(
                    line =>
                    {
                        // if next line is empty we toggle IsNextLineEmpty to true
                        IsNextLineEmpty = string.IsNullOrWhiteSpace(line?.Text);

                        NextLine = line;
                    }));
    }

    private void SubscribeToAudioServiceEvents()
    {
        _subsMgr.Add(
            Observable.FromEventPattern<PlaybackEventArgs>(
                h => _audioService.PlaybackStateChanged += h,
                h => _audioService.PlaybackStateChanged -= h)
                .Select(evt => evt.EventArgs)
                .ObserveOn(RxSchedulers.UI)
                .Subscribe(
                    async x => await HandlePlaybackStateChange(x),
                    ex => _logger.LogError(ex, "Error in PlaybackStateChanged subscription")));

        _subsManager.Add(Observable.FromEventPattern<PlaybackEventArgs>(
            h => _audioService.ErrorOccurred += h,
            h => _audioService.ErrorOccurred -= h)
                .Select(evt => evt.EventArgs)
            .ObserveOn(RxSchedulers.UI)
            .Subscribe(async x =>
            {
                await OnPlayBackErrorOccured(x);
            },
            ex =>
            {
                _logger.LogError(ex, "Error in Subscribing to OnErrorOccured");
            }));
        _subsMgr.Add(
            Observable.FromEventPattern<PlaybackEventArgs>(
                h => _audioService.IsPlayingChanged += h,
                h => _audioService.IsPlayingChanged -= h)
                .Select(evt => evt.EventArgs.IsPlaying)
                .ObserveOn(RxSchedulers.UI)
                .Subscribe(
                    isPlaying =>
                    {
                        IsDimmerPlaying = isPlaying;
                    },
                    ex => _logger.LogError(ex, "Error in IsPlayingChanged subscription")));


        _subsMgr.Add(
            Observable.FromEventPattern<double>(
                h => _audioService.PositionChanged += h,
                h => _audioService.PositionChanged -= h)
                .Select(evt => evt.EventArgs)
                .ObserveOn(RxSchedulers.UI)
                .Subscribe(OnPositionChanged, ex => _logger.LogError(ex, "Error in PositionChanged subscription")));

        //_subsMgr.Add(Observable.FromEventPattern<double>(
        //        h => _audioService.SeekCompleted += h,
        //        h => _audioService.SeekCompleted -= h)
        //    .Select(evt => evt.EventArgs)
        //    .ObserveOn(RxSchedulers.UI)
        //    .Subscribe(newPost =>
        //    {
        //        OnSeekCompleted(newPost);
        //    }, ex => _logger.LogError(ex, "Error in SeekCompleted subscription")));


        _subsMgr.Add(
            Observable.FromEventPattern<PlaybackEventArgs>(
                h => _audioService.PlayEnded += h,
                h => _audioService.PlayEnded -= h)
                .ObserveOn(RxSchedulers.UI)
                .Subscribe(
                    async _ =>
                    {
                        await OnPlaybackEnded();
                    },
                    ex => _logger.LogError(ex, "Error in PlayEnded subscription")));


        _subsMgr.Add(
            Observable.FromEventPattern<PlaybackEventArgs>(
                h => _audioService.MediaKeyNextPressed += h,
                h => _audioService.MediaKeyNextPressed -= h)
                .Subscribe(
                    async _ => await NextTrackAsync(),
                    ex => _logger.LogError(ex, "Error in MediaKeyNextPressed subscription")));

        _subsMgr.Add(
            Observable.FromEventPattern<PlaybackEventArgs>(
                h => _audioService.MediaKeyPreviousPressed += h,
                h => _audioService.MediaKeyPreviousPressed -= h)
                .Subscribe(
                    async _ => await PreviousTrack(),
                    ex => _logger.LogError(ex, "Error in MediaKeyPreviousPressed subscription")));
    }

    private async Task OnPlayBackErrorOccured(PlaybackEventArgs x)
    {
        _logger.LogError(message: x.EventType.ToString());
        await UiDialogs.SafeDisplayActionSheetAsync(x.EventType.ToString(), "OK");
    }

    private void LatestDeviceLog(AppLogModel model)
    {

        MainThread.BeginInvokeOnMainThread(() =>
        {
            LatestAppLog = model;
            LatestScanningLog = model.Log;
            _logger.LogInformation("Device Log: {Log}", model.Log);

        });
    }

    [ObservableProperty] public partial string CurrentPlaybackQuery { get; set; }

    [ObservableProperty]
    public partial bool IsAppScanning { get; set; }

    private Random _random = new();


    [RelayCommand]
    public void SeekTrackPosition(double positionSeconds)
    {
        _logger.LogDebug("SeekTrackPosition called by UI to: {PositionSeconds}s", positionSeconds);

        _audioService.Seek(positionSeconds);
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
    public void IncreaseVolumeLevel() { SetVolumeLevel(DeviceVolumeLevel + 0.05); }

    [RelayCommand]
    public void DecreaseVolumeLevel() { SetVolumeLevel(DeviceVolumeLevel - 0.05); }


    public async Task SetSelectedArtist(ArtistModelView? artist)
    {
        if(artist is null)
        {
            return;
        }

        SelectedArtist = artist;
        //artist.ListOfSimilarArtists = SearchResults
        //    .SelectMany(s => s.ArtistToSong)
        //    .Where(a => similar.Contains(a.Name) && a.Name != artist.Name)
        //    .DistinctBy(a => a.Name)
        //    .Take(10)
        //    .ToList();
    }
    public async Task<bool> SelectedArtistAndNavtoPage(SongModelView? song)
    {
        song ??= CurrentPlayingSongView;
        if (song is null)
        {
            return false;
        }

        var allArts = song.OtherArtistsName.Split(", ");

        _logger.LogTrace("SelectedArtistAndNavtoPage called with song: {SongTitle}", song.Title);
        var result = await Shell.Current.DisplayActionSheet("Select Action", "Cancel", null, allArts);
        if (result == "Cancel" || string.IsNullOrEmpty(result))
            return false;


        var realm = RealmFactory.GetRealmInstance();
        var artDb = realm.All<ArtistModel>().ToList().FirstOrDefault(x => x.Name == result);

        DeviceStaticUtils.SelectedArtistOne = artDb.ToModelView(_mapper);


        return true;
    }

    public void SetCurrentlyPickedSongForContext(SongModelView? song)
    {
        _logger.LogTrace("SetCurrentlyPickedSongForContext called with: {SongTitle}", song?.Title ?? "None");

        if (song is null) return;
        song.PlayEvents = DimmerPlayEventList.Where(x => x.SongId == song.Id).ToObservableCollection();
        SelectedSongForContext = song;
    }

    [RelayCommand]
    public void LoadInSongsAndEvents()
    {
        //Task.Run(() => libService.LoadInSongsAndEvents());
    }

    [RelayCommand]
    public async Task AddMusicFolderByPassingToService(string folderPath)
    {
        _logger.LogInformation("User requested to add music folder.");
        _stateService.SetCurrentState(new PlaybackStateInfo(DimmerUtilityEnum.FolderAdded, folderPath, null, null));
        _ = Task.Run(async () => await _folderMgtService.AddFolderToWatchListAndScan(folderPath));
    }

    [ObservableProperty]
    public partial ObservableCollection<PlaylistModelView> Playlists { get; set; } = new();

    [RelayCommand]
    public void LoadPlaylists()
    {
        var playlistsFromDb = _playlistRepo.GetAll().OrderByDescending(p => p.LastPlayedDate).ToList();
        Playlists.Clear();
        foreach (var pl in playlistsFromDb)
        {
            Playlists.Add(_mapper.Map<PlaylistModelView>(pl));
        }
    }

    [RelayCommand]
    public void LoadLastHundredPlayEvents()
    {
        DimmerPlayEventList?.Clear();

        var realm = RealmFactory.GetRealmInstance();
        var allQSongs = realm.All<SongModel>();
        SongToEventsDict = allQSongs.ToDictionary(s => s, s => s.PlayHistory.ToList());

        var eventsFromDb = dimmerPlayEventRepo.GetAll().OrderByDescending(e => e.EventDate).Take(200).ToList();
        DimmerPlayEventList = _mapper.Map<IEnumerable<DimmerPlayEventView>>(eventsFromDb).ToObservableCollection();

        _logger.LogInformation("Loaded {Count} recent play events from the database.", DimmerPlayEventList.Count);
    }

    [ObservableProperty]
    public partial Dictionary<SongModel, List<DimmerPlayEvent>>? SongToEventsDict { get; set; }


    public void UpdatePlaylistDescription(PlaylistModelView? playlist, string newDescription)
    {
        if (playlist == null)
            return;
        _playlistRepo.Update(
            playlist.Id,
            pl =>
            {
                pl.Description = newDescription;
            });
        var songIdInPl = playlist.SongsIdsInPlaylist?.ToList();
        if (songIdInPl == null || songIdInPl.Count > 0)
            return;
        foreach (var songId in songIdInPl)
        {
            var songInDb = songRepo.GetById(songId);

            if (songInDb == null)
                continue;

            songInDb.UserNotes.Add(new UserNoteModel() { UserMessageText = newDescription, });
        }
        LoadPlaylists();
    }


    [RelayCommand]
    public void DeletePlaylist(PlaylistModelView? playlist)
    {
        if (playlist == null)
            return;
        _playlistRepo.Delete(_mapper.Map<PlaylistModel>(playlist));
        Playlists.Remove(playlist);
    }

    public void RenamePlaylist(ObjectId plId, string newName)
    {
        var pl = _playlistRepo.GetById(plId);
        if (pl == null)
            return;
        pl.PlaylistName = newName;

        _playlistRepo.Update(
            plId,
            pl =>
            {
                pl.PlaylistName = newName;
            });
        LoadPlaylists();
    }


    [RelayCommand]
    public async Task ReScanMusicFolderByPassingToService(string folderPath)
    {
        _logger.LogInformation("User requested to add music folder.");
        await _folderMgtService.ReScanFolder(folderPath);
        _stateService.SetCurrentState(new PlaybackStateInfo(DimmerUtilityEnum.FolderReScanned, folderPath, null, null));
    }

    public void AddMusicFoldersByPassingToService(List<string> folderPaths)
    {
        _logger.LogInformation("User requested to add music folder.");
        _folderMgtService.AddManyFoldersToWatchListAndScan(folderPaths);
    }

    public void ViewAlbumDetails(AlbumModelView albumView)
    {
        SelectedAlbum = albumView;
        SearchSongForSearchResultHolder(TQlStaticMethods.SetQuotedSearch("album", albumView.Name));
    }

    [ObservableProperty]
    public partial bool IsNextLineEmpty { get; set; }


    [RelayCommand]
    public async Task RetroactivelyLinkArtists()
    {
        await Shell.Current
            .DisplayAlert(
                "Process Started",
                "Starting to link artists for all songs. This may take a moment. The app might be a bit slow.",
                "OK");

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        await Task.Run(
            () =>
            {
                if (RealmFactory is null)
                {
                    _logger.LogError("RealmFactory is not available.");
                    return;
                }
                var realm = RealmFactory.GetRealmInstance();
                if (realm is null)
                {
                    _logger.LogError("Failed to get Realm instance.");
                    return;
                }


                _logger.LogInformation("Searching for songs with unlinked artists...");
                var songsToFix = realm.All<SongModel>().Filter("ArtistToSong.@count == 0").ToList();

                if (songsToFix.Count == 0)
                {
                    _logger.LogInformation(
                        "No songs found that require artist linking. Database is already up-to-date!");

                    MainThread.BeginInvokeOnMainThread(
                        async () =>
                        {
                            await Shell.Current
                                .DisplayAlert(
                                    "All Done!",
                                    "No songs needed fixing. Everything is already linked correctly.",
                                    "OK");
                        });
                    return;
                }

                _logger.LogInformation("Found {SongCount} songs to process.", songsToFix.Count);


                var allArtistNames = songsToFix
                .Select(s => s.ArtistName)
                    .Concat(
                        songsToFix.SelectMany(
                                s => (s.OtherArtistsName ?? "").Split(
                                    new[] { ", " },
                                    StringSplitOptions.RemoveEmptyEntries)))
                    .Where(name => !string.IsNullOrEmpty(name))
                    .Distinct()
                    .ToList();

                _logger.LogInformation(
                    "Found {ArtistCount} unique artist names to look up in the database.",
                    allArtistNames.Count);


                var artistClauses = Enumerable.Range(0, allArtistNames.Count).Select(i => $"Name == ${i}");
                var artistQueryString = string.Join(" OR ", artistClauses);
                var artistQueryArgs = allArtistNames.Select(name => (QueryArgument)name).ToArray();

                var artistsFromDb = realm.All<ArtistModel>()
                    .Filter(artistQueryString, artistQueryArgs)
                    .ToDictionary(a => a.Name);

                _logger.LogInformation(
                    "Successfully fetched {ArtistCount} matching Artist objects from the database.",
                    artistsFromDb.Count);


                _logger.LogInformation("Beginning database write transaction to link artists...");
                realm.Write(
                    () =>
                    {
                        foreach (var song in songsToFix)
                        {
                            var namesForThisSong = new List<string> { song.ArtistName }.Concat(
                                (song.OtherArtistsName ?? "").Split(
                                    new[] { ", " },
                                    StringSplitOptions.RemoveEmptyEntries))
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
                                    _logger.LogWarning(
                                        "Could not find artist '{ArtistName}' in the database to link to song '{SongTitle}'.",
                                        artistName,
                                        song.Title);
                                }
                            }
                        }
                    });

                stopwatch.Stop();
                _logger.LogInformation(
                    "Successfully linked artists for {SongCount} songs in {ElapsedMilliseconds} ms.",
                    songsToFix.Count,
                    stopwatch.ElapsedMilliseconds);


                MainThread.BeginInvokeOnMainThread(
                    async () =>
                    {
                        await Shell.Current
                            .DisplayAlert(
                                "Success!",
                                $"Successfully updated {songsToFix.Count} songs in {stopwatch.Elapsed.TotalSeconds:F2} seconds.",
                                "Awesome!");
                    });
            });
    }

    [RelayCommand]
    public void LoadAllArtistsFromOurDatabase()
    {
        var db = RealmFactory.GetRealmInstance();
        var allArts = db.All<ArtistModel>().ToList().OrderBy(a => a.Name).ToList();

        AllArtistsInDb?.Clear();
        AllArtistsInDb = new ObservableCollection<ArtistModelView>();
        foreach (var art in allArts)
        {
            AllArtistsInDb.Add(art.ToModelView(_mapper));
        }
        _logger.LogInformation("Loaded {ArtistCount} artists from the database.", AllArtistsInDb.Count);
    }

    [RelayCommand]
    public void LoadAllAlbumsFromOurDatabase()
    {
        var db = RealmFactory.GetRealmInstance();
        var allAlbs = db.All<AlbumModel>().ToList().OrderBy(a => a.Name).ToList();
        AllAlbumsInDb?.Clear();
        AllAlbumsInDb = new ObservableCollection<AlbumModelView>();
        foreach (var alb in allAlbs)
        {
            AllAlbumsInDb.Add(alb.ToModelView());
        }
        _logger.LogInformation("Loaded {AlbumCount} albums from the database.", AllAlbumsInDb.Count);
    }


    [ObservableProperty]
    public partial ObservableCollection<AlbumModelView>? AllAlbumsInDb { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<ArtistModelView>? AllArtistsInDb { get; set; }

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
        int topCount = 10;


        var endDate = DateTimeOffset.UtcNow;
        var monthStartDate = endDate.AddMonths(-1);
        var yearStartDate = endDate.AddYears(-1);
    }

    /// <summary>
    /// Loads ALL statistics for a single song. Call this when a song is selected.
    /// </summary>
    [RelayCommand]
    public async Task LoadStatsForSelectedSong(SongModelView? song)
    {
        song ??= SelectedSong;
        var endDate = DateTimeOffset.Now;
        var startDate = endDate.AddDays(-7);

        realm = RealmFactory.GetRealmInstance();
        // Phase 1: Data Preparation
        var allDimmerEvents = realm.All<DimmerPlayEvent>()
            .ToList();
        var dimmerEventViewEnum = _mapper.Map<IEnumerable<DimmerPlayEventView>>(allDimmerEvents);
        var allSongs = realm.All<SongModel>().ToList();
        _reportGenerator = new ListeningReportGenerator(allSongs, dimmerEventViewEnum, _logger, _mapper, RealmFactory);
        await GenerateWeeklyReportAsync();
    }

    [RelayCommand]
    public async Task LoadStatsForSpecificSong(SongModelView? song)
    {
        song ??= SelectedSong;
        if (song is null) return;
        var endDate = DateTimeOffset.Now;
        var startDate = endDate.AddDays(-7);

        realm = RealmFactory.GetRealmInstance();
        // Phase 1: Data Preparation
        var scrobblesInPeriod = realm.Find<SongModel>(song.Id)?.PlayHistory
            //.Where(p => p.EventDate >= startDate && p.EventDate < endDate &&
            //            (p.PlayType == (int)PlayType.Play || p.PlayType == (int)PlayType.Completed))
            //.OrderBy(p => p.EventDate) // Important for sequential analysis
            .ToList();
        var allSongs = realm.All<SongModel>().ToList();
        var allDimmsForSong = _mapper.Map<IEnumerable<DimmerPlayEventView>>(scrobblesInPeriod ?? new List<DimmerPlayEvent>());
        _reportGenerator = new ListeningReportGenerator(allSongs, allDimmsForSong, _logger, _mapper, RealmFactory);
        await GenerateWeeklyReportAsync();
    }

    [RelayCommand]
    private async Task GenerateWeeklyReportAsync()
    {
        // clear all the stats first
        ClearSingleSongStats();

        IsLoading = true;
        try
        {
            // Define the period for a weekly report ending today
            var endDate = DateTimeOffset.UtcNow.Date.AddDays(1); // To include all of today
            var startDate = endDate.AddDays(-7);

            var reportData = await _reportGenerator.GenerateReportAsync(startDate, endDate);
            if (reportData)
            {
                // Find each stat by its title and assign it to the correct property.
                // Use FirstOrDefault to avoid exceptions if a stat isn't generated.
                TotalScrobblesStat = _reportGenerator.GetTotalScrobbles();

                TopTracks = _reportGenerator.GetTopTracks();
                TopArtists = _reportGenerator.GetTopArtists();
                TopAlbums = _reportGenerator.GetTopAlbums();

                ListeningClockData = _reportGenerator.GetListeningClock();

                MusicByDecadeData = _reportGenerator.GetMusicByDecade();


                // --- Unique Count Cards (NEW) ---
                UniqueTracksStat = _reportGenerator.GetUniqueTracks();
                UniqueArtistsStat = _reportGenerator.GetUniqueArtists();
                UniqueAlbumsStat = _reportGenerator.GetUniqueAlbums();

                // --- Listening Fingerprint (NEW) ---
                ConsistencyStat = _reportGenerator.GetConsistence();
                DiscoveryRateStat = _reportGenerator.GetDiscoveryRate();
                VarianceStat = _reportGenerator.GetVariance();
                ConcentrationStat = _reportGenerator.GetConcentration();
                ReplayRateStat = _reportGenerator.GetReplayRate();

                // --- Quick Facts (NEW) ---
                TotalListeningTimeStat = _reportGenerator.GetTotalListeningTime();
                AverageScrobblesPerDayStat = _reportGenerator.GetAverageScrobblesDay();
                MostActiveDayStat = _reportGenerator.GetMostActiveDay();

                // --- Advanced Plots (NEW) ---
                ListeningConcentrationStat = _reportGenerator.GetListeningConcentration();
                EddingtonNumberStat = _reportGenerator.GetMusicEddingtonNumber();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate weekly report.");
            // Handle error, maybe show a message to the user
        }
        finally
        {
            IsLoading = false;
        }
    }

    #region Main Cards & Charts
    [ObservableProperty]
    public partial DimmerStats? TotalScrobblesStat { get; set; }

    [ObservableProperty]
    public partial DimmerStats? ListeningClockData { get; set; }

    [ObservableProperty]
    public partial DimmerStats? MusicByDecadeData { get; set; }

    [ObservableProperty]
    public partial DimmerStats? TopTagsEvolutionData { get; set; }
    #endregion

    #region Top Lists
    [ObservableProperty]
    public partial List<DimmerStats>? TopTracks { get; set; }

    [ObservableProperty]
    public partial List<DimmerStats>? TopArtists { get; set; }

    [ObservableProperty]
    public partial List<DimmerStats>? TopAlbums { get; set; }
    #endregion

    #region Unique Count Cards
    [ObservableProperty]
    public partial DimmerStats? UniqueTracksStat { get; set; }

    [ObservableProperty]
    public partial DimmerStats? UniqueArtistsStat { get; set; }

    [ObservableProperty]
    public partial DimmerStats? UniqueAlbumsStat { get; set; }
    #endregion

    #region Listening Fingerprint
    [ObservableProperty]
    public partial DimmerStats? ConsistencyStat { get; set; }

    [ObservableProperty]
    public partial DimmerStats? DiscoveryRateStat { get; set; }

    [ObservableProperty]
    public partial DimmerStats? VarianceStat { get; set; }

    [ObservableProperty]
    public partial DimmerStats? ConcentrationStat { get; set; }

    [ObservableProperty]
    public partial DimmerStats? ReplayRateStat { get; set; }
    #endregion

    #region Quick Facts
    [ObservableProperty]
    public partial DimmerStats? TotalListeningTimeStat { get; set; }

    [ObservableProperty]
    public partial DimmerStats? AverageScrobblesPerDayStat { get; set; }

    [ObservableProperty]
    public partial DimmerStats? MostActiveDayStat { get; set; }
    #endregion

    #region Advanced Plots
    [ObservableProperty]
    public partial DimmerStats? ListeningConcentrationStat { get; set; } // Pareto

    [ObservableProperty]
    public partial DimmerStats? EddingtonNumberStat { get; set; }
    #endregion

    private ListeningReportGenerator _reportGenerator;

    [ObservableProperty]
    public partial List<DimmerStats> ReportData { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    private void ClearSingleSongStats()
    {
        SongPlayTypeDistribution = null;
        SongPlayDistributionByHour = null;
        SongPlayHistoryOverTime = null;
        SongDropOffPoints = null;
        SongWeeklyOHLC = null;
        SongBingeFactor = null;
        SongAverageListenThrough = null;
        TotalScrobblesStat = null;

        TopTracks = null;
        TopArtists = null;
        TopAlbums = null;

        ListeningClockData = null;

        MusicByDecadeData = null;
        TopTagsEvolutionData = null;


        // --- Unique Count Cards (NEW) ---
        UniqueTracksStat = null;
        UniqueArtistsStat = null;
        UniqueAlbumsStat = null;

        // --- Listening Fingerprint (NEW) ---
        ConsistencyStat = null;
        DiscoveryRateStat = null;
        VarianceStat = null;
        ConcentrationStat = null;
        ReplayRateStat = null;

        // --- Quick Facts (NEW) ---
        TotalListeningTimeStat = null;
        AverageScrobblesPerDayStat = null;
        MostActiveDayStat = null;

        // --- Advanced Plots (NEW) ---
        ListeningConcentrationStat = null;
        EddingtonNumberStat = null;


        SongListeningStreak = null;
    }

    public async Task SaveNoteToListOfSongs(IEnumerable<SongModelView> songs)
    {
        foreach (var item in songs)
        {
            await SaveUserNoteToSong(item);
        }
        //TODO : make an error handling logic here
    }


    [RelayCommand]
    public void DeleteFolderPath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return;
        _logger.LogInformation("Requesting to delete folder path: {Path}", path);
        FolderPaths.Remove(path);
        _folderMgtService.RemoveFolderFromWatchListAsync(path);

        var realm = RealmFactory.GetRealmInstance();
        var appModel = realm.All<AppStateModel>().FirstOrDefault();
        if (appModel is null)
            return;
        realm.Write(
            () =>
            {
                appModel.UserMusicFoldersPreference.Remove(path);
                realm.Add(appModel, true);
            });
    }



    public void RateSong(int newRating)
    {
        if (CurrentPlayingSongView == null || CurrentPlayingSongView.Id == ObjectId.Empty)
        {
            _logger.LogWarning("RateSong called but CurrentPlayingSongView is null.");
            return;
        }
        _logger.LogInformation(
            "Rating song '{SongTitle}' with new rating: {NewRating}",
            CurrentPlayingSongView.Title,
            newRating);
        var songModel = CurrentPlayingSongView.ToModel(_mapper);
        if (songModel == null)
        {
            _logger.LogWarning("RateSong: Could not map CurrentPlayingSongView to SongModel.");
            return;
        }
        songModel.Rating = newRating;
        var song = songRepo.Upsert(songModel);
        _logger.LogInformation("evt '{SongTitle}' updated with new rating: {NewRating}", songModel.Title, newRating);

        _stateService.SetCurrentSong(song.ToModelView());
    }

    [RelayCommand]
    public void ToggleArtistAsFavorite(ArtistModelView artist)
    {
        artist.IsFavorite = !artist.IsFavorite;
        _baseAppFlow.UpsertArtist(artist.ToModel());
    }
    

    [RelayCommand]
    public void ToggleAlbumAsFavorite(AlbumModelView album)
    {
        album.IsFavorite = !album.IsFavorite;
        _baseAppFlow.UpsertAlbum(album.ToModel());
    }


    [RelayCommand]
    public async Task AddFavoriteRatingToSong(SongModelView songModel)
    {
        if (songModel is null || songModel.Id == ObjectId.Empty)
        {
            _logger.LogWarning("AddFavoriteRatingToSong called with invalid parameters.");
            return;
        }

        try
        {
            // mark locally for UI
            songModel.IsFavorite = true;

            // record event (favorited)
            await _baseAppFlow.UpdateDatabaseWithPlayEvent(
                RealmFactory,
                songModel,
                StatesMapper.Map(DimmerPlaybackState.Favorited),
                CurrentTrackPositionSeconds);

            // increment only manual count (persistent)
            songModel.ManualFavoriteCount++;

            // recalc derived field deterministically
            songModel.NumberOfTimesFaved =
                songModel.ManualFavoriteCount + (songModel.PlayCompletedCount / 4);

            // persist Realm update off-UI thread
            var updated = await Task.Run(() => songRepo.Upsert(songModel.ToModel(_mapper)).ToModelView());
            if (updated is not null)
                MainThread.BeginInvokeOnMainThread(() => songModel =updated);


            // network side effect only once per manual first love
            if (songModel.ManualFavoriteCount == 1)
            {
                await lastfmService.LoveTrackAsync(songModel);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to favorite {SongTitle}", songModel.Title);
        }
    }

    [RelayCommand]
    public async Task UnloveSong(SongModelView songModel)
    {
        if (songModel is null || songModel.Id == ObjectId.Empty)
        {
            _logger.LogWarning("UnloveSong called with invalid SongModel.");
            return;
        }

        // IsBusy = true;

        try
        {
            // 1. Await the I/O network call correctly.
            await lastfmService.UnloveTrackAsync(songModel);

            // 2. Update local model state ONLY after the network call succeeds.
            songModel.NumberOfTimesFaved = 0;
            songModel.IsFavorite = false;

            // 3. Move the synchronous DB work to a background thread.
            await Task.Run(() => songRepo.Upsert(songModel.ToModel(_mapper)));
        }
        catch (Exception ex)
        {
            // 4. Gracefully handle network or database errors.
            _logger.LogError(ex, "Failed to unlove song: {SongTitle}", songModel.Title);
            // Optional: Show an error message to the user
        }
        finally
        {
            // IsBusy = false;
        }
    }

    public void AddToPlaylist(string playlistName, IEnumerable<SongModelView> songsToAdd, string PlQuery)
    {
        if (string.IsNullOrEmpty(playlistName) || songsToAdd == null || !songsToAdd.Any())
        {
            _logger.LogWarning("AddToPlaylist called with invalid parameters.");
            return;
        }

        _logger.LogInformation(
            "Attempting to add {Count} songs to playlist '{PlaylistName}'.",
            songsToAdd.Count(),
            playlistName);


        var matchingPlaylists = _playlistRepo.Query(p => p.PlaylistName == playlistName);
        var targetPlaylist = matchingPlaylists.FirstOrDefault();


        if (targetPlaylist == null)
        {
            _logger.LogInformation(
                "Playlist '{PlaylistName}' not found. Creating it as a new manual playlist.",
                playlistName);
            var newPlaylistModel = new PlaylistModel { PlaylistName = playlistName, IsSmartPlaylist = false };
            var realm = RealmFactory.GetRealmInstance();
            realm.Write(
                () =>
                {
                    newPlaylistModel.Id = ObjectId.GenerateNewId();
                    newPlaylistModel.DateCreated = DateTimeOffset.UtcNow;
                    newPlaylistModel.LastPlayedDate = DateTimeOffset.UtcNow;
                    newPlaylistModel.QueryText = PlQuery;
                    newPlaylistModel.SongsIdsInPlaylist.AddRange(songsToAdd.Select(s => s.Id).Distinct());

                    //newPlaylistModel.SongsInPlaylist.AddRange(songsToAdd.Select(s => s.ToModel(_mapper)).Distinct());

                    realm.Add(newPlaylistModel, true);
                });

            //targetPlaylist = _playlistRepo.Create(newPlaylistModel);
        }


        if (targetPlaylist.IsSmartPlaylist)
        {
            _logger.LogWarning(
                "Cannot manually add songs to the smart playlist '{PlaylistName}'. Change its query instead.",
                playlistName);

            return;
        }


        var songIdsToAdd = songsToAdd.Select(s => s.Id).ToHashSet();

        _playlistRepo.Update(
            targetPlaylist.Id,
            livePlaylist =>
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
                _logger.LogInformation(
                    "Successfully added {Count} new songs to manual playlist '{PlaylistName}'.",
                    songsAddedCount,
                    playlistName);
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
            _subsManager.Dispose();
            CompositeDisposables.Dispose();
        }

        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    public void RaisePropertyChanging(System.ComponentModel.PropertyChangingEventArgs args)
    { OnPropertyChanging(args); }

    public void RaisePropertyChanged(PropertyChangedEventArgs args) { OnPropertyChanged(args); }

    [ObservableProperty]
    public partial double ArtistLoyaltyIndex { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<ArtistModelView> MyCoreArtists { get; set; }

    [ObservableProperty]
    public partial (DateTimeOffset Date, int PlayCount) ArtistBingeScore { get; set; }

    [ObservableProperty]
    public partial SongModelView SongThatHookedMeOnAnArtist { get; set; }


    public ReadOnlyObservableCollection<DimmerPlayEventView> PlayEventsByTimeChartData { get; private set; }

   
    public ReadOnlyObservableCollection<DimmerPlayEventView> PlaysByDurationChartData { get; private set; }


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


    public async Task LoadSongDataAsync(
        Progress<LyricsProcessingProgress>? progressReporter,
        CancellationTokenSource _lyricsCts)
    {
        var allSongsFromDb = await songRepo.GetAllAsync();


        await SongDataProcessor.ProcessLyricsAsync(
            RealmFactory,
            allSongsFromDb,
            _lyricsMetadataService,
            progressReporter,
            _lyricsCts.Token);
    }

    public record QueryComponents(
        Func<SongModelView, bool>? Predicate,
        IComparer<SongModelView> Comparer,
        LimiterClause? Limiter);


    [ObservableProperty]
    public partial bool IsFindingDuplicates { get; set; }

    [ObservableProperty]
    public partial bool UseTitleCriteria { get; set; }

    [ObservableProperty]
    public partial bool UseArtistCriteria { get; set; }

    [ObservableProperty]
    public partial bool UseAlbumCriteria { get; set; }

    [ObservableProperty]
    public partial bool UseDurationCriteria { get; set; } = true;

    [ObservableProperty]
    public partial bool UseFileSizeCriteria { get; set; } = true;

    private CancellationTokenSource _findDuplicatesCts;
    [RelayCommand]
    public async Task FindDuplicatesForSongAsync(SongModelView song)
    {
        if (song == null) return;

        // For simplicity, you might use a predefined criteria or pop a small dialog
        // to let the user choose the criteria for this specific search.
        var criteria = DuplicateCriteria.Title | DuplicateCriteria.Artist | DuplicateCriteria.Duration;

        IsBusy = true;
        try
        {
            // This call is fast, so Task.Run might be optional but is still good practice.
            var result = await Task.Run(() => _duplicateFinderService.FindDuplicatesForSong(song, criteria));

            if (result.DuplicateSets.Count == 0)
            {
                if (CurrentPageContext == CurrentPage.SingleSongPage)
                {
                    _stateService.SetCurrentLogMsg(new AppLogModel
                    {
                        Log = $"No duplicates found for '{song.Title}'.",
                        ViewSongModel = song
                    });
                }
                return;
            }
            IsDuplicateFound = true;
            _duplicateSource.Clear();
            _duplicateSource.AddRange(result.DuplicateSets);

            await Task.Delay(4000);
            IsDuplicateFound = false;
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogInformation($"Duplicate find operation was canceled. {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to find duplicates.");
            // Optionally, display an error message to the user
            _stateService.SetCurrentLogMsg(new AppLogModel
            {
                Log = $"Error when searching duplicates for {song.Title} {Environment.NewLine}Error: {ex.Message}",
                ViewSongModel = song
            });
        }
        finally
        {
            IsBusy = false;
        }
    }
    [RelayCommand]
    private async Task FindDuplicatesAsync()
    {
        var criteria = DuplicateCriteria.None;
        if (UseTitleCriteria)
            criteria |= DuplicateCriteria.Title;
        if (UseArtistCriteria)
            criteria |= DuplicateCriteria.Artist;
        if (UseAlbumCriteria)
            criteria |= DuplicateCriteria.Album;
        if (UseDurationCriteria)
            criteria |= DuplicateCriteria.Duration;
        if (UseFileSizeCriteria)
            criteria |= DuplicateCriteria.FileSize;


        if (criteria == DuplicateCriteria.None)
        {
            await Shell.Current
                .DisplayAlert("No Criteria", "Please select at least one field to check for duplicates.", "OK");
            return;
        }

        IsBusy = true;
        _findDuplicatesCts = new CancellationTokenSource();
        try
        {
            var progress = new Progress<string>(
            message =>
            {
                LatestAppLog?.Log = message;
            });

            // 2. Call the new flexible service method
            DuplicateSearchResult? result = await Task.Run(() => _duplicateFinderService.FindDuplicates(criteria, progress));
            //DuplicateSearchResult? result = _duplicateFinderService.FindDuplicates(criteria, progress);

            // 3. Update the UI with the results
            _duplicateSource.Clear();
            _duplicateSource.AddRange(result.DuplicateSets);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Duplicate find operation was canceled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to find duplicates.");
            // Optionally, display an error message to the user
            await Shell.Current.DisplayAlert("Error", "An unexpected error occurred while searching for duplicates.", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [ObservableProperty]
    public partial bool IsDuplicateFound { get; set; }

    [ObservableProperty]
    public partial bool IsAboutToConsolidateDupes { get; set; }
    [RelayCommand]
    private void CancelFindDuplicates()
    {
        _findDuplicatesCts?.Cancel();
    }
    [RelayCommand]
    public void ClearDuplicateResults() { _duplicateSource.Clear(); }

    [RelayCommand]
    private async Task ApplyDuplicateActionsAsync()
    {
        var setsWithDeletions = DuplicateSets
       .Where(set => set.Items.Any(item => item.Action == DuplicateAction.Delete))
            .ToList();

        var itemsToDelete = setsWithDeletions
            .SelectMany(set => set.Items)
            .Where(item => item.Action == DuplicateAction.Delete)
            .ToList();

        if (itemsToDelete.Count == 0)
            return;

        var deletedCount = await _duplicateFinderService.ResolveDuplicatesAsync(itemsToDelete);
        _logger.LogInformation("Successfully resolved duplicates, deleting {Count} items.", deletedCount);
        _duplicateSource.RemoveMany(setsWithDeletions);

        await ShowNotification($"{deletedCount} duplicate songs have been removed.");

        //var itemsToDelete = DuplicateSets
        //    .SelectMany(set => set.Items)
        //    .Where(item => item.Action == DuplicateAction.Delete)
        //    .ToList();

        //if (itemsToDelete.Count==0)
        //    return;

        //var deletedCount = await _duplicateFinderService.ResolveDuplicatesAsync(itemsToDelete);
        //_logger.LogInformation("Successfully resolved duplicates, deleting {Count} items.", deletedCount);


        //_searchQuerySubject.OnNext(CurrentTqlQuery);

        //// Clear the local list of duplicate sets
        //await ShowNotification($"{deletedCount} duplicate songs have been removed.");
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
            // --- MODIFIED: Query fresh data directly from the repository ---
            var allSongsFromDb = await songRepo.GetAllAsync();
            var validationResult = await Task.Run(
                () => _duplicateFinderService.ValidateMultipleFilesPresenceAsync(
                    _mapper.Map<List<SongModelView>>(allSongsFromDb)));

            if (validationResult.MissingCount == 0)
            {
                _logger.LogInformation("Library validation complete. No missing files found.");
                return;
            }

            _logger.LogInformation(
                "Found {Count} songs with missing files. Removing from database.",
                validationResult.MissingCount);

            var missingIds = validationResult.MissingSongs.Select(s => s.Id).ToHashSet();
            await songRepo.DeleteManyAsync(missingIds); // Assuming your repo has a DeleteManyAsync

            var allSongs = await songRepo.GetAllAsync();
            Debug.WriteLine(allSongs.Count);
            // --- REPLACED: Refresh the UI by re-running the current search ---
            _searchQuerySubject.OnNext(CurrentTqlQuery);
            await ShowNotification($"{validationResult.MissingCount} missing songs removed from library.");
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
    public async Task RefreshSongMissingMetaDataWithSourceAsFile(SongModelView? song)
    {
        if (song is null)
            return;

        await ApplyGenreToSongsAsync();
    }

    [RelayCommand]
    private async Task ReconcileLibraryAsync()
    {
        if (IsReconcilingLibrary)
            return;

        IsReconcilingLibrary = true;
        _logger.LogInformation("Starting library reconciliation...");


        await ApplyGenreToSongsAsync();
        try
        {
            var allSongs = await songRepo.GetAllAsync();
            if (allSongs == null || allSongs.Count == 0)
            {
                _logger.LogInformation("No songs found in the library. Nothing to reconcile.");
                return;
            }
            _logger.LogInformation("Found {Count} songs in the library to reconcile.", allSongs.Count);
            var songItems = _mapper.Map<IEnumerable<SongModelView>>(allSongs);
            var result = await Task.Run(() => _duplicateFinderService.ReconcileLibraryAsync(songItems));

            if (result.MigratedCount == 0 && result.UnresolvedCount == 0)
            {
                _logger.LogInformation("Reconciliation complete. Library is already in a perfect state.");
                return;
            }


            await ApplyGenreToSongsAsync();
            var songsToRemove = new List<SongModelView>();


            songsToRemove.AddRange(result.UnresolvedMissingSongs);


            songsToRemove.AddRange(result.MigratedSongs.Select(m => m.From));


            songsToRemove.AddRange(result.MigratedSongs.Select(m => m.To));


            var songsToAdd = result.MigratedSongs.Select(m => m.To).ToList();


            await ApplyGenreToSongsAsync();

            _logger.LogInformation(
                "UI updated. Removed {RemoveCount} entries, added back {AddCount} updated entries.",
                songsToRemove.Count,
                songsToAdd.Count);
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

    [RelayCommand]
    private async Task ValidateSongAsync(SongModelView song)
    {
        if (IsCheckingFilePresence)
            return;

        IsCheckingFilePresence = true;
        _logger.LogInformation("Starting library validation...");


        try
        {
            var validationResult = await _duplicateFinderService.ValidateMultipleFilesPresenceAsync(
                new List<SongModelView> { song });

            if (validationResult.MissingCount == 0)
            {
                _logger.LogInformation("Song validation complete. File is present.");
                await ShowNotification("Song file is present.");
                return;
            }

            _logger.LogInformation("Song file is missing. Removing from database.");

            await songRepo.DeleteAsync(song.Id); // Assuming your repo has a DeleteAsync(id)

            // --- REPLACED: Refresh the UI by re-running the current search ---
            _searchQuerySubject.OnNext(CurrentTqlQuery);
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
    public partial string LyricsAlbumNameSearch { get; set; }

    [ObservableProperty]
    public partial string LyricsTrackNameSearch { get; set; }

    [ObservableProperty]
    public partial string LyricsArtistNameSearch { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasLyricsSearchResults))]
    public partial ObservableCollection<LrcLibLyrics> LyricsSearchResults { get; set; } = new();

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
                var artistName = SelectedSong.ArtistName
                    .Split("| ", StringSplitOptions.RemoveEmptyEntries)
                    .FirstOrDefault();
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
            //ILyricsMetadataService _lyricsMetadataService = IPlatformApplication.Current!.Services.GetService<ILyricsMetadataService>()!;
            CancellationTokenSource cts = new();

            IEnumerable<LrcLibLyrics>? results = await _lyricsMetadataService.SearchLyricsAsync(
                LyricsTrackNameSearch,
                LyricsArtistNameSearch,
                LyricsAlbumNameSearch,
                cts.Token);

            foreach (var result in results)
            {
                LyricsSearchResults.Add(result);
            }
            _logger.LogInformation(
                "Successfully fetched {Count} lyrics search results for '{Query}'",
                LyricsSearchResults.Count,
                query);

            if (LyricsSearchResults.Count == 0)
            {
                _logger.LogInformation("No lyrics found for the search query: {Query}", query);
                await Shell.Current
                    .DisplayAlert("No Results", "No lyrics found for the specified search criteria.", "OK");
            }
            else
            {
                _logger.LogInformation(
                    "Found {Count} lyrics results for query: {Query}",
                    LyricsSearchResults.Count,
                    query);
            }

            await Shell.Current
                .DisplayAlert("Search Complete", $"Found {LyricsSearchResults.Count} results for '{query}'", "OK");


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
            LyricsMgtFlow.LoadLyrics(selectedResult.SyncedLyrics);


            var lyricsInfo = new LyricsInfo();
            lyricsInfo.Parse(selectedResult.SyncedLyrics);


            await _lyricsMetadataService.SaveLyricsForSongAsync(
                SelectedSong.Id,
                string.Empty,
                selectedResult.SyncedLyrics);


            SelectedSong.SyncLyrics = selectedResult.SyncedLyrics;
            SelectedSong.HasLyrics = true;


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


        bool confirm = await Shell.Current
            .DisplayAlert(
                "Unlink Lyrics",
                "Are > sure > want to unlink the lyrics from this song? This will remove all synced lyrics.",
                "Yes, Unlink",
                "Cancel");

        if (!confirm)
            return;


        var emptyLyricsInfo = new LyricsInfo();
        await _lyricsMetadataService.SaveLyricsForSongAsync(SelectedSong.Id, string.Empty, string.Empty);


        SelectedSong.SyncLyrics = string.Empty;
        SelectedSong.UnSyncLyrics = string.Empty;
        SelectedSong.HasLyrics = false;


        LyricsMgtFlow.LoadLyrics(string.Empty);
    }

    [RelayCommand]
    public async Task ApplyNewSongEdits(SongModelView song)
    {
        if (song == null)
            return;
        _logger.LogInformation("Applying edits to song '{SongTitle}'", song.Title);

        var songInDb = songRepo.GetById(song.Id);
        if (songInDb == null)
        {
            _logger.LogWarning("Song with ID {SongId} not found in database.", song.Id);
            return;
        }
        // Update fields
        songInDb.Title = song.Title;
        songInDb.ArtistName = song.ArtistName;
        songInDb.AlbumName = song.AlbumName;
        songInDb.GenreName = song.GenreName;


        songInDb.TrackNumber = song.TrackNumber;
        songInDb.ReleaseYear = song.ReleaseYear;
        songInDb.Rating = song.Rating;
        songInDb.IsFavorite = song.IsFavorite;
        songInDb.OtherArtistsName = song.OtherArtistsName;
        songInDb.Composer = song.Composer;
        // Handle Artist relationship
        if (!string.IsNullOrWhiteSpace(song.ArtistName))
        {
            var artist = artistRepo.Query(a => a.Name == song.ArtistName).FirstOrDefault();
            if (artist == null)
            {
                artist = artistRepo.Create(new ArtistModel { Name = song.ArtistName });
            }
            songInDb.Artist = artist;
            if (!songInDb.ArtistToSong.Contains(artist))
            {
                songInDb.ArtistToSong.Add(artist);
            }
        }
        // Handle Album relationship
        if (!string.IsNullOrWhiteSpace(song.AlbumName))
        {
            var album = albumRepo.Query(a => a.Name == song.AlbumName).FirstOrDefault();
            if (album == null)
            {
                var albumArtist = artistRepo.Query(a => a.Name == song.ArtistName).FirstOrDefault();
                album = albumRepo.Create(new AlbumModel { Name = song.AlbumName, Artist = albumArtist });
            }
            songInDb.Album = album;
        }

        // Handle Genre relationship
        if (!string.IsNullOrWhiteSpace(song.GenreName))
        {
            var genre = genreRepo.Query(g => g.Name == song.GenreName).FirstOrDefault();
            if (genre == null)
            {
                genre = genreRepo.Create(new GenreModel { Name = song.GenreName });
            }
            songInDb.Genre = genre;
        }

        // Save changes
        await songRepo.UpdateAsync(
            songInDb.Id,
            _ =>
            {
            }); // Trigger update
        var updatedSongView = _mapper.Map<SongModelView>(songRepo.GetById(song.Id));
        //MasterListContext.SetSearchQuery(MasterListContext.CurrentTqlQuery);
    }


    /// <summary>
    /// Quickly assigns a single song to an existing artist. This is a lightweight "move" operation.
    /// </summary>
    /// <param name="context">A tuple containing the Song to change and the target Artist.</param>

    public async Task AssignSongToArtistAsync((SongModelView Song, ArtistModelView TargetArtist) context)
    {
        if (context.Song == null || context.TargetArtist == null)
            return;

        _logger.LogInformation(
            "Assigning song '{SongTitle}' to artist '{ArtistName}'",
            context.Song.Title,
            context.TargetArtist.Name);


        await songRepo.UpdateAsync(
            context.Song.Id,
            songInDb =>
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
        //MasterListContext.SetSearchQuery(MasterListContext.CurrentTqlQuery);
        //CurrentViewContext.SetSearchQuery(CurrentViewContext.CurrentTqlQuery);
        // Maybe even show a notification
        await ShowNotification("Artist updated successfully!");
    }

    /// <summary>
    /// Creates a new artist in the database and assigns the selected song(s) to it. Useful for quickly categorizing
    /// untagged files.
    /// </summary>
    /// <param name="songsToAssign">The list of songs to assign to the new artist.</param>
    [RelayCommand]
    private async Task CreateArtistAndAssignSongsAsync()
    {
        var songsToAssign = _searchResults;
        if (songsToAssign == null || !songsToAssign.Any())
            return;


        string? newArtistName = await Shell.Current
            .DisplayPromptAsync("Create New Artist", "Enter the name for the new artist:");

        if (string.IsNullOrWhiteSpace(newArtistName))
            return;

        _logger.LogInformation(
            "Creating new artist '{ArtistName}' and assigning {Count} songs.",
            newArtistName,
            songsToAssign.Count);


        var newArtist = new ArtistModel { Name = newArtistName };
        var createdArtist = artistRepo.Create(newArtist);


        var songIds = songsToAssign.Select(s => s.Id).ToList();
        foreach (var songId in songIds)
        {
            songRepo.Update(
                songId,
                songInDb =>
                {
                    songInDb.ArtistToSong.Clear();
                    songInDb.ArtistToSong.Add(createdArtist);
                    songInDb.Artist = createdArtist;
                    songInDb.ArtistName = createdArtist.Name;
                });
        }


        var updatedSongs = _mapper.Map<List<SongModelView>>(songRepo.Query(s => songIds.Contains(s.Id)));
    }


    //public SearchContextViewModel ArtistSongsContext { get; }
    //public SearchContextViewModel MasterListContext { get; }
    //public SearchContextViewModel CurrentViewContext { get; }


    /// <summary>
    /// Merges multiple songs into a single album, creating the album if it doesn't exist. This is the core command for
    /// "compiling" an album from loose tracks.
    /// </summary>
    /// <param name="songsToAlbumize">The list of songs to group into an album.</param>
    [RelayCommand]
    private async Task GroupSongsIntoAlbumAsync()
    {
        var songsToAlbumize = _searchResults;
        if (songsToAlbumize == null || !songsToAlbumize.Any())
            return;


        string? albumName = await Shell.Current.DisplayPromptAsync("Group into Album", "Enter the album name:");
        if (string.IsNullOrWhiteSpace(albumName))
            return;


        string? albumArtistName = await Shell.Current
            .DisplayPromptAsync(
                "Group into Album",
                "Enter the album artist name:",
                initialValue: songsToAlbumize.First().ArtistName);
        if (string.IsNullOrWhiteSpace(albumArtistName))
            return;


        var albumArtist = artistRepo.Query(a => a.Name == albumArtistName).FirstOrDefault() ??
            artistRepo.Create(new ArtistModel { Name = albumArtistName });
        var album = albumRepo.Query(a => a.Name == albumName).FirstOrDefault() ??
            albumRepo.Create(new AlbumModel { Name = albumName, Artist = albumArtist });


        var songIds = songsToAlbumize.Select(s => s.Id).ToList();
        songRepo.UpdateMany(
            songIds,
            songInDb =>
            {
                songInDb.Album = album;
                songInDb.AlbumName = album.Name;
                songInDb.OtherArtistsName = albumArtist.Name;
            });


        var updatedSongs = _mapper.Map<List<SongModelView>>(songRepo.Query(s => songIds.Contains(s.Id)));
    }


    /// <summary>
    /// Applies a single genre to a batch of selected songs.
    /// </summary>
    /// <param name="songsToGenre">The songs to apply the genre to.</param>
    [RelayCommand]
    private async Task ApplyGenreToSongsAsync()
    {
        var songsToGenre = _searchResults;
        if (songsToGenre == null || !songsToGenre.Any())
            return;

        string? genreName = await Shell.Current.DisplayPromptAsync("Apply Genre", "Enter the genre to apply:");
        if (string.IsNullOrWhiteSpace(genreName))
            return;

        var realm = RealmFactory.GetRealmInstance(); // Use a local realm instance for the transaction
        var songIds = songsToGenre.Select(s => s.Id).ToList();

        await realm.WriteAsync(
            () =>
            {
                //search realm using RQL directly. its fast and safe
                //var RQL


                var genre = realm.All<GenreModel>().ToList().FirstOrDefault(g => g.Name == genreName) ??
                    new GenreModel { Name = genreName };

                var songsInDb = realm.All<SongModel>().Where(s => songIds.Contains(s.Id));
                foreach (var songInDb in songsInDb)
                {
                    songInDb.Genre = genre;
                    songInDb.GenreName = genre.Name;
                }
            });

        // --- REPLACED: Refresh the UI by re-running the current search ---
        _searchQuerySubject.OnNext(CurrentTqlQuery);
        await ShowNotification($"Genre '{genreName}' applied to {songsToGenre.Count} songs.");
    }

    /// <summary>
    /// Applies one or more tags (comma-separated) to a batch of selected songs.
    /// </summary>
    /// <param name="songsToTag">The songs to apply tags to.</param>
    [RelayCommand]
    private async Task ApplyTagsToSongsAsync()
    {
        var songsToTag = _searchResults;
        if (songsToTag == null || !songsToTag.Any())
            return;

        string? tagsInput = await Shell.Current.DisplayPromptAsync("Apply Tags", "Enter tags, separated by commas:");
        if (string.IsNullOrWhiteSpace(tagsInput))
            return;

        var tagNames = tagsInput.Split(',', ';').Select(t => t.Trim()).Where(t => !string.IsNullOrEmpty(t)).ToList();
        var songIds = songsToTag.Select(s => s.Id).ToList();

        songRepo.UpdateMany(
            songIds,
            songInDb =>
            {
                foreach (var tagName in tagNames)
                {
                    if (!songInDb.Tags.Any(t => t.Name.Equals(tagName, StringComparison.OrdinalIgnoreCase)))
                    {
                        songInDb.Tags.Add(new TagModel { Name = tagName });
                    }
                }
            });


        _searchQuerySubject.OnNext(CurrentTqlQuery);
        await ShowNotification($"Tags applied to {songsToTag.Count} songs.");
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


        var queryWithoutDirectives = Regex.Replace(
            currentQuery,
            @"(asc|desc|random|shuffle|first|last)\s*\w*\s*",
            "",
            RegexOptions.IgnoreCase)
            .Trim();

        _searchQuerySubject.OnNext($"{queryWithoutDirectives} {sortClause}");
    }

    public void UpdateQueryWithClause(string tqlClause, bool isExclusion)
    {
        if (string.IsNullOrWhiteSpace(tqlClause))
            return;


        var currentQuery = CurrentTqlQuery.Trim();
        string newQuery;

        // The clause from the UI might contain spaces, so if we're appending it,
        // it's safest to wrap it in parentheses to ensure it's treated as a single unit.
        // e.g., "year:>2000 add (artist:\"Red Hot Chili Peppers\")"
        string clauseUnit = tqlClause.Contains(' ') ? $"({tqlClause})" : tqlClause;

        // Case 1: The search box is empty, or just a non-filter keyword.
        // In this case, the new clause becomes the entire query. We don't use 'add' or 'exclude'.
        if (string.IsNullOrWhiteSpace(currentQuery) || currentQuery.Equals("random", StringComparison.OrdinalIgnoreCase))
        {
            // If the first action is an exclusion, it's implicitly "everything BUT this".
            // Your parser handles "not (...)" correctly at the start of a query.
            newQuery = isExclusion ? $"not {clauseUnit}" : clauseUnit;
        }
        // Case 2: There is an existing query to modify.
        else
        {
            // Use the powerful TQL keywords your parser understands.
            string connector = isExclusion ? "exclude" : "add";
            newQuery = $"{currentQuery} {connector} {clauseUnit}";
        }

        // Update the property and trigger the search pipeline
        CurrentTqlQuery = newQuery;
        _searchQuerySubject.OnNext(CurrentTqlQuery);
    }

    public ObservableCollection<ActiveFilterViewModel> ActiveFilters { get; } = new();

    [ObservableProperty]
    public partial bool IsFirmSearchEnabled { get; set; }

    /// <summary>
    /// The main command for adding a new filter. This is the heart of the Lego system. It's smart and knows how to ask
    /// the user for input based on the field type.
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
                string? value = await Shell.Current
                    .DisplayPromptAsync($"Filter by {fieldDef.PrimaryName}", "Enter the text to search for:");
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

                string? numValue = await Shell.Current
                    .DisplayPromptAsync($"Filter by {fieldDef.PrimaryName}", "Enter the value (e.g., >2000 or 3:30):");
                if (!string.IsNullOrWhiteSpace(numValue))
                {
                    tqlClause = $"{tqlField}:{numValue}";
                    displayText = $"{fieldDef.PrimaryName} {numValue}";
                }
                break;

            case FieldType.Date:


                string? dateValue = await Shell.Current
                    .DisplayPromptAsync(
                        $"Filter by {fieldDef.PrimaryName}",
                        "Enter a date or range (e.g., today, last month, 2023-12-25):");
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

        IsCreatingSegment = false;
    }


    public async Task LoadUserLastFMDataAsync()
    {
        ListOfUserRecentTracks = await lastfmService.GetUserRecentTracksAsync(lastfmService.AuthenticatedUser, 50);

        LastFMUserInfo = await lastfmService.GetUserInfoAsync();

        CollectionUserTopTracks = await lastfmService.GetUserTopTracksAsync();

        ListOfUserLovedTracks = await lastfmService.GetLovedTracksAsync();
        ListOfGetTopArtistsChart = await lastfmService.GetTopArtistsChartAsync();
        ListOfSimilarTracks = await lastfmService.GetSimilarAsync(
            CurrentPlayingSongView.ArtistName,
            CurrentPlayingSongView.Title);
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
            $"Are > sure > want to permanently delete {songsToDelete.Count()} song(s)? This will remove the files from disk and cannot be undone.",
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
    /// Flags a song as "hidden". This requires a change to >r core filtering logic to be effective.
    /// </summary>
    /// <param name="songToBlacklist">The song to hide from the library.</param>
    [RelayCommand]
    public async Task BlacklistSong(SongModelView songToBlacklist)
    {
        if (songToBlacklist == null)
            return;

        using var realm = RealmFactory.GetRealmInstance();

        await realm.WriteAsync(
            () =>
            {
                var songInDb = realm.Find<SongModel>(songToBlacklist.Id);
                if (songInDb != null)
                {
                    songInDb.IsHidden = true;
                }
            });


        _logger.LogInformation("Blacklisted song '{Title}'. It will be hidden from view.", songToBlacklist.Title);
    }


    public enum FileOperation
    {
        Copy,
        Move,
        Delete
    }

    /// <summary>
    /// Private helper to handle the logic for copying, moving, or deleting song files and updating the database.
    /// </summary>
    public async Task PerformFileOperationAsync(
        IEnumerable<SongModelView> songs,
        string destinationPath,
        FileOperation operation)
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
                    _logger.LogWarning(
                        "Skipping file operation for '{Title}' because source file was not found at '{Path}'.",
                        song.Title,
                        sourcePath);
                    continue;
                }

                if (operation == FileOperation.Delete)
                {
                    File.Delete(sourcePath);
                    if (PlaybackQueue.Contains(song))
                    {
                        _playbackQueueSource.Remove(song);
                    }
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


        if (processedSongIds.Count == 0)
            return;

        using var realm = RealmFactory.GetRealmInstance();
        await realm.WriteAsync(
            () =>
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
            _searchQuerySubject.OnNext(CurrentTqlQuery);
        }
        else if (operation == FileOperation.Move)
        {
            foreach (var song in songs.Where(s => processedSongIds.Contains(s.Id)))
            {
                song.FilePath = Path.Combine(destinationPath, Path.GetFileName(song.FilePath));
            }
        }

        _logger.LogInformation(
            "Successfully completed file operation '{Operation}' for {Count} songs.",
            operation,
            processedSongIds.Count);
    }

    [ObservableProperty]
    public partial ObservableCollection<LyricEditingLineViewModel> LyricsInEditor { get; set; }

    [ObservableProperty]
    public partial bool IsLyricEditorActive { get; set; }

    public int _currentLineIndexToTimestamp = 0;


    /// <summary>
    /// Takes plain text, splits it into lines, and prepares the editor UI.
    /// </summary>
    [RelayCommand]
    public void StartLyricsEditingSession(string plainText)
    {
        if (string.IsNullOrWhiteSpace(plainText))
            return;

        var rawLines = plainText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .ToList();

        if (rawLines.Count == 0)
            return;

        var processedLines = new List<LyricEditingLineViewModel>();
        LyricEditingLineViewModel? lastSectionHeader = null;
        int i = 0;

        while (i < rawLines.Count)
        {
            string line = rawLines[i];

            // --- [1] Detect SECTION HEADERS ----------------------------------
            if (line.StartsWith("[") && line.EndsWith("]"))
            {
                string sectionName = line.Trim('[', ']');
                processedLines.Add(new LyricEditingLineViewModel
                {
                    Text = sectionName,
                    IsSectionHeader = true,
                    SectionType = GetUniversalSectionType(sectionName)
                });
                lastSectionHeader = processedLines.Last();
                i++;
                continue;
            }

            // --- [2] Detect LINE-LEVEL repeats (e.g. "Não te percas ... x2") ----
            if (Regex.IsMatch(line, @"x\d+$"))
            {
                var match = Regex.Match(line, @"x(\d+)$");
                if (match.Success && processedLines.Count > 0)
                {
                    int repeatCount = int.Parse(match.Groups[1].Value);
                    var lastLine = processedLines.LastOrDefault(l => !l.IsSectionHeader);
                    if (lastLine != null)
                    {
                        for (int j = 1; j < repeatCount; j++)
                            processedLines.Add(new LyricEditingLineViewModel
                            {
                                Text = lastLine.Text,
                                IsRepeated = true,
                                BelongsToSection = lastSectionHeader?.SectionType
                            });
                    }
                }
                i++;
                continue;
            }

            // --- [3] Detect [RepeatStart] ... [RepeatEnd xN] ---
            if (line.Equals("[RepeatStart]", StringComparison.OrdinalIgnoreCase))
            {
                int start = i + 1;
                int end = -1;
                int repeatCount = 1;

                for (int j = start; j < rawLines.Count; j++)
                {
                    if (Regex.IsMatch(rawLines[j], @"^\[RepeatEnd(?:\s+x(\d+))?\]$", RegexOptions.IgnoreCase))
                    {
                        end = j - 1;

                        var match = Regex.Match(rawLines[j], @"x(\d+)", RegexOptions.IgnoreCase);
                        if (match.Success)
                            repeatCount = int.Parse(match.Groups[1].Value);
                        break;
                    }
                }

                if (end != -1)
                {
                    var block = rawLines.Skip(start).Take(end - start + 1).ToList();
                    for (int r = 0; r < repeatCount; r++)
                    {
                        foreach (var bl in block)
                        {
                            processedLines.Add(new LyricEditingLineViewModel
                            {
                                Text = bl,
                                BelongsToSection = lastSectionHeader?.SectionType,
                                IsRepeated = r > 0
                            });
                        }
                    }

                    i = end + 2;
                    continue;
                }
            }

            // --- [4] Normal lyric line -----------------------------------------
            processedLines.Add(new LyricEditingLineViewModel
            {
                Text = line,
                BelongsToSection = lastSectionHeader?.SectionType
            });

            i++;
        }

        LyricsInEditor = new ObservableCollection<LyricEditingLineViewModel>(processedLines);
        _currentLineIndexToTimestamp = 0;
        if (LyricsInEditor.Any())
            LyricsInEditor[0].IsCurrentLine = true;

        IsLyricEditorActive = true;
        _logger.LogInformation("Started lyrics editing session with {Count} lines.", LyricsInEditor.Count);
    }
    private string GetUniversalSectionType(string name)
    {
        string n = name.ToLowerInvariant();

        string[] intro = { "intro", "entrada", "introduction", "ouverture" };
        string[] verse = { "verse", "couplet", "estrofe", "estrofa", "strofa" };
        string[] prechorus = { "pre-chorus", "prechorus", "pré-refrain", "pré-chorus", "precor", "pré-coro" };
        string[] chorus = { "chorus", "refrain", "coro", "refrao", "ritornello" };
        string[] bridge = { "bridge", "pont", "ponte" };
        string[] outro = { "outro", "finale", "conclusion" };
        string[] instrumental = { "instrumental", "interlude", "solo" };

        if (intro.Any(n.Contains)) return "Intro";
        if (verse.Any(n.Contains)) return "Verse";
        if (prechorus.Any(n.Contains)) return "Pre-Chorus";
        if (chorus.Any(n.Contains)) return "Chorus";
        if (bridge.Any(n.Contains)) return "Bridge";
        if (outro.Any(n.Contains)) return "Outro";
        if (instrumental.Any(n.Contains)) return "Instrumental";

        return "Generic";
    }

    async partial void OnIsLyricEditorActiveChanged(bool oldValue, bool newValue)
    {
        if (newValue)
        {
            IsTimestampingInProgress = true;

            Shell.Current.FlyoutBehavior = FlyoutBehavior.Flyout;
        }
        else
        {
            IsTimestampingInProgress = false;
            Shell.Current.FlyoutBehavior = FlyoutBehavior.Flyout;
        }

    }

    [ObservableProperty]
    public partial bool IsTimestampingInProgress { get; set; }

    [ObservableProperty]
    public partial string CurrentSongPlainLyricsEdit { get; set; }
    partial void OnCurrentSongPlainLyricsEditChanged(string oldValue, string newValue)
    {
        if (!string.IsNullOrEmpty(newValue))
        {

        }
    }
    [ObservableProperty]
    public partial List<LrcLibLyrics>? ListOfPlainLyricsFromLrcLib { get; set; }

    /// <summary>
    /// This is the main action button. It grabs the current playback time and applies it to the current lyric line.
    /// </summary>
    [RelayCommand]
    public void TimestampCurrentLyricLine(LyricEditingLineViewModel lyricIndex)
    {
        _currentLineIndexToTimestamp = LyricsInEditor.IndexOf(lyricIndex);

        if (!IsLyricEditorActive || _currentLineIndexToTimestamp >= LyricsInEditor.Count)
        {
            return;
        }
        if (lyricIndex.IsSectionHeader || lyricIndex.SectionType == "Generic")
            return;

        var currentTime = TimeSpan.FromSeconds(_audioService.CurrentPosition);
        var timestampString = currentTime.ToString(@"mm\:ss\.ff");


        LyricEditingLineViewModel? currentLine = lyricIndex;
        currentLine.Timestamp = $"[{timestampString}]";
        currentLine.IsTimed = true;
        currentLine.IsCurrentLine = false;


        if (_currentLineIndexToTimestamp < LyricsInEditor.Count)
        {
            LyricsInEditor[_currentLineIndexToTimestamp].IsCurrentLine = true;
        }
        else
        {
            _logger.LogInformation("All lyric lines have been timestamped.");
        }
    }


    public void SaveLyricEditedByUserBeforeTimestamping(LyricEditingLineViewModel lyricIndex, string newText)
    {
        if (!IsLyricEditorActive)
        {
            return;
        }
        var line = lyricIndex;
        line.Text = newText;
    }


    [RelayCommand]
    public void DeleteTimestampFromLine(LyricEditingLineViewModel lyricIndex)
    {
        if (!IsLyricEditorActive)
        {
            return;
        }

        var line = lyricIndex;
        LyricsInEditor.Remove(line);
        line.Timestamp = string.Empty;
        line.IsTimed = false;
        line.IsCurrentLine = false;
    }
    [ObservableProperty]
    public partial int SingleSongPageTabIndex { get; set; }
    [ObservableProperty]
    public partial bool IsSearchingLyricsOnline { get; set; }
    [RelayCommand]
    public async Task ContributeToLrcLib()
    {
        CancellationTokenSource cts = new();
        if (SelectedSong == null)
            return;
        if (string.IsNullOrWhiteSpace(SelectedSong.SyncLyrics))
        {
            var ress = await Shell.Current.DisplayAlert("No Lyrics", "Do You wish to synchronize and contribute?", "Yes", "Cancel");
            if (!ress) return;

            SingleSongPageTabIndex = 1;
            IsSearchingLyricsOnline = true;
            ListOfPlainLyricsFromLrcLib = await _lyricsMetadataService.GetAllPlainLyricsOnlineAsync(SelectedSong, cts.Token);
            if (ListOfPlainLyricsFromLrcLib is not null)
            {

                if (ListOfPlainLyricsFromLrcLib.Count == 1)
                {
                    var plainLyr = ListOfPlainLyricsFromLrcLib[0].PlainLyrics;
                    if (!string.IsNullOrEmpty(plainLyr))
                    {
                        CurrentSongPlainLyricsEdit = plainLyr;
                    }
                }
            }
            IsSearchingLyricsOnline = false;
            return;
        }
        var syncedLyrics = SelectedSong.SyncLyrics;
        var plainLyrics = SelectedSong.UnSyncLyrics;
        //ensure plain lyrics does not have timestamps

        var timestampPattern = new Regex(@"\[\d{2}:\d{2}(?:\.\d{2})?\]");

        plainLyrics = timestampPattern.Replace(plainLyrics ?? string.Empty, string.Empty).Trim();


        LrcLibPublishRequest newRequest = new LrcLibPublishRequest
        {
            TrackName = SelectedSong.Title,
            ArtistName = SelectedSong.ArtistName,
            AlbumName = SelectedSong.AlbumName ?? string.Empty,
            Duration = (int)SelectedSong.DurationInSeconds,
            PlainLyrics = plainLyrics ?? string.Empty,
            SyncedLyrics = syncedLyrics ?? string.Empty
        };
        CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(2));

        var res = await _lyricsMetadataService.PublishLyricsAsync(newRequest, cancellationTokenSource.Token);
        if (res)
        {
            await _dialogueService.ShowAlertAsync(
                "Thank you!",
                "Your contribution has been submitted to the LRC library. It may take some time for it to be reviewed and published.",
                "OK");
        }
        else
        {
            await _dialogueService.ShowAlertAsync(
                "Submission Failed",
                "There was an error submitting your lyrics. Please try again later.",
                "OK");
        }
    }

    /// <summary>
    /// Builds the final LRC content from the editor and saves it using the existing service.
    /// </summary>
    [RelayCommand]
    public async Task SaveTimestampedLyrics(string plainLyrics)
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

        await _lyricsMetadataService.SaveLyricsForSongAsync(
            songToUpdate.Id,
            plainLyrics,
            finalLrcContent);

        await Clipboard.Default.SetTextAsync(finalLrcContent);

        IsLyricEditorActive = false;
        LyricsInEditor.Clear();
        // offer to save file Next to MusicFile as .lrc 
        var userSaveChoice = await _dialogueService.ShowConfirmationAsync(
            "Lyrics Saved",
            "The timestamped lyrics have been saved to the database and copied to your clipboard. Would you like to save a copy as an .lrc file next to the music file?",
            "Yes, Save .lrc",
            "No, Thanks");
        if (!userSaveChoice)
        {
            return;
        }
        else
        {
            try
            {
                var musicFilePath = songToUpdate.FilePath;
                var lrcFilePath = Path.ChangeExtension(musicFilePath, ".lrc");
                await File.WriteAllTextAsync(lrcFilePath, finalLrcContent);
                _logger.LogInformation("Saved .lrc file to '{LrcPath}'", lrcFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save .lrc file.");
                await _dialogueService.ShowAlertAsync("Error", "Failed to save .lrc file.", "OK");
            }
        }
    }

    [RelayCommand]
    private void CancelLyricsEditingSession()
    {
        IsLyricEditorActive = false;
        LyricsInEditor?.Clear();
        _logger.LogInformation("Lyrics editing session cancelled.");
    }

    public async Task LoadPlainLyricsFromFile(string PickedPath)
    {
        var fileContent = await File.ReadAllTextAsync(PickedPath);
        StartLyricsEditingSession(fileContent);
    }

    [RelayCommand]
    public async Task LoadSyncLyricsFromLrcFileOnly()
    {
        var res = await FilePicker.PickAsync(
            new PickOptions
            {
                FileTypes =
                    new FilePickerFileType(
                            new Dictionary<DevicePlatform, IEnumerable<string>>
                            {
                    { DevicePlatform.iOS, new[] { "public.lrc" } },
                    { DevicePlatform.Android, new[] { "text/x-lrc", "application/octet-stream" } },
                    { DevicePlatform.WinUI, new[] { ".lrc" } },
                    { DevicePlatform.Tizen, new[] { "*/*" } },
                    { DevicePlatform.MacCatalyst, new[] { "public.lrc" } },
                            }),
                PickerTitle = "Select an LRC file with synced lyrics"
            });
        if (res == null)
            return;
        var fileContent = await File.ReadAllTextAsync(res.FullPath);

        if (SelectedSong is null) return;
        var parsedLyrics = await _lyricsMetadataService.SaveLyricsForSongAsync(
            SelectedSong.Id,
            string.Empty,
            fileContent);
        if (!parsedLyrics)
        {
            await _dialogueService.ShowAlertAsync(
                "No synced lyrics found in the selected LRC file.",
                "Invalid LRC",
                "OK");
            return;
        }
    }

    public enum PlaylistEditMode
    {
        Add,
        Remove
    }

    [ObservableProperty]
    public partial PlaylistEditMode CurrentEditMode { get; set; } = PlaylistEditMode.Add;

    [ObservableProperty]
    public partial string CurrentTqlQuery { get; set; } = "";

    [ObservableProperty]
    public partial string TQLUserSearchErrorMessage { get; set; }

    [ObservableProperty]
    public partial string NLPQuery { get; set; }

    [ObservableProperty]
    public partial string InvalidField { get; set; }

    [ObservableProperty]
    public partial string? NewFieldSuggestion { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsInCommandMode))]
    public partial IQueryNode TQLParsedCommand { get; set; }

    public bool IsInCommandMode => TQLParsedCommand is not null;

    [RelayCommand]
    private void HandleSongTap(SongModelView tappedSong)
    {
        if (tappedSong is null)
            return;
        string songTitleQueryPart = $"\"{tappedSong.Title}\"";


        string newQuerySegment;
        if (CurrentEditMode == PlaylistEditMode.Add)
        {
            newQuerySegment = $" include title:{songTitleQueryPart}";
        }
        else
        {
            newQuerySegment = $" exclude title:{songTitleQueryPart}";
        }


        var newFullQuery = $"{CurrentTqlQuery}{newQuerySegment}".Trim();


        CurrentTqlQuery = newFullQuery;


        _searchQuerySubject.OnNext(newFullQuery);
    }


    [RelayCommand]
    private void ToggleEditMode()
    { CurrentEditMode = (CurrentEditMode == PlaylistEditMode.Add) ? PlaylistEditMode.Remove : PlaylistEditMode.Add; }

    /// <summary>
    /// Plays a song that has just been transferred from another device, and immediately seeks to the correct starting
    /// position. This ensures a seamless handover.
    /// </summary>
    /// <param name="transferredSong">The SongModelView representing the downloaded file.</param>
    /// <param name="startPositionSeconds">The position to seek to.</param>
    public async Task PlayTransferredSongAsync(SongModelView transferredSong, double startPositionSeconds)
    {
        if (transferredSong == null)
            return;

        // We don't add the transferred song to the main queue, we just play it directly.
        // Or, you could decide to insert it. For now, let's play it as a one-off.

        // Stop any current playback
        if (_audioService.IsPlaying)
        {
            _audioService.Stop();
        }

        _logger.LogInformation(
            "Playing transferred song '{Title}' and seeking to {Position}s.",
            transferredSong.Title,
            startPositionSeconds);

        // Set the current song so the UI updates
        CurrentPlayingSongView = transferredSong;

        // Load and play the new file
        await _audioService.InitializeAsync(transferredSong, startPositionSeconds);

        // Now, seek to the correct position
        if (startPositionSeconds > 0 && startPositionSeconds < transferredSong.DurationInSeconds)
        {
            _audioService.Seek(startPositionSeconds);
        }
    }

    [ObservableProperty]
    public partial bool IsDarkModeOn { get; set; }

    public ObservableCollection<string> QueryChips { get; } = new();

    // We still need a command to handle removing a chip.
    [RelayCommand]
    private void RemoveQueryChip(string chipText)
    {
        if (string.IsNullOrEmpty(chipText))
            return;

        QueryChips.Remove(chipText);

        // After removing a chip, we need to rebuild the full query string
        // and re-run the search.
        string newFullQuery = string.Join(" ", QueryChips);
        _searchQuerySubject.OnNext(newFullQuery);
    }

    /// <summary>
    /// Public method to explicitly trigger a search with a full query string. Used by the "chip" system when a query is
    /// finalized.
    /// </summary>
    public void TriggerSearch(string fullQuery)
    {
        // Update the main query property first
        CurrentTqlQuery = fullQuery;
        // Then push the value into the reactive pipeline
        _searchQuerySubject.OnNext(fullQuery);
    }

    public async Task ApplyCurrentImageToMainArtist(SongModelView? selectedSong)
    {
        if (selectedSong is null)
        {
            if (SelectedSong is null)
            {
                SelectedSong = CurrentPlayingSongView;
            }
            else
            {
                //SelectedSong = SongColView.SelectedItem as SongModelView;
            }
        }
        else
        {
            SelectedSong = selectedSong;
        }

        var realm = RealmFactory.GetRealmInstance();
        await realm.WriteAsync(
            () =>
            {
                var songInDb = realm.Find<SongModel>(selectedSong.Id);
                if (songInDb is null)
                {
                    return;
                }
                var album = songInDb.Album;
                var songArtist = songInDb.Artist;
                if (album is null || songArtist is null)
                {
                    return;
                }

                songArtist.ImagePath = songInDb.CoverImagePath;

                // save changes

                realm.Add(songArtist, update: true);
            });
    }

    [RelayCommand]
    public async Task PickAndApplyImageToSong(SongModelView? selectedSong)
    {
        if (selectedSong is null)
        {
            if (SelectedSong is null)
            {
                SelectedSong = CurrentPlayingSongView;
            }
            else
            {
                //SelectedSong = SongColView.SelectedItem as SongModelView;
            }
        }
        else
        {
            SelectedSong = selectedSong;
        }
        if (SelectedSong is null)
        {
            return;
        }
        var result = await FilePicker.Default
            .PickAsync(new PickOptions { PickerTitle = "Select an image", FileTypes = FilePickerFileType.Images, });
        if (result is null)
        {
            return;
        }


        var realm = RealmFactory.GetRealmInstance();
        await realm.WriteAsync(
            () =>
            {
                var songInDb = realm.Find<SongModel>(selectedSong.Id);
                if (songInDb is null)
                {
                    return;
                }

                songInDb.CoverImagePath = result.FullPath;

                // save changes

                realm.Add(songInDb, update: true);
            });
    }

    [RelayCommand]
    public async Task ApplCurrentImageToSong(SongModelView? selectedSong)
    {
        if (selectedSong is null)
        {
            if (SelectedSong is null)
            {
                SelectedSong = CurrentPlayingSongView;
            }
            else
            {
                //SelectedSong = SongColView.SelectedItem as SongModelView;
            }
        }
        else
        {
            SelectedSong = selectedSong;
        }
        if (SelectedSong is null)
        {
            return;
        }


        var realm = RealmFactory.GetRealmInstance();
        await realm.WriteAsync(
            () =>
            {
                var songInDb = realm.Find<SongModel>(selectedSong.Id);
                if (songInDb is null)
                {
                    return;
                }

                songInDb.CoverImagePath = selectedSong.CoverImagePath;

                // save changes

                realm.Add(songInDb, update: true);
            });
    }

    [RelayCommand]
    public async Task ApplyCurrentImageToAllSongsInAlbum(SongModelView? selectedSong)
    {
        if (selectedSong is null)
        {
            if (SelectedSong is null)
            {
                SelectedSong = CurrentPlayingSongView;
            }
            else
            {
                //SelectedSong = SongColView.SelectedItem as SongModelView;
            }
        }
        else
        {
            SelectedSong = selectedSong;
        }
        if (SelectedSong is null)
        {
            return;
        }

        var realm = RealmFactory.GetRealmInstance();

        await realm.WriteAsync(
            () =>
            {
                var songInDb = realm.Find<SongModel>(selectedSong.Id);
                if (songInDb is null)
                {
                    return;
                }
                var album = songInDb.Album;
                var songsInAlbum = songInDb.Album.SongsInAlbum;
                if (album is null || songsInAlbum is null)
                {
                    return;
                }
                foreach (var song in songsInAlbum)
                {
                    song.CoverImagePath = songInDb.CoverImagePath;
                }

                // save changes

                realm.Add(songInDb, update: true);
            });
    }


    public async Task<(byte[]? imgBytes, Stream? ImgStream)> ShareCurrentPlayingAsStoryInCardLikeGradient(
        SongModelView? selectedSong,
        bool ShareToClipboardInstead = false)
    {
        if (selectedSong is null)
        {
            if (SelectedSong is null)
            {
                SelectedSong = CurrentPlayingSongView;
            }
            else
            {
                //SelectedSong = SongColView.SelectedItem as SongModelView;
            }
        }
        else
        {
            SelectedSong = selectedSong;
        }
        if (SelectedSong is null)
        {
            return (null, null);
        }

        // first create the image with SkiaSharp
        var result = CoverArtService.CreateStoryImageAsync(SelectedSong, null);
        if (ShareToClipboardInstead)
        {
            return (result.stream, result.memStream);
        }
        var imagePath = result.filePath;
        if (string.IsNullOrEmpty(imagePath))
        {
            await Shell.Current.DisplayAlert("Error", "Failed to create story image.", "OK");
            return (null, null);
        }
        // then share it
        ShareFileRequest request = new ShareFileRequest
        {
            Title = $"Share {SelectedSong.Title} by {SelectedSong.ArtistName}",
            File = new ShareFile(imagePath),
        };
        await Share.RequestAsync(request);


        return (null, null);
    }


    public async Task SaveCurrentCoverToDisc(SongModelView? selectedSong)
    {
        // save current cover to disc using file saver in pictures folder
        if (selectedSong is null)
        {
            if (SelectedSong is null)
            {
                SelectedSong = CurrentPlayingSongView;
            }
            else
            {
                ////SelectedSong = SongColView.SelectedItem as SongModelView;
            }
        }
        else
        {
            SelectedSong = selectedSong;
        }
        if (SelectedSong is null)
        {
            return;
        }
        // Save the image to the Pictures folder with a unique name

        var fileName = $"{SelectedSong.Title}_{SelectedSong.ArtistName}.jpg";
        // Use FileSaver from CommunityToolkit.Maui.Storage


        var bytess = File.ReadAllBytes(SelectedSong.CoverImagePath);
        var stream = new MemoryStream(bytess);
        var result = await FileSaver.Default.SaveAsync(fileName, SelectedSong.CoverImagePath, stream);

        if (result.IsSuccessful)
        {
            ShareFileRequest request = new ShareFileRequest
            {
                Title = $"Share {SelectedSong.Title} by {SelectedSong.ArtistName}",
                File = new ShareFile(result.FilePath),
            };
        }
        else
        {
            await Shell.Current.DisplayAlert("Error", $"Failed to save image: {result.Exception.Message}", "OK");
        }
    }

    [ObservableProperty]
    public partial CollectionViewMode CurrentViewMode { get; set; } = CollectionViewMode.Grid;

    // --- NEW: The command that the toggle button will call ---
    [RelayCommand]
    private void ToggleViewMode()
    {
        // Simply cycle between the two modes.
        CurrentViewMode = (CurrentViewMode == CollectionViewMode.Grid)
            ? CollectionViewMode.List
            : CollectionViewMode.Grid;
    }


    public async Task PostAppUpdateAsync(string title, string notes, string url)
    { await ParseStatics.PostNewUpdateAsync(title, notes, url); }
    public async Task SaveTqlQueryAsync(string queryName, string tqlString)
    { await ParseStatics.SaveTqlQueryAsync(queryName, tqlString); }

    [RelayCommand]
    public async Task FindMusicalNeighborsAsync() { await ParseStatics.FindMusicalNeighborsAsync(); }

    [RelayCommand]
    public async Task GetSharedSongDetailsAsync(string sharedSongId)
    { await ParseStatics.GetSharedSongDetailsAsync(sharedSongId); }

    [ObservableProperty]
    public partial bool IsCheckingForUpdates { get; set; }

    [ObservableProperty]
    public partial AppUpdateModel? AppUpdateObj { get; set; }


    [RelayCommand]
    public async Task CheckForAppUpdatesAsync()
    {
        IsCheckingForUpdates = true;
        AppUpdateObj = await ParseStatics.CheckForAppUpdatesAsync();
        IsCheckingForUpdates = false;
    }

    [RelayCommand]
    public async Task DownloadAndInstall()
    {
        if (AppUpdateObj is null || string.IsNullOrWhiteSpace(AppUpdateObj.url))
            return;
        await Browser.Default.OpenAsync(AppUpdateObj.url, BrowserLaunchMode.SystemPreferred);
    }

    public async Task LoadSongDominantColorIfNotYetDoneAsync(SongModelView? song)
    {
        return;
        if (song is null)
            return;
        if (song.CurrentPlaySongDominantColor != null)
        {
            return;
        }
        var color = await ImageResizer.GetDomminantMauiColorAsync(song.CoverImagePath, 1f);
        // i need an inverted BG color that will work well with this dominant color
        if (color is not null)
        {
            var bgColor = color.MultiplyAlpha(0.1f);

            
        }
    }

    public async Task ReAssignDominantColor(SongModelView song)
    {
        if (song is null)
            return;

        var color = await ImageResizer.GetDomminantMauiColorAsync(song.CoverImagePath, 1f);
        song.CurrentPlaySongDominantColor = color;
        if (CurrentPlayingSongView != null && CurrentPlayingSongView.Id == song.Id)
        {
            CurrentPlayingSongView.CurrentPlaySongDominantColor = color;
        }
    }

    #region lastfm
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
        UserLocal.LastFMAccountInfo.Playcount = usr.Playcount;
        UserLocal.LastFMAccountInfo.Playlists = usr.Playlists;
        UserLocal.LastFMAccountInfo.Registered = usr.Registered;
        UserLocal.LastFMAccountInfo.Gender = usr.Gender;
        UserLocal.LastFMAccountInfo.Image = new LastFMUserView.LastImageView();
        UserLocal.LastFMAccountInfo.Image.Url = usr.Images.LastOrDefault()?.Url;
        UserLocal.LastFMAccountInfo.Image.Size = usr.Images.LastOrDefault()?.Size;
        var rlm = RealmFactory.GetRealmInstance();
        rlm.Write(
            () =>
            {
                var usre = rlm.All<UserModel>().ToList();
                if (usre is not null)
                {
                    var usrr = usre.FirstOrDefault();
                    if (usrr is not null)
                    {
                        usrr.LastFMAccountInfo = new();
                        usrr.LastFMAccountInfo.Name = usr.Name;
                        usrr.LastFMAccountInfo.RealName = usr.RealName;
                        usrr.LastFMAccountInfo.Url = usr.Url;
                        usrr.LastFMAccountInfo.Country = usr.Country;
                        usrr.LastFMAccountInfo.Age = usr.Age;
                        usrr.LastFMAccountInfo.Playcount = usr.Playcount;
                        usrr.LastFMAccountInfo.Playlists = usr.Playlists;
                        usrr.LastFMAccountInfo.Registered = usr.Registered;
                        usrr.LastFMAccountInfo.Gender = usr.Gender;

                        usrr.LastFMAccountInfo.Image = new LastFMUser.LastImage();
                        usrr.LastFMAccountInfo.Image.Url = usr.Images.LastOrDefault().Url;
                        usrr.LastFMAccountInfo.Image.Size = usr.Images.LastOrDefault().Size;
                        rlm.Add(usrr, update: true);
                    }
                    else
                    {
                        usrr = new UserModel();
                        usrr.LastFMAccountInfo = new();
                        usrr.Id = new();
                        usrr.UserName = usr.Name;
                        usrr.LastFMAccountInfo.Name = usr.Name;
                        usrr.LastFMAccountInfo.RealName = usr.RealName;
                        usrr.LastFMAccountInfo.Url = usr.Url;
                        usrr.LastFMAccountInfo.Country = usr.Country;
                        usrr.LastFMAccountInfo.Age = usr.Age;
                        usrr.LastFMAccountInfo.Playcount = usr.Playcount;
                        usrr.LastFMAccountInfo.Playlists = usr.Playlists;
                        usrr.LastFMAccountInfo.Registered = usr.Registered;
                        usrr.LastFMAccountInfo.Gender = usr.Gender;
                        usrr.LastFMAccountInfo.Image = new LastFMUser.LastImage();
                        usrr.LastFMAccountInfo.Image.Url = usr.Images.LastOrDefault().Url;
                        usrr.LastFMAccountInfo.Image.Size = usr.Images.LastOrDefault().Size;

                        rlm.Add(usrr, update: true);
                    }
                }
            });
    }


    [ObservableProperty]
    public partial bool IsLastfmAuthenticated { get; set; }


    [ObservableProperty]
    public partial bool LastFMLoginBtnVisible { get; set; } = true;

    [ObservableProperty]
    public partial bool lastFMCOmpleteLoginBtnVisible { get; set; }

    [ObservableProperty]
    public partial bool IsLastFMNeedsToConfirm { get; set; }

    [ObservableProperty]
    public partial bool IsLastFMNeedsUsername { get; set; }

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string LastfmUsername { get; set; }

    [RelayCommand]
    public void LoadLastFMSession()
    {
        lastfmService.LoadSession();
        IsLastfmAuthenticated = lastfmService.IsAuthenticated;
        if (IsLastfmAuthenticated)
        {
            LastFMLoginBtnVisible = false;
            lastFMCOmpleteLoginBtnVisible = false;
            IsLastFMNeedsToConfirm = false;
            _ = LoadUserLastFMInfo();
        }
    }

    [RelayCommand]
    public async Task LoginToLastfm()
    {
        if (string.IsNullOrEmpty(UserLocal.LastFMAccountInfo.Name))
        {
            IsLastFMNeedsUsername = true;
            return;
        }
        IsBusy = true;
        try
        {
            string? webUrl = await lastfmService.GetAuthenticationUrlAsync();

            if (string.IsNullOrEmpty(webUrl)) return;
            IsLastFMNeedsToConfirm = true;
            LastFMLoginBtnVisible = false;
            lastFMCOmpleteLoginBtnVisible = true;
            await Launcher.Default.OpenAsync(new Uri(webUrl));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Last.fm authentication URL.");
            IsBusy = false;
            return;
        }
    }

    [RelayCommand]
    public async Task CompleteLastFMLoginAsync()
    {
        IsBusy = true;
        try
        {
            string? lastFMUName = UserLocal.LastFMAccountInfo.Name;
            if (string.IsNullOrEmpty(lastFMUName)) return;

            IsLastfmAuthenticated = await lastfmService.CompleteAuthenticationAsync(lastFMUName);
            if (IsLastfmAuthenticated)
            {
                IsLastFMNeedsToConfirm = false;
                lastFMCOmpleteLoginBtnVisible = false;
                await LoadUserLastFMInfo();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while completing Last.fm login.");
        }
        finally
        {
            if (IsLastFMNeedsToConfirm)
            {
                await Shell.Current
                    .DisplayAlert(
                        "Error",
                        "Failed to authenticate with Last.fm. Please ensure you have authorized the app in the browser and try again.",
                        "OK");
            }
            IsBusy = false;
        }
    }

    public async Task LoadSelectedSongLastFMData()
    {
        if (SelectedSong is not null)
        {
            SelectedSongLastFMData = null;
            CorrectedSelectedSongLastFMData = null;

            //_ = Task.Run(() => LoadStatsForSelectedSong(SelectedSong));


            SelectedSecondDomColor = await ImageResizer.GetDomminantMauiColorAsync(SelectedSong.CoverImagePath);
            await LoadSongLastFMData();
        }
    }

    [ObservableProperty]
    public partial Hqub.Lastfm.Entities.Track? SelectedSongLastFMData { get; set; }

    [ObservableProperty]
    public partial Hqub.Lastfm.Entities.Track? CorrectedSelectedSongLastFMData { get; set; }

    public async Task LoadSongLastFMData()
    {
        return;
        if (SelectedSong is null || SelectedSong.ArtistName == "Unknown Artist")
        {
            return;
        }


        var artistName = SelectedSong.ArtistName.Split("| ", StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        artistName ??= string.Empty;
        SelectedSongLastFMData = await lastfmService.GetTrackInfoAsync(artistName, SelectedSong.Title);
        if (SelectedSongLastFMData is null)
        {
            return;
        }
        SelectedSongLastFMData.Artist = await lastfmService.GetArtistInfoAsync(artistName);
        SelectedSongLastFMData.Album = await lastfmService.GetAlbumInfoAsync(artistName, SelectedSong.AlbumName);
    }

    public void LoadSongLastFMMoreData()
    {
        if (SelectedSong is null)
        {
            return;
        }
        //SimilarTracks=   await lastfmService.GetSimilarAsync(SelectedSong.ArtistName, SelectedSong.Title);

        //IEnumerable<LrcLibSearchResult>? s = await LyricsMetadataService.SearchOnlineManualParamsAsync(SelectedSong.Title, SelectedSong.ArtistName, SelectedSong.AlbumName);
        //AllLyricsResultsLrcLib = s.ToObservableCollection();
    }


    [ObservableProperty]
    public partial ObservableCollection<Hqub.Lastfm.Entities.Track> SimilarSongs
    {
        get;
        set;
    } = new ObservableCollection<Hqub.Lastfm.Entities.Track>();

    [ObservableProperty]
    public partial ObservableCollection<Hqub.Lastfm.Entities.Track>? SimilarTracks { get; set; }
    #endregion

    [RelayCommand]
    public async Task OpenSongInOnlineSearch(string? service)
    {
        service ??= "google";
        if (SelectedSong is null && CurrentPlayingSongView is not null)
        {
            SelectedSong = CurrentPlayingSongView;
        }
        if (SelectedSong is null)
            return;
        string query = $"{SelectedSong.Title} {SelectedSong.ArtistName}";
        string url = service.ToLower() switch
        {
            "google" => $"https://www.google.com/search?q={Uri.EscapeDataString(query)}",
            "youtube" => $"https://www.youtube.com/results?search_query={Uri.EscapeDataString(query)}",
            "lastfm" => $"https://www.last.fm/search?q={Uri.EscapeDataString(query)}",
            "spotify" => $"https://open.spotify.com/search/{Uri.EscapeDataString(query)}",
            "apple music" => $"https://music.apple.com/us/search?term={Uri.EscapeDataString(query)}",
            "deezer" => $"https://www.deezer.com/en/search/{Uri.EscapeDataString(query)}",
            //"lrclib" => $"https://www.lrclib.com/search?query={Uri.EscapeDataString(query)}",   
            _ => $"https://www.google.com/search?q={Uri.EscapeDataString(query)}",
        };
        await Launcher.Default.OpenAsync(new Uri(url));
    }

    [RelayCommand]
    public async Task SearchSongPlainLyricsnOnlineSearch(string? service)
    {
        // searhc for lyrics online similar to searching for song only 
        //await Launcher.Default.OpenAsync(new Uri(url));

    }

    [RelayCommand]
    public async Task SharePlaylistByFullTQLManualGeneration()
    {
        var StringTQLQueryToAddSongTitleAndArtistName = string.Empty;
        // say SearchResults has 3 songs, then the resulting TQL should be:
        // (title:"Song1" AND artist:"Artist1") Add (title:"Song2" AND artist:"Artist2") Add (title:"Song3" AND artist:"Artist3")
        foreach (var song in SearchResults)
        {
            if (!string.IsNullOrWhiteSpace(StringTQLQueryToAddSongTitleAndArtistName))
            {
                StringTQLQueryToAddSongTitleAndArtistName += " Add ";
            }
            StringTQLQueryToAddSongTitleAndArtistName += $"(title:\"{song.Title}\" AND artist:\"{song.ArtistName}\")";
        }

        // Now share this TQL string
        var shareText = $"Here is my playlist:\n{StringTQLQueryToAddSongTitleAndArtistName}\n\nYou can use this TQL query in the app to load the same playlist.";
        ShareTextRequest request = new ShareTextRequest
        {
            Title = "Share Playlist TQL",
            Text = shareText,
            Subject = "My Playlist TQL",
        };
        await Share.RequestAsync(request);
        await Clipboard.Default.SetTextAsync(shareText);
    }

    [RelayCommand]
    public async Task ShareSongDetailsAsText(SongModelView song)
    {
        if (song is null)
        {
            song = SelectedSong!;
        }
        if (song is null && CurrentPlayingSongView is not null)
        {
            song = CurrentPlayingSongView;
        }
        if (song is null)
            return;

     
           string?  WelDoneMessage = AppUtils.GetWellFormattedSharingTextHavingSongStats(song);
            await  Clipboard.Default.SetTextAsync(WelDoneMessage);
            
            //await Share.Default.RequestAsync(new ShareTextRequest
            //{
            //    Title = $"Share {song.Title} by {song.ArtistName}",
            //    Text = WelDoneMessage,

            //});

            await Share.Default
                .RequestAsync(
                    new ShareFileRequest
                    {
                        Title = WelDoneMessage,
                        File = new ShareFile(song.CoverImagePath),
                    });
           
        
        }

    [RelayCommand]
    public void LoadSongsWithUserNotes()
    {
        SongsWithNotes = SearchResults.Where(s => !string.IsNullOrWhiteSpace(s.UserNoteAggregatedText))
            .ToObservableCollection();
        DifferentUniqueNotes = SearchResults.Select(s => s.UserNoteAggregatedText)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct()
            .ToObservableCollection();
    }

    [ObservableProperty]
    public partial ObservableCollection<SongModelView>? SongsWithNotes { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<string?>? DifferentUniqueNotes { get; set; }

    [ObservableProperty]
    public partial bool ShowWelcomeScreen { get; set; }
    partial void OnShowWelcomeScreenChanged(bool oldValue, bool newValue)
    {
        if (newValue)
        {

        }
    }

    [ObservableProperty]
    public partial WelcomeTabViewIndexEnum WelcomeTabIndexEnum { get; set; }


    [ObservableProperty]
    public partial int WelcomeTabViewItemsCount { get; set; }


    [ObservableProperty]
    public partial bool IsLibraryEmpty { get; set; }

    [ObservableProperty]
    public partial string PickFilesOutputText { get; set; }

    [ObservableProperty]
    public partial string WelcomeStep { get; set; }

    [ObservableProperty]
    public partial string NextBtnText { get; set; } = DimmerLanguage.next_btn;

    [ObservableProperty]
    public partial bool IsLoadingLyrics { get; set; }
    [ObservableProperty]
    public partial bool IsSearchingLyrics { get; set; }


    [RelayCommand]
    public void AppSetupPagePreviousBtnClick()
    {
        var prevInd = (WelcomeTabIndexEnum - 1);

        if (prevInd < 0)
        {
            return;
        }
        NextBtnText = DimmerLanguage.next_btn;
        WelcomeTabIndexEnum--;
    }

    public virtual async Task AppSetupPageNextBtnClick(bool isLastTab)
    {


        if (isLastTab)
        {

            NextBtnText = DimmerLanguage.txt_done;
            await Shell.Current
                .DisplayAlert(
                    Shell.Current.Title,
                    "You have completed the setup wizard. You can access it later from the Help menu.",
                    "OK");
            return;
        }
        WelcomeTabIndexEnum++;

        switch (WelcomeTabIndexEnum)
        {
            case WelcomeTabViewIndexEnum.Folders:
                WelcomeStep = "Step 1 of 3: Add Music Folders";
                break;
            case WelcomeTabViewIndexEnum.LastFM:
                WelcomeStep = "Step 2 of 3: Connect Last.fm (Optional)";
                break;
            case WelcomeTabViewIndexEnum.TQL:
                WelcomeStep = DimmerLanguage.txt_stepthree;
                break;
            default:
                WelcomeStep = string.Empty;
                break;
        }
    }


}


public enum CollectionViewMode
{
    List,
    Grid
}
