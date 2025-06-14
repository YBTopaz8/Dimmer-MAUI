using Android.App;
using Android.Content;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Service.QuickSettings;
using Android.Util;
using Android.Widget;

using AndroidX.Media3.Session;

namespace Dimmer.DimmerAudio;
[Service(Name = "com.yvanbrunel.dimmer.QuickSettingsTileService", // <<< CHANGE TO YOUR UNIQUE NAME
           Permission = Android.Manifest.Permission.BindQuickSettingsTile,
           Label = "@string/qs_tile_label", // Define in strings.xml
           Icon = "@drawable/ic_qs_tile_icon", // Create this drawable icon
           Exported = true)] // Must be exported
[IntentFilter(new[] { ActionQsTile })] // Listen for tile actions
public class QuickSettingsTileService : TileService
{
    private const string TAG = "QuickSettingsTile";
    public const string ActionTogglePlayback = "com.yvanbrunel.dimmer.ACTION_TOGGLE_PLAYBACK"; // Action for Service
    public const string ActionRequestUpdate = "com.yvanbrunel.dimmer.ACTION_REQUEST_TILE_UPDATE"; // Action for TileService

    // --- Tile Lifecycle Methods ---
    public override void OnTileAdded()
    {
        base.OnTileAdded();
        Log.Debug(TAG, "OnTileAdded");
        // Optional: Perform one-time setup if needed
        RequestUpdate(); // Request initial state update
        if (QsTile is not null)
        {
            MyTile = QsTile;
        }
    }

    public override void OnTileRemoved()
    {
        base.OnTileRemoved();
        Log.Debug(TAG, "OnTileRemoved");
        // Optional: Clean up resources if needed

    }

    public override void OnStartListening()
    {
        base.OnStartListening();
        Log.Debug(TAG, "OnStartListening");
        // Update the tile state based on the current app state
        RequestUpdate();
        if (QsTile is not null)
        {
            MyTile = QsTile;
        }
    }
    private static Tile? MyTile { get; set; }
    public override void OnStopListening()
    {
        base.OnStopListening();
        Log.Debug(TAG, "OnStopListening");
        // Optional: Pause expensive updates if any
    }

    private void SendCommandToMediaService(string action)
    {
        try
        {
            Intent serviceIntent = new Intent(this, typeof(ExoPlayerService)); // <<< YOUR MEDIA SERVICE
            serviceIntent.SetAction(action);
            StartService(serviceIntent); // Or StartForegroundService if appropriate for your media service's needs
            Log.Debug(TAG, $"Sent command to MediaService: {action}");
        }
        catch (Exception ex)
        {
            Log.Error(TAG, $"Error sending command '{action}': {ex.Message}");
            Toast.MakeText(this, $"Error: {action.Split('.').LastOrDefault()}", ToastLength.Short)?.Show();
        }
    }

    private void LaunchMainActivity()
    {
        Intent mainActivityIntent = new Intent(this, typeof(MainActivity)); // <<< YOUR MAIN ACTIVITY
        mainActivityIntent.AddFlags(ActivityFlags.NewTask | ActivityFlags.ClearTop);
        try
        {
            // For API 34+, TileService has StartActivityAndCollapse(PendingIntent)
            // For older, ShowDialog collapses. If not using dialog, StartActivity might leave panel open.
            // A common way is to send a PendingIntent that the system can use.
            var pendingIntent = PendingIntent.GetActivity(this, 0, mainActivityIntent, PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);

            if (Build.VERSION.SdkInt >= BuildVersionCodes.UpsideDownCake) // API 34
            {
                StartActivityAndCollapse(pendingIntent);
            }
            else
            {
                // For older versions, a direct StartActivity after a dialog usually works fine as ShowDialog collapses the panel.
                // If OnClick directly launches activity without dialog, panel might stay open on some OEM skins/versions.
                StartActivity(mainActivityIntent);
                // To be more robust in collapsing the panel on older versions if not using ShowDialog:
                // SendBroadcast(new Intent(Intent.ActionCloseSystemDialogs)); // This is a bit of a hack
            }
            Log.Debug(TAG, "Launched MainActivity");
        }
        catch (Exception ex)
        {
            Log.Error(TAG, $"Error launching MainActivity: {ex.Message}");
        }
    }

