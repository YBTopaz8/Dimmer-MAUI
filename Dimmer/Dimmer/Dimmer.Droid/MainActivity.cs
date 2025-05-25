using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Transitions;
using Android.Util;
namespace Dimmer;
[IntentFilter(new[] { Platform.Intent.ActionAppAction }, // Use the constant
                Categories = new[] { Intent.CategoryDefault })]
[IntentFilter(new[] { Intent.ActionSend, Intent.ActionSendMultiple }, // Handle single and multiple files/items
                Categories = new[] { Intent.CategoryDefault }, // for implicit intents                                           
                DataMimeType = "audio/*")]

[IntentFilter(new[] { Intent.ActionView },
         Categories = new[] { Intent.CategoryDefault },
         DataMimeType = "audio/*" // Or more specific MIME types/schemes/paths
        )]
[Activity(Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
        Name = "com.yvanbrunel.dimmer.MainActivity",
    LaunchMode = LaunchMode.SingleTask,
    ConfigurationChanges = ConfigChanges.ScreenSize |
    ConfigChanges.Orientation | ConfigChanges.UiMode |
    ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize |
    ConfigChanges.Density)]
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
        Task.Run(async () => await HandleIntent(intent));


    }

    private static async Task HandleIntent(Intent? intent)
    {
        //Console.WriteLine(intent?.Action?.ToString());
        //Platform.OnNewIntent(intent);
        //// Log that the intent was received
        //Console.WriteLine("MainActivity: OnNewIntent received.");

        //if (intent == null)
        //    return;

        //var action = intent.Action;
        //Android.Net.Uri? dataUri = intent.Data; // Data URI for ACTION_VIEW

        //if (action == Intent.ActionView && dataUri != null)
        //{

        //    // If it wasn't our specific app link, it might be a general ACTION_VIEW for a file
        //    if (dataUri != null && intent.Type != null && intent.Type.StartsWith("audio/", StringComparison.OrdinalIgnoreCase))
        //    {
        //        Console.WriteLine($"[MainActivity_ProcessIntent] General ACTION_VIEW for audio: {dataUri}");
        //        _ = IntentHandlerUtils.HandleSharedAudioUrisAsync(new List<string> { dataUri.ToString() });
        //        return; // Handled as a general audio view
        //    }
        //}


        //if (intent.Type != null)
        //{

        //    var type = intent.Type; // MIME Type
        //    var dataUris = intent.Data; // Usually for ACTION_VIEW
        //    var clipData = intent.ClipData; // Often used for ACTION_SEND* with URIs
        //    var streamUri = intent.GetParcelableExtra(Intent.ExtraStream) as Android.Net.Uri; // For ACTION_SEND single item

        //    Console.WriteLine($"MainActivity: Processing Intent - Action={action}, Type={type}, Data={dataUri}, HasClipData={clipData != null}, HasStreamExtra={streamUri != null}");

        //    // Determine if it's a share or view intent we should handle
        //    bool isShareIntent = action == Intent.ActionSend || action == Intent.ActionSendMultiple;
        //    bool isViewIntent = action == Intent.ActionView;
        //    bool isAudio = type != null && type.StartsWith("audio/", StringComparison.OrdinalIgnoreCase);

        //    // Prioritize Share Intents containing audio URIs
        //    if (isShareIntent && isAudio)
        //    {
        //        var fileUris = new List<Android.Net.Uri>();

        //        // Handle single item shared via EXTRA_STREAM
        //        if (streamUri != null && action == Intent.ActionSend)
        //        {
        //            fileUris.Add(streamUri);
        //        }
        //        // Handle single or multiple items shared via ClipData
        //        else if (clipData != null)
        //        {
        //            for (int i = 0; i < clipData.ItemCount; i++)
        //            {
        //                var itemUri = clipData.GetItemAt(i)?.Uri;
        //                if (itemUri != null)
        //                {
        //                    fileUris.Add(itemUri);
        //                }
        //            }
        //        }

        //        if (fileUris.Count > 0)
        //        {
        //            Console.WriteLine($"MainActivity: Found {fileUris.Count} audio URI(s) in Share Intent.");
        //            // Pass URIs to shared handler (using helper class below)
        //            _ = IntentHandlerUtils.HandleSharedAudioUrisAsync(fileUris.Select(u => u.ToString()).ToList());
        //        }
        //        else
        //        {
        //            // Handle shared text if needed
        //            string sharedText = intent.GetStringExtra(Intent.ExtraText);
        //            if (!string.IsNullOrEmpty(sharedText))
        //            {
        //                Console.WriteLine($"MainActivity: Received shared text: {sharedText}");
        //                // _ = IntentHandlerUtils.HandleSharedTextAsync(sharedText); // Implement if needed
        //            }
        //        }
        //    }
        //    // Handle File Opening (ACTION_VIEW)
        //    else if (isViewIntent && dataUri != null && isAudio)
        //    {
        //        Console.WriteLine($"MainActivity: Found audio URI in View Intent: {dataUri}");
        //        _ = IntentHandlerUtils.HandleSharedAudioUrisAsync(new List<string> { dataUri.ToString() });
        //    }
        //    // Handle App Actions via MAUI Essentials (already done in OnNewIntent)
        //    else if (action == Platform.Intent.ActionAppAction)
        //    {
        //        Console.WriteLine("MainActivity: Detected App Action, handled by Platform.OnNewIntent.");
        //        // MAUI's Platform.OnNewIntent will trigger your OnAppAction delegate
        //    }
        //    else
        //    {
        //        Console.WriteLine($"MainActivity: Intent action '{action}' not explicitly handled here.");
        //    }
        //}
    }
    private static bool _firstTimeSetupDone = false;
    const string FirstTimeSetupKey = "FirstTimeBubbleSetupDone";
    private void FirstTimeBubbleSetup(Context context)
    {
        // Using SharedPreferences to ensure this runs only once per install,
        // or when you want to force a reset by changing the logic.
        ISharedPreferences prefs = Android.Preferences.PreferenceManager.GetDefaultSharedPreferences(context);
        bool alreadyDone = prefs.GetBoolean(FirstTimeSetupKey, false);

        if (!_firstTimeSetupDone && !alreadyDone) // Only run once per app session AND once per install
        {
            Log.Warn("BubbleDebug", "PERFORMING AGGRESSIVE ONE-TIME BUBBLE CHANNEL RESET!");

            // 1. Delete the old channel if it exists
            var notificationManager = (NotificationManager)context.GetSystemService(Context.NotificationService);
            if (notificationManager != null)
            {
                notificationManager.DeleteNotificationChannel(NotificationHelper.ChannelId);
                Log.Info("BubbleDebug", $"Attempted to delete channel: {NotificationHelper.ChannelId}");
            }

            // 2. Recreate the channel with bubble support explicitly enabled
            // Call your existing CreateChannel, ensuring it explicitly sets SetAllowBubbles(true)
            NotificationHelper.CreateChannel(context); // Use the version that can force

            // 3. Mark as done
            prefs.Edit().PutBoolean(FirstTimeSetupKey, true).Apply();
            _firstTimeSetupDone = true;
            Log.Info("BubbleDebug", "One-time bubble channel reset complete.");

            // 4. Immediately check and log the status
            var channel = notificationManager.GetNotificationChannel(NotificationHelper.ChannelId);
            if (channel != null && Build.VERSION.SdkInt >= BuildVersionCodes.Q)
            {
                Log.Info("BubbleDebug", $"IMMEDIATE POST-RESET: Channel '{NotificationHelper.ChannelId}' CanBubble: {channel.CanBubble()}");
                if (!channel.CanBubble())
                {
                    Log.Error("BubbleDebug", "STILL CANNOT BUBBLE IMMEDIATELY AFTER RESET. Check global/app/developer settings!");
                    // This is the point where you might Toast to guide the user directly to settings.
                    Toast.MakeText(context, "Bubble setup issue. Please check App Notification Settings & Developer Options for Bubbles.", ToastLength.Long).Show();
                    NotificationHelper.OpenBubbleSettings(context); // Or OpenBubbleSettings
                }
            }
            else if (channel == null)
            {
                Log.Error("BubbleDebug", "Channel is NULL immediately after recreate attempt!");
            }
        }
        else
        {
            // On subsequent starts, just ensure channel exists without aggressive reset
            NotificationHelper.CreateChannel(context);
        }
    }

    protected override void OnResume()
    {
        base.OnResume();
        Platform.OnResume(this);
        // Log that the activity resumed
        Console.WriteLine("MainActivity: OnResume called.");
    }

    const int REQUEST_WRITE_STORAGE = 1001;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
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



