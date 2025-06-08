
//using System.Reactive.Linq;

using System.ComponentModel;
using System.Security.Cryptography.X509Certificates;

using Android.Views;

using CommunityToolkit.Maui.Storage;

using Dimmer.Data.Models;
using Dimmer.Interfaces.Services;
using Dimmer.Interfaces.Services.Interfaces;
using Dimmer.Utilities.Extensions;
using Dimmer.ViewModel;

using Microsoft.Extensions.Logging;

namespace Dimmer.ViewModels;
public partial class BaseViewModelAnd : ObservableObject, IDisposable
{
    // _subs is inherited from BaseViewModel as _subsManager and should be used for subscriptions here too
    // private readonly SubscriptionManager _subsLocal = new(); // Use _subsManager from base
    private readonly IMapper mapper;
    private readonly IAppInitializerService appInitializerService;
    private readonly IDimmerLiveStateService dimmerLiveStateService;
    private readonly AlbumsMgtFlow albumsMgtFlow;
    private readonly IFolderPicker folderPicker;
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
    private readonly BaseViewModel baseVM;
    public BaseViewModel BaseVM => baseVM; // Expose BaseViewModel reference if needed


    [ObservableProperty]
    private DXCollectionView? _songLyricsCV; // Nullable, ensure it's set from XAML

    // Removed local stateService and mapper as they are protected in BaseViewModel
    private readonly ILogger<BaseViewModelAnd> logger;





    public BaseViewModelAnd(IMapper mapper, IAppInitializerService appInitializerService, IDimmerLiveStateService dimmerLiveStateService, AlbumsMgtFlow albumsMgtFlow, IFolderPicker folderPicker,
       IDimmerAudioService _audioService, PlayListMgtFlow playlistsMgtFlow, SongsMgtFlow songsMgtFlow, IDimmerStateService stateService, ISettingsService settingsService, SubscriptionManager subsManager,
IRepository<SongModel> songRepository, IRepository<ArtistModel> artistRepository, IRepository<AlbumModel> albumRepository, IRepository<GenreModel> genreRepository, LyricsMgtFlow lyricsMgtFlow, IFolderMgtService folderMgtService, ILogger<BaseViewModelAnd> logger, BaseViewModel baseViewModel)
    {
        baseVM = baseViewModel; // Store the BaseViewModel reference if needed
        this.mapper=mapper;
        this.appInitializerService=appInitializerService;
        this.dimmerLiveStateService=dimmerLiveStateService;
        this.albumsMgtFlow=albumsMgtFlow;
        this.folderPicker=folderPicker;
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

    public async Task AddMusicFolderViaPickerAsync(string? selectedFolder = null)
    {

        logger.LogInformation("SelectSongFromFolderAndroid: Requesting storage permission.");
        var status = await Permissions.RequestAsync<CheckPermissions>();

        if (status == PermissionStatus.Granted)
        {
            var res = await folderPicker.PickAsync(CancellationToken.None);

            if (res is not null)
            {


                string? selectedFolderPath = res!.Folder!.Path;



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
}