namespace Dimmer.Orchestration;
public class StatisticsMgtFlow
{
    public StatisticsMgtFlow(IDimmerStateService state, IRepository<SongModel> songRepo,
        IRepository<UserModel> userRepo,
        IRepository<GenreModel> genreRepo,
        IRepository<DimmerPlayEvent> pdlRepo,
        IRepository<PlaylistModel> playlistRepo, IRepository<ArtistModel> artistRepo,
        IRepository<AlbumModel> albumRepo, ISettingsService settings,
        IRepository<AppStateModel> appstateRepo,
        IFolderMgtService folderMonitor, IMapper mapper, SubscriptionManager subs)
    { }
}
