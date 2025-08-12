using DynamicData;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.DimmerSearch.Interfaces;

public interface IDeviceSessionService
{
    // A property to observe the user's other available devices
    IObservable<IChangeSet<UserDeviceSession, string>> OtherAvailableDevices { get; }

    // Call this on app startup to register the device with Parse
    Task RegisterCurrentDeviceAsync();

    // Call this when the app is closing or going to the background
    Task MarkCurrentDeviceInactiveAsync();

    // Call this to initiate the transfer
    Task InitiateSessionTransferAsync(UserDeviceSession targetDevice, DimmerSharedSong currentSongState);

    // Call this to start listening for device changes
    void StartListening();

    // Call this to stop listening
    void StopListening();
}