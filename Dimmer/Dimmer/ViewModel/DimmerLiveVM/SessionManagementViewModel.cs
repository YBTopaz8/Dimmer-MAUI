

using System.IO;

namespace Dimmer.ViewModel;

public partial class SessionManagementViewModel : ObservableObject, IDisposable
{
    private readonly ILiveSessionManagerService _sessionManager;
    private readonly IBluetoothSessionManagerService? _bluetoothSessionManager;
    public ILiveSessionManagerService SessionManager => _sessionManager;
    public LoginViewModel LoginViewModel;
    private BaseViewModel _mainViewModel; // To get current song state
    private readonly ILogger<SessionManagementViewModel> _logger;
    private readonly CompositeDisposable _disposables = new();

    private readonly ReadOnlyObservableCollection<UserDeviceSession> _otherDevices;
    public ReadOnlyObservableCollection<UserDeviceSession> OtherDevices => _otherDevices;

    private readonly ReadOnlyObservableCollection<BluetoothDeviceInfo> _bluetoothDevices;
    public ReadOnlyObservableCollection<BluetoothDeviceInfo> BluetoothDevices => _bluetoothDevices;

    public UserModelOnline? CurrentUser => LoginViewModel.CurrentUserOnline;

    public ObservableCollection<CloudBackupModel> AvailableBackups { get; } = new();
    public bool IsBusy { get; private set; }
    
    [ObservableProperty]
    public partial string CurrentReferralCode { get; set; }

    [ObservableProperty]
    public partial string ReferralStats { get; set; }
    
    [ObservableProperty]
    public partial string StatusMessage { get; set; }

    [ObservableProperty]
    public partial bool IsTransferInProgress { get; set; }

    [ObservableProperty]
    public partial bool IsBluetoothAvailable { get; set; }

    [ObservableProperty]
    public partial string BluetoothStatus { get; set; } = "Disconnected";

    [ObservableProperty]
    public partial bool UseBluetoothTransfer { get; set; }


    public SessionManagementViewModel(LoginViewModel loginViewModel,
        ILiveSessionManagerService sessionManager,
        ILogger<SessionManagementViewModel> logger,
        BaseViewModel mainViewModel,
        IBluetoothSessionManagerService? bluetoothSessionManager = null)
    {
        LoginViewModel = loginViewModel ?? throw new ArgumentNullException(nameof(loginViewModel));
        _sessionManager = sessionManager;
        _bluetoothSessionManager = bluetoothSessionManager;
        _logger = logger;
        _mainViewModel = mainViewModel;

        // Bind the list of other devices to our UI property
        _sessionManager.OtherAvailableDevices
            .ObserveOn(RxSchedulers.UI)
            .Bind(out _otherDevices)
            .Subscribe()
            .DisposeWith(_disposables);

        // Listen for incoming transfer requests from the service's observable
        _sessionManager.IncomingTransferRequests
            .ObserveOn(RxSchedulers.UI)
            .Subscribe(HandleIncomingTransferRequest)
            .DisposeWith(_disposables);

        // Setup Bluetooth if available
        if (_bluetoothSessionManager != null)
        {
            _bluetoothSessionManager.AvailableDevices
                .ObserveOn(RxSchedulers.UI)
                .Bind(out _bluetoothDevices)
                .Subscribe()
                .DisposeWith(_disposables);

            _bluetoothSessionManager.IncomingTransferRequests
                .ObserveOn(RxSchedulers.UI)
                .Subscribe(HandleIncomingBluetoothTransferRequest)
                .DisposeWith(_disposables);

            // Initialize Bluetooth with proper exception handling
            Task.Run(async () =>
            {
                try
                {
                    await InitializeBluetoothAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to initialize Bluetooth");
                }
            });
        }

        _ = LoadCloudDataAsync();

    }
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
            var rawBackups = await _sessionManager.GetAvailableBackupsAsync();

            AvailableBackups.Clear();
            foreach (var item in rawBackups)
            {
                AvailableBackups.Add(new CloudBackupModel(item));
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
            CurrentReferralCode = null; // UI will show "Generate" button
            ReferralStats = "Join the program to invite friends.";
            return;
        }

        CurrentReferralCode = codeObj.Get<string>("referralCode");
        int uses = codeObj.ContainsKey("timesUsed") ? codeObj.Get<int>("timesUsed") : 0;
        int remaining = codeObj.ContainsKey("usesRemaining") ? codeObj.Get<int>("usesRemaining") : 0;

