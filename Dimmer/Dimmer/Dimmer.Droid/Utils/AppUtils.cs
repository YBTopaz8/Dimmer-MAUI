using Android.Content;

using Dimmer.Interfaces.Services.Interfaces;

using System.Security.Cryptography;
using System.Text;

using Window = Microsoft.Maui.Controls.Window;

namespace Dimmer.Utils;
public class AppUtil : IAppUtil
{
    public Shell GetShell()
    {
        return new AppShell();
    }

    public Window LoadWindow()
    {
        Window window = new Window();
        window.Page = GetShell();
        return window;
    }
    public static class ShareHelper
    {
        public static void ShareText(string text, string title = "Share via")
        {
            Intent sendIntent = new Intent();
            sendIntent.SetAction(Intent.ActionSend);
            sendIntent.PutExtra(Intent.ExtraText, text);
            sendIntent.SetType("text/plain");

            Intent shareIntent = Intent.CreateChooser(sendIntent, title)!;
            shareIntent.SetFlags(ActivityFlags.NewTask); // Needed if calling from non-Activity context
            Platform.AppContext.StartActivity(shareIntent);
        }

        public static void ShareUri(string uriString, string mimeType, string title = "Share via")
        {
            // IMPORTANT: Ensure the URI is accessible! Use FileProvider for local files.
            // This example assumes a content:// URI or publicly accessible http:// URI
            Android.Net.Uri? contentUri = Android.Net.Uri.Parse(uriString);
            if (contentUri == null)
                return;

            Intent sendIntent = new Intent();
            sendIntent.SetAction(Intent.ActionSend);
            sendIntent.PutExtra(Intent.ExtraStream, contentUri);
            sendIntent.SetType(mimeType);
            sendIntent.AddFlags(ActivityFlags.GrantReadUriPermission); // Crucial for content:// URIs

            Intent shareIntent = Intent.CreateChooser(sendIntent, title)!;
            shareIntent.SetFlags(ActivityFlags.NewTask);
            Platform.AppContext.StartActivity(shareIntent);
        }
        // Add method to get ContentUri using FileProvider for local files
    }

    public static async Task UpdateAppActionsForRecentAudioAsync(Func<Task<IEnumerable<string>>> getRecentFilesFunc, int count)
    {
        if (!AppActions.IsSupported || getRecentFilesFunc == null)
        {
            Debug.WriteLine("App Actions not supported or function to get files not provided.");
            return;
        }

        try
        {
            IEnumerable<string> recentFiles = await getRecentFilesFunc();
            var actions = recentFiles.Take(count)
                .Select(filePath =>
                {
                    string fileName = Path.GetFileNameWithoutExtension(filePath);
                    // Create a relatively stable ID from the file path
                    string id = $"play_recent_{GenerateStableId(filePath)}";
                    return new AppAction(id, $"Play {fileName}", subtitle: Path.GetFileName(filePath));
                    // TODO: Consider adding an icon if available
                })
                .ToList();

            // Include any essential static actions if desired, otherwise this replaces all
            // var staticActions = GetStaticActions(); // Your method to get base actions
            // actions.AddRange(staticActions);

            await AppActions.Current.SetAsync(actions);
            Debug.WriteLine($"Updated App Actions with {actions.Count} recent files.");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error updating App Actions for recent audio: {ex.Message}");
            // LogError("UpdateRecentAudioActions", ex); // Use your logging function
        }
    }

    // Helper for generating a somewhat stable ID from a path
    private static string GenerateStableId(string input)
    {
        // Use the static HashData method instead of creating an instance of SHA256  
        byte[] hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        // Use a portion of the hash, Base64 encoded for ID safety  
        return Convert.ToBase64String(hashBytes).Substring(0, 12)
                      .Replace("/", "_").Replace("+", "-"); // Make URL/ID safe  
    }

