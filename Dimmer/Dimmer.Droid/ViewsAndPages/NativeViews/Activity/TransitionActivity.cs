

using AndroidX.CoordinatorLayout.Widget;
using AndroidX.Core.View;
using AndroidX.DrawerLayout.Widget;

using Dimmer.NativeServices;
using Dimmer.ViewsAndPages.NativeViews.DimmerLive;
using Dimmer.ViewsAndPages.NativeViews.StatsSection;
using Dimmer.WinUI.UiUtils;

using Google.Android.Material.BottomNavigation;
using Google.Android.Material.Dialog;
using Google.Android.Material.Navigation;

using Explode = AndroidX.Transitions.Explode;
using Fade = AndroidX.Transitions.Fade;
using Slide = AndroidX.Transitions.Slide;
using Transition = AndroidX.Transitions.Transition;

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
public class TransitionActivity : AppCompatActivity, IOnApplyWindowInsetsListener
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


    public BottomNavigationView NavBar { get; private set; }


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
        WindowCompat.SetDecorFitsSystemWindows(Window, false);

        // Make bars transparent
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
        SetupDrawerLayout();

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
            
            Fragment current = SupportFragmentManager.FindFragmentById(_contentContainer.Id);
            MyViewModel.CurrentPage = current;
        };

        CheckAndRequestPermissions();


    }
    DrawerLayout _drawerLayout;
    private void SetupDrawerLayout()
    {
        var currentTheme = Resources?.Configuration?.UiMode & UiMode.NightMask;
        var bgColor = currentTheme == UiMode.NightYes ? Color.ParseColor("#121212") : Color.ParseColor("#F5F5F5");
        _drawerLayout = new DrawerLayout(this)
        {
            LayoutParameters = new ViewGroup.LayoutParams(-1, -1),
        };
        _drawerLayout.Id = View.GenerateViewId();

        _mainContentCoordinator = new CoordinatorLayout(this)
        {
            LayoutParameters = new DrawerLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent),
        };
        _mainContentCoordinator.SetBackgroundColor(bgColor);

        // 2a. Fragment Container (The Page)
        _contentContainer = new FrameLayout(this)
        {
            Id = View.GenerateViewId(),
            LayoutParameters = new CoordinatorLayout.LayoutParams(-1, -1)
        };
        _contentContainer.Id = Resource.Id.custom_fragment_container;
        MyStaticID = Resource.Id.custom_fragment_container;
        // 2b. Player Sheet Container
        _sheetContainer = new FrameLayout(this)
        {
            Id = View.GenerateViewId(),
            LayoutParameters = new CoordinatorLayout.LayoutParams(-1, -1)
            {
                Gravity = (int)GravityFlags.Bottom
            },
            Elevation = 16 * Resources.DisplayMetrics.Density,
            Clickable = true,
            Focusable = true
        };
        _sheetContainer.SetBackgroundColor(currentTheme == UiMode.NightYes ? Color.ParseColor("#1E1E1E") : Color.White);

        // Configure Sheet Behavior
        var sheetParams = (CoordinatorLayout.LayoutParams)_sheetContainer.LayoutParameters;
        SheetBehavior = new BottomSheetBehavior();
        SheetBehavior.PeekHeight = AppUtil.DpToPx(70);
        SheetBehavior.State = BottomSheetBehavior.StateCollapsed;
        sheetParams.Behavior = SheetBehavior;

        // Add views to Coordinator (Content first, then Player on top)
        _mainContentCoordinator.AddView(_contentContainer);
        _mainContentCoordinator.AddView(_sheetContainer);

        // 3. Navigation View ( The Side Menu)
        _navigationView = new NavigationView(this)
        {
            LayoutParameters = new DrawerLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.MatchParent)
            {
                Gravity = (int)GravityFlags.Start // Slides from Left
            }
        };

        // Setup Menu Items
        _navigationView.Menu.Add(0, 100, 0, "Home").SetIcon(Resource.Drawable.musicaba);
        _navigationView.Menu.Add(0, 101, 0, "Browser / Graph").SetIcon(Resource.Drawable.heart);
        _navigationView.Menu.Add(0, 102, 0, "Last FM").SetIcon(Resource.Drawable.lastfm);
        _navigationView.Menu.Add(0, 103, 0, "Settings").SetIcon(Resource.Drawable.settings);

        // Handle Clicks
        _navigationView.NavigationItemSelected += (s, e) =>
        {
            NavigateToId(e.MenuItem.ItemId);
            _drawerLayout.CloseDrawer(GravityCompat.Start); // Close after click
        };

        // 4. Assemble Root
        _drawerLayout.AddView(_mainContentCoordinator);
        _drawerLayout.AddView(_navigationView);


        var headerView = new LinearLayout(this)
        {
            Orientation = Orientation.Vertical,
            LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, AppUtil.DpToPx(200)) // Height: 200dp
        };
        headerView.SetBackgroundColor(Color.ParseColor("#2D2D30")); // Dark Header BG
        headerView.SetGravity (GravityFlags.Bottom);
        headerView.SetPadding(40, 40, 40, 40);

        var appTitle = new TextView(this)
        {
            Text = "Dimmer",
            TextSize = 24,
            Typeface = Typeface.DefaultBold
        };
        appTitle.SetTextColor(Color.White);

        var appSub = new TextView(this)
        {
            Text = "Your Library",
            TextSize = 14
        };
        appSub.SetTextColor(Color.LightGray);

        headerView.AddView(appTitle);
        headerView.AddView(appSub);

        _navigationView.AddHeaderView(headerView);


        SetContentView(_drawerLayout);
        ViewCompat.SetOnApplyWindowInsetsListener(_drawerLayout, this);
    }
    private int _systemBarBottom;
    public WindowInsetsCompat OnApplyWindowInsets(View v, WindowInsetsCompat insets)
    {
        var bars = insets.GetInsets(WindowInsetsCompat.Type.SystemBars());

        int top = bars.Top;
        int bottom = bars.Bottom;

        // Content: status bar + mini-player height
        int miniPlayerHeight = AppUtil.DpToPx(70);
        _systemBarBottom = bars.Bottom;
        _contentContainer.SetPadding(
            0,
            top,
            0,
            bottom + miniPlayerHeight
        );

        // Player sheet sits ABOVE system nav bar
        var lp = (CoordinatorLayout.LayoutParams)_sheetContainer.LayoutParameters;
        lp.BottomMargin = _systemBarBottom;
        _sheetContainer.LayoutParameters = lp;
        SheetBehavior.PeekHeight = miniPlayerHeight + _systemBarBottom;
        return WindowInsetsCompat.Consumed;
    }

    private void NavigateToId(int id)
    {
        Fragment? selectedFrag = null;
        string tag = "";

        switch (id)
        {
            case 100:
                if (SupportFragmentManager.FindFragmentByTag("HomePageFragment") != null)
                {
                    SupportFragmentManager.PopBackStack("HomePageFragment", 0);
                    return;
                }
                selectedFrag = new HomePageFragment(MyViewModel);
                tag = "HomePageFragment";
                break;
            case 101:
                var vm = MainApplication.ServiceProvider.GetRequiredService<StatisticsViewModel>();
                selectedFrag = new LibraryStatsHostFragment(MyViewModel, vm);
                tag = "StatsFragment";

                Task.Run(()=> vm.LoadLibraryStatsCommand.Execute(null) );
                break; 
            case 102:
                selectedFrag = new LastFmInfoFragment( MyViewModel);
                tag = "LastFMFragment";
                break; 
            case 103:
                selectedFrag = new SettingsFragment("settingsTrans", MyViewModel);
                tag = "SettingsFragment";
                break;
        }

        if (selectedFrag != null)
        {
            SupportFragmentManager.BeginTransaction()
                .SetCustomAnimations(Android.Resource.Animation.FadeIn, Android.Resource.Animation.FadeOut)
                .Replace(_contentContainer.Id, selectedFrag, tag)
                .AddToBackStack(tag)
                .Commit();
        }
    }

    public void OpenDrawer()
    {
        _drawerLayout.OpenDrawer(GravityCompat.Start);
    }

    
    public void NavigateTo(Fragment fragment, string tag)
    {
        var trans = SupportFragmentManager.BeginTransaction();
        trans.SetCustomAnimations(
            Android.Resource.Animation.FadeIn,
            Android.Resource.Animation.FadeOut,
            Android.Resource.Animation.FadeIn,
            Android.Resource.Animation.FadeOut);

        trans.Replace(_contentContainer.Id, fragment, tag);
        trans.AddToBackStack(tag);
        trans.Commit();


        // Update Nav Bar Visibility
        // Example: Hide Nav Bar on "Now Playing" or specific deep dives
        bool showNav = tag == "HomePageFragment" || tag == "GraphFragment";
        ToggleNavBar(showNav);
    }
    public void ToggleNavBar(bool show)
    {
        if (NavBar == null || _sheetContainer == null) return;

        // Calculate height: If NavBar.Height is 0, use density math (80dp)
        int navHeight = NavBar.Height;
        if (navHeight == 0) navHeight = (int)(80 * Resources.DisplayMetrics.Density);

        var sheetParams = (CoordinatorLayout.LayoutParams)_sheetContainer.LayoutParameters;

        if (show)
        {
            if (NavBar.Visibility != ViewStates.Visible)
            {
                NavBar.Visibility = ViewStates.Visible;
                NavBar.Alpha = 0f;
                NavBar.Animate()?.Alpha(1f).SetDuration(200).Start();
            }

           
        }
        else
        {
            if (NavBar.Visibility == ViewStates.Visible)
            {
                NavBar.Animate()?.Alpha(0f).SetDuration(200).WithEndAction(new Java.Lang.Runnable(() =>
                {
                    NavBar.Visibility = ViewStates.Gone;
                })).Start();
            }

            // Reset Margin to 0 (Rock Bottom)
            if (sheetParams.BottomMargin != 0)
            {
                sheetParams.BottomMargin = 0;
                _sheetContainer.LayoutParameters = sheetParams;
                _sheetContainer.RequestLayout();
            }
        }
    }

    internal class NavLayoutListener : Java.Lang.Object, ViewTreeObserver.IOnPreDrawListener
    {
        private readonly TransitionActivity _act;
        public NavLayoutListener(TransitionActivity act) { _act = act; }

        public bool OnPreDraw()
        {
            if (_act.NavBar.Height > 0)
            {
                _act.NavBar.ViewTreeObserver.RemoveOnPreDrawListener(this);
                // Height is now known, apply the overlap fix
                _act.ToggleNavBar(true);
            }
            return true;
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
            //if (newState == BottomSheetBehavior.StateExpanded)
                
            //    //_fragment.SetInputActive(true);
            //else if (newState == BottomSheetBehavior.StateCollapsed)
                //_fragment.SetInputActive(false);
        }

        public override void OnSlide(View bottomSheet, float slideOffset)
        {
            // slideOffset: 0.0 (Collapsed) -> 1.0 (Expanded)
            //_fragment.AnimateTransition(slideOffset);
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

            Window.SetStatusBarColor(UiBuilder.ThemedBGColor(this.ApplicationContext));
            Window.SetNavigationBarColor(UiBuilder.ThemedBGColor(this.ApplicationContext));

            SetupBackNavigation();
            // Log that the activity resumed
            Console.WriteLine("TransitionActivity: OnResume called.");

        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    const int REQUEST_OPEN_FOLDER = 100;
    TaskCompletionSource<string?>? _folderPickerTcs;
    private NavigationView _navigationView;
    private CoordinatorLayout _mainContentCoordinator;

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
        MyViewModel.OnAppClosing();
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


                RxSchedulers.UI.ScheduleToUI(() =>
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
        new MaterialAlertDialogBuilder(this)?
            .SetTitle("Startup Error")?
            .SetMessage($"Failed to load dependencies:\n{ex.Message}")?
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