using System.Text.Json;

using Android.Bluetooth;

using Dimmer.DimmerLive;

using Java.Util;

namespace Dimmer.NativeServices;

public class AndroidBluetoothService : IBluetoothService
{
    // A unique UUID for your app. Must match on Windows.
    private static readonly UUID MyServiceUuid = UUID.FromString("00001101-1899-1000-8000-00805F9B34FB");

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

    public Task<List<string?>?> GetPairedDevicesAsync()
    {
        if (_adapter == null || !_adapter.IsEnabled) return Task.FromResult(new List<string>());

        // Note: You need permissions check here in real code
        var devices = _adapter.BondedDevices;
        return Task.FromResult(devices?.Select(d => d.Name).ToList());
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

            // Send size first (optional but good practice), keeping it simple for now
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
        var buffer = new byte[1024];
        var sb = new StringBuilder();

        while (_socket != null && _socket.IsConnected)
        {
            try
            {
                // Simple reading logic. For large files, you'd want a length-prefix protocol.
                int bytesRead = await _socket.InputStream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead > 0)
                {
                    string part = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    sb.Append(part);

                    // Quick hack to detect end of JSON for this example
                    // In production, send the Length of the bytes first (4 bytes int), read that length, then parse.
                    if (IsJsonComplete(sb.ToString()))
                    {
                        DataReceived?.Invoke(this, sb.ToString());
                        sb.Clear();
                    }
                }
            }
            catch { break; }
        }
    }

    private bool IsJsonComplete(string input) => input.Trim().EndsWith("}");

    public void Disconnect()
    {
        _socket?.Close();
        _socket = null;
        StatusChanged?.Invoke(this, "Disconnected");
    }
}