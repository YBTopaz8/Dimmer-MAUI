using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.WinUI.ViewModel;

public partial class SettingsViewModelWin : SettingsViewModel
{
    public BaseViewModelWin BaseViewModelWin { get; set; }
    public SettingsViewModelWin(IDimmerStateService dimmerStateService, MusicDataService musicDataService, IAppInitializerService appInitializerService, IDimmerAudioService audioServ, ISettingsService settingsService, ILyricsMetadataService lyricsMetadataService, SubscriptionManager subsManager, LyricsMgtFlow lyricsMgtFlow, ICoverArtService coverArtService, IFolderMgtService folderMgtService, IRepository<SongModel> _songRepo, IDuplicateFinderService duplicateFinderService, ILastfmService _lastfmService, IRepository<ArtistModel> artistRepo, IRepository<AlbumModel> albumModel, IRepository<GenreModel> genreModel, IDialogueService dialogueService, IRepository<PlaylistModel> PlaylistRepo, IRealmFactory RealmFact, IFolderMonitorService FolderServ, ILibraryScannerService LibScannerService, IRepository<DimmerPlayEvent> DimmerPlayEventRepo, BaseAppFlow BaseAppClass, ILogger<BaseViewModel> logger) : base(dimmerStateService, musicDataService, appInitializerService, audioServ, settingsService, lyricsMetadataService, subsManager, lyricsMgtFlow, coverArtService, folderMgtService, _songRepo, duplicateFinderService, _lastfmService, artistRepo, albumModel, genreModel, dialogueService, PlaylistRepo, RealmFact, FolderServ, LibScannerService, DimmerPlayEventRepo, BaseAppClass, logger)
    {
        BaseViewModelWin = IPlatformApplication.Current!.Services.GetService<BaseViewModelWin>()!;

    }

}
