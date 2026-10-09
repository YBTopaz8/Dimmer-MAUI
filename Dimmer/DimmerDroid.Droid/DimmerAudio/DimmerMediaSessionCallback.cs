namespace Dimmer.DimmerAudio;

using Android.App.Admin;
using Android.Support.V4.Media.Session;
using DevExpress.XtraPrinting;
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


    public override async void OnSeekTo(long posMS)
    {
        double targetSec = posMS / 1000.0;
        if (_audioService != null)
        {
            await _audioService.SeekAsync(targetSec);
        }
    }
    public override void OnSkipToNext()
    {
        if (_audioService is OwnAudioService srv) srv.TriggerNext();
    }

    public override void OnSkipToPrevious()
    {
        if (_audioService is OwnAudioService srv) srv.TriggerPrevious();
    }
    public Action<double>? OnOptimisticSeek { get; set; }


    public override void OnCustomAction(string? action, Bundle? extras)
    {

        if (action == ActionFavorite)
        {
            if (_audioService is OwnAudioService srv)
            {
                
                srv.TriggerFavorite();
            }
        }
        else
        {
            Debug.WriteLine($"Unknown custom action: {action}");
        }
    }
}
