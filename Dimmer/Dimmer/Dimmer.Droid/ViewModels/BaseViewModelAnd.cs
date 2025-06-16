
//using System.Reactive.Linq;

using System.ComponentModel;
using System.Runtime.CompilerServices;

using CommunityToolkit.Maui.Core.Extensions;
using CommunityToolkit.Maui.Storage;

using Dimmer.Data.Models;
using Dimmer.ViewModel;

using Microsoft.Extensions.Logging;

namespace Dimmer.ViewModels;
public partial class BaseViewModelAnd : ObservableObject, IDisposable
{
    public readonly IMapper _mapper;
    private readonly IAppInitializerService appInitializerService;
    protected readonly IDimmerStateService _stateService;
    protected readonly ISettingsService _settingsService;
    protected readonly SubscriptionManager _subsManager;
    protected readonly IFolderMgtService _folderMgtService;
    protected readonly ILogger<BaseViewModel> _logger;
    private readonly IDimmerAudioService audioService;

    // _subs is inherited from BaseViewModel as _subsManager and should be used for subscriptions here too
    // private readonly SubscriptionManager _subsLocal = new(); // Use _subsManager from base
    private readonly IMapper mapper;
    private readonly IDimmerLiveStateService dimmerLiveStateService;
    private readonly AlbumsMgtFlow albumsMgtFlow;
    private readonly IFolderPicker folderPicker;
    private readonly IAnimationService animService;
    private readonly PlayListMgtFlow playlistsMgtFlow;
    private readonly SongsMgtFlow songsMgtFlow;
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


    [ObservableProperty]
    private DXCollectionView? _songLyricsCV; // Nullable, ensure it's set from XAML

    // Removed local stateService and mapper as they are protected in BaseViewModel
    private readonly ILogger<BaseViewModelAnd> logger;





    public BaseViewModelAnd(IMapper mapper, IAppInitializerService appInitializerService, IDimmerLiveStateService dimmerLiveStateService, AlbumsMgtFlow albumsMgtFlow, IFolderPicker folderPicker, IAnimationService animService,
       IDimmerAudioService _audioService, PlayListMgtFlow playlistsMgtFlow, SongsMgtFlow songsMgtFlow, IDimmerStateService stateService, ISettingsService settingsService, SubscriptionManager subsManager,
IRepository<SongModel> songRepository, IRepository<ArtistModel> artistRepository, IRepository<AlbumModel> albumRepository, IRepository<GenreModel> genreRepository, LyricsMgtFlow lyricsMgtFlow, IFolderMgtService folderMgtService, ILogger<BaseViewModelAnd> logger, BaseViewModel baseViewModel)
    {
        baseVM = baseViewModel; // Store the BaseViewModel reference if needed
        this.mapper=mapper;
        this.appInitializerService=appInitializerService;
        this.dimmerLiveStateService=dimmerLiveStateService;
        this.albumsMgtFlow=albumsMgtFlow;
        this.folderPicker=folderPicker;
        this.animService=animService;
        audioService=_audioService;
        this.playlistsMgtFlow=playlistsMgtFlow;
        this.songsMgtFlow=songsMgtFlow;
        this.stateService=stateService;
        this.settingsService=settingsService;
        this.subsManager=subsManager;
        this.songRepository=songRepository;
        this.artistRepository=artistRepository;
        this.albumRepository=albumRepository;
        this.genreRepository=genreRepository;
        this.lyricsMgtFlow=lyricsMgtFlow;
        this.folderMgtService=folderMgtService;
        this.logger=logger;

        // mapper and stateService are accessible via base class protected fields.
        // _subs (passed as subsManager) is managed by BaseViewModel as _subsManager.
        this.playlistsMgtFlow=playlistsMgtFlow;


        isAppBooting=true;
        logger.LogInformation("BaseViewModelAnd initialized.");
    }
    bool isAppBooting = false;

    public async Task FiniInit()
    {
        if (isAppBooting)
        {
            await baseVM.Initialize();
            isAppBooting = false;
        }
    }




    [ObservableProperty] public partial Page CurrentUserPage { get; set; }

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
    public async Task AddMusicFolderViaPickerAsync(string? selectedFolder = null)
    {

        logger.LogInformation("SelectSongFromFolderAndroid: Requesting storage permission.");
        var status = await Permissions.RequestAsync<CheckPermissions>();

        if (status == PermissionStatus.Granted)
        {
            var res = await folderPicker.PickAsync(CancellationToken.None);

            if (res is not null)
            {


                string? selectedFolderPath = res?.Folder?.Path;



                if (!string.IsNullOrEmpty(selectedFolderPath))
                {
                    logger.LogInformation("Folder selected: {FolderPath}. Adding to preferences and triggering scan.", selectedFolderPath);
                    // The FolderManagementService should handle adding to settings and triggering the scan.
                    // We just need to tell it the folder was selected by the user.

                    baseVM.AddMusicFolderByPassingToService(selectedFolderPath);
                }
                else
                {
                    logger.LogInformation("No folder selected by user.");
                }


            }

        }
        else
        {
            logger.LogWarning("Storage permission denied for adding music folder.");
            // TODO: Show message to user explaining why permission is needed.
        }

    }

    public void Dispose()
    {
        ((IDisposable)songsMgtFlow).Dispose();
    }

    internal void ViewArtistDetails(ArtistModelView? s)
    {
        baseVM.ViewArtistDetails(s);
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
    [ObservableProperty]
    public partial DXCollectionView SongsColView { get; set; } // Nullable, ensure it's set from XAML
    [RelayCommand]
    void ScrollToSong()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            int itemHandle = SongsColView.FindItemHandle(BaseVM.CurrentPlayingSongView);
            SongsColView.ScrollTo(itemHandle, DXScrollToPosition.Start);
        });
    }

    public void LoadTheCurrentColView(DXCollectionView colView)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (colView is not null)
            {
                SongsColView = colView;
                // Optionally, you can also set the current item to scroll to it.
                int itemHandle = SongsColView.FindItemHandle(BaseVM.CurrentPlayingSongView);
                SongsColView.ScrollTo(itemHandle, DXScrollToPosition.Start);
            }
        });
    }

    #region INotifyPropertyChanged
    public event PropertyChangedEventHandler PropertyChanged;
    protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "")
    {
        if (EqualityComparer<T>.Default.Equals(backingStore, value))
            return false;
        backingStore = value;
        OnPropertyChanged(propertyName);
        return true;
    }
    protected void OnPropertyChanged([CallerMemberName] string propertyName = "") =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    #endregion
}