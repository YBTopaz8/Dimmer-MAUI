using Dimmer.DimmerSearch.Interfaces;

using DynamicData;

using Parse.LiveQuery;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.DimmerLive.Interfaces.Implementations;
public class ParseDeviceSessionService : ILiveSessionManagerService, IDisposable
{
    private readonly ILogger<ParseDeviceSessionService> _logger;
    private readonly IAuthenticationService _authService;
    private readonly ParseLiveQueryClient _liveQueryClient;
    private Subscription<ChatMessage> _messageSubscription;
    private UserDeviceSession _thisDeviceSession; 
    private readonly Subject<DimmerSharedSong> _incomingTransfers = new();

    public IObservable<IChangeSet<UserDeviceSession, string>> OtherAvailableDevices => _otherDevicesCache.Connect();
    public IObservable<DimmerSharedSong> IncomingTransferRequests => _incomingTransfers.AsObservable();


    // The source cache for other devices
    private readonly SourceCache<UserDeviceSession, string> _otherDevicesCache = new(session => session.ObjectId);

    // Public observable property from the interface

    public ParseDeviceSessionService(ILogger<ParseDeviceSessionService> logger, IAuthenticationService authService, ParseLiveQueryClient liveQueryClient)
    {
        _logger = logger;
        _authService = authService;
        _liveQueryClient = liveQueryClient;
    }

    public async Task RegisterCurrentDeviceAsync()
    {
        if (_authService.CurrentUserValue == null)
        {
            _logger.LogWarning("Cannot register device, user is not logged in.");
            return;
        }

        try
        {
            var parameters = new Dictionary<string, object>
            {
                { "currentDeviceName", DeviceInfo.Name },
                { "currentDeviceId", Preferences.Get("MyDeviceId", Guid.NewGuid().ToString()) },
                { "currentDeviceIdiom", DeviceInfo.Idiom.ToString() },
                { "currentDeviceOSVersion", DeviceInfo.VersionString }
            };

            // This now returns the full object, not just a dictionary
            var result = await ParseClient.Instance.CallCloudCodeFunctionAsync<UserDeviceSession>("setActiveChatDevice", parameters);
            _thisDeviceSession = result;

            _logger.LogInformation("Successfully registered and activated this device session: {SessionId}", _thisDeviceSession.ObjectId);
            await FetchOtherDevicesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register device session.");
        }
    }

    private async Task FetchOtherDevicesAsync()
    {
        if (_authService.CurrentUserValue == null)
            return;
        try
        {
            var otherDevices = await ParseClient.Instance.CallCloudCodeFunctionAsync<IList<UserDeviceSession>>("getMyDeviceSessions", new Dictionary<string, object>());

            otherDevices = otherDevices.DistinctBy(x => x.DeviceName).ToList();
            // The cloud function getMyDeviceSessions should already exclude the current device if we modify it.
            // Or we can filter client-side.
            _otherDevicesCache.Edit(update => {
                update.Clear();
                update.AddOrUpdate(otherDevices);
            });
            _logger.LogInformation("Fetched {Count} other device sessions.", otherDevices.Count);
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
            await ParseClient.Instance.CallCloudCodeFunctionAsync<UserDeviceSession>("markSessionInactive", parameters);
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
            await ParseClient.Instance.CallCloudCodeFunctionAsync<UserDeviceSession>("setActiveChatDevice", parameters);

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
                TargetDeviceSessionId = targetDevice.ObjectId, 
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

    public void StartListeners()
    {
        if (_thisDeviceSession == null)
        {
            _logger.LogWarning("Cannot start session listeners, current device session is not registered.");
            return;
        }

        var messageQuery = new ParseQuery<ChatMessage>(ParseClient.Instance)
            .WhereEqualTo("messageType", "SessionTransfer")
            .WhereEqualTo("targetDeviceSessionId", _thisDeviceSession.ObjectId); // **Listen only for messages targeting this specific device**

        _messageSubscription = _liveQueryClient.Subscribe(messageQuery);
        _messageSubscription.On(
            Subscription.Event.Create,
            OnSessionTransferMessageReceived);
    }


    private async void OnSessionTransferMessageReceived(ChatMessage message)
    {
        _logger.LogInformation("Received targeted SessionTransfer message.");

        var songPointer = message.Get<ParseObject>("SharedSong");
        if (songPointer != null)
        {
            try
            {
                var sharedSong = await songPointer.FetchAsync() as DimmerSharedSong;
                if (sharedSong != null)
                {
                    // Fire the observable for the ViewModel to catch
                    _incomingTransfers.OnNext(sharedSong);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch shared song from transfer message.");
            }
        }
    }

    public void StopListeners()
    {
        _messageSubscription?.UnsubscribeNow();
    }

    public void Dispose()
    {
        StopListeners();
        _otherDevicesCache?.Dispose();
        _incomingTransfers?.Dispose();
    }

    public async Task AcknowledgeTransferCompleteAsync(DimmerSharedSong transferredSong)
    {
        _logger.LogInformation("Acknowledging transfer complete for {SongTitle}", transferredSong.Title);

        // We can notify the original device by sending a simple "system" message.
        // We need to know who the original uploader/sender was.
        var originalSender = transferredSong.Get<ParseUser>("uploader");
        if (originalSender == null)
            return;

        var message = new ChatMessage
        {
            MessageType = "SessionTransferAck", // Acknowledgment message type
            Text = $"Device '{DeviceInfo.Name}' has started playing '{transferredSong.Title}'.",
            // This needs a target user/device, but for simplicity, we can just send it
            // and the original device can listen for ACKs related to songs it uploaded.
        };
        // Set an ACL so only the original sender can read it.
        var acl = new ParseACL();
        acl.SetReadAccess(originalSender.ObjectId, true);
        message.ACL = acl;

        await message.SaveAsync();
    }
}