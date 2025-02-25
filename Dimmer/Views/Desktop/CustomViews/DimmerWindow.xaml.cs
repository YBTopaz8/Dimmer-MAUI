#if WINDOWS
using Microsoft.Maui.Controls.Platform;
using Microsoft.UI.Xaml.Input;
#endif
using System.Drawing;
using System.Runtime.InteropServices;

namespace Dimmer_MAUI.Views.Desktop.CustomViews;

public partial class DimmerWindow : Window
{
	public DimmerWindow()
	{
        InitializeComponent();
        
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        MyViewModel.CurrentAppState = AppState.OnForeGround;
        MyViewModel.DimmerGlobalSearchBar = SearchSongSB;
    }

    protected override void OnDeactivated()
    {
        base.OnDeactivated();
        MyViewModel.CurrentAppState = AppState.OnBackGround;

    }
    public HomePageVM MyViewModel { get; set; }

    protected override void OnCreated()
    {
        base.OnCreated();
        this.MinimumHeight = 950;
        this.MinimumWidth = 1200;
        this.Height = 950;
        this.Width = 1200;
#if DEBUG
        DimmerTitleBar.Subtitle = "v1.3-debug";
        DimmerTitleBar.BackgroundColor = Microsoft.Maui.Graphics.Colors.DarkSeaGreen;
#endif

#if RELEASE
        DimmerTitleBar.Subtitle = "v1.3-Release";
#endif

        if (!InitChecker())
        {
            return;
        }
        
        StickTopImgBtn.IsVisible = MyViewModel.IsStickToTop;
        UnStickTopImgBtn.IsVisible = !MyViewModel.IsStickToTop;
        if(MyViewModel.CurrentUser is not null)
        {
            onlineCloud.IsVisible = MyViewModel.CurrentUser.IsAuthenticated;
        }


    }
    bool InitChecker()
    {
        MyViewModel ??= IPlatformApplication.Current!.Services.GetService<HomePageVM>();

        if (MyViewModel is null)
        {
            return false;
        }
        return true;
    }
    private CancellationTokenSource _debounceTimer;
    private async void SearchSongSB_TextChanged(object sender, TextChangedEventArgs e)
    {
        if  (!InitChecker())
        {
            return;
        }
        var searchBar = (SearchBar)sender;
        var txt = searchBar.Text;

        _debounceTimer?.Cancel();
        _debounceTimer = new CancellationTokenSource();
        var token = _debounceTimer.Token;

        switch (MyViewModel.CurrentPage)
        {
            case PageEnum.SetupPage:
                break;
            case PageEnum.SettingsPage:
                break;
            case PageEnum.MainPage:

                if (MyViewModel.SongsMgtService.AllSongs is null)
                {
                    return;
                }
                if (MyViewModel.DisplayedSongs is null)
                {
                    return;
                }

                try
                {
                    await Task.Delay(600, token);

                    if (!string.IsNullOrEmpty(txt))
                    {
                        if (txt.Length >= 1)
                        {
                            MyViewModel.IsOnSearchMode = true;
                            MyViewModel.DisplayedSongs.Clear();

                            // Directly filter the songs based on the search text, with null checks
                            var fSongs = MyViewModel.SongsMgtService.AllSongs
                                .Where(item => (!string.IsNullOrEmpty(item.Title) && item.Title.Contains(txt, StringComparison.OrdinalIgnoreCase)) ||
                                               (!string.IsNullOrEmpty(item.ArtistName) && item.ArtistName.Contains(txt, StringComparison.OrdinalIgnoreCase)) ||
                                               (!string.IsNullOrEmpty(item.AlbumName) && item.AlbumName.Contains(txt, StringComparison.OrdinalIgnoreCase)))
                                .ToList();

                            MyViewModel.FilteredSongs = fSongs;

                            foreach (var song in fSongs)
                            {
                                MyViewModel.DisplayedSongs.Add(song);
                            }
                            MyViewModel.CurrentQueue = 1;

                            OnPropertyChanged(nameof(MyViewModel.DisplayedSongs));
                            return;
                        }
                    }
                    else
                    {
                        MyViewModel.IsOnSearchMode = false;
                        MyViewModel.DisplayedSongs.Clear();

                        // Repopulate with all songs when search is empty
                        if (MyViewModel.SongsMgtService.AllSongs != null)
                        {
                            foreach (var song in MyViewModel.SongsMgtService.AllSongs)
                            {
                                MyViewModel.DisplayedSongs.Add(song);
                            }
                        }
                        MyViewModel.CurrentQueue = 0;
                    }
                }
                //catch (TaskCanceledException)
                //{
                //    // Expected if the debounce timer is cancelled
                //}
                catch (Exception ex)
                {
                    Debug.WriteLine($"Search Error: {ex}"); // Log the full exception for debugging
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
            case PageEnum.AllAlbumsPage:
                if (MyViewModel.SongsMgtService.AllAlbums is null)
                {
                    return;
                }
                
                if (MyViewModel.AllAlbums is null)
                {
                    return;
                }
                try
                {
                    await Task.Delay(300, token);

                    if (!string.IsNullOrEmpty(txt))
                    {
                        if (txt.Length >= 1)
                        {

                            MyViewModel.AllAlbums.Clear();
                            // Directly filter the songs based on the search text, with null checks
                            var fAlbums = MyViewModel.SongsMgtService.AllAlbums
                                .Where(item => (!string.IsNullOrEmpty(item.Name) && item.Name.Contains(txt, StringComparison.OrdinalIgnoreCase))).ToList();


                            foreach (var song in fAlbums)
                            {
                                MyViewModel.AllAlbums.Add(song);
                            }

                            OnPropertyChanged(nameof(MyViewModel.AllAlbums));
                            return;
                        }
                    }
                    else
                    {
                        MyViewModel.IsOnSearchMode = false;
                        MyViewModel.AllAlbums.Clear();

                        // Repopulate with all songs when search is empty
                        if (MyViewModel.SongsMgtService.AllAlbums != null)
                        {
                            foreach (var song in MyViewModel.SongsMgtService.AllAlbums)
                            {
                                MyViewModel.AllAlbums.Add(song);
                            }
                            OnPropertyChanged(nameof(MyViewModel.AllAlbums));
                        }
                    }
                }
                catch (TaskCanceledException)
                {
                    // Expected if the debounce timer is cancelled
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Search Error: {ex}"); // Log the full exception for debugging
                }
                break;
            case PageEnum.SpecificAlbumPage:
                break;
            default:
                break;
        }
        if (MyViewModel.CurrentPage != PageEnum.MainPage)
            return;

    }


#if WINDOWS
    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    private const int SW_HIDE = 0;
    private TrayIconHelper _trayIconHelper;
    private const int SW_SHOW = 5;
    // For hooking the native window procedure.
    private WndProcDelegate _newWndProcDelegate;
    private IntPtr _oldWndProc = IntPtr.Zero;
    private const int GWL_WNDPROC = -4;
    private const int WM_COMMAND = 0x0111; // Message from thumbnail buttons
    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, WndProcDelegate newProc);  
    // Overload for unhooking (passing an IntPtr)
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr newProc);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
    private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        // Check if this is our tray icon callback.
        if (msg == TrayIconHelper.WM_TRAYICON)
        {
            int lParamInt = lParam.ToInt32();
            // Check for WM_LBUTTONUP (0x0202) or WM_LBUTTONDBLCLK (0x0203)
            if (lParamInt == 0x0202 || lParamInt == 0x0203)
            {
                ShowWindow(hWnd, SW_SHOW);
                SetWindowLongPtr(hWnd, GWL_WNDPROC, _oldWndProc);
                return IntPtr.Zero;
            }
        }
        else  // Handle WM_COMMAND for thumbnail button clicks.
        if (msg == WM_COMMAND)
        {
            // The low-order word of wParam is the button command ID.
            int commandId = wParam.ToInt32() & 0xffff;
            if (commandId == 100)
            {                
                Debug.WriteLine("Play button clicked.");
                return IntPtr.Zero;
            }
            else if (commandId == 101)
            {
                System.Diagnostics.Debug.WriteLine("Stop button clicked.");
                return IntPtr.Zero;
            }
        }
        return CallWindowProc(_oldWndProc, hWnd, msg, wParam, lParam);
    }

