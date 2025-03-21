

using CommunityToolkit.Maui.Extensions;

namespace Dimmer_MAUI.Utilities.OtherUtils;
public static class GeneralStaticUtilities
{
   

#if WINDOWS
    public static void UpdateTaskBarProgress(double progress)
    {
        return;
        //// wait for now.
        //int maxProgressbarValue = 100;
        //var taskbarInstance = Microsoft.WindowsAPICodePack.Taskbar.TaskbarManager.Instance;
        //taskbarInstance.SetProgressState(Microsoft.WindowsAPICodePack.Taskbar.TaskbarProgressBarState.Normal);
        //taskbarInstance.SetProgressValue((int)progress, maxProgressbarValue);

        //if (progress >= maxProgressbarValue)
        //{
        //    taskbarInstance.SetProgressState(Microsoft.WindowsAPICodePack.Taskbar.TaskbarProgressBarState.NoProgress);
        //}

    }
#endif


    public static void RunFireAndForget(Task task, Action<Exception>? onException = null)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await task;
            }
            catch (Exception ex)
            {
                onException?.Invoke(ex);
            }
        });
    }

    public static void ClearUp()
    {
        var DimmerAudioService = IPlatformApplication.Current!.Services.GetService<IDimmerAudioService>() as DimmerAudioService;
        DimmerAudioService?.Dispose();
    }
    public static void ShowNotificationInternally(string msgText, int delayBtnSwitch = 500, Label? text = null, SearchBar? bar = null, TitleBar? titleBar = null)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {

            if (text is null || bar is null || titleBar is null)
                return;

            text.Text = msgText;
            text.Opacity = 1; // Ensure the label is fully opaque initially.
            text.IsVisible = true; // Ensure label is visible
            bar.Opacity = 0.01;  // Make Search Bar almost invisible
            bar.IsVisible = false; // Ensure Search Bar is hidden initially.

            // Fade out the search bar completely and hide it.
            await titleBar.BackgroundColorTo(Color.FromArgb("#483D8B"), length: 500);
            await bar.DimmOutCompletelyAndHide();


            // Fade in the notification label and show it.
            await text.DimmInCompletelyAndShow();

            // Wait for the specified delay.
            await Task.Delay(delayBtnSwitch);

            // Fade out the notification label and hide.
            await text.DimmOutCompletelyAndHide();

            // Fade in the search bar and show it.
            await bar.DimmInCompletelyAndShow();

#if DEBUG

            await titleBar.BackgroundColorTo(Color.FromArgb("#483D8B"), length: 500);
#elif RELEASE
        await titleBar.BackgroundColorTo(Colors.Black, length: 500);
#endif
        });
    }
}


public static class UserActivityLogger
{
    public static async Task LogUserActivity(
        ParseUser sender,        
        PlayType activityType, 
        ParseUser? recipient = null,
        Message? chatMessage = null,
        SharedPlaylist? sharedPlaylist = null,
        SongModelView? nowPlaying = null,
        bool? isRead = null,
        Dictionary<string, object>? additionalData = null,
        ChatRoom? chatRoomm =null,
        ParseUser? CurrentUserOnline=null)
    {
        // --- Input Validation (Crucial for Robustness) ---
        
        if (sender == null)
        {
            throw new ArgumentNullException(nameof(sender), "Sender cannot be null.");
        }
        
        
        if (CurrentUserOnline is null)
        {
            return;
        }
        // --- Create the UserActivity Object ---

        try
        {
            

            UserActivity newActivity = new UserActivity();

            // --- Set Core Fields ---

            newActivity["sender"] = sender;
            newActivity["activityType"] = (int)activityType;          
            newActivity["devicePlatform"] = DeviceInfo.Current.Platform.ToString();
            newActivity["deviceIdiom"] = DeviceInfo.Current.Idiom.ToString();
            newActivity["deviceVersion"] = DeviceInfo.Current.VersionString;

            // --- Set Optional Related Objects (Pointers) ---
            // Use null-conditional operator and null-coalescing operator for brevity and safety.

            newActivity["chatMessage"] = chatMessage ?? null; //if chatMessage is not null, set newActivity["chatMessage"] = chatMessage , else set to null.
            

            // --- Set isRead (if provided) ---

            if (isRead.HasValue)
            {
                newActivity["isRead"] = isRead.Value;
            }

            // --- Add Additional Data (Flexibility) ---

            if (additionalData != null)
            {
                foreach (var kvp in additionalData)
                {
                    newActivity[kvp.Key] = kvp.Value;
                }
            }

            // --- Save the UserActivity ---

            await newActivity.SaveAsync(); //thrown on this line
        }
        catch (Exception ex)
        {
            // CRITICAL:  Handle errors!  In a real app, you'd log this properly.
            Console.WriteLine($"Error logging user activity: {ex.Message}");
            // Consider re-throwing the exception, or returning a result indicating failure,
            // depending on how you want to handle errors in the calling code.
            throw; // Re-throw for now, so the caller knows it failed.
        }
    }

    
}