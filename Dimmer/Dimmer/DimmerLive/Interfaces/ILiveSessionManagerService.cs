

namespace Dimmer.DimmerLive.Interfaces;

public interface ILiveSessionManagerService
{
    IObservable<IChangeSet<UserDeviceSession, string>> OtherAvailableDevices { get; }
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
    Task SyncDeviceStateAsync();
}