        ReferralStats = $"Used {uses} times ({remaining} remaining)";
    }
    [RelayCommand]
    public async Task RegisterCurrentDeviceAsync()
    {
        try
        {
            StatusMessage = "Registering device...";
            await _sessionManager.RegisterCurrentDeviceAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = "Error registering device. "+ex.Message;
            
            _logger.LogError(ex, "Failed to register device for session transfer.");
        }
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
            CoverImagePath = currentSong.CoverImagePath,
            PositionInSeconds = _mainViewModel.CurrentTrackPositionSeconds,
            IsFav = currentSong.IsFavorite
        };

        // 3. Pass the object, NOT null
        await _sessionManager.InitiateSessionTransferAsync(targetDevice, songEventView);

        IsTransferInProgress = false;
        StatusMessage = "Transfer request sent.";
    }


    private async void HandleIncomingTransferRequest(DimmerSharedSong request)
    {
        // UI Prompt
        bool accept = await Shell.Current.DisplayAlert(
            "Session Transfer",
            $"Resume '{request.Title}' from {request.Uploader?.Username ?? "remote device"}?",
            "Yes", "No"
        );

        if (!accept) return;

        StatusMessage = "Syncing playback...";

        try
        {
            // --- NEW LOGIC: Metadata Only ---

            // 1. Try to find the song locally on THIS device
            // You need a method in your MainViewModel or DataService to find a song by ID or Title
            var localSong = _mainViewModel.RealmFactory.GetRealmInstance()
      .Find<DimmerPlayEvent>(request.OriginalSongId).SongsLinkingToThisEvent.FirstOrDefault() // Assuming ID matches
      ?? _mainViewModel.RealmFactory.GetRealmInstance()
      .All<DimmerPlayEvent>()
      .FirstOrDefault(s => s.SongName == request.Title && s.ArtistName == request.ArtistName).SongsLinkingToThisEvent.FirstOrDefault();
            if (localSong != null)
            {
                StatusMessage = "Song found locally. Playing...";

                // 2. Play the LOCAL file
                await _mainViewModel.PlaySongAsync(localSong.ToSongModelView());

                // 3. Seek to position
                if (request.SharedPositionInSeconds.HasValue)
                {
                    _mainViewModel.SeekTrackPosition(request.SharedPositionInSeconds.Value);
                }

                // 4. Acknowledge
                await _sessionManager.AcknowledgeTransferCompleteAsync(request);
            }
            else
            {
                // Fallback: If song not found locally, maybe search YouTube/Spotify?
                StatusMessage = "Song file not found on this device.";
                await Shell.Current.DisplayAlert("Missing File", $"Could not find '{request.Title}' on this device.", "OK");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Session transfer failed.");
            StatusMessage = "Transfer failed.";
        }
    }
    public void Dispose()
    {
        _disposables.Dispose();
        _sessionManager.StopListeners(); // Important
        _bluetoothSessionManager?.StopServer();
        (_bluetoothSessionManager as IDisposable)?.Dispose();
    }
    
    private async Task InitializeBluetoothAsync()
    {
        if (_bluetoothSessionManager == null)
            return;

        try
        {
            IsBluetoothAvailable = await _bluetoothSessionManager.IsBluetoothEnabledAsync();
            if (IsBluetoothAvailable)
            {
                await _bluetoothSessionManager.RefreshDevicesAsync();
                await _bluetoothSessionManager.StartServerAsync();
                BluetoothStatus = _bluetoothSessionManager.GetConnectionStatus();
            }
            else
            {
                BluetoothStatus = "Bluetooth not available";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Bluetooth");
            BluetoothStatus = "Bluetooth initialization failed";
        }
    }

    [RelayCommand]
    public async Task RefreshBluetoothDevicesAsync()
    {
        if (_bluetoothSessionManager == null || !IsBluetoothAvailable)
        {
            StatusMessage = "Bluetooth is not available";
            return;
        }

        try
        {
            StatusMessage = "Scanning for Bluetooth devices...";
            await _bluetoothSessionManager.RefreshDevicesAsync();
            BluetoothStatus = _bluetoothSessionManager.GetConnectionStatus();
            StatusMessage = $"Found {BluetoothDevices.Count} paired devices";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh Bluetooth devices");
            StatusMessage = "Failed to scan for devices";
        }
    }

    [RelayCommand]
    public async Task TransferToBluetoothDevice(BluetoothDeviceInfo targetDevice)
    {
        if (_bluetoothSessionManager == null || targetDevice == null)
            return;

        var currentSong = _mainViewModel.CurrentPlayingSongView;
        if (currentSong == null)
        {
            StatusMessage = "No song is playing to transfer.";
            return;
        }

        // Check if device is paired
        if (!await _bluetoothSessionManager.IsDevicePairedAsync(targetDevice.DeviceName))
        {
            bool openSettings = await Shell.Current.DisplayAlert(
                "Device Not Paired",
                $"'{targetDevice.DeviceName}' is not paired. Would you like to open Bluetooth settings to pair it?",
                "Yes", "No");

            if (openSettings)
            {
                await _bluetoothSessionManager.PromptPairingAsync();
            }
            return;
        }

        IsTransferInProgress = true;
        StatusMessage = $"Transferring session to {targetDevice.DeviceName}...";

        try
        {
            var songEventView = new DimmerPlayEventView
            {
                SongName = currentSong.Title,
                ArtistName = currentSong.ArtistName,
                AlbumName = currentSong.AlbumName,
                SongId = currentSong.Id,
                CoverImagePath = currentSong.CoverImagePath,
                PositionInSeconds = _mainViewModel.CurrentTrackPositionSeconds,
                IsFav = currentSong.IsFavorite
            };

            await _bluetoothSessionManager.InitiateSessionTransferAsync(targetDevice, songEventView);
            StatusMessage = "Transfer sent successfully!";
            BluetoothStatus = _bluetoothSessionManager.GetConnectionStatus();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bluetooth transfer failed");
            StatusMessage = $"Transfer failed: {ex.Message}";
        }
        finally
        {
            IsTransferInProgress = false;
        }
    }

    [RelayCommand]
    public async Task OpenBluetoothSettings()
    {
        if (_bluetoothSessionManager == null)
            return;

        try
        {
            await _bluetoothSessionManager.PromptPairingAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open Bluetooth settings");
        }
    }

    private void HandleIncomingBluetoothTransferRequest(DimmerSharedSong request)
    {
        // Fire-and-forget with proper exception handling
        Task.Run(async () =>
        {
            try
            {
                // UI Prompt
                bool accept = await Shell.Current.DisplayAlert(
                    "Bluetooth Session Transfer",
                    $"Resume '{request.Title}' by {request.ArtistName} from a paired device?",
                    "Yes", "No"
                );

                if (!accept) return;

                StatusMessage = "Loading song...";

                try
                {
                    // Try to find the song locally
                    var localSong = _mainViewModel.RealmFactory.GetRealmInstance()
                        .Find<DimmerPlayEvent>(request.OriginalSongId)?.SongsLinkingToThisEvent.FirstOrDefault()
                        ?? _mainViewModel.RealmFactory.GetRealmInstance()
                            .All<DimmerPlayEvent>()
                            .FirstOrDefault(s => s.SongName == request.Title && s.ArtistName == request.ArtistName)
                            ?.SongsLinkingToThisEvent.FirstOrDefault();

                    if (localSong != null)
                    {
                        StatusMessage = "Playing song...";
                        await _mainViewModel.PlaySongAsync(localSong.ToSongModelView());

                        if (request.SharedPositionInSeconds.HasValue)
                        {
                            _mainViewModel.SeekTrackPosition(request.SharedPositionInSeconds.Value);
                        }

                        StatusMessage = "Transfer completed successfully!";
                    }
                    else
                    {
                        StatusMessage = "Song not found on this device.";
                        await Shell.Current.DisplayAlert(
                            "Song Not Found",
                            $"Could not find '{request.Title}' by {request.ArtistName} on this device.",
                            "OK");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Bluetooth session transfer failed");
                    StatusMessage = "Transfer failed.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling Bluetooth transfer request");
            }
        });
    }
    [RelayCommand]
    public async Task BackUpDataToCloud()
    {
        if (IsBusy) return;
        IsBusy = true;
        StatusMessage = "Generating Cloud Backup...";

        try
        {


                var result = await _sessionManager.CreateBackupAsync();
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

    public void OnPageNavigatedTo()
    {

    }

    public async Task UpdateProfilePicture(byte[]? resultByteArray)
    {
        
        if (IsBusy) return;
        if (resultByteArray is null) return;

        // upload to Parse cloud and expect full User object back
        var parseUser = await ParseClient.Instance.CallCloudCodeFunctionAsync<UserModelOnline>("uploadProfilePicture", new Dictionary<string, object>
        {
            { "imageData", Convert.ToBase64String(resultByteArray) }
        });

        if (parseUser == null) return;
        LoginViewModel.CurrentUserOnline = parseUser;
    }
}