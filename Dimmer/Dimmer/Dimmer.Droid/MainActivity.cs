﻿using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Database;
using Android.OS;
using Android.Provider;
using Dimmer.DimmerLive.Models;
using System.Diagnostics;
using System.Threading.Tasks;
using UraniumUI.Material.Controls;

namespace Dimmer;
[IntentFilter(
       new[] { Platform.Intent.ActionAppAction }, // Use the constant
       Categories = new[] { Intent.CategoryDefault }
   )]
[IntentFilter(
        new[] { Intent.ActionSend, Intent.ActionSendMultiple }, // Handle single and multiple files/items
        Categories = new[] { Intent.CategoryDefault }, // REQUIRED for implicit intents
                                                       // Specify MIME types your app can handle via sharing
        DataMimeType = "audio/*" // Handle any audio type
                                 // OR be more specific:
                                 // DataMimeTypes = new[] { "audio/mpeg", "audio/wav", "audio/ogg", "audio/flac", "audio/x-m4a", "audio/aac" }
    )]

[IntentFilter(
         new[] { Intent.ActionView },
         Categories = new[] { Intent.CategoryDefault },
         DataMimeType = "audio/*" // Or more specific MIME types/schemes/paths
     )]
[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTask, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    private static string AppLinkHost;


    MediaPlayerServiceConnection? _serviceConnection;
    Intent? _serviceIntent;
    private ExoPlayerServiceBinder? _binder;
    public ExoPlayerServiceBinder? Binder
    {
        get => _binder
               ?? throw new InvalidOperationException("Service not bound yet");
        set => _binder = value;

        
    }
    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        HandleIntent(intent);

        
    }

    private static async Task HandleIntent(Intent? intent)
    {
        Console.WriteLine(intent?.Action?.ToString());
        Platform.OnNewIntent(intent);
        // Log that the intent was received
        Console.WriteLine("MainActivity: OnNewIntent received.");
  
        if (intent == null)
            return;

        var action = intent.Action;
        Android.Net.Uri? dataUri = intent.Data; // Data URI for ACTION_VIEW

        if (action == Intent.ActionView && dataUri != null)
        {
            Console.WriteLine($"[MainActivity_ProcessIntent] Received ACTION_VIEW with URI: {dataUri}");
            if (dataUri.Scheme == "https" && dataUri.Host.Equals(AppLinkHost, StringComparison.OrdinalIgnoreCase))
            {
                // It's an app link for our domain!
                Console.WriteLine($"[MainActivity_ProcessIntent] Matched AppLinkHost: {AppLinkHost}");

                // Example path check: if (dataUri.PathSegments.Contains("invite"))
                // For now, let's assume any path on this host with query params is for us.

                string? eventType = dataUri.GetQueryParameter("type");
                string? eventId = dataUri.GetQueryParameter("id");
                // string senderId = dataUri.GetQueryParameter("sender"); // If you need it

                Console.WriteLine($"[MainActivity_ProcessIntent_AppLink] EventType: '{eventType}', EventId: '{eventId}'");

                if (!string.IsNullOrEmpty(eventType) && !string.IsNullOrEmpty(eventId))
                {
                    // Pass to shared MAUI code (App.xaml.cs or a dedicated service)
                    // Ensure App.ProcessAppLink exists and is accessible (e.g., static)
                    // Or use a messaging service if you prefer.
                    //processa ) ProcessAppLink(eventType, eventId /*, senderId */);

                    await IntentHandlerUtils.ProcessAppLink(eventType, eventId /*, senderId */);  
                    


                    return; // App link handled, no need to process further as a generic share
                }
                else
                {
                    Console.WriteLine("[MainActivity_ProcessIntent_AppLink] 'type' or 'id' missing in app link query.");
                }
            }
            // --- END OF APP LINK HANDLING ---
            // If it wasn't our specific app link, it might be a general ACTION_VIEW for a file
            else if (dataUri != null && intent.Type != null && intent.Type.StartsWith("audio/", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"[MainActivity_ProcessIntent] General ACTION_VIEW for audio: {dataUri}");
                _ = IntentHandlerUtils.HandleSharedAudioUrisAsync(new List<string> { dataUri.ToString() });
                return; // Handled as a general audio view
            }
        }


        if (intent.Type != null)
        {

            var type = intent.Type; // MIME Type
            var dataUris = intent.Data; // Usually for ACTION_VIEW
            var clipData = intent.ClipData; // Often used for ACTION_SEND* with URIs
            var streamUri = intent.GetParcelableExtra(Intent.ExtraStream) as Android.Net.Uri; // For ACTION_SEND single item

            Console.WriteLine($"MainActivity: Processing Intent - Action={action}, Type={type}, Data={dataUri}, HasClipData={clipData != null}, HasStreamExtra={streamUri != null}");

            // Determine if it's a share or view intent we should handle
            bool isShareIntent = action == Intent.ActionSend || action == Intent.ActionSendMultiple;
            bool isViewIntent = action == Intent.ActionView;
            bool isAudio = type != null && type.StartsWith("audio/", StringComparison.OrdinalIgnoreCase);

            // Prioritize Share Intents containing audio URIs
            if (isShareIntent && isAudio)
            {
                var fileUris = new List<Android.Net.Uri>();

                // Handle single item shared via EXTRA_STREAM
                if (streamUri != null && action == Intent.ActionSend)
                {
                    fileUris.Add(streamUri);
                }
                // Handle single or multiple items shared via ClipData
                else if (clipData != null)
                {
                    for (int i = 0; i < clipData.ItemCount; i++)
                    {
                        var itemUri = clipData.GetItemAt(i)?.Uri;
                        if (itemUri != null)
                        {
                            fileUris.Add(itemUri);
                        }
                    }
                }

                if (fileUris.Count > 0)
                {
                    Console.WriteLine($"MainActivity: Found {fileUris.Count} audio URI(s) in Share Intent.");
                    // Pass URIs to shared handler (using helper class below)
                    _ = IntentHandlerUtils.HandleSharedAudioUrisAsync(fileUris.Select(u => u.ToString()).ToList());
                }
                else
                {
                    // Handle shared text if needed
                    string sharedText = intent.GetStringExtra(Intent.ExtraText);
                    if (!string.IsNullOrEmpty(sharedText))
                    {
                        Console.WriteLine($"MainActivity: Received shared text: {sharedText}");
                        // _ = IntentHandlerUtils.HandleSharedTextAsync(sharedText); // Implement if needed
                    }
                }
            }
            // Handle File Opening (ACTION_VIEW)
            else if (isViewIntent && dataUri != null && isAudio)
            {
                Console.WriteLine($"MainActivity: Found audio URI in View Intent: {dataUri}");
                _ = IntentHandlerUtils.HandleSharedAudioUrisAsync(new List<string> { dataUri.ToString() });
            }
            // Handle App Actions via MAUI Essentials (already done in OnNewIntent)
            else if (action == Platform.Intent.ActionAppAction)
            {
                Console.WriteLine("MainActivity: Detected App Action, handled by Platform.OnNewIntent.");
                // MAUI's Platform.OnNewIntent will trigger your OnAppAction delegate
            }
            else
            {
                Console.WriteLine($"MainActivity: Intent action '{action}' not explicitly handled here.");
            }
        }
    }


    protected override void OnResume()
    {
        base.OnResume();
        Platform.OnResume(this);
        // Log that the activity resumed
        Console.WriteLine("MainActivity: OnResume called.");
    }
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        if (DeviceInfo.Idiom == DeviceIdiom.Watch)
        {
            return;
        }
        if (!Android.OS.Environment.IsExternalStorageManager)
        {
            Intent intent = new Intent();
            intent.SetAction(Settings.ActionManageAppAllFilesAccessPermission);
            Android.Net.Uri uri = Android.Net.Uri.FromParts("package", PackageName!, null)!;
            intent.SetData(uri);
            StartActivity(intent);
        }

        //Win
        // Ensure Window is not null before accessing it
        if (Window != null)
        {
            
#if ANDROID_35
            Window.SetStatusBarColor(Android.Graphics.Color.ParseColor("#861B2D"));
#else
            // Alternative implementation for Android versions >= 35
            // Add your custom logic here if needed
#endif
        }

        IAudioActivity? audioSvc = IPlatformApplication.Current!.Services.GetService<IDimmerAudioService>()
            as IAudioActivity
            ?? throw new InvalidOperationException("AudioService missing");

        // 1) Start the foreground service
        _serviceIntent = new Intent(this, typeof(ExoPlayerService));
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            StartForegroundService(_serviceIntent);
        else
            StartService(_serviceIntent);
        _serviceConnection = new MediaPlayerServiceConnection(audioSvc);
        BindService(_serviceIntent, _serviceConnection, Bind.AutoCreate);

        // Optional: Handle intent if app was launched FROM CLOSED by the action
         Platform.OnNewIntent(Intent); // Call this here *too* if needed for cold start actions
    }
    protected override void OnDestroy()
    {
        if (_serviceConnection != null)
        {
            UnbindService(_serviceConnection);
            _serviceConnection.Disconnect();
        }
        base.OnDestroy();
    }


}
public static class IntentHandlerUtils
{
    // Handles a list of URIs received from Share or View intents
    public static async Task HandleSharedAudioUrisAsync(List<string> uriStrings)
    {
        if (uriStrings == null || uriStrings.Count == 0)
            return;

        Console.WriteLine($"IntentHandlerUtils: Received {uriStrings.Count} URI(s) to handle.");
        var resolvedFilePaths = new List<string>();

        foreach (var uriString in uriStrings)
        {
            if (string.IsNullOrWhiteSpace(uriString))
                continue;

            string fn = "Attachment.txt";
            string file = Path.Combine(FileSystem.CacheDirectory, fn);

            File.WriteAllText(file, "Hello World");

            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Share text file",
                File = new ShareFile(file)
            });
            Console.WriteLine($"IntentHandlerUtils: Attempting to resolve URI: {uriString}");
            string? filePath = await ResolveUriToFilePathAsync(uriString);
            if (!string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath))
            {
                Console.WriteLine($"IntentHandlerUtils: Resolved to path: {filePath}");
                resolvedFilePaths.Add(filePath);
            }
            else
            {
                Console.WriteLine($"IntentHandlerUtils: Failed to resolve or access file for URI: {uriString}");
                // Optionally notify the user of the failure for this specific URI
            }
        }

        if (resolvedFilePaths.Count > 0)
        {
            // --- YOUR MAUI LOGIC HERE ---
            // Process the resolved file paths (e.g., add to playlist, navigate to player)
            // IMPORTANT: Perform UI operations on the main thread!
            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    Console.WriteLine($"IntentHandlerUtils: Dispatching to handle {resolvedFilePaths.Count} resolved audio files.");
                    
                        Console.WriteLine($"TODO: Navigate or add to playlist: {string.Join(", ", resolvedFilePaths)}");

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"IntentHandlerUtils: Error during UI dispatch: {ex}");
                }
            });
            // --- END YOUR MAUI LOGIC ---
        }
        else
        {
            Console.WriteLine("IntentHandlerUtils: No valid file paths resolved from input URIs.");
            // Optionally inform the user that no compatible files were shared/opened.
        }
    }


    // --- URI Resolution Helper (Primarily for Android Content URIs) ---
    // Tries to get a usable file path, potentially by copying to cache.
    private static async Task<string?> ResolveUriToFilePathAsync(string uriString)
    {
        if (string.IsNullOrWhiteSpace(uriString))
            return null;

        Uri uri;
        bool isUri = Uri.TryCreate(uriString, UriKind.Absolute, out uri!);

        // 1. Handle direct file paths or file:// URIs
        if (isUri && uri.IsFile || !isUri && File.Exists(uriString))
        {
            return isUri ? uri.LocalPath : uriString;
        }

        // 2. Handle Android content:// URIs
        if (isUri && uri.Scheme == ContentResolver.SchemeContent)
        {
            var context = Platform.AppContext;
            if (context == null)
            {
                Console.WriteLine("ResolveUriToFilePathAsync: Android context is null.");
                return null;
            }
            string? displayName = null;
            ICursor? cursor = null;

            // Convert System.Uri to Android.Net.Uri
            var androidUri = Android.Net.Uri.Parse(uriString);

            // Try getting display name for the target cache file
            try
            {
                cursor = context.ContentResolver.Query(androidUri, new[] { OpenableColumns.DisplayName }, null, null, null);
                if (cursor != null && cursor.MoveToFirst())
                {
                    int nameIndex = cursor.GetColumnIndex(OpenableColumns.DisplayName);
                    if (nameIndex != -1)
                        displayName = cursor.GetString(nameIndex);
                }
            }
            catch (Exception ex) { Console.WriteLine($"ResolveUriToFilePathAsync: Error getting display name: {ex.Message}"); }
            finally { cursor?.Close(); }

            string targetFileName = Path.GetFileNameWithoutExtension(displayName ?? Path.GetRandomFileName())
                                    + "_" + Guid.NewGuid().ToString("N").Substring(0, 8) // Add randomness
                                    + Path.GetExtension(displayName ?? ".audio"); // Preserve extension if known
            string targetFilePath = Path.Combine(FileSystem.CacheDirectory, targetFileName);

            // Copy the content stream to the cache directory (most reliable way)
            try
            {
                // Replace the obsolete 'OpenableColumns' and 'OpenableColumns.DisplayName' with 'Android.Provider.IOpenableColumns' and 'Android.Provider.IOpenableColumns.DisplayName' respectively.

                cursor = context.ContentResolver.Query(androidUri, [IOpenableColumns.DisplayName], null, null, null);
                using var inputStream = context.ContentResolver.OpenInputStream(androidUri); // Fixed type mismatch
                if (inputStream == null)
                    throw new FileNotFoundException("Could not open input stream for content URI.", uriString);

                using var outputStream = new FileStream(targetFilePath, FileMode.Create, FileAccess.Write);
                await inputStream.CopyToAsync(outputStream);
                Console.WriteLine($"ResolveUriToFilePathAsync: Copied content URI to cache: {targetFilePath}");
                return targetFilePath; // Return path to cached file
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ResolveUriToFilePathAsync: Error copying content URI to cache: {ex.Message}");
                if (File.Exists(targetFilePath))
                { try { File.Delete(targetFilePath); } catch { /* Ignore delete error */ } }
                return null; // Return null if copy failed
            }
        }
        Console.WriteLine($"ResolveUriToFilePathAsync: URI scheme '{uri?.Scheme}' not handled or invalid path: {uriString}");
        return null;
    }

    // Optional: Handler for shared text
    // public static Task HandleSharedTextAsync(string text) { ... }
    public static async Task ProcessAppLink(string eventType, string eventId /*, string senderId = null */)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            Console.WriteLine($"[App.xaml.cs] Processing App Link: Type='{eventType}', ID='{eventId}'");

            
                Console.WriteLine("[App.xaml.cs] MainPage not ready for App Link.");
            
            // Ensure LiveStateService is initialized and user is logged in
            // This logic needs to be robust.
            // For simplicity, assuming LiveStateService is accessible and will handle user state.

            if (eventType.Equals("userInvite", StringComparison.OrdinalIgnoreCase))
            {
                await HandleUserInviteAppLink(eventId);
            }
            // ... other event types
        });
    }

    private static async Task HandleUserInviteAppLink(string inviterUserId)
    {
        

        //if (LiveStateService.UserOnline.ObjectId == inviterUserId)
        //{
        //    await Shell.Current.DisplayAlert("Invitation", "You cannot invite yourself.", "OK");
        //    return;
        //}

        //UserModelOnline? inviterUser = null;
        //try
        //{
        //    inviterUser = await ParseObject.CreateWithoutData<UserModelOnline>(inviterUserId).FetchIfNeededAsync() as UserModelOnline;
        //}
        //catch (Exception ex) { Console.WriteLine($"Error fetching inviter {inviterUserId}: {ex.Message}"); }

        //string inviterName = inviterUser?.Username ?? "A user";
        //bool accept = await Shell.Current.DisplayAlert("User Invitation",
        //    $"{inviterName} wants to connect. Accept?", "Accept", "Decline");

        //if (accept)
        //{
        //    if (inviterUser == null)
        //        inviterUser = ParseObject.CreateWithoutData<UserModelOnline>(inviterUserId); // Re-create pointer if fetch failed

        //    ChatConversation? conversation = await LiveStateService.GetOrCreateConversationWithUserAsync(inviterUser);
        //    if (conversation != null)
        //    {
        //        await Shell.Current.DisplayAlert("Connected!", $"You are now connected with {inviterName}.", "OK");
        //        // TODO: Navigate to conversation: await Shell.Current.GoToAsync($"//chatPage?conversationId={conversation.ObjectId}");
        //    }
        //    else
        //    {
        //        await Shell.Current.DisplayAlert("Error", "Could not establish connection.", "OK");
        //    }
        //}
    
    }   
}
