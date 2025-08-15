using Dimmer.DimmerLive.Interfaces.Services;

using DynamicData;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.DimmerLive.Interfaces;

public interface IFriendshipService
{
    // Live streams of data for the ViewModel to bind to.
    IObservable<IChangeSet<UserModelOnline, string>> Friends { get; }
    IObservable<IChangeSet<FriendRequest, string>> IncomingFriendRequests { get; }

    // Actions the user can perform.
    Task<AuthResult> SendFriendRequestAsync(string username);
    Task AcceptFriendRequestAsync(FriendRequest request);
    Task RejectFriendRequestAsync(FriendRequest request);

    // One-time data fetch for lists of users.
    Task<IEnumerable<UserModelOnline>> FindUsersAsync(string searchTerm);

    // Lifecycle methods
    void StartListeners();
    void StopListeners();
}