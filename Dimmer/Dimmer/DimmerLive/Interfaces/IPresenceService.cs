using DynamicData;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.DimmerLive.Interfaces;

public interface IPresenceService : IDisposable
{
    // A live stream of the user's OTHER devices that are currently online.
    IObservable<IChangeSet<UserDeviceSession, string>> OtherActiveDevices { get; }

    // Call this when the app starts and the user is logged in.
    Task AnnouncePresenceAsync();

    // Call this when the app is closing or backgrounding.
    Task GoOfflineAsync();
    void StartListeners();
    void StopListeners();

}