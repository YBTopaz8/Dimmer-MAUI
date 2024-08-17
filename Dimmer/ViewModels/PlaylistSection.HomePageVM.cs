using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;

namespace Dimmer_MAUI.ViewModels;
public partial class HomePageVM
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
    PlaylistMenuBtmSheet btmSheet { get; set; }

    [RelayCommand]
    public async Task OpenSpecificPlaylistPage(ObjectId PlaylistID)//string playlistName)
    {
        PlayBackManagerService.GetSongsFromPlaylistID(PlaylistID);
        //PlayListService.GetSongsFromPlaylistID
        
        SelectedPlaylistPageTitle = PlayBackManagerService.SelectedPlaylistName;
        //PlayBackService.UpdateCurrentQueue(, 1);
        //PlayListService.
#if ANDROID
        await Shell.Current.GoToAsync(nameof(SinglePlaylistPageM), true);
#endif
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
        PlayBackManagerService.AddSongToPlayListWithPlayListID(SelectedSongToOpenBtmSheet, PlaylistID);
        //DisplayedPlaylists = PlayListService.GetAllPlaylists();
        var toast = Toast.Make(songAddedToPlaylistText, duration);
        await toast.Show(cts.Token);
    }

    [RelayCommand]
    public async Task CreatePlaylistAndAddSong(string PlaylistName)
    {
        if (!string.IsNullOrEmpty(PlaylistName))
        {
            PlayBackManagerService.AddSongToPlayListWithPlayListName(SelectedSongToOpenBtmSheet, PlaylistName);
            //DisplayedPlaylists = PlayListService.GetAllPlaylists();
            var toast = Toast.Make(songAddedToPlaylistText, duration);
            await toast.Show(cts.Token);
        }
    }

    [RelayCommand]
    public async Task DeletePlaylist()
    {
        await btmSheet.DismissAsync();
        PlayBackManagerService.DeletePlaylistThroughID(SelectedPlaylistToOpenBtmSheet.Id);
        //DisplayedPlaylists = PlayListService.GetAllPlaylists();
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
        //PlaylistManagementService.RenamePlaylist(SelectedPlaylistToOpenBtmSheet.Id, newPlaylistName);
        //HiDisplayedPlaylists = PlayListService.GetAllPlaylists();
    }
}
