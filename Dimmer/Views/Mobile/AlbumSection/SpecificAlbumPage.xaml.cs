namespace Dimmer_MAUI.Views.Mobile;

public partial class SpecificAlbumPage : ContentPage
{
    NowPlayingBtmSheet? btmSheet { get; set; }
    public SpecificAlbumPage(HomePageVM homePageVM)
    {
        InitializeComponent();
        this.BindingContext = homePageVM;
        MyViewModel = homePageVM;

        btmSheet = IPlatformApplication.Current?.Services.GetService<NowPlayingBtmSheet>();
        //this.Attachments.Add(btmSheet);
    }
    public HomePageVM MyViewModel { get; }
    protected override void OnAppearing()
    {
        base.OnAppearing();
        MyViewModel.CurrentPage = PageEnum.SpecificAlbumPage;
    }

    private void SongInAlbumFromArtistPage_TappedToPlay(object sender, TappedEventArgs e)
    {
        MyViewModel.CurrentQueue = 1;
        var s = (Border)sender;
        var song = s.BindingContext as SongModelView;
        MyViewModel.PlaySong(song);
    }

    private async void FetchAlbumCover_TapReleased(object sender, DevExpress.Maui.Core.DXTapEventArgs e)
    {
        await MyViewModel.FetchAlbumCoverImage(MyViewModel.SelectedAlbumOnArtistPage);
    }
}
