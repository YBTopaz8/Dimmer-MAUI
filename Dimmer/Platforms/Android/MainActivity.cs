using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;

using AndroidX.Core.App;
using AndroidX.Core.Content;
using Dimmer_MAUI.Platforms.Android.CurrentActivity;

namespace Dimmer_MAUI;
[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]

public class MainActivity : MauiAppCompatActivity, IAudioActivity
{
    MediaPlayerServiceConnection mediaPlayerServiceConnection;

    public MediaPlayerServiceBinder Binder { get; set; }

    public event StatusChangedEventHandler StatusChanged;
    public event CoverReloadedEventHandler CoverReloaded;
    public event PlayingEventHandler Playing;
    public event BufferingEventHandler Buffering;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        if (DeviceInfo.Idiom == DeviceIdiom.Watch)
        {
            return;
        }
        var plat = DeviceInfo.Platform.ToString();
        var plate = DeviceInfo.Name.ToString();
        var platee = DeviceInfo.Manufacturer.ToString();

        //if (Build.VERSION.SdkInt >= BuildVersionCodes.R) // Android 11 (API 30) and above
        //{
        //    // Check if the app has "all files access" permission
        //    if (!Android.OS.Environment.IsExternalStorageManager)
        //    {
        //        // Request permission to manage all files (MANAGE_EXTERNAL_STORAGE)
        //        Intent intent = new Intent();
        //        intent.SetAction(Android.Provider.Settings.ActionManageAppAllFilesAccessPermission);
        //        Android.Net.Uri uri = Android.Net.Uri.FromParts("package", this.PackageName!, null);
        //        intent.SetData(uri);
        //        StartActivity(intent);
        //    }
        //}
        //else
        //{
        //    // For devices below Android 11, you don't need MANAGE_EXTERNAL_STORAGE,
        //    // Just request READ_EXTERNAL_STORAGE and WRITE_EXTERNAL_STORAGE as needed
        //    if (ContextCompat.CheckSelfPermission(this, Android.Manifest.Permission.ReadExternalStorage) == Permission.Granted)
        //    {
        //        // Access files using scoped storage or MediaStore
        //    }
        //    else
        //    {
        //        ActivityCompat.RequestPermissions(this, new string[] { Android.Manifest.Permission.ReadExternalStorage }, 101);
        //    }
        //}


        //Window.SetStatusBarColor(Android.Graphics.Color.ParseColor("#000000"));
#if RELEASE
        Window.SetStatusBarColor(Android.Graphics.Color.Black);
#elif DEBUG
        Window.SetStatusBarColor(Android.Graphics.Color.ParseColor("#861B2D"));
#endif
        Window.SetNavigationBarColor(Android.Graphics.Color.Black);
        ////Window.SetNavigationBarColor(Android.Graphics.Color.ParseColor("#0C0E0D"));
        CrossCurrentActivity.Current.Init(this, savedInstanceState);

        NotificationHelper.CreateNotificationChannel(Platform.AppContext);
        if (mediaPlayerServiceConnection is null)
        {
            MediaPlayerService mediaPlayerService = new MediaPlayerService();
            mediaPlayerService.MainAct = this;
            InitializeMedia();
            
        }

    }

    private void InitializeMedia()
    {
        mediaPlayerServiceConnection = new MediaPlayerServiceConnection(this);
        var mediaPlayerServiceIntent = new Intent(Platform.AppContext, typeof(MediaPlayerService));
        BindService(mediaPlayerServiceIntent, mediaPlayerServiceConnection, Bind.AutoCreate);
    }

    protected override async void OnStop()
    {
        base.OnStop();

        var homeVM = IPlatformApplication.Current!.Services.GetService<HomePageVM>()!;
        await homeVM.ExitingApp();
    }

}