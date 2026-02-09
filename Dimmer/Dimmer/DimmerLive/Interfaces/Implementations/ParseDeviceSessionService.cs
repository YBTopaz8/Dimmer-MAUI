using System.IO.Compression;
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
            _logger.LogError("Cannot register device, user is not logged in.");
            throw new InvalidOperationException("No user authenticated");
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
            StartListeners();
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


    public async Task<string> CreateFullBackupAsync()
    {
        if (ParseClient.Instance.CurrentUser == null) return "Not Logged In";

        try
        {
            _logger.LogInformation("Gathering local data...");
            var realm = vm.RealmFactory.GetRealmInstance();

            // 1. Gather ALL Data into the container
            // Note: Ensure your .ToView() methods map all properties correctly!
            var backupData = new FullBackupData
            {
                Platform = DeviceInfo.Platform.ToString(),
                Songs = realm.All<SongModel>().AsEnumerable(),
                PlayEvents = realm.All<DimmerPlayEvent>().AsEnumerable(),
                Playlists = realm.All<PlaylistModel>().AsEnumerable(),
                Settings = realm.All<AppStateModel>().FirstOrDefault(),
                // Map UserStats if you have a ToView() for it, or direct object if no circular refs
                Stats = realm.All<UserStats>().AsEnumerable()
            };

            if (backupData.Songs.Count() == 0 && backupData.PlayEvents.Count() == 0)
                return "No data to backup.";

            _logger.LogInformation($"Serializing {backupData.Songs.Count()} songs and {backupData.PlayEvents.Count()} events...");

            // 2. Serialize to JSON
            string jsonString = JsonSerializer.Serialize(backupData);
            byte[] jsonBytes = System.Text.Encoding.UTF8.GetBytes(jsonString);

            // 3. Compress (GZip)
            byte[] compressedBytes;
            using (var outStream = new MemoryStream())
            {
                using (var archive = new GZipStream(outStream, CompressionLevel.Optimal))
                {
                    archive.Write(jsonBytes, 0, jsonBytes.Length);
                }
                compressedBytes = outStream.ToArray();
            }

            _logger.LogInformation($"Compressed size: {compressedBytes.Length / 1024} KB");

            // 4. Create ParseFile
            // Important: .json.gz extension helps identify content type
            string fileName = $"backup_{ParseClient.Instance.CurrentUser.ObjectId}_{DateTime.UtcNow.Ticks}.json.gz";
            ParseFile backupFile = new ParseFile(fileName, compressedBytes, "application/gzip");

            // Upload the file to Parse Storage
            await backupFile.SaveAsync(ParseClient.Instance);

            // 5. Link to Cloud Code
            var parameters = new Dictionary<string, object>
        {
            { "backupFile", backupFile },
            { "songCount", backupData.Songs.Count() },
            { "eventCount", backupData.PlayEvents.Count() },
            { "deviceName", DeviceInfo.Name }
        };

            // This function will handle the "Keep Max 3" logic
            var result = await ParseClient.Instance.CallCloudCodeFunctionAsync<IDictionary<string, object>>("saveBackupReference", parameters);

            return "Backup Successful";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Backup failed");
            return $"Error: {ex.Message}";
        }
    }

    public async Task<List<BackupMetadata>> GetAvailableBackupsAsync()
    {
        try
        {
            return null;
            var result = await ParseClient.Instance.CallCloudCodeFunctionAsync<IList<object>>("getUserBackups", null);
            var backups = new List<BackupMetadata>();

            foreach (Dictionary<string, object> item in result)
            {
                backups.Add(new BackupMetadata
                {
                    ObjectId = item.ContainsKey("objectId") ? item["objectId"].ToString() : "",
                    CreatedAt = item.ContainsKey("createdAt") ? DateTime.Parse(item["createdAt"].ToString()) : null,
                    SongCount = item.ContainsKey("songCount") ? int.Parse(item["songCount"].ToString()) : 0,
                    EventCount = item.ContainsKey("eventCount") ? int.Parse(item["eventCount"].ToString()) : 0,
                    DeviceName = item.ContainsKey("deviceName") ? item["deviceName"].ToString() : "Unknown",
                    FileUrl = item.ContainsKey("fileUrl") ? item["fileUrl"].ToString() : ""
                });
            }
            return backups;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch backups");
            return new List<BackupMetadata>();
        }
    }

    public async Task<bool> DeleteBackupAsync(string backupObjectId)
    {
        var parameters = new Dictionary<string, object> { { "backupId", backupObjectId } };
        try
        {
            await ParseClient.Instance.CallCloudCodeFunctionAsync<object>("deleteUserBackup", parameters);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete backup");
            return false;
        }
    }
    public async Task<ParseObject?> GenerateReferralCodeAsync()
    {
        if (_authService.CurrentUserValue == null) return null;
        try
        {
            // Calls the Cloud Code function we wrote
            return await ParseClient.Instance.CallCloudCodeFunctionAsync<ParseObject>("generateReferralCode", null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate referral code.");
            return null;
        }
    }
    public async Task<ParseObject?> GetMyReferralCodeAsync()
    {
        if (_authService.CurrentUserValue == null) return null;
        try
        {
            var query = new ParseQuery<ParseObject>(ParseClient.Instance, "ReferralCode")
                .WhereEqualTo("owner", _authService.CurrentUserValue);
            return await query.FirstAsync();
        }
        catch { return null; } // Return null if none exists
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

            if (!metaData.TryGetValue("url", out object? value))
            {
                _logger.LogWarning("No backup URL returned.");
                return ;
            }

            string? fileUrl = value.ToString();

            // 2. Download the JSON File
            using HttpClient client = new HttpClient();
            var jsonString = await client.GetStringAsync(fileUrl);

            if (string.IsNullOrEmpty(jsonString)) return ;

            // 3. Deserialize JSON back to Objects
            var eventsFromCloud = JsonSerializer.Deserialize<List<DimmerPlayEventView>>(jsonString);

            if (eventsFromCloud == null || eventsFromCloud.Count == 0) return ;


            var realm = vm.RealmFactory.GetRealmInstance();

            await realm.WriteAsync(() =>
            {
                foreach (var view in eventsFromCloud)
                {
                    var rEvt = view.ToDimmerPlayEvent()!;

                    var res = realm.Add(rEvt, update: true);
                    
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