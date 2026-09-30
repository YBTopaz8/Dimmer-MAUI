namespace Dimmer.DimmerAudio;

using Android.Support.V4.Media.Session;
using System.Diagnostics;

public partial class DimmerMediaSessionCallback : MediaSessionCompat.Callback
{
    private readonly IDimmerAudioService _audioService;

    // Define our custom action string for the Favorite button
    public const string ActionFavorite = "com.dimmer.action.FAVORITE";

    public DimmerMediaSessionCallback(IDimmerAudioService audioService)
    {
        _audioService = audioService;
    }

    public override void OnPlay() => _ = _audioService.PlayAsync(_audioService.CurrentPosition);
    public override void OnPause() => _ = _audioService.PauseAsync();

    // Handles the user dragging the seekbar in the notification!
    public override void OnSeekTo(long pos) => _ = _audioService.SeekAsync(pos / 1000.0);

    public override void OnSkipToNext()
    {
        if (_audioService is OwnAudioService srv) srv.TriggerNext();
    }

    public override void OnSkipToPrevious()
    {
        if (_audioService is OwnAudioService srv) srv.TriggerPrevious();
    }

    public override void OnCustomAction(string? action, Bundle? extras)
    {
        Debugger.Break();
        if (action == ActionFavorite)
        {
            if (_audioService is OwnAudioService srv) srv.TriggerFavorite();
        }
    }

}
