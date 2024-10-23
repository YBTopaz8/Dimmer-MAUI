
#if WINDOWS
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
#endif
namespace Dimmer_MAUI;

public partial class AppShell : Shell
{

    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(nameof(HomeD), typeof(HomeD));
        Routing.RegisterRoute(nameof(NowPlayingD), typeof(NowPlayingD));
        Routing.RegisterRoute(nameof(PlaylistsPageD), typeof(PlaylistsPageD));
        Routing.RegisterRoute(nameof(ArtistsPageD), typeof(ArtistsPageD));
        Routing.RegisterRoute(nameof(FullStatsD), typeof(FullStatsD));
        Routing.RegisterRoute(nameof(SingleSongStatsPageD), typeof(SingleSongStatsPageD));

#if WINDOWS

        // Subscribe to events
        this.Loaded += AppShell_Loaded;
        this.Unloaded += AppShell_Unloaded;
        this.Focused += AppShell_Focused;
        this.Unfocused += AppShell_Unfocused;

#endif

    }

#if WINDOWS
    private void AppShell_Unloaded(object? sender, EventArgs e)
    {
        // Unsubscribe from events when the shell is unloaded
        this.Loaded -= AppShell_Loaded;
        this.Unloaded -= AppShell_Unloaded;
        this.Focused -= AppShell_Focused;
        this.Unfocused -= AppShell_Unfocused;
    }
#endif

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
        //var window = this.Window.Handler.PlatformView as MauiWinUIWindow;

        var currentMauiwindow = Current.Window.Handler.PlatformView as MauiWinUIWindow;
        // If the content itself has child elements, ensure AllowDrop is set on them

        //currentMauiwindow.Content.DragEnter += Content_DragEnter;
        //currentMauiwindow.Content.DragLeave += Content_DragLeave;
        //currentMauiwindow.Content.DragOver += Content_DragOver;
        //currentMauiwindow.Content.Drop += Content_Drop;


        var nativeElement = currentMauiwindow.Content;

        HomePageVM = IPlatformApplication.Current.Services.GetService<HomePageVM>();
        if (nativeElement != null)
        {

            nativeElement.PointerPressed += OnGlobalPointerPressed;
            //nativeElement.KeyDown += NativeElement_KeyDown; just experimenting
        }
    }


    private async void Content_Drop(object sender, Microsoft.UI.Xaml.DragEventArgs e)
    {
        if (e.DataView != null)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();
                if (items.Count > 0)
                {
                    var storageFile = items[0];/// as StorageFile
                    string filePath = storageFile.Path;

                    Debug.WriteLine($"File dropped: {filePath}");
                }
            }
        }
        Debug.WriteLine("Dropped");
    }

    private void Content_DragOver(object sender, Microsoft.UI.Xaml.DragEventArgs e)
    {
        Debug.WriteLine("Drag Over");
    }

    private void Content_DragLeave(object sender, Microsoft.UI.Xaml.DragEventArgs e)
    {
        Debug.WriteLine("DragLeave");
    }

    private async void Content_DragEnter(object sender, Microsoft.UI.Xaml.DragEventArgs e)
    {
        
        if (e.DataView != null)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var w = e.DataView;
                var items = await e.DataView.GetStorageItemsAsync();
                bool validFiles = true;

                if (items.Count > 0)
                {
                    foreach (var item in items)
                    {
                        if (item is StorageFile file)
                        {
                            /// Check file extension
                            string fileExtension = file.FileType.ToLower();
                            if (fileExtension != ".mp3" && fileExtension != ".flac" &&
                                fileExtension != ".wav" && fileExtension != ".m4a")
                            {
                                validFiles = false;
                                break;  // If any invalid file is found, break the loop
                            }
                        }
                    }

                    if (validFiles)
                    {
                        e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
                    }
                    else
                    {
                        e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.None;  // Deny drop if file types don't match
                    }

                }
            }
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
            Debug.WriteLine("Navigation Exception " + ex.Message);
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