using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.DimmerLive;

public enum DataPackageType
{
    Backup,
    SessionTransfer
}

public class BluetoothDataPackage
{
    public DataPackageType Type { get; set; }
    public string PayloadJson { get; set; } // The serialized data
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.Now;
}

// Wrapper for your backup
public class FullBackupData
{
    public UserModelView User { get; set; }
    public AppStateModelView AppState { get; set; }

    public List<string?>? PlayBackSongTitleAndDurationId { get; set; }
    public List<string?>? FavoriteSongsTitleAndDurationId { get; set; }

    public DateTimeOffset BackupDate { get; set; } = DateTimeOffset.UtcNow;
    public string AppVersion { get; set; } = "1.0.0"; // Good for versioning migrations later
    public string Platform { get; set; }

    // The Data Lists
    public IEnumerable<SongModel> Songs { get; set; }
    public IEnumerable<DimmerPlayEvent> PlayEvents { get; set; }
    public IEnumerable<PlaylistModel> Playlists { get; set; }
    public IEnumerable<UserStats> Stats { get; set; }
    public AppStateModel Settings { get; set; }
}

public class BackupMetadata
{
    public string ObjectId { get; set; }
    public DateTime? CreatedAt { get; set; }
    public string FileUrl { get; set; }
    public int SongCount { get; set; }
    public int EventCount { get; set; }
    public string DeviceName { get; set; }
}
public interface IBluetoothService
{
    // Scans and returns list of paired devices
    Task<List<string>> GetPairedDevicesAsync();

    // Starts acting as a Server (waiting for incoming data)
    Task StartServerAsync();

    // Connects to a specific device (acting as Client)
    Task ConnectToDeviceAsync(string deviceName);

    // Sends the data
    Task SendDataAsync(BluetoothDataPackage data);

    // Disconnect
    void Disconnect();

    // Events
    event EventHandler<string> DataReceived;
    event EventHandler<string> StatusChanged;
}