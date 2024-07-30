namespace Dimmer_MAUI;

public partial class App : Application
{
    public App(INativeAudioService audioService)
    {
        InitializeComponent();
#if WINDOWS
        MainPage = new AppShell();

#elif ANDROID
        MainPage = new AppShellMobile();
#endif
    }

    public ISongsManagementService SongsManagementService { get; }
    public IPlaylistManagementService PlaylistManagementService { get; }

    protected override Window CreateWindow(IActivationState activationState)
    {
        var window = base.CreateWindow(activationState);
        window.MinimumHeight = 800;
        window.MinimumWidth = 1200;
        window.Height = 900;
        window.Width = 1200;
        
        window.Title = "Dimmer";

        return window;
    }

    
    //protected override void OnSleep()
    //{
    //    (SongsManagementService as IDisposable)?.Dispose();
    //    base.OnSleep();

    //}
}
