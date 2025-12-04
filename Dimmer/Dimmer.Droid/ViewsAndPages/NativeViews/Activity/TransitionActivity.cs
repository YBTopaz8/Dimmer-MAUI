

using AndroidX.CoordinatorLayout.Widget;

using Dimmer.NativeServices;
using Dimmer.Utilities.Extensions;

using Google.Android.Material.Dialog;

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

[Activity(
    MainLauncher = true, SupportsPictureInPicture = true,
        Name = "com.yvanbrunel.dimmer.TransitionActivity",
    LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges = ConfigChanges.ScreenSize |
    ConfigChanges.Orientation | ConfigChanges.UiMode |
    ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize |
    ConfigChanges.Density)]
public class TransitionActivity : AppCompatActivity
{
    public BottomSheetBehavior SheetBehavior { get; private set; }
     private FrameLayout _sheetContainer;
    private FrameLayout _contentContainer;
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
    const int REQUEST_AUDIO_PERMS = 99;


    protected override void OnCreate(Bundle? savedInstanceState)
    {
        SetTheme(Resource.Style.Theme_Dimmer);
        base.OnCreate(savedInstanceState);

        // 1. Initialize DI
        MainApplication.ServiceProvider ??= Bootstrapper.Init();
        try
        {
            MyViewModel = MainApplication.ServiceProvider.GetRequiredService<BaseViewModelAnd>();
        }
        catch (Exception ex)
        {
            ShowCrashDialog(ex);
            return;
        }

        Dimmer.Utils.UiThreads.InitializeMainHandler();

        // 2. Setup Coordinator Layout Architecture
        SetupLayout();

        // 3. Load Fragments (If fresh start)
        if (savedInstanceState == null)
        {
            // A. Load Content (Home)
            var startFragment = new HomePageFragment(MyViewModel);
            SupportFragmentManager
                .BeginTransaction()
                .Replace(_contentContainer.Id, startFragment, "HomePageFragment")
                .Commit();

            // B. Load Player (Sheet)
            var nowPlayingFrag = new NowPlayingFragment(MyViewModel);
            SupportFragmentManager
                .BeginTransaction()
                .Replace(_sheetContainer.Id, nowPlayingFrag, "NowPlayingFragment")
                .Commit();

            // C. Link Behavior to Fragment Animations
            SheetBehavior.AddBottomSheetCallback(new PlayerSheetCallback(nowPlayingFrag));
        }
        else
        {
            // Re-attach callback if restoring state
            var nowPlayingFrag = SupportFragmentManager.FindFragmentByTag("NowPlayingFragment") as NowPlayingFragment;
            if (nowPlayingFrag != null)
            {
                SheetBehavior.AddBottomSheetCallback(new PlayerSheetCallback(nowPlayingFrag));
            }
        }

        // 4. Standard Setup
        ProcessIntent(Intent);
        SetupService();
        SetStatusBarColor();
        SetupBackNavigation();

        // 5. Backstack Listener for ViewModel
        SupportFragmentManager.BackStackChanged += (s, e) =>
        {
            if (SupportFragmentManager.BackStackEntryCount == 0)
            {
                var homeFrag = SupportFragmentManager.FindFragmentByTag("HomePageFragment");
                MyViewModel.CurrentPage = homeFrag as Fragment;
            }
            else
            {
                MyViewModel.CurrentPage = SupportFragmentManager.Fragments.LastOrDefault();
            }
        };

        CheckAndRequestPermissions();
    }

    private void SetupLayout()
    {
        // Root Coordinator
        var coordinator = new CoordinatorLayout(this)
        {
            LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent)
        };
        coordinator.Id = View.GenerateViewId();