    // --- State Update Logic ---


    private static string TruncateString(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;
        return value.Length <= maxLength ? value : value.Substring(0, maxLength - 3) + "...";
    }

    public override void OnClick()
    {
        base.OnClick();
        Log.Debug(TAG, "OnClick");
        ShowPlaybackOptionsDialog();
        // --- Send Command to your MediaSessionService ---

    }


    private void ShowPlaybackOptionsDialog()
    {
        // Check if the device is locked - Dialogs might not show properly over lock screen
        // Depending on what the dialog does, you might need unlockAndRun
        if (IsLocked)
        {
            Log.Warn(TAG, "Device is locked, dialog may not display correctly. Consider unlockAndRun.");
            // Example using unlockAndRun (if the dialog action needs security)
            UnlockAndRun(new Java.Lang.Runnable(() =>
            {
                // Code to show dialog *after* unlocking
                BuildAndShowDialog();
            }));
            return; // Don't show dialog directly if locked and needs unlock

            // Or just inform the user if the action is safe but dialog won't show well
            Toast.MakeText(this, "Unlock device to interact", ToastLength.Short)?.Show();
            return; // Don't proceed if locked and dialog is the primary action
        }

        BuildAndShowDialog();

    }

    private void BuildAndShowDialog()
    {
        AlertDialog.Builder builder = new AlertDialog.Builder(this); // Use 'this' (Service context)

        builder.SetTitle("Playback Options"); // Set a title

        // Add Dialog CurrentItems (Example: Play/Pause, Next, Open App)
        string[] items = { "Toggle Play/Pause", "Next Track", "Open Dimmer App" };
        builder.SetItems(items, (sender, args) =>
        {
            // Handle item clicks within the dialog
            switch (args.Which)
            {
                case 0: // Toggle Play/Pause
                    Log.Debug(TAG, "Dialog: Toggle Play/Pause selected");
                    SendToggleCommandToService(); // Implement this method to handle the toggle action
                    break;
                case 1: // Next Track
                    Log.Debug(TAG, "Dialog: Next Track selected");
                    NotificationHelper.ShowPlaybackBubble(this, "YBbbb");
                    SendNextCommandToService(); // <<< Implement this action in your service
                    break;
                case 2: // Open App
                    Log.Debug(TAG, "Dialog: Open App selected");
                    LaunchMainActivity();
                    break;
            }
            // Dialog dismisses automatically on item click by default
        });

        // Optional: Add Negative Button (Cancel/Dismiss)
        builder.SetNegativeButton("Cancel", (sender, args) =>
        {
            Log.Debug(TAG, "Dialog: Cancel clicked");
            // Dialog dismisses automatically
        });

        // Create the dialog
        Dialog dialog = builder.Create();

        // --- Show the Dialog ---
        // This method handles collapsing the QS panel
        try
        {
            ShowDialog(dialog);
            Log.Debug(TAG, "Dialog shown via ShowDialog()");
        }
        catch (Exception ex)
        {
            Log.Error(TAG, $"Error showing dialog: {ex.Message}");
            // Fallback or logging
        }
    }

    // Helper method to send toggle command
    private void SendToggleCommandToService()
    {
        try
        {
            Intent activityIntent = new Intent(this, typeof(MainActivity));
            activityIntent.AddFlags(ActivityFlags.NewTask);
            activityIntent.SetAction(ActionTogglePlayback);
            StartActivity(activityIntent);
            //StartService(serviceIntent);
            Log.Debug(TAG, $"Sent intent with action: {ActionTogglePlayback}");
            // Maybe request an update after a short delay?
        }
        catch (Exception ex)
        {
            Log.Error(TAG, $"Error sending toggle command: {ex.Message}");
        }
    }

