using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using System.Diagnostics;

namespace Dimmer.Droid;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{


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
        HandleIntent(intent);
    }

    private static void HandleIntent(Intent? intent)
    {
        Console.WriteLine(intent?.Action?.ToString());
    }

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        if (DeviceInfo.Idiom == DeviceIdiom.Watch)
        {
            return;
        }
        if (!Android.OS.Environment.IsExternalStorageManager)
        {
            Intent intent = new Intent();
            intent.SetAction(Android.Provider.Settings.ActionManageAppAllFilesAccessPermission);
            Android.Net.Uri uri = Android.Net.Uri.FromParts("package", this.PackageName!, null)!;
            intent.SetData(uri);
            StartActivity(intent);
        }

        //Win
        // Ensure Window is not null before accessing it
        if (Window != null)
        {
            
#if ANDROID_35
            Window.SetStatusBarColor(Android.Graphics.Color.ParseColor("#861B2D"));
#else
            // Alternative implementation for Android versions >= 35
            // Add your custom logic here if needed
#endif
        }

        IAudioActivity? audioSvc = IPlatformApplication.Current!.Services.GetService<IDimmerAudioService>()
            as IAudioActivity
            ?? throw new InvalidOperationException("AudioService missing");

        // 1) Start the foreground service
        _serviceIntent = new Intent(this, typeof(ExoPlayerService));
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            StartForegroundService(_serviceIntent);
        else
            StartService(_serviceIntent);
        _serviceConnection = new MediaPlayerServiceConnection(audioSvc);
        BindService(_serviceIntent, _serviceConnection, Bind.AutoCreate);
    }
    protected override void OnDestroy()
    {
        if (_serviceConnection != null)
        {
            UnbindService(_serviceConnection);
            _serviceConnection.Disconnect();
        }
        base.OnDestroy();
    }
}
