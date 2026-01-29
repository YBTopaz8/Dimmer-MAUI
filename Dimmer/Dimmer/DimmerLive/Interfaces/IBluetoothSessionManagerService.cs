using DynamicData;
using Dimmer.DimmerLive.Models;

namespace Dimmer.DimmerLive.Interfaces;

/// <summary>
/// Represents a Bluetooth device available for session transfer
/// </summary>
public class BluetoothDeviceInfo
{
    public string DeviceName { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public bool IsPaired { get; set; }
    public bool IsCurrentDevice { get; set; }
}

/// <summary>
/// Service for managing offline session transfers via Bluetooth
/// </summary>
public interface IBluetoothSessionManagerService
{
    /// <summary>
    /// Observable list of available Bluetooth devices
    /// </summary>
    IObservable<IChangeSet<BluetoothDeviceInfo, string>> AvailableDevices { get; }

    /// <summary>
    /// Observable stream of incoming transfer requests
    /// </summary>
    IObservable<DimmerSharedSong> IncomingTransferRequests { get; }

    /// <summary>
    /// Check if Bluetooth is enabled on the device
    /// </summary>
    Task<bool> IsBluetoothEnabledAsync();

    /// <summary>
    /// Check if a device is paired
    /// </summary>
    Task<bool> IsDevicePairedAsync(string deviceName);

    /// <summary>
    /// Refresh the list of available Bluetooth devices
    /// </summary>
    Task RefreshDevicesAsync();

    /// <summary>
    /// Start acting as a Bluetooth server to receive connections
    /// </summary>
    Task StartServerAsync();

    /// <summary>
    /// Stop the Bluetooth server
    /// </summary>
    void StopServer();

    /// <summary>
    /// Initiate a session transfer to a target Bluetooth device
    /// </summary>
    Task InitiateSessionTransferAsync(BluetoothDeviceInfo targetDevice, DimmerPlayEventView currentSongView);

    /// <summary>
    /// Prompt user to pair with a device (opens system settings)
    /// </summary>
    Task PromptPairingAsync();

    /// <summary>
    /// Get current connection status
    /// </summary>
    string GetConnectionStatus();
}
