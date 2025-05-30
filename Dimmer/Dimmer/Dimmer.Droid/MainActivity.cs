using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Transitions;
using Android.Util;
using Android.Window;
using AndroidX.Activity;
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

    private IOnBackInvokedCallback _onBackInvokedCallback; // For API 33+
    private bool _isBackCallbackRegistered = false;
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


        if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop) // Transitions API Level 21+
        {
            Window.RequestFeature(WindowFeatures.ContentTransitions); // Crucial for enabling transitions

            // Define Enter Transition (how this activity appears when started)
            Transition? enterTransition = CreateTransition(PublicStats.EnterTransition);
            if (enterTransition != null)
            {
                Window.EnterTransition = enterTransition;
            }

            // Define Exit Transition (how this activity disappears when finishing)
            Transition? exitTransition = CreateTransition(PublicStats.ExitTransition);
            if (exitTransition != null)
            {
                Window.ExitTransition = exitTransition;
            }

            // Define Reenter Transition (how this activity appears when returning from a subsequent activity)
            Transition? reenterTransition = CreateTransition(PublicStats.ReenterTransition);
            if (reenterTransition != null)
            {
                Window.ReenterTransition = reenterTransition;
            }

            // Define Return Transition (how this activity disappears when it's the one returning to a previous activity)
            // This is often the reverse of the Enter transition of the activity it's returning to.
            Transition? returnTransition = CreateTransition(PublicStats.ReturnTransition);
            if (returnTransition != null)
            {
                Window.ReturnTransition = returnTransition;
            }

            // Optional: Allow overlap for smoother transitions between activities
            Window.AllowEnterTransitionOverlap = true;
            Window.AllowReturnTransitionOverlap = true;

        }



        base.OnCreate(savedInstanceState);

        SetupBackNavigation();

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

        SetStatusBarColor();
        FirstTimeBubbleSetup(Platform.AppContext);
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





        //        // Optional: Handle intent if app was launched FROM CLOSED by the action
        //         Platform.OnNewIntent(Intent); // Call this here *too* if needed for cold start actions
    }

    private void SetStatusBarColor()
    {
        if (Window == null)
            return; // Should not happen in OnCreate after base call

#if RELEASE
     Window.SetStatusBarColor(Android.Graphics.Color.DarkSlateBlue);
#elif DEBUG
        Window.SetStatusBarColor(Android.Graphics.Color.ParseColor("#861B2D"));
        Window.SetStatusBarColor(Android.Graphics.Color.ParseColor("#861B2D"));
        
        //Window.SetStatusBarColor(Android.Graphics.Color.Transparent); // Make status bar transparent
                                                                      // Tells the Window to draw under the status bar


#endif
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
        if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu && _onBackInvokedCallback != null && _isBackCallbackRegistered)
        {
            OnBackInvokedDispatcher.UnregisterOnBackInvokedCallback(_onBackInvokedCallback);
            _isBackCallbackRegistered = false;
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

    private static Transition? CreateTransition(ActivityTransitionType type)
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.Lollipop)
            return null;

        Transition? transition = null;

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
            transition.SetInterpolator(PublicStats.BounceInterpolator); // This is ITimeInterpolator, your PublicStats.DefaultInterpolator should match

            transition.ExcludeTarget(Android.Resource.Id.StatusBarBackground, false);
            transition.ExcludeTarget(Android.Resource.Id.NavigationBarBackground, false);
        }

        return transition;
    }

    private void SetupBackNavigation()
    {
        if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu) // API 33+
        {
            _onBackInvokedCallback = new BackInvokedCallback(() =>
            {
                HandleBackPressInternal();
            });
            // Registering with Priority_DEFAULT. Higher priority callbacks are invoked first.
            OnBackInvokedDispatcher.RegisterOnBackInvokedCallback(IOnBackInvokedDispatcher.PriorityDefault, _onBackInvokedCallback);
            _isBackCallbackRegistered = true;
        }
        else
        {
            // For older versions, OnBackPressed() will be called by the system.
            // We can also use the AndroidX OnBackPressedDispatcher for a more consistent approach
            // across versions if preferred, but overriding OnBackPressed is simpler for API < 33
            // if you are not using other Jetpack Activity features that rely on OnBackPressedDispatcher.
            // Example using AndroidX (MauiAppCompatActivity provides this dispatcher):
            // OnBackPressedDispatcher.AddCallback(this, new MyOnBackPressedCallback(true, this));
        }
    }

    // This method contains the logic for what to do when back is pressed.
    private void HandleBackPressInternal()
    {
        System.Diagnostics.Debug.WriteLine("HandleBackPressInternal invoked.");
        bool mauiHandled = Shell.Current.SendBackButtonPressed(); // Ask MAUI to handle it

        if (mauiHandled)
        {
            System.Diagnostics.Debug.WriteLine("MAUI handled OnBackPressed.");
            return;
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("MAUI did not handle OnBackPressed. Calling base.OnBackPressed().");
            // If MAUI didn't handle it, and we're on API < 33 using the OnBackPressed override,
            // calling base.OnBackPressed() would perform the default finish.
            // For API 33+, if this callback is invoked and MAUI didn't handle it,
            // the system will typically finish the activity if this callback doesn't do something else
            // or if there isn't a lower priority callback.
            // If this is the root activity, it might go to background instead of finishing.
            // To explicitly finish:
            // Finish();
            // Or move to back:
            // MoveTaskToBack(true);

            // For API 33+, if you want the *default system behavior* after your checks,
            // you might need to unregister the callback TEMPORARILY and then trigger a back press,
            // or rely on the fact that if your callback doesn't "consume" the event fully,
            // the system might proceed. This area is a bit nuanced.
            // Often, if MAUI doesn't handle it, and it's the root page, you let the system minimize the app.
            // If it's NOT the root, you'd typically call Finish();

            // For now, let's assume if MAUI doesn't handle it, and this is our callback,
            // we might want to finish if it's not the root task.
            // This logic mirrors roughly what the default OnBackPressed does.
            if (!IsTaskRoot)
            {
                Finish();
            }
            else
            {
                // If it's the root task, the system will usually move it to the back.
                // You could explicitly do MoveTaskToBack(true); but often not needed here.
            }
        }
    }

    // This is a helper class for the OnBackInvokedCallback
    sealed class BackInvokedCallback : Java.Lang.Object, IOnBackInvokedCallback
    {
        private readonly Action _action;
        public BackInvokedCallback(Action action)
        {
            _action = action;
        }
        public void OnBackInvoked()
        {
            _action?.Invoke();
        }
    }


    // --- Fallback for API < 33 ---
