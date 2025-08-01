// --- START OF FILE BaseViewModelWin.cs ---
using CommunityToolkit.Maui.Storage;

using Dimmer.Data.Models;
using Dimmer.Data.RealmStaticFilters;
using Dimmer.Interfaces.Services.Interfaces;
using Dimmer.Interfaces.Services.Interfaces.FileProcessing;
using Dimmer.LastFM;
using Dimmer.Utilities.FileProcessorUtils;


// Assuming SkiaSharp and ZXing.SkiaSharp are correctly referenced for barcode scanning

// Assuming Vanara.PInvoke.Shell32 and TaskbarList are for Windows-specific taskbar progress
using Dimmer.WinUI.Utils.WinMgt;

using Hqub.Lastfm.Entities;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using System.Threading.Tasks;

namespace Dimmer.WinUI.ViewModel; // Assuming this is your WinUI MyViewModel namespace

public partial class BaseViewModelWin : BaseViewModel // BaseViewModel is in Dimmer.MyViewModel
{
    public LoginViewModel LoginViewModel => loginViewModel;
    private readonly LoginViewModel loginViewModel;

    public DimmerLiveViewModel DimmerLiveViewModel => dimmerLiveViewModel;



    private readonly IMapper _mapper;
    private readonly IAppInitializerService appInitializerService;
    private readonly IDimmerLiveStateService dimmerLiveStateService;
    private readonly IDimmerAudioService audioServ;
    private readonly IFolderPicker folderPicker;
    private readonly IDuplicateFinderService duplicateFinderService;
    private readonly ILastfmService lastfmService;
    private readonly IWindowManagerService windowManager1;
    private readonly DimmerLiveViewModel dimmerLiveViewModel;
    private readonly IWindowManagerService windowManager;
    private readonly IDimmerStateService stateService;
    private readonly ISettingsService settingsService;
    private readonly SubscriptionManager subsManager;
    private readonly IRepository<SongModel> songRepository;
    private readonly IRepository<ArtistModel> artistRepository;
    private readonly IRepository<AlbumModel> albumRepository;
    private readonly IRepository<GenreModel> genreRepository;
    private readonly LyricsMgtFlow lyricsMgtFlow;
    private readonly ICoverArtService coverArtService;
    private readonly IFolderMgtService folderMgtService;
    private readonly IRepository<SongModel> songRepo;
    private readonly IDeviceConnectivityService deviceConnectivityService;
    private readonly IRepository<ArtistModel> artistRepo;
    private readonly IRepository<AlbumModel> albumModel;
    private readonly IRepository<GenreModel> genreModel;
    private readonly ILogger<BaseViewModel> logger;
    private readonly IWindowManagerService winMgrService;

    public BaseViewModelWin(IMapper mapper, IAppInitializerService appInitializerService,
        LoginViewModel loginViewModel,
        DimmerLiveViewModel dimmerLiveViewModel
        , IFolderPicker _folderPicker, IWindowManagerService windowManager,
        IDimmerLiveStateService dimmerLiveStateService, IDimmerAudioService audioServ,
        IDimmerStateService stateService, ISettingsService settingsService, SubscriptionManager subsManager, 
        LyricsMgtFlow lyricsMgtFlow, ICoverArtService coverArtService, IFolderMgtService folderMgtService, 
        IRepository<SongModel> songRepo, IDeviceConnectivityService deviceConnectivityService, 
        IDuplicateFinderService duplicateFinderService, ILastfmService lastfmService,
        IRepository<ArtistModel> artistRepo, IRepository<AlbumModel> albumModel,
        IRepository<GenreModel> genreModel, ILogger<BaseViewModel> logger) :
        base(mapper, appInitializerService, dimmerLiveStateService, audioServ,
            stateService, settingsService, subsManager, lyricsMgtFlow, coverArtService, 
            folderMgtService, songRepo, deviceConnectivityService, duplicateFinderService, 
            lastfmService, artistRepo, albumModel, genreModel, logger)

    {



        _mapper=mapper;
        this.appInitializerService=appInitializerService;
        this.loginViewModel=loginViewModel;
        folderPicker=_folderPicker;
        this.dimmerLiveStateService=dimmerLiveStateService;
        this.audioServ=audioServ;
        this.stateService=stateService;
        this.settingsService=settingsService;
        this.subsManager=subsManager;
        this.lyricsMgtFlow=lyricsMgtFlow;
        this.coverArtService=coverArtService;
        this.folderMgtService=folderMgtService;
        this.songRepo=songRepo;
        this.deviceConnectivityService=deviceConnectivityService;
        this.artistRepo=artistRepo;
        this.albumModel=albumModel;
        this.genreModel=genreModel;
        this.logger=logger;
        this.duplicateFinderService=duplicateFinderService;
        this.lastfmService=lastfmService;
        windowManager1=windowManager;
        this.dimmerLiveViewModel=dimmerLiveViewModel;
    }

    //public BaseViewModelWin(IMapper mapper, IAppInitializerService appInitializerService, 
    //    IFolderPicker _folderPicker, IWindowManagerService windowManager,
    //    ILyricsMetadataService lyricsMetadataService,
    //    IDimmerLiveStateService dimmerLiveStateService, IDimmerAudioService audioServ, IDimmerStateService stateService, ISettingsService settingsService, SubscriptionManager subsManager, LyricsMgtFlow lyricsMgtFlow, ICoverArtService coverArtService, IFolderMgtService folderMgtService, IRepository<SongModel> songRepo, ILastfmService lastfmService, IRepository<ArtistModel> artistRepo, IRepository<AlbumModel> albumModel, IRepository<GenreModel> genreModel, ILogger<BaseViewModel> logger) : base(mapper, appInitializerService,  dimmerLiveStateService, audioServ, stateService,  settingsService, subsManager, lyricsMgtFlow, coverArtService, folderMgtService, songRepo, lastfmService, artistRepo, albumModel, genreModel, logger)
    //{





    [ObservableProperty]
    public partial int MediaBarGridRowPosition { get; set; }
    public CollectionView SongColView { get; internal set; }

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

    public void RescanFolderPath(string folderPath)
    {
        if (string.IsNullOrEmpty(folderPath))
        {
            return;
        }
        AddMusicFolderByPassingToService(folderPath);
    }
    public async Task AddMusicFolderViaPickerAsync()
    {


        var res = await folderPicker.PickAsync(CancellationToken.None);

        if (res is not null && res.Folder is not null)
        {


            string? selectedFolderPath = res!.Folder!.Path;



            if (!string.IsNullOrEmpty(selectedFolderPath))
            {


                AddMusicFolderByPassingToService(selectedFolderPath);
            }
            else
            {

            }
        }
        else
        {

        }
    }
    public async Task PickFolderToScan()
    {
        await AddMusicFolderViaPickerAsync();
    }

    public async Task<bool> InitializeLoginUserData()
    {
        loginViewModel.Username=base.UserLocal.Username;
        return await loginViewModel.InitializeAsync();
    }

    public async Task<bool> IsUserOkayForTransfer()
    {
        if (loginViewModel.CurrentUser is not null)
        {
            return true;

        }
        else
        {

            if (await InitializeLoginUserData())
            {

                return true;
            }
            return false;
        }
    }

    public async Task ProcessAndMoveToViewSong(SongModelView? selectedSec)
    {
        if (selectedSec is null)
        {
            if (SelectedSong is null)
            {
                SelectedSong=CurrentPlayingSongView;
            }
            else
            {
                SelectedSong = SongColView.SelectedItem as SongModelView;

            }


        }
        await Shell.Current.GoToAsync(nameof(SingleSongPage), true);
    }
}