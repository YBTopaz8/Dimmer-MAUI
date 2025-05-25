using System.Drawing.Imaging;
using ImageFormat = System.Drawing.Imaging.ImageFormat;

namespace Dimmer.WinUI.Utils.StaticUtils;
public static class PlatUtils
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool PrintWindow(IntPtr hwnd, IntPtr hdcBlt, uint nFlags);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetWindowRect(IntPtr hwnd, out Rect lpRect);

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect { public int Left, Top, Right, Bottom; }

    /// <summary>
    /// Captures the given MAUI Window to a PNG-backed ImageSource.
    /// </summary>
    public static ImageSource CaptureWindow(this Window mauiWindow)
    {
        if (mauiWindow == null)
            throw new ArgumentNullException(nameof(mauiWindow));
        // 1) get native handle
        var native = mauiWindow.Handler.PlatformView as Microsoft.Maui.MauiWinUIWindow
                     ?? throw new InvalidOperationException("Not running on WinUI");
        IntPtr hwnd = WindowNative.GetWindowHandle(native);

        // 2) grab bounds
        GetWindowRect(hwnd, out var rect);
        int w = rect.Right - rect.Left;
        int h = rect.Bottom - rect.Top;

        // 3) render into Bitmap
        using var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        IntPtr hdc = g.GetHdc();
        PrintWindow(hwnd, hdc, 0);
        g.ReleaseHdc(hdc);

        // 4) encode to PNG in-memory
        var ms = new MemoryStream();
        bmp.Save(ms, ImageFormat.Png);
        ms.Position = 0;

        // 5) wrap in MAUI ImageSource
        return ImageSource.FromStream(() => ms);
    }
    public static IntPtr DimmerHandle { get; set; }
    public static bool IsAppInForeground { get; set; }
    public static AppWindowPresenter? AppWinPresenter { get; set; }
    public static OverlappedPresenter? OverLappedPres { get; set; }

    public static class ShowCloseConfirmationPopUp
    {
        const bool showCloseConfirmation = false;
        public static bool ShowCloseConfirmation
        {
            get => Preferences.Default.Get(nameof(ShowCloseConfirmation), showCloseConfirmation);
            set => Preferences.Default.Set(nameof(ShowCloseConfirmation), value);
        }
        public static void ToggleCloseConfirmation(bool showClose)
        {
            ShowCloseConfirmation = showClose;
        }
        public static bool GetCloseConfirmation()
        {
            return ShowCloseConfirmation;
        }
    }
    // Method to set the window on top
    public static void ToggleWindowAlwaysOnTop(bool topMost, AppWindowPresenter? appPresenter)
    {
        try
        {

            OverLappedPres = appPresenter as OverlappedPresenter;
            if (topMost)
            {
                OverLappedPres!.IsAlwaysOnTop = true;
            }
            else
            {
                OverLappedPres!.IsAlwaysOnTop = false;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"{ex.Message}");
        }
    }

    public static void OpenAlbumWindow(SongModelView song)
    {
        var MyVM = IPlatformApplication.Current!.Services.GetService<HomeViewModel>()!;
        var mapper = IPlatformApplication.Current!.Services.GetService<IMapper>()!;
       
        AlbumWindow newWindow = new AlbumWindow( MyVM, mapper);

        Application.Current!.OpenWindow(newWindow);

        //MyVM.AlbumsMgtFlow.GetAlbumsByArtistName(song.ArtistName!);

    }


    public static void OpenArtistWindow(SongModelView song)
    {
        var mapper = IPlatformApplication.Current!.Services.GetService<IMapper>()!;
        var MyVM = IPlatformApplication.Current!.Services.GetService<HomeViewModel>()!;
        //MyViewModel.AlbumsMgtFlow.GetAlbumsBySongId(song.Id);

        ArtistWindow newWindow = new ArtistWindow( MyVM, mapper);
        newWindow.SetTitle(song);
        //newWindow.
        Application.Current!.OpenWindow(newWindow);

        //MyVM.AlbumsMgtFlow.GetAlbumsByArtistName(song.ArtistName!);

    }


    public static void OpenSettingsWindow()
    {
        var MyVM = IPlatformApplication.Current!.Services.GetService<HomeViewModel>();
        //MyViewModel.AlbumsMgtFlow.GetAlbumsBySongId(song.Id);
        

        SettingsWindow newWindow = new(MyVM);
        
        //newWindow.SetTitle(song);
        Application.Current!.OpenWindow(newWindow);

        //MyVM.AlbumsMgtFlow.GetAlbumsBySongId(song.Id!);

    }


    public static void MiniMimizeWindow(Window win)
    {
        
        var nativeWindow = win.Handler.PlatformView;
        IntPtr windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(nativeWindow);
        ShowWindow(windowHandle, SW_HIDE);
        //System.Windows.SystemCommands.MinimizeWindow(win);
    }
    public static void ToggleFullScreenMode(bool IsToFullScreen, AppWindowPresenter appPresenter)
    {
        try
        {
             OverLappedPres = appPresenter as OverlappedPresenter;
            if (IsToFullScreen)
            {
                OverLappedPres!.IsAlwaysOnTop = true;
                OverLappedPres.SetBorderAndTitleBar(false, false);
                OverLappedPres!.Maximize();
                
            }
            else
            {
                OverLappedPres!.IsAlwaysOnTop = false;
                OverLappedPres.SetBorderAndTitleBar(true, true);
                OverLappedPres!.Restore();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"{ex.Message}");
        }
    }
    public static bool DeleteSongFile(SongModelView song)
    {
        try
        {
            if (File.Exists(song.FilePath))
            {
                FileSystem.DeleteFile(song.FilePath, UIOption.AllDialogs, RecycleOption.SendToRecycleBin);

            }
            return true;
        }
        catch (UnauthorizedAccessException e)
        {
            Debug.WriteLine("Unauthorized to delete file: " + e.Message);
            return false;
        }
        catch (IOException e)
        {
            Debug.WriteLine("An IO exception occurred: " + e.Message);
            return false;
        }
        catch (Exception e)
        {
            Debug.WriteLine("An error occurred: " + e.Message);
            return false;
        }
    }

    public static bool MultiDeleteSongFiles(ObservableCollection<SongModelView> songs)
    {
        try
        {
            foreach (var song in songs.Where(song => File.Exists(song.FilePath)))
            {
                FileSystem.DeleteFile(song.FilePath, UIOption.AllDialogs, RecycleOption.SendToRecycleBin);
            }

            return true;
        }
        catch (UnauthorizedAccessException e)
        {
            Debug.WriteLine("Unauthorized to delete file: " + e.Message);
            return false;
        }
        catch (IOException e)
        {
            Debug.WriteLine("An IO exception occurred: " + e.Message);
            return false;
        }
        catch (Exception e)
        {
            Debug.WriteLine("An error occurred: " + e.Message);
            return false;
        }
    }
    // Helper to retrieve a valid window handle from your main window
    public static IntPtr GetWindowHandle()
    {
        var window = IPlatformApplication.Current!.Services.GetService<DimmerWin>()!;
        
        // Get the underlying native window (WinUI).
        var nativeWindow = window.Handler?.PlatformView as Microsoft.UI.Xaml.Window??throw new InvalidOperationException("Unable to retrieve the native window.");


        DimmerHandle = WindowNative.GetWindowHandle(nativeWindow);
        return DimmerHandle;
    }
    public static IntPtr GetAnyWindowHandle(Window window)
    {
        
        if (window == null)
            throw new ArgumentNullException(nameof(window));
        // Get the underlying native window (WinUI).
        var nativeWindow = window.Handler?.PlatformView as Microsoft.UI.Xaml.Window??throw new InvalidOperationException("Unable to retrieve the native window.");

        var intPtrHandle = WindowNative.GetWindowHandle(nativeWindow);
        
        return intPtrHandle;
    }

    [DllImport("user32.dll")]
#pragma warning disable S4200 // Native methods should be wrapped
#pragma warning disable SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time
#pragma warning disable CA1401 // P/Invokes should not be visible
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
#pragma warning restore CA1401 // P/Invokes should not be visible
#pragma warning restore SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time
#pragma warning restore S4200 // Native methods should be wrapped

    public const int SW_HIDE = 0;
    public const int SW_RESTORE = 9;
    public static class ApplicationProps
    {
        public static DisplayArea? DisplayArea { get; set; }

        public static void LaunchSecondWindow()
        {
            HomeViewModel? vm = IPlatformApplication.Current!.Services.GetService<HomeViewModel>()!;

            var window = new TestPage(vm);
            window.Activate();
        }
        
        public async static Task LaunchNotificationWindowAndFadeItAwayAfterSixSeconds(BaseViewModelWin vm)
        {

            SongNotifierWindow newNotif = new SongNotifierWindow(vm);
        
            newNotif.Height = 300;
            newNotif.Width = AppUtils.UserScreenWidth;

            Application.Current?.OpenWindow(newNotif);

            
            await Task.Delay(6000);


            Application.Current?.CloseWindow(newNotif);

            
        }

    }


}
