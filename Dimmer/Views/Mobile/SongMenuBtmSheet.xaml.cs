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
    
    private void AddToPlaylist_Tapped(object sender, TappedEventArgs e)
    {
        FirstPageBtmSheet.IsVisible = false;        
        CreateNewPlayListPageBtmSheet.IsVisible = false;
        AddSongToPlayListPageBtmSheet.IsVisible = true;
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

    private async void CreatePlaylistBtn_Clicked(object sender, EventArgs e)
    {
        HomePageVM.CreatePlaylistAndAddSongCommand.Execute(NewPlaylistName.Text); //TODO ADD TOAST NOTIFICATION SAYING SONG ADDED
        await NewPlaylistName.EntryView.HideKeyboardAsync();
        await this.DismissAsync();
    }

    private void CloseBtmSheet_Tapped(object sender, TappedEventArgs e)
    {
        this.DismissAsync();
    }

    private async void OpenNavPlayingSongPage_Tapped(object sender, TappedEventArgs e)
    {
        HomePageVM.NavToNowPlayingPageCommand.Execute(null);
        await this.DismissAsync();
    }
}