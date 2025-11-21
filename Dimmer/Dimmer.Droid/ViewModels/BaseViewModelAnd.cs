
//using System.Reactive.Linq;

using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

using Android.Graphics;

using CommunityToolkit.Maui.Core.Extensions;
using CommunityToolkit.Maui.Storage;

using Dimmer.Interfaces.IDatabase;
using Dimmer.Interfaces.Services.Interfaces.FileProcessing;
using Dimmer.Interfaces.Services.Interfaces.FileProcessing.FileProcessorUtils;
using Dimmer.LastFM;
using Dimmer.Utilities.Events;
using Dimmer.Utilities.Extensions;
using Dimmer.Utilities.StatsUtils;
using Dimmer.ViewsAndPages.NativeViews;

using Microsoft.Extensions.Logging;

using static System.TimeZoneInfo;

using RegexOption = System.Text.RegularExpressions.RegexOptions;

namespace Dimmer.ViewModels;
public partial class BaseViewModelAnd : BaseViewModel, IDisposable
{
    public LoginViewModel LoginViewModel => _loginViewModel;
    private readonly LoginViewModel _loginViewModel;
    private readonly IAppInitializerService appInitializerService;
    private readonly IDimmerAudioService audioService;

    // _subs is inherited from BaseViewModel as _subsManager and should be used for subscriptions here too
    // private readonly SubscriptionManager _subsLocal = new(); // Use _subsManager from base
    private readonly IMapper mapper;
    private readonly IFolderPicker folderPicker;
    private readonly IAnimationService animService;
    private readonly IDimmerStateService stateService;
    private readonly ISettingsService settingsService;
    private readonly SubscriptionManager subsManager;
    private readonly IRepository<SongModel> songRepository;
    private readonly IRepository<ArtistModel> artistRepository;
    private readonly IRepository<AlbumModel> albumRepository;
    private readonly IRepository<GenreModel> genreRepository;
    private readonly LyricsMgtFlow lyricsMgtFlow;
    private readonly IFolderMgtService folderMgtService;
    private readonly BaseViewModel baseVM;
    public BaseViewModel BaseVM => baseVM; // Expose BaseViewModel reference if needed

    public Fragment? PreviousPage { get; set; }
    public Fragment? CurrentPage { get; set; }

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

            _logger.LogInformation("SelectSongFromFolderAndroid: Requesting storage permission.");
            var status = await Permissions.RequestAsync<CheckPermissions>();

