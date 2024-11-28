namespace Dimmer_MAUI.Platforms.Windows;
// All the code in this file is only included on Windows.
public class CheckPermissions
{
    public static async Task<bool> CheckAndRequestStoragePermissionAsync()
    {
        return true;
    }
}
