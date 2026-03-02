

using Android.Views.InputMethods;
using AndroidX.CoordinatorLayout.Widget;
using AndroidX.DrawerLayout.Widget;
using AndroidX.Lifecycle;
using ATL;
using Dimmer.NativeServices;
using Dimmer.ViewsAndPages.NativeViews.Adapters;
using Dimmer.ViewsAndPages.NativeViews.DeviceTransfer;
using Dimmer.ViewsAndPages.NativeViews.DimmerEvents;
using Dimmer.ViewsAndPages.NativeViews.DimmerLive;
using Dimmer.ViewsAndPages.NativeViews.DimmerLive.LastFMViews;
using Dimmer.ViewsAndPages.NativeViews.Stats;
using Google.Android.Material.BottomNavigation;
using Google.Android.Material.ImageView;
using Google.Android.Material.Navigation;
using Google.Android.Material.Shape;
using ImageButton = Android.Widget.ImageButton;

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
public class TransitionActivity :  AppCompatActivity, IOnApplyWindowInsetsListener
{
    public BottomSheetBehavior SheetBehavior { get; private set; }
     private FrameLayout _sheetContainer;
    private FrameLayout _contentContainerFrameLayout;
    public TransitionActivity() { }
    public static int MyStaticID;
    private object? _onBackInvokedCallback; // For API 33+
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



    protected override void OnPause()
    {
        base.OnPause();
        Debug.WriteLine("pauised");
        MyViewModel.IsBackGrounded = true;
    }

    protected override void OnStop()
    {
        base.OnStop();
        Debug.WriteLine("stopped");
    }

    protected override void OnNightModeChanged(int mode)
    {
        base.OnNightModeChanged(mode);

        Debug.WriteLine(mode);
    }
    
    protected override void OnSaveInstanceState(Bundle outState)
    {
        base.OnSaveInstanceState(outState);

         
    }
    LinearLayoutManager layoutManager;
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

            if (BaseContext is not null)
            {
                MyViewModel.HomeAdapter = new HomePageAdapter(this.BaseContext,
                    MyViewModel, startFragment
                    );
            }
            SupportFragmentManager
                .BeginTransaction()
                .Replace(_contentContainerFrameLayout.Id, startFragment, "HomePageFragment")
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
            
