

namespace Dimmer.ViewsAndPages.NativeViews.Activity;


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
        Name = "com.yvanbrunel.dimmer.TransitionActivity",
    LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges = ConfigChanges.ScreenSize |
    ConfigChanges.Orientation | ConfigChanges.UiMode |
    ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize |
    ConfigChanges.Density)]
public class TransitionActivity : AppCompatActivity
{
    public TransitionActivity() { }
    public static int MyStaticID;
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

    protected override void OnCreate(Bundle? savedInstanceState)
    {

        if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop) // Transitions API Level 21+
        {
            Window?.RequestFeature(WindowFeatures.ContentTransitions); 

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

        if (MainApplication.ServiceProvider == null)
        {
            // Failsafe: If app was killed and restored oddly
            MainApplication.ServiceProvider = Bootstrapper.Init();
        }

        try
        {
            MyViewModel = MainApplication.ServiceProvider.GetRequiredService<BaseViewModelAnd>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"DI Error: {ex.Message}");
         

        }

        var container = new FrameLayout(this)
        {
            LayoutParameters = new FrameLayout.LayoutParams(
        ViewGroup.LayoutParams.MatchParent,
        ViewGroup.LayoutParams.MatchParent)
        };
        container.Id = Resource.Id.content;
        MyStaticID = container.Id;

        SetContentView(container);


        UiThreads.InitializeMainHandler();

        if (savedInstanceState == null)
        {
            var startFragment = new HomePageFragment(MyViewModel);
            SupportFragmentManager
                .BeginTransaction()
                .Replace(container.Id, startFragment)
                .Commit();
        }


        MyViewModel.InitializeAllVMCoreComponents();


        ProcessIntent(Intent);

        //SetupBackNavigation();

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

        SetupBackNavigation();


        SupportFragmentManager.BackStackChanged += (s, e) =>
        {
            int count = SupportFragmentManager.BackStackEntryCount;

            if (count == 0)
            {
                // If stack is empty, we must be back at Home
                // You might need to cast or search via Tag if you store the instance elsewhere
                var homeFrag = SupportFragmentManager.FindFragmentByTag("HomePageFragment");
                MyViewModel.CurrentPage = homeFrag as Fragment;
            }
            else
            {
                // If stack has items, get the top one
                // (This assumes you manage CurrentPage strictly for UI state)
                var topFrag = SupportFragmentManager.Fragments.LastOrDefault();
                MyViewModel.CurrentPage = topFrag;
            }
        };



        return;


    }

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);

        ProcessIntent(Intent);
    }

    private void SetStatusBarColor()
    {
        if (Window == null)
            return; // Should not happen in OnCreate after base call

#if RELEASE
        Window.SetStatusBarColor(Android.Graphics.Color.DarkSlateBlue);
#elif DEBUG
        Window.SetStatusBarColor(Android.Graphics.Color.ParseColor("#861B2D"));
        Window.SetUiOptions(UiOptions.SplitActionBarWhenNarrow); // Split action bar for narrow screens
        //Window.SetStatusBarColor(Android.Graphics.Color.Transparent); // Make status bar transparent
        // Tells the Window to draw under the status bar


#endif
    }
    protected async override void OnResume()
    {
        try
        {
            base.OnResume();
            Platform.OnResume(this);

            if (MyViewModel is null) return;
            if (MyViewModel.IsLastFMNeedsToConfirm)
            {
                //bool isLastFMAuthorized = await Shell.Current.DisplayAlert("LAST FM Confirm", "Is Authorization done?", "Yes", "No");
                //if (isLastFMAuthorized)
                //{
                //    await MyViewModel.CompleteLastFMLoginCommand.ExecuteAsync(null);
                //}
                //else
                //{
                //    MyViewModel.IsLastFMNeedsToConfirm = false;
                //    await Shell.Current.DisplayAlert("Action Cancelled", "Last FM Authorization Cancelled", "OK");

                //}
            }
            SetupBackNavigation();
            // Log that the activity resumed
            Console.WriteLine("MainActivity: OnResume called.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
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

    const int REQUEST_WRITE_STORAGE = 1001;
    public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
    {
        base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

        if (requestCode == REQUEST_WRITE_STORAGE)
        {
            bool granted = grantResults.Length > 0 && grantResults[0] == Permission.Granted;
            Android.Util.Log.Debug("MainActivity", $"WRITE_EXTERNAL_STORAGE granted? {granted}");
        }
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

    private void SetupBackNavigation()
    {
        if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu) // API 33+
        {
            _onBackInvokedCallback = new BackInvokedCallback(() =>
            {
                var currentFragment = MyViewModel.CurrentPage as HomePageFragment;
                if (currentFragment is not null) 
                {
                    HandleBackPressInternal();

                };
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
        // 1. Check if there are Fragments in the stack (e.g., NowPlaying or Settings)
        if (SupportFragmentManager.BackStackEntryCount > 0)
        {
            // Go back one step in the native Fragment history
            SupportFragmentManager.PopBackStack();
        }
        else
        {
            // 2. We are at the Root (HomePageFragment)
            // Minimize the app (Standard Android behavior)
            MoveTaskToBack(true);

            // OR if you truly want to close the app process:
            // Finish(); 
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
                    Intent mainActivityIntent = new Intent(this, typeof(TransitionActivity)); // <<< YOUR MAIN ACTIVITY
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