using Android;
using Microsoft.Maui.ApplicationModel;

namespace Dimmer_MAUI.Platforms.Android;

// All the code in this file is only included on Android.
public class CheckPermissions : Permissions.BasePlatformPermission
{
    public override (string androidPermission, bool isRuntime)[] RequiredPermissions
    {
        get
        {
            var result = new List<(string androidPermission, bool isRuntime)>();
            if (OperatingSystem.IsAndroidVersionAtLeast(33))
            {
                result.Add((Manifest.Permission.PostNotifications, true));
                result.Add((Manifest.Permission.ReadMediaAudio, true));
                result.Add((Manifest.Permission.ReadMediaImages, true));
                result.Add((Manifest.Permission.ManageExternalStorage, true));
                result.Add((Manifest.Permission.ReadExternalStorage, true));
                result.Add((Manifest.Permission.WriteExternalStorage, true));

            }
            return result.ToArray();
        }
    }
}
