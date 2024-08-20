namespace Dimmer_MAUI.Views.CustomViews;

public partial class SyncedLyricsView : ContentView
{
	public SyncedLyricsView()
	{
		InitializeComponent();
        
	}
    private void LyricsColView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LyricsColView.IsLoaded && LyricsColView.ItemsSource is not null)
        {
            LyricsColView.ScrollTo(LyricsColView.SelectedItem, null,ScrollToPosition.Center);
            //LyricsColView.ScrollTo(HomePageVM.CurrentLyricPhrase, ScrollToPosition.Center);
        }
    }

    private void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
    {
        var bor = (Border)sender;
        var lyr = (LyricPhraseModel)bor.BindingContext;

    }
}