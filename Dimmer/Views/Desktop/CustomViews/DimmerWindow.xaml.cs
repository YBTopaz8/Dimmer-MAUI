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

        if (MyViewModel is null)
        {
            return;
        }
        MyViewModel.CurrentAppState = AppState.OnForeGround;
        MyViewModel.DimmerGlobalSearchBar = SearchSongSB;
        MyViewModel.InternalNotificationLabelVM = InternalNotificationLabel;
        MyViewModel.InternalSearchSongSBVM= SearchSongSB;
        
        GeneralStaticUtilities.ShowNotificationInternally("Window Activated");
        
    }

    protected override void OnDeactivated()
    {
        base.OnDeactivated();

        if (MyViewModel is null)
        {
            return;
        }
        MyViewModel.CurrentAppState = AppState.OnBackGround;
        MyViewModel.InternalNotificationLabelVM = null;
        MyViewModel.InternalSearchSongSBVM= null;
    }
    public HomePageVM? MyViewModel { get; set; }

    protected async override void OnCreated()
    {
        base.OnCreated();
        this.MinimumHeight = 950;
        this.MinimumWidth = 1200;
        this.Height = 950;
        this.Width = 1200;
#if DEBUG
        DimmerTitleBar.Subtitle = "v1.7-debug";
        DimmerTitleBar.BackgroundColor = Microsoft.Maui.Graphics.Colors.DarkSeaGreen;
#elif RELEASE
        DimmerTitleBar.Subtitle = "v1.7-Release";
#endif

        if (!InitChecker())
        {
            return;
        }
        if (MyViewModel is null)
        {
            return;
        }
        StickTopImgBtn.IsVisible = MyViewModel.IsStickToTop;
        UnStickTopImgBtn.IsVisible = !MyViewModel.IsStickToTop;
        if(MyViewModel.CurrentUser is not null)
        {
            onlineCloud.IsVisible = MyViewModel.CurrentUser.IsAuthenticated;
        }
        //MyViewModel.SongsMgtService.GetUserAccount();


        //await MyViewModel.ConnectToLiveQueriesAsync();
        //await MyViewModel.SetChatRoom(ChatRoomOptions.PersonalRoom);
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
    private CancellationTokenSource? _debounceTimer; 

    private async void SearchSongSB_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (MyViewModel is null)
        {
            return;
        }
        if (!InitChecker())
        {
            return;
        }

        var searchBar = (SearchBar)sender;
        var txt = searchBar.Text;

        _debounceTimer?.Cancel();
        _debounceTimer = new CancellationTokenSource();
        var token = _debounceTimer.Token;

        // Determine the delay based on the page.  This makes the code cleaner.
        int delayMilliseconds = MyViewModel.CurrentPage == PageEnum.AllAlbumsPage ? 300 : 600;

        try
        {
            await Task.Delay(delayMilliseconds, token);

            // Check if the task was canceled *after* the delay.  This is the correct way.
            if (token.IsCancellationRequested)
            {
                return; // Exit if canceled
            }

            // Use Dispatcher to update the UI on the main thread
            Dispatcher.Dispatch(() =>
            {
                switch (MyViewModel.CurrentPage)
                {
                    case PageEnum.SetupPage:
                    case PageEnum.SettingsPage:
                    case PageEnum.NowPlayingPage:
                    case PageEnum.PlaylistsPage:
                    case PageEnum.FullStatsPage:
                    case PageEnum.AllArtistsPage:
                    case PageEnum.SpecificAlbumPage:  
                        break; // Do nothing for these pages

                    case PageEnum.MainPage:
                        SearchSongs(txt);
                        break;

                    case PageEnum.AllAlbumsPage:
                        SearchAlbums(txt);
                        break;
                }
            });
        }
        catch (Exception ex) // Catch *all* exceptions, but log them
        {
            Debug.WriteLine($"Search Error: {ex}"); // Log the full exception for debugging
        }
    }
    private void SearchSongs(string? searchText)
    {
        if (MyViewModel.SongsMgtService.AllSongs is null || MyViewModel.DisplayedSongs is null)
        {
            return; // Nothing to search
        }

        if (string.IsNullOrEmpty(searchText))
        {
            MyViewModel.IsOnSearchMode = false;
            MyViewModel.DisplayedSongs.Clear();
            MyViewModel.CurrentQueue = 0;
            // Repopulate with all songs when search is empty
            foreach (var song in MyViewModel.SongsMgtService.AllSongs)
            {
                MyViewModel.DisplayedSongs.Add(song);
            }
            OnPropertyChanged(nameof(MyViewModel.DisplayedSongs)); // Notify UI after *all* changes
            return;
        }
        // Search text is not empty, perform search
        MyViewModel.IsOnSearchMode = true;
        MyViewModel.DisplayedSongs.Clear();

        // Filter with null checks. This is your existing logic, and it's correct.
        var fSongs = MyViewModel.SongsMgtService.AllSongs
            .Where(item => (!string.IsNullOrEmpty(item.Title) && item.Title.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||
                           (!string.IsNullOrEmpty(item.ArtistName) && item.ArtistName.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||
                           (!string.IsNullOrEmpty(item.AlbumName) && item.AlbumName.Contains(searchText, StringComparison.OrdinalIgnoreCase)))
            .ToList();
        MyViewModel.FilteredSongs = fSongs;
        MyViewModel.CurrentQueue = 1;
        foreach (var song in fSongs)
        {
            MyViewModel.DisplayedSongs.Add(song);
        }
        OnPropertyChanged(nameof(MyViewModel.DisplayedSongs));
    }
    private void SearchAlbums(string? searchText)
    {
        if (MyViewModel.SongsMgtService.AllAlbums is null || MyViewModel.AllAlbums is null)
        {
            return;
        }

        if (string.IsNullOrEmpty(searchText))
        {
            MyViewModel.IsOnSearchMode = false; // Consistent naming
            MyViewModel.AllAlbums.Clear();

            foreach (var album in MyViewModel.SongsMgtService.AllAlbums)
            {
                MyViewModel.AllAlbums.Add(album);
            }

            OnPropertyChanged(nameof(MyViewModel.AllAlbums));
            return;
        }
        // Search text is not empty, perform search

        MyViewModel.AllAlbums.Clear();

        // Filter with null checks
        var fAlbums = MyViewModel.SongsMgtService.AllAlbums
            .Where(item => (!string.IsNullOrEmpty(item.Name) && item.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase)))
            .ToList();
        foreach (var album in fAlbums)
        {
            MyViewModel.AllAlbums.Add(album);
        }

        OnPropertyChanged(nameof(MyViewModel.AllAlbums));

    }
#if WINDOWS
    private TrayIconHelper? _trayIconHelper;
    [DllImport("user32.dll")]
    private static extern IntPtr FindWindow(string className, string windowTitle);

    [DllImport("user32.dll")]
    private static extern IntPtr FindWindowEx(IntPtr parentWindow, IntPtr childWindow, string className, string windowTitle);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    private const int SW_HIDE = 0;
    private const int SW_SHOW = 5;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, WndProcDelegate newProc);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr newProc);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    private WndProcDelegate? _newWndProcDelegate;
    private IntPtr _oldWndProc = IntPtr.Zero;
    private const int GWL_WNDPROC = -4;
    private const int WM_COMMAND = 0x0111;
    private const int WM_NOTIFY = 0x004E;
    private IntPtr _thumbnailPreviewWindowHandle = IntPtr.Zero; // Store the handle

    public void InitializeThumbnailHandling()
    {
        // 1. Find the Thumbnail Preview Window Handle
        _thumbnailPreviewWindowHandle = FindThumbnailPreviewWindow();

        if (_thumbnailPreviewWindowHandle != IntPtr.Zero)
        {
            // 2. Hook the Window Procedure
            _newWndProcDelegate = WndProc;
            _oldWndProc = SetWindowLongPtr(_thumbnailPreviewWindowHandle, GWL_WNDPROC, _newWndProcDelegate);

            if (_oldWndProc == IntPtr.Zero)
            {
                Debug.WriteLine("Failed to hook window procedure. Error code: " + Marshal.GetLastWin32Error());
            }
        }
        else
        {
            Debug.WriteLine("Could not find thumbnail preview window.");
        }
    }

    private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == WM_NOTIFY)
        {
            NMHDR nmhdr = (NMHDR)Marshal.PtrToStructure(lParam, typeof(NMHDR));

            if (nmhdr.code == TTN_COMMAND)
            {
                var commandId = nmhdr.idFrom;
                //int commandId = nmhdr.idCommand;

                if (commandId == 100)
                {
                    Debug.WriteLine("Play button clicked (Thumbnail).");
                    // Your Play button logic here
                }
                else if (commandId == 101)
                {
                    Debug.WriteLine("Pause button clicked (Thumbnail).");
                    // Your Pause button logic here
                }
                else if (commandId == 102)
                {
                    Debug.WriteLine("Resume button clicked (Thumbnail).");
                    // Your Resume button logic here
                }
                else if (commandId == 103)
                {
                    Debug.WriteLine("Previous button clicked (Thumbnail).");
                    // Your Previous button logic here
                }
                else if (commandId == 104)
                {
                    Debug.WriteLine("Next button clicked (Thumbnail).");
                    // Your Next button logic here
                }
            }
        }
        else if (msg == WM_COMMAND)
        {
            // Handle WM_COMMAND if needed (e.g., for other window messages)
        }

        return CallWindowProc(_oldWndProc, hWnd, msg, wParam, lParam);
    }

    private IntPtr FindThumbnailPreviewWindow()
    {
        // **IMPORTANT:** Replace these with the correct class name and window title
        // obtained using Spy++.  These are just placeholders.
        string className = "Shell_ThumbPreview"; // Example - likely incorrect
        string windowTitle = "Your Application Title"; // Example - likely incorrect

        IntPtr hwnd = FindWindow(className, windowTitle);

        if (hwnd == IntPtr.Zero)
        {
            // Try finding a child window if the direct search fails
            hwnd = FindWindowEx(IntPtr.Zero, IntPtr.Zero, className, windowTitle);
        }

        return hwnd;
    }

    public void UnhookThumbnailHandling()
    {
        if (_thumbnailPreviewWindowHandle != IntPtr.Zero)
        {
            SetWindowLongPtr(_thumbnailPreviewWindowHandle, GWL_WNDPROC, _oldWndProc);
            _thumbnailPreviewWindowHandle = IntPtr.Zero;
        }
    }


// Structures needed for WM_NOTIFY handling
[StructLayout(LayoutKind.Sequential)]
public struct NMHDR
{
    public IntPtr hwndFrom;
    public uint idFrom;
    public uint code;
    }

    public const int TTN_COMMAND = 0x0100;

#endif


    private void StickTopImgBtn_Clicked(object sender, EventArgs e)
    {
        if (MyViewModel is null)
        {
            return;
        }
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

    private void SearchSongSB_Focused(object sender, FocusEventArgs e)
    {
        
    }

    private void SearchSongSB_Unfocused(object sender, FocusEventArgs e)
    {
    }

    private void PointerGestureRecognizer_PointerEntered(object sender, PointerEventArgs e)
    {
        MyViewModel.IsOnSearchMode = true;

    }

    private void PointerGestureRecognizer_PointerExited(object sender, PointerEventArgs e)
    {
        MyViewModel.IsOnSearchMode = false;
    }
}