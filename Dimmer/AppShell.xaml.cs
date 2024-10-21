using System.Diagnostics;

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

        this.Focused += AppShell_Focused;
        this.Unfocused += AppShell_Unfocused;
#endif

    }

    private void AppShell_Focused(object? sender, FocusEventArgs e)
    {
        var vm = IPlatformApplication.Current.Services.GetService<HomePageVM>();
        var vmm = IPlatformApplication.Current.Services.GetService<PlaybackUtilsService>();
        if (vm != null)
        {
            vm.CurrentAppState = AppState.OnForeGround;
        }
        if (vmm != null)
        {
            vmm.CurrentAppState = AppState.OnForeGround;
        }
    }

    private void AppShell_Unfocused(object? sender, FocusEventArgs e)
    {
        var vm = IPlatformApplication.Current.Services.GetService<HomePageVM>();
        var vmm = IPlatformApplication.Current.Services.GetService<PlaybackUtilsService>();
        if (vm != null)
        {
            vm.CurrentAppState = AppState.OnBackGround;
        }
        if (vmm != null)
        {
            vmm.CurrentAppState = AppState.OnBackGround;
        }
    }

#if WINDOWS
    private void AppShell_Loaded(object? sender, EventArgs e)
    {
        
        var nativeElement = this.Handler.PlatformView as Microsoft.UI.Xaml.UIElement;
        
        HomePageVM = IPlatformApplication.Current.Services.GetService<HomePageVM>();
        if (nativeElement != null)
        {
            nativeElement.PointerPressed += OnGlobalPointerPressed;
            //nativeElement.KeyDown += NativeElement_KeyDown; just experimenting
        }
    }

    public HomePageVM HomePageVM { get; set; }
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                  
    private async void OnGlobalPointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        try
        {
            
            var nativeElement = this.Handler.PlatformView as Microsoft.UI.Xaml.UIElement;
            var properties = e.GetCurrentPoint(nativeElement).Properties;

            if (properties != null && properties.IsXButton1Pressed)
            {
                // Handle mouse button 4
                var currentPage = Current.CurrentPage;

                var targetPages = new[] { typeof(PlaylistsPageD), typeof(ArtistsPageD), typeof(FullStatsD) };

                if (targetPages.Contains(currentPage.GetType()))
                {

                    shelltabbar.CurrentItem = homeTab;
                    await Current.Navigation.PopAsync();
                    return;
                }
                await Current.GoToAsync("..");
            }

        }
        catch (Exception ex)
        {
            Debug.WriteLine("Navigation Exception "+ex.Message);
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


    //protected override void OnNavigated(ShellNavigatedEventArgs args)
    //{
    //    base.OnNavigated(args);
    //    //if (args.Current.Location.OriginalString.Contains("MainPageD")) USE THIS TO DO SOMETHING WHEN USER CLICKS BTN
    //    //{
    //    //    HandleHomeButtonClicked();
    //    //}
    //}

        
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