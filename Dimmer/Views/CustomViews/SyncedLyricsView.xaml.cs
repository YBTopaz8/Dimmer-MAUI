namespace Dimmer_MAUI.Views.CustomViews;

public partial class SyncedLyricsView : ContentView
{
	public SyncedLyricsView()
	{
		InitializeComponent();
	}
    private void LyricsColView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LyricsColView.IsLoaded)
        {
            LyricsColView.ScrollTo(LyricsColView.SelectedItem, ScrollToPosition.Center);
            //LyricsColView.ScrollTo(HomePageVM.CurrentLyricPhrase, ScrollToPosition.Center);
        }
    }
}