    // Helper method to send "Next" command (Implement the action string)
    private void SendNextCommandToService()
    {
        try
        {
            // Define this action string and handle it in your MediaSessionService
            const string ActionNextTrack = "com.yvanbrunel.dimmer.ACTION_NEXT_TRACK";
            Intent serviceIntent = new Intent(this, typeof(MediaSessionService)); // <<< YOUR MEDIA SERVICE
            serviceIntent.SetAction(ActionNextTrack);
            StartService(serviceIntent);
            Log.Debug(TAG, $"Sent intent with action: {ActionNextTrack}");
        }
        catch (Exception ex)
        {
            Log.Error(TAG, $"Error sending next command: {ex.Message}");
        }
    }




    // --- State Update Logic ---

    // Call this static method from your MediaSessionService when playback state changes
    public static void RequestTileUpdate(Context context)
    {
        Log.Debug(TAG, "Static RequestTileUpdate called.");
        if (Build.VERSION.SdkInt >= BuildVersionCodes.N) // RequestListeningState requires API 24
        {
            // This asks the system to bind to the service and call OnStartListening soon
            RequestListeningState(context, new ComponentName(context, Java.Lang.Class.FromType(typeof(QuickSettingsTileService))));
        }
        else
        {
            // Fallback for older versions (less reliable): Send a broadcast maybe?
            // Or rely on OnStartListening when panel is opened.
        }
    }

    // Method to get the current state and update the tile UI
    private void RequestUpdate()
    {
        // --- Method 1: Check simple state (e.g., from SharedPreferences) ---
        // This is only suitable if your service reliably updates a simple flag.
        // bool isPlaying = GetPlaybackStateFromPrefs();
        // UpdateTileVisualState(isPlaying);

        // --- Method 2 (Better): Query your MediaSessionService ---
        // This is more complex as the TileService lifecycle is short.
        // Binding is often too slow. Sending an Intent and waiting for a reply
        // via Broadcast is possible but adds significant complexity.
        // The RECOMMENDED way is for the MediaSessionService to proactively
        // call QuickSettingsTileService.RequestTileUpdate(context) when its state changes.
        // So, OnStartListening mostly relies on the *last known state* or waits for the service push.

        // For simplicity here, we'll assume a helper method exists or use a placeholder
        bool isPlaying = IsCurrentlyPlaying(); // Placeholder - Implement this!
        UpdateTileVisualState(isPlaying);
    }

    // Updates the visual appearance of the tile
    public void UpdateTileVisualState(bool isPlaying, SongModelView? song = null)
    {
        Tile? tile = MyTile; // Tile property from base class
        if (tile == null || song is null)
        {
            Log.Warn(TAG, "Tile object is null in UpdateTileVisualState.");
            return;
        }

        try
        {
            Icon? newIcon;
            string newLabel;

            if (isPlaying)
            {

                newIcon = Icon.CreateWithResource(Platform.AppContext, Resource.Drawable.atom); // <<< CREATE THIS ICON
            }
            else
            {
                newIcon = Icon.CreateWithResource(Platform.AppContext, Resource.Drawable.atom); // <<< CREATE THIS ICON

            }
            tile.Subtitle = song?.ArtistName ?? "Unknown Artist"; // Set subtitle to song title
            tile.Icon = newIcon;
            tile.Label = song?.Title ?? "Unknown Title";
            tile.ContentDescription = "Dimmer";

            tile.UpdateTile(); // Apply the changes
        }
        catch (Exception ex)
        {
            Log.Error(TAG, $"Error updating tile visuals: {ex.Message}");
        }
    }

    // --- Placeholder for getting playback state ---
    // Replace this with actual logic to check your service's state.
    // This might involve reading SharedPreferences set by the service,
    // or ideally, just reflecting the state passed when the service requested an update.
    private bool IsCurrentlyPlaying()
    {
        var vm = IPlatformApplication.Current.Services.GetService<AudioService>();
        if (vm != null)
        {
            //return vm.IsPlaying; // Use the ExoPlayer's IsPlaying property
        }
        Log.Warn(TAG, "IsCurrentlyPlaying() using placeholder value (false). Implement actual state check!");
        return false; // Default/placeholder
    }
}