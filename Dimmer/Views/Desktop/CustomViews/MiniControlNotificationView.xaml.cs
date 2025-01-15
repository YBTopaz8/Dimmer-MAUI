using Timer = System.Timers.Timer;
namespace Dimmer_MAUI.Views.CustomViews;

public partial class MiniControlNotificationView : ContentPage
{
   
    private Timer _closeTimer;
    HomePageVM vm;
    public MiniControlNotificationView(string title, string artistName, string imagePath)
    {
		InitializeComponent();
        vm = IPlatformApplication.Current!.Services.GetService<HomePageVM>()!;
        BindingContext = vm;
#if WINDOWS
        _closeTimer = new Timer(5000);
        _closeTimer.Elapsed += _closeTimer_Elapsed;
        _closeTimer.Start();
#endif
        songTitle.Text = title;
        ArtistName.Text = artistName;
        ImagePathh.Source = imagePath;
    }

    private void CloseImgBtn_Clicked(object sender, EventArgs e)
    {
        CloseWindow();
    }

    private void CloseWindow()
    {
      
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var window = Application.Current?.Windows.FirstOrDefault(win => win.Page is MiniControlNotificationView);

            if (window != null)
            {
                Application.Current?.CloseWindow(window);
            }
        });
    }
    public void ResetTimer()
    {
        _closeTimer.Stop();
        _closeTimer.Start();
    }
#if WINDOWS
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

#endif

    public void Update(string title, string artistName, string imagePath)
    {
        songTitle.Text = title;
        ArtistName.Text = artistName;
        ImagePathh.Source = imagePath;
        ResetTimer();
    }

    private void ToggleRepeatButton_Clicked(object sender, EventArgs e)
    {
        ResetTimer();
    }

    private void ImageButton_Clicked(object sender, EventArgs e)
    {
        ResetTimer();
    }

    private void pauseImgBtn_Clicked(object sender, EventArgs e)
    {
        vm.PauseSong();
        ResetTimer();
    }

    private async void playImgBtn_Clicked(object sender, EventArgs e)
    {
        vm.ResumeSong();
        ResetTimer();
    }
}