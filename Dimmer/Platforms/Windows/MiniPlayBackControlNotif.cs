namespace Dimmer_MAUI.UtilitiesServices;
public static class MiniPlayBackControlNotif
{
    public static void ShowUpdateMiniView(SongModelView song)
    {
        try
        {
            // Ensure everything that manipulates the UI is on the main thread
            MainThread.BeginInvokeOnMainThread(() =>
            {
                // Query main display info inside the main thread block
                var mainScreenBounds = DeviceDisplay.Current.MainDisplayInfo;
                var miniPlayerWindow = Application.Current?.Windows.FirstOrDefault(window => window.Page is MiniControlNotificationView)?.Page as MiniControlNotificationView;

                if (miniPlayerWindow is not null)
                {
                    // Update the mini player if it's already open
                    miniPlayerWindow.Update(song.Title, song.ArtistName, song.CoverImagePath);
                    return;
                }

                // Create and show the new mini player window
                var miniPlayerView = new MiniControlNotificationView(song.Title, song.ArtistName, song.CoverImagePath);
                var secondWindow = new Window(miniPlayerView)
                {
                    Title = "MP",
                    MaximumHeight = 150,
                    MinimumHeight = 150,
                    MaximumWidth = 400,
                    MinimumWidth = 400,
                    X = mainScreenBounds.Width - 400,
                    Y = 0
                };

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
        var mainWindow = Application.Current!.Windows[0]!;
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

