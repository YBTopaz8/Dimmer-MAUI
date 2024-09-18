namespace Dimmer_MAUI.Views.Mobile;

public partial class PlaylistsPageM : ContentPage
{
	public PlaylistsPageM(HomePageVM homePageVM, NowPlayingSongPageBtmSheet nowPlayingSongPageBtmSheet)
    {
		InitializeComponent();
        NowPlayingBtmSheet = nowPlayingSongPageBtmSheet;
        HomePageVM = homePageVM;
        BindingContext = homePageVM;
    }
    public HomePageVM HomePageVM { get; }
    public NowPlayingSongPageBtmSheet NowPlayingBtmSheet { get; set; }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        HomePageVM.CurrentPage = PageEnum.PlaylistsPage;
        HomePageVM.RefreshPlaylists();
    }
    protected override void OnDisappearing()
    {
        base.OnDisappearing();        
    }
   
    private async void MediaControlBtmBar_Tapped(object sender, TappedEventArgs e)
    {
        await NowPlayingBtmSheet.ShowAsync();
        DeviceDisplay.Current.KeepScreenOn = true;
    }

}