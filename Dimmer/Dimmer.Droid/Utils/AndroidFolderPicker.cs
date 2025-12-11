using Application = Android.App.Application;
namespace Dimmer.Utils;


public class AndroidFolderPicker
{
    private TaskCompletionSource<string?>? _tcs;
    public const int PICK_FOLDER_REQUEST_CODE = 9999;

    public async Task<string?> PickFolderAsync()
    {
        var activity = MainApplication.CurrentActivity;
        if (activity == null)
        {
            Console.WriteLine("AndroidFolderPicker: No Current Activity found.");
            return null;
        }

        _tcs = new TaskCompletionSource<string?>();

        try
        {
            // 1. Create the Native Android Intent
            var intent = new Intent(Intent.ActionOpenDocumentTree);
            intent.AddFlags(ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantPersistableUriPermission);

            // 2. Launch it
            activity.StartActivityForResult(intent, PICK_FOLDER_REQUEST_CODE);

            // 3. Wait for OnActivityResult to set the result
            return await _tcs.Task;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Folder Picker Error: {ex.Message}");
            _tcs.TrySetResult(null);
            return null;
        }
    }

    // Call this from your Activity's OnActivityResult
    public void OnResult(int resultCode, Intent? data)
    {
        if (resultCode == (int)Android.App.Result.Ok && data?.Data != null)
        {
            var uri = data.Data;

            // CRITICAL: Persist permission so you can access this folder after app restart
            var takeFlags = data.Flags & (ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantWriteUriPermission);
            Application.Context.ContentResolver?.TakePersistableUriPermission(uri, takeFlags);

            // Convert content:// URI to a displayable path
            string path = GetPathFromUri(uri);
            _tcs?.TrySetResult(path);
        }
        else
        {
            _tcs?.TrySetResult(null); // User cancelled
        }
    }

    private string GetPathFromUri(Android.Net.Uri uri)
    {
        // This is a helper to try and make the path look like a file system path.
        // Note: In modern Android, you usually work with the Stream/URI directly, 
        // but this helps for display purposes.

        string path = uri.Path ?? "";
        if (path.Contains(":"))
        {
            string id = path.Split(":")[1];
            // Returns generic path structure like /storage/emulated/0/Music
            return Android.OS.Environment.ExternalStorageDirectory + "/" + id;
        }
        return path;
    }
}