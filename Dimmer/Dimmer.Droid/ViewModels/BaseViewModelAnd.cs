
//using System.Reactive.Linq;

using AndroidX.Lifecycle;

using Bumptech.Glide;

using Dimmer.ViewsAndPages.NativeViews.ArtistSection;
using Dimmer.WinUI.UiUtils;



namespace Dimmer.ViewModels;
public partial class BaseViewModelAnd : BaseViewModel, IDisposable
{
    public BaseViewModelAnd(
        AndroidFolderPicker picker,
        IDimmerStateService dimmerStateService, MusicDataService musicDataService,

        IAppInitializerService appInitializerService, IDimmerAudioService audioServ, ISettingsService settingsService, ILyricsMetadataService lyricsMetadataService, SubscriptionManager subsManager, LyricsMgtFlow lyricsMgtFlow, ICoverArtService coverArtService, IFolderMgtService folderMgtService, IRepository<SongModel> _songRepo, IDuplicateFinderService duplicateFinderService, ILastfmService _lastfmService, IRepository<ArtistModel> artistRepo, IRepository<AlbumModel> albumModel, IRepository<GenreModel> genreModel, IDialogueService dialogueService, IRepository<PlaylistModel> PlaylistRepo, IRealmFactory RealmFact, IFolderMonitorService FolderServ, ILibraryScannerService LibScannerService, IRepository<DimmerPlayEvent> DimmerPlayEventRepo, BaseAppFlow BaseAppClass, ILogger<BaseViewModel> logger) : base( dimmerStateService, musicDataService, appInitializerService, audioServ, settingsService, lyricsMetadataService, subsManager, lyricsMgtFlow, coverArtService, folderMgtService, _songRepo, duplicateFinderService, _lastfmService, artistRepo, albumModel, genreModel, dialogueService, PlaylistRepo, RealmFact, FolderServ, LibScannerService, DimmerPlayEventRepo, BaseAppClass, logger)
    {
        fPicker = picker;
        // mapper and stateService are accessible via base class protected fields.
        // _subs (passed as subsManager) is managed by BaseViewModel as _subsManager.

        this._logger = new LoggerFactory().CreateLogger<BaseViewModelAnd>();
        isAppBooting = true;
        this._logger.LogInformation("BaseViewModelAnd initialized.");
        audioService = audioServ;
    }
    public LoginViewModel LoginViewModel => _loginViewModel;
    private readonly LoginViewModel _loginViewModel;
    private readonly IDimmerAudioService audioService;
    private readonly IAnimationService animService;
    private readonly IRepository<SongModel> songRepository;
    private readonly BaseViewModel baseVM;
    AndroidFolderPicker fPicker;
    public BaseViewModel BaseVM => baseVM; // Expose BaseViewModel reference if needed

    public Fragment? PreviousPage { get; set; }
    public Fragment? CurrentFragment { get; set; }

    // Removed local stateService and mapper as they are protected in BaseViewModel



    [ObservableProperty]
    public partial int NowPlayingQueueItemSpan { get; set; }


    [ObservableProperty]
    public partial int NowPlayingTabIndex { get; set; }

    [ObservableProperty]
    public partial bool NowPlayingUI { get; set; }

    partial void OnNowPlayingTabIndexChanged(int oldValue, int newValue)
    {
        
        switch (newValue)
        {
            case 0: IsNowPlayingQueue =false;
                IsNowAllSongsQueue=true;
                NowPlayingUI =false;
               
                break;
            case 1:
                
                
                IsNowPlayingQueue =false;

                IsNowAllSongsQueue=false;
                NowPlayingUI=true;

                break;
            case 2:
                break;
            default:
                break;
        }
    }

    
    [ObservableProperty]
    public partial bool IsNowPlayingQueue { get; set; }
    [ObservableProperty]
    public partial bool IsNowAllSongsQueue { get; set; } = true;


  

    partial void OnNowPlayingQueueItemSpanChanged(int oldValue, int newValue)
    {
        // Handle any additional logic when NowPlayingQueueItemSpan changes, if needed.
        _logger.LogInformation("NowPlayingQueueItemSpan changed from {OldValue} to {NewValue}", oldValue, newValue);
    }


