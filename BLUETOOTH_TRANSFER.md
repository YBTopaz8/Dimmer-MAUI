# Bluetooth Offline Session Transfer

This feature enables offline session transfer between Dimmer devices using Bluetooth connectivity. It complements the existing Parse-based online transfer functionality.

## Overview

The Bluetooth session transfer allows users to:
- Transfer currently playing song metadata to another paired device
- Work completely offline without internet connection
- Support cross-platform transfers (Windows ↔ Android)

## Architecture

### Core Components

1. **IBluetoothService** - Platform-agnostic Bluetooth service interface
   - Defined in: `Dimmer/Dimmer/DimmerLive/IBluetoothService.cs`
   - Handles low-level Bluetooth communication
   - Supports server and client modes

2. **Platform Implementations**
   - **WindowsBluetoothService** - Windows/WinUI implementation using UWP Bluetooth APIs
   - **AndroidBluetoothService** - Android implementation using Android Bluetooth APIs

3. **IBluetoothSessionManagerService** - High-level session management interface
   - Defined in: `Dimmer/Dimmer/DimmerLive/Interfaces/IBluetoothSessionManagerService.cs`
   - Manages device discovery, pairing checks, and transfer orchestration
   - Implementation: `BluetoothSessionManagerService`

4. **SessionManagementViewModel** - UI/ViewModel integration
   - Supports both online (Parse) and offline (Bluetooth) modes
   - Handles user interactions for device selection and transfer initiation

## How It Works

### Transfer Protocol

1. **Device Discovery**
   - Lists paired Bluetooth devices
   - Filters out the current device
   - Presents available devices to the user

2. **Pairing Check**
   - Verifies target device is already paired
   - If not paired, prompts user to open Bluetooth settings
   - Ensures secure connection before transfer

3. **Connection Establishment**
   - Sender acts as Bluetooth client
   - Receiver runs as Bluetooth server (listening for connections)
   - Uses standard Serial Port Profile (SPP) UUID: `00001101-0000-1000-8000-00805F9B34FB`

4. **Data Transfer**
   - Sends metadata only (no audio files)
   - Uses length-prefixed protocol for reliable message framing
   - Serializes data as JSON using `BluetoothDataPackage`

5. **Transfer Types**
   - **SessionTransfer**: Current song playback state (title, artist, album, position)
   - **Backup**: Full playback history (reserved for future implementation)

### Data Format

```csharp
public class BluetoothDataPackage
{
    public DataPackageType Type { get; set; }      // SessionTransfer or Backup
    public string PayloadJson { get; set; }         // Serialized DimmerSharedSong
    public DateTimeOffset Timestamp { get; set; }   // Transfer timestamp
}
```

### Song Metadata

```csharp
public class DimmerSharedSong
{
    public string Title { get; set; }
    public string ArtistName { get; set; }
    public string AlbumName { get; set; }
    public string OriginalSongId { get; set; }      // To locate file on receiving device
    public double? SharedPositionInSeconds { get; set; }
    public bool IsFavorite { get; set; }
    public bool IsPlaying { get; set; }
}
```

## Usage

### For End Users

1. **Ensure Devices are Paired**
   - Pair devices via system Bluetooth settings
   - Both devices must have Dimmer installed

2. **Start Receiver Mode**
   - Receiving device automatically starts Bluetooth server when app launches
   - No manual action required

3. **Initiate Transfer**
   - On sender device, open Session Management page
   - Toggle "Use Bluetooth Transfer" mode
   - Select target device from list
   - Confirm transfer

4. **Accept Transfer**
   - Receiver gets a prompt to accept the transfer
   - If song exists locally, it starts playing at the same position
   - If song not found, user is notified

### For Developers

#### Registering Services

Services are automatically registered in the DI container:

```csharp
// Core service (in Dimmer/Dimmer/ServiceRegistration.cs)
services.AddSingleton<IBluetoothSessionManagerService, BluetoothSessionManagerService>();

// Platform-specific (in respective MauiProgram.cs or Bootstrapper.cs)
services.AddSingleton<IBluetoothService, WindowsBluetoothService>();  // WinUI
services.AddSingleton<IBluetoothService, AndroidBluetoothService>();  // Android
```

#### Using the Service

