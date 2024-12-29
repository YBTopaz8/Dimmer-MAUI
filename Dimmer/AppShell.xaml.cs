#if WINDOWS
using Microsoft.Maui.Platform;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Microsoft.Maui.ApplicationModel;
using Microsoft.UI.Input; // Required for KeyboardAccelerator
using Microsoft.UI.Xaml.Input; // Required for KeyRoutedEventArgs
using System.Collections.Generic; // Required for List<string> for navigation history
#endif
using System.Diagnostics;

namespace Dimmer_MAUI;

public partial class AppShell : Shell
{

    public AppShell(HomePageVM vm)
    {
        InitializeComponent();

        Routing.RegisterRoute(nameof(MainPageD), typeof(MainPageD));
        Routing.RegisterRoute(nameof(SingleSongShellPageD), typeof(SingleSongShellPageD));
        Routing.RegisterRoute(nameof(PlaylistsPageD), typeof(PlaylistsPageD));
        Routing.RegisterRoute(nameof(ArtistsPageD), typeof(ArtistsPageD));
        Routing.RegisterRoute(nameof(FullStatsPageD), typeof(FullStatsPageD));
        Routing.RegisterRoute(nameof(SingleSongStatsPageD), typeof(SingleSongStatsPageD));
        Routing.RegisterRoute(nameof(SettingsPageD), typeof(SettingsPageD));
        Routing.RegisterRoute(nameof(LandingPageD), typeof(LandingPageD));

        Vm = vm;
        //#if WINDOWS

        //        // Subscribe to events
        //        this.Loaded += AppShell_Loaded;
        //        this.Unloaded += AppShell_Unloaded;
        //        this.Focused += AppShell_Focused;
        //        this.Unfocused += AppShell_Unfocused;

        //#endif
        BindingContext = vm;
        //currentPage = Current.CurrentPage;
    }

    public HomePageVM Vm { get; }

    private async void NavToSingleSongShell_Tapped(object sender, Microsoft.Maui.Controls.TappedEventArgs e)
    {
        await Vm.NavToSingleSongShell();
    }

    private async void MultiSelect_TouchDown(object sender, EventArgs e)
    {
        switch (Vm.CurrentPage)
        {
            case PageEnum.MainPage:
                var mainPage = Current.CurrentPage as MainPageD;

                mainPage!.ToggleMultiSelect_Clicked(sender, e);
                if (Vm.IsMultiSelectOn)
                {
                    GoToSong.IsEnabled = false;
                    Vm.ToggleFlyout(true);
                    GoToSong.Opacity = 0.4;
                    await Task.WhenAll(
                     MultiSelectView.AnimateFadeInFront());
                }
                else
                {
                    Vm.MultiSelectText = string.Empty;
                    GoToSong.IsEnabled = true;
                    GoToSong.Opacity = 1;
                    Vm.ToggleFlyout(false);
                    await Task.WhenAll(MultiSelectView.AnimateFadeOutBack());
                }
                break;
            case PageEnum.NowPlayingPage:
                break;
            case PageEnum.PlaylistsPage:
                break;
            case PageEnum.FullStatsPage:
                break;
            case PageEnum.AllAlbumsPage:
                break;
            case PageEnum.SpecificAlbumPage:
                break;
            default:
                break;
        }
    }

    private void SfEffectsView_TouchDown(object sender, EventArgs e)
    {

    }

#if WINDOWS

    private void AppShell_Loaded(object? sender, EventArgs e)
    {
        //var window = this.Window.Handler.PlatformView as MauiWinUIWindow;

        var currentMauiwindow = Current.Window.Handler.PlatformView as MauiWinUIWindow;

        //currentMauiwindow.ExtendsContentIntoTitleBar=true;

        //AppWindowTitleBar? ss = currentMauiwindow.AppWindow.TitleBar;
        //currentMauiwindow.AppWindow = IPlatformApplication.Current!.Services.GetServices<CustomTitleBar>();

        // If the content itself has child elements, ensure AllowDrop is set on them

        //currentMauiwindow.Content.DragEnter += Content_DragEnter;
        //currentMauiwindow.Content.DragLeave += Content_DragLeave;
        //currentMauiwindow.Content.DragOver += Content_DragOver;
        //currentMauiwindow.Content.Drop += Content_Drop;

        var nativeElement = currentMauiwindow.Content;

        if (nativeElement != null)
        {
            nativeElement.PointerPressed += OnGlobalPointerPressed;
            //nativeElement.KeyDown += NativeElement_KeyDown; just experimenting
            nativeElement.KeyDown += NativeElement_KeyDown; // Re-add for global key press detection
        }
    }

    private void AppShell_Unloaded(object? sender, EventArgs e)
    {
        // Unsubscribe from events when the shell is unloaded
        this.Loaded -= AppShell_Loaded;
        this.Unloaded -= AppShell_Unloaded;
        this.Focused -= AppShell_Focused;
        this.Unfocused -= AppShell_Unfocused;

        // Unsubscribe from global event handlers
        if (Handler?.PlatformView is MauiWinUIWindow mauiWinUIWindow && mauiWinUIWindow.Content != null)
        {
            mauiWinUIWindow.Content.PointerPressed -= OnGlobalPointerPressed;
            mauiWinUIWindow.Content.KeyDown -= NativeElement_KeyDown; // Ensure unsubscription
        }
    }

