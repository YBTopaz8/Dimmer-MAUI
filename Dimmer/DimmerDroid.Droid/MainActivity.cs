using Dimmer.Interfaces.Services.Interfaces.FileProcessing.FileProcessorUtils;
using Dimmer.NativeServices;
using Dimmer.Views.LastFM;
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

    BaseViewModelAnd? MyViewModel { get; set; }
    public MainActivity()
    {
      }

    const int REQUEST_AUDIO_PERMS = 99;
    const int REQUEST_STORAGE_PERMS = 98;

    private object? _onBackInvokedCallback;

    protected override void OnDestroy()
    {
        _serviceStarter?.Dispose();

        base.OnDestroy();

    }
    public static event Action? AudioStoragePermissionGranted;
    public override void OnRequestPermissionsResult(
    int requestCode,
    string[] permissions,
    [Android.Runtime.GeneratedEnum] Permission[] grantResults)
    {
        base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

        if (requestCode == REQUEST_AUDIO_PERMS && AndroidPermissionsService.HasAudioStoragePermission())
        {
            AudioStoragePermissionGranted?.Invoke();
        }
    }
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        Dimmer.Utils.UiThreads.InitializeMainHandler();
        ProcessIntent(Intent);
        SetupBackNavigation();

        // 1. Single unified permissions check
        if (!AndroidPermissionsService.HasAllPermissions())
        {
            AndroidPermissionsService.RequestAllPermissions(this, REQUEST_AUDIO_PERMS);
        }

        // 2. Do NOT run heavy DI synchronously in OnCreate.
        // Post to the handler so MainActivity finishes its lifecycle startup immediately!
        new Android.OS.Handler(Android.OS.Looper.MainLooper!).Post(() =>
        {
            TaggingUtils.PlatformSpecificScanner = AndroidPermissionsService.ScanAndroidFolderWithMediaStore;
            SetupService();
        });

        ThreadPool.SetMinThreads(4, 4);
        ConfigureJsonOptions();
        TaggingUtils.PlatformSpecificStreamCopier = (treeUriString, fileName, sourceFilePath) =>
        {
            var context = Android.App.Application.Context;
            var treeUri = Android.Net.Uri.Parse(treeUriString)!;
            var pickedDir = AndroidX.DocumentFile.Provider.DocumentFile.FromTreeUri(context, treeUri);
            var newFile = pickedDir?.CreateFile("application/octet-stream", fileName);
            if (newFile == null) throw new IOException("Could not create document in selected folder.");

            using var input = File.OpenRead(sourceFilePath);
            using var output = context.ContentResolver!.OpenOutputStream(newFile.Uri!)
                ?? throw new IOException("Could not open output stream.");
            input.CopyTo(output);
        };
        TaggingUtils.PlatformWriteStreamHook = (destFolderOrUri, fileName, mimeType, sourceStream) =>
        {
            var context = Android.App.Application.Context;

            if (destFolderOrUri.StartsWith("content://", StringComparison.OrdinalIgnoreCase))
            {
                var treeUri = Android.Net.Uri.Parse(destFolderOrUri);
                var pickedDir = AndroidX.DocumentFile.Provider.DocumentFile.FromTreeUri(context, treeUri);
                if (pickedDir == null || !pickedDir.CanWrite()) return false;

                // Replace existing file with same name if present
                pickedDir.FindFile(fileName)?.Delete();

                var newFile = pickedDir.CreateFile(mimeType, fileName);
                if (newFile == null) return false;

                using var outStream = context.ContentResolver?.OpenOutputStream(newFile.Uri);
                if (outStream == null) return false;

                sourceStream.CopyTo(outStream);
                outStream.Flush();
                return true;
            }

            Directory.CreateDirectory(destFolderOrUri);
            var destPath = Path.Combine(destFolderOrUri, fileName);
            using var fs = new FileStream(destPath, FileMode.Create, FileAccess.Write);
            sourceStream.CopyTo(fs);
            return true;
        };
    }

    //public void CheckAndRequestPermissions()
    //{
    //    var act = this;
    //    if (act == null)
    //    {
    //        Debug.WriteLine("Current activity is null. Cannot check permissions.");
    //        return;
    //    }
    //    if (!AndroidPermissionsService.HasAudioPermissions())
    //    {

    //        AndroidPermissionsService.RequestAudioPermissions(act, REQUEST_AUDIO_PERMS);
    //    }
    //    if (!AndroidPermissionsService.HasStoragePermissions())
    //    {
    //        AndroidPermissionsService.RequestStoragePermissions(act, REQUEST_STORAGE_PERMS);
    //    }

    //    if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu) // API 33+
    //    {
    //        if (CheckSelfPermission(Android.Manifest.Permission.PostNotifications) != Permission.Granted)
    //        {
    //            RequestPermissions(new[] { Android.Manifest.Permission.PostNotifications }, 100);
    //        }
    //    }

    //}



    public static JsonSerializerOptions JsonOptions => _jsonOptions;
    private static JsonSerializerOptions _jsonOptions;
    private IDisposable? _serviceStarter;

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
        var appContext = ApplicationContext!;
        var audioService = IPlatformApplication.Current!.Services.GetRequiredService<IDimmerAudioService>();

        _serviceStarter = audioService.PlaybackStateObs
         .Where(state => state == DimmerPlaybackState.Playing)
         .Take(1)
         .Subscribe(_ =>
         {
             var intent = new Intent(appContext, typeof(DimmerCompatMediaService));
             if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
                 appContext.StartForegroundService(intent);
             else
                 appContext.StartService(intent);
         });
    }
    private void ProcessIntent(Android.Content.Intent? intent)
    {
        if (intent == null || string.IsNullOrEmpty(intent.Action))
        {
            return;
        }
        if (MyViewModel is null) return;
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
        if (MyViewModel is null)
        {
            MyViewModel = IPlatformApplication.Current!.Services.GetRequiredService<BaseViewModelAnd>();

        }
        if (MyViewModel.IsNowPlayingBtmSheetOpened)
    {
        return;
    }
        Type typeOfCurrentPage = Shell.Current.CurrentPage.GetType();

    if (typeOfCurrentPage == typeof(HomePage))
    {
            if(MyViewModel.HomePageIndex != 0)
            {
                MyViewModel.HomePageIndex = 0;
                return;
            }
            new MaterialAlertDialogBuilder(this)?
                                    .SetTitle("Exit Dimmer")?
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
        return;
    }
        if (
            typeOfCurrentPage == typeof(AllArtistsPage) ||
            typeOfCurrentPage == typeof(AllAlbumsPage) ||
            typeOfCurrentPage == typeof(LastFMHomePage)||
            typeOfCurrentPage == typeof(SettingsPage))
        {
            new MaterialAlertDialogBuilder(this)?
                        .SetTitle("Confirm Action")?
                        .SetMessage("Return to Home Page?")?
                        .SetPositiveButton(
            "Confirm",
                async (s, e) =>
            {
                await Shell.Current.GoToAsync("//HomePage");

            })
            .SetNegativeButton(
                "Cancel",
                (s, e) =>
                { /* Do nothing */
                })
            .Show();
        }


    Debug.WriteLine(Shell.Current.Items.Count);
            //await Shell.Current.Navigation.PopAsync(true);
            await  Shell.Current.GoToAsync("..");
     
           
});

        // Note: Ensure _onBackInvokedCallback is defined as 'object' or inside this scope 
        // to avoid field-level verification issues on older phones.

        OnBackInvokedDispatcher.RegisterOnBackInvokedCallback(
            IOnBackInvokedDispatcher.PriorityDefault,
            (IOnBackInvokedCallback)_onBackInvokedCallback
        );


    }


    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);

        ProcessIntent(intent);
    }

    protected override void OnResume()
    {
        base.OnResume();
        if (MyViewModel is null) return;
        MyViewModel.IsBackGrounded = false;
    }
    protected override void OnPause()
    {
        base.OnPause();
        Debug.WriteLine("paused");
        if (MyViewModel is null) return;
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


