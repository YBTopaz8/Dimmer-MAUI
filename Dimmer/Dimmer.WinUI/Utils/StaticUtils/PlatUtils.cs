
using System.Drawing.Imaging;

using Dimmer.WinUI.Utils.WinMgt;
using Dimmer.WinUI.Views.WinUIPages;

using Microsoft.Maui.Platform;
using Microsoft.UI.Xaml.Media;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;

using Windows.Graphics;
using Windows.Graphics.Display;

using FlyoutBase = Microsoft.UI.Xaml.Controls.Primitives.FlyoutBase;
using ImageFormat = System.Drawing.Imaging.ImageFormat;
using ImageSource = Microsoft.Maui.Controls.ImageSource;

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




    public static void MiniMimizeWindow(Window win)
    {

        var nativeWindow = win.Handler.PlatformView;
        IntPtr windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(nativeWindow);
        ShowWindow(windowHandle, SW_HIDE);
        //System.Windows.SystemCommands.MinimizeWindow(win);
    }
    public static void ToggleFullScreenMode(bool IsToFullScreen, AppWindowPresenter? appPresenter)
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

    public static void OpenAndSetWindowToEdgePosition( Window concernedWindow, RectInt32 positionToSet)
    {
        var nativeWindow = GetNativeWindow(concernedWindow);
        if (nativeWindow is null) return;
        nativeWindow.AppWindow.IsShownInSwitchers = false;
        var sysBackDrop = new MicaBackdrop();
        nativeWindow.SystemBackdrop = sysBackDrop;
        var pres = nativeWindow.AppWindow.Presenter;
        //window.SetTitleBar()
        var p = pres as OverlappedPresenter;
        if (p != null)
        {
            p.SetBorderAndTitleBar(false, false);
            p.IsResizable = false;
            p.IsAlwaysOnTop = true;
            
        }

        
        nativeWindow.Activate();
        nativeWindow.AppWindow.MoveAndResize(positionToSet);

        nativeWindow.AppWindow.MoveInZOrderAtTop();

    }

    public static void MoveAndResizeWindow(this Window concernedWindow, RectInt32 positionToSet)
    {
        var nativeWindow = GetNativeWindow(concernedWindow);

        //var disInfo= DisplayInformation.GetForCurrentView();

        var width = DisplayArea.Primary.WorkArea.Width;
        var height = DisplayArea.Primary.WorkArea.Height;

        //var width = DisplayArea.Primary.WorkArea.Width;
        //var height = DisplayArea.Primary.WorkArea.Height;
        //var x = DisplayArea.Primary.WorkArea.X;
        //var y = DisplayArea.Primary.WorkArea.Y;
        //AppWindow.MoveAndResize(new Windows.Graphics.RectInt32
        //{
        //    Height = height,
        //    Width = 340,
        //    X = x,
        //    Y = y
        //});

        // move to left x - (width - 400)
        // move to right x + (width - 400)

        //move to top y - (height - 400)
        //move to top y + (height - 400)

        nativeWindow.AppWindow.MoveAndResize(positionToSet);

        nativeWindow.AppWindow.MoveInZOrderAtTop();
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

       
        DimmerHandle = WindowNative.GetWindowHandle(window);
        return DimmerHandle;
    }

    public static async Task EnsureWindowReadyAsync(Window? MauiWindow = null)
    {
        if (MauiWindow == null)
        {       // Ensure there’s at least one window created by MAUI
            MauiWindow = Application.Current?.Windows.FirstOrDefault();
        }
        int attempts = 0;
        while ((MauiWindow?.Handler == null) && attempts++ < 20)
        {
            await Task.Delay(100);
            MauiWindow = Application.Current?.Windows.FirstOrDefault();
        }

        if (MauiWindow?.Handler == null)
            throw new InvalidOperationException("Window handler was not ready after waiting.");
    }
    public static Microsoft.UI.Xaml.Window GetNativeWindow(Window? MauiWindow = null)
    {
        if (MauiWindow == null)
        {       // Ensure there’s at least one window created by MAUI
             MauiWindow = Application.Current?.Windows.FirstOrDefault();
        }
        if (MauiWindow == null)
            throw new InvalidOperationException("No MAUI window available yet.");

       

        var nativeWindow = MauiWindow.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
        if (nativeWindow == null)
            throw new InvalidOperationException("Unable to retrieve native window.");

        return nativeWindow;
    }
 
    public static Microsoft.UI.Xaml.Controls.Frame? GetNativeFrame(this IElement element)
    {
        if (element.Handler?.PlatformView is Microsoft.UI.Xaml.FrameworkElement fe)
        {
            Microsoft.UI.Xaml.DependencyObject? parent = fe;
            while (parent != null)
            {
                if (parent is Microsoft.UI.Xaml.Controls.Frame frame)
                    return frame;

                parent = VisualTreeHelper.GetParent(parent);
            }
        }
        return null;
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

    public static void OpenAllSongsWindow(BaseViewModelWin vm)
    {
        var winMgr = IPlatformApplication.Current!.Services.GetService<IWinUIWindowMgrService>()!;

        AllSongsWindow? win = winMgr.GetOrCreateUniqueWindow(vm, windowFactory: () => new AllSongsWindow(vm));
        
        // move and resize to the center of the screen
        
        var pres = win?.AppWindow.Presenter;
        
        //window.SetTitleBar()
        if (pres is OverlappedPresenter p)
        {
            //p.PreferredMaximumHeight = 1200;
            //p.PreferredMaximumWidth = 720;
            //p.PreferredMinimumWidth = 600;
            //p.PreferredMinimumHeight = 800;
            p.IsResizable = true;
            p.SetBorderAndTitleBar(true, true); // Remove title bar and border
            p.IsAlwaysOnTop = false;
            
        }

    }

    public static async Task ShowNewSongNotification(string songTitle, string artistName, string albumArtPath)
    {
        try
        {

            var notificationBuilder = new AppNotificationBuilder()
                .AddArgument("action", "viewSong")
                //.AddArgument("songId", "12345")
                .SetAppLogoOverride(new Uri(albumArtPath), AppNotificationImageCrop.Circle, songTitle)
                .AddText("Now Playing...", new AppNotificationTextProperties().SetMaxLines(2))
                .AddText(songTitle, new AppNotificationTextProperties().SetMaxLines(2))
                
                .AddText(artistName, new AppNotificationTextProperties().SetMaxLines(2));
                
            //.AddButton(new AppNotificationButton("View Song Details").AddArgument("action", "play"))
            //.AddButton(new AppNotificationButton("Queue").AddArgument("action", "queue")


            await AppNotificationManager.Default.RemoveAllAsync();
            var notif = notificationBuilder.BuildNotification();
            
            AppNotificationManager.Default.Show(notif);
        } catch(Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }

    public static void ShowContextMenu(this Element element)
    {
        // Get the MenuFlyout attached to the MAUI element
        var menuFlyout = Microsoft.Maui.Controls.FlyoutBase.GetContextFlyout(element);
        if (menuFlyout == null)
        {
            // No context menu to show
            return;
        }

        // Use platform-specific code to trigger the flyout
        var platformView = element.Handler?.PlatformView as Microsoft.UI.Xaml.FrameworkElement;
        if (platformView != null)
        {
            var flyoutMenu = FlyoutBase.GetAttachedFlyout(platformView);
            if (flyoutMenu is null) return;
            
            // The native way to show a context flyout on WinUI
            FlyoutBase.ShowAttachedFlyout(platformView);
        }
    }
}
