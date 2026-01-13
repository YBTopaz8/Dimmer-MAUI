using System.Text;
using System.Text.Json;
using Dimmer.DimmerLive;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Devices.Enumeration;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace Dimmer.WinUI.NativeServices;

public class WindowsBluetoothService : IBluetoothService
{
    // Standard SPP UUID (Serial Port Profile) - must match Android
    private static readonly Guid MyServiceUuid = new("00001101-0000-1000-8000-00805F9B34FB");

    private StreamSocket? _socket;
    private StreamSocketListener? _listener;
    private DataWriter? _writer;
    private DataReader? _reader;
    private bool _isServer;
    private CancellationTokenSource? _listenerCts;

    public event EventHandler<string>? DataReceived;
    public event EventHandler<string>? StatusChanged;

    public async Task<List<string>> GetPairedDevicesAsync()
    {
        try
        {
            // Request paired Bluetooth devices
            var selector = BluetoothDevice.GetDeviceSelectorFromPairingState(true);
            var devices = await DeviceInformation.FindAllAsync(selector);
            
            return devices.Select(d => d.Name).Where(n => !string.IsNullOrEmpty(n)).ToList()!;
        }
        catch (Exception ex)
        {
            StatusChanged?.Invoke(this, $"Error getting paired devices: {ex.Message}");
            return new List<string>();
        }
    }

    public async Task StartServerAsync()
    {
        try
        {
            StatusChanged?.Invoke(this, "Starting Bluetooth server...");
            _isServer = true;
            _listenerCts = new CancellationTokenSource();

            _listener = new StreamSocketListener();
            _listener.ConnectionReceived += OnConnectionReceived;

            // Start listening for RFCOMM connections
            await _listener.BindServiceNameAsync(RfcommServiceId.FromUuid(MyServiceUuid).AsString());

            StatusChanged?.Invoke(this, "Bluetooth server started. Waiting for connections...");
        }
        catch (Exception ex)
        {
            StatusChanged?.Invoke(this, $"Server error: {ex.Message}");
            throw;
        }
    }

