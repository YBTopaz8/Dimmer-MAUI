using Timer = System.Timers.Timer;
namespace Dimmer_MAUI.Views.CustomViews;

public partial class MiniControlNotificationView : ContentPage
{
    public event Action<int> ButtonClicked; // Event to pass values to the main app

    private readonly TaskCompletionSource<int> _buttonClickedTcs = new();

    private Timer _closeTimer;
    public MiniControlNotificationView(SongsModelView playingSong)
	{
		InitializeComponent();// Initialize and start the timer
        
#if WINDOWS
        songTitle.Text = playingSong.Title;
        songArtistName.Text = playingSong.ArtistName;
        coverImage.Source = playingSong.CoverImagePath;
        _closeTimer = new Timer(5000);
        _closeTimer.Elapsed += _closeTimer_Elapsed;
        _closeTimer.Start();
#endif
    }

    private void ImageButton_Clicked(object sender, EventArgs e)
    {
        ResetTimer();
    }
    private void CloseImgBtn_Clicked(object sender, EventArgs e)
    {
        CloseWindow();
    }

#if WINDOWS
    private void CloseWindow()
    {
        Dispatcher.Dispatch(() =>
        {
            var window = Application.Current?.Windows.FirstOrDefault(win => win.Page is MiniControlNotificationView);
            if (window != null)
            {
                Application.Current?.CloseWindow(window);
            }
        });
    }
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _closeTimer?.Stop();
        _closeTimer?.Dispose();

    }
    private void _closeTimer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        CloseWindow();
    }

    private void ResetTimer()
    {
        _closeTimer.Stop();
        _closeTimer.Start();
    }
    public void UpdateSongDetails(SongsModelView playingSong)
    {
        songTitle.Text = playingSong.Title;
        songArtistName.Text = playingSong.ArtistName;
        coverImage.Source = playingSong.CoverImagePath;
        ResetTimer();
    }

#endif
}