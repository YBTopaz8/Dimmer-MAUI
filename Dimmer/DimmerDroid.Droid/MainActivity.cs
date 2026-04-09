using Android.App;
using Android.Content.PM;
using Android.OS;
using Dimmer.NativeServices;
using Google.Android.Material.Dialog;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DimmerDroid.Droid;

[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize |
    ConfigChanges.Density)]
public partial class MainActivity : MauiAppCompatActivity
{
    MediaPlayerServiceConnection? _serviceConnection;
    Intent? _serviceIntent;
    private ExoPlayerServiceBinder? _binder;
    BaseViewModelAnd MyViewModel { get; set; }
    public MainActivity()
    {
    }

    public ExoPlayerServiceBinder? Binder
    {
        get => _binder
               ?? throw new InvalidOperationException("Service not bound yet");
        set => _binder = value;


    }
    const int REQUEST_AUDIO_PERMS = 99;
    const int REQUEST_STORAGE_PERMS = 98;

    private object? _onBackInvokedCallback;
    private bool _isBackCallbackRegistered = false;
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        var curPlat = IPlatformApplication.Current;
        var serv = curPlat?.Services;
        if (serv != null)
        {
            MyViewModel = IPlatformApplication.Current!.Services.GetRequiredService<BaseViewModelAnd>();
        }
        Dimmer.Utils.UiThreads.InitializeMainHandler();
        ProcessIntent(Intent);
        SetupService();

        SetupBackNavigation();



        CheckAndRequestPermissions();     
        
        // Increase thread pool for background operations
        ThreadPool.SetMinThreads(4, 4);

        // Configure JsonSerializer for mobile
        ConfigureJsonOptions();
    }
    public static JsonSerializerOptions JsonOptions => _jsonOptions;
    private static JsonSerializerOptions _jsonOptions;
    private void ConfigureJsonOptions()
    {
        // Use these settings for better mobile performance
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
            MaxDepth = 32, // Limit depth for security/performance
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };
    }
    public void SetupService()
    {
        _serviceConnection = new MediaPlayerServiceConnection();
        _serviceIntent = new Intent(this, typeof(ExoPlayerService));

        // Start service but delay binding
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            StartForegroundService(_serviceIntent);
        else
            StartService(_serviceIntent);

        BindService(_serviceIntent, _serviceConnection, Bind.AutoCreate);

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
                throw new NotImplementedException("ShowMiniPlayer intent handling not implemented yet");
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

    public override void OnLowMemory()
    {
        base.OnLowMemory();
        //System.Diagnostics.Debugger.Break();
    }



    private void ShowCrashDialog(Exception ex)
    {
        new MaterialAlertDialogBuilder(this)?
            .SetTitle("Startup Error")?
            .SetMessage($"Failed to load dependencies:\n{ex.Message}")?
            .SetPositiveButton("Close", (s, e) => FinishAffinity())
            .Show();

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
        _onBackInvokedCallback = new BackInvokedCallback(async () =>
        {

            switch (Shell.Current.CurrentItem.Title)
            {
                case "Home":

                    new MaterialAlertDialogBuilder(this)?
                                .SetTitle("Exit App")?
                                .SetMessage("Close Application?")?
                                .SetPositiveButton(
                    "Exit",
                    async (s, e) =>
                    {
                        await MyViewModel.OnAppClosingAsync();
                        FinishAffinity();
                    })
                    .SetNegativeButton(
                        "Cancel",
                        (s, e) =>
                        { /* Do nothing */
                        })
                    .Show();
                    break;
                case "Artists":

                    new MaterialAlertDialogBuilder(this)?
                                .SetTitle("Confirm action")?
                                .SetMessage("Return to Home Page?")?
                                .SetPositiveButton(
                    "Confirm",
                     (s, e) =>
                    {
                        Shell.Current.CurrentItem = Shell.Current.Items.FirstOrDefault(i => i.Route == "HomePage"); 
                        
                    })
                    .SetNegativeButton(
                        "Cancel",
                        (s, e) =>
                        { /* Do nothing */
                        })
                    .Show();
                    break;

                case "Settings":

                new MaterialAlertDialogBuilder(this)?
                            .SetTitle("Confirm action")?
                            .SetMessage("Return to Home Page?")?
                            .SetPositiveButton(
                "Confirm",
                    (s, e) =>
                    {
                        Shell.Current.CurrentItem = Shell.Current.Items.FirstOrDefault(i => i.Route == "HomePage");

                    })
                .SetNegativeButton(
                    "Cancel",
                    (s, e) =>
                    { /* Do nothing */
                    })
                .Show();
                break;

                default:
                  await  Shell.Current.GoToAsync("..");
                    break;
            }
           
        });

        // Note: Ensure _onBackInvokedCallback is defined as 'object' or inside this scope 
        // to avoid field-level verification issues on older phones.

        OnBackInvokedDispatcher.RegisterOnBackInvokedCallback(
            IOnBackInvokedDispatcher.PriorityDefault,
            (IOnBackInvokedCallback)_onBackInvokedCallback
        );

        _isBackCallbackRegistered = true;
    }
    private void CheckAndRequestPermissions()
    {
        if (!AndroidPermissionsService.HasAudioPermissions())
        {
            // Show UI explanation? Or just request
            AndroidPermissionsService.RequestAudioPermissions(this, REQUEST_AUDIO_PERMS);
        }
        if (!AndroidPermissionsService.HasStoragePermissions())

        {
            AndroidPermissionsService.RequestStoragePermissions(this, REQUEST_STORAGE_PERMS);
        }
        
            // Permissions already granted, load music
            InitializeAppLogic();
        
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
}


