using System.Text.Json;

using Android.Bluetooth;

using Dimmer.DimmerLive;

using Java.Util;

namespace Dimmer.NativeServices;

public class AndroidBluetoothService : IBluetoothService
{
    // A unique UUID for your app. Must match on Windows.
    private static readonly UUID MyServiceUuid = UUID.FromString("00001101-0000-1000-8000-00805F9B34FB");

    private BluetoothAdapter? _adapter;
    private BluetoothSocket? _socket;
    private BluetoothServerSocket? _serverSocket;
    private bool _isServer = false;

    public event EventHandler<string>? DataReceived;
    public event EventHandler<string>? StatusChanged;

    public AndroidBluetoothService()
    {
        _adapter = BluetoothAdapter.DefaultAdapter;
    }

    public Task<List<string>> GetPairedDevicesAsync()
    {
        if (_adapter == null || !_adapter.IsEnabled) return Task.FromResult(new List<string>());

        // Note: You need permissions check here in real code
        var devices = _adapter.BondedDevices;
        return Task.FromResult(devices.Select(d => d.Name).ToList());
    }

    public Task StartServerAsync()
    {
        return Task.Run(async () =>
        {
            try
            {
                StatusChanged?.Invoke(this, "Starting Server...");
                _serverSocket = _adapter?.ListenUsingRfcommWithServiceRecord("DimmerApp", MyServiceUuid);
                _isServer = true;

                // Blocking call, waiting for connection
                StatusChanged?.Invoke(this, "Waiting for connection...");
                _socket = _serverSocket?.Accept();

                StatusChanged?.Invoke(this, $"Connected to {_socket?.RemoteDevice?.Name}");

                // Start reading loop
                await ListenForDataAsync();
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke(this, $"Server Error: {ex.Message}");
            }
        });
    }

    public Task ConnectToDeviceAsync(string deviceName)
    {
        return Task.Run(async () =>
        {
            try
            {
                var device = _adapter?.BondedDevices.FirstOrDefault(d => d.Name == deviceName);
                if (device == null) throw new Exception("Device not found.");

                StatusChanged?.Invoke(this, $"Connecting to {deviceName}...");
                _socket = device.CreateRfcommSocketToServiceRecord(MyServiceUuid);
                _socket?.Connect();

                StatusChanged?.Invoke(this, "Connected!");

                // Start reading loop (in case bidirectional)
                await ListenForDataAsync();
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke(this, $"Connection Failed: {ex.Message}");
            }
        });
    }

    public async Task SendDataAsync(BluetoothDataPackage data)
    {
        if (_socket == null || !_socket.IsConnected) return;

        try
        {
            string json = JsonSerializer.Serialize(data);
            byte[] bytes = Encoding.UTF8.GetBytes(json);

            // Send length prefix (4 bytes) followed by data
            byte[] lengthPrefix = BitConverter.GetBytes(bytes.Length);
            await _socket.OutputStream.WriteAsync(lengthPrefix, 0, lengthPrefix.Length);
            await _socket.OutputStream.WriteAsync(bytes, 0, bytes.Length);
            
            StatusChanged?.Invoke(this, "Data Sent.");
        }
        catch (Exception ex)
        {
            StatusChanged?.Invoke(this, $"Send Failed: {ex.Message}");
        }
    }

    private async Task ListenForDataAsync()
    {
        while (_socket != null && _socket.IsConnected)
        {
            try
            {
                // Read length prefix (4 bytes)
                byte[] lengthBuffer = new byte[4];
                int lengthBytesRead = await _socket.InputStream.ReadAsync(lengthBuffer, 0, 4);
                if (lengthBytesRead != 4)
                {
                    // Connection closed or error
                    break;
                }

                int messageLength = BitConverter.ToInt32(lengthBuffer, 0);
                if (messageLength <= 0 || messageLength > 10 * 1024 * 1024) // 10MB max
                {
                    StatusChanged?.Invoke(this, "Invalid message length received.");
                    break;
                }

                // Read the actual data
                byte[] messageBuffer = new byte[messageLength];
                int totalBytesRead = 0;
                while (totalBytesRead < messageLength)
                {
                    int bytesRead = await _socket.InputStream.ReadAsync(
                        messageBuffer, 
                        totalBytesRead, 
                        messageLength - totalBytesRead);
                    
                    if (bytesRead <= 0)
                    {
                        break;
                    }
                    totalBytesRead += bytesRead;
                }

                if (totalBytesRead != messageLength)
                {
                    StatusChanged?.Invoke(this, "Failed to read complete message.");
                    break;
                }

                string json = Encoding.UTF8.GetString(messageBuffer);
                DataReceived?.Invoke(this, json);
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke(this, $"Listening error: {ex.Message}");
                break;
            }
        }
    }

    public void Disconnect()
    {
        _socket?.Close();
        _socket = null;
        StatusChanged?.Invoke(this, "Disconnected");
    }
}