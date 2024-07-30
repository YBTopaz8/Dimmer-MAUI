namespace Dimmer_MAUI.Views.Mobile;

public partial class PlaylistsPageM : ContentPage
{
	public PlaylistsPageM(PlaylistsPageVM playlistsPageVM, HomePageVM homePageVM, NowPlayingSongPageBtmSheet nowPlayingSongPageBtmSheet)
    {
		InitializeComponent();
        PlaylistsPageVM = playlistsPageVM;
        NowPlayingBtmSheet = nowPlayingSongPageBtmSheet;
        HomePageVM = homePageVM;
        BtmMediaControlsVSL.BindingContext = homePageVM;
        BindingContext = playlistsPageVM;
    }

    public PlaylistsPageVM PlaylistsPageVM { get; }
    public HomePageVM HomePageVM { get; }
    public NowPlayingSongPageBtmSheet NowPlayingBtmSheet { get; set; }

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        PlaylistsPageVM.UpdatePlayLists();
    }

    private void playImgBtn_Clicked(object sender, EventArgs e)
    {
        HomePageVM.PauseResumeSongCommand.Execute(null);
        playImgBtn.IsVisible = false;
        pauseImgBtn.IsVisible = true;
    }

    private void pauseImgBtn_Clicked(object sender, EventArgs e)
    {
        HomePageVM.PauseResumeSongCommand.Execute(null);
        playImgBtn.IsVisible = true;
        pauseImgBtn.IsVisible = false;
    }
    private async void MediaControlBtmBar_Tapped(object sender, TappedEventArgs e)
    {
        await NowPlayingBtmSheet.ShowAsync();
        DeviceDisplay.Current.KeepScreenOn = true;
        //await Shell.Current.GoToAsync(nameof(NowPlayingPageM),true);        
    }

}