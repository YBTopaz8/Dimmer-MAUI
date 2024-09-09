namespace Dimmer_MAUI.Views.Mobile;

public partial class AlbumPageM : UraniumContentPage
{
	public AlbumPageM(HomePageVM homePageVM)
    {
        InitializeComponent();
        HomePageVM = homePageVM;
        this.BindingContext = homePageVM;
        HomePageVM.GetAllArtistsCommand.Execute(null);
       
    }

    public HomePageVM HomePageVM { get; }
    private void SongInAlbumFromArtistPage_TappedToPlay(object sender, TappedEventArgs e)
    {
        HomePageVM.CurrentQueue = 1;
        var s = (Border)sender;
        var song = s.BindingContext as SongsModelView;
        HomePageVM.PlaySongCommand.Execute(song);
    }
    ObjectId previousAlbID = ObjectId.Empty;
    private void ShowArtistAlbums_Tapped(object sender, TappedEventArgs e)
    {        
        var t = (VerticalStackLayout)sender;
        var album = t.BindingContext as AlbumModelView;
        if (previousAlbID == album.Id)
        {
            return;
        }
        HomePageVM.ShowSpecificArtistsSongsWithAlbumIdCommand.Execute(album.Id);
        previousAlbID = album.Id;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        HomePageVM.CurrentPage = PageEnum.AllAlbumsPage;
    }

    private void ToolbarItem_Clicked(object sender, EventArgs e)
    {

    }
}