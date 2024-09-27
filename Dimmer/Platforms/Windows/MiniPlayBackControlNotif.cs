
namespace Dimmer_MAUI.UtilitiesServices;
public static class MiniPlayBackControlNotif
{      
    public static void ShowUpdateMiniView(SongsModelView song)
    {
        try
        {

            var miniPlayerWindow = Application.Current?.Windows.FirstOrDefault(window => window.Page is MiniControlNotificationView)?.Page as MiniControlNotificationView;
            if (miniPlayerWindow is not null)
            {
                miniPlayerWindow.Update(song.Title, song.ArtistName, song.CoverImagePath);
                return;
            }
            var miniPlayerView = new MiniControlNotificationView(song.Title, song.ArtistName, song.CoverImagePath);

            // Ensure this is on the main thread
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var secondWindow = new Window(miniPlayerView);
                var mainScreenBounds = DeviceDisplay.MainDisplayInfo;

                secondWindow.Title = "MP";
                secondWindow.MaximumHeight = 150;
                secondWindow.MinimumHeight = 150;
                secondWindow.MaximumWidth = 400;
                secondWindow.MinimumWidth = 400;
                secondWindow.X = mainScreenBounds.Width - 400;
                secondWindow.Y = 0;

                Application.Current?.OpenWindow(secondWindow);
            });

        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }
    public static void BringAppToFront()
    {
        //var mainWindow = Application.Current?.Windows[]FirstOrDefault(window => window.Page is AppShell);
        var mainWindow = Application.Current!.Windows[0]! as Window;
        if (mainWindow.Handler is not null)
        {
            var pv = mainWindow.Handler.PlatformView;
            if (pv is MauiWinUIWindow winUIWin)
            {
                winUIWin.Activate();
            }
        }
    }
}

