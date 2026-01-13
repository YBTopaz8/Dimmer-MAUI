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
    public List<DimmerPlayEventView> PlayEvents { get; set; }
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

    // Opens system Bluetooth settings for pairing
    Task OpenBluetoothSettingsAsync();

    // Events
    event EventHandler<string> DataReceived;
    event EventHandler<string> StatusChanged;
}