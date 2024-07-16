namespace Dimmer_MAUI.ViewModels;

public partial class PlaylistsPageVM : ObservableObject
{
    [ObservableProperty]
    ObservableCollection<PlaylistModelView> displayedPlaylists;
    [ObservableProperty]
    string selectedPlaylistPageTitle;

    [ObservableProperty]
    ObservableCollection<SongsModelView> displayedSongsFromPlaylist;

    [ObservableProperty]
    SongsModelView selectedSongToOpenBtmSheet;
    public IPlayBackService PlayBackService { get; }
    public IPlayListService PlayListService { get; }
    public IPlaylistManagementService PlaylistManagementService { get; }
    public PlaylistsPageVM(IPlayBackService playBackService, IPlayListService playListService,
        IPlaylistManagementService playlistManagementService)
    {
        PlayBackService = playBackService;
        PlayListService = playListService;
        PlaylistManagementService = playlistManagementService;
        DisplayedPlaylists = PlayListService.AllPlaylists;
    }

    public void UpdatePlayLists()
    {
        DisplayedPlaylists.Clear();
        DisplayedPlaylists = PlayListService.GetAllPlaylists();
    }

    [RelayCommand]
    public async Task OpenSpecificPlaylistPage(ObjectId PlaylistID)
    {
        PlayListService.GetSongsFromPlaylistID(PlaylistID);
        SelectedPlaylistPageTitle = PlayListService.SelectedPlaylistName;
        DisplayedSongsFromPlaylist = PlayListService.SongsFromPlaylist.ToObservableCollection();
        await Shell.Current.GoToAsync(nameof(SinglePlaylistPageM), true);
    
    }
    [RelayCommand]
    public void PlaySong(SongsModelView song)
    {
        PlayBackService.PlaySongAsync(song);
        //HomePageVM.PlaySongCommand.Execute(song);
    }

    [RelayCommand]
    public void AddSongToSpecifcPlaylist(ObjectId PlaylistID)
    {

    }

    [RelayCommand]
    public void CreatePlaylistAndAddSong(string PlaylistName)
    {
        if (!string.IsNullOrEmpty(PlaylistName))
        {
            PlaylistManagementService.AddSongToPlayListWithPlayListName(SelectedSongToOpenBtmSheet, PlaylistName);
            DisplayedPlaylists = PlayListService.GetAllPlaylists();
        }
        
    }
}
