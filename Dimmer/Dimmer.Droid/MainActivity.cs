using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Util;
using Android.Transitions;
using Android.Window;
using Dimmer.ViewsAndPages.NativeViews.Activity;

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
    MainLauncher = true, SupportsPictureInPicture = true,
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
    BaseViewModelAnd MyViewModel { get; set; }
    public ExoPlayerServiceBinder? Binder
    {
        get => _binder
               ?? throw new InvalidOperationException("Service not bound yet");
        set => _binder = value;


    }
    public static int MyStaticID { get; private set; }

    public MainActivity()
    {

        MyViewModel = IPlatformApplication.Current?.Services.GetService<BaseViewModelAnd>() ?? throw new InvalidOperationException("BaseViewModelAnd not found in DI container");
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

    protected async override void OnResume()
    {
        try
        {
            base.OnResume();
            Platform.OnResume(this);
            if (MyViewModel.IsLastFMNeedsToConfirm)
            {
                bool isLastFMAuthorized = await Shell.Current.DisplayAlert("LAST FM Confirm", "Is Authorization done?", "Yes", "No");
                if (isLastFMAuthorized)
                {
                    await MyViewModel.CompleteLastFMLoginCommand.ExecuteAsync(null);
                }
                else
                {
                    MyViewModel.IsLastFMNeedsToConfirm = false;
                    await Shell.Current.DisplayAlert("Action Cancelled", "Last FM Authorization Cancelled", "OK");

                }
            }
            // Log that the activity resumed
            Console.WriteLine("MainActivity: OnResume called.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    const int REQUEST_WRITE_STORAGE = 1001;


    protected override void OnCreate(Bundle? savedInstanceState)
    {

        try
        {


            base.OnCreate(savedInstanceState);


            ProcessIntent(Intent);

            SetupBackNavigation();

            // 2. Create the service connection and give it the proxy instance.
            _serviceConnection = new MediaPlayerServiceConnection();


            // 1) StartAsync the foreground service
            _serviceIntent = new Intent(this, typeof(ExoPlayerService));
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {

                this.StartForegroundService(_serviceIntent);

            }
            else
                StartService(_serviceIntent);
            //_serviceConnection = new MediaPlayerServiceConnection(audioSvc);
            BindService(_serviceIntent, _serviceConnection, Bind.AutoCreate);

            SetStatusBarColor();
            //FirstTimeBubbleSetup(Platform.AppContext);


            // Skip directly into your native activity
            var intent = new Intent(this, typeof(TransitionActivity));
            StartActivity(intent);

            // Optional: close MainActivity so back button won't return to it
            //Finish();
            return;



        }
        catch (Java.Lang.IllegalArgumentException ex)
        {
            Console.WriteLine(ex.Message);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
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
    
            if (!IsTaskRoot)
            {
                Finish();
            }
            else
            {
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
            string? searchQuery = intent.GetStringExtra(SearchManager.Query);

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                System.Diagnostics.Debug.WriteLine($"Voice Search Query Received: '{searchQuery}'");


                MainThread.InvokeOnMainThreadAsync(() =>
                {
                    Intent mainActivityIntent = new Intent(this, typeof(MainActivity)); // <<< YOUR MAIN ACTIVITY
                    mainActivityIntent.AddFlags(ActivityFlags.NewTask | ActivityFlags.ClearTop);
                    try
                    {
                        StartActivity(mainActivityIntent);
                    }
                    catch
                    (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
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
    protected override void OnUserLeaveHint()
    {
        base.OnUserLeaveHint();

    }

    private void TryEnterPipMode()
    {
        try
        {



            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var builder = new PictureInPictureParams.Builder();
                builder.SetAspectRatio(new Rational(16, 9)); // adjust for your UI
                var pipParams = builder.Build();
                if (pipParams is not null)
                {

                    EnterPictureInPictureMode(pipParams);
                }
                Console.WriteLine("Entered PiP mode");
            }

        }
        catch (Exception ex)

        {
            Console.WriteLine(ex.Message);
        }
    }

    public override void OnPictureInPictureModeChanged(bool isInPipMode, Configuration? newConfig)
    {
        base.OnPictureInPictureModeChanged(isInPipMode, newConfig);

        // Hide or show MAUI shell UI depending on mode
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var page = Shell.Current?.CurrentPage;
            if (page != null)
                page.IsVisible = !isInPipMode;
        });
    }
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning restore CS0672 // Member overrides obsolete member


}

