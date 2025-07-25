using DynamicData;

using Parse.LiveQuery;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.DimmerLive.Interfaces;
public class DeviceConnectivityService : IDeviceConnectivityService, IDisposable
{
    private readonly SourceCache<DeviceState, string> _availablePlayersCache = new(player => player.DeviceId);

    // THIS IS THE PUBLIC OBSERVABLE. The ViewModel will connect to this.
    public IObservable<IChangeSet<DeviceState, string>> AvailablePlayers => _availablePlayersCache.Connect();
    private readonly ParseLiveQueryClient _liveQueryClient;
    private readonly ILogger<DeviceConnectivityService> _logger;
    private string _myDeviceId;
    private DeviceState _myDeviceState;

    // These hold our long-running subscriptions. This solves the lifetime issue.
    private Subscription<DeviceCommand>? _commandSubscription;
    private Subscription<DeviceState>? _stateSubscription;
    private System.Timers.Timer _presenceTimer;

    // The SOURCE for our public observables
    private readonly SourceCache<DeviceState, string> _availablePlayers = new(player => player.DeviceId);
    private readonly Subject<DeviceCommand> _incomingCommands = new();

    public IObservable<DeviceCommand> IncomingCommands => _incomingCommands.AsObservable();

    public DeviceConnectivityService(ParseLiveQueryClient liveQueryClient, ILogger<DeviceConnectivityService> logger)
    {
        _liveQueryClient = liveQueryClient;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        // Step 1: Get or create our unique ID
        _myDeviceId = Preferences.Get("MyDeviceId", Guid.NewGuid().ToString());
        Preferences.Set("MyDeviceId", _myDeviceId);

        // Step 2: Find our state object on the server or create it
        var query = new ParseQuery<DeviceState>(ParseClient.Instance).WhereEqualTo("deviceId", _myDeviceId);
        _myDeviceState = await query.FirstOrDefaultAsync() ?? new DeviceState { DeviceId = _myDeviceId };

        _myDeviceState.DeviceName = DeviceInfo.Name; // e.g., "MCS's Desktop"
        _myDeviceState.LastSeen = DateTime.UtcNow;
        await _myDeviceState.SaveAsync();

        // Step 3: Start a timer to periodically update our "lastSeen" timestamp
        _presenceTimer = new System.Timers.Timer(TimeSpan.FromSeconds(60).TotalMilliseconds);
        _presenceTimer.Elapsed += async (s, e) => {
            if (_myDeviceState != null)
            {
                _myDeviceState.LastSeen = DateTime.UtcNow;
                await _myDeviceState.SaveAsync();
            }
        };
        _presenceTimer.AutoReset = true;
        _presenceTimer.Start();
    }

    public async void StartListeners()
    {
        // --- Listener for incoming commands ---
        var commandQuery = new ParseQuery<DeviceCommand>(ParseClient.Instance)
            .WhereEqualTo("targetDeviceId", _myDeviceId)
            .WhereEqualTo("isHandled", false);

        _commandSubscription = await _liveQueryClient.SubscribeAsync(commandQuery);
        _commandSubscription.Events
            .Where(e => e.EventType == Subscription.Event.Create)
            .Subscribe(e => _incomingCommands.OnNext(e.Object));

        // --- Listener for other devices' states ---
        var stateQuery = new ParseQuery<DeviceState>(ParseClient.Instance)
            .WhereNotEqualTo("deviceId", _myDeviceId); // Don't listen to our own state

        _stateSubscription = await _liveQueryClient.SubscribeAsync(stateQuery);

        // Use the full power of DynamicData to manage the collection
        _stateSubscription.Events
            .Subscribe(e => {
                var device = e.Object;
                switch (e.EventType)
                {
                    case Subscription.Event.Enter: // A new device came online
                    case Subscription.Event.Create:
                    case Subscription.Event.Update:
                        _availablePlayers.AddOrUpdate(device);
                        break;
                    case Subscription.Event.Leave: // A device went offline
                    case Subscription.Event.Delete:
                        _availablePlayers.Remove(device);
                        break;
                }
            });
    }

    public void StopListeners()
    {
        _commandSubscription?.UnsubscribeNow();
        _stateSubscription?.UnsubscribeNow();
        _presenceTimer?.Stop();
    }

    public async Task SendCommandAsync(string targetDeviceId, string commandName, IDictionary<string, object>? payload = null)
    {
        var command = new DeviceCommand
        {
            TargetDeviceId = targetDeviceId,
            SourceDeviceId = _myDeviceId,
            CommandName = commandName,
            Payload = payload,
            IsHandled = false,
            Timestamp = DateTime.UtcNow
        };
        await command.SaveAsync();
    }

    public async Task UpdateDeviceStateAsync(string playbackState, SongModelView? currentSong, double position, double volume)
    {
        if (_myDeviceState == null)
            return;

        _myDeviceState.PlaybackState = playbackState;
        _myDeviceState.CurrentSongId = currentSong?.Id.ToString() ?? string.Empty;
        _myDeviceState.CurrentPosition = position;
        _myDeviceState.Volume = volume;
        // ... update other fields like shuffle, repeat ...
        _myDeviceState.LastSeen = DateTime.UtcNow;

        await _myDeviceState.SaveAsync();
    }

    public void Dispose()
    {
        StopListeners();
        _presenceTimer?.Dispose();
        _availablePlayers?.Dispose();
        _incomingCommands?.Dispose();
    }
}