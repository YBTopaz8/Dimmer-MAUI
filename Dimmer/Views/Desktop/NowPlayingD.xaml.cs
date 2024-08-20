namespace Dimmer_MAUI.Views.Desktop;

public partial class NowPlayingD : ContentPage
{
	public NowPlayingD(HomePageVM homePageVM)
    {
		InitializeComponent();
        HomePageVM = homePageVM;
        BindingContext = homePageVM;
        MediaPlayBackCW.BindingContext = homePageVM;

        EditableSongsTagsV.HomePageVM = homePageVM;
        EditableSongsTagsV.BindingContext = HomePageVM;
    }
    public HomePageVM HomePageVM { get; }
    private void LyricsColView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        //if (LyricsColView.IsLoaded && LyricsColView.ItemsSource is not null)
        //{
        //    LyricsColView.ScrollTo(LyricsColView.SelectedItem, null,ScrollToPosition.Center, false);
        //    //LyricsColView.ScrollTo(HomePageVM.CurrentLyricPhrase, ScrollToPosition.Center);
        //}
    }
}