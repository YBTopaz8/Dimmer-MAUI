namespace Dimmer_MAUI;

public partial class AppShell : Shell
{

    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(nameof(HomeD), typeof(HomeD));
        Routing.RegisterRoute(nameof(NowPlayingD), typeof(NowPlayingD));
        Routing.RegisterRoute(nameof(PlaylistsPageD), typeof(PlaylistsPageD));
        Routing.RegisterRoute(nameof(ArtistsPageD), typeof (ArtistsPageD));
        Routing.RegisterRoute(nameof(FullStatsD), typeof (FullStatsD));
        Routing.RegisterRoute(nameof(SingleSongStatsPageD), typeof (SingleSongStatsPageD));

#if WINDOWS
        //InitializeSharpHook();
#endif
    }

    /* If/When I decide to put back sharphook
#if WINDOWS
    private void InitializeSharpHook()
    {
        hook = new SimpleReactiveGlobalHook();

        // Subscribe to KeyPressed event
        keyPressedSubscription = hook.KeyPressed
            .Where(e => e.Data.KeyCode == KeyCode.VcF9)
            .Subscribe(OnGlobalKeyPressed);

        // Subscribe to MousePressed event
        mousePressedSubscription = hook.MousePressed
            .Where(e => e.Data.Button == MouseButton.Button4 || e.Data.Button == MouseButton.Button5)
            .Subscribe(OnGlobalMousePressed);

        hook.RunAsync();

        // Get the window handle
        var mauiWindow = Application.Current?.Windows?.FirstOrDefault();
        if (mauiWindow != null)
        {
            var nativeWindow = mauiWindow.Handler.PlatformView;
            _hwnd = WindowNative.GetWindowHandle(nativeWindow);
        }
    }

    private void OnGlobalKeyPressed(KeyboardHookEventArgs e)
    {
        
        MiniPlayBackControlNotif.BringAppToFront();
    }

    private void OnGlobalMousePressed(MouseHookEventArgs e)
    {
        // Check if the app is in focus
        var mauiWindow = Application.Current?.Windows?.FirstOrDefault();
        if (mauiWindow != null)
        {
            
            if (e.Data.Button == MouseButton.Button4)
            {
                // Handle mouse button 4
                Debug.WriteLine("Mouse button 4 clicked.");
            }
            else if (e.Data.Button == MouseButton.Button5)
            {
                // Handle mouse button 5
                Debug.WriteLine("Mouse button 5 clicked.");
            }
        }
    }


    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        // Dispose of subscriptions and stop the hook
        keyPressedSubscription?.Dispose();
        mousePressedSubscription?.Dispose();
        hook?.Dispose();
    }
#endif
    */


    protected override void OnNavigated(ShellNavigatedEventArgs args)
    {
        base.OnNavigated(args);
        //if (args.Current.Location.OriginalString.Contains("MainPageD")) USE THIS TO DO SOMETHING WHEN USER CLICKS BTN
        //{
        //    HandleHomeButtonClicked();
        //}
    }

        
}

public enum PageEnum
{
    MainPage,
    NowPlayingPage,
    PlaylistsPage,
    FullStatsPage,
    AllAlbumsPage,
    SpecificAlbumPage
}