    // 2. Add Contextual App Action
    public static async Task AddContextualAppActionAsync(string id, string title, string contextKey, string subtitle = null, string icon = null)
    {
        if (!AppActions.IsSupported || string.IsNullOrWhiteSpace(id))
        {
            Debug.WriteLine("App Actions not supported or invalid ID.");
            return;
        }

        try
        {
            var currentActions = (await AppActions.Current.GetAsync()).ToList();
            // Prefix ID with contextKey for easy removal later
            string contextualId = $"ctx_{contextKey}_{id}";

            // Avoid adding duplicates
            if (!currentActions.Any(a => a.Id == contextualId))
            {
                var newAction = new AppAction(contextualId, title, subtitle, icon);
                currentActions.Add(newAction);
                await AppActions.Current.SetAsync(currentActions);
                Debug.WriteLine($"Added contextual App Action: {contextualId}");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error adding contextual App Action: {ex.Message}");
        }
    }

    // 3. Remove Contextual App Action
    public static async Task RemoveContextualAppActionsAsync(string contextKey)
    {
        if (!AppActions.IsSupported || string.IsNullOrWhiteSpace(contextKey))
        {
            Debug.WriteLine("App Actions not supported or context key invalid.");
            return;
        }

        try
        {
            var currentActions = await AppActions.Current.GetAsync();
            string prefix = $"ctx_{contextKey}_";
            var filteredActions = currentActions.Where(a => !a.Id.StartsWith(prefix)).ToList();

            // Only update if actions were actually removed
            if (filteredActions.Count < currentActions.Count())
            {
                await AppActions.Current.SetAsync(filteredActions);
                Debug.WriteLine($"Removed contextual App Actions for key: {contextKey}");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error removing contextual App Actions: {ex.Message}");
        }
    }

    // 4. Parse App Action Payload (Example: ID="type_value1_value2")
    public static Dictionary<string, string> ParseAppActionPayload(AppAction action, char delimiter = '_')
    {
        var payload = new Dictionary<string, string>();
        if (action == null || string.IsNullOrWhiteSpace(action.Id))
        {
            return payload;
        }

        try
        {
            string[] parts = action.Id.Split(delimiter);
            if (parts.Length > 0)
                payload["action_type"] = parts[0];
            if (parts.Length > 1)
                payload["value1"] = parts[1];
            if (parts.Length > 2)
                payload["value2"] = parts[2];
            // Add more based on your expected format
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error parsing App Action ID '{action.Id}': {ex.Message}");
        }
        return payload;
    }

    // 5. Get Localized App Action Title
    // Assumes you have a ResourceManager named 'AppResources' or similar
    public static string GetLocalizedAppActionTitle(string actionId, string defaultTitle = null)
    {
        // Example using a hypothetical ResourceManager setup
        // Replace with your actual localization mechanism
        /*
        try
        {
            // Use a convention, e.g., AppAction_ID_Title resource key
            string resourceKey = $"AppAction_{actionId}_Title";
            string localizedTitle = YourApp.Resources.AppResources.ResourceManager.GetString(resourceKey);

            if (!string.IsNullOrEmpty(localizedTitle))
            {
                return localizedTitle;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting localized title for action '{actionId}': {ex.ChatMessage}");
        }
        */
        // Fallback to provided default or the action ID itself
        return defaultTitle ?? actionId;
    }

    // 6. Validate App Action Icon Exists (Platform Dependent)
    public static bool ValidateAppActionIconExists(string iconName)
    {
        if (string.IsNullOrWhiteSpace(iconName))
            return false; // No icon is valid

        try
        {

            var context = Android.App.Application.Context;
            var resources = context.Resources;
            // Ensure icon name doesn't include extension for Android drawable lookup
            string nameWithoutExtension = Path.GetFileNameWithoutExtension(iconName);
            int resourceId = resources.GetIdentifier(nameWithoutExtension, "drawable", context.PackageName);
            return resourceId != 0;

        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error validating icon resource '{iconName}': {ex.Message}");
            return false;
        }
    }
}
