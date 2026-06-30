using SkiaSharp;
using System.IO.Compression;

namespace Dimmer.ViewModel;

public partial class SessionManagementViewModel : ObservableObject, IDisposable
{
    private readonly ILiveSessionManagerService _sessionManager;

    

    public ILiveSessionManagerService SessionManager => _sessionManager;
    public LoginViewModel LoginViewModel;
    private BaseViewModel _mainViewModel; // To get current song state
    public BaseViewModel BaseVM => _mainViewModel;
    private readonly ILogger<SessionManagementViewModel> _logger;
    private readonly CompositeDisposable _disposables = new();

    private readonly ReadOnlyObservableCollection<UserDeviceSession> _otherDevices;
    [ObservableProperty] public partial UserDeviceSession SelectedDevice { get; set; }
    public ReadOnlyObservableCollection<UserDeviceSession> OtherDevices => _otherDevices;
    [ObservableProperty]
    public partial UserModelOnline? CurrentUser { get; set; }
    [ObservableProperty]
    public partial ObservableCollection<FavSongsTransferClass> FavSongs { get; set; } 

    public ObservableCollection<BackupMetadata> AvailableBackups { get; } = new();
    
    [ObservableProperty]
    public partial string CurrentReferralCode { get; set; }

    [ObservableProperty]
    public partial string ReferralStats { get; set; }
    [ObservableProperty]
    public partial string StatusMessage { get; set; }

    [ObservableProperty]
    public partial bool IsTransferInProgress { get; set; }
    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial bool HasOnlineBackup { get; set; }

    [ObservableProperty]
    public partial bool IsBusyLoadingOnlineBackUp { get; set; }

    [ObservableProperty]
    public partial bool IsBusyConnectingToLiveQueries { get; set; }

    [ObservableProperty]
    public partial bool IsBusyLoadingDevices { get; set; }


    public SessionManagementViewModel(LoginViewModel loginViewModel,
        ILiveSessionManagerService sessionManager,
      ParseLiveQueryClient lqClient,
        ILogger<SessionManagementViewModel> logger,
        BaseViewModel mainViewModel)
    {
        LoginViewModel = loginViewModel ?? throw new ArgumentNullException(nameof(loginViewModel));
        _sessionManager = sessionManager;
        _logger = logger;
        _mainViewModel = mainViewModel;

        _liveQueryClient = lqClient;
        // Bind the list of other devices to our UI property
        _sessionManager.OtherAvailableDevices
            .ObserveOn(RxSchedulers.UI)
            .Bind(out _otherDevices)
            .Subscribe(devs=>
            {
                Debug.WriteLine(devs.Count);
            })
            .DisposeWith(_disposables);


        lQDisposables = new();
        LoginViewModel.WhenPropertyChanged(nameof(LoginViewModel.CurrentUserOnline), v => LoginViewModel.CurrentUserOnline)
            .ObserveOn(RxSchedulers.UI)
            .Subscribe(curUser =>
            {
                if (curUser is null) return;
                CurrentUser = curUser;

            })
            ;



    }
    CompositeDisposable? lQDisposables;


