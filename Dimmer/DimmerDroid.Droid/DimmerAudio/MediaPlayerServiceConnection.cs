namespace Dimmer.DimmerAudio;

/// <summary>
/// Connects the native Android ExoPlayerService to the cross-platform singleton AudioService proxy.
/// Its only job is to find the proxy and pass the service binder to it.
/// </summary>
public class MediaPlayerServiceConnection : Java.Lang.Object, IServiceConnection
{
    public MediaPlayerServiceConnection() { }

    public void OnServiceConnected(ComponentName? name, IBinder? service)
    {
        // Step 1: Check if the connection provided a valid binder.
        if (service is not ExoPlayerServiceBinder binder)
        {
            return;
        }

        // Step 2: Get the application-wide singleton instance of our AudioService proxy.
        // We need the concrete class to call the 'SetBinder' method.
        var audioServiceProxy = MainApplication.ServiceProvider.GetService<IDimmerAudioService>() as Dimmer.DimmerAudio.AudioService;
        // Step 3: Activate the proxy by giving it the binder.
        // The proxy's internal logic will then handle subscribing to all necessary events.
        audioServiceProxy?.SetBinder(binder);
    }

    public void OnServiceDisconnected(ComponentName? name)
    {
        // When the OS unexpectedly kills the service, we must deactivate the proxy.
        var audioServiceProxy = MainApplication.ServiceProvider.GetService<IDimmerAudioService>() as Dimmer.DimmerAudio.AudioService;
        // Setting the binder to null will cause the proxy to unsubscribe from the old service's events.
        audioServiceProxy?.SetBinder(null);
    }
}