    bool isAppBooting = false;

    public void FiniInit()
    {
        if (isAppBooting)
        {
            isAppBooting = false;
        }
    }

    

    [RelayCommand]
    public static async Task OpenFileInFolder(string filePath)
    {
        Uri uriF = new Uri(filePath);
        if( await Launcher.Default.CanOpenAsync(uriF))
        {

           await Launcher.Default.OpenAsync(new OpenFileRequest()
            {
                File = new ReadOnlyFile(filePath),
                Title = "Open with",

            });
        }
    }



    [ObservableProperty] public partial ObservableCollection<AnimationSetting>? PageAnimations { get; set; }
    public void GetAllAnimations()
    {
        PageAnimations = animService.GetAvailableAnimations().ToObservableCollection();
    }
    public async Task SavePage(Page PageToSave, int duration, bool IsEnter)
    {

        // (Add null checks here for safety)

        // STEP 3: Call our public API to save the settings.
        // This is the key method call you were asking about!
        // It takes the page type and the four chosen AnimationSetting objects.
        AnimationManager.SetPageAnimations(
            PageToSave.GetType(),
            null,
            null,
            null,
            null
        );

        await Shell.Current.DisplayAlert("Success", "Settings saved!", "OK");
    }
    public async Task AddMusicFolderViaPickerAsync()
    {

        try
        {
            if (fPicker is null) return;
            // 2. Call it (No need to request permissions first, the picker handles the grant)
            string? selectedFolderPath = await fPicker.PickFolderAsync();

            if (!string.IsNullOrEmpty(selectedFolderPath))
            {
                _logger.LogInformation($"Native Folder Selected: {selectedFolderPath}");

                // Pass to your logic
                await AddMusicFolderByPassingToService(selectedFolderPath);
            }
            else
            {
                _logger.LogInformation("No folder selected.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Native Picker Failed");

            // Native Alert
            MainApplication.CurrentActivity?.RunOnUiThread(() =>
            {
                var materialDialog = new Google.Android.Material.Dialog.MaterialAlertDialogBuilder(MainApplication.CurrentActivity)?
                    .SetTitle("Error")?
                    .SetMessage(ex.Message)?
                    .SetPositiveButton("OK", (s, e) => { })
                    .Show();


            });
        }
    }
    internal void ViewArtistDetails(ArtistModelView? s)
    {
        ViewArtistDetails(s);
    }

    private bool _isExpanded;


  
    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            // This is how we link the native sheet's state to our UI's appearance.
            if (SetProperty(ref _isExpanded, value))
            {
                OnPropertyChanged(nameof(IsMiniPlayerVisible));
                OnPropertyChanged(nameof(IsFullPlayerVisible));
            }
        }
    }

    // These properties will drive the visibility of our layouts in XAML.
    public bool IsMiniPlayerVisible => !_isExpanded;
    public bool IsFullPlayerVisible => _isExpanded;

    // --- Communication Bridge ---
    // This event is for the ViewModel to tell the native UI what to do.
    public event EventHandler<bool> RequestSheetStateChange;

    // Call this from your MAUI UI (e.g., a tap gesture) to expand the sheet.
    public void TriggerExpand() => RequestSheetStateChange?.Invoke(this, true);

    // Call this from your MAUI UI (e.g., a "down arrow" button) to collapse.
    public void TriggerCollapse() => RequestSheetStateChange?.Invoke(this, false);


    [RelayCommand]
    void PlayClicked()
    {
        TriggerExpand();
    }

    [RelayCommand]
    async Task SkipNextClicked()
    {
        await Shell.Current.DisplayAlert("SkipNext Clicked", "SkipNext button was clicked!", "OK");
    }

    #region INotifyPropertyChanged
    public new event PropertyChangedEventHandler PropertyChanged;
    protected new bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string? propertyName = "")
    {
        if (EqualityComparer<T>.Default.Equals(backingStore, value))
            return false;
        backingStore = value;
        OnPropertyChanged(propertyName);
        return true;
    }
    protected new void OnPropertyChanged([CallerMemberName] string? propertyName = "") =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public void Dispose()
    {

        this.Dispose();
    }

  
    #endregion


    public async Task InitializeDimmerLiveData()
    {
        _loginViewModel.Username=CurrentUserLocal.Username;
        await _loginViewModel.InitializeAsync();
    }
 
    [ObservableProperty]
    public partial Microsoft.Maui.Controls.View SelectedSongView { get; internal set; }

    [ObservableProperty]
    public partial bool IsInMultiSelectMode { get; internal set; }

    [ObservableProperty]
    public partial bool IsSongLongPressed { get; set; }


    [RelayCommand]
    public void AddArtistToTQL(string ArtistName)
    {
        string tqlClause = $"{CurrentTqlQuery} add artist:\"{ArtistName}\"";
        AddFilterToSearch(tqlClause);
    }
    [RelayCommand]
    public void RemoveArtistFromTQL(string artistName)
    {
        if (string.IsNullOrWhiteSpace(CurrentTqlQuery) || string.IsNullOrWhiteSpace(artistName))
            return;

        string query = CurrentTqlQuery;

        // Escape artist name safely
        string escapedArtist = Regex.Escape(artistName);

        // 1️⃣ Remove the last occurrence of artist:"X" (deepest/rightmost)
        // Use regex with rightmost match
        string pattern = $@"(?i)(.*)(artist:\s*{escapedArtist})(.*)";
        if (Regex.IsMatch(query, pattern))
        {
            // Keep everything except the matched artist term
            query = Regex.Replace(query, $@"(?i)(\s*[\(\)]*\s*)(artist:\s*{escapedArtist})", "", RegexOptions.RightToLeft, new TimeSpan(1));
        }

        // 2️⃣ Clean up logical operators left hanging (and/or)
        query = Regex.Replace(query, @"\s*(and|or)\s*(and|or)\s*", " ", RegexOptions.IgnoreCase);
        query = Regex.Replace(query, @"(^\s*(and|or)\s*)|(\s*(and|or)\s*$)", "", RegexOptions.IgnoreCase);

        // 3️⃣ Clean up empty parentheses and extra spaces
        query = Regex.Replace(query, @"\(\s*\)", "");
        query = Regex.Replace(query, @"\s{2,}", " ").Trim();

        // 4️⃣ Fix cases like 'include ()' or leftover operators before closing parenthesis
        query = Regex.Replace(query, @"include\s*\(\s*\)", "", RegexOptions.IgnoreCase);
        query = Regex.Replace(query, @"\(\s*(and|or)\s*\)", "", RegexOptions.IgnoreCase);

        // 5️⃣ Assign and search
        CurrentTqlQuery = query.Trim();
        SearchSongForSearchResultHolder(query);
    }

    [RelayCommand]
    public void AddAlbumToTQL(string AlbumName)
    {
        string tqlClause = $"{CurrentTqlQuery} add album:\"{AlbumName}\"";
        AddFilterToSearch(tqlClause);
    }

    protected override async Task HandlePlaybackStateChange(PlaybackEventArgs args)
    {
        // STEP 1: Always a good practice to let the base class do its work first.
        // This will run the logic in A (setting IsPlaying, etc.).
        await base.HandlePlaybackStateChange(args);


        PlayType? state = StatesMapper.Map(args.EventType);

        if (state == PlayType.Play)
        {
            // Do something that ONLY ViewModel B cares about.
            // For example, maybe B is the VM for a mini-player and needs to
            // trigger a specific animation.
            TriggerMiniPlayerGlowAnimation();
            _logger.LogInformation("Playback started, ViewModel B is reacting specifically.");
        }
        else if (state == PlayType.Pause)
        {
            // Stop the animation.
            StopMiniPlayerGlowAnimation();
        }
    }

    private void StopMiniPlayerGlowAnimation()
    {
        //throw new NotImplementedException();
    }

    private void TriggerMiniPlayerGlowAnimation()
    {
        //throw new NotImplementedException();
    }

    [RelayCommand]
    public async Task DeleteFileFromSystem(SongModelView song)
    {
        bool confirm = await Shell.Current.DisplayAlert("Confirm Delete", $"Are you sure you want to delete '{song.Title}' from your device? This action cannot be undone.", "Delete", "Cancel");
        if (confirm)
        {
            try
            {
                if(TaggingUtils.FileExists(song.FilePath))
                {
                   await RemoveFromQueue(song);
                    var songsToDelete = new List<SongModelView> { song };
                    await PerformFileOperationAsync(songsToDelete, string.Empty, FileOperation.Delete);
                    // Then, remove from the database.
                    await songRepository.DeleteAsync(song.Id);
                    // Optionally, you might want to refresh your UI or notify other components here.

                }

                await Shell.Current.DisplayAlert("Deleted", $"'{song.Title}' has been deleted from your device.", "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"Failed to delete '{song.Title}': {ex.Message}", "OK");
            }
        }
    }

    [RelayCommand]

    public async Task ShareSongViewClipboard(SongModelView song)
    {


        var byteData = await ShareCurrentPlayingAsStoryInCardLikeGradient(song, true);

        string clipboardText = $"{song.Title} - {song.ArtistName}\nAlbum: {song.AlbumName}\n\nShared via Dimmer Music Player v{CurrentAppVersion}";

        if (byteData.imgBytes != null)
        {
           //await Clipboard.SetTextAsync(clipboardText);
            

        }

    }


    #region Navigation Section

    public void NavigateToNowPlayingFragmentFromHome(
        Fragment callerFrag, View sourceArt,
        View sourceTitle, View sourceArtist,
        View sourceAlbum)
    {
        if (callerFrag == null || !callerFrag.IsAdded) return;

        // 1. Setup Unique Transition Names
        string? artTransName = sourceArt?.TransitionName;
        string? titleTransName = sourceTitle?.TransitionName;
        string? artistTransName = sourceArtist?.TransitionName;
        string? albumTransName = sourceAlbum?.TransitionName;


        // 3. Create Destination Fragment and pass the names
        var nowPlayingFrag = new NowPlayingFragment(this)
        {
            //ArtTransitionName = artTransName,
            //TitleTransitionName = titleTransName,
            //ArtistTransitionName = artistTransName,
            //AlbumTransitionName = albumTransName
        };

        CurrentFragment = nowPlayingFrag; // Update your tracking property

        // 4. Define the Shared Element Transition (The Fly Animation)
        var sharedSet = new TransitionSet();
        sharedSet.AddTransition(new ChangeBounds());
        sharedSet.AddTransition(new ChangeTransform());
        sharedSet.AddTransition(new ChangeImageTransform()); // Crucial for ImageViews
        sharedSet.SetDuration(400);
        sharedSet.SetInterpolator(new LinearInterpolator());

        nowPlayingFrag.SharedElementEnterTransition = sharedSet;
        nowPlayingFrag.SharedElementReturnTransition = sharedSet;

        // 5. Define the Page Transition (The Fade In/Out of non-shared stuff)
        // Fade Through is standard for Material 3
        var fadeThrough = new Google.Android.Material.Transition.MaterialFadeThrough();
        fadeThrough.SetDuration(300);
        nowPlayingFrag.EnterTransition = fadeThrough;
        nowPlayingFrag.ExitTransition = fadeThrough;
        var trans = callerFrag.ParentFragmentManager.BeginTransaction()
            .SetReorderingAllowed(true);
        // 6. Add Shared Elements Only if they exist
        if (sourceArt != null && artTransName != null)
            trans.AddSharedElement(sourceArt, artTransName);

        if (sourceTitle != null && titleTransName != null)
            trans.AddSharedElement(sourceTitle, titleTransName);

        if (sourceArtist != null && artistTransName != null)
            trans.AddSharedElement(sourceArtist, artistTransName);

        if (sourceAlbum != null && albumTransName != null)
            trans.AddSharedElement(sourceAlbum, albumTransName);

        trans.Replace(Resource.Id.custom_fragment_container, nowPlayingFrag)
             .AddToBackStack("NowPlaying")
             .Commit();
    }

    public void NavigateToEditSongPage(Fragment callerFragment, string transitionName, List<View> sharedViews)
    {
        var fragment = new EditSingleSongFragment(this, transitionName);

        // Ensure transition name exists
        if (sharedViews.Count > 0 && sharedViews[0] != null)
            sharedViews[0].TransitionName = transitionName;

        // Container Transform
        var container = new MaterialContainerTransform
        {
            DrawingViewId = Resource.Id.custom_fragment_container,
            ScrimColor = Color.Transparent,
            ContainerColor = Color.Transparent,
            FadeMode = MaterialContainerTransform.FadeModeCross
        };
        container.SetDuration(400);

        fragment.SharedElementEnterTransition = container;
        fragment.SharedElementReturnTransition = container; // Reuse same config

        fragment.EnterTransition = new MaterialElevationScale(true);
        callerFragment.ExitTransition = new Hold();

        var trans = callerFragment.ParentFragmentManager.BeginTransaction()
             .SetReorderingAllowed(true);

        if (sharedViews.Count > 0 && sharedViews[0] != null)
            trans.AddSharedElement(sharedViews[0], transitionName);

        trans.Replace(Resource.Id.custom_fragment_container, fragment)
             .AddToBackStack("EditSong")
             .Commit();
    }

    public void NavigateToArtistPage(Fragment callerFrag, string artistId, string artistName, View sharedView)
    {
        if (callerFrag == null || !callerFrag.IsAdded) return;

        var fragment = new ArtistFragment(this, artistName, artistId);
        CurrentFragment = fragment;

        // Shared Element (Image Morph)
        string tName = sharedView?.TransitionName ?? $"artist_{artistId}";
        if (sharedView != null) sharedView.TransitionName = tName;

        var containerTransform = new MaterialContainerTransform
        {
            DrawingViewId = Resource.Id.custom_fragment_container,
            ScrimColor = Color.Transparent,
            ContainerColor = Color.Transparent,
            FadeMode = MaterialContainerTransform.FadeModeThrough,
        };
        containerTransform.SetDuration(400);

        fragment.SharedElementEnterTransition = containerTransform;
        fragment.SharedElementReturnTransition = containerTransform;

        fragment.EnterTransition = new MaterialElevationScale(true);
        fragment.ExitTransition = new MaterialElevationScale(false);

        callerFrag.ExitTransition = new Hold();

        var trans = callerFrag.ParentFragmentManager.BeginTransaction()
            .SetReorderingAllowed(true);

        if (sharedView != null)
            trans.AddSharedElement(sharedView, tName);

        trans.Replace(Resource.Id.custom_fragment_container, fragment)
             .AddToBackStack($"Artist_{artistId}")
             .Commit();
    }

    public void NavigateToArtistEventsStats(Fragment callerFrag)
    {
        if (callerFrag == null || !callerFrag.IsAdded) return;

        var fragment = new ArtistEventsStatsFragment();
        CurrentFragment = fragment;

        // Lateral Slide
        fragment.EnterTransition = new MaterialSharedAxis(MaterialSharedAxis.X, true);
        fragment.ReturnTransition = new MaterialSharedAxis(MaterialSharedAxis.X, false);
        callerFrag.ExitTransition = new MaterialSharedAxis(MaterialSharedAxis.X, true);
        callerFrag.ReenterTransition = new MaterialSharedAxis(MaterialSharedAxis.X, false);

        callerFrag.ParentFragmentManager.BeginTransaction()
            .Replace(Resource.Id.custom_fragment_container, fragment)
            .AddToBackStack("ArtistStats")
            .Commit();
    }

    public void NavigateToSettings(Fragment callerFrag)
    {
        if (callerFrag == null || !callerFrag.IsAdded) return;
        var fragment = new SettingsFragment("SettingsTrans", this);
        CurrentFragment = fragment;

        // Z-Axis Depth Transition
        callerFrag.ExitTransition = new MaterialSharedAxis(MaterialSharedAxis.Z, true);
        callerFrag.ReenterTransition = new MaterialSharedAxis(MaterialSharedAxis.Z, false);

        callerFrag.ParentFragmentManager.BeginTransaction()
            .Replace(Resource.Id.custom_fragment_container, fragment)
            .AddToBackStack("Settings")
            .Commit();
    }

    public void NavigateToAnyPageOfGivenType(Fragment callerFrag, Fragment destinationFrag, string tag)
    {
        if (callerFrag == null || !callerFrag.IsAdded) return;
        CurrentFragment = destinationFrag;

        var fade = new MaterialFadeThrough();
        destinationFrag.EnterTransition = fade;
        callerFrag.ExitTransition = fade;

        callerFrag.ParentFragmentManager.BeginTransaction()
            .Replace(Resource.Id.custom_fragment_container, destinationFrag)
            .AddToBackStack(tag)
            .Commit();
    }


    public void NavigateToLibraryStats(Fragment callerFrag) =>
        NavigateToAnyPageOfGivenType(callerFrag, new LibraryStatsFragment(this), "LibraryStats");


    public void NavigateToSingleSongPageFromHome(Fragment? callerFrag, string transitionName, View sharedView)
    {
        if (callerFrag == null || !callerFrag.IsAdded || callerFrag.Activity == null) return;

        // 1. Setup the Destination
        var detailFrag = new SongDetailFragment(transitionName, this);

        // 2. Configure the "Morph" (The Shared Element Transition)
        var transform = new MaterialContainerTransform
        {
            DrawingViewId = Resource.Id.custom_fragment_container,
            ScrimColor = Color.Transparent,
            ContainerColor = Color.ParseColor("#121212"), // Set this to your actual page background color!
            FadeMode = MaterialContainerTransform.FadeModeCross,
            

            // This adds the ARC motion you wanted from the second block
            PathMotion = new MaterialArcMotion()
        };
        transform.SetDuration(400);
        // 3. Assign Transitions
        detailFrag.SharedElementEnterTransition = transform;
        detailFrag.SharedElementReturnTransition = transform; // It handles the reverse automatically

        // 4. Configure the "Non-Shared" views (The background list)
        // HOLD prevents the list from disappearing while the card expands
        callerFrag.ExitTransition = new Hold();
        callerFrag.ReenterTransition = new Hold();

        // 5. Execute
        callerFrag.ParentFragmentManager.BeginTransaction()
            .SetReorderingAllowed(true)
            .AddSharedElement(sharedView, transitionName)
            .Replace(Resource.Id.custom_fragment_container, detailFrag)
            .AddToBackStack(transitionName)
            .Commit();
    }


    #endregion

    #region Binding Views Section

    private readonly BehaviorSubject<SongModelView?> _currentSong = new(null);


    public IObservable<SongModelView?> CurrentSongChanged => _currentSong.AsObservable();

    public void SetCurrentSong(SongModelView? song)
    {
        _currentSong.OnNext(song);
    }

    protected override async Task ProcessSongChangeAsync(SongModelView value)
    {
        await base.ProcessSongChangeAsync(value);
        SetCurrentSong(value);
    }
    
  

    #endregion

    public System.Reactive.Subjects.Subject<System.Reactive.Unit> ScrollToCurrentSongRequest { get; }
    = new System.Reactive.Subjects.Subject<System.Reactive.Unit>();

    // 2. Helper method to trigger it
    public void TriggerScrollToCurrentSong()
    {
        ScrollToCurrentSongRequest.OnNext(System.Reactive.Unit.Default);
    }

    public MaterialTextView? CurrentDeviceLogTextView;
    public override void SetlatestDevicelog()
    {
        if(CurrentDeviceLogTextView is null)
            { return; }
        CurrentDeviceLogTextView.Text = LatestScanningLog;
    }

    internal void ToggleAppThemeAnd()
    {
        var isDarkMode = UiBuilder.IsDark(CurrentFragment.Context);
        if (isDarkMode)
        {
            //set white theme aka light mode reload/refresh app as if light mode was toggled in system
        }
        else
        {
        }
        CurrentTheme = isDarkMode ? UIUtils.CurrentAppTheme.Dark : UIUtils.CurrentAppTheme.Light;
        ToggleAppTheme();
        
        // Recreate activity to properly apply theme changes across all UI elements.
        // This is the recommended Android approach for theme changes as it ensures
        // all views, drawables, and resources are reloaded with the new theme.
        // Alternative approaches would require manually updating each view which is error-prone.
        if (CurrentFragment?.Activity is TransitionActivity activity)
        {
            activity.RunOnUiThread(() =>
            {
                activity.Recreate();
            });
        }
    }
}