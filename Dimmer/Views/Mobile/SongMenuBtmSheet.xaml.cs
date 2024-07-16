namespace Dimmer_MAUI.Views.Mobile;

public partial class SongMenuBtmSheet : BottomSheet
{
    public bool IsFavorite { get; set; }
    public HomePageVM HomePageVM { get; set; }
    public PlaylistsPageVM PlaylistsPageVM { get; }

    public SongMenuBtmSheet(PlaylistsPageVM playlistsPageVM, SongsModelView selectedSong)
	{
		InitializeComponent();
        this.HasBackdrop = true;
        this.HasHandle = true;
        PlaylistsPageVM = playlistsPageVM;
        BindingContext = playlistsPageVM;
        PlaylistsPageVM.SelectedSongToOpenBtmSheet = selectedSong!;
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

    private void CreatePlaylistBtn_Clicked(object sender, EventArgs e)
    {
        PlaylistsPageVM.CreatePlaylistAndAddSongCommand.Execute(NewPlaylistName.Text);
    }
}