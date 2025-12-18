using Parse.LiveQuery;

namespace Dimmer.DimmerLive.Interfaces.Implementations;
public class ParseFriendshipService : IFriendshipService, IDisposable
{
    private readonly IAuthenticationService _authService;
    private readonly ParseLiveQueryClient _liveQueryClient;
    private readonly ILogger<ParseFriendshipService> _logger;

    private readonly SourceCache<UserModelOnline, string> _friendsCache = new(u => u.ObjectId);
    private readonly SourceCache<FriendRequest, string> _requestsCache = new(r => r.ObjectId);
    private Subscription<FriendRequest>? _requestSubscription;
    private Subscription<ParseUser>? _friendSubscription;

    public IObservable<IChangeSet<UserModelOnline, string>> Friends => _friendsCache.Connect();
    public IObservable<IChangeSet<FriendRequest, string>> IncomingFriendRequests => _requestsCache.Connect();

    public ParseFriendshipService(IAuthenticationService authService, ParseLiveQueryClient liveQueryClient, ILogger<ParseFriendshipService> logger)
    {
        _authService = authService;
        _liveQueryClient = liveQueryClient;
        _logger = logger;
    }

    public void StartListeners()
    {
        var currentUser = _authService.CurrentUserValue;
        if (currentUser == null)
            return;

        // Listen for incoming friend requests
        var requestQuery = new  ParseQuery<FriendRequest>(ParseClient.Instance)
            .WhereEqualTo("recipient", currentUser)
            .WhereEqualTo("status", "pending")
            .Include("sender");

        _requestSubscription = _liveQueryClient.Subscribe(requestQuery);
        _requestSubscription.On(Subscription.Event.Create, req => _requestsCache.AddOrUpdate(req));
        _requestSubscription.On(Subscription.Event.Delete, req => _requestsCache.Remove(req));

        // TODO: Listen for changes to the 'friends' relation on the current user
        // This is more advanced and can be added later. For now, we'll fetch friends manually.
        Task.Run(FetchInitialFriendsAsync);
    }

    private async Task FetchInitialFriendsAsync()
    {
        var currentUser = _authService.CurrentUserValue;
        if (currentUser == null)
            return;
        var friends = await currentUser.GetRelation<UserModelOnline>("friends").Query.FindAsync();
        _friendsCache.AddOrUpdate(friends);
    }

    public async Task<AuthResult> SendFriendRequestAsync(string username)
    {
        var currentUser = _authService.CurrentUserValue;
        if (currentUser == null)
            return AuthResult.Failure("You must be logged in.");

        var recipient = await new ParseQuery<UserModelOnline>(ParseClient.Instance).WhereEqualTo("username", username).FirstOrDefaultAsync();
        if (recipient == null)
            return AuthResult.Failure("User not found.");

        // Call a Cloud Code function for security and logic.
        var parameters = new Dictionary<string, object> { { "recipientId", recipient.ObjectId } };
        await ParseClient.Instance.CallCloudCodeFunctionAsync<string>("sendFriendRequest", parameters);
        return AuthResult.Success();
    }

    public async Task AcceptFriendRequestAsync(FriendRequest request)
    {
        var parameters = new Dictionary<string, object> { { "requestId", request.ObjectId } };
        await ParseClient.Instance.CallCloudCodeFunctionAsync<string>("acceptFriendRequest", parameters);
        _requestsCache.Remove(request); // Optimistically remove from UI
        await FetchInitialFriendsAsync(); // Refresh friend list
    }

    public async Task RejectFriendRequestAsync(FriendRequest request)
    {
        var parameters = new Dictionary<string, object> { { "requestId", request.ObjectId } };
        await ParseClient.Instance.CallCloudCodeFunctionAsync<string>("rejectFriendRequest", parameters);
        _requestsCache.Remove(request); // Optimistically remove
    }

    public async Task<IEnumerable<UserModelOnline>> FindUsersAsync(string searchTerm)
    {
        return await new ParseQuery<UserModelOnline>(ParseClient.Instance)
            .WhereStartsWith("username", searchTerm)
            .WhereNotEqualTo("objectId", _authService.CurrentUserValue?.ObjectId)
            .Limit(10)
            .FindAsync();
    }

    public void StopListeners()
    {
        _requestSubscription?.UnsubscribeNow();
        _friendSubscription?.UnsubscribeNow();
    }

    public void Dispose() => StopListeners();
}