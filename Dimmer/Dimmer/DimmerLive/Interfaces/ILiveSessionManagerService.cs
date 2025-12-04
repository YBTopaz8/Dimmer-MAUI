namespace Dimmer.DimmerLive.Interfaces;

public interface ILiveSessionManagerService
{
    // A property to observe the user's other available devices
    IObservable<IChangeSet<UserDeviceSession, string>> OtherAvailableDevices { get; }
    // An observable that fires when a new session transfer request arrives for this device
    IObservable<DimmerSharedSong> IncomingTransferRequests { get; }

    Task RegisterCurrentDeviceAsync();
    Task MarkCurrentDeviceInactiveAsync();
    Task InitiateSessionTransferAsync(UserDeviceSession targetDevice, DimmerSharedSong currentSongState);
    void StartListeners();
    void StopListeners();
    Task AcknowledgeTransferCompleteAsync(DimmerSharedSong transferredSong);
}