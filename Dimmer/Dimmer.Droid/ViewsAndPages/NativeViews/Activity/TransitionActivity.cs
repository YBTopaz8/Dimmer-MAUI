

using Android.Views.InputMethods;
using AndroidX.CoordinatorLayout.Widget;
using AndroidX.Core.View;
using AndroidX.DrawerLayout.Widget;
using AndroidX.Lifecycle;
using Dimmer.NativeServices;
using Dimmer.UiUtils;
using Dimmer.ViewsAndPages.NativeViews.DimmerLive;
using Dimmer.ViewsAndPages.NativeViews.DimmerLive.LastFMViews;
using Dimmer.ViewsAndPages.NativeViews.StatsSection;

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
        //SetupCsharpUi();
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
            MyViewModel.CurrentFragment = current;
        };

        CheckAndRequestPermissions();


        ProcessLifecycleOwner.Get().Lifecycle.AddObserver(new AppLifeCycleObserver());

    }
    private SmoothBottomBar _bottomBar;
    // Source - https://stackoverflow.com/a
    // Posted by rmirabelle, modified by community. See post 'Timeline' for change history
    // Retrieved 2026-01-18, License - CC BY-SA 4.0

    public static void hideKeyboard(TransitionActivity activity)
    {
        
        InputMethodManager? imm = (InputMethodManager?)activity.GetSystemService(InputMethodService);
        //Find the currently focused view, so we can grab the correct window token from it.
        View? view = activity.CurrentFocus ?? new View(activity);
        imm?.HideSoftInputFromWindow(view.WindowToken, 0);
    }

    public override void OnConfigurationChanged(Configuration newConfig)
    {
        base.OnConfigurationChanged(newConfig);
        Configuration cong = new Configuration();
        
        RefreshBottomSheet();
    }
    public override void OnWindowFocusChanged(bool hasFocus)
    {
        base.OnWindowFocusChanged(hasFocus);
        if(hasFocus)
        {

        }
        else
        {

        }
    }
    private void RefreshBottomSheet()
    {
        SheetBehavior.State = BottomSheetBehavior.StateHidden;

        var frag = SupportFragmentManager.FindFragmentByTag("NowPlayingFragment");
            if(frag is not null)
        {
            SupportFragmentManager.BeginTransaction()
                .Remove(frag)
                .CommitNow();
        }


        var nowPlayingFrag = new NowPlayingFragment(MyViewModel);
        SupportFragmentManager
            .BeginTransaction()
            .Replace(_sheetContainer.Id, nowPlayingFrag, "NowPlayingFragment")
            .CommitNow();
    }

    private void SetupCsharpUi()
    {
        // 1. Theme Colors
        var currentTheme = Resources?.Configuration?.UiMode & UiMode.NightMask;
        var bgColor = currentTheme == UiMode.NightYes ? Color.ParseColor("#121212") : Color.ParseColor("#F5F5F5");
        var barColor = Color.ParseColor("#2D2D30"); // Dark Grey Bar

        // 2. ROOT: CoordinatorLayout
        _mainContentCoordinator = new CoordinatorLayout(this)
        {
            LayoutParameters = new ViewGroup.LayoutParams(-1, -1)
        };
        _mainContentCoordinator.SetBackgroundColor(bgColor);

        // 3. CONTENT CONTAINER (Where Fragments Live)
        _contentContainer = new FrameLayout(this)
        {
            Id = View.GenerateViewId(),
            LayoutParameters = new CoordinatorLayout.LayoutParams(-1, -1)
        };
        // Add bottom margin (70dp) so content isn't covered by the bar
        var contentParams = (CoordinatorLayout.LayoutParams)_contentContainer.LayoutParameters;
        contentParams.BottomMargin = (int)(70 * Resources.DisplayMetrics.Density);
        _contentContainer.LayoutParameters = contentParams;
        MyStaticID = _contentContainer.Id;

        // 4. THE SMOOTH BOTTOM BAR
        _bottomBar = new SmoothBottomBar(this);
        _bottomBar.Id = View.GenerateViewId();

        // Layout Params & Behavior
        var barParams = new CoordinatorLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, (int)(70 * Resources.DisplayMetrics.Density));
        barParams.Gravity = (int)GravityFlags.Bottom;

        // Attach the Scroll Behavior we defined in the C# port
        //var scrollBehavior = new HideBottomViewOnScrollBehavior<SmoothBottomBar>();
        //barParams.Behavior = scrollBehavior;

        _bottomBar.LayoutParameters = barParams;

        // --- Styling ---
        _bottomBar.SetBarBackgroundColor(Color.Red);
        //_bottomBar.SetBarBackgroundColor(barColor);
        _bottomBar.SetTextColor(Color.White);
        _bottomBar.SetIndicatorColor(Color.ParseColor("#861B2D"));
        _bottomBar.SetIconTint(Color.ParseColor("#80FFFFFF"), Color.White);
        _bottomBar.SetBarCornerRadius(20); // 20dp corners

        // --- MENU POPULATION (The Fix) ---
        // We don't use PopupMenu anymore. We create the Items list directly.
        var items = new List<BottomBarItem>
    {
        new BottomBarItem("Home", AndroidX.Core.Content.ContextCompat.GetDrawable(this, Resource.Drawable.musicaba)),
        new BottomBarItem("Stats", AndroidX.Core.Content.ContextCompat.GetDrawable(this, Resource.Drawable.heart)),
        new BottomBarItem("LastFM", AndroidX.Core.Content.ContextCompat.GetDrawable(this, Resource.Drawable.lastfm)),
        new BottomBarItem("Settings", AndroidX.Core.Content.ContextCompat.GetDrawable(this, Resource.Drawable.settings))
    };

        // Pass the list to the bar
        _bottomBar.SetMenuItems(items);

        // --- LISTENERS ---
        _bottomBar.OnItemSelected += (s, pos) =>
        {
            // Map Index (0,1,2..) back to your IDs (100,101..)
            int navId = 100 + pos;
            NavigateToId(navId);
        };

        _bottomBar.OnItemReselected += (s, pos) =>
        {
            int navId = 100 + pos;
            if (navId == 100) // Home
            {
                var currentFrag = SupportFragmentManager.FindFragmentById(_contentContainer.Id);
                if (currentFrag is HomePageFragment homeFrag)
                {
                    homeFrag.ScrollToCurrent();
                }
            }
        };

        // 5. PLAYER SHEET CONTAINER
        _sheetContainer = new FrameLayout(this)
        {
            Id = View.GenerateViewId(),
            LayoutParameters = new CoordinatorLayout.LayoutParams(-1, -1)
            {
                Gravity = (int)GravityFlags.Bottom
            },

            Background = new ColorDrawable(Color.Transparent),
            BackgroundTintList = AppUtil.ToColorStateList(Color.Transparent),
            Elevation = 30 * Resources.DisplayMetrics.Density,
            Clickable = false,
            Focusable = false
        };

        SheetBehavior = new BottomSheetBehavior();
        // PeekHeight = Bar Height (70) + MiniPlayer Height (70) = 140dp
        SheetBehavior.PeekHeight = (int)(140 * Resources.DisplayMetrics.Density);
        var sheetParams = (CoordinatorLayout.LayoutParams)_sheetContainer.LayoutParameters;
        sheetParams.Behavior = SheetBehavior;
        
        // 6. ADD VIEWS (Order determines Z-Index)
        _mainContentCoordinator.AddView(_contentContainer);
      
        _mainContentCoordinator.AddView(_bottomBar);
        _mainContentCoordinator.AddView(_sheetContainer); // Sheet sits ON TOP of the bar

        // Set Content
        SetContentView(_mainContentCoordinator);
        ViewCompat.SetOnApplyWindowInsetsListener(_mainContentCoordinator, this);
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
        _navigationView.Menu.Add(0, 101, 0, "Library").SetIcon(Resource.Drawable.heart);
        _navigationView.Menu.Add(0, 102, 0, "Last FM").SetIcon(Resource.Drawable.lastfm);
        _navigationView.Menu.Add(0, 103, 0, "Settings").SetIcon(Resource.Drawable.settings);
        _navigationView.Menu.Add(0, 104, 0, "Dimmer Cloud").SetIcon(Resource.Drawable.cloudbolt);

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
    public WindowInsetsCompat? OnApplyWindowInsets(View? v, WindowInsetsCompat? insets)
    {
        if (insets is not null)
        {
            var bars = insets.GetInsets(WindowInsetsCompat.Type.SystemBars());
            if (bars is not null)
            {
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
                CoordinatorLayout.LayoutParams? lp = (CoordinatorLayout.LayoutParams?)_sheetContainer.LayoutParameters;
                lp?.BottomMargin = _systemBarBottom;
                _sheetContainer.LayoutParameters = lp;
                SheetBehavior.PeekHeight = miniPlayerHeight + _systemBarBottom;
            }
        }
        return WindowInsetsCompat.Consumed;
    }
    public void NavToHomeDirectly()
    {
        if (SupportFragmentManager.FindFragmentByTag("HomePageFragment") != null)
        {
            SupportFragmentManager.PopBackStack("HomePageFragment", 0);
            return;
        }
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

                selectedFrag = new LibraryStatsFragment(MyViewModel);
                tag = "LibraryStatsFragment";

                break; 
            case 102:
                
                selectedFrag = new LastFMLoginFragment( "toLastFMInfo", MyViewModel);
                tag = "LastFMFragment";
                break; 
            case 103:
                selectedFrag = new SettingsFragment("settingsTrans", MyViewModel);
                tag = "SettingsFragment";
                break;
            case 104:

                var viewModel = MainApplication.ServiceProvider.GetService<SessionManagementViewModel>();
                if (viewModel is not null)
                {
                    selectedFrag = new CloudDataFragment("SessionManagementTrans", viewModel);
                    tag = "SessionMgt";
                }
                break;
        }

        if (selectedFrag != null)
        {
            SupportFragmentManager.BeginTransaction()
                .SetCustomAnimations(Resource.Animation.m3_bottom_sheet_slide_in, Resource.Animation.m3_bottom_sheet_slide_out)
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
            Resource.Animation.m3_side_sheet_enter_from_left,
            Resource.Animation.m3_side_sheet_exit_to_right,
            Resource.Animation.m3_bottom_sheet_slide_in,
            Resource.Animation.m3_motion_fade_exit);

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

        ProcessIntent(intent);
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
        intent.AddFlags(ActivityFlags.GrantReadUriPermission| ActivityFlags.GrantWriteUriPermission | ActivityFlags.GrantPersistableUriPermission);

        StartActivityForResult(intent, REQUEST_OPEN_FOLDER);

        return _folderPickerTcs.Task;
    }

    protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
    {
        base.OnActivityResult(requestCode, resultCode, data);
        if (requestCode == REQUEST_OPEN_FOLDER && resultCode == Result.Ok && data?.Data != null)
        {

            var uri = data.Data;

            // Persist permission so we can access it after reboot

            ContentResolver.TakePersistableUriPermission(uri,
            ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantWriteUriPermission);

            // Convert URI to a usable string/path (depends on your logic, 
            // but for SAF you usually keep the string URI)
            
            _folderPickerTcs?.TrySetResult(uri.ToString());
        }
        else
        {
            _folderPickerTcs?.TrySetResult(null); // Cancelled
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
                
                var currentFragment = MyViewModel.CurrentFragment as HomePageFragment;
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

    public void HandleBackPressInternal()
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
        if(intent.Action == "ShowMiniPlayer")
        {
            SheetBehavior.State = BottomSheetBehavior.StateExpanded;
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


                RxSchedulers.UI.ScheduleTo(() =>
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

class AppLifeCycleObserver : Java.Lang.Object, ILifecycleEventObserver
{
    [Lifecycle.Event.OnStart]
    public void OnForeground()
    {

    }
    [Lifecycle.Event.OnStop]
    public void OnBackground()
    {

    }

    

    public void OnStateChanged(ILifecycleOwner source, Lifecycle.Event e)
    {
    
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