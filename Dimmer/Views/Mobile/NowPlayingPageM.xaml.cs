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