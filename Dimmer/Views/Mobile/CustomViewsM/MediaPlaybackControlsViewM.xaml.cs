namespace Dimmer_MAUI.Views.Mobile.CustomViewsM;

public partial class MediaPlaybackControlsViewM : ContentView
{
    public HomePageVM HomePageVM { get; }
    public NowPlayingSongPageBtmSheet NowPlayingBtmSheet { get; set; }

    public MediaPlaybackControlsViewM()
    {
		InitializeComponent();

        HomePageVM = IPlatformApplication.Current.Services.GetService<HomePageVM>();
        NowPlayingBtmSheet = IPlatformApplication.Current.Services.GetService<NowPlayingSongPageBtmSheet>();
        BindingContext = HomePageVM;
        
        NowPlayingBtmSheet.Dismissed += NowPlayingBtmSheet_Dismissed;
    }
    private void NowPlayingBtmSheet_Dismissed(object? sender, DismissOrigin e)
    {
        DeviceDisplay.Current.KeepScreenOn = false;
    }

    private async void MediaControlBtmBar_Tapped(object sender, TappedEventArgs e)
    {

        await NowPlayingBtmSheet.ShowAsync();
        DeviceDisplay.Current.KeepScreenOn = true;
        //await Shell.Current.GoToAsync(nameof(NowPlayingPageM),true);        
    }

    private async void SwipeGestureRecognizer_Swiped(object sender, SwipedEventArgs e)
    {
        await NowPlayingBtmSheet.ShowAsync();
        DeviceDisplay.Current.KeepScreenOn = true;
    }
}