using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Dimmer.DimmerAudio;
using Dimmer.Interfaces;
using static Android.Media.MediaCodec;

namespace Dimmer.Droid;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity//, IAudioActivity
{

    public event StatusChangedEventHandler StatusChanged;
    public event BufferingEventHandler Buffering;
    public event CoverReloadedEventHandler CoverReloaded;
    public event PlayingEventHandler Playing;
    public event PlayingChangedEventHandler PlayingChanged;
    public event PositionChangedEventHandler PositionChanged;

    MediaPlayerServiceConnection? _serviceConnection;
    Intent? _serviceIntent;
    //public ExoPlayerServiceBinder Binder 
    //{ 
    //    get => throw new NotImplementedException(); set => throw new NotImplementedException();
    //}
    private ExoPlayerServiceBinder? _binder;
    public ExoPlayerServiceBinder? Binder
    {
        get => _binder
               ?? throw new InvalidOperationException("Service not bound yet");
        set => _binder = value;
    }
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        var audioSvc = IPlatformApplication.Current.Services.GetService<IDimmerAudioService>()
    as IAudioActivity
    ?? throw new InvalidOperationException("AudioService missing");


        // 1) Start the foreground service
        _serviceIntent = new Intent(this, typeof(ExoPlayerService));
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            StartForegroundService(_serviceIntent);
        else
            StartService(_serviceIntent);
        _serviceConnection = new MediaPlayerServiceConnection(audioSvc);
        //var conn = new MediaPlayerServiceConnection(audioSvc);
        BindService(_serviceIntent, _serviceConnection, Bind.AutoCreate);

        // 3) Hook up your UI or console handlers
        StatusChanged   += OnStatusChanged;
        Buffering       += OnBuffering;
        CoverReloaded   += OnCoverReloaded;
        Playing         += OnPlaying;
        PlayingChanged  += OnPlayingChanged;
        PositionChanged += OnPositionChanged;
    }
    //// 1) bind to the service, passing *this* as the IAudioActivity
    //var intent = new Intent(this, typeof(ExoPlayerService));
    //    connection = new MediaPlayerServiceConnection(this);
    //    BindService(intent, connection, Bind.AutoCreate);

    //    // 2) subscribe your UI handlers to your own events
    //    StatusChanged   += OnStatusChanged;
    //    Buffering       += OnBuffering;
    //    PlayingChanged  += OnPlayingChanged;
    //    PositionChanged += OnPositionChanged;
    //    CoverReloaded   += OnCoverReloaded;
    //    Playing         += OnPlaying;
    //}
    protected override void OnDestroy()
    {
        if (_serviceConnection != null)
        {
            UnbindService(_serviceConnection);
            _serviceConnection.Disconnect();
        }
        base.OnDestroy();
    }
    public void OnStatusChanged(object sender, EventArgs e)
    {
        Console.WriteLine("▶ StatusChanged");
    }

    public void OnBuffering(object sender, EventArgs e)
    {
        Console.WriteLine("▶ Buffering");
    }

    public void OnCoverReloaded(object sender, EventArgs e)
    {
        Console.WriteLine("▶ CoverReloaded");
    }

    public void OnPlaying(object sender, EventArgs e)
    {
        Console.WriteLine("▶ Playing");
    }

    public void OnPlayingChanged(object sender, bool isPlaying)
    {
        Console.WriteLine($"▶ IsPlaying={isPlaying}");
    }

    public void OnPositionChanged(object sender, long positionMs)
    {
        Console.WriteLine($"▶ Position={positionMs}ms");
    }

    //// your handlers:
    //void OnStatusChanged(object s, EventArgs e) => Console.WriteLine("▶ StatusChanged");
    //void OnBuffering(object s, EventArgs e) => Console.WriteLine("▶ Buffering");
    //void OnCoverReloaded(object s, EventArgs e) => Console.WriteLine("▶ CoverReloaded");
    //void OnPlaying(object s, EventArgs e) => Console.WriteLine("▶ Playing");
    //void OnPlayingChanged(object s, bool isPlaying) => Console.WriteLine($"▶ IsPlaying={isPlaying}");
    //void OnPositionChanged(object s, long positionMs) => Console.WriteLine($"▶ Position={positionMs}ms");
}
