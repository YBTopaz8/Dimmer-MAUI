namespace Dimmer_MAUI.Views.Mobile;

public partial class AlbumPageM : ContentPage
{
    NowPlayingBtmSheet? btmSheet { get; set; }
    public AlbumPageM(HomePageVM homePageVM)
    {
        InitializeComponent();
        HomePageVM = homePageVM;
        this.BindingContext = homePageVM;

        btmSheet = IPlatformApplication.Current?.Services.GetService<NowPlayingBtmSheet>();
        //this.Attachments.Add(btmSheet);
    }

    public HomePageVM HomePageVM { get; }
    
    ObjectId previousAlbID = ObjectId.Empty;
    private async void ShowArtistAlbums_Tapped(object sender, TappedEventArgs e)
    {        
        var t = (VerticalStackLayout)sender;
        var album = t.BindingContext as AlbumModelView;
        if (previousAlbID == album!.Id)
        {
            return;
        }
        await HomePageVM.ShowSpecificArtistsSongsWithAlbum(album);
        previousAlbID = album.Id;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        //AllAlbumsColView.SelectedItem = HomePageVM.SelectedAlbumOnArtistPage;
        HomePageVM.CurrentPage = PageEnum.AllAlbumsPage;

        if (HomePageVM.SelectedSongToOpenBtmSheet is null)
        {
            HomePageVM.SelectedSongToOpenBtmSheet = HomePageVM.TemporarilyPickedSong!;
        }
        HomePageVM.GetAllArtistsAlbum(song: HomePageVM.TemporarilyPickedSong, isFromSong: true);
    }

    private void ToolbarItem_Clicked(object sender, EventArgs e)
    {

    }
    private async void ImageButton_Clicked(object sender, EventArgs e)
    {
        var send = (View)sender;
        var album = send.BindingContext as AlbumModelView;
        await HomePageVM.ShowSpecificArtistsSongsWithAlbum(album!);
    }

    private void NowPlaySearchBtmSheet_TapReleased(object sender, DevExpress.Maui.Core.DXTapEventArgs e)
    {

    }

    private void NowPlaySearchBtmSheet_TapPressed(object sender, DevExpress.Maui.Core.DXTapEventArgs e)
    {
     
    }

    private void DXCollectionView_TapConfirmed(object sender, DevExpress.Maui.CollectionView.CollectionViewGestureEventArgs e)
    {
        HomePageVM.CurrentQueue = 1;

        var song = e.Item as SongModelView;
        HomePageVM.PlaySongCommand.Execute(song);
    }
}