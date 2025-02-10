#if WINDOWS
using Microsoft.Maui.Platform;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Microsoft.Maui.ApplicationModel;
using Microsoft.UI.Input; // Required for KeyboardAccelerator
using Microsoft.UI.Xaml.Input; // Required for KeyRoutedEventArgs
using System.Collections.Generic;
using Syncfusion.Maui.Toolkit.Chips; // Required for List<string> for navigation history
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

        //#if WINDOWS

        //        // Subscribe to events
        //        this.Loaded += AppShell_Loaded;
        //        this.Unloaded += AppShell_Unloaded;
        //        this.Focused += AppShell_Focused;
        //        this.Unfocused += AppShell_Unfocused;

        //#endif
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

            SongsColView.ScrollTo(MyViewModel.TemporarilyPickedSong, position: ScrollToPosition.Center, animate: false);

            MyViewModel.PartOfNowPlayingSongsCV = SongsColView;
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

    private void Grid_Loaded(object sender, EventArgs e)
    {

    }

    private void Grid_Unloaded(object sender, EventArgs e)
    {

    } 
    private void PlaySong_Tapped(object sender, TappedEventArgs e)
    {
        if (MyViewModel.TemporarilyPickedSong is not null)
        {
            MyViewModel.TemporarilyPickedSong.IsCurrentPlayingHighlight = false;
        }


        var send = (View)sender;
        var song = (SongModelView)send.BindingContext;
        if (song is not null)
        {
            song.IsCurrentPlayingHighlight = false;
        }

        MyViewModel.PlaySong(song);
    }



    /*
    private void PlayPauseBtn_Clicked(object sender, EventArgs e)
    {
        if (MyViewModel.IsPlaying)
        {
            MyViewModel.PauseSong();
        }
        else
        {
            MyViewModel.ResumeSong();
        }
    }
    private void Slider_DragCompleted(object sender, EventArgs e)
    {
        MyViewModel.SeekSongPosition();
        //if (_isThrottling)
        //    return;

        //_isThrottling = true;



        //await Task.Delay(throttleDelay);
        //_isThrottling = false;
    }

    private void LyricsColView_SelectionChanged(object sender, Microsoft.Maui.Controls.SelectionChangedEventArgs e)
    {
        try
        {
            if (LyricsColView is not null && LyricsColView.ItemsSource is not null)
            {
                if (LyricsColView.SelectedItem is not null)
                {
                    LyricsColView.ScrollTo(LyricsColView.SelectedItem, null, ScrollToPosition.Center, true);
                }
            }

        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }

    private void SeekSongPosFromLyric_Tapped(object sender, Microsoft.Maui.Controls.TappedEventArgs e)
    {
        if (MyViewModel.IsPlaying)
        {
            var bor = (Border)sender;
            var lyr = (LyricPhraseModel)bor.BindingContext;
            MyViewModel.SeekSongPosition(lyr);
        }
    }

    Label CurrentLyrLabel { get; set; }
    private void Label_Loaded(object sender, EventArgs e)
    {
        CurrentLyrLabel = (Label)sender;
    }

    private void TabView_SelectionChanged(object sender, Syncfusion.Maui.Toolkit.TabView.TabSelectionChangedEventArgs e)
    {
        switch (e.NewIndex)
        {
            case 0:
                
                break;
            case 1:
                
                break;
            default:
                break;
        }
    }

    private void OpenCloseQBtmSheet_Clicked(object sender, EventArgs e)
    {
        MyViewModel.IsNowPlayingBtmSheetVisible  = !MyViewModel.IsNowPlayingBtmSheetVisible;
    }

    private void SongInAlbumFromArtistPage_TappedToPlay(object sender, Microsoft.Maui.Controls.TappedEventArgs e)
    {
        MyViewModel.CurrentQueue = 1;
        var s = (Border)sender;
        var song = s.BindingContext as SongModelView;
        MyViewModel.PlaySong(song);
    }
    private void PointerGestureRecognizer_PointerEntered(object sender, Microsoft.Maui.Controls.PointerEventArgs e)
    {
        var send = (View)sender;
        var song = send.BindingContext! as SongModelView;

        send.BackgroundColor = Microsoft.Maui.Graphics.Colors.DarkSlateBlue;
        
    }
    private void ToggleRepeat_Tapped(object sender, Microsoft.Maui.Controls.TappedEventArgs e)
    {
        MyViewModel.ToggleRepeatModeCommand.Execute(true);  
    }*/

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
        MyViewModel.SetContextMenuSong(song!);
        send.BackgroundColor = Microsoft.Maui.Graphics.Colors.Transparent;
        //isPointerEntered = true;
        //isPointerEntered = true;
    }

    private void UserHoverOutSongInColView(object sender, Microsoft.Maui.Controls.PointerEventArgs e)
    {
        var send = (View)sender;
        send.BackgroundColor = Microsoft.Maui.Graphics.Colors.Transparent;

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