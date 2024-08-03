namespace Dimmer_MAUI.Views.Desktop;

public partial class NowPlayingD : ContentPage
{
	public NowPlayingD(HomePageVM homePageVM)
    {
		InitializeComponent();
        HomePageVM = homePageVM;
        BindingContext = homePageVM;
    }
    public HomePageVM HomePageVM { get; }
    private void LyricsColView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LyricsColView.IsLoaded)
        {
            LyricsColView.ScrollTo(HomePageVM.CurrentLyricPhrase, ScrollToPosition.Center);
        }
    }

}