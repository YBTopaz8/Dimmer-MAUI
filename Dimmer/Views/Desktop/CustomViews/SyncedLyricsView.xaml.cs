namespace Dimmer_MAUI.Views.CustomViews;

public partial class SyncedLyricsView : ContentView
{
    public string UnSyncLyrics { get; set; }
    public SyncedLyricsView()
    {
        InitializeComponent();
    }

    public void ScrollToLyric()
    {
        LyricsColView_SelectionChanged(null, null);
    }

    private void LyricsColView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (CanScroll)
            {
                if (LyricsColView.IsLoaded && LyricsColView.ItemsSource is not null)
                {
                    LyricsColView.ScrollTo(LyricsColView.SelectedItem, null, ScrollToPosition.Center, false);
                }
            }

        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }

    HomePageVM ViewModel { get; set; }
    private void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
    {
        if (ViewModel is null)
        {
            ViewModel = IPlatformApplication.Current.Services.GetService<HomePageVM>();
        }
        var bor = (Border)sender;
        var lyr = (LyricPhraseModel)bor.BindingContext;
        ViewModel.SeekSongPosition(lyr);
    }

    bool CanScroll = true;
    private void PointerGestureRecognizer_PointerEntered(object sender, PointerEventArgs e)
    {
        CanScroll = false;
    }

    private void PointerGestureRecognizer_PointerExited(object sender, PointerEventArgs e)
    {
        CanScroll = true;
    }
}