using System.IO.Compression;

namespace Dimmer.DimmerLive.Interfaces.Implementations;
public class ParseDeviceSessionService : ILiveSessionManagerService, IDisposable
{
    private readonly ILogger<ParseDeviceSessionService> _logger;
    private readonly IAuthenticationService _authService;
    private readonly ParseLiveQueryClient _liveQueryClient;
    private Subscription<ChatMessage> _messageSubscription;

    private readonly Subject<DimmerSharedSong> _incomingTransfers = new();

    private string MyDeviceId => ParseUser.CurrentUser.ObjectId +"|" + Environment.MachineName + "|" + Environment.OSVersion.Platform + "|" + Environment.OSVersion.VersionString;
    private Subscription<CloudScrobble>? _scrobbleSubscription;

    public IObservable<DeviceCommand> OnRemoteCommandReceived => _remoteCommandSubject.AsObservable();
    private readonly Subject<DeviceCommand> _remoteCommandSubject = new();

    public IObservable<CloudScrobble> OnLiveScrobbleReceived => _liveScrobbleSubject.AsObservable();
    private readonly Subject<CloudScrobble> _liveScrobbleSubject = new();



    public IObservable<IChangeSet<UserDeviceSession, string>> OtherAvailableDevices { get; }
    public IObservable<DimmerSharedSong> IncomingTransferRequests { get; }

    // The source cache for other devices
    private readonly SourceCache<UserDeviceSession, string> _otherDevicesCache = new(session => session.ObjectId);


    private readonly BaseViewModel _vm;


    private UserDeviceSession? _myCurrentSession;
    private Subscription<DeviceCommand>? _commandSub;
    public Subscription<UserDeviceSession> DevSessionSubscription { get; private set; }

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

        try
        {
            _logger.LogInformation("Registering current device session...");

            StartCommandListener();



        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register current device session.");
        }
    }

    private void StartCommandListener()
    {
        var devSessionsQuery = new ParseQuery<UserDeviceSession>(ParseClient.Instance)
            .WhereEqualTo("userOwner.objectId", ParseClient.Instance.CurrentUser.ObjectId)
           ;

        DevSessionSubscription = _liveQueryClient.Subscribe(devSessionsQuery);
        DevSessionSubscription.On(Subscription.Event.Create, cmd =>
        {
            if(cmd.DeviceId == MyDeviceId)
            {
                _myCurrentSession = cmd;
                _logger.LogInformation("My device session created: {SessionId}", cmd.ObjectId);
            }
            else
            {
                _otherDevicesCache.AddOrUpdate(cmd);
                _logger.LogInformation("Other device session created: {SessionId}", cmd.ObjectId);
            }
            

        });
        DevSessionSubscription.On(Subscription.Event.Update, cmd =>
        {
            if (cmd.DeviceId == MyDeviceId)
            {
                _myCurrentSession = cmd;
                _logger.LogInformation("My device session created: {SessionId}", cmd.ObjectId);
            }
            if (_otherDevicesCache.Items.FirstOrDefault(x => x.ObjectId == cmd.ObjectId) != null)
            {  
                _otherDevicesCache.AddOrUpdate(cmd);
                _logger.LogInformation("Other device session created: {SessionId}", cmd.ObjectId);
            }
            

        });


        DevSessionSubscription.Subscribes.Subscribe(async q =>
        {


            Debug.WriteLine("Successfully Subscribed! Now listening for data...");

            // 1. Prepare parameters for the Cloud Function you already wrote
            var parameters = new Dictionary<string, object>
            {
                { "deviceId", MyDeviceId },
                { "deviceName", DeviceInfo.Name },
                { "deviceIdiom", DeviceInfo.Idiom.ToString() },
                { "deviceOSVersion", DeviceInfo.VersionString },
                { "deviceManufacturer", System.Environment.MachineName },
                { "deviceModel", DeviceInfo.Model }
            };

            try
            {
                // 2. Call your Cloud Function instead of SaveAsync()
                // This will securely create the object on the server AND set the ACL properly!
                var result = await ParseClient.Instance.CallCloudCodeFunctionAsync<UserDeviceSession>(
                    "registerDevicePresence",
                    parameters
                );

                Debug.WriteLine($"Created Session via Cloud Code! ID: {result.ObjectId}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to register device presence: {ex.Message}");
            }
        });
      


    }
    private async Task SendDeviceReplyAsync(string targetDeviceId, string text)
    {
        try
        {
            var message = new ChatMessage
            {
                Text = text,
                MessageType = "System", // Or "TerminalResponse"
            };
            message["UserName"] = DeviceInfo.Name + " (Device)";
            message["senderId"] = MyDeviceId;
            message["UserSenderId"] = ParseUser.CurrentUser.ObjectId;

            await message.SaveAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send device reply.");
        }
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

    class LibraryUploadMapSongModel
    {
        public required string TitleAndDurationKey { get; set; }
        public required string Title { get; set; }
        public int Duration { get; set; }
        public string ArtistName { get; set; } = string.Empty;
        public string? AlbumName { get; set; }
        public string? GenreName { get; set; }
    }
   

    public async Task SyncDeviceStateAsync()
    {

        return;


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
 

    public void StartListeners()
    {
        if (ParseUser.CurrentUser == null) return;
       

       


        var updateQuery = new ParseQuery<AppUpdateModel>(ParseClient.Instance)
            ;

        var updateSubscription = _liveQueryClient.Subscribe(updateQuery);
        updateSubscription.On(Subscription.Event.Create,
        OnUpdatePushed);




        // Listen for Scrobbles from OTHER devices
        var scrobbleQuery = new ParseQuery<CloudScrobble>(ParseClient.Instance)
            .WhereEqualTo("owner", ParseUser.CurrentUser);

        _scrobbleSubscription = _liveQueryClient.Subscribe(scrobbleQuery);
        _scrobbleSubscription.On(Subscription.Event.Create, scrobble =>
        {
            _logger.LogInformation($"Remote device playing: {scrobble.SongTitleDurationKey}");
            _liveScrobbleSubject.OnNext(scrobble);
        });
        _scrobbleSubscription.On(Subscription.Event.Enter, scrobble =>
        {
            _logger.LogInformation($"Remote device playing: {scrobble.SongTitleDurationKey}");
            _liveScrobbleSubject.OnNext(scrobble);
        });
        _scrobbleSubscription.On(Subscription.Event.Update, scrobble =>
        {
            _logger.LogInformation($"Remote device playing: {scrobble.SongTitleDurationKey}");
            _liveScrobbleSubject.OnNext(scrobble);
        });

    }
    public void StopListeners()
    {
        _messageSubscription?.UnsubscribeNow();

        _scrobbleSubscription?.UnsubscribeNow();
    }
   

    public async Task MarkCurrentDeviceInactiveAsync()
    {
      

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

        _commandSub?.UnsubscribeNow();
        DevSessionSubscription?.UnsubscribeNow();
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

public enum DimmerCommandsEnum
{
    StatePresence,
    Play,
    Pause,
    Stop,
    RemovePresence,
    GetNowPlayingQueue,
    GetListOfSongs,
    
}