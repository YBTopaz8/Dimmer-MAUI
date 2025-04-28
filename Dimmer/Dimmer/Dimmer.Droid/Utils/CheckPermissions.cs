using Android;

namespace Dimmer.Utils; 
public class CheckPermissions : Permissions.BasePlatformPermission
{
    public override (string androidPermission, bool isRuntime)[] RequiredPermissions
    {
        get
        {
            List<(string androidPermission, bool isRuntime)> result = new List<(string androidPermission, bool isRuntime)>();
            if (OperatingSystem.IsAndroidVersionAtLeast(33))
            {
                result.Add((Manifest.Permission.PostNotifications, true));
                result.Add((Manifest.Permission.ReadMediaAudio, true));
                result.Add((Manifest.Permission.ReadMediaImages, true));
                result.Add((Manifest.Permission.ManageExternalStorage, true));
                result.Add((Manifest.Permission.ReadExternalStorage, true));
                result.Add((Manifest.Permission.WriteExternalStorage, true));

            }
            return [.. result];
        }
    }
}

