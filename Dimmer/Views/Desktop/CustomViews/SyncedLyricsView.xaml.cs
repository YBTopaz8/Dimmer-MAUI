namespace Dimmer_MAUI.Views.CustomViews;

public partial class SyncedLyricsView : ContentView
{
    public string UnSyncLyrics { get; set; }
    public SyncedLyricsView()
    {
        InitializeComponent();
    }

    private void LyricsColView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (LyricsColView.IsLoaded && LyricsColView.ItemsSource is not null)
            {
                LyricsColView.ScrollTo(LyricsColView.SelectedItem, null, ScrollToPosition.Center, false);
            }

        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }

    private void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
    {
        var bor = (Border)sender;
        var lyr = (LyricPhraseModel)bor.BindingContext;
    }
}