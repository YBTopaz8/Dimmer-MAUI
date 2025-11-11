using Dimmer.DimmerLive.Interfaces.Implementations;

using DynamicData;

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