using Android.Content;
using Android.OS;

namespace Dimmer.DimmerAudio;
public class MediaPlayerServiceConnection : Java.Lang.Object, IServiceConnection
{
    readonly IAudioActivity _activity;
    public MediaPlayerServiceConnection(IAudioActivity act) => _activity = act;
    public ExoPlayerServiceBinder? Binder => _binder;
    private ExoPlayerServiceBinder? _binder;
    private bool _isConnected = false;

    public void OnServiceConnected(ComponentName? name, IBinder? service)
    {
        Console.WriteLine("[MediaPlayerServiceConnection] onServiceConnected");
        if (_isConnected)
            return;
        if (service is ExoPlayerServiceBinder b)
        {
            _binder = b;
            _isConnected = true;
            _activity.Binder = b;
            var svc = b.Service;
            svc.StatusChanged   += _activity.OnStatusChanged;
            svc.Buffering       += _activity.OnBuffering;
            svc.CoverReloaded   += _activity.OnCoverReloaded;
            svc.SeekCompleted += _activity.OnSeekCompleted;
            svc.PlayingChanged  += _activity.OnPlayingChanged;
            svc.PositionChanged += _activity.OnPositionChanged;
        }
    }
    public void OnServiceDisconnected(ComponentName? name)
    {
        Console.WriteLine($"[MediaPlayerServiceConnection] Service Disconnected: {name?.ClassName}");
        // Clean up: Unsubscribe from events
        if (_isConnected && _binder?.Service != null)
        {
            var svc = _binder.Service;
            svc.StatusChanged   -= _activity.OnStatusChanged;
            svc.Buffering       -= _activity.OnBuffering;
            svc.CoverReloaded   -= _activity.OnCoverReloaded;
            svc.SeekCompleted  -= _activity.OnSeekCompleted;
            svc.PlayingChanged  -= _activity.OnPlayingChanged;
            svc.PositionChanged -= _activity.OnPositionChanged;
        }
        _activity.Binder = null;
        _binder = null;
        _isConnected = false;
    }

    /// <summary>
    /// Helper to manually disconnect and clean up listeners if needed before unbinding.
    /// </summary>
    public void Disconnect()
    {
        if (_isConnected && _binder?.Service != null)
        {
            OnServiceDisconnected(null); // Simulate disconnect for cleanup
        }
    }
}