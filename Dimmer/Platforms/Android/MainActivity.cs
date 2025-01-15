﻿using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Dimmer_MAUI.Platforms.Android.CurrentActivity;
using Dimmer_MAUI.Platforms.Android.MAudioLib;

namespace Dimmer_MAUI;
[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]

public class MainActivity : MauiAppCompatActivity, IAudioActivity
{
    MediaPlayerServiceConnection mediaPlayerServiceConnection;

    public MediaPlayerService MediaPlayerService { get; set; }

    public event StatusChangedEventHandler StatusChanged;
    public event CoverReloadedEventHandler CoverReloaded;
    public event PlayingEventHandler Playing;
    public event BufferingEventHandler Buffering;

    public AndroidX.Media3.Session.IMediaController CustomMediController {get;set;}

    HomePageVM ViewModel { get; set; }
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

        if (!Android.OS.Environment.IsExternalStorageManager)
        {
            Intent intent = new Intent();
            intent.SetAction(Android.Provider.Settings.ActionManageAppAllFilesAccessPermission);
            Android.Net.Uri uri = Android.Net.Uri.FromParts("package", this.PackageName!, null);
            intent.SetData(uri);
            StartActivity(intent);
        }

        //Window.SetStatusBarColor(Android.Graphics.Color.ParseColor("#000000"));
#if RELEASE
        Window.SetStatusBarColor(Android.Graphics.Color.Black);
#elif DEBUG
        Window.SetStatusBarColor(Android.Graphics.Color.ParseColor("#861B2D"));
#endif
        Window.SetNavigationBarColor(Android.Graphics.Color.Black);
        ////Window.SetNavigationBarColor(Android.Graphics.Color.ParseColor("#0C0E0D"));
        CrossCurrentActivity.Current.Init(this, savedInstanceState);

        //NotificationHelper.CreateNotificationChannel(Platform.AppContext);

        ViewModel = IPlatformApplication.Current!.Services.GetService<HomePageVM>()!;
        //var e = IPlatformApplication.Current!.Services.GetService<MediaPlayerService>()!;

        //MediaPlayerService = e;
    }

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        //use this to do something when user taps notif bar
    }
    protected override async void OnStop()
    {
        base.OnStop();

        var homeVM = IPlatformApplication.Current!.Services.GetService<HomePageVM>()!;
        await homeVM.ExitingApp();
    }
    public void test()
    {
        
    }
}