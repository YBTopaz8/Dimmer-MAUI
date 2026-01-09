using Dimmer.Interfaces;
using Dimmer.Interfaces.Services.Interfaces.FileProcessing.FileProcessorUtils;

namespace Dimmer.ViewModel;

public partial class SettingsViewModel : BaseViewModel
{
  
    public SettingsViewModel( IDimmerStateService dimmerStateService, MusicDataService musicDataService,
        IAppInitializerService appInitializerService, IDimmerAudioService audioServ,
        ISettingsService settingsService, ILyricsMetadataService lyricsMetadataService, 
        SubscriptionManager subsManager, LyricsMgtFlow lyricsMgtFlow, ICoverArtService coverArtService,
        IFolderMgtService folderMgtService, IRepository<SongModel> _songRepo, IDuplicateFinderService duplicateFinderService, 
        ILastfmService _lastfmService, IRepository<ArtistModel> artistRepo, IRepository<AlbumModel> albumModel, 
        IRepository<GenreModel> genreModel, IDialogueService dialogueService, IRepository<PlaylistModel> PlaylistRepo, 
        IRealmFactory RealmFact, IFolderMonitorService FolderServ, ILibraryScannerService LibScannerService, 
        IRepository<DimmerPlayEvent> DimmerPlayEventRepo, BaseAppFlow BaseAppClass, ILogger<BaseViewModel> logger) : base( dimmerStateService, musicDataService, appInitializerService, audioServ, settingsService, lyricsMetadataService, subsManager, lyricsMgtFlow, coverArtService, folderMgtService, _songRepo, duplicateFinderService, _lastfmService, artistRepo, albumModel, genreModel, dialogueService, PlaylistRepo, RealmFact, FolderServ, LibScannerService, DimmerPlayEventRepo, BaseAppClass, logger)
    {
    }

    [ObservableProperty]
    public partial int WizardCurrentViewIndex { get; set; }
    public string ErrorMessage { get; set; }

    public async Task AddMusicFolderViaPickerAsync()
    {
        try
        {
            var res = await FolderPicker.Default.PickAsync(CancellationToken.None);

            if (res is not null && res.Folder is not null)
            {
                string? selectedFolderPath = res!.Folder!.Path;

                if (!string.IsNullOrEmpty(selectedFolderPath))
                {
                    _ = Task.Run(async () => await AddMusicFolderByPassingToService(selectedFolderPath));
                }

            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }

    public void AllowBackNavigationWithMouseFour(bool? isChecked)
    {
        var realm = RealmFactory.GetRealmInstance();
        var appModel = realm.All<AppStateModel>().FirstOrDefaultNullSafe();
        if (appModel != null && isChecked.HasValue)
        {
            realm.Write(() =>
            {
                appModel.AllowBackNavigationWithMouseFour = isChecked.Value;
            });
        }
    }

    public void SetAllowLyricsContribution(string allow)
    {
        var realm = RealmFactory.GetRealmInstance();
        var appModel = realm.All<AppStateModel>().FirstOrDefaultNullSafe();
        if (appModel != null)
        {
            realm.Write(() =>
            {
                appModel.AllowLyricsContribution = allow;
            });
        }
    }

    public void SetPreferredLyricsFormat(string allow)
    {
        var realm = RealmFactory.GetRealmInstance();
        var appModel = realm.All<AppStateModel>().FirstOrDefaultNullSafe();
        if (appModel != null)
        {
            realm.Write(() =>
            {
                appModel.PreferredLyricsFormat = allow;
            });
        }
    }

    public void SetPreferredLyricsSource(string v)
    {
        var realm = RealmFactory.GetRealmInstance();
        var appModel = realm.All<AppStateModel>().FirstOrDefaultNullSafe();
        if (appModel != null)
        {
            realm.Write(() =>
            {
                appModel.PreferredLyricsSource = v;
            });
        }
    }

    [RelayCommand]
    public void SetPreferredMiniLyricsViewPosition(string position)
    {
        var realm = RealmFactory.GetRealmInstance();    
        var appModel = realm.All<AppStateModel>().FirstOrDefaultNullSafe();
        if (appModel != null)
        {
            realm.Write(() =>
            {
                appModel.PreferredMiniLyricsViewPosition = position;
            });
        }
    }

    [RelayCommand]
    public void ToggleIsMiniLyricsViewEnable(bool? isChecked)
    {
        var realm = RealmFactory.GetRealmInstance();
        var appModel = realm.All<AppStateModel>().FirstOrDefaultNullSafe();
        if (appModel != null && isChecked.HasValue)
        {
            realm.Write(() =>
            {
                appModel.IsMiniLyricsViewEnabled = isChecked.Value;
            });
        }
    }

    
}
