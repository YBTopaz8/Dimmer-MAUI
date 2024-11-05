namespace Dimmer_MAUI.Views.Mobile;

public partial class AlbumsM : UraniumContentPage
{
    NowPlayingBtmSheet btmSheet { get; set; }
    public AlbumsM(HomePageVM homePageVM)
    {
        InitializeComponent();
        HomePageVM = homePageVM;
        this.BindingContext = homePageVM;

        btmSheet = IPlatformApplication.Current.Services.GetService<NowPlayingBtmSheet>();
        //this.Attachments.Add(btmSheet);
    }

    public HomePageVM HomePageVM { get; }
    private void SongInAlbumFromArtistPage_TappedToPlay(object sender, TappedEventArgs e)
    {
        HomePageVM.CurrentQueue = 1;
        var s = (View)sender;
        var song = s.BindingContext as SongsModelView;
        HomePageVM.PlaySongCommand.Execute(song);
    }
    ObjectId previousAlbID = ObjectId.Empty;
    private void ShowArtistAlbums_Tapped(object sender, TappedEventArgs e)
    {        
        var t = (View)sender;
        var album = t.BindingContext as AlbumModelView;
        if (previousAlbID == album!.Id)
        {
            return;
        }
        HomePageVM.NavigateToSpecificAlbumPageCommand.Execute(album.Id);
        previousAlbID = album.Id;
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

    private void AlbumsColView_Tap(object sender, DevExpress.Maui.CollectionView.CollectionViewGestureEventArgs e)
    {
        var album = e.Item as AlbumModelView;
        HomePageVM.CurrentQueue = 1;
        HomePageVM.NavigateToSpecificAlbumPageCommand.Execute(album.Id);
    }
}