    public HomePageVM HomePageVM { get; set; }
    Type[] targetPages = new[] { typeof(PlaylistsPageD), typeof(ArtistsPageD), typeof(FullStatsPageD), typeof(SettingsPageD) };
    Page currentPage = new();

    // Custom navigation history for forward navigation
    private Stack<string> _navigationHistory = new Stack<string>();
    private string _previousPage = string.Empty;

    private async void OnGlobalPointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        try
        {
            var nativeElement = this.Handler?.PlatformView as Microsoft.UI.Xaml.UIElement;
            if (nativeElement == null)
                return;

            var properties = e.GetCurrentPoint(nativeElement).Properties;

            if (properties != null)
            {
                // Check for various pointer input properties

                if (properties.IsBarrelButtonPressed)
                {
                    Debug.WriteLine("Pointer barrel button pressed.");
                }

                if (properties.IsCanceled)
                {
                    Debug.WriteLine("Pointer input canceled.");
                }

                if (properties.IsEraser)
                {
                    Debug.WriteLine("Pointer is eraser.");
                }

                if (properties.IsHorizontalMouseWheel)
                {
                    Debug.WriteLine("Horizontal mouse wheel used.");
                }

                if (properties.IsInRange)
                {
                    Debug.WriteLine("Pointer is in range.");
                }

                if (properties.IsInverted)
                {
                    Debug.WriteLine("Pen is inverted.");
                }

                if (properties.IsLeftButtonPressed)
                {
                    Debug.WriteLine("Left mouse button pressed.");
                }

                if (properties.IsMiddleButtonPressed)
                {
                    Debug.WriteLine("Middle mouse button pressed.");
                }

                if (properties.IsPrimary)
                {
                    Debug.WriteLine("Input is from the primary pointer.");
                }

                if (properties.IsRightButtonPressed)
                {
                    Debug.WriteLine("Right mouse button pressed.");
                }

                if (properties.IsXButton1Pressed)
                {
                    // Handle mouse button 4 (Back)
                    Debug.WriteLine("Mouse button 4 clicked globally.");
                    await Current.GoToAsync("..");
                }

                if (properties.IsXButton2Pressed)
                {
                    // Handle mouse button 5 (Forward)
                    Debug.WriteLine("Mouse button 5 clicked globally.");
                    if (_navigationHistory.Count > 0)
                    {
                        string forwardRoute = _navigationHistory.Pop();
                        await Current.GoToAsync(forwardRoute);
                    }
                }

                if (properties.MouseWheelDelta != 0)
                {
                    Debug.WriteLine($"Mouse wheel delta: {properties.MouseWheelDelta}");
                }

                if (properties.Orientation != 0)
                {
                    Debug.WriteLine($"Pointer orientation: {properties.Orientation}");
                }

                Debug.WriteLine($"Pointer update kind: {properties.PointerUpdateKind}");

                if (properties.Pressure != 0.5) // Default pressure is 0.5
                {
                    Debug.WriteLine($"Pointer pressure: {properties.Pressure}");
                }

                if (properties.TouchConfidence)
                {
                    Debug.WriteLine("Touch contact is confident.");
                }
                else
                {
                    Debug.WriteLine("Touch contact is not confident.");
                }

                if (properties.Twist != 0)
                {
                    Debug.WriteLine($"Pointer twist: {properties.Twist}");
                }

                if (properties.XTilt != 0)
                {
                    Debug.WriteLine($"Pointer X tilt: {properties.XTilt}");
                }

                if (properties.YTilt != 0)
                {
                    Debug.WriteLine($"Pointer Y tilt: {properties.YTilt}");
                }
            }
                //else if (properties.IsRightButtonPressed)
                //{
                //    Debug.WriteLine("Clicked");
                //}

                //args.Handled = true; // Stop propagation if needed
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Navigation Exception " + ex.Message);
        }
    }

    private void NativeElement_KeyDown(object? sender, KeyRoutedEventArgs e)
    {
        Debug.WriteLine($"Pressed key: {e.Key} (VirtualKey: {(int)e.Key})");

        // Example of handling specific keys
        //if (e.Key == Windows.System.VirtualKey.F5)
        //{
        //    Debug.WriteLine("F5 key pressed globally!");
        //}
    }

    private void AppShell_Focused(object? sender, FocusEventArgs e)
    {
        var vm = IPlatformApplication.Current!.Services.GetService<HomePageVM>();
        var vmm = IPlatformApplication.Current!.Services.GetService<PlaybackUtilsService>();
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
        var vm = IPlatformApplication.Current!.Services.GetService<HomePageVM>();
        var vmm = IPlatformApplication.Current!.Services.GetService<PlaybackUtilsService>();
        if (vm != null)
        {
            vm.CurrentAppState = AppState.OnBackGround;
        }
        if (vmm != null)
        {
            vmm.CurrentAppState = AppState.OnBackGround;
        }
    }
#endif

    protected override void OnNavigated(ShellNavigatedEventArgs args)
    {
        base.OnNavigated(args);
        //if (args.Current.Location.OriginalString.Contains("MainPageD")) USE THIS TO DO SOMETHING WHEN USER CLICKS BTN
        //{
        //    HandleHomeButtonClicked();
        //}

#if WINDOWS
        // Update navigation history
        if (!string.IsNullOrEmpty(args.Previous?.Location?.OriginalString) && args.Previous.Location.OriginalString != "//AppShell") // Avoid adding the initial navigation
        {
            _navigationHistory.Push(args.Previous.Location.OriginalString);
        }
#endif
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