using Dimmer_MAUI.ViewModels;

namespace Dimmer_MAUI.Views.Mobile;

public partial class NowPlayingPageM : UraniumContentPage
{
	public NowPlayingPageM(HomePageVM homePageVM)
    {
		InitializeComponent();
        this.HomePageVM = homePageVM;
        BindingContext = homePageVM;
        
    }
    public HomePageVM HomePageVM { get; }
    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (Shell.Current.CurrentPage == this)
        {
            DeviceDisplay.Current.KeepScreenOn = true;
        }
    }
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        DeviceDisplay.Current.KeepScreenOn = false;
    }
    private void syncCol_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            //SyncedLyricsColView.ScrollTo(HomePageVM.CurrentLyricPhrase);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message + " When scrolling");
        }

    }

    private async void SwipeGestureRecognizer_Swiped(object sender, SwipedEventArgs e)
    {
        await Shell.Current.GoToAsync("..",true);
    }
}