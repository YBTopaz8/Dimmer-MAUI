using Android;
using AndroidX.Core.Content;
using AndroidX.Core.App;
using Application = Android.App.Application;

namespace Dimmer.NativeServices;

public static class AndroidPermissionsService
{
    // 1. Define which permissions we need based on Android Version
    public static string[] GetRequiredAudioPermissions()
    {
        if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu) // Android 13+ (API 33)
        {
            return new[]
            {
                Manifest.Permission.ReadMediaAudio,
                Manifest.Permission.BluetoothScan,
                Manifest.Permission.BluetoothConnect,
                Manifest.Permission.Bluetooth,
                

                Manifest.Permission.PostNotifications // Required for Foreground Service notification
            };
        }
        else if (Build.VERSION.SdkInt >= BuildVersionCodes.R) // Android 11/12
        {
            return new[] { Manifest.Permission.ReadExternalStorage };
        }
        else // Android 10 and below
        {
            return new[]
            {
                Manifest.Permission.ReadExternalStorage,
                Manifest.Permission.WriteExternalStorage
            };
        }
    }

    // 2. Check if we have them
    public static bool HasAudioPermissions()
    {
        var context = Application.Context;
        var permissions = GetRequiredAudioPermissions();

        foreach (var perm in permissions)
        {
            if (ContextCompat.CheckSelfPermission(context, perm) != Permission.Granted)
            {
                return false;
            }
        }
        return true;
    }

    // 3. Request them (Needs the current Activity)
    // Note: In Native Android, the result comes back in OnRequestPermissionsResult in MainActivity
    public static void RequestAudioPermissions(Activity activity, int requestCode = 1001)
    {
        var permissions = GetRequiredAudioPermissions();
        ActivityCompat.RequestPermissions(activity, permissions, requestCode);
    }
}