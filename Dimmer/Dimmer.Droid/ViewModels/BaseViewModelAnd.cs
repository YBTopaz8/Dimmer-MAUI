using Dimmer.Data;
using Dimmer.Interfaces;
using Dimmer.Interfaces.IDatabase;
using Dimmer.Interfaces.Services.Interfaces.FileProcessing;
using Dimmer.Interfaces.Services.Interfaces.FileProcessing.FileProcessorUtils;
using Dimmer.LastFM;
using Microsoft.Extensions.Logging;

namespace Dimmer.ViewModels;

internal partial class BaseViewModelAnd : BaseViewModel, IDisposable
{
    public BaseViewModelAnd(
        IDimmerStateService dimmerStateService,
        MusicDataService musicDataService,
        IAppInitializerService appInitializerService,
        IDimmerAudioService audioServ,
        ISettingsService settingsService,
        ILyricsMetadataService lyricsMetadataService,
        SubscriptionManager subsManager,
        LyricsMgtFlow lyricsMgtFlow,
        ICoverArtService coverArtService,
        IFolderMgtService folderMgtService,
        IRepository<SongModel> songRepo,
        IDuplicateFinderService duplicateFinderService,
        ILastfmService lastfmService,
        IRepository<ArtistModel> artistRepo,
        IRepository<AlbumModel> albumModel,
        IRepository<GenreModel> genreModel,
        IDialogueService dialogueService,
        IRepository<PlaylistModel> playlistRepo,
        IRealmFactory realmFact,
        IFolderMonitorService folderServ,
        ILibraryScannerService libScannerService,
        IRepository<DimmerPlayEvent> dimmerPlayEventRepo,
        BaseAppFlow baseAppClass,
        ILogger<BaseViewModel> logger) : base(
        dimmerStateService,
        musicDataService,
        appInitializerService,
        audioServ,
        settingsService,
        lyricsMetadataService,
        subsManager,
        lyricsMgtFlow,
        coverArtService,
        folderMgtService,
        songRepo,
        duplicateFinderService,
        lastfmService,
        artistRepo,
        albumModel,
        genreModel,
        dialogueService,
        playlistRepo,
        realmFact,
        folderServ,
        libScannerService,
        dimmerPlayEventRepo,
        baseAppClass,
        logger)
    {
    }
}