using System.Text.Json;
using DynamicData;
using System.Reactive.Subjects;
using Microsoft.Extensions.Logging;
using Dimmer.DimmerLive.Models;

namespace Dimmer.DimmerLive.Interfaces.Implementations;

public class BluetoothSessionManagerService : IBluetoothSessionManagerService, IDisposable
{
    private readonly IBluetoothService _bluetoothService;
    private readonly ILogger<BluetoothSessionManagerService> _logger;
    private readonly Subject<DimmerSharedSong> _incomingTransfers = new();
    private readonly SourceCache<BluetoothDeviceInfo, string> _devicesCache = new(device => device.DeviceId);
    private string _currentStatus = "Disconnected";
    private bool _isServerRunning;

    public IObservable<IChangeSet<BluetoothDeviceInfo, string>> AvailableDevices => _devicesCache.Connect();
    public IObservable<DimmerSharedSong> IncomingTransferRequests => _incomingTransfers.AsObservable();

    public BluetoothSessionManagerService(
        IBluetoothService bluetoothService,
        ILogger<BluetoothSessionManagerService> logger)
    {
        _bluetoothService = bluetoothService;
        _logger = logger;

        // Subscribe to Bluetooth events
        _bluetoothService.DataReceived += OnDataReceived;
        _bluetoothService.StatusChanged += OnStatusChanged;
    }

    public async Task<bool> IsBluetoothEnabledAsync()
    {
        try
        {
            // Try to get paired devices as a way to check if Bluetooth is enabled
            var devices = await _bluetoothService.GetPairedDevicesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> IsDevicePairedAsync(string deviceName)
    {
        try
        {
            var pairedDevices = await _bluetoothService.GetPairedDevicesAsync();
            return pairedDevices.Contains(deviceName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if device is paired");
            return false;
        }
    }

    public async Task RefreshDevicesAsync()
    {
        try
        {
            _logger.LogInformation("Refreshing Bluetooth devices...");
            var pairedDevices = await _bluetoothService.GetPairedDevicesAsync();

            var currentDeviceName = DeviceInfo.Name;

            var deviceInfos = pairedDevices
                .Where(name => !string.IsNullOrEmpty(name))
                .Select(name => new BluetoothDeviceInfo
                {
                    DeviceName = name,
                    DeviceId = name, // Using name as ID for simplicity
                    IsPaired = true,
                    IsCurrentDevice = name == currentDeviceName
                })
                .Where(d => !d.IsCurrentDevice) // Exclude current device
                .ToList();

            _devicesCache.Edit(update =>
            {
                update.Clear();
                update.AddOrUpdate(deviceInfos);
            });

            _logger.LogInformation("Found {Count} paired Bluetooth devices", deviceInfos.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh Bluetooth devices");
        }
    }

    public async Task StartServerAsync()
    {
        if (_isServerRunning)
        {
            _logger.LogWarning("Bluetooth server is already running");
            return;
        }

        try
        {
            _logger.LogInformation("Starting Bluetooth server...");
            await _bluetoothService.StartServerAsync();
            _isServerRunning = true;
            _currentStatus = "Server running, waiting for connections...";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start Bluetooth server");
            _currentStatus = $"Server error: {ex.Message}";
            throw;
        }
    }

    public void StopServer()
    {
        if (!_isServerRunning)
        {
            return;
        }

        try
        {
            _logger.LogInformation("Stopping Bluetooth server...");
            _bluetoothService.Disconnect();
            _isServerRunning = false;
            _currentStatus = "Server stopped";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping Bluetooth server");
        }
    }

    public async Task InitiateSessionTransferAsync(BluetoothDeviceInfo targetDevice, DimmerPlayEventView currentSongView)
    {
        if (targetDevice == null || currentSongView == null)
        {
            _logger.LogWarning("Cannot initiate transfer: missing target device or song info");
            return;
        }

        try
        {
            _logger.LogInformation("Initiating Bluetooth session transfer to {DeviceName}", targetDevice.DeviceName);

            // Check if device is paired
            if (!await IsDevicePairedAsync(targetDevice.DeviceName))
            {
                _logger.LogWarning("Device {DeviceName} is not paired", targetDevice.DeviceName);
                throw new InvalidOperationException($"Device '{targetDevice.DeviceName}' is not paired. Please pair the device first.");
            }

            // Connect to the target device
            _currentStatus = $"Connecting to {targetDevice.DeviceName}...";
            await _bluetoothService.ConnectToDeviceAsync(targetDevice.DeviceName);

            // Create the shared song data (metadata only, no audio file)
            var sharedSong = new DimmerSharedSong
            {
                Title = currentSongView.SongName,
                ArtistName = currentSongView.ArtistName,
                AlbumName = currentSongView.AlbumName,
                OriginalSongId = currentSongView.SongId?.ToString() ?? string.Empty,
                SharedPositionInSeconds = currentSongView.PositionInSeconds,
                IsFavorite = currentSongView.IsFav,
                IsPlaying = true
            };

            // Serialize and send as a Bluetooth data package
            var package = new BluetoothDataPackage
            {
                Type = DataPackageType.SessionTransfer,
                PayloadJson = JsonSerializer.Serialize(sharedSong),
                Timestamp = DateTimeOffset.Now
            };

            await _bluetoothService.SendDataAsync(package);
            _currentStatus = "Transfer sent successfully";
            _logger.LogInformation("Session transfer sent to {DeviceName}", targetDevice.DeviceName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initiate Bluetooth session transfer");
            _currentStatus = $"Transfer failed: {ex.Message}";
            throw;
        }
    }

    public async Task PromptPairingAsync()
    {
        try
        {
            _logger.LogInformation("Opening Bluetooth settings for pairing...");
            await _bluetoothService.OpenBluetoothSettingsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open Bluetooth settings");
        }
    }

    public string GetConnectionStatus()
    {
        return _currentStatus;
    }

    private void OnDataReceived(object? sender, string jsonData)
    {
        try
        {
            _logger.LogInformation("Received Bluetooth data");

            var package = JsonSerializer.Deserialize<BluetoothDataPackage>(jsonData);
            if (package == null)
            {
                _logger.LogWarning("Failed to deserialize Bluetooth data package");
                return;
            }

            switch (package.Type)
            {
                case DataPackageType.SessionTransfer:
                    HandleSessionTransfer(package.PayloadJson);
                    break;

                case DataPackageType.Backup:
                    // Future: Handle backup data
                    _logger.LogInformation("Backup data received (not yet implemented)");
                    break;

                default:
                    _logger.LogWarning("Unknown data package type: {Type}", package.Type);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing received Bluetooth data");
        }
    }

    private void HandleSessionTransfer(string payloadJson)
    {
        try
        {
            var sharedSong = JsonSerializer.Deserialize<DimmerSharedSong>(payloadJson);
            if (sharedSong != null)
            {
                _logger.LogInformation("Session transfer received: {Title} by {Artist}", 
                    sharedSong.Title, sharedSong.ArtistName);
                _incomingTransfers.OnNext(sharedSong);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle session transfer");
        }
    }

    private void OnStatusChanged(object? sender, string status)
    {
        _currentStatus = status;
        _logger.LogInformation("Bluetooth status: {Status}", status);
    }

    public void Dispose()
    {
        _bluetoothService.DataReceived -= OnDataReceived;
        _bluetoothService.StatusChanged -= OnStatusChanged;
        _bluetoothService.Disconnect();
        _devicesCache?.Dispose();
        _incomingTransfers?.Dispose();
    }
}
