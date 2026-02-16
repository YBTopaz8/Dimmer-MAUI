

namespace Dimmer.DimmerLive.Interfaces;

public interface ILiveSessionManagerService
{
    // A property to observe the user's other available devices
    IObservable<IChangeSet<UserDeviceSession, string>> OtherAvailableDevices { get; }
    // An observable that fires when a new session transfer request arrives for this device
    IObservable<DimmerSharedSong> IncomingTransferRequests { get; }
    UserDeviceSession ThisDeviceSession { get; }

    Task RegisterCurrentDeviceAsync();
    Task MarkCurrentDeviceInactiveAsync();
    void StartListeners();
    void StopListeners();
    Task AcknowledgeTransferCompleteAsync(DimmerSharedSong transferredSong);
    Task InitiateSessionTransferAsync(UserDeviceSession targetDevice, DimmerPlayEventView currentSongView);
    Task RestoreBackupAsync(string backupObjectId);

    Task<ParseObject?> GetMyReferralCodeAsync();
    Task<ParseObject?> GenerateReferralCodeAsync();
    Task<string> CreateFullBackupAsync();
    Task<List<BackupMetadata>> GetAvailableBackupsAsync();
}