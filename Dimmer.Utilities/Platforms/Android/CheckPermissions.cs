using Android;
using Android.Content.PM;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using static Android.Preferences.PreferenceManager;

namespace Dimmer.Utilities;

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
