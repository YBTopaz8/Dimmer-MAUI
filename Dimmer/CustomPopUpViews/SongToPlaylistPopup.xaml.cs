namespace Dimmer_MAUI.CustomPopUpViews;

public partial class SongToPlaylistPopup : Popup
{
    public HomePageVM HomePageVM { get; }

    public SongToPlaylistPopup(HomePageVM homePageVM, IList<string> AllPlaylists)
	{
		InitializeComponent();

        PlaylistsCV.ItemsSource = null;
        PlaylistsCV.ItemsSource = AllPlaylists;
        HomePageVM = homePageVM;
    }

    private void AddToPlaylist_Tapped(object sender, TappedEventArgs e)
    {
        CreateNewPlayListPageBtmSheet.IsVisible = false;
        AddSongToPlayListPageBtmSheet.IsVisible = true;
    }
    private void ShowPlaylistCreationBtmPage_Clicked(object sender, EventArgs e)
    {
        AddSongToPlayListPageBtmSheet.IsVisible = false;
        CreateNewPlayListPageBtmSheet.IsVisible = true;
    }

    private void CancelAddSongToPlaylist_Clicked(object sender, EventArgs e)
    {
        this.Close();
    }

    private void CancelCreateNewPlaylist_Clicked(object sender, EventArgs e)
    {
        CreateNewPlayListPageBtmSheet.IsVisible = false;
        AddSongToPlayListPageBtmSheet.IsVisible = true;
    }

    private void CreatePlaylistBtn_Clicked(object sender, EventArgs e)
    {
        HomePageVM.CreatePlaylistAndAddSongCommand.Execute(NewPlaylistName.Text); 
        this.Close();
    }
    private void CloseBtmSheet_Tapped(object sender, TappedEventArgs e)
    {
        this.Close();
    }

    private void PlaylistsCV_ItemSelected(object sender, SelectedItemChangedEventArgs e)
    {

    }
}