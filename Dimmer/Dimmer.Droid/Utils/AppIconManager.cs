using Android.Content;
using Android.Content.PM;

namespace Dimmer.Utils;

/// <summary>
/// Manages app icon changes for Android platform
/// </summary>
public static class AppIconManager
{
    // Define available icon aliases (these will be defined in AndroidManifest.xml)
    public static readonly Dictionary<string, string> IconAliases = new()
    {
        { "Default", "com.yvanbrunel.dimmer.DefaultIcon" },
        { "Music", "com.yvanbrunel.dimmer.MusicIcon" },
        { "Vinyl", "com.yvanbrunel.dimmer.VinylIcon" },
        { "Headphones", "com.yvanbrunel.dimmer.HeadphonesIcon" }
    };

    /// <summary>
    /// Changes the app icon by enabling/disabling activity-alias components
    /// </summary>
    /// <param name="context">Android context</param>
    /// <param name="iconName">Name of the icon to activate (must match keys in IconAliases)</param>
    public static void ChangeAppIcon(Context context, string iconName)
    {
        if (!IconAliases.ContainsKey(iconName))
        {
            throw new ArgumentException($"Icon '{iconName}' is not defined in IconAliases");
        }

        var packageManager = context.PackageManager;
        if (packageManager == null)
            return;

        // Disable all other icon aliases
        foreach (var alias in IconAliases.Values)
        {
            try
            {
                packageManager.SetComponentEnabledSetting(
                    new ComponentName(context, alias),
                    ComponentEnabledState.Disabled,
                    ComponentEnableOption.DontKillApp
                );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disabling icon alias {alias}: {ex.Message}");
            }
        }

        // Enable the selected icon alias
        var selectedAlias = IconAliases[iconName];
        try
        {
            packageManager.SetComponentEnabledSetting(
                new ComponentName(context, selectedAlias),
                ComponentEnabledState.Enabled,
                ComponentEnableOption.DontKillApp
            );

            // Save the preference
            AppSettingsService.AppIconPreference.SetAppIcon(iconName);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error enabling icon alias {selectedAlias}: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Gets the currently active app icon name
    /// </summary>
    public static string GetCurrentIcon()
    {
        return AppSettingsService.AppIconPreference.GetAppIcon();
    }

    /// <summary>
    /// Gets all available icon names
    /// </summary>
    public static List<string> GetAvailableIcons()
    {
        return IconAliases.Keys.ToList();
    }
}
