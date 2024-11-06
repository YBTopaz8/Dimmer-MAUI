namespace Dimmer_MAUI.Views.Mobile.CustomViewsM;

public partial class MediaPlaybackControlsViewM : ContentView
{
    public HomePageVM HomePageVM { get; }
    public NowPlayingBtmSheetContainer NowPlayingBtmSheet { get; set; }

    public MediaPlaybackControlsViewM()
    {
		InitializeComponent();

        HomePageVM = IPlatformApplication.Current.Services.GetService<HomePageVM>();
        //NowPlayingBtmSheet = IPlatformApplication.Current.Services.GetService<NowPlayingBtmSheet>();
        BindingContext = HomePageVM;
        
        
    }
    private void MediaControlBtmBar_Tapped(object sender, TappedEventArgs e)
    {

        //await NowPlayingBtmSheet.ShowAsync();
        DeviceDisplay.Current.KeepScreenOn = true;
        //await Shell.Current.GoToAsync(nameof(NowPlayingPageM),true);        
    }

    private async void SwipeGestureRecognizer_Swiped(object sender, SwipedEventArgs e)
    {
        //await NowPlayingBtmSheet.ShowAsync();
        DeviceDisplay.Current.KeepScreenOn = true;
    }
}