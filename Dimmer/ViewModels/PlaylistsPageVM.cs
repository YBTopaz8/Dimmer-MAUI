

namespace Dimmer_MAUI.ViewModels;

public partial class PlaylistsPageVM : ObservableObject
{
    [ObservableProperty]
    ObservableCollection<PlaylistModelView> displayedPlaylists;
    public PlaylistsPageVM(HomePageVM homePageVM, IPlayListService playListService, IPlaylistManagementService playlistManagementService)
    {
        HomePageVM = homePageVM;
        PlayListService = playListService;
        PlaylistManagementService = playlistManagementService;
        DisplayedPlaylists = playlistManagementService.AllPlaylists.ToObservableCollection();
    }

    public HomePageVM HomePageVM { get; }
    public IPlayListService PlayListService { get; }
    public IPlaylistManagementService PlaylistManagementService { get; }

    [RelayCommand]
    public void OpenSpecificPlaylistPage(ObjectId PlaylistID)
    {
        if (PlaylistID != null)
        {
            Debug.WriteLine(PlaylistID.ToString());
            Debug.WriteLine(PlaylistID.GetType());
            PlayListService.GetSongsFromPlaylistID(PlaylistID);
        }
    }
}
