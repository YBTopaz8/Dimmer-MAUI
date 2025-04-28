using Android.Content;

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
}
