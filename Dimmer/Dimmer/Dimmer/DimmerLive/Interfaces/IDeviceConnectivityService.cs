using DynamicData;

using Parse.LiveQuery;

namespace Dimmer.DimmerLive.Interfaces;
public record RemotePlayerState(string DeviceId, string DeviceName, string PlaybackState, string CurrentSongTitle);

public interface IDeviceConnectivityService
{
    /// <summary>
    /// Initializes the service, getting the local device ID and registering it on the server.
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// Starts listening for incoming commands and state changes.
    /// </summary>
    void StartListeners();

    /// <summary>
    /// Stops all listeners and goes offline.
    /// </summary>
    void StopListeners();

    /// <summary>
    /// Sends a command to another device.
    /// </summary>
    Task SendCommandAsync(string targetDeviceId, string commandName, IDictionary<string, object>? payload = null);

    /// <summary>
    /// An observable stream of other devices that are online and available to be controlled.
    /// </summary>
    IObservable<IChangeSet<DeviceState, string>> AvailablePlayers { get; }

    /// <summary>
    /// An observable stream of commands received from other devices that this instance should execute.
    /// </summary>
    IObservable<DeviceCommand> IncomingCommands { get; }
    ParseLiveQueryClient LiveQueryClient { get; }

    /// <summary>
    /// Broadcasts the current state of this device to the server.
    /// </summary>
    Task UpdateDeviceStateAsync(string playbackState, SongModelView? currentSong, double position, double volume);
}