    private async Task LoadCloudDataAsync()
    {
        await LoadBackupsAsync();
        await LoadReferralInfoAsync();
    }
    [RelayCommand]
    public async Task LoadBackupsAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            List<BackupMetadata>? rawBackups = await _sessionManager.GetAvailableBackupsAsync();
            if (rawBackups is null) return;
            AvailableBackups.Clear();
            HasOnlineBackup = AvailableBackups.Count > 0;
            foreach (var item in rawBackups)
            {
                AvailableBackups.Add(item);
            }
        }
        finally 
        { 
            IsBusy = false; 
        }
    }

    [RelayCommand]
    public async Task CreateReferralCode()
    {
        IsBusy = true;
        try
        {
            var codeObj = await _sessionManager.GenerateReferralCodeAsync();
            UpdateReferralUI(codeObj);
        }
        catch (Exception ex)
        {
            StatusMessage = "Failed to generate code.";
            _logger.LogError(ex, "Referral generation error");
        }
        finally { IsBusy = false; }
    }

    private async Task LoadReferralInfoAsync()
    {
        var codeObj = await _sessionManager.GetMyReferralCodeAsync();
        UpdateReferralUI(codeObj);
    }

    private void UpdateReferralUI(Parse.ParseObject? codeObj)
    {
        if (codeObj == null)
        {
            CurrentReferralCode = string.Empty; // UI will show "Generate" button
            ReferralStats = "Join the program to invite friends.";
            return;
        }

        CurrentReferralCode = codeObj.Get<string>("referralCode");
        int uses = codeObj.ContainsKey("timesUsed") ? codeObj.Get<int>("timesUsed") : 0;
        int remaining = codeObj.ContainsKey("usesRemaining") ? codeObj.Get<int>("usesRemaining") : 0;

        ReferralStats = $"Used {uses} times ({remaining} remaining)";
    }

    private Subscription<DeviceCommand> _commandSub;

    // In your constructor or an Init method:
    private void SetupLiveQueries()
    {
        if (_commandSub != null) return; // Already setup
        if (lQDisposables == null) return; // Already setup

        var cmdQuery = new ParseQuery<DeviceCommand>(ParseClient.Instance)
     .WhereEqualTo("receiverId", ParseClient.Instance.CurrentUser.ObjectId)
     .WhereEqualTo("isProcessed", false); 

        _commandSub = _liveQueryClient.Subscribe(cmdQuery);


        _commandSub.Subscribes.Subscribe(evt =>
        {

        });

        _commandSub.On(Subscription.Event.Create, async cmd =>
        {
            _logger.LogInformation($"Remote Command Received: {cmd.Command}");
            if (_mainViewModel.IsBackGrounded) return;
            RxSchedulers.UI.ScheduleTo(async () =>
            {
                switch (cmd.Command)
                {


                    case "PAUSE":
                    case "PLAY":
                        _mainViewModel.PlayPauseToggleCommand.Execute(null);
                        break;
                    case "SEEK":
                        _mainViewModel.SeekTrackPosition(double.Parse(cmd.Payload));
                        break;

                    case "GetFavs":
                        await ShareListOfFavSongData(cmd);
                        break;
                    case "FAVS_READY":
                        await OnFavSongsListReceived(cmd);
                        break;
                }
            });

        });





    }

    
    public async Task RegisterCurrentDeviceAsync()
    {
        
            StatusMessage = "Registering device...";
            await _sessionManager.RegisterCurrentDeviceAsync();

        //_sessionManager.StartListeners();
        //await _sessionManager.SyncDeviceStateAsync();


        SetupLiveQueries();

    }
        
    public partial class FavSongsTransferClass : ObservableObject
    {
        public string TitleDurationKey { get; set; }
        public string ArtistName { get; set; }
        public string OtherArtistsName { get; set; }
        public string AlbumName { get; set; }
        public string GenreName { get; set; }
        public int PlayCompletedCount { get; set; }
        public int PlayCount { get; set; }
        public int ManualFavoriteCount { get; set; }
    }
    private async Task ShareListOfFavSongData(DeviceCommand cmd)
    {
        var allSongs = _mainViewModel.RealmFactory.GetRealmInstance().All<SongModel>().AsEnumerable();
        var listOfFavSongs = allSongs.Where(x => x.IsFavorite);
        var ll = listOfFavSongs.Select(x =>
        {
            return new FavSongsTransferClass()
            {
                TitleDurationKey = x.TitleDurationKey,
                ArtistName = x.ArtistName,
                OtherArtistsName = x.OtherArtistsName,
                GenreName = x.GenreName,
                AlbumName = x.AlbumName,
                PlayCompletedCount = x.PlayCompletedCount,
                PlayCount = x.PlayCount,
                ManualFavoriteCount = x.ManualFavoriteCount,
            };
        }
            );



        var options = new JsonSerializerOptions
        {

            MaxDepth = 64 // Increase if needed
        };
        var jsonString = JsonSerializer.Serialize<IEnumerable<FavSongsTransferClass>>(ll, options);

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
        var size = compressedBytes.Length;


        string cleanDeviceId = MyDeviceId.Replace("|", "_");

        // 2. Format the date to be URL-safe (e.g., 20260613_200259)
        string safeDate = DateTimeOffset.UtcNow.ToString("yyyyMMdd_HHmmss");

        // 3. Generate a completely Parse-compliant filename
        string fileName = $"Favs_{cleanDeviceId}_{safeDate}.json.gz";

        // The rest of your code remains exactly the same!
        ParseFile stateFile = new ParseFile(fileName, compressedBytes, "application/gzip");
        await stateFile.SaveAsync(ParseClient.Instance);



        /// 1. Create the reply command
        var replyCmd = new DeviceCommand
        {
            Command = "FAVS_READY", // A new command type for the receiver to listen for
            Payload = stateFile.Url.ToString(),
            TargetDeviceId = cmd.SenderDeviceId, // Send it back to the device that asked for it
            SenderDeviceId = MyDeviceId,
            SenderId = ParseClient.Instance.CurrentUser.ObjectId,
            ReceiverId = cmd.SenderId,
            IsProcessed = false
        };

        // 1. Create the ACL
        var acl = new ParseACL(ParseClient.Instance.CurrentUser);
        if (!string.IsNullOrEmpty(replyCmd.ReceiverId) && replyCmd.ReceiverId != replyCmd.SenderId)
        {
            acl.SetReadAccess(replyCmd.ReceiverId, true);
            acl.SetWriteAccess(replyCmd.ReceiverId, true);
        }

        // 2. FORCE the SDK to track it as a dirty operation using .Set() instead of the property
        replyCmd.Set("ACL", acl);

        await replyCmd.SaveAsync();

        // 4. Mark the original request as processed
        cmd.IsProcessed = true;
        await cmd.SaveAsync();
    }
    private async Task OnFavSongsListReceived(DeviceCommand cmd)
    {
        string? fileUrl = cmd.Payload;
        if (string.IsNullOrEmpty(fileUrl))
        {
            _logger.LogWarning("Received FavSongs command but Payload (URL) was empty.");
            return;
        }

        try
        {
            using var httpClient = new HttpClient();

            // 1. Download the compressed file as a raw stream (Very memory efficient)
            using var networkStream = await httpClient.GetStreamAsync(fileUrl);

            // 2. Wrap the network stream in a GZip decompressor
            using var decompressStream = new GZipStream(networkStream, CompressionMode.Decompress);

            // 3. Must use the exact same options as the sender to handle ReferenceHandler.Preserve
            var options = new JsonSerializerOptions
            {
                ReferenceHandler = ReferenceHandler.Preserve,
                MaxDepth = 64
            };

            // 4. Deserialize directly from the decompressed stream!
            var receivedSongsList = await JsonSerializer.DeserializeAsync<IEnumerable<FavSongsTransferClass>>(
                decompressStream, options);

            if (receivedSongsList != null)
            {
                // 5. Update the UI property. 
                // ObservableCollection updates must happen on the Main UI Thread in MAUI!
                RxSchedulers.UI.ScheduleTo(() =>
                {
                    FavSongs = new ObservableCollection<FavSongsTransferClass>(receivedSongsList);

                    //_logger.LogInformation($"Successfully loaded {FavSongs.Count} favorite songs from remote device.");
                });
            }

            // Mark the receive command as processed
            cmd.IsProcessed = true; 
            var acl = new ParseACL(ParseClient.Instance.CurrentUser);
            cmd.ACL = acl;

            await cmd.SaveAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to download and parse remote fav songs: {ex.Message}");
        }
    }

       
    public async Task TransferFullQueue(UserDeviceSession targetDevice)
    {
        var queue = _mainViewModel.PlaybackQueue.Select(song => new DimmerPlayEventView
        {
            SongId = song.Id,
            SongName = song.Title,
            ArtistName = song.ArtistName
        }).ToList();

        //await _sessionManager.InitiateQueueTransferAsync(targetDevice, queueEvents);
    }
    [RelayCommand]
    public async Task TransferToDevice(UserDeviceSession targetDevice)
    {
        // 1. Validate we have a a2ng
        var currentSong = _mainViewModel.CurrentPlayingSongView;
        if (targetDevice == null || currentSong == null)
        {
            StatusMessage = "No song is playing to transfer.";
            return;
        }

        IsTransferInProgress = true;
        StatusMessage = $"Sending session to {targetDevice.DeviceName}...";

        // 2. Map SongModelView to DimmerPlayEventView (or whatever your Service expects)
        // Your Service expects 'DimmerPlayEventView', so let's create a temporary one or map it.
        var songEventView = new DimmerPlayEventView
        {
            SongName = currentSong.Title,
            ArtistName = currentSong.ArtistName,
            AlbumName = currentSong.AlbumName,
            SongId = currentSong.Id, // CRITICAL: This ID allows the other device to find the file
            //CoverImagePath = currentSong.CoverImagePath,
            PositionInSeconds = _mainViewModel.CurrentTrackPositionSeconds,
            IsFav = currentSong.IsFavorite
        };

        // 3. Pass the object, NOT null
        await _sessionManager.InitiateSessionTransferAsync(targetDevice, songEventView);

        IsTransferInProgress = false;
        StatusMessage = "Transfer request sent.";
    }

   

  
    public void Dispose()
    {
        _disposables.Dispose();
        _sessionManager.StopListeners(); // Important
    }
    [RelayCommand]
    public async Task BackUpDataToCloud()
    {
        if (IsBusy) return;
        IsBusy = true;
        StatusMessage = "Generating Cloud Backup...";

        try
        {


                var result = await _sessionManager.CreateFullBackupAsync();
                StatusMessage = result;
                
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cloud backup failed");
            StatusMessage = "Backup failed.";
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    [RelayCommand]
    public async Task RestoreBackupAsync(string? backupObjectId)
    {
        if (backupObjectId is null) return;
        if (IsBusy) return;
        IsBusy = true;
        StatusMessage = "Generating Cloud Backup...";

        try
        {


                await _sessionManager.RestoreBackupAsync(backupObjectId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cloud backup failed");
            StatusMessage = "Backup failed.";
        }
        finally
        {
            IsBusy = false;
        }
    }


    public async Task UpdateProfilePicture(byte[]? resultByteArray)
    {
        if (resultByteArray == null) return;
        if (IsBusy) return;
        IsBusy = true;

        try
        {
            // Optimize image with SkiaSharp
            using var ms = new MemoryStream(resultByteArray);
            using var original = SKBitmap.Decode(ms);

            // Resize to standard avatar size (256x256, maintain aspect ratio)
            var resizeRatio = Math.Min(256f / original.Width, 256f / original.Height);
            var newWidth = (int)(original.Width * resizeRatio);
            var newHeight = (int)(original.Height * resizeRatio);

            using var resized = original.Resize(new SKImageInfo(newWidth, newHeight), SKFilterQuality.High);

            // Create square crop if needed (for perfect circle avatars)
            using var square = new SKBitmap(256, 256);
            using var canvas = new SKCanvas(square);

            // Center the image
            var x = (256 - newWidth) / 2;
            var y = (256 - newHeight) / 2;
            canvas.DrawBitmap(resized, x, y);

            // Encode to JPEG with compression
            using var outputMs = new MemoryStream();
            using var image = SKImage.FromBitmap(square);
            using var data = image.Encode(SKEncodedImageFormat.Jpeg, 85); // 85% quality
            data.SaveTo(outputMs);

            var optimizedBytes = outputMs.ToArray();

            _logger.LogInformation($"Original size: {resultByteArray.Length / 1024}KB, " +
                                   $"Optimized: {optimizedBytes.Length / 1024}KB");

            // Upload to Parse
            var parseUser = await ParseClient.Instance.CallCloudCodeFunctionAsync<UserModelOnline>(
                "uploadProfilePicture",
                new Dictionary<string, object>
                {
                { "imageData", Convert.ToBase64String(optimizedBytes) }
                });

            if (parseUser != null)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    LoginViewModel.CurrentUserOnline = parseUser;
                    StatusMessage = "Profile picture updated!";
                });

                // Broadcast update to other devices
                //await _sessionManager.BroadcastUserUpdateAsync(parseUser);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update profile picture");
            StatusMessage = "Failed to update profile picture";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [ObservableProperty]
    public partial bool IsScreeningActive { get; set; }

   


    // This is the "Virtual Library" of the device we are screening
    public ObservableCollection<SongModelView> RemoteLibrary { get; } = new();

  

    private readonly ParseLiveQueryClient _liveQueryClient;

 


    public async Task ProcessIncomingSnapshot(UserDeviceSession updatedSession)
    {
        if (updatedSession.FileData == null) return;

        StatusMessage = "Downloading snapshot...";
        var stream = await new HttpClient().GetStreamAsync(updatedSession.FileData.Url);

        // Lead Dev Suggestion: Use GZip for massive JSON lists
        using var decompressionStream = new GZipStream(stream, CompressionMode.Decompress);
        using var reader = new StreamReader(decompressionStream);
        string json = await reader.ReadToEndAsync();

        await LoadRemoteLibraryFromJSON(json);
        // Save to local cache
        string cachePath = Path.Combine(FileSystem.CacheDirectory, $"lib_{updatedSession.DeviceId}.json");
        await File.WriteAllTextAsync(cachePath, json);

    }

    private async Task LoadRemoteLibraryFromJSON(string json)
    {
        var songs = JsonSerializer.Deserialize<List<SongModelView>>(json);

        MainThread.BeginInvokeOnMainThread(() => {
            RemoteLibrary.Clear();
            foreach (var s in songs) RemoteLibrary.Add(s);
        });
    }

    private async Task LoadRemoteLibraryFromCache(string path)
    {
        string json = await File.ReadAllTextAsync(path);
        var songs = JsonSerializer.Deserialize<List<SongModelView>>(json);

        MainThread.BeginInvokeOnMainThread(() => {
            RemoteLibrary.Clear();
            foreach (var s in songs) RemoteLibrary.Add(s);
        });
    }

    private string GetLocalLibraryTag(string deviceId)
    {
        return Preferences.Get($"tag_{deviceId}", string.Empty);
    }

    private string MyDeviceId => ParseUser.CurrentUser.ObjectId + "_" + DeviceInfo.Current.DeviceType.ToString() + "_" + DeviceInfo.Current.Idiom;
    public async Task SendDeviceCommand(string commandParameter)
    {
        DeviceCommand newCmd = new DeviceCommand();
        newCmd.SenderId = ParseClient.Instance.CurrentUser.ObjectId; 
        newCmd.Command = commandParameter;
        newCmd.SenderDeviceId = MyDeviceId;
        newCmd.ReceiverDeviceId = MyDeviceId;
        newCmd.ReceiverId= ParseClient.Instance.CurrentUser.ObjectId;
        newCmd.IsProcessed = false;

        var acl = new ParseACL(ParseClient.Instance.CurrentUser);
        newCmd.ACL = acl;
        await newCmd.SaveAsync();
    }

    public async Task UpdateDeviceNameAsync(UserDeviceSession dev)
    {
        await dev.SaveAsync();
    }
}