using Parse.LiveQuery;
using System.IO.Compression;
using System.Text.Json.Serialization;
using System.Xml.Linq;

namespace Dimmer.DimmerLive.Interfaces.Implementations;
public class ParseDeviceSessionService : ILiveSessionManagerService, IDisposable
{
    private readonly ILogger<ParseDeviceSessionService> _logger;
    private readonly IAuthenticationService _authService;
    private readonly ParseLiveQueryClient _liveQueryClient;
    private Subscription<ChatMessage> _messageSubscription;
    private UserDeviceSession _thisDeviceSession; 
    private readonly Subject<DimmerSharedSong> _incomingTransfers = new();

    private string MyDeviceId => Preferences.Get("MyDeviceId", Guid.NewGuid().ToString());
    private Subscription<RemotePlaybackCommand>? _commandSubscription;
    private Subscription<CloudScrobble>? _scrobbleSubscription;

    public IObservable<RemotePlaybackCommand> OnRemoteCommandReceived => _remoteCommandSubject.AsObservable();
    private readonly Subject<RemotePlaybackCommand> _remoteCommandSubject = new();

    public IObservable<CloudScrobble> OnLiveScrobbleReceived => _liveScrobbleSubject.AsObservable();
    private readonly Subject<CloudScrobble> _liveScrobbleSubject = new();

    public UserDeviceSession ThisDeviceSession => _thisDeviceSession;

    public IObservable<IChangeSet<UserDeviceSession, string>> OtherAvailableDevices { get; }
    public IObservable<DimmerSharedSong> IncomingTransferRequests { get; }

    // The source cache for other devices
    private readonly SourceCache<UserDeviceSession, string> _otherDevicesCache = new(session => session.ObjectId);


    private readonly BaseViewModel _vm;
    private PeriodicTimer? _heartbeatTimer;
    private CancellationTokenSource? _heartbeatCts;

    private DeviceState? _myCurrentState;
    private Subscription<DeviceCommand>? _commandSub;



 
    public ParseDeviceSessionService(ILogger<ParseDeviceSessionService> logger, IAuthenticationService authService, ParseLiveQueryClient liveQueryClient,BaseViewModel vm)
    {
        _logger = logger;
        _authService = authService;
        _liveQueryClient = liveQueryClient;
        this._vm = vm;

        OtherAvailableDevices = _otherDevicesCache.Connect();
        IncomingTransferRequests = _incomingTransfers.AsObservable();
    }

    class DimmerEventsBackUpModel
    {
        public required ObjectId? SongId { get; set; }
        public required string? SongName { get; set; }
        public required string? PlayEventStr { get; set; }
        public required ObjectId Id { get; set; }
        public required DateTimeOffset EventDate { get; set; }
        public required string? AudioOutputDeviceName { get; set; }
        public required double PositionInSeconds { get; set; }
    }
    public async Task RegisterCurrentDeviceAsync()
    {
        if (ParseUser.CurrentUser == null) return;





        // Create or Update Presence
        var query = new ParseQuery<DeviceState>(ParseClient.Instance).WhereEqualTo("deviceId", MyDeviceId);
        _myCurrentState = await query.FirstOrDefaultAsync() ?? new DeviceState { DeviceId = MyDeviceId };

        _myCurrentState.DeviceName = DeviceInfo.Name;
        _myCurrentState.Owner = ParseUser.CurrentUser;
        _myCurrentState.ACL = new ParseACL(ParseUser.CurrentUser);
        await _myCurrentState.SaveAsync();

        StartHeartbeat();
        StartCommandListener();
        await UploadLibraryMapAsync(); // Upload the "What I have" list
        await FetchOtherDevicesAsync();
    }

