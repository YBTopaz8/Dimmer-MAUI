#pragma warning disable CS0618
#pragma warning disable CA1422
namespace Dimmer.DimmerAudio;

using Android.Media;

[BroadcastReceiver(Exported = true)]
public partial class AudioBecomingNoisyReceiver : BroadcastReceiver
{
    private readonly IDimmerAudioService? _audioService;

    public AudioBecomingNoisyReceiver() { } // Required for Android Manifest
    public AudioBecomingNoisyReceiver(IDimmerAudioService audioService)
    {
        _audioService = audioService;
    }

    public override void OnReceive(Context? context, Intent? intent)
    {
        if (intent?.Action == AudioManager.ActionAudioBecomingNoisy)
        {
            // The user just ripped their headphones out. PAUSE IMMEDIATELY.
            _ = _audioService?.PauseAsync();
        }
    }
}