#if WINDOWS
using Microsoft.Maui.Platform;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Microsoft.Maui.ApplicationModel;
using Microsoft.UI.Input; // Required for KeyboardAccelerator
using Microsoft.UI.Xaml.Input; // Required for KeyRoutedEventArgs
using System.Collections.Generic;
using Syncfusion.Maui.Toolkit.Chips;
using Syncfusion.Maui.Toolkit.EffectsView;
using Microsoft.UI.Xaml; // Required for List<string> for navigation history
#endif

namespace Dimmer_MAUI;
public partial class AppShell : Shell
{

    public AppShell(HomePageVM vm)
    {
        MyViewModel = vm;
        BindingContext = vm;
        InitializeComponent();

        Routing.RegisterRoute(nameof(MainPageD), typeof(MainPageD));
        Routing.RegisterRoute(nameof(SingleSongShellPageD), typeof(SingleSongShellPageD));
        Routing.RegisterRoute(nameof(PlaylistsPageD), typeof(PlaylistsPageD));
        Routing.RegisterRoute(nameof(ArtistsPageD), typeof(ArtistsPageD));
        Routing.RegisterRoute(nameof(AlbumsPageD), typeof(AlbumsPageD));
        Routing.RegisterRoute(nameof(FullStatsPageD), typeof(FullStatsPageD));
        Routing.RegisterRoute(nameof(SingleSongStatsPageD), typeof(SingleSongStatsPageD));
        Routing.RegisterRoute(nameof(SettingsPageD), typeof(SettingsPageD));
        Routing.RegisterRoute(nameof(LandingPageD), typeof(LandingPageD));

#if WINDOWS

        // Subscribe to events
        this.Loaded += AppShell_Loaded;
        this.Unloaded += AppShell_Unloaded;
        //this.Focused += AppShell_Focused;
        //this.Unfocused += AppShell_Unfocused;

#endif
        //currentPage = Current.CurrentPage;
    }

    public HomePageVM MyViewModel { get; }

    private async void NavToSingleSongShell_Tapped(object sender, Microsoft.Maui.Controls.TappedEventArgs e)
    {
        await MyViewModel.NavToSingleSongShell();
    }

    private void SongsColView_Loaded(object sender, EventArgs e)
    {
        try
        {
            if (MyViewModel.PickedSong is null || MyViewModel.TemporarilyPickedSong is null)
            {
                return;
            }
            MyViewModel.PickedSong = MyViewModel.TemporarilyPickedSong;

            if (SongsColView is not null && MyViewModel.CurrentAppState == AppState.OnForeGround)
            {
                SongsColView.ScrollTo(MyViewModel.TemporarilyPickedSong, position: ScrollToPosition.Start, animate: false);
            }
            
            MyViewModel.PartOfNowPlayingSongsCV = SongsColView;

            MyViewModel.UpdateContextMenuData(MyViewModel.MySelectedSong);

        }
        catch (Exception ex)
        {
            Debug.WriteLine("Error when scrolling " + ex.Message);
        }
    }



    private async void MultiSelect_TouchDown(object sender, EventArgs e)
    {
        switch (MyViewModel.CurrentPage)
        {
            case PageEnum.MainPage:
                var mainPage = Current.CurrentPage as MainPageD;
                if (MyViewModel.DisplayedSongs.Count < 1)
                {
                    return;
                }
                mainPage!.ToggleMultiSelect_Clicked(sender, e);
                if (MyViewModel.IsMultiSelectOn)
                {
                    GoToSong.IsEnabled = false;
                    //MyViewModel.ToggleFlyout(true);
                    GoToSong.Opacity = 0.4;
                    await Task.WhenAll(MultiSelectView.AnimateFadeInFront());
                }
                else
                {
                    MyViewModel.MultiSelectText = string.Empty;
                    GoToSong.IsEnabled = true;
                    GoToSong.Opacity = 1;
                    //MyViewModel.ToggleFlyout(false);
                    await Task.WhenAll(MultiSelectView.AnimateFadeOutBack());
                }
                break;
            case PageEnum.NowPlayingPage:
                break;
            case PageEnum.PlaylistsPage:
                break;
            case PageEnum.FullStatsPage:
                break;
            case PageEnum.AllArtistsPage:
                break;
            case PageEnum.SpecificAlbumPage:
                break;
            default:
                break;
        }
    }

