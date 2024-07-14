namespace Dimmer_MAUI.ViewModels;

public partial class PlaylistsPageVM : ObservableObject
{
    [ObservableProperty]
    ObservableCollection<PlaylistModelView> displayedPlaylists;
    [ObservableProperty]
    string selectedPlaylistPageTitle;

    [ObservableProperty]
    ObservableCollection<SongsModelView> displayedSongsFromPlaylist;    
    
    public HomePageVM HomePageVM { get; }
    public IPlayListService PlayListService { get; }
    public IPlaylistManagementService PlaylistManagementService { get; }
    public PlaylistsPageVM(HomePageVM homePageVM, IPlayListService playListService, IPlaylistManagementService playlistManagementService)
    {
        HomePageVM = homePageVM;
        PlayListService = playListService;
        PlaylistManagementService = playlistManagementService;
        DisplayedPlaylists = playlistManagementService.AllPlaylists.ToObservableCollection();

        PlayListService.AllPlaylistsChanged += PlayListService_AllPlaylistsChanged;
    }

    private void PlayListService_AllPlaylistsChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        DisplayedPlaylists.Clear();
        DisplayedPlaylists = PlayListService.AllPlaylists.ToObservableCollection();
    }

    public void UpdatePlayLists()
    {
        DisplayedPlaylists.Clear();
        DisplayedPlaylists = PlaylistManagementService!.AllPlaylists.ToObservableCollection();
    }

    [RelayCommand]
    public async Task OpenSpecificPlaylistPage(ObjectId PlaylistID)
    {
        Debug.WriteLine(PlaylistID.ToString());
        Debug.WriteLine(PlaylistID.GetType());
        SelectedPlaylistPageTitle = PlayListService.SelectedPlaylistName;
        PlayListService.GetSongsFromPlaylistID(PlaylistID);
        DisplayedSongsFromPlaylist = PlayListService.SongsFromPlaylist.ToObservableCollection();
        await Shell.Current.GoToAsync(nameof(SinglePlaylistPageM), true);
    
    }
    [RelayCommand]
    public void PlaySong(SongsModelView song)
    {
        HomePageVM.PlaySongCommand.Execute(song);
    }
}
