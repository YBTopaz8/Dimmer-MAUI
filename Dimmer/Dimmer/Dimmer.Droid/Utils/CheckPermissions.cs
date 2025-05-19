using Android;
using Android.OS;

namespace Dimmer.Utils; 
public class CheckPermissions : Permissions.BasePlatformPermission // Renamed for clarity
{
    public override (string androidPermission, bool isRuntime)[] RequiredPermissions
    {
        get
        {
            var result = new List<(string androidPermission, bool isRuntime)>();

            if (OperatingSystem.IsAndroidVersionAtLeast(33)) // API 33+
            {
                result.Add((Manifest.Permission.PostNotifications, true));
            }

            // Media Access
            if (OperatingSystem.IsAndroidVersionAtLeast(33)) // API 33+ (Android 13)
            {
                result.Add((Manifest.Permission.ReadMediaAudio, true));
                // If you also need images:
                 result.Add((Manifest.Permission.ReadMediaImages, true));
             
            }
            else // Pre-API 33 (Android 12 and older, down to where runtime permissions are needed - API 23)
            {

                result.Add((Manifest.Permission.ReadExternalStorage, true));
            }


            if (Build.VERSION.SdkInt < BuildVersionCodes.Q) // Below Android 10 (API 29)
            {
                result.Add((Manifest.Permission.WriteExternalStorage, true));
            }
             else if (Build.VERSION.SdkInt == BuildVersionCodes.Q && IsLegacyStorageOptedIn()) // Android 10 (API 29) with legacy opt-in
             {
                result.Add((Manifest.Permission.WriteExternalStorage, true));
             }
            

            return result.ToArray();
        }
    }
    private bool IsLegacyStorageOptedIn()
    {
        
        return false; // Placeholder
    }

}