    private void SfEffectsView_TouchDown(object sender, EventArgs e)
    {
#if WINDOWS
        var send = (SfEffectsView)sender;
        var song = send.TouchDownCommandParameter as SongModelView; 

        MyViewModel.MySelectedSong = song;

#endif
    }

#if WINDOWS
    UIElement DimmerUIElement;

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

        DimmerUIElement = currentMauiwindow.Content;

        if (DimmerUIElement != null)
        {
            DimmerUIElement.PointerPressed += OnGlobalPointerPressed;

            DimmerUIElement.PointerWheelChanged += MainLayout_PointerWheelChanged;
            DimmerUIElement.KeyDown += MainLayout_KeyDown;
            //DimmerUIElement.ProcessKeyboardAccelerators += DimmerUIElement_ProcessKeyboardAccelerators;
            //nativeElement.KeyDown += NativeElement_KeyDown; just experimenting
            //nativeElement.KeyDown += NativeElement_KeyDown; // Re-add for global key press detection
        }
    }

    //private void DimmerUIElement_ProcessKeyboardAccelerators(UIElement sender, ProcessKeyboardAcceleratorEventArgs args)
    //{
    //    if (args.Key == Windows.System.VirtualKey.Control)
    //    {

    //    }
    //}

    private void AppShell_Unloaded(object? sender, EventArgs e)
    {
        // Unsubscribe from events when the shell is unloaded
        this.Loaded -= AppShell_Loaded;
        this.Unloaded -= AppShell_Unloaded;
        this.Focused -= AppShell_Focused;
        this.Unfocused -= AppShell_Unfocused;

        DimmerUIElement.PointerPressed -= OnGlobalPointerPressed;
        DimmerUIElement.PointerWheelChanged -= MainLayout_PointerWheelChanged;
        DimmerUIElement.KeyDown -= MainLayout_KeyDown;
        DimmerUIElement.DragOver += DimmerUIElement_DragOverAsync;
    }

    List<string> supportedFilePaths;
    bool isAboutToDropFiles = false;
    private async void DimmerUIElement_DragOverAsync(object sender, Microsoft.UI.Xaml.DragEventArgs WindowsEventArgs)
    {

#if WINDOWS
        try
        {

            if (!isAboutToDropFiles)
            {
                isAboutToDropFiles = true;

                var dragUI = WindowsEventArgs.DragUIOverride;


                var items = await WindowsEventArgs.DataView.GetStorageItemsAsync();
                WindowsEventArgs.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.None;
                supportedFilePaths = new List<string>();

                if (items.Count > 0)
                {
                    foreach (var item in items)
                    {
                        if (item is Windows.Storage.StorageFile file)
                        {
                            /// Check file extension
                            string fileExtension = file.FileType.ToLower();
                            if (fileExtension != ".mp3" && fileExtension != ".flac" &&
                                fileExtension != ".wav" && fileExtension != ".m4a")
                            {
                                WindowsEventArgs.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.None;
                                dragUI.IsGlyphVisible = true;
                                dragUI.Caption = $"{fileExtension.ToUpper()} Files Not Supported";
                                continue;
                                //break;  // If any invalid file is found, break the loop
                            }
                            else
                            {
                                
                                dragUI.IsGlyphVisible = false;
                                dragUI.Caption = "Drop to Play!";
                                Debug.WriteLine($"File is {item.Path}");
                                supportedFilePaths.Add(item.Path.ToLower());
                            }
                        }
                    }

                }
            }

        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
        //return Task.CompletedTask;
#endif
    }

    private void MainLayout_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        // Retrieve the mouse wheel delta value
        var pointerPoint = e.GetCurrentPoint(null);
        int mouseWheelDelta = pointerPoint.Properties.MouseWheelDelta;

        // Check if the event is from a mouse wheel
        if (mouseWheelDelta != 0)
        {
            // Positive delta indicates wheel scrolled up
            // Negative delta indicates wheel scrolled down
            if (mouseWheelDelta > 0)
            {
                MyViewModel.IncreaseVolumeCommand.Execute(true);
                // Handle scroll up
            }
            else
            {
                MyViewModel.DecreaseVolumeCommand.Execute(true);
                // Handle scroll down
            }
        }

        // Mark the event as handled
        e.Handled = true;
    }
    bool isCtrlPressed = false;
    bool isShiftPressed = false;
    bool isAltPressed = false;
    private void MainLayout_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        isCtrlPressed = e.Key.HasFlag(Windows.System.VirtualKey.Control) || e.Key.HasFlag(Windows.System.VirtualKey.LeftControl) || e.Key.HasFlag(Windows.System.VirtualKey.RightControl);
        isShiftPressed = e.Key.HasFlag(Windows.System.VirtualKey.LeftShift);
        isAltPressed = e.Key.HasFlag(Windows.System.VirtualKey.LeftMenu);

        switch (e.Key)
        {
            case Windows.System.VirtualKey.None:
                break;
            case Windows.System.VirtualKey.LeftButton:
                break;
            case Windows.System.VirtualKey.RightButton:
                break;
            case Windows.System.VirtualKey.Cancel:
                break;
            case Windows.System.VirtualKey.MiddleButton:
                break;
            case Windows.System.VirtualKey.XButton1:
                break;
            case Windows.System.VirtualKey.XButton2:
                break;
            case Windows.System.VirtualKey.Back:
                break;
            case Windows.System.VirtualKey.Tab:
                break;
            case Windows.System.VirtualKey.Clear:
                break;
            case Windows.System.VirtualKey.Enter:
                break;
            case Windows.System.VirtualKey.Shift:
                break;
            case Windows.System.VirtualKey.Control:
                break;
            case Windows.System.VirtualKey.Menu:
                break;
            case Windows.System.VirtualKey.Pause:
                break;
            case Windows.System.VirtualKey.CapitalLock:
                break;
            case Windows.System.VirtualKey.Kana:
                break;
            case Windows.System.VirtualKey.Junja:
                break;
            case Windows.System.VirtualKey.Final:
                break;
            case Windows.System.VirtualKey.Hanja:
                break;
            case Windows.System.VirtualKey.Escape:
                break;
            case Windows.System.VirtualKey.Convert:
                break;
            case Windows.System.VirtualKey.NonConvert:
                break;
            case Windows.System.VirtualKey.Accept:
                break;
            case Windows.System.VirtualKey.ModeChange:
                break;
            case Windows.System.VirtualKey.Space:
                if (MyViewModel.IsPlaying)
                {
                    MyViewModel.PauseSong();
                }
                else
                {
                    MyViewModel.ResumeSong();
                }
                break;
            case Windows.System.VirtualKey.PageUp:
                break;
            case Windows.System.VirtualKey.PageDown:
                break;
            case Windows.System.VirtualKey.End:
                break;
            case Windows.System.VirtualKey.Home:
                break;
            case Windows.System.VirtualKey.Left:
                break;
            case Windows.System.VirtualKey.Up:
                break;
            case Windows.System.VirtualKey.Right:
                break;
            case Windows.System.VirtualKey.Down:
                break;
            case Windows.System.VirtualKey.Select:
                break;
            case Windows.System.VirtualKey.Print:
                break;
            case Windows.System.VirtualKey.Execute:
                break;
            case Windows.System.VirtualKey.Snapshot:
                break;
            case Windows.System.VirtualKey.Insert:
                break;
            case Windows.System.VirtualKey.Delete:
                MyViewModel.DeleteFileCommand.Execute(MyViewModel.MySelectedSong);
                break;
            case Windows.System.VirtualKey.Help:
                break;
            case Windows.System.VirtualKey.Number0:
                break;
            case Windows.System.VirtualKey.Number1:
                break;
            case Windows.System.VirtualKey.Number2:
                break;
            case Windows.System.VirtualKey.Number3:
                break;
            case Windows.System.VirtualKey.Number4:
                break;
            case Windows.System.VirtualKey.Number5:
                break;
            case Windows.System.VirtualKey.Number6:
                break;
            case Windows.System.VirtualKey.Number7:
                break;
            case Windows.System.VirtualKey.Number8:
                break;
            case Windows.System.VirtualKey.Number9:
                break;
            case Windows.System.VirtualKey.A:
                break;
            case Windows.System.VirtualKey.B:
                break;
            case Windows.System.VirtualKey.C:
                break;
            case Windows.System.VirtualKey.D:
                break;
            case Windows.System.VirtualKey.E:
                break;
            case Windows.System.VirtualKey.F:
                //SearchSongSB.Focus();
                break;
            case Windows.System.VirtualKey.G:
                break;
            case Windows.System.VirtualKey.H:
                break;
            case Windows.System.VirtualKey.I:
                break;
            case Windows.System.VirtualKey.J:
                break;
            case Windows.System.VirtualKey.K:
                break;
            case Windows.System.VirtualKey.L:
                break;
            case Windows.System.VirtualKey.M:
                break;
            case Windows.System.VirtualKey.N:
                break;
            case Windows.System.VirtualKey.O:
                break;
            case Windows.System.VirtualKey.P:
                break;
            case Windows.System.VirtualKey.Q:
                break;
            case Windows.System.VirtualKey.R:
                break;
            case Windows.System.VirtualKey.S:
                break;
            case Windows.System.VirtualKey.T:
                break;
            case Windows.System.VirtualKey.U:
                break;
            case Windows.System.VirtualKey.V:
                break;
            case Windows.System.VirtualKey.W:
                break;
            case Windows.System.VirtualKey.X:
                break;
            case Windows.System.VirtualKey.Y:
                break;
            case Windows.System.VirtualKey.Z:
                break;
            case Windows.System.VirtualKey.LeftWindows:
                break;
            case Windows.System.VirtualKey.RightWindows:
                break;
            case Windows.System.VirtualKey.Application:
                break;
            case Windows.System.VirtualKey.Sleep:
                break;
            case Windows.System.VirtualKey.NumberPad0:
                break;
            case Windows.System.VirtualKey.NumberPad1:
                break;
            case Windows.System.VirtualKey.NumberPad2:
                break;
            case Windows.System.VirtualKey.NumberPad3:
                break;
            case Windows.System.VirtualKey.NumberPad4:
                break;
            case Windows.System.VirtualKey.NumberPad5:
                break;
            case Windows.System.VirtualKey.NumberPad6:
                break;
            case Windows.System.VirtualKey.NumberPad7:
                break;
            case Windows.System.VirtualKey.NumberPad8:
                break;
            case Windows.System.VirtualKey.NumberPad9:
                break;
            case Windows.System.VirtualKey.Multiply:
                break;
            case Windows.System.VirtualKey.Add:
                break;
            case Windows.System.VirtualKey.Separator:
                break;
            case Windows.System.VirtualKey.Subtract:
                break;
            case Windows.System.VirtualKey.Decimal:
                break;
            case Windows.System.VirtualKey.Divide:
                break;
            case Windows.System.VirtualKey.F1:
                break;
            case Windows.System.VirtualKey.F2:
                break;
            case Windows.System.VirtualKey.F3:
                break;
            case Windows.System.VirtualKey.F4:
                break;
            case Windows.System.VirtualKey.F5:
                break;
            case Windows.System.VirtualKey.F6:
                break;
            case Windows.System.VirtualKey.F7:
                break;
            case Windows.System.VirtualKey.F8:
                break;
            case Windows.System.VirtualKey.F9:
                break;
            case Windows.System.VirtualKey.F10:
                break;
            case Windows.System.VirtualKey.F11:
                break;
            case Windows.System.VirtualKey.F12:
                break;
            case Windows.System.VirtualKey.F13:
                break;
            case Windows.System.VirtualKey.F14:
                break;
            case Windows.System.VirtualKey.F15:
                break;
            case Windows.System.VirtualKey.F16:
                break;
            case Windows.System.VirtualKey.F17:
                break;
            case Windows.System.VirtualKey.F18:
                break;
            case Windows.System.VirtualKey.F19:
                break;
            case Windows.System.VirtualKey.F20:
                break;
            case Windows.System.VirtualKey.F21:
                break;
            case Windows.System.VirtualKey.F22:
                break;
            case Windows.System.VirtualKey.F23:
                break;
            case Windows.System.VirtualKey.F24:
                break;
            case Windows.System.VirtualKey.NavigationView:
                break;
            case Windows.System.VirtualKey.NavigationMenu:
                break;
            case Windows.System.VirtualKey.NavigationUp:
                break;
            case Windows.System.VirtualKey.NavigationDown:
                break;
            case Windows.System.VirtualKey.NavigationLeft:
                break;
            case Windows.System.VirtualKey.NavigationRight:
                break;
            case Windows.System.VirtualKey.NavigationAccept:
                break;
            case Windows.System.VirtualKey.NavigationCancel:
                break;
            case Windows.System.VirtualKey.NumberKeyLock:
                break;
            case Windows.System.VirtualKey.Scroll:
                break;
            case Windows.System.VirtualKey.LeftShift:
                break;
            case Windows.System.VirtualKey.RightShift:
                break;
            case Windows.System.VirtualKey.LeftControl:
                break;
            case Windows.System.VirtualKey.RightControl:
                break;
            case Windows.System.VirtualKey.LeftMenu:
                break;
            case Windows.System.VirtualKey.RightMenu:
                break;
            case Windows.System.VirtualKey.GoBack:
                break;
            case Windows.System.VirtualKey.GoForward:
                break;
            case Windows.System.VirtualKey.Refresh:
                break;
            case Windows.System.VirtualKey.Stop:
                break;
            case Windows.System.VirtualKey.Search:
                break;
            case Windows.System.VirtualKey.Favorites:
                break;
            case Windows.System.VirtualKey.GoHome:
                break;
            case Windows.System.VirtualKey.GamepadA:
                break;
            case Windows.System.VirtualKey.GamepadB:
                break;
            case Windows.System.VirtualKey.GamepadX:
                break;
            case Windows.System.VirtualKey.GamepadY:
                break;
            case Windows.System.VirtualKey.GamepadRightShoulder:
                break;
            case Windows.System.VirtualKey.GamepadLeftShoulder:
                break;
            case Windows.System.VirtualKey.GamepadLeftTrigger:
                break;
            case Windows.System.VirtualKey.GamepadRightTrigger:
                break;
            case Windows.System.VirtualKey.GamepadDPadUp:
                break;
            case Windows.System.VirtualKey.GamepadDPadDown:
                break;
            case Windows.System.VirtualKey.GamepadDPadLeft:
                break;
            case Windows.System.VirtualKey.GamepadDPadRight:
                break;
            case Windows.System.VirtualKey.GamepadMenu:
                break;
            case Windows.System.VirtualKey.GamepadView:
                break;
            case Windows.System.VirtualKey.GamepadLeftThumbstickButton:
                break;
            case Windows.System.VirtualKey.GamepadRightThumbstickButton:
                break;
            case Windows.System.VirtualKey.GamepadLeftThumbstickUp:
                break;
            case Windows.System.VirtualKey.GamepadLeftThumbstickDown:
                break;
            case Windows.System.VirtualKey.GamepadLeftThumbstickRight:
                break;
            case Windows.System.VirtualKey.GamepadLeftThumbstickLeft:
                break;
            case Windows.System.VirtualKey.GamepadRightThumbstickUp:
                break;
            case Windows.System.VirtualKey.GamepadRightThumbstickDown:
                break;
            case Windows.System.VirtualKey.GamepadRightThumbstickRight:
                break;
            case Windows.System.VirtualKey.GamepadRightThumbstickLeft:
                break;
            default:
                break;
        }
    }


    private void Grid_Loaded(object sender, EventArgs e)
    {

    }




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
                    if (MyViewModel.CurrentPage == PageEnum.MainPage)
                    {
                        MyViewModel.IsMultiSelectOn = true;
                    }
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
            MyViewModel.CurrentAppState = AppState.OnForeGround;
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
            MyViewModel.CurrentAppState = AppState.OnBackGround;
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


    private void Grid_Unloaded(object sender, EventArgs e)
    {

    } 
    private void PlaySong_Tapped(object sender, TappedEventArgs e)
    {
        var send = (View)sender;
        var song = (SongModelView)send.BindingContext;
        if (song is not null)
        {
            song.IsCurrentPlayingHighlight = false;
        }

        MyViewModel.PlaySong(song);
    }




    private void UserHoverOnSongInColView(object sender, Microsoft.Maui.Controls.PointerEventArgs e)
    {
        try
        {
            if (MyViewModel.MySelectedSong is null || MyViewModel.TemporarilyPickedSong is null)
            {
                return;
            }
            MyViewModel.PickedSong = MyViewModel.MySelectedSong;

            //SongsColView.ScrollTo(MyViewModel.MySelectedSong, position: ScrollToPosition.Start, animate: true);
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Error when scrolling " + ex.Message);
        }
        var send = (View)sender;
        var song = send.BindingContext! as SongModelView;
        //MyViewModel.SetContextMenuSong(song!);
        send.BackgroundColor = Microsoft.Maui.Graphics.Colors.Transparent;
        //isPointerEntered = true;
        //isPointerEntered = true;
    }

    private void UserHoverOutSongInColView(object sender, Microsoft.Maui.Controls.PointerEventArgs e)
    {
        var send = (View)sender;
        send.BackgroundColor = Microsoft.Maui.Graphics.Colors.Transparent;

    }

    private void AddPlayNextEff_TouchDown(object sender, EventArgs e)
    {
        MyViewModel.AddNextInQueue(MyViewModel.MySelectedSong);
    }

    private void AddToPlaylist_Tapped(object sender, TappedEventArgs e)
    {
        CreateNewPlayListPageBtmSheet.IsVisible = false;
        AddSongToPlayListPageBtmSheet.IsVisible = true;
    }
    private void ShowPlaylistCreationBtmPage_Clicked(object sender, EventArgs e)
    {
        AddSongToPlayListPageBtmSheet.IsVisible = false;
        CreateNewPlayListPageBtmSheet.IsVisible = true;
    }

    private void CancelAddSongToPlaylist_Clicked(object sender, EventArgs e)
    {
        //this.Close();
    }

    private void CancelCreateNewPlaylist_Clicked(object sender, EventArgs e)
    {
        CreateNewPlayListPageBtmSheet.IsVisible = false;
        AddSongToPlayListPageBtmSheet.IsVisible = true;
    }

    private void CreatePlaylistBtn_Clicked(object sender, EventArgs e)
    {
        MyViewModel.CreatePlaylistAndAddSongCommand.Execute(NewPlaylistName.Text);
        //this.Close();
    }
    private void CloseBtmSheet_Tapped(object sender, TappedEventArgs e)
    {
        //this.Close();
    }

    private void PlaylistsCV_ItemSelected(object sender, SelectedItemChangedEventArgs e)
    {

    }

    private void TabView_SelectionChanged(object sender, Syncfusion.Maui.Toolkit.TabView.TabSelectionChangedEventArgs e)
    {
        switch (e.NewIndex)
        {
            case 0:
                break;
            case 1:
                if (MyViewModel.AllSyncLyrics is not null)
                {
                    MyViewModel.AllSyncLyrics = new();
                }
                break;
            case 2:
                break;
            default:

                break;
        }
        if (e.NewIndex == 2)
        {
            MyViewModel.ShowSingleSongStatsCommand.Execute(MyViewModel.MySelectedSong);
        }
    }

    private void DeleteSongEff_TouchDown(object sender, EventArgs e)
    {
        MyViewModel.DeleteFileCommand.Execute(MyViewModel.MySelectedSong);
    }

    private void GoToArtistPageEff_TouchDown(object sender, EventArgs e)
    {

    }

    private void GoToAlbumPageEff_TouchDown(object sender, EventArgs e)
    {

    }
}

public enum PageEnum
{
    SetupPage,
    SettingsPage,
    MainPage,
    NowPlayingPage,
    PlaylistsPage,
    FullStatsPage,
    AllArtistsPage,
    AllAlbumsPage,
    SpecificAlbumPage
}