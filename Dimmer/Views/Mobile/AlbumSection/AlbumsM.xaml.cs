namespace Dimmer_MAUI.Views.Mobile;

public partial class AlbumsM : ContentPage
{
    NowPlayingBtmSheet btmSheet { get; set; }
    public AlbumsM(HomePageVM homePageVM)
    {
        InitializeComponent();
        HomePageVM = homePageVM;
        this.BindingContext = homePageVM;

        btmSheet = IPlatformApplication.Current?.Services.GetService<NowPlayingBtmSheet>()!;
        //this.Attachments.Add(btmSheet);
    }

    public HomePageVM HomePageVM { get; }
    //private void SongInAlbumFromArtistPage_TappedToPlay(object sender, TappedEventArgs e)
    //{
    //    HomePageVM.CurrentQueue = 1;
    //    var s = (View)sender;
    //    var song = s.BindingContext as SongsModelView;
    //    HomePageVM.PlaySong(song);
    //}
    string previousAlbID = string.Empty;
    private void ShowArtistAlbums_Tapped(object sender, TappedEventArgs e)
    {
        var t = (View)sender;
        var album = t.BindingContext as AlbumModelView;
        if (previousAlbID == album!.LocalDeviceId)
        {
            return;
        }
        HomePageVM.NavigateToSpecificAlbumPageCommand.Execute(album.LocalDeviceId);
        previousAlbID = album.LocalDeviceId;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        HomePageVM.CurrentPage = PageEnum.AllAlbumsPage;
        HomePageVM.GetAllAlbums();
    }

    private void ToolbarItem_Clicked(object sender, EventArgs e)
    {

    }
    protected override bool OnBackButtonPressed()
    {

        return base.OnBackButtonPressed();
    }


    private void AlbumNameTextEdit_TextChanged(object sender, EventArgs e)
    {
        var searchBar = (TextEdit)sender;
        var txt = searchBar.Text;

        if (!string.IsNullOrEmpty(txt))
        {
            if (txt.Length >= 1)
            {
                HomePageVM.IsOnSearchMode = true;
                // Setting the FilterString for SongsColView
                AlbumsColView.FilterString = $"Contains([Name], '{AlbumNameTextEdit.Text}')";
                
            }
            else
            {
                HomePageVM.IsOnSearchMode = false;                
            }
        }
    }
    private void ClearSearch_Clicked(object sender, EventArgs e)
    {
        AlbumsColView.FilterString = string.Empty;
        AlbumNameTextEdit.Text = string.Empty;
    }

    private void UILayoutToggled_SelectionChanged(object sender, EventArgs e)
    {
        var s = sender as ChoiceChipGroup;
        switch (s.SelectedIndex)
        {
            case 0:
                AlbumsColView.ItemSpanCount = 1;
                break;

            case 1:
                AlbumsColView.ItemSpanCount = 2;

                break;
            case 2:
                AlbumsColView.ItemSpanCount = 3;
                break;
            case 3:
                AlbumsColView.ItemSpanCount = 4;
                break;
            default:
                break;
        }
        Debug.WriteLine(s.GetType());
    }

    private void SpecificAlbum_Clicked(object sender, EventArgs e)
    {
        var send = (View)sender;
        var album = send.BindingContext as AlbumModelView;
        HomePageVM.CurrentQueue = 1;
        HomePageVM.NavigateToSpecificAlbumPageCommand.Execute(album);
    }
}