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

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        //HomePageVM.RefreshPlaylists();
    }

    private void playImgBtn_Clicked(object sender, EventArgs e)
    {
        HomePageVM.PauseResumeSongCommand.Execute(null);        
    }

    private void pauseImgBtn_Clicked(object sender, EventArgs e)
    {
        HomePageVM.PauseResumeSongCommand.Execute(null);
    }
    private async void MediaControlBtmBar_Tapped(object sender, TappedEventArgs e)
    {
        await NowPlayingBtmSheet.ShowAsync();
        DeviceDisplay.Current.KeepScreenOn = true;
        //await Shell.Current.GoToAsync(nameof(NowPlayingPageM),true);        
    }

}