    private void StartCommandListener()
    {
        var cmdQuery = new ParseQuery<DeviceCommand>(ParseClient.Instance)
            .WhereEqualTo("targetDeviceId", MyDeviceId)
            .WhereEqualTo("isProcessed", false);

        _commandSub = _liveQueryClient.Subscribe(cmdQuery);
        _commandSub.On(Subscription.Event.Create, async cmd =>
        {
            _logger.LogInformation($"Remote Command Received: {cmd.Command}");

            RxSchedulers.UI.ScheduleTo(async () =>
            {
                switch (cmd.Command)
                {
                    case "PLAY_KEY":
                        await HandleRemotePlay(cmd.Payload); // Payload is TitleDurationKey
                        break;
                    case "PAUSE":
                        _vm.PlayPauseToggleCommand.Execute(null);
                        break;
                    case "SEEK":
                        _vm.SeekTrackPosition(double.Parse(cmd.Payload));
                        break;
                }
            });

            cmd.IsProcessed = true;
            await cmd.SaveAsync();
        });
    }
    public async Task InitiateSessionTransferAsync(UserDeviceSession targetDevice, DimmerPlayEventView currentSong)
    {
        // Instead of sending the full object, we send a command to the target device
        var cmd = new DeviceCommand
        {
            TargetDeviceId = targetDevice.DeviceId,
            SenderDeviceId = MyDeviceId,
            Command = "PLAY_KEY",
            Payload = currentSong.SongViewObject?.TitleDurationKey, // The minimalist ID
            IsProcessed = false,
            ACL = new ParseACL(ParseUser.CurrentUser)
        };
        await cmd.SaveAsync();

        // Also send the seek position as a secondary command
        await SendPlaybackCommandAsync(targetDevice.DeviceId, "SEEK", currentSong.PositionInSeconds.ToString());
    }
    private async Task HandleRemotePlay(string titleDurationKey)
    {
        var realm = _vm.RealmFactory.GetRealmInstance();
        var song = realm.All<SongModel>().FirstOrDefaultNullSafe(s => s.TitleDurationKey == titleDurationKey);

        if (song != null)
        {
            await _vm.PlaySongAsync(song.ToSongModelView());
        }
        else
        {
            _logger.LogWarning("Remote requested play for song not found locally.");
            // Optional: Trigger a YouTube search fallback here
        }
    }
    private void StartHeartbeat()
    {
        _heartbeatCts = new CancellationTokenSource();
        _heartbeatTimer = new PeriodicTimer(TimeSpan.FromSeconds(15));
        Task.Run(async () =>
        {
            while (await _heartbeatTimer.WaitForNextTickAsync(_heartbeatCts.Token))
            {
                if (_myCurrentState == null) continue;

                _myCurrentState.CurrentSongKey = _vm.CurrentPlayingSongView?.TitleDurationKey ?? "";
                _myCurrentState.Position = _vm.CurrentTrackPositionSeconds;
                _myCurrentState.IsPlaying = _vm.IsDimmerPlaying; 
                _myCurrentState.LastSeen = DateTime.UtcNow;
                await _myCurrentState.SaveAsync();
            }
        });
    }
    private async Task UploadLibraryMapAsync()
    {
        //var realm = _vm.RealmFactory.GetRealmInstance();
        //// We only care about the keys. This allows other devices to check if they have the file.
        //var keys = realm.All<SongModel>().AsEnumerable().Select(s => s.TitleDurationKey).ToList();

        //var json = JsonSerializer.Serialize(keys);
        //var file = new ParseFile($"lib_{MyDeviceId}.json", System.Text.Encoding.UTF8.GetBytes(json));
        //await file.SaveAsync(ParseClient.Instance);

        // Update the session object with the link to this map
        //var parameters = new Dictionary<string, object> { { "mapUrl", file.Url } };
        //await ParseClient.Instance.CallCloudCodeFunctionAsync<string>("updateDeviceLibraryMap", parameters);
    }

    public async Task SyncDeviceStateAsync()
    {

        return;

        if (ParseUser.CurrentUser == null) return;

        try
        {
            _logger.LogInformation("Zipping and syncing device state to Cloud...");
            var realm = _vm.RealmFactory.GetRealmInstance();

            // Extract ONLY what we need (to avoid Realm cross-thread exceptions)
            var allSongKeys = realm.All<SongModel>().AsEnumerable().Select(x => x.TitleDurationKey).ToList();
            var allPlayData = realm.All<DimmerPlayEvent>().AsEnumerable().Select(x =>
            {
                return new DimmerEventsBackUpModel() 
                {
                    SongId = x.SongId!,
                    SongName = x.SongName,
                    
                    PlayEventStr = x.PlayTypeStr,
                    PositionInSeconds = x.PositionInSeconds,
                    AudioOutputDeviceName = x.AudioOutputDevice?.Name,
                    EventDate = x.EventDate,
                    Id = x.Id,
                };
            }).ToList();
           
            var currentQueue = _vm.PlaybackQueue?.Select(x => x.TitleDurationKey).ToList();

            var stateData = new
            {
                Songs = allSongKeys,
                //Events = allPlayEvents,
                Queue = currentQueue,
                CurrentSong = _vm.CurrentPlayingSongView?.TitleDurationKey
                ,
                Events = allPlayData,
            };

            // Serialize & Compress

            var options = new JsonSerializerOptions
            {
                ReferenceHandler = ReferenceHandler.Preserve, // This handles cycles!
                MaxDepth = 64 // Increase if needed
            };
            string jsonString = JsonSerializer.Serialize(stateData, options);
            byte[] jsonBytes = System.Text.Encoding.UTF8.GetBytes(jsonString);

            byte[] compressedBytes;
            using (var outStream = new MemoryStream())
            {
                using (var archive = new GZipStream(outStream, CompressionLevel.Optimal))
                {
                    archive.Write(jsonBytes, 0, jsonBytes.Length);
                }
                compressedBytes = outStream.ToArray();
            }

            string fileName = $"state_{MyDeviceId}.json.gz";
            ParseFile stateFile = new ParseFile(fileName, compressedBytes, "application/gzip");
            await stateFile.SaveAsync(ParseClient.Instance);

            // Check if we already have a SyncState for this Device ID
            var query = new ParseQuery<DeviceSyncState>(ParseClient.Instance)
                .WhereEqualTo("deviceId", MyDeviceId)
                .WhereEqualTo("owner", ParseUser.CurrentUser);

            var existingState = await query.FirstOrDefaultAsync();

            var syncObj = existingState ?? new DeviceSyncState();
            syncObj.DeviceId = MyDeviceId;
            syncObj.DeviceName = DeviceInfo.Name;
            syncObj.Owner = ParseUser.CurrentUser;
            syncObj.StateFile = stateFile;

            // ACL: ONLY the current user can read/write this file. Ultimate Security.
            syncObj.ACL = new ParseACL(ParseUser.CurrentUser);

            await syncObj.SaveAsync();
            _logger.LogInformation("Device state synced successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync device state.");
        }
    }
    public async Task ScrobbleCurrentSongAsync(SongModelView song)
    {
        if (ParseUser.CurrentUser == null || song == null) return;

        try
        {
            var scrobble = new CloudScrobble
            {
                SongTitleDurationKey = song.TitleDurationKey,
                DeviceId = MyDeviceId,
                Owner = ParseUser.CurrentUser,
                ACL = new ParseACL(ParseUser.CurrentUser) // Private to user
            };
            await scrobble.SaveAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to scrobble song.");
        }
    }
    public async Task SendPlaybackCommandAsync(string targetDeviceId, string command, string payload = "")
    {
        if (ParseUser.CurrentUser == null) return;

        try
        {
            var cmd = new RemotePlaybackCommand
            {
                TargetDeviceId = targetDeviceId,
                SenderDeviceId = MyDeviceId,
                CommandType = command,
                Payload = payload,
                Owner = ParseUser.CurrentUser,
                ACL = new ParseACL(ParseUser.CurrentUser)
            };
            await cmd.SaveAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send remote command.");
        }
    }