#pragma warning disable CS0672 // Member overrides obsolete member
#pragma warning disable CS0618 // Type or member is obsolete
    public override void OnBackPressed()
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.Tiramisu) // Only use this logic for older APIs
        {
            System.Diagnostics.Debug.WriteLine("OnBackPressed (API < 33) invoked.");
            bool mauiHandled = Shell.Current.SendBackButtonPressed(); // Ask MAUI to handle it

            if (mauiHandled)
            {
                System.Diagnostics.Debug.WriteLine("MAUI handled OnBackPressed.");
                return;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("MAUI did not handle OnBackPressed. Calling base.OnBackPressed().");
                base.OnBackPressed(); // Perform default activity finish or move to back
            }
        }
        else
        {
            // For API 33+, the OnBackInvokedCallback should be handling it.
            // Calling base.OnBackPressed() here is generally not recommended by Android
            // as it might have unintended side effects with the new system.
            // However, MAUI's base MauiAppCompatActivity might have its own logic.
            // It's safer to let the new callback system handle it.
            // If MAUI's base needs this, it should also be using the new callback system.
            System.Diagnostics.Debug.WriteLine("OnBackPressed (API >= 33) called, but OnBackInvokedCallback should be active.");
            base.OnBackPressed(); // Call base just in case MAUI's base activity has specific logic
        }
    }
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning restore CS0672 // Member overrides obsolete member


}
