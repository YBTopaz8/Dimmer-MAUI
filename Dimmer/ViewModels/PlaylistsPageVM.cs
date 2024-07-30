using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;

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
    PlaylistModelView selectedPlaylistToOpenBtmSheet;
    [ObservableProperty]
    SongsModelView selectedSongToOpenBtmSheet;
    public IPlayBackService PlayBackService { get; }
    public IPlayListService PlayListService { get; }
    public IPlaylistManagementService PlaylistManagementService { get; }
    PlaylistMenuBtmSheet btmSheet {  get; set; }
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
        DisplayedSongsFromPlaylist = PlayListService.SongsFromPlaylist;
        PlayBackService.UpdateCurrentQueue();
        await Shell.Current.GoToAsync(nameof(SinglePlaylistPageM), true);
    
    }
    [RelayCommand]
    public void PlaySong(SongsModelView song)
    {
        PlayBackService.PlaySongAsync(song);
        //HomePageVM.PlaySongCommand.Execute(song);
    }
    CancellationTokenSource cts = new();
    const string songAddedToPlaylistText = "Song Added to Playlist";
    const string songDeletedFromPlaylistText = "Song Removed from Playlist";
    const string PlaylistCreatedText = "Playlist Created Successfully!";
    const string PlaylistDeletedText = "Playlist Deleted Successfully!";
    const ToastDuration duration = ToastDuration.Short;
    
    [RelayCommand]
    public async Task AddSongToSpecifcPlaylist(ObjectId PlaylistID)
    {
        PlaylistManagementService.AddSongToPlayListWithPlayListID(SelectedSongToOpenBtmSheet, PlaylistID);
        DisplayedPlaylists = PlayListService.GetAllPlaylists();
        var toast = Toast.Make(songAddedToPlaylistText, duration);
        await toast.Show(cts.Token);
    }

    [RelayCommand]
    public async Task CreatePlaylistAndAddSong(string PlaylistName)
    {
        if (!string.IsNullOrEmpty(PlaylistName))
        {
            PlaylistManagementService.AddSongToPlayListWithPlayListName(SelectedSongToOpenBtmSheet, PlaylistName);
            DisplayedPlaylists = PlayListService.GetAllPlaylists();
            var toast = Toast.Make(songAddedToPlaylistText, duration);
            await toast.Show(cts.Token);
        }        
    }

    [RelayCommand]
    public async Task DeletePlaylist()
    {
        await btmSheet.DismissAsync();
        PlaylistManagementService.DeletePlaylist(SelectedPlaylistToOpenBtmSheet.Id);
        DisplayedPlaylists = PlayListService.GetAllPlaylists();
        var toast = Toast.Make(PlaylistDeletedText, duration);
        await toast.Show(cts.Token);
    }

    [RelayCommand]
    async Task OpenPlaylistMenuBtmSheet(PlaylistModelView playlist)
    {
        SelectedPlaylistToOpenBtmSheet = playlist;
        btmSheet = new PlaylistMenuBtmSheet(this);
        await btmSheet.ShowAsync();
    }

    [RelayCommand]
    void RenamePlaylist(string newPlaylistName)
    {
        PlaylistManagementService.RenamePlaylist(SelectedPlaylistToOpenBtmSheet.Id, newPlaylistName);
        DisplayedPlaylists = PlayListService.GetAllPlaylists();
    }
}
