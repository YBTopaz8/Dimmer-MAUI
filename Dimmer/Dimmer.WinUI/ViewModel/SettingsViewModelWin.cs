namespace Dimmer.WinUI.ViewModel;

public partial class SettingsViewModelWin : SettingsViewModel
{
    public SettingsViewModelWin(BaseViewModelWin baseVMWindows,IDimmerStateService dimmerStateService, MusicDataService musicDataService, IAppInitializerService appInitializerService, IDimmerAudioService audioServ, ISettingsService settingsService, ILyricsMetadataService lyricsMetadataService, SubscriptionManager subsManager, LyricsMgtFlow lyricsMgtFlow, ICoverArtService coverArtService, IFolderMgtService folderMgtService, IRepository<SongModel> songRepo, IDuplicateFinderService duplicateFinderService, IRepository<ArtistModel> artistRepo, IRepository<AlbumModel> albumModel, IRepository<GenreModel> genreModel, IDialogueService dialogueService, IRepository<PlaylistModel> playlistRepo, IRealmFactory realmFact, IFolderMonitorService folderServ, ILibraryScannerService libScannerService, IRepository<DimmerPlayEvent> dimmerPlayEventRepo, BaseAppFlow baseAppClass, ILastfmService lastfmService, ILogger<BaseViewModel> logger) : base(dimmerStateService, musicDataService, appInitializerService, audioServ, settingsService, lyricsMetadataService, subsManager, lyricsMgtFlow, coverArtService, folderMgtService, songRepo, duplicateFinderService, artistRepo, albumModel, genreModel, dialogueService, playlistRepo, realmFact, folderServ, libScannerService, dimmerPlayEventRepo, baseAppClass, lastfmService, logger)
    {
        BaseViewModelWin = baseVMWindows;

    }

    public BaseViewModelWin BaseViewModelWin { get; set; }
  

}