            if (status == PermissionStatus.Granted)
            {
                
                var res = await FolderPicker.Default.PickAsync(CancellationToken.None);

                if (res is not null)
                {


                    string? selectedFolderPath = res?.Folder?.Path;



                    if (!string.IsNullOrEmpty(selectedFolderPath))
                    {
                        _logger.LogInformation("Folder selected: {FolderPath}. Adding to preferences and triggering scan.", selectedFolderPath);
                        // The FolderManagementService should handle adding to settings and triggering the scan.
                        // We just need to tell it the folder was selected by the user.

                        await AddMusicFolderByPassingToService(selectedFolderPath);
                    }
                    else
                    {
                        _logger.LogInformation("No folder selected by user.");
                    }


                }

            }
            else
            {
                _logger.LogWarning("Storage permission denied for adding music folder.");
                // TODO: Show message to user explaining why permission is needed.
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }

    }

    internal void ViewArtistDetails(ArtistModelView? s)
    {
        ViewArtistDetails(s);
    }

    private bool _isExpanded;

    public BaseViewModelAnd(IDimmerAudioService AudioService, ILogger<BaseViewModelAnd> logger, 
        IMapper mapper, IDimmerStateService dimmerStateService, MusicDataService musicDataService,
        IAppInitializerService appInitializerService, IDimmerAudioService audioServ, ISettingsService settingsService, 
        ILyricsMetadataService lyricsMetadataService, SubscriptionManager subsManager, LyricsMgtFlow lyricsMgtFlow, 
        ICoverArtService coverArtService, IFolderMgtService folderMgtService, IRepository<SongModel> SongRepo, 
        IDuplicateFinderService duplicateFinderService, ILastfmService LastfmService, IRepository<ArtistModel> artistRepo, 
        IRepository<AlbumModel> albumModel, IRepository<GenreModel> genreModel, IDialogueService dialogueService) : base(mapper, dimmerStateService, musicDataService, appInitializerService, audioServ, settingsService, lyricsMetadataService, subsManager, lyricsMgtFlow, coverArtService, folderMgtService, SongRepo, duplicateFinderService, LastfmService, artistRepo, albumModel, genreModel, dialogueService, logger)
    {
       
        
        // mapper and stateService are accessible via base class protected fields.
        // _subs (passed as subsManager) is managed by BaseViewModel as _subsManager.

        this._logger = new LoggerFactory().CreateLogger<BaseViewModelAnd>();
        isAppBooting=true;
        this._logger.LogInformation("BaseViewModelAnd initialized.");
        audioService=AudioService;
    }

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
        _loginViewModel.Username=UserLocal.Username;
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
                if(File.Exists(song.FilePath))
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

        if (byteData.imgBytes != null)
        {
            string clipboardText = $"{song.Title} - {song.ArtistName}\nAlbum: {song.AlbumName}\n\nShared via Dimmer Music Player v{CurrentAppVersion}";

             await Clipboard.Default.SetTextAsync(clipboardText);

        }
    }


    #region Navigation Section


    public void NavigateToSingleSongPageFromHome(Fragment? callerFrag, string transitionName, View sharedView)
    {
        if (callerFrag == null) return;
        if (!callerFrag.IsAdded || callerFrag.Activity == null) return;

    
        sharedView.TransitionName = transitionName;

        var typeOfFragment = callerFrag.GetType();
        if (typeOfFragment == typeof(HomePageFragment))
        {
            HomePageFragment homeFrag = (HomePageFragment)callerFrag;
            var fragment = new SongDetailPage(transitionName, this);
            CurrentPage = fragment;

            
            var enterSet = new TransitionSet();


            var container = new MaterialContainerTransform
            {
                DrawingViewId = TransitionActivity.MyStaticID,  // container for fragments
                ScrimColor = Color.Transparent,
                ContainerColor = Color.Transparent,
                FadeMode = MaterialContainerTransform.FadeModeThrough,
                StartShapeAppearanceModel = ShapeAppearanceModel.InvokeBuilder().SetAllCorners(CornerFamily.Rounded, 50f).Build(),
                EndShapeAppearanceModel = ShapeAppearanceModel.InvokeBuilder().SetAllCorners(CornerFamily.Rounded, 0f).Build(),
            };
            
            container.PathMotion = new MaterialArcMotion();
            container.SetDuration(380);

            homeFrag._pageFAB?.Animate()?
            .Alpha(0f)
            .SetDuration(container.Duration)
            .Start();


            fragment.SharedElementEnterTransition = container;
            fragment.SharedElementReturnTransition = container.Clone();

            var nonSharedEnterAnim = new Google.Android.Material.Transition.MaterialFadeThrough
            {

            };

            var scaleUp = new MaterialElevationScale(true);
            scaleUp.SetDuration(300);

            var scaleDown = new MaterialElevationScale(false);
            scaleDown.SetDuration(200);
            nonSharedEnterAnim.SetDuration(360);
            fragment.EnterTransition = scaleUp;

            var nonShareExitAnim = new Google.Android.Material.Transition.MaterialFadeThrough
            {
            };
            nonShareExitAnim.SetDuration(180);
            fragment.ExitTransition = scaleDown;




            Hold enterHold = new Hold();
            enterHold.AddTarget(TransitionActivity.MyStaticID);
            enterHold.SetDuration(100);
            homeFrag.ParentFragment?.ExitTransition = enterHold;


            homeFrag.ParentFragmentManager.BeginTransaction()
                .AddSharedElement(sharedView, transitionName)
                .Replace(TransitionActivity.MyStaticID, fragment)
                .AddToBackStack(null)
                .Commit();


            // Set up the transition (this is pseudo-code; actual implementation may vary)
            // You would typically use a navigation service that supports shared element transitions.
        }

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
    
    public void SetupSubscriptions()
    {
        // Example subscription to CurrentSongChanged
        var songSubscription = CurrentSongChanged
            .Where(song => song != null)
         .ObserveOn(RxSchedulers.UI)    

            .Subscribe(song =>
            {
                if (song == null || song.TitleDurationKey is null) return;
                var isCurrentHomePage = CurrentPage is HomePageFragment;
                if (!isCurrentHomePage) return;
                var currenthomeFrag = (HomePageFragment)CurrentPage!;

                currenthomeFrag._albumArt?.SetImageBitmap(BitmapFactory.DecodeFile(song.CoverImagePath));
                currenthomeFrag._titleTxt?.Text = song.Title;
                currenthomeFrag._albumTxt?.Text = song.AlbumName;
            });

        SubsManager.Add(songSubscription);


        var songPos= AudioEnginePositionObservable.
            ObserveOn(RxSchedulers.UI)
            .Subscribe(songPosition =>
            {
                
            
                var isCurrentHomePage = CurrentPage is HomePageFragment;
                if (!isCurrentHomePage) return;
                
                var currenthomeFrag = (HomePageFragment)CurrentPage!;
                
                
                
                currenthomeFrag.CurrentTimeTextView?.Text = PublicStats.FormatTimeSpan(songPosition);
            });

    }
    #endregion


}