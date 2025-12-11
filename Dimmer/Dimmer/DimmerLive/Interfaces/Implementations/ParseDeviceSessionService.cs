using Parse.LiveQuery;

namespace Dimmer.DimmerLive.Interfaces.Implementations;
public class ParseDeviceSessionService : ILiveSessionManagerService, IDisposable
{
    private readonly ILogger<ParseDeviceSessionService> _logger;
    private readonly IAuthenticationService _authService;
    private readonly ParseLiveQueryClient _liveQueryClient;
    private readonly BaseViewModel vm;
    private Subscription<ChatMessage> _messageSubscription;
    private UserDeviceSession _thisDeviceSession; 
    private readonly Subject<DimmerSharedSong> _incomingTransfers = new();

    public IObservable<IChangeSet<UserDeviceSession, string>> OtherAvailableDevices => _otherDevicesCache.Connect();
    public IObservable<DimmerSharedSong> IncomingTransferRequests => _incomingTransfers.AsObservable();


    // The source cache for other devices
    private readonly SourceCache<UserDeviceSession, string> _otherDevicesCache = new(session => session.ObjectId);

    // Public observable property from the interface

    public ParseDeviceSessionService(ILogger<ParseDeviceSessionService> logger, IAuthenticationService authService, ParseLiveQueryClient liveQueryClient,BaseViewModel vm)
    {
        _logger = logger;
        _authService = authService;
        _liveQueryClient = liveQueryClient;
        this.vm = vm;
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
                { "currentDevicePlatform", DeviceInfo.Platform.ToString() },
                { "currentDeviceModel", DeviceInfo.Model.ToString() },
                { "currentDeviceManufacturer", DeviceInfo.Manufacturer.ToString() },
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

    public async Task InitiateSessionTransferAsync(UserDeviceSession targetDevice, DimmerPlayEventView currentSongView)
    {
        if (targetDevice is null || currentSongView is null || _authService.CurrentUserValue is null)
        {
            _logger.LogWarning("Aborting session transfer due to missing data.");
            return;
        }

        _logger.LogInformation("Initiating session transfer to {DeviceName}", targetDevice.DeviceName);

        try
        {
            // 1. Create Metadata Payload
            var metadata = new Dictionary<string, object>
        {
            { "Title", currentSongView.SongName },
            { "ArtistName", currentSongView.ArtistName },
            { "AlbumName", currentSongView.AlbumName },
            { "SongId", currentSongView.SongId?.ToString() }, // Send ID to lookup locally on other device
            { "CoverImagePath", currentSongView.CoverImagePath }
        };

            var parameters = new Dictionary<string, object>
        {
            { "targetSessionId", targetDevice.ObjectId },
            { "songMetadata", metadata },
            { "position", currentSongView.PositionInSeconds }
        };

            // 2. Call Cloud Code
            // This creates the DimmerSharedSong and the ChatMessage on the server side
            var result = await ParseClient.Instance.CallCloudCodeFunctionAsync<Dictionary<string, object>>("initiateSessionTransfer", parameters);

            _logger.LogInformation("SessionTransfer initiated via Cloud.");
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

        // The message contains a Pointer to 'DimmerSharedSong'
        var songPointer = message.Get<ParseObject>("sharedSong");

        if (songPointer != null)
        {
            try
            {
                // Fetch the metadata object
                // Since we stripped the audio file, this is a very small/fast fetch
                var sharedSongObj = await songPointer.FetchAsync();

                // Map ParseObject back to your DimmerSharedSong model or View Model
                // Assuming you have a DimmerSharedSong subclass of ParseObject:
                if (sharedSongObj is DimmerSharedSong sharedSong)
                {
                    _incomingTransfers.OnNext(sharedSong);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch shared song metadata.");
            }
        }
    }

    public async Task<string> CreateBackupAsync()
    {
        // 1. Check Login Status
        if (ParseClient.Instance.CurrentUser == null)
        {
            _logger.LogError("Cannot backup: User is not logged in via ParseClient.");
            return "Not Logged In";
        }

        try
        {
            _logger.LogInformation("Preparing local data for backup...");

            // 2. Get Data from Realm
            // We use ToList() to materialize the query so we can serialize it safely off the Realm thread
            var realmEvents = vm.RealmFactory.GetRealmInstance()
                                .All<DimmerPlayEvent>()
                                .ToList();
            var currentUser = ParseClient.Instance.CurrentUser;
            // 3. Convert to DTO/View objects to strip Realm-specific properties
            var eventViews = realmEvents.Select(x => x.ToDimmerPlayEventView()).ToList();

            if (eventViews.Count == 0)
            {
                return "No events to backup.";
            }

            // 4. Serialize to JSON String
            string jsonString = JsonSerializer.Serialize(eventViews);

            // 5. Prepare Parameters
            var parameters = new Dictionary<string, object>
        {
            { "eventsJson", jsonString }
        };

            _logger.LogInformation($"Uploading backup ({eventViews.Count} events)...");

            // 6. Call Cloud Code
            var result = await ParseClient.Instance.CallCloudCodeFunctionAsync<IDictionary<string, object>>("generateCloudBackup", parameters);

            if (result != null && result.ContainsKey("success") && (bool)result["success"])
            {
                var url = result.ContainsKey("url") ? result["url"].ToString() : "N/A";
                _logger.LogInformation($"Backup successful. File URL: {url}");
                return "Backup Successful";
            }

            return "Backup Failed (Server returned failure)";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating cloud backup");
            return $"Error: {ex.Message}";
        }
    }
    public async Task<List<ParseObject>> GetAvailableBackupsAsync()
    {
        if (_authService.CurrentUserValue == null) return new List<ParseObject>();

        try
        {
            var query = new ParseQuery<ParseObject>(ParseClient.Instance,"UserBackup")
                .WhereEqualTo("user", _authService.CurrentUserValue)
                .OrderByDescending("createdAt");

            var backups = await query.FindAsync();
            return backups.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch backups.");
            return new List<ParseObject>();
        }
    }

    public async Task RestoreBackupAsync(string backupObjectId)
    {
        try
        {
            var parameters = new Dictionary<string, object>
        {
            { "backupObjectId", backupObjectId }
        };

            var metaData = await ParseClient.Instance.CallCloudCodeFunctionAsync<IDictionary<string, object>>("getLatestBackupUrl", null);

            if (!metaData.ContainsKey("url"))
            {
                _logger.LogWarning("No backup URL returned.");
                return ;
            }

            string fileUrl = metaData["url"].ToString();

            // 2. Download the JSON File
            using HttpClient client = new HttpClient();
            var jsonString = await client.GetStringAsync(fileUrl);

            if (string.IsNullOrEmpty(jsonString)) return ;

            // 3. Deserialize JSON back to Objects
            var eventsFromCloud = JsonSerializer.Deserialize<List<DimmerPlayEventView>>(jsonString);

            if (eventsFromCloud == null || eventsFromCloud.Count == 0) return ;

            // 4. Write to Realm (UPSERT Logic)
            var realm = vm.RealmFactory.GetRealmInstance();

            await realm.WriteAsync(() =>
            {
                foreach (var view in eventsFromCloud)
                {
                    var rEvt = view.ToDimmerPlayEvent();
                    // The 'true' flag updates if ID exists, inserts if not
                    realm.Add(rEvt, update: true);
                }
            });

            _logger.LogInformation($"Restored {eventsFromCloud.Count} events to local database.");
          
        
       
    _logger.LogInformation("Restore completed.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring backup");
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