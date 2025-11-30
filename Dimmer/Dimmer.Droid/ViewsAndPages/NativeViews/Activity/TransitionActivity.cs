

using Dimmer.NativeServices;
using Dimmer.Utilities.Extensions;

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

        Android.Util.Log.Error("DIMMER_INIT2", "log2");
        base.OnCreate(savedInstanceState);
        //if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop) // Transitions API Level 21+
        //{
        //    Window?.RequestFeature(WindowFeatures.ContentTransitions);

        //    Transition? enterTransition = CreateTransition(PublicStats.EnterTransition);
        //    if (enterTransition != null)
        //    {
        //        Window?.EnterTransition = enterTransition;
        //    }

        //    Transition? exitTransition = CreateTransition(PublicStats.ExitTransition);
        //    if (exitTransition != null)
        //    {
        //        Window?.ExitTransition = exitTransition;
        //    }

        //     Transition? reenterTransition = CreateTransition(PublicStats.ReenterTransition);
        //    if (reenterTransition != null)
        //    {
        //        Window?.ReenterTransition = reenterTransition;
        //    }

        //     Transition? returnTransition = CreateTransition(PublicStats.ReturnTransition);
        //    if (returnTransition != null)
        //    {
        //        Window?.ReturnTransition = returnTransition;
        //    }

        //    Window?.AllowEnterTransitionOverlap = true;
        //    Window?.AllowReturnTransitionOverlap = true;

        //}


        Android.Util.Log.Error("DIMMER_INITeec", "logec");
        var container = new FrameLayout(this)
        {
            LayoutParameters = new FrameLayout.LayoutParams(
        ViewGroup.LayoutParams.MatchParent,
        ViewGroup.LayoutParams.MatchParent)
        };
        container.Id = Resource.Id.content;
        ////MyStaticID = container.Id;
        //container.Id = View.GenerateViewId();
        MyStaticID = container.Id;

        container.SetFitsSystemWindows(true);

        Android.Util.Log.Error("DIMMER_INITeew", "logew");

        var currentTheme = Resources?.Configuration?.UiMode & UiMode.NightMask;
        if (currentTheme == UiMode.NightYes)
            container.SetBackgroundColor(Color.ParseColor("#3E3E42"));
        else
            container.SetBackgroundColor(Color.ParseColor("#DAD9E0"));
        

        SetContentView(container);

        Android.Util.Log.Error("DIMMER_INITee", "loge");
        MainApplication.ServiceProvider ??= Bootstrapper.Init();

        Android.Util.Log.Error("DIMMER_INITe", "logx");
        try
        {
            MyViewModel = MainApplication.ServiceProvider.GetRequiredService<BaseViewModelAnd>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"CRITICAL DI ERROR: {ex}");

            new Android.App.AlertDialog.Builder(this)?
                .SetTitle("Startup Error")?
                .SetMessage($"Failed to load dependencies:\n{ex.Message}\n\nInner: {ex.InnerException?.Message}")?
                .SetPositiveButton("Close", (s, e) => FinishAffinity())?
                .Show();


            Android.Util.Log.Error("DIMMER_INITA", "loga Error");
        }



        Dimmer.Utils.UiThreads.InitializeMainHandler();

        if (savedInstanceState == null)
        {
            var startFragment = new HomePageFragment(MyViewModel);
            SupportFragmentManager
                .BeginTransaction()
                .Replace(container.Id, startFragment)
                .Commit();
        }

        Android.Util.Log.Error("DIMMER_INIT1", "log1" );

      

        Android.Util.Log.Error("DIMMER_INITOK2", "logOK2");
        ProcessIntent(Intent);

        _serviceConnection = new MediaPlayerServiceConnection();


        _serviceIntent = new Intent(this, typeof(ExoPlayerService));
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {

            this.StartForegroundService(_serviceIntent);

        }
        else
            StartService(_serviceIntent);
        BindService(_serviceIntent, _serviceConnection, Bind.AutoCreate);

        SetStatusBarColor();

        SetupBackNavigation();


        SupportFragmentManager.BackStackChanged += (s, e) =>
        {
            int count = SupportFragmentManager.BackStackEntryCount;

            if (count == 0)
            {
                 var homeFrag = SupportFragmentManager.FindFragmentByTag("HomePageFragment");
                MyViewModel.CurrentPage = homeFrag as Fragment;
            }
            else
            {
                 var topFrag = SupportFragmentManager.Fragments.LastOrDefault();
                MyViewModel.CurrentPage = topFrag;
            }
        };



        CheckAndRequestPermissions();
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