#if RELEASE
        Window.SetStatusBarColor(Android.Graphics.Color.DarkSlateBlue);
#elif DEBUG
        Window.SetStatusBarColor(Android.Graphics.Color.ParseColor("#861B2D"));
#endif


        return;



        //        // Android Native Unhandled Exception Handler (Java/Kotlin layer)
        //        Android.Runtime.AndroidEnvironment.UnhandledExceptionRaiser += (sender, args) =>
        //        {
        //            System.Diagnostics.Debug.WriteLine($"******** AndroidEnvironment.UnhandledExceptionRaiser: {args.Exception} ********");
        //            LogUnhandledException("AndroidEnvironment_Unhandled", args.Exception);
        //            args.Handled = true; // Try to prevent immediate termination for logging, may not always work
        //        };

        //        // .NET Unhandled Exceptions (also in MauiProgram.cs for broader coverage)
        //        AppDomain.CurrentDomain.UnhandledException += (sender, args) => {
        //            System.Diagnostics.Debug.WriteLine($"******** AppDomain.CurrentDomain.UnhandledException (MainActivity): {args.ExceptionObject} ********");
        //            LogUnhandledException("AppDomain_Unhandled_MainActivity", (Exception)args.ExceptionObject);
        //        };

        //        TaskScheduler.UnobservedTaskException += (sender, args) => {
        //            System.Diagnostics.Debug.WriteLine($"******** TaskScheduler.UnobservedTaskException (MainActivity): {args.Exception} ********");
        //            LogUnhandledException("TaskScheduler_Unobserved_MainActivity", args.Exception);
        //            args.SetObserved();
        //        };

        //        Android.Util.Log.Debug("MainActivity", $"Running in package: {PackageName}");
        //        // --- STORAGE PERMISSION / MANAGER CHECK ---
        //        if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
        //        {
        //            // API 30+ use isExternalStorageManager
        //            bool hasManageAll = Android.OS.Environment.IsExternalStorageManager;
        //            Android.Util.Log.Debug("MainActivity", $"MANAGE_EXTERNAL_STORAGE granted? {hasManageAll}");
        //            if (!hasManageAll)
        //            {
        //                // fire intent to ask user to grant MANAGE_EXTERNAL_STORAGE
        //                var uri = Android.Provider.Settings.ActionManageAllFilesAccessPermission;
        //                var intent = new Android.Content.Intent(uri);
        //                StartActivity(intent);
        //            }
        //        }
        //        else
        //        {
        //            // API <30 fallback to WRITE_EXTERNAL_STORAGE
        //            if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.WriteExternalStorage)
        //                    != Permission.Granted)
        //            {
        //                ActivityCompat.RequestPermissions(
        //                    this,
        //                    new[] { Manifest.Permission.WriteExternalStorage },
        //                    REQUEST_WRITE_STORAGE);
        //            }
        //            else
        //            {
        //                Android.Util.Log.Debug("MainActivity", "WRITE_EXTERNAL_STORAGE already granted");
        //            }
        //        }

        //        if (DeviceInfo.Idiom == DeviceIdiom.Watch)
        //        {
        //            return;
        //        }
        //        if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
        //        {
        //            if (!Android.OS.Environment.IsExternalStorageManager)
        //            {
        //                Intent intent = new Intent();
        //                intent.SetAction(Settings.ActionManageAppAllFilesAccessPermission);
        //                Android.Net.Uri uri = Android.Net.Uri.FromParts("package", PackageName!, null)!;
        //                intent.SetData(uri);
        //                StartActivity(intent);
        //            }
        //        }
        //        //Win
        //        // Ensure Window is not null before accessing it
        //        if (Window != null)
        //        {

        //#if ANDROID_35
        //            Window.SetStatusBarColor(Android.Graphics.Color.ParseColor("#861B2D"));
        //#else
        //            // Alternative implementation for Android versions >= 35
        //            // Add your custom logic here if needed
        //#endif
        //        }




        //        // Optional: Handle intent if app was launched FROM CLOSED by the action
        //         Platform.OnNewIntent(Intent); // Call this here *too* if needed for cold start actions
    }

    private static void LogUnhandledException(string source, Exception ex)
    {
        if (ex == null)
            return;
        // For Android, FileSystem.AppDataDirectory should work,
        // but you can also use Android.App.Application.Context.GetExternalFilesDir(null).AbsolutePath
        string appDataDir = FileSystem.AppDataDirectory;
        string errorLogPath = Path.Combine(appDataDir, "error_log_android.txt");
        try
        {
            string errorMessage = $"[{DateTime.Now}] Unhandled Exception from {source}:\n{ex.GetType().FullName}: {ex.Message}\nStack Trace:\n{ex.StackTrace}\n";
            if (ex.InnerException != null)
            {
                errorMessage += $"\nInner Exception:\n{ex.InnerException.GetType().FullName}: {ex.InnerException.Message}\nStack Trace:\n{ex.InnerException.StackTrace}\n";
            }
            File.AppendAllText(errorLogPath, errorMessage + "\n-----------------------------------\n");
            System.Diagnostics.Debug.WriteLine($"Logged to: {errorLogPath}");

            // If you want to display an alert (requires UI thread)
            // MainThread.BeginInvokeOnMainThread(() =>
            // {
            //    App.Current.MainPage.DisplayAlert("Crash Report", $"Error in {source}:\n{ex.Message}", "OK");
            // });
        }
        catch (Exception logEx)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to write to error log: {logEx.Message}");
        }
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


    public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
    {
        base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

        if (requestCode == REQUEST_WRITE_STORAGE)
        {
            bool granted = grantResults.Length > 0 && grantResults[0] == Permission.Granted;
            Android.Util.Log.Debug("MainActivity", $"WRITE_EXTERNAL_STORAGE granted? {granted}");
        }
    }

    private Transition CreateTransition(ActivityTransitionType type)
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.Lollipop)
            return null;

        Transition transition = null;

        switch (type)
        {
            case ActivityTransitionType.Fade:
                transition = new Fade();
                break;
            case ActivityTransitionType.SlideFromEnd:
                transition = new Slide(GravityFlags.End);
                break;
            case ActivityTransitionType.SlideFromStart:
                transition = new Slide(GravityFlags.Start);
                break;
            case ActivityTransitionType.SlideFromBottom:
                transition = new Slide(GravityFlags.Bottom);
                break;
            case ActivityTransitionType.Explode:
                transition = new Explode();
                break;
            case ActivityTransitionType.None:
            default:
                return null;
        }

        if (transition != null)
        {
            // Use setter methods instead of property initializers
            transition.SetDuration(PublicStats.ActivityTransitionDurationMs);
            transition.SetInterpolator(PublicStats.ActivityTransitionInterpolator); // This is ITimeInterpolator, your PublicStats.DefaultInterpolator should match

            transition.ExcludeTarget(Android.Resource.Id.StatusBarBackground, true);
            transition.ExcludeTarget(Android.Resource.Id.NavigationBarBackground, true);
        }

        return transition;
    }
}
