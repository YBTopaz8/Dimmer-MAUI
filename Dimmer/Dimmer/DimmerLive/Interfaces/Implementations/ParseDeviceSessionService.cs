using DynamicData;

using Parse.LiveQuery;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.DimmerSearch.Interfaces.Implementations;
public class ParseDeviceSessionService : IDeviceSessionService, IDisposable
{
    private readonly ILogger<ParseDeviceSessionService> _logger;
    private readonly IAuthenticationService _authService;
    private readonly ParseLiveQueryClient _liveQueryClient;
    private Subscription<ChatMessage> _messageSubscription;
    private UserDeviceSession _thisDeviceSession;

    // The source cache for other devices
    private readonly SourceCache<UserDeviceSession, string> _otherDevicesCache = new(session => session.ObjectId);

    // Public observable property from the interface
    public IObservable<IChangeSet<UserDeviceSession, string>> OtherAvailableDevices => _otherDevicesCache.AsObservableCache().Connect();

    public ParseDeviceSessionService(ILogger<ParseDeviceSessionService> logger, IAuthenticationService authService, ParseLiveQueryClient liveQueryClient)
    {
        _logger = logger;
        _authService = authService;
        _liveQueryClient = liveQueryClient;
    }

    public async Task RegisterCurrentDeviceAsync()
    {
        if (_authService.CurrentUser is null)
        {
            _logger.LogWarning("Cannot register device, user is not logged in.");
            return;
        }

        try
        {
            var parameters = new Dictionary<string, object>
            {
                { "currentDeviceName", DeviceInfo.Name },
                { "currentDeviceId", Preferences.Get("MyDeviceId", Guid.NewGuid().ToString()) }, // Ensure you have a unique ID
                { "currentDeviceIdiom", DeviceInfo.Idiom.ToString() },
                { "currentDeviceOSVersion", DeviceInfo.VersionString }
            };

            // This cloud function now handles find-or-create AND sets this device as active
            var result = await ParseClient.Instance.CallCloudCodeFunctionAsync<IDictionary<string, object>>("setActiveChatDevice", parameters);

            // Store this device's session object for later use
            _thisDeviceSession = ParseClient.Instance.CreateObjectWithoutData<UserDeviceSession>(result["activeSessionId"].ToString());

            _logger.LogInformation("Successfully registered and activated this device session: {SessionId}", _thisDeviceSession.ObjectId);

            // Now, fetch other devices
            await FetchOtherDevicesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register device session.");
        }
    }

    private async Task FetchOtherDevicesAsync()
    {
        if (_authService.CurrentUser is null)
            return;
        try
        {
            var otherDevices = await ParseClient.Instance.CallCloudCodeFunctionAsync<IList<UserDeviceSession>>("getMyDeviceSessions", new Dictionary<string, object>());

            // Exclude the current device from the list
            var filteredDevices = otherDevices.Where(d => d.ObjectId != _thisDeviceSession.ObjectId);

            _otherDevicesCache.Edit(update => {
                update.Clear();
                update.AddOrUpdate(filteredDevices);
            });
            _logger.LogInformation("Fetched {Count} other device sessions.", filteredDevices.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch other device sessions.");
        }
    }

    public async Task MarkCurrentDeviceInactiveAsync()
    {
        if (_thisDeviceSession is null)
            return;
        try
        {
            var parameters = new Dictionary<string, object> { { "deviceName", DeviceInfo.Name } };
            await ParseClient.Instance.CallCloudCodeFunctionAsync<IDictionary<string, object>>("markSessionInactive", parameters);
            _logger.LogInformation("Marked this device session as inactive.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark device session inactive.");
        }
    }

    public async Task InitiateSessionTransferAsync(UserDeviceSession targetDevice, DimmerSharedSong currentSongState)
    {
        if (targetDevice is null || currentSongState is null || _authService.CurrentUser is null)
        {
            _logger.LogWarning("Aborting session transfer due to missing data.");
            return;
        }

        _logger.LogInformation("Initiating session transfer to {DeviceName}", targetDevice.DeviceName);

        try
        {
            // 1. Tell the server to make the target device the new active one.
            var parameters = new Dictionary<string, object> { { "selectedDeviceSessionObjectId", targetDevice.ObjectId } };
            await ParseClient.Instance.CallCloudCodeFunctionAsync<IDictionary<string, object>>("setActiveChatDevice", parameters);

            // 2. Save the song state object to get a pointer
            await currentSongState.SaveAsync();
            var songPointer = ParseClient.Instance.CreateObjectWithoutData<DimmerSharedSong>(currentSongState.ObjectId);

            // 3. Create and send the transfer message
            var message = new ChatMessage
            {
                Text = $"Session transfer for '{currentSongState.Title}'",
                MessageType = "SessionTransfer",
                UserSenderId = _authService.CurrentUserValue!.ObjectId,
                UserName = _authService.CurrentUserValue!.Username,
                SharedSong = songPointer
            };

            // IMPORTANT: Set an ACL so ONLY the target user (which is ourself) can read it.
            // This prevents other users from ever seeing these system messages.
            var acl = new ParseACL(ParseUser.CurrentUser)
            {
                
                PublicReadAccess = false,
                PublicWriteAccess  = false
            };
            message.ACL = acl;

            await message.SaveAsync();
            _logger.LogInformation("SessionTransfer message sent successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initiate session transfer.");
        }
    }

    public void StartListening()
    {
        // This is where you'll listen for incoming SessionTransfer messages.
        // It's part of the chat system, so it makes sense to handle it here.
        var messageQuery = new ParseQuery<ChatMessage>(ParseClient.Instance)
            .WhereEqualTo("messageType", "SessionTransfer"); // Only listen for transfers

        _messageSubscription = _liveQueryClient.Subscribe(messageQuery);
        _messageSubscription.On(Subscription.Event.Create, OnSessionTransferMessageReceived);
    }

    private async void OnSessionTransferMessageReceived(ChatMessage message)
    {
        _logger.LogInformation("Received potential SessionTransfer message.");

        // First, refresh our own device session status from the server
        await _thisDeviceSession.FetchAsync();

        if (_thisDeviceSession.Get<bool>("isActive"))
        {
            _logger.LogInformation("This device IS the active target. Processing transfer.");
            // We need to pass this event back up to the ViewModel to act on it.
            // We can use a simple event or a Subject<T>.
            var songPointer = message.Get<ParseObject>("SharedSong");
            if (songPointer != null)
            {
                var sharedSong = await songPointer.FetchAsync() as DimmerSharedSong;
                // !!! This is where you'd trigger an event to tell the UI to play the song.
                // For simplicity, we'll log for now. In a full app, you'd use a MessageBus or event.
                Debug.WriteLine($"EVENT: Play this song -> {sharedSong.Title} at {sharedSong.SharedPositionInSeconds}s");
            }
        }
        else
        {
            _logger.LogInformation("This device is not the active target. Ignoring transfer message.");
        }
    }

    public void StopListening()
    {
        _messageSubscription?.UnsubscribeNow();
    }

    public void Dispose()
    {
        StopListening();
        _otherDevicesCache?.Dispose();
    }
}