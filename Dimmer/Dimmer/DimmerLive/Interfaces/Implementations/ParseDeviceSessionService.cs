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

    //class DimmerEventsBackUpModel
    //{
    //    public required ObjectId? SongId { get; set; }
    //    public required string? SongName { get; set; }
    //    public required string? PlayEventStr { get; set; }
    //    public required ObjectId Id { get; set; }
    //    public required DateTimeOffset EventDate { get; set; }
    //    public required string? AudioOutputDeviceName { get; set; }
    //    public required double PositionInSeconds { get; set; }
    //}
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
            .WhereEqualTo(x=>x.UserOwner.ObjectId, ParseClient.Instance.CurrentUser.ObjectId)
           ;

        DevSessionSubscription = _liveQueryClient.Subscribe(devSessionsQuery);
        DevSessionSubscription.On(Subscription.Event.Create, cmd => HandleDeviceSessionEvent(cmd));
        DevSessionSubscription.On(Subscription.Event.Update, cmd => HandleDeviceSessionEvent(cmd));
        DevSessionSubscription.On(Subscription.Event.Delete, cmd =>
        {
            if (cmd.DeviceId != MyDeviceId) _otherDevicesCache.Remove(cmd);
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
    // A single, clean routing method
    private void HandleDeviceSessionEvent(UserDeviceSession cmd)
    {
        if (cmd.DeviceId == MyDeviceId)
        {
            _myCurrentSession = cmd;
            var ind = _otherDevicesCache.Items.FirstOrDefault(x => x.DeviceId == cmd.DeviceId);
            if(ind is null)
            {
                _otherDevicesCache.AddOrUpdate(cmd);
            }
            _logger.LogInformation("My device session updated: {SessionId}", cmd.ObjectId);
        }
        else
        {
            // If it's active, add/update it in the UI. If it went inactive, remove it from the UI.
            if (cmd.IsActive)
            {
                _otherDevicesCache.AddOrUpdate(cmd);
                _logger.LogInformation("Other device became active: {DeviceName}", cmd.Get<string>("deviceName"));
            }
            else
            {
                _otherDevicesCache.Remove(cmd);
                _logger.LogInformation("Other device went inactive: {DeviceName}", cmd.Get<string>("deviceName"));
            }
        }
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
                SongTitleDurationKey = song.TitleDurationKey!,
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

    public async Task<bool> CreateFullBackupAsync(string? devId)
    {
        var currentUser = ParseUser.CurrentUser;
        if (currentUser == null) return false;

        var myDevId = _myCurrentSession?.DeviceId ?? MyDeviceId;

        if (string.IsNullOrEmpty(devId) || devId == myDevId)
        {
            string tempRealmPath = Path.Combine(FileSystem.CacheDirectory, $"cloud_snap_{Guid.NewGuid():N}.realm");
            string tempGzPath = tempRealmPath + ".gz";

            try
            {
                var (songCount, eventCount, schemaVer) = await Task.Run(() =>
                {
                    using var realm = _vm.RealmFactory.GetRealmInstance();
                    int songs = realm.All<SongModel>().Count();
                    int events = realm.All<DimmerPlayEvent>().Count();
                    if (songs == 0 && events == 0)
                        return (0, 0, 0UL);

                    var copyConfig = new Realms.RealmConfiguration(tempRealmPath)
                    {
                        SchemaVersion = realm.Config.SchemaVersion
                    };
                    realm.WriteCopy(copyConfig);

                    // Stream-compress to disk (minimal RAM usage even for 50k+ rows)
                    using var input = File.OpenRead(tempRealmPath);
                    using var output = File.Create(tempGzPath);
                    using var gzip = new GZipStream(output, CompressionLevel.Optimal);
                    input.CopyTo(gzip);

                    return (songs, events, realm.Config.SchemaVersion);
                }).ConfigureAwait(false);

                if (songCount == 0 && eventCount == 0) return false;

                byte[] compressedBytes = await File.ReadAllBytesAsync(tempGzPath).ConfigureAwait(false);
                _logger.LogInformation("Compressed .realm backup: {KB} KB ({Songs} songs, {Events} events)",
                    compressedBytes.Length / 1024, songCount, eventCount);

                string fileName = $"backup_{currentUser.ObjectId}_{DateTime.UtcNow.Ticks}.realm.gz";
                var backupFile = new ParseFile(fileName, compressedBytes, "application/gzip");
                await backupFile.SaveAsync(ParseClient.Instance);

                var parameters = new Dictionary<string, object>
            {
                { "backupFile", backupFile },
                { "songCount", songCount },
                { "eventCount", eventCount },
                { "deviceName", DeviceInfo.Name },
                { "platform", DeviceInfo.Platform.ToString() },
                { "schemaVersion", (long)schemaVer }
            };

                await ParseClient.Instance.CallCloudCodeFunctionAsync<IDictionary<string, object>>("saveBackupReference", parameters);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Backup failed");
                return false;
            }
            finally
            {
                try { if (File.Exists(tempRealmPath)) File.Delete(tempRealmPath); } catch { }
                try { if (File.Exists(tempGzPath)) File.Delete(tempGzPath); } catch { }
            }
        }
        else
        {
            var cmd = new DeviceCommand
            {
                TargetDeviceId = devId,
                SenderDeviceId = myDevId,
                Command = "CREATE_BACKUP",
                IsProcessed = false,
                ACL = new ParseACL(currentUser)
            };
            await cmd.SaveAsync();
            return true;
        }
    }
    private static readonly HttpClient _httpClient = new();

    public async Task<bool> RestoreBackupAsync(string backupObjectId, bool replaceWholeDbIfSamePlatform = true)
    {
        string tempGz = Path.Combine(FileSystem.CacheDirectory, $"restore_{Guid.NewGuid():N}.realm.gz");
        string tempRealm = Path.Combine(FileSystem.CacheDirectory, $"restore_{Guid.NewGuid():N}.realm");

        try
        {
            var parameters = new Dictionary<string, object> { { "backupObjectId", backupObjectId } };
            var metaData = await ParseClient.Instance.CallCloudCodeFunctionAsync<IDictionary<string, object>>("getLatestBackupUrl", parameters);

            if (metaData == null || !metaData.TryGetValue("url", out var urlObj) || string.IsNullOrWhiteSpace(urlObj?.ToString()))
            {
                _logger.LogWarning("No backup URL returned.");
                return false;
            }

            string fileUrl = urlObj.ToString()!;

            // 1. Stream download & decompress to temp .realm file
            using (var netStream = await _httpClient.GetStreamAsync(fileUrl).ConfigureAwait(false))
            await using (var fs = File.Create(tempGz))
            {
                await netStream.CopyToAsync(fs).ConfigureAwait(false);
            }

            await Task.Run(() =>
            {
                using var input = File.OpenRead(tempGz);
                using var gzip = new GZipStream(input, CompressionMode.Decompress);
                using var output = File.Create(tempRealm);
                gzip.CopyTo(output);
            }).ConfigureAwait(false);

            // 2. Validate it opens cleanly
            string localDbPath;
            ulong localSchemaVer;
            using (var localRealm = _vm.RealmFactory.GetRealmInstance())
            {
                localDbPath = localRealm.Config.DatabasePath;
                localSchemaVer = localRealm.Config.SchemaVersion;
            }

            if (replaceWholeDbIfSamePlatform)
            {
                // Stage for clean swap on next startup before Realm opens
                using (var test = Realms.Realm.GetInstance(new Realms.RealmConfiguration(tempRealm)
                {
                    IsReadOnly = true,
                    SchemaVersion = localSchemaVer
                })) { }

                File.Copy(tempRealm, localDbPath + ".restore_pending", overwrite: true);
                _logger.LogInformation("Realm backup staged. Restart app to apply.");
                return true;
            }

            // Cross-platform merge mode: open downloaded .realm read-only & merge favorites/play history by TitleDurationKey
            await Task.Run(async () =>
            {
                using var backupRealm = Realms.Realm.GetInstance(new Realms.RealmConfiguration(tempRealm)
                {
                    IsReadOnly = true,
                    SchemaVersion = localSchemaVer
                });
                using var targetRealm = _vm.RealmFactory.GetRealmInstance();

                var localSongsByKey = targetRealm.All<SongModel>()
                    .AsEnumerable()
                    .Where(s => !string.IsNullOrEmpty(s.TitleDurationKey))
                    .GroupBy(s => s.TitleDurationKey!, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

                var backupSongKeysById = backupRealm.All<SongModel>()
                    .AsEnumerable()
                    .Where(s => !string.IsNullOrEmpty(s.TitleDurationKey))
                    .ToDictionary(s => s.Id, s => s.TitleDurationKey!);

                await targetRealm.WriteAsync(() =>
                {
                    // Merge favorites
                    foreach (var bSong in backupRealm.All<SongModel>().Filter("IsFavorite == true"))
                    {
                        if (!string.IsNullOrEmpty(bSong.TitleDurationKey) &&
                            localSongsByKey.TryGetValue(bSong.TitleDurationKey, out var localSong))
                        {
                            localSong.IsFavorite = true;
                        }
                    }

                    // Merge play events
                    foreach (var bEvt in backupRealm.All<DimmerPlayEvent>())
                    {
                        if (targetRealm.Find<DimmerPlayEvent>(bEvt.Id) != null) continue;

                        var cloned = new DimmerPlayEvent
                        {
                            Id = bEvt.Id,
                            SongName = bEvt.SongName,
                            IsFav = bEvt.IsFav,
                            PlayType = bEvt.PlayType,
                            PlayTypeStr = bEvt.PlayTypeStr,
                            DatePlayed = bEvt.DatePlayed,
                            WasPlayCompleted = bEvt.WasPlayCompleted,
                            PositionInSeconds = bEvt.PositionInSeconds,
                            EventDate = bEvt.EventDate,
                            DeviceName = bEvt.DeviceName,
                            DeviceFormFactor = bEvt.DeviceFormFactor,
                            DeviceModel = bEvt.DeviceModel,
                            DeviceManufacturer = bEvt.DeviceManufacturer,
                            DeviceVersion = bEvt.DeviceVersion
                        };

                        if (bEvt.SongId.HasValue &&
                            backupSongKeysById.TryGetValue(bEvt.SongId.Value, out var key) &&
                            localSongsByKey.TryGetValue(key, out var localSong))
                        {
                            cloned.SongId = localSong.Id;
                            cloned.SongName = localSong.Title;
                            localSong.PlayHistory.Add(cloned);
                        }

                        targetRealm.Add(cloned);
                    }
                });
            }).ConfigureAwait(false);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring backup");
            return false;
        }
        finally
        {
            try { if (File.Exists(tempGz)) File.Delete(tempGz); } catch { }
            try { if (File.Exists(tempRealm)) File.Delete(tempRealm); } catch { }
        }
    }
    public async Task<List<BackupMetadata>?> GetAvailableBackupsAsync()
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