    public void StartListeners()
    {
        if (ParseUser.CurrentUser == null) return;
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

        var updateQuery = new ParseQuery<AppUpdateModel>(ParseClient.Instance)
            ;

        var updateSubscription = _liveQueryClient.Subscribe(updateQuery);
        updateSubscription.On(Subscription.Event.Create,
        OnUpdatePushed);

        // Listen for Remote Commands targeted at THIS device (or "ALL")
        var commandQuery = new ParseQuery<RemotePlaybackCommand>(ParseClient.Instance)
            .WhereEqualTo("owner", ParseUser.CurrentUser)
            .WhereNotEqualTo("senderDeviceId", MyDeviceId); // Don't listen to our own commands

        _commandSubscription = _liveQueryClient.Subscribe(commandQuery);
        _commandSubscription.On(Subscription.Event.Create, cmd =>
        {
            if (cmd.TargetDeviceId == MyDeviceId || cmd.TargetDeviceId == "ALL")
            {
                _logger.LogInformation($"Received command: {cmd.CommandType}");
                _remoteCommandSubject.OnNext(cmd);
            }
        });

        // Listen for Scrobbles from OTHER devices
        var scrobbleQuery = new ParseQuery<CloudScrobble>(ParseClient.Instance)
            .WhereEqualTo("owner", ParseUser.CurrentUser)
            .WhereNotEqualTo("deviceId", MyDeviceId);

        _scrobbleSubscription = _liveQueryClient.Subscribe(scrobbleQuery);
        _scrobbleSubscription.On(Subscription.Event.Create, scrobble =>
        {
            _logger.LogInformation($"Remote device playing: {scrobble.SongTitleDurationKey}");
            _liveScrobbleSubject.OnNext(scrobble);
        });
    }
    public void StopListeners()
    {
        _messageSubscription?.UnsubscribeNow();
        _commandSubscription?.UnsubscribeNow();
        _scrobbleSubscription?.UnsubscribeNow();
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


    private void OnUpdatePushed(AppUpdateModel AppUpdateModel)
    {
        //throw new NotImplementedException();
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
            var realm = _vm.RealmFactory.GetRealmInstance();

            // 1. Gather ALL Data into the container
            // Note: Ensure your .ToView() methods map all properties correctly!
            var backupData = new FullBackupData
            {
                Platform = DeviceInfo.Platform.ToString(),
                Songs = realm.All<SongModel>().AsEnumerable(),
                PlayEvents = realm.All<DimmerPlayEvent>().AsEnumerable(),
                //Playlists = realm.All<PlaylistModel>().AsEnumerable(),
                Settings = realm.All<AppStateModel>().FirstOrDefault(),
                // Map UserStats if you have a ToView() for it, or direct object if no circular refs
                //Stats = realm.All<UserStats>().AsEnumerable()
            };

            if (!backupData.Songs.Any() && !backupData.PlayEvents.Any())
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


            var realm = _vm.RealmFactory.GetRealmInstance();

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



    public void Dispose()
    {
        StopListeners();
        _otherDevicesCache?.Dispose();
        _incomingTransfers?.Dispose();
        _heartbeatCts?.Cancel();
        _commandSub?.UnsubscribeNow();
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

        var acl = new ParseACL();
        acl.SetReadAccess(originalSender.ObjectId, true);
        message.ACL = acl;

        await message.SaveAsync();
    }
}