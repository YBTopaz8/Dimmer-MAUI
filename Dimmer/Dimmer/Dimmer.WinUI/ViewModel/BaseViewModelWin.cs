// --- START OF FILE BaseViewModelWin.cs ---
using Dimmer.Data.Models;
using Dimmer.Interfaces.Services.Interfaces;


// Assuming SkiaSharp and ZXing.SkiaSharp are correctly referenced for barcode scanning

// Assuming Vanara.PInvoke.Shell32 and TaskbarList are for Windows-specific taskbar progress
using Dimmer.WinUI.Utils.WinMgt;
using Dimmer.WinUI.Views.ArtistsSpace;

using Microsoft.Extensions.Logging; // For ILogger

namespace Dimmer.WinUI.ViewModel; // Assuming this is your WinUI MyViewModel namespace

public partial class BaseViewModelWin : BaseViewModel // BaseViewModel is in Dimmer.MyViewModel
{
    private readonly IMapper mapper;
    private readonly IAppInitializerService appInitializerService;
    private readonly IDimmerLiveStateService dimmerLiveStateService;
    private readonly AlbumsMgtFlow albumsMgtFlow;
    private readonly IWindowManagerService windowManager;
    private readonly IDimmerAudioService audioService;
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
    private readonly ILogger<BaseViewModelWin> logger;
    private readonly ISettingsWindowManager settingsWindwow;

    public BaseViewModelWin(IMapper mapper, IAppInitializerService appInitializerService, IDimmerLiveStateService dimmerLiveStateService, AlbumsMgtFlow albumsMgtFlow,
        IWindowManagerService windowManager,
       IDimmerAudioService _audioService, PlayListMgtFlow playlistsMgtFlow, SongsMgtFlow songsMgtFlow, IDimmerStateService stateService, ISettingsService settingsService, SubscriptionManager subsManager,
IRepository<SongModel> songRepository, IRepository<ArtistModel> artistRepository, IRepository<AlbumModel> albumRepository, IRepository<GenreModel> genreRepository, LyricsMgtFlow lyricsMgtFlow, IFolderMgtService folderMgtService, ILogger<BaseViewModelWin> logger, ISettingsWindowManager settingsWindwow) : base(mapper, appInitializerService, dimmerLiveStateService, _audioService, albumsMgtFlow, playlistsMgtFlow, songsMgtFlow, stateService, settingsService, subsManager, lyricsMgtFlow, folderMgtService, songRepository, artistRepository, albumRepository, genreRepository, logger)
    {
        this.mapper=mapper;
        this.appInitializerService=appInitializerService;
        this.dimmerLiveStateService=dimmerLiveStateService;
        this.albumsMgtFlow=albumsMgtFlow;
        this.windowManager=windowManager;
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
        this.settingsWindwow=settingsWindwow;
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

    public void OpenSettingsWindow()
    {
        if (!settingsWindwow.IsSettingsWindowOpen)
        {

            settingsWindwow.ShowSettingsWindow(this);
        }
        else
        {
            settingsWindwow.BringSettingsWindowToFront();
        }
    }


    public void OpenArtistsWindow()
    {

        windowManager.GetOrCreateUniqueWindow<ArtistGeneralWindow>();

    }



    [ObservableProperty]
    public partial bool IsSearching { get; set; }
}