```csharp
// Inject the service
private readonly IBluetoothSessionManagerService _bluetoothManager;

// Check if Bluetooth is available
bool isAvailable = await _bluetoothManager.IsBluetoothEnabledAsync();

// Refresh device list
await _bluetoothManager.RefreshDevicesAsync();

// Subscribe to available devices
_bluetoothManager.AvailableDevices
    .ObserveOn(RxSchedulers.UI)
    .Bind(out _bluetoothDevices)
    .Subscribe()
    .DisposeWith(_disposables);

// Initiate transfer
await _bluetoothManager.InitiateSessionTransferAsync(targetDevice, currentSongView);

// Handle incoming transfers
_bluetoothManager.IncomingTransferRequests
    .ObserveOn(RxSchedulers.UI)
    .Subscribe(HandleIncomingTransfer)
    .DisposeWith(_disposables);
```

## Security Considerations

1. **Bluetooth Pairing Required**
   - Only paired devices can connect
   - Uses system-level Bluetooth security

2. **No Audio File Transfer**
   - Only metadata is transferred
   - Reduces attack surface and improves performance

3. **Local-Only Communication**
   - No internet connection required
   - Data stays on local devices

## Limitations

1. **Bluetooth Range**
   - Limited to standard Bluetooth range (~10 meters)
   - Devices must be in proximity

2. **Platform Support**
   - Currently supports Windows and Android
   - iOS/macOS support discontinued

3. **Song Availability**
   - Receiving device must have the song file locally
   - If song not found, transfer displays notification only

4. **One-to-One Transfer**
   - Currently supports transfer to one device at a time
   - Multi-device broadcast not yet implemented

## Troubleshooting

### Connection Issues

**Problem**: "Device not found"
- **Solution**: Ensure devices are paired in system Bluetooth settings

**Problem**: "Transfer failed: Connection failed"
- **Solution**: 
  - Check Bluetooth is enabled on both devices
  - Ensure devices are within range
  - Try unpairing and re-pairing devices

**Problem**: "Song not found on this device"
- **Solution**: The receiving device doesn't have the song file. Add the song to the library or use online transfer instead.

### Permissions

**Android**: Ensure the following permissions are granted:
- BLUETOOTH
- BLUETOOTH_CONNECT
- BLUETOOTH_SCAN

**Windows**: Ensure app has Bluetooth capability in Package.appxmanifest

## Future Enhancements

1. **Automatic Device Discovery**
   - Discover unpaired devices nearby
   - In-app pairing workflow

2. **Multi-Device Broadcast**
   - Send to multiple devices simultaneously
   - Group listening sessions

3. **Backup/Restore via Bluetooth**
   - Complete playback history transfer
   - Settings synchronization

4. **Resume Transfer**
   - Handle interrupted transfers gracefully
   - Retry mechanism for failed transfers

5. **Transfer History**
   - Log of recent transfers
   - Statistics and insights

## Technical Details

### Protocol Specification

Messages are framed using a length-prefix protocol:

```
[4 bytes: message length (int32, little-endian)]
[N bytes: JSON payload]
```

This ensures reliable message boundaries even for large payloads.

### Thread Safety

- All Bluetooth operations are async
- UI updates are marshaled to main thread using RxSchedulers.UI
- Connection state is managed internally by platform-specific implementations

### Resource Management

- Services implement IDisposable
- Bluetooth connections are properly closed on disposal
- Event handlers are unsubscribed to prevent memory leaks

## Testing

Manual testing scenarios:

1. **WinUI to WinUI**: Transfer between two Windows devices
2. **Android to Android**: Transfer between two Android devices  
3. **WinUI to Android**: Cross-platform transfer
4. **Android to WinUI**: Reverse cross-platform transfer
5. **Unpaired Device**: Verify pairing prompt appears
6. **Song Not Found**: Verify appropriate error message
7. **Connection Drop**: Test behavior when Bluetooth disconnects mid-transfer

## References

- [Windows Bluetooth RFCOMM](https://docs.microsoft.com/en-us/windows/uwp/devices-sensors/send-or-receive-files-with-rfcomm)
- [Android Bluetooth Guide](https://developer.android.com/guide/topics/connectivity/bluetooth)
- [Serial Port Profile (SPP)](https://www.bluetooth.com/specifications/specs/serial-port-profile-1-2/)