        // A. Content Container (Home, Settings, etc.)
        _contentContainer = new FrameLayout(this)
        {
            Id = View.GenerateViewId(),
            LayoutParameters = new CoordinatorLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent)
        };
        // Add bottom margin so the list isn't hidden behind the mini-player (approx 70dp)
        var contentParams = (CoordinatorLayout.LayoutParams)_contentContainer.LayoutParameters;
        contentParams.BottomMargin = (int)(Resources.DisplayMetrics.Density * 70);

        // **IMPORTANT**: Assign MyStaticID to this container so logic using it still works
        MyStaticID = _contentContainer.Id;

        var currentTheme = Resources?.Configuration?.UiMode & UiMode.NightMask;
        _contentContainer.SetBackgroundColor(currentTheme == UiMode.NightYes ? Color.ParseColor("#3E3E42") : Color.ParseColor("#DAD9E0"));

        // B. Bottom Sheet Container (Player)
        _sheetContainer = new FrameLayout(this)
        {
            Id = View.GenerateViewId(),
            LayoutParameters = new CoordinatorLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent),
            Elevation = 16 * Resources.DisplayMetrics.Density,
            Clickable = true,
            Focusable = true
        };
        // Background must be solid to cover content
        _sheetContainer.SetBackgroundColor(currentTheme == UiMode.NightYes ? Color.ParseColor("#121212") : Color.White);

        // Configure BottomSheetBehavior
        var sheetParams = (CoordinatorLayout.LayoutParams)_sheetContainer.LayoutParameters;
        SheetBehavior = new BottomSheetBehavior();
        SheetBehavior.PeekHeight = (int)(Resources.DisplayMetrics.Density * 70); // Mini Player Height
        SheetBehavior.State = BottomSheetBehavior.StateCollapsed;
        sheetParams.Behavior = SheetBehavior;

        // Add to Root
        coordinator.AddView(_contentContainer);
        coordinator.AddView(_sheetContainer);

        SetContentView(coordinator);


        AndroidX.Core.View.WindowCompat.SetDecorFitsSystemWindows(Window, false);
        AndroidX.Core.View.ViewCompat.SetOnApplyWindowInsetsListener(_contentContainer, new MyInsetsListener());
        AndroidX.Core.View.ViewCompat.SetOnApplyWindowInsetsListener(_sheetContainer, new MySheetInsetsListener());

    }

    class MyInsetsListener : Java.Lang.Object, AndroidX.Core.View.IOnApplyWindowInsetsListener
    {
        public AndroidX.Core.View.WindowInsetsCompat OnApplyWindowInsets(View? v, AndroidX.Core.View.WindowInsetsCompat? insets)
        {
            var bars = insets?.GetInsets(AndroidX.Core.View.WindowInsetsCompat.Type.SystemBars());
            // Apply padding to top (Status bar) and bottom (Nav bar)
            // Note: For content, we might only need Top if the List handles Bottom padding internally
            v.SetPadding(bars.Left, bars.Top, bars.Right, 0);
            return AndroidX.Core.View.WindowInsetsCompat.Consumed;
        }
    }

    class MySheetInsetsListener : Java.Lang.Object, AndroidX.Core.View.IOnApplyWindowInsetsListener
    {
        public AndroidX.Core.View.WindowInsetsCompat OnApplyWindowInsets(View? v, AndroidX.Core.View.WindowInsetsCompat? insets)
        {
            var bars = insets.GetInsets(AndroidX.Core.View.WindowInsetsCompat.Type.SystemBars());
            // Add bottom padding so the player controls aren't covered by gesture bar/nav buttons
            v.SetPadding(0, 0, 0, bars.Bottom);
            return insets; // Don't consume, let others see it if needed
        }
    }
    private void SetupService()
    {
        _serviceConnection = new MediaPlayerServiceConnection();
        _serviceIntent = new Intent(this, typeof(ExoPlayerService));

        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            StartForegroundService(_serviceIntent);
        else
            StartService(_serviceIntent);

        BindService(_serviceIntent, _serviceConnection, Bind.AutoCreate);
    }

    // --- Bottom Sheet Controls ---

    public void TogglePlayer()
    {
        if (SheetBehavior.State == BottomSheetBehavior.StateCollapsed)
            SheetBehavior.State = BottomSheetBehavior.StateExpanded;
        else
            SheetBehavior.State = BottomSheetBehavior.StateCollapsed;
    }

    public void CollapsePlayer()
    {
        if (SheetBehavior.State == BottomSheetBehavior.StateExpanded)
            SheetBehavior.State = BottomSheetBehavior.StateCollapsed;
    }

    // Callback class to drive animations in NowPlayingFragment
    private class PlayerSheetCallback : BottomSheetBehavior.BottomSheetCallback
    {
        private readonly NowPlayingFragment _fragment;
        public PlayerSheetCallback(NowPlayingFragment fragment) { _fragment = fragment; }

        public override void OnStateChanged(View bottomSheet, int newState)
        {
            if (newState == BottomSheetBehavior.StateExpanded)
                _fragment.SetInputActive(true);
            else if (newState == BottomSheetBehavior.StateCollapsed)
                _fragment.SetInputActive(false);
        }

        public override void OnSlide(View bottomSheet, float slideOffset)
        {
            // slideOffset: 0.0 (Collapsed) -> 1.0 (Expanded)
            _fragment.AnimateTransition(slideOffset);
        }
    }


    private void CheckAndRequestPermissions()
    {
        if (!AndroidPermissionsService.HasAudioPermissions())
        {
            // Show UI explanation? Or just request
            AndroidPermissionsService.RequestAudioPermissions(this, REQUEST_AUDIO_PERMS);
        }
        else
        {
            // Permissions already granted, load music
            InitializeAppLogic();
        }
    }

    public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
    {
        base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

        if (requestCode == REQUEST_AUDIO_PERMS)
        {
            // Check if all were granted
            bool allGranted = grantResults.All(r => r == Permission.Granted);

            if (allGranted)
            {
                // Success! Start scanning
                InitializeAppLogic();
            }
            else
            {

                // Denied. Show a dialog explaining why you need them.
                var materialDialog = new Google.Android.Material.Dialog.MaterialAlertDialogBuilder(this);
                materialDialog.SetTitle("Permissions Required");
                materialDialog.SetMessage("Dimmer needs access to storage to play your music.");
                materialDialog.SetPositiveButton("Retry", (s, e) => CheckAndRequestPermissions());
                materialDialog.SetNegativeButton("Exit", (s, e) => FinishAffinity());
                materialDialog.Show();
            }
        }
    }

    private void InitializeAppLogic()
    {
        Task.Run(() =>
        {
            try
            {
                MyViewModel.InitializeAllVMCoreComponents();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"VM INIT CRASH: {ex}");
                Android.Util.Log.Error("DIMMER_INIT", ex.ToString());
            }
        });
    }


    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);

        ProcessIntent(Intent);
    }

    private void SetStatusBarColor()
    {
        if (Window == null)
            return; 

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

            if (MyViewModel is null) return;
         
            SetupBackNavigation();
            // Log that the activity resumed
            Console.WriteLine("TransitionActivity: OnResume called.");
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
             transition.SetDuration(PublicStats.ActivityTransitionDurationMs);
            transition.SetInterpolator(PublicStats.BounceInterpolator); // This is ITimeInterpolator, your PublicStats.DefaultInterpolator should match

            transition.ExcludeTarget(Android.Resource.Id.StatusBarBackground, false);
            transition.ExcludeTarget(Android.Resource.Id.NavigationBarBackground, false);
        }

        return transition;
    }

    const int REQUEST_OPEN_FOLDER = 100;
    TaskCompletionSource<string?>? _folderPickerTcs;

    public Task<string?> PickFolderAsync()
    {
        _folderPickerTcs = new TaskCompletionSource<string?>();

        var intent = new Intent(Intent.ActionOpenDocumentTree);
        intent.AddFlags(ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantPersistableUriPermission);

        StartActivityForResult(intent, REQUEST_OPEN_FOLDER);

        return _folderPickerTcs.Task;
    }

    protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
    {
        base.OnActivityResult(requestCode, resultCode, data);
        if (requestCode == AndroidFolderPicker.PICK_FOLDER_REQUEST_CODE && resultCode == Result.Ok && data?.Data != null)
        {
            var picker = MainApplication.ServiceProvider.GetRequiredService<AndroidFolderPicker>();
            picker.OnResult((int)resultCode, data);
        }
        if (requestCode == REQUEST_OPEN_FOLDER)
        {
            
            if (resultCode == Result.Ok && data?.Data != null)
            {
                var uri = data.Data;

                // Persist permission so we can access it after reboot
                ContentResolver?.TakePersistableUriPermission(uri, ActivityFlags.GrantReadUriPermission);

                // Convert URI to a usable string/path (depends on your logic, 
                // but for SAF you usually keep the string URI)
                _folderPickerTcs?.TrySetResult(uri.ToString());
            }
            else
            {
                _folderPickerTcs?.TrySetResult(null); // Cancelled
            }
        }
    }


    const int REQUEST_WRITE_STORAGE = 1001;

    protected override void OnDestroy()
    {
        if (_serviceConnection != null)
        {
            UnbindService(_serviceConnection);
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
                if (currentFragment is null)
                { 
                    HandleBackPressInternal();

                };
            });
            OnBackInvokedDispatcher.RegisterOnBackInvokedCallback(IOnBackInvokedDispatcher.PriorityDefault, _onBackInvokedCallback);
            _isBackCallbackRegistered = true;
        }
        else
        {
             }
    }

    private void HandleBackPressInternal()
    {
        if (SupportFragmentManager.BackStackEntryCount > 0)
        {
            SupportFragmentManager.PopBackStack();
        }
        else
        {
            MoveTaskToBack(true);

        }
    }
    private void ProcessIntent(Android.Content.Intent? intent)
    {
        if (intent == null || string.IsNullOrEmpty(intent.Action))
        {
            return;
        }

        if (intent.Action == Android.Content.Intent.ActionView || intent.Action == Android.Content.Intent.ActionSend)
        {
            var uri = intent.Data;
            if (uri != null)
            {
                System.Diagnostics.Debug.WriteLine($"Received file to play: {uri}");
            }
            return;
        }

         if (intent.Action == "android.media.action.MEDIA_PLAY_FROM_SEARCH")
        {
             string? searchQuery = intent.GetStringExtra(SearchManager.Query);

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                System.Diagnostics.Debug.WriteLine($"Voice Search Query Received: '{searchQuery}'");


                RxSchedulers.UI.Schedule(() =>
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
    private void ShowCrashDialog(Exception ex)
    {
        new MaterialAlertDialogBuilder(this)
            .SetTitle("Startup Error")
            .SetMessage($"Failed to load dependencies:\n{ex.Message}")
            .SetPositiveButton("Close", (s, e) => FinishAffinity())
            .Show();
        
    }
}
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