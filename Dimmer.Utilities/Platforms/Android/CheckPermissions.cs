using Android;
using Android.Content.PM;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using static Android.Preferences.PreferenceManager;

namespace Dimmer.Utilities;

// All the code in this file is only included on Android.
public class CheckPermissions
{
    public static async Task<bool> CheckAndRequestStoragePermissionAsync()
    {
        // Check if permission is already granted
        var status = await Permissions.CheckStatusAsync<Permissions.StorageRead>();

        if (status == PermissionStatus.Granted)
            return true;

        // Explain to the user why the permission is needed
        if (status == PermissionStatus.Denied && DeviceInfo.Platform == DevicePlatform.Android)
        {
            bool isRationaleAccepted = await Shell.Current.DisplayAlert("Permission Request",
                "Please grant storage permissions to access files.",
                "OK", "Cancel");

            if (isRationaleAccepted)
            {
                //AppInfo.ShowSettingsUI();

                // Request permission
                status = await Permissions.RequestAsync<Permissions.StorageRead>();

                // Return true if the permission was granted
                Debug.WriteLine(status);
                return true;

            }
                return false;
        }

        // Request permission
        status = await Permissions.RequestAsync<Permissions.StorageRead>();

        // Return true if the permission was granted
        return status == PermissionStatus.Granted;
    }
}