#endif


    private void StickTopImgBtn_Clicked(object sender, EventArgs e)
    {
        if (!InitChecker())
        {
            return;
        }
        MyViewModel.ToggleStickToTopCommand.Execute(null);
        StickTopImgBtn.IsVisible = MyViewModel.IsStickToTop;
        UnStickTopImgBtn.IsVisible = !MyViewModel.IsStickToTop;
    }

    private void UnStickTopImgBtn_Clicked(object sender, EventArgs e)
    {
        if (!InitChecker())
        {
            return;
        }
        MyViewModel.ToggleStickToTopCommand.Execute(null);
        StickTopImgBtn.IsVisible = MyViewModel.IsStickToTop;
        UnStickTopImgBtn.IsVisible = !MyViewModel.IsStickToTop;
    }

    private void SfEffectsView_TouchUp(object sender, EventArgs e)
    {        
        //EventEmoji.IsAnimationPlaying = !EventEmoji.IsAnimationPlaying;
    }


    private void Minimize_Clicked(object sender, EventArgs e)
    {

#if WINDOWS

        IntPtr hwnd = PlatSpecificUtils.DimmerHandle;

        // Hook the window procedure to capture tray icon messages.
        _newWndProcDelegate = new WndProcDelegate(WndProc);
        _oldWndProc = SetWindowLongPtr(hwnd, GWL_WNDPROC, _newWndProcDelegate);


        // Get an icon handle (using the app's executable icon).
        Icon appIcon = Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location);
        IntPtr iconHandle = appIcon.Handle;


        // Create the tray icon.
        _trayIconHelper = new TrayIconHelper();
        _trayIconHelper.CreateTrayIcon( "Dimmer", iconHandle);

        // Hide the main window.
        ShowWindow(hwnd, SW_HIDE);
        this.OnDeactivated();
#endif

        return;
    }
}