    private void OnConnectionReceived(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
    {
        // Fire-and-forget with proper exception handling
        _ = Task.Run(async () =>
        {
            try
            {
                _socket = args.Socket;
                StatusChanged?.Invoke(this, $"Client connected: {_socket.Information.RemoteHostName}");

                _writer = new DataWriter(_socket.OutputStream);
                _reader = new DataReader(_socket.InputStream);

                // Start listening for incoming data
                await ListenForDataAsync(_listenerCts?.Token ?? CancellationToken.None);
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke(this, $"Connection handling error: {ex.Message}");
            }
        });
    }

    public async Task ConnectToDeviceAsync(string deviceName)
    {
        try
        {
            StatusChanged?.Invoke(this, $"Connecting to {deviceName}...");

            // Find the device by name
            var selector = BluetoothDevice.GetDeviceSelectorFromPairingState(true);
            var devices = await DeviceInformation.FindAllAsync(selector);
            var deviceInfo = devices.FirstOrDefault(d => d.Name == deviceName);

            if (deviceInfo == null)
            {
                throw new Exception($"Device '{deviceName}' not found among paired devices.");
            }

            // Get the Bluetooth device
            var bluetoothDevice = await BluetoothDevice.FromIdAsync(deviceInfo.Id);
            if (bluetoothDevice == null)
            {
                throw new Exception("Failed to get Bluetooth device.");
            }

            // Get RFCOMM services
            var rfcommServices = await bluetoothDevice.GetRfcommServicesForIdAsync(
                RfcommServiceId.FromUuid(MyServiceUuid),
                BluetoothCacheMode.Uncached);

            if (rfcommServices.Services.Count == 0)
            {
                throw new Exception("Target device is not running the Dimmer Bluetooth service.");
            }

            var service = rfcommServices.Services[0];

            // Connect socket
            _socket = new StreamSocket();
            await _socket.ConnectAsync(
                service.ConnectionHostName,
                service.ConnectionServiceName);

            _writer = new DataWriter(_socket.OutputStream);
            _reader = new DataReader(_socket.InputStream);

            StatusChanged?.Invoke(this, "Connected successfully!");

            // Create CTS before starting listener to avoid race condition
            _listenerCts = new CancellationTokenSource();
            var token = _listenerCts.Token;
            
            // Start listening for responses in the background
            _ = Task.Run(async () =>
            {
                try
                {
                    await ListenForDataAsync(token);
                }
                catch (Exception ex)
                {
                    if (!token.IsCancellationRequested)
                    {
                        StatusChanged?.Invoke(this, $"Listener error: {ex.Message}");
                    }
                }
            });
        }
        catch (Exception ex)
        {
            StatusChanged?.Invoke(this, $"Connection failed: {ex.Message}");
            throw;
        }
    }

    public async Task SendDataAsync(BluetoothDataPackage data)
    {
        if (_writer == null || _socket == null)
        {
            throw new InvalidOperationException("Not connected to any device.");
        }

        try
        {
            string json = JsonSerializer.Serialize(data);
            byte[] bytes = Encoding.UTF8.GetBytes(json);

            // Write length prefix (4 bytes) followed by data
            _writer.WriteInt32(bytes.Length);
            _writer.WriteBytes(bytes);
            await _writer.StoreAsync();

            StatusChanged?.Invoke(this, "Data sent successfully.");
        }
        catch (Exception ex)
        {
            StatusChanged?.Invoke(this, $"Send failed: {ex.Message}");
            throw;
        }
    }

    private async Task ListenForDataAsync(CancellationToken cancellationToken)
    {
        if (_reader == null) return;

        try
        {
            while (!cancellationToken.IsCancellationRequested && _socket != null)
            {
                // Read length prefix (4 bytes)
                uint sizeFieldCount = await _reader.LoadAsync(sizeof(int));
                if (sizeFieldCount != sizeof(int))
                {
                    // Connection closed or error
                    break;
                }

                int messageLength = _reader.ReadInt32();
                if (messageLength <= 0 || messageLength > 10 * 1024 * 1024) // 10MB max
                {
                    StatusChanged?.Invoke(this, "Invalid message length received.");
                    break;
                }

                // Read the actual data
                uint actualLength = await _reader.LoadAsync((uint)messageLength);
                if (actualLength != messageLength)
                {
                    StatusChanged?.Invoke(this, "Failed to read complete message.");
                    break;
                }

                byte[] messageBytes = new byte[messageLength];
                _reader.ReadBytes(messageBytes);

                string json = Encoding.UTF8.GetString(messageBytes);
                DataReceived?.Invoke(this, json);
            }
        }
        catch (Exception ex)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                StatusChanged?.Invoke(this, $"Listening error: {ex.Message}");
            }
        }
    }

    public void Disconnect()
    {
        try
        {
            // Cancel any ongoing operations first
            var ctsToDispose = _listenerCts;
            _listenerCts = null;
            ctsToDispose?.Cancel();
            
            // Clean up resources
            _writer?.Dispose();
            _writer = null;

            _reader?.Dispose();
            _reader = null;

            _socket?.Dispose();
            _socket = null;

            if (_listener != null)
            {
                _listener.ConnectionReceived -= OnConnectionReceived;
                _listener.Dispose();
                _listener = null;
            }

            // Dispose CTS after all operations are cancelled
            ctsToDispose?.Dispose();

            StatusChanged?.Invoke(this, "Disconnected.");
        }
        catch (Exception ex)
        {
            StatusChanged?.Invoke(this, $"Disconnect error: {ex.Message}");
        }
    }

    public async Task OpenBluetoothSettingsAsync()
    {
        try
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings:bluetooth"));
        }
        catch (Exception ex)
        {
            StatusChanged?.Invoke(this, $"Failed to open Bluetooth settings: {ex.Message}");
            throw;
        }
    }
}
