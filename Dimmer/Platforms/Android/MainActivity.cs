using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Dimmer_MAUI.Platforms.Android.MAudioLib;
using Dimmer_MAUI.Platforms.Android.CurrentActivity;

namespace Dimmer_MAUI;
[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]

public class MainActivity: MauiAppCompatActivity, IAudioActivity
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
        Window.SetStatusBarColor(Android.Graphics.Color.ParseColor("#483D8B"));
        Window.SetNavigationBarColor(Android.Graphics.Color.ParseColor("#483D8B"));
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
    
}
