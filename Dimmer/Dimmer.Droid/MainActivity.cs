using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Transitions;
using Android.Window;

using Dimmer.ViewModel;
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
[IntentFilter(new[] { "android.media.action.MEDIA_PLAY_FROM_SEARCH" },
              Categories = new[] { Intent.CategoryDefault })]
[IntentFilter(new[] { "android.intent.action.MUSIC_PLAYER" },
              Categories = new[] { Intent.CategoryDefault, "android.intent.category.APP_MUSIC" })]
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

    private IOnBackInvokedCallback? _onBackInvokedCallback; // For API 33+
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

        ProcessIntent(Intent);
    }

    private static void HandleIntent(Intent? intent)
    {

    }
    private static bool _firstTimeSetupDone = false;
    const string FirstTimeSetupKey = "FirstTimeBubbleSetupDone";

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
            Window?.RequestFeature(WindowFeatures.ContentTransitions); // Crucial for enabling transitions

            // Define Enter Transition (how this activity appears when started)
            Transition? enterTransition = CreateTransition(PublicStats.EnterTransition);
            if (enterTransition != null)
            {
                Window?.EnterTransition = enterTransition;
            }

            // Define Exit Transition (how this activity disappears when finishing)
            Transition? exitTransition = CreateTransition(PublicStats.ExitTransition);
            if (exitTransition != null)
            {
                Window?.ExitTransition = exitTransition;
            }

            // Define Reenter Transition (how this activity appears when returning from a subsequent activity)
            Transition? reenterTransition = CreateTransition(PublicStats.ReenterTransition);
            if (reenterTransition != null)
            {
                Window?.ReenterTransition = reenterTransition;
            }

            // Define Return Transition (how this activity disappears when it's the one returning to a previous activity)
            // This is often the reverse of the Enter transition of the activity it's returning to.
            Transition? returnTransition = CreateTransition(PublicStats.ReturnTransition);
            if (returnTransition != null)
            {
                Window?.ReturnTransition = returnTransition;
            }

            // Optional: Allow overlap for smoother transitions between activities
            Window?.AllowEnterTransitionOverlap = true;
            Window?.AllowReturnTransitionOverlap = true;

        }



        base.OnCreate(savedInstanceState);
        ProcessIntent(Intent);

        SetupBackNavigation();

        // 2. Create the service connection and give it the proxy instance.
        _serviceConnection = new MediaPlayerServiceConnection();


        // 1) Start the foreground service
        _serviceIntent = new Intent(this, typeof(ExoPlayerService));
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {

            this.
            StartForegroundService(_serviceIntent);

        }
        else
            StartService(_serviceIntent);
        //_serviceConnection = new MediaPlayerServiceConnection(audioSvc);
        BindService(_serviceIntent, _serviceConnection, Bind.AutoCreate);

        SetStatusBarColor();
        //FirstTimeBubbleSetup(Platform.AppContext);
        return;


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
        Window.SetUiOptions(UiOptions.SplitActionBarWhenNarrow); // Split action bar for narrow screens
        var ss = Resource.Drawable.appicon;
        Window.SetIcon(ss); // Set the app icon
        //Window.SetStatusBarColor(Android.Graphics.Color.Transparent); // Make status bar transparent
        // Tells the Window to draw under the status bar


#endif
    }

    protected override void OnDestroy()
    {
        if (_serviceConnection != null)
        {
            UnbindService(_serviceConnection);
            //_serviceConnection.OnServiceDisconnected(App);
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
    private void ProcessIntent(Android.Content.Intent? intent)
    {
        // First, check if the intent and action are what we expect
        if (intent == null || string.IsNullOrEmpty(intent.Action))
        {
            return;
        }

        // Handle "Open With..." or "Share" for a single file
        if (intent.Action == Android.Content.Intent.ActionView || intent.Action == Android.Content.Intent.ActionSend)
        {
            var uri = intent.Data;
            if (uri != null)
            {
                // TODO: Pass this URI to your audio service to be played
                System.Diagnostics.Debug.WriteLine($"Received file to play: {uri}");
            }
            return;
        }

        // *** THIS IS THE KEY PART FOR SearchManager.Query ***
        // Handle a search request from Google Assistant or Android Search
        if (intent.Action == "android.media.action.MEDIA_PLAY_FROM_SEARCH")
        {
            // Use the constant SearchManager.Query to get the search string
            // It's just a key to look inside the Intent's "extras" data.
            string searchQuery = intent.GetStringExtra(SearchManager.Query);

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                System.Diagnostics.Debug.WriteLine($"Voice Search Query Received: '{searchQuery}'");


                MainThread.InvokeOnMainThreadAsync(() =>
                {
                    var audioService = IPlatformApplication.Current.Services.GetService<BaseViewModel>();
                    if (audioService != null)
                    {
                        // You would define a method like this on your service interface
                        Console.WriteLine($"Searching and playing: {searchQuery}");
                        //audioService.SearchAndPlayAsync(searchQuery);
                    }
                });
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
