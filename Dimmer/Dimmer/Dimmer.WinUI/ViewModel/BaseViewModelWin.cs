// --- START OF FILE BaseViewModelWin.cs ---
using CommunityToolkit.Maui.Storage;

using Dimmer.Data.Models;
using Dimmer.Data.RealmStaticFilters;
using Dimmer.Interfaces.Services.Interfaces;


// Assuming SkiaSharp and ZXing.SkiaSharp are correctly referenced for barcode scanning

// Assuming Vanara.PInvoke.Shell32 and TaskbarList are for Windows-specific taskbar progress
using Dimmer.WinUI.Utils.WinMgt;

using Microsoft.Extensions.Logging;

namespace Dimmer.WinUI.ViewModel; // Assuming this is your WinUI MyViewModel namespace

public partial class BaseViewModelWin : BaseViewModel // BaseViewModel is in Dimmer.MyViewModel
{
    private readonly IMapper mapper;
    private readonly IAppInitializerService appInitializerService;
    private readonly IDimmerLiveStateService dimmerLiveStateService;
    private readonly AlbumsMgtFlow albumsMgtFlow;
    private readonly IFolderPicker folderPicker;
    private readonly IWindowManagerService windowManager;
    private readonly IDimmerAudioService audioService;
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
    private readonly ILogger<BaseViewModelWin> logger;
    private readonly IWindowManagerService winMgrService;

    public BaseViewModelWin(IMapper mapper, IAppInitializerService appInitializerService,
        IWindowManagerService windMgr,
        IDimmerLiveStateService dimmerLiveStateService, IDimmerAudioService _audioService, AlbumsMgtFlow albumsMgtFlow, IDimmerStateService stateService, ISettingsService settingsService, SubscriptionManager subsManager, LyricsMgtFlow lyricsMgtFlow, IFolderMgtService folderMgtService, IRepository<SongModel> songRepo, IRepository<ArtistModel> artistRepo, IRepository<AlbumModel> albumModel, IRepository<GenreModel> genreModel, ILogger<BaseViewModel> logger) : base(mapper, appInitializerService, dimmerLiveStateService, _audioService, albumsMgtFlow, stateService, settingsService, subsManager, lyricsMgtFlow, folderMgtService, songRepo, artistRepo, albumModel, genreModel, logger)
    {

        this.mapper=mapper;
        this.appInitializerService=appInitializerService;
        this.dimmerLiveStateService=dimmerLiveStateService;
        this.albumsMgtFlow=albumsMgtFlow;
        audioService=_audioService;
        this.songsMgtFlow=songsMgtFlow;
        this.stateService=stateService;
        this.settingsService=settingsService;
        this.subsManager=subsManager;
        this.lyricsMgtFlow=lyricsMgtFlow;
        this.folderMgtService=folderMgtService;
        this.winMgrService=windMgr;
    }

    [ObservableProperty]
    public partial int MediaBarGridRowPosition { get; set; }

    [RelayCommand]
    public void SwapMediaBarPosition()
    {
        if (MediaBarGridRowPosition==0)
        {
            MediaBarGridRowPosition = 1;
        }
        else
        {
            MediaBarGridRowPosition=0;
        }
    }


    public async Task AddMusicFolderViaPickerAsync()
    {

        logger.LogInformation("SelectSongFromFolderWindows: Requesting storage permission.");

        var res = await folderPicker.PickAsync(CancellationToken.None);

        if (res is not null && res.Folder is not null)
        {


            string? selectedFolderPath = res!.Folder!.Path;



            if (!string.IsNullOrEmpty(selectedFolderPath))
            {
                logger.LogInformation("Folder selected: {FolderPath}. Adding to preferences and triggering scan.", selectedFolderPath);

                AddMusicFolderByPassingToService(selectedFolderPath);
            }
            else
            {
                logger.LogInformation("No folder selected by user.");
            }
        }
        else
        {
            logger.LogInformation("Folder selection was cancelled or failed.");
        }
    }

    public async Task PickFolderToScan()
    {
        await AddMusicFolderViaPickerAsync();
    }





    [ObservableProperty]
    public partial bool IsSearching { get; set; }
    [ObservableProperty]
    public partial SongModelView SelectedSongOnPage { get; set; }
}