
#if WINDOWS
using Microsoft.Maui.Platform;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Microsoft.Maui.ApplicationModel;
#endif
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

    private async void NavToSingleSongShell_Tapped(object sender, TappedEventArgs e)
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

            //nativeElement.PointerPressed += OnGlobalPointerPressed;
            //nativeElement.KeyDown += NativeElement_KeyDown; just experimenting
        }
    }


    private void AppShell_Unloaded(object? sender, EventArgs e)
    {
        // Unsubscribe from events when the shell is unloaded
        this.Loaded -= AppShell_Loaded;
        this.Unloaded -= AppShell_Unloaded;
        this.Focused -= AppShell_Focused;
        this.Unfocused -= AppShell_Unfocused;
    }


    #region contentdropWindows
    //private async void Content_Drop(object sender, Microsoft.UI.Xaml.DragEventArgs e)
    //{
    //    if (e.DataView != null)
    //    {
    //        if (e.DataView.Contains(StandardDataFormats.StorageItems))
    //        {
    //            var items = await e.DataView.GetStorageItemsAsync();
    //            if (items.Count > 0)
    //            {
    //                var storageFile = items[0];/// as StorageFile
    //                string filePath = storageFile.Path;

    //                Debug.WriteLine($"File dropped: {filePath}");
    //            }
    //        }
    //    }
    //    Debug.WriteLine("Dropped");
    //}

    //private void Content_DragOver(object sender, Microsoft.UI.Xaml.DragEventArgs e)
    //{
    //    Debug.WriteLine("Drag Over");
    //}

    //private void Content_DragLeave(object sender, Microsoft.UI.Xaml.DragEventArgs e)
    //{
    //    Debug.WriteLine("DragLeave");
    //}

    //private async void Content_DragEnter(object sender, Microsoft.UI.Xaml.DragEventArgs e)
    //{

    //    if (e.DataView != null)
    //    {
    //        if (e.DataView.Contains(StandardDataFormats.StorageItems))
    //        {
    //            var w = e.DataView;
    //            var items = await e.DataView.GetStorageItemsAsync();
    //            bool validFiles = true;

    //            if (items.Count > 0)
    //            {
    //                foreach (var item in items)
    //                {
    //                    if (item is StorageFile file)
    //                    {
    //                        /// Check file extension
    //                        string fileExtension = file.FileType.ToLower();
    //                        if (fileExtension != ".mp3" && fileExtension != ".flac" &&
    //                            fileExtension != ".wav" && fileExtension != ".m4a")
    //                        {
    //                            validFiles = false;
    //                            break;  // If any invalid file is found, break the loop
    //                        }
    //                    }
    //                }

    //                if (validFiles)
    //                {
    //                    e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
    //                }
    //                else
    //                {
    //                    e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.None;  // Deny drop if file types don't match
    //                }

    //            }
    //        }
    //    }
    //}

    #endregion
    public HomePageVM HomePageVM { get; set; }
    Type[] targetPages = new[] { typeof(PlaylistsPageD), typeof(ArtistsPageD), typeof(FullStatsPageD), typeof(SettingsPageD) };
    Page currentPage = new();
    private async void OnGlobalPointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        try
        {

            var nativeElement = this.Handler.PlatformView as Microsoft.UI.Xaml.UIElement;
            var properties = e.GetCurrentPoint(nativeElement).Properties;

            if (properties != null && properties.IsXButton1Pressed)
            {
                // Handle mouse button 4
                

                
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