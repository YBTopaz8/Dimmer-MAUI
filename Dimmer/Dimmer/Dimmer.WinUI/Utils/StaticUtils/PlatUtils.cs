using System.Linq;
using WinRT.Interop;

namespace Dimmer.WinUI.Utils.StaticUtils;
public static class PlatUtils
{

    public static IntPtr DimmerHandle { get; set; }
    public static nint DimmerHandleNInt { get; set; }
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

            var OverLappedPres = appPresenter as OverlappedPresenter;
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

    public static void ToggleFullScreenMode(bool IsToFullScreen, AppWindowPresenter appPresenter)
    {
        try
        {
            var OverLappedPres = appPresenter as OverlappedPresenter;
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
        var window = Application.Current.Windows[0];
        if (window == null)
            throw new ArgumentNullException(nameof(window));
        // Get the underlying native window (WinUI).
        var nativeWindow = window.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
        if (nativeWindow == null)
            throw new InvalidOperationException("Unable to retrieve the native window.");

        DimmerHandleNInt = WindowNative.GetWindowHandle(nativeWindow);
        DimmerHandle = WindowNative.GetWindowHandle(nativeWindow);
        return DimmerHandle;
    }

    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    public const int SW_HIDE = 0;
    public const int SW_RESTORE = 9;
}