            Fragment? current = SupportFragmentManager.FindFragmentById(_contentContainerFrameLayout.Id);
            MyViewModel.CurrentFragment = current;
        };

        CheckAndRequestPermissions();


        ProcessLifecycleOwner.Get().Lifecycle.AddObserver(new AppLifeCycleObserver());
       
    }
    private SmoothBottomBar _bottomBar;
  

    public static void HideKeyboard(TransitionActivity activity)
    {
        
        InputMethodManager? imm = (InputMethodManager?)activity.GetSystemService(InputMethodService);
       
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


    DrawerLayout _drawerLayout;
    private void SetupDrawerLayout()
    {
        var currentTheme = Resources?.Configuration?.UiMode & UiMode.NightMask;
        var bgColor = currentTheme == UiMode.NightYes ? Color.ParseColor("#121212") : Color.ParseColor("#F5F5F5");
        var drawerBgColor = currentTheme == UiMode.NightYes ? Color.ParseColor("#1E1E1E") : Color.White;
        _drawerLayout = new DrawerLayout(this)
        {
            LayoutParameters = new ViewGroup.LayoutParams(-1, -1),
        };
        _drawerLayout.Id = View.GenerateViewId();

        _mainContentCoordinatorLayout = new CoordinatorLayout(this)
        {
            LayoutParameters = new DrawerLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent),
        };
        _mainContentCoordinatorLayout.SetBackgroundColor(bgColor);

        // 2a. Fragment Container (The Page)
        _contentContainerFrameLayout = new FrameLayout(this)
        {
            Id = View.GenerateViewId(),
            LayoutParameters = new CoordinatorLayout.LayoutParams(-1, -1)
        };
        _contentContainerFrameLayout.Id = Resource.Id.custom_fragment_container;
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
        _mainContentCoordinatorLayout.AddView(_contentContainerFrameLayout);
        _mainContentCoordinatorLayout.AddView(_sheetContainer);

        var drawerContainer = new LinearLayout(this)
        {
            Orientation = Orientation.Vertical,
            LayoutParameters = new DrawerLayout.LayoutParams(AppUtil.DpToPx(300), ViewGroup.LayoutParams.MatchParent) // Standard Drawer Width
            {
                Gravity = (int)GravityFlags.Start
            }
        };
        drawerContainer.SetBackgroundColor(drawerBgColor);
        drawerContainer.Clickable = true;



        // 3. Navigation View ( The Side Menu)
        _navigationView = new NavigationView(this)
        {
            LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, 1f) // Weight 1 to take remaining space
        };

        // Setup Menu Items
        _navigationView.Menu?.Add(0, 100, 0, "Home")?.SetIcon(Resource.Drawable.musicaba);
        _navigationView.Menu?.Add(0, 101, 0, "Library")?.SetIcon(Resource.Drawable.heart);
        _navigationView.Menu?.Add(0, 102, 0, "Last FM")?.SetIcon(Resource.Drawable.lastfm);
        _navigationView.Menu?.Add(0, 104, 0, "Play History")?.SetIcon(Resource.Drawable.time);
        _navigationView.Menu?.Add(0, 103, 0, "Settings")?.SetIcon(Resource.Drawable.settings);
        //_navigationView.Menu?.Add(0, 104, 0, "Device Transfer")?.SetIcon(Resource.Drawable.media3_icon_share);

        //_navigationView.Menu?.Add(0, 105, 0, "Dimmer Cloud")?.SetIcon(Resource.Drawable.cloudbolt);
        
        // Handle Clicks
        _navigationView.NavigationItemSelected += (s, e) =>
        {
            NavigateToId(e.MenuItem.ItemId);
            _drawerLayout.CloseDrawer(GravityCompat.Start); // Close after click
        };

        var headerView = new LinearLayout(this)
        {
            Orientation = Orientation.Vertical,
            LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, AppUtil.DpToPx(200)) // Height: 200dp
        };
        headerView.SetBackgroundColor(Color.ParseColor("#2D2D30")); // Dark Header BG
        headerView.SetGravity(GravityFlags.Bottom);
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



        var bottomProfileLayout = new LinearLayout(this)
        {
            Orientation = Orientation.Horizontal,
            LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, AppUtil.DpToPx(80))
        };
        bottomProfileLayout.SetPadding(AppUtil.DpToPx(16), AppUtil.DpToPx(16), AppUtil.DpToPx(16), AppUtil.DpToPx(16));
        bottomProfileLayout.SetGravity(GravityFlags.CenterVertical);

        // Top Border
        var border = new View(this) { LayoutParameters = new LinearLayout.LayoutParams(-1, AppUtil.DpToPx(1)) };
        border.SetBackgroundColor(Color.ParseColor("#33808080"));

        // Avatar (ImageView)
        avatarImg = new ShapeableImageView(this)
        {
            LayoutParameters = new LinearLayout.LayoutParams(AppUtil.DpToPx(48), AppUtil.DpToPx(48))
        };
        avatarImg.SetImageResource(Resource.Drawable.usercircle); // Placeholder
                                                                   // Make it round
        avatarImg.ShapeAppearanceModel = avatarImg.ShapeAppearanceModel.ToBuilder().SetAllCornerSizes(ShapeAppearanceModel.Pill!).Build();
        avatarImg.SetScaleType(ImageView.ScaleType.CenterCrop);
        avatarImg.SetBackgroundColor(Color.Gray);

        // Text Stack (Name + Device)
        var textStack = new LinearLayout(this)
        {
            Orientation = Orientation.Vertical,
            LayoutParameters = new LinearLayout.LayoutParams(0, -2, 1f) // Weight 1 to push Cog to right
        };
        ((LinearLayout.LayoutParams)textStack.LayoutParameters).LeftMargin = AppUtil.DpToPx(12);

        userNameTv = new TextView(this) { Text = MyViewModel.CurrentUserLocal?.Username ?? "User", TextSize = 16, Typeface = Typeface.DefaultBold };
        deviceTv = new TextView(this) { Text = Android.OS.Build.Model, TextSize = 12, Alpha = 0.7f };

        textStack.AddView(userNameTv);
        textStack.AddView(deviceTv);

        // Settings Cog
        var settingsBtn = new ImageButton(this)
        {
            LayoutParameters = new LinearLayout.LayoutParams(AppUtil.DpToPx(48), AppUtil.DpToPx(48))
        };
        settingsBtn.SetImageResource(Resource.Drawable.settings);
        settingsBtn.SetBackgroundColor(Color.Transparent);
        settingsBtn.Click += (s, e) =>
        {
            NavigateToId(103); // Settings ID
            _drawerLayout.CloseDrawer(GravityCompat.Start);
        };

        bottomProfileLayout.AddView(avatarImg);
        bottomProfileLayout.AddView(textStack);
        bottomProfileLayout.AddView(settingsBtn);

        // Add components to Drawer Container
        drawerContainer.AddView(_navigationView);
        drawerContainer.AddView(border); // Separator line
        drawerContainer.AddView(bottomProfileLayout);



        // 4. Assemble Root
        _drawerLayout.AddView(_mainContentCoordinatorLayout);

        _drawerLayout.AddView(drawerContainer);


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
                _contentContainerFrameLayout.SetPadding(
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

                var statsVm = MainApplication.ServiceProvider.GetService<StatisticsViewModel>();
                if (statsVm != null)
                {
                    selectedFrag = new GlobalLibraryStatsFragment(statsVm);
                    tag = "GlobalLibraryStatsFragment";
                }
                break;

            case 102:
                
                selectedFrag = new LastFMLoginFragment( MyViewModel);
                tag = "LastFMFragment";
                break; 
            case 103:
                selectedFrag = new SettingsFragment("settingsTrans", MyViewModel);
                tag = "SettingsFragment";
                break;
            case 105:

                var DeviceTransferViaBTViewModel = MainApplication.ServiceProvider.GetService<DeviceTransferViaBTViewModel>();
                if (DeviceTransferViaBTViewModel is not null)
                {
                    selectedFrag = new DevTransferFragment(DeviceTransferViaBTViewModel);
                    tag = "devTransMgt";
                }
                break;
            case 104:

                
                    selectedFrag = new HistoryFragment( MyViewModel);
                    tag = "HistoryFragment";
                
                break;
            case 106:

                var viewModel = MainApplication.ServiceProvider.GetService<SessionManagementViewModel>();
                if (viewModel is not null)
                {
                    selectedFrag = new LoginFragment("LoginFragmentTrans", MyViewModel);
                    tag = "SessionMgt";
                }
                break;
        }

        if (selectedFrag != null)
        {
            SupportFragmentManager.BeginTransaction()
                .SetCustomAnimations(Resource.Animation.m3_bottom_sheet_slide_in, Resource.Animation.m3_bottom_sheet_slide_out)
                .Replace(_contentContainerFrameLayout.Id, selectedFrag, tag)
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

        trans.Replace(_contentContainerFrameLayout.Id, fragment, tag);
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

        // Start service but delay binding
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            StartForegroundService(_serviceIntent);
        else
            StartService(_serviceIntent);

        // Bind with delay to avoid service startup contention
        Dimmer.Utils.UiThreads.AndroidUIHanlder?.PostDelayed(() =>
        {
            BindService(_serviceIntent, _serviceConnection, Bind.AutoCreate);
        }, 500);
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
                var startTime = Java.Lang.JavaSystem.CurrentTimeMillis();

                MyViewModel.InitializeAllVMCoreComponents();

                var duration = Java.Lang.JavaSystem.CurrentTimeMillis() - startTime;
                Console.WriteLine($"InitializeAppLogic took {duration}ms");
                if (duration > 2000)
                    Android.Util.Log.Warn("ANR_WARNING", $"OnCreate took {duration}ms - ANR risk!");
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


    IAuthenticationService? authService;
    ILiveSessionManagerService? sessionManager;
    LoginViewModel? loginVM;
    private async Task SetupSession()
    {
        try
        {
            // 1. Check Connectivity FIRST
            if (Connectivity.NetworkAccess != NetworkAccess.Internet)
            {
                Console.WriteLine("No internet. Skipping Parse auto-login.");
                return;
            }
            if(authService is null)
            {

                // 2. Resolve Services
                authService = MainApplication.ServiceProvider.GetService<IAuthenticationService>()!;
                 sessionManager = MainApplication.ServiceProvider.GetService<ILiveSessionManagerService>()!;
                 loginVM = MainApplication.ServiceProvider.GetService<LoginViewModel>();

                // 3. Attempt Auto-Login
                Console.WriteLine("Attempting Parse Auto-Login...");
                bool isLoggedIn = await authService.InitializeAsync(); // This checks SecureStorage tokens

                if (isLoggedIn)
                {
                    Console.WriteLine($"Auto-Login Successful for user: {authService.CurrentUserValue?.Username}");

                    // 4. Update UI Model
                    loginVM.CurrentUserOnline = authService.CurrentUserValue;

                    // 5. CRITICAL: Register Device Session
                    // This tells Parse "I am here, my Device ID is X, send me commands"
                    await sessionManager.RegisterCurrentDeviceAsync();

                    // 6. Sync Initial State (Queues, Playing Song)
                    await sessionManager.SyncDeviceStateAsync();

                    // 7. Open the WebSocket Listeners (LiveQuery)
                    sessionManager.StartListeners();

                    Console.WriteLine("Device Session & Listeners Active.");
                }
                else
                {
                    Console.WriteLine("Auto-Login failed or no token found.");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"BOOT ERROR: {ex.Message}");
            // Optional: Log to Analytics/Crashlytics
        }
    
    }


    private bool _isDialogActive = false;
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
            MyViewModel.IsBackGrounded = false;


            if (_isDialogActive)
                return;

            var typee = BaseViewModel.WindowActivationRequestTypeStatic;
            if (typee == "Confirm LastFM")
            {
                _isDialogActive = true;
                try
                {
                    await MyViewModel.CheckToCompleteActivation(typee);
                }
                finally
                {
                    // Ensure the flag is reset even if an error occurs
                    _isDialogActive = false;
                }
            }
            //await SetupSession();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    const int REQUEST_OPEN_FOLDER = 100;
    TaskCompletionSource<string?>? _folderPickerTcs;
    private NavigationView _navigationView;
    private CoordinatorLayout _mainContentCoordinatorLayout;
    private TextView userNameTv;
    private TextView deviceTv;
    private ShapeableImageView avatarImg;

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

    protected override async void OnDestroy()
    {
        if (_serviceConnection != null)
        {
            UnbindService(_serviceConnection);
         }
        if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu && _onBackInvokedCallback != null && _isBackCallbackRegistered)
        {
            OnBackInvokedDispatcher.UnregisterOnBackInvokedCallback((IOnBackInvokedCallback)_onBackInvokedCallback);
            _isBackCallbackRegistered = false;
        }

       await MyViewModel?.OnAppClosingAsync();
        base.OnDestroy();
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
    private void SetupBackNavigation()
    {
        // The check remains here
        if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu) // API 33+
        {
            // We call a SEPARATE method. 
            // The runtime won't look inside this method unless this line is reached.
            SetupBackNavigationApi33();
        }
        else
        {
            // API 32 and lower logic (OnBackPressed override usually)
        }
    }

    // This attribute is good practice, tells the compiler this is for API 33+
    [System.Runtime.Versioning.SupportedOSPlatform("android33.0")]
    private void SetupBackNavigationApi33()
    {
        // The dangerous code lives exclusively in here
        _onBackInvokedCallback = new BackInvokedCallback(() =>
        {
            var currentFragment = MyViewModel.CurrentFragment as HomePageFragment;
            if (currentFragment is null)
            {
                HandleBackPressInternal();
            }
            ;
        });

        // Note: Ensure _onBackInvokedCallback is defined as 'object' or inside this scope 
        // to avoid field-level verification issues on older phones.

        OnBackInvokedDispatcher.RegisterOnBackInvokedCallback(
            IOnBackInvokedDispatcher.PriorityDefault,
            (IOnBackInvokedCallback)_onBackInvokedCallback
        );

        _isBackCallbackRegistered = true;
    }
    public override void OnLowMemory()
    {
        base.OnLowMemory();
        //System.Diagnostics.Debugger.Break();
    }
    private void ProcessIntent(Android.Content.Intent? intent)
    {
        if (intent == null || string.IsNullOrEmpty(intent.Action))
        {
            return;
        }
        if (intent.Action == "ShowMiniPlayer")
        {
            if (MyViewModel.OpenMediaUIOnNotificationTap)
            {
                SheetBehavior.State = BottomSheetBehavior.StateExpanded;
            }
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
        Debug.WriteLine(e.TargetState);
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