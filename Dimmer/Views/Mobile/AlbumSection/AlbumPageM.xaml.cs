namespace Dimmer_MAUI.Views.Mobile;

public partial class AlbumPageM : ContentPage
{
    NowPlayingBtmSheet? btmSheet { get; set; }
    public AlbumPageM(HomePageVM homePageVM)
    {
        InitializeComponent();
        MyViewModel = homePageVM;
        this.BindingContext = homePageVM;

        btmSheet = IPlatformApplication.Current?.Services.GetService<NowPlayingBtmSheet>();
        //this.Attachments.Add(btmSheet);
    }

    public HomePageVM MyViewModel { get; }
    
    string previousAlbID = string.Empty;
    private async void ShowArtistAlbums_Tapped(object sender, TappedEventArgs e)
    {        
        var t = (VerticalStackLayout)sender;
        var album = t.BindingContext as AlbumModelView;
        if (previousAlbID == album!.LocalDeviceId)
        {
            return;
        }
        await MyViewModel.ShowSpecificArtistsSongsWithAlbum(album);
        previousAlbID = album.LocalDeviceId;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        //AllAlbumsColView.SelectedItem = MyViewModel.SelectedAlbumOnArtistPage;
        MyViewModel.CurrentPage = PageEnum.AllArtistsPage;

        if (MyViewModel.MySelectedSong is null)
        {
            MyViewModel.MySelectedSong = MyViewModel.TemporarilyPickedSong!;
        }
        MyViewModel.LoadAllArtistsAlbumsAndLoadAnAlbumSong(song: MyViewModel.TemporarilyPickedSong, isFromSong: true);
    }

    private void ToolbarItem_Clicked(object sender, EventArgs e)
    {

    }
    private async void ImageButton_Clicked(object sender, EventArgs e)
    {
        var send = (View)sender;
        var album = send.BindingContext as AlbumModelView;
        await MyViewModel.ShowSpecificArtistsSongsWithAlbum(album!);
    }

    private void NowPlaySearchBtmSheet_TapReleased(object sender, DevExpress.Maui.Core.DXTapEventArgs e)
    {

    }

    private void NowPlaySearchBtmSheet_TapPressed(object sender, DevExpress.Maui.Core.DXTapEventArgs e)
    {
     
    }

    private void DXCollectionView_TapConfirmed(object sender, DevExpress.Maui.CollectionView.CollectionViewGestureEventArgs e)
    {
        MyViewModel.CurrentQueue = 1;

        var song = e.Item as SongModelView;
        MyViewModel.PlaySong(song);
    }
}