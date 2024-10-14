namespace Dimmer_MAUI.Views.Mobile;

public partial class SongMenuBtmSheet : BottomSheet
{
    public bool IsFavorite { get; set; }
    public HomePageVM HomePageVM { get; set; }

    public SongMenuBtmSheet(HomePageVM homePageVM, SongsModelView selectedSong)
    {
        InitializeComponent();
        this.HasBackdrop = true;
        this.HasHandle = true;

        BindingContext = homePageVM;
        homePageVM.SelectedSongToOpenBtmSheet = selectedSong!;
        HomePageVM = homePageVM;
    }

    private void ShowPlaylistCreationBtmPage_Clicked(object sender, EventArgs e)
    {
        FirstPageBtmSheet.IsVisible = false;
        AddSongToPlayListPageBtmSheet.IsVisible = false;
        CreateNewPlayListPageBtmSheet.IsVisible = true;
    }

    private void CancelAddSongToPlaylist_Clicked(object sender, EventArgs e)
    {
        FirstPageBtmSheet.IsVisible = true;
        CreateNewPlayListPageBtmSheet.IsVisible = false;
        AddSongToPlayListPageBtmSheet.IsVisible = false;
    }

    private void CancelCreateNewPlaylist_Clicked(object sender, EventArgs e)
    {
        FirstPageBtmSheet.IsVisible = false;
        CreateNewPlayListPageBtmSheet.IsVisible = false;
        AddSongToPlayListPageBtmSheet.IsVisible = true;
    }

    private void CloseBtmSheet_Tapped(object sender, TappedEventArgs e)
    {
        this.DismissAsync();
    }

    private async void GoToAlbum_Clicked(object sender, EventArgs e)
    {
        await HomePageVM.NavigateToSpecificAlbumPageFromBtmSheet(HomePageVM.SelectedSongToOpenBtmSheet);
        await this.DismissAsync(true);
    }

    private async void GoToArtist_Clicked(object sender, EventArgs e)
    {
        await HomePageVM.NavigateToArtistsPage(HomePageVM.SelectedSongToOpenBtmSheet);
        await this.DismissAsync(true);
    }

    private async void SetPlayRepeat_Clicked(object sender, EventArgs e)
    {
        await this.DismissAsync();
        HomePageVM.OpenRepeatSetterPopupCommand.Execute(null);
    }

    private async void ShareSong_Clicked(object sender, EventArgs e)
    {
        HomePageVM.NavigateToShareStoryPageCommand.Execute(null);
        await this.DismissAsync(true);
    }

    private void DltSongFromDevice_Clicked(object sender, EventArgs e)
    {
        this.DismissAsync(true);
        HomePageVM.DeleteFileCommand.Execute(HomePageVM.SelectedSongToOpenBtmSheet);
    }

    private async void ExploreSong_Clicked(object sender, EventArgs e)
    {
        HomePageVM.NavToNowPlayingPageCommand.Execute(null);
        await this.DismissAsync();
    }

    private async void AddToPlaylist_Clicked(object sender, EventArgs e)
    {
        FirstPageBtmSheet.IsVisible = false;
        CreateNewPlayListPageBtmSheet.IsVisible = false;
        AddSongToPlayListPageBtmSheet.IsVisible = true;
    }

    private async void CreatePlaylistBtn_Clicked(object sender, EventArgs e)
    {
        HomePageVM.CreatePlaylistAndAddSongCommand.Execute(NewPlaylistName.Text);
        await NewPlaylistName.EntryView.HideKeyboardAsync();
        await this.DismissAsync();
    }
}