// --- START OF FILE BaseViewModelWin.cs ---

using CommunityToolkit.Maui.Storage;

using Dimmer.Data.Models;
using Dimmer.Data.RealmStaticFilters;
using Dimmer.DimmerSearch.TQL.TQLCommands;
using Dimmer.Interfaces.IDatabase;
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

namespace Dimmer.WinUI.ViewModel; 

public partial class BaseViewModelWin: BaseViewModel

{
    private readonly IWindowManagerService windowManager;
    private readonly IRepository<SongModel> songRepository;
    private readonly IRepository<ArtistModel> artistRepository;
    private readonly IRepository<AlbumModel> albumRepository;
    private readonly IRepository<GenreModel> genreRepository;
    private readonly IWindowManagerService winMgrService;
    private readonly LoginViewModel loginViewModel;
    private readonly IFolderPicker _folderPicker;
    public BaseViewModelWin(IMapper mapper, MusicDataService musicDataService,LoginViewModel _loginViewModel,
         IDimmerStateService dimmerStateService, IFolderPicker _folderPicker, IAppInitializerService appInitializerService, IDimmerAudioService audioServ, ISettingsService settingsService, ILyricsMetadataService lyricsMetadataService, SubscriptionManager subsManager, LyricsMgtFlow lyricsMgtFlow, ICoverArtService coverArtService, IFolderMgtService folderMgtService, IRepository<SongModel> _songRepo,  IDuplicateFinderService duplicateFinderService, ILastfmService _lastfmService, IRepository<ArtistModel> artistRepo, IRepository<AlbumModel> albumModel, IRepository<GenreModel> genreModel, IDialogueService dialogueService, ILogger<BaseViewModel> logger) : base(mapper, dimmerStateService,musicDataService, appInitializerService, audioServ, settingsService, lyricsMetadataService, subsManager, lyricsMgtFlow, coverArtService, folderMgtService, _songRepo, duplicateFinderService, _lastfmService, artistRepo, albumModel, genreModel, dialogueService, logger)
    {
        this.loginViewModel=_loginViewModel;
        this._folderPicker = _folderPicker;
    }

  

    [ObservableProperty]
    public partial int MediaBarGridRowPosition { get; set; }
    public CollectionView SongColView { get; internal set; }


    [ObservableProperty]
    public partial List<string> DraggedAudioFiles { get; internal set; }

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


        var res = await _folderPicker.PickAsync(CancellationToken.None);

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
        else
        {
            SelectedSong=selectedSec;
        }
            await Shell.Current.GoToAsync(nameof(SingleSongPage), true);
    }

    public async Task InitializeParseUser()
    {
       await loginViewModel.InitializeAsync();
    }

    
}