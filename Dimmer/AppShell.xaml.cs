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
        this.Loaded += AppShell_Loaded;

        this.Unloaded -= AppShell_Loaded;
#endif
        
    }
#if WINDOWS
    private void AppShell_Loaded(object? sender, EventArgs e)
    {
        var nativeElement = this.Handler.PlatformView as Microsoft.UI.Xaml.UIElement;

        if (nativeElement != null)
        {
            nativeElement.PointerPressed += OnGlobalPointerPressed;
        }
    }

    private async void OnGlobalPointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        var nativeElement = this.Handler.PlatformView as Microsoft.UI.Xaml.UIElement;
        var properties = e.GetCurrentPoint(nativeElement).Properties;

        //if (properties.IsLeftButtonPressed)
        //{
        //    // Handle left-click
        //    Debug.WriteLine("Left mouse button clicked globally.");
        //}
        //else if (properties.IsMiddleButtonPressed)
        //{
        //    // Handle middle-click
        //    Debug.WriteLine("Middle mouse button clicked globally.");
        //}
        if (properties.IsXButton1Pressed)
        {
            // Handle mouse button 4
            var currentPage = Current.CurrentPage;

            var targetPages = new[] { typeof(PlaylistsPageM), typeof(AlbumsM), typeof(TopStatsPageM) };

            if (targetPages.Contains(currentPage.GetType()))
            {
                
                shelltabbar.CurrentItem = homeTab;
                //return true;
            }


            if (currentPage.GetType() != typeof(HomeD))
            {

                if (currentPage.GetType() == typeof(PlaylistsPageD))
                {
                    var homePageInstance = IPlatformApplication.Current.Services.GetService<HomeD>();


                    // Clear the existing navigation stack
                    List<ShellItem>? navigationStack = Current.Items.ToList();
                    var s = Current.CurrentItem;

                    await Current.GoToAsync(nameof(HomeD));

                    return;
                }
                await Current.GoToAsync("..");
            }
        }


        //else if (properties.IsXButton2Pressed)
        //{
        //    // Handle mouse button 5
        //    Debug.WriteLine("Mouse button 5 clicked globally.");
        //}
        //else if (properties.IsRightButtonPressed)
        //{
        //    Debug.WriteLine("Clicked");

        //}

        //args.Handled = true; // Stop propagation if needed


    }
#endif

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