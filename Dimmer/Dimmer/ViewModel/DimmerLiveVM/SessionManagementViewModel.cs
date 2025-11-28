

using System.IO;

namespace Dimmer.ViewModel;

public partial class SessionManagementViewModel : ObservableObject, IDisposable
{
    private readonly ILiveSessionManagerService _sessionManager;
    public LoginViewModel LoginViewModel;
    private BaseViewModel _mainViewModel; // To get current song state
    private readonly ILogger<SessionManagementViewModel> _logger;
    private readonly CompositeDisposable _disposables = new();

    private readonly ReadOnlyObservableCollection<UserDeviceSession> _otherDevices;
    public ReadOnlyObservableCollection<UserDeviceSession> OtherDevices => _otherDevices;

    public UserModelOnline? CurrentUser => LoginViewModel.CurrentUser;

    public bool IsBusy { get; private set; }
    [ObservableProperty]
    public partial string StatusMessage { get; set; }

    [ObservableProperty]
    public partial bool IsTransferInProgress { get; set; }


    public SessionManagementViewModel(LoginViewModel loginViewModel,
        ILiveSessionManagerService sessionManager,
        ILogger<SessionManagementViewModel> logger,
        BaseViewModel mainViewModel)
    {
        LoginViewModel = loginViewModel ?? throw new ArgumentNullException(nameof(loginViewModel));
        _sessionManager = sessionManager;
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

        // Lifecycle management: StartAsync/stop listeners when the viewmodel is used
        // In a real app, this would be tied to page appearing/disappearing
        _sessionManager.StartListeners();
    }
    [RelayCommand]
    public async Task RegisterCurrentDeviceAsync()
    {
        try
        {
            StatusMessage = "Registering device...";
            await _sessionManager.RegisterCurrentDeviceAsync();
            StatusMessage = "Device registered successfully!";
        }
        catch (Exception ex)
        {
            StatusMessage = "Error registering device.";
            _logger.LogError(ex, "Failed to register device for session transfer.");
        }
    }

    public async Task TransferToDevice(UserDeviceSession targetDevice, SongModelView song)
    {

        if (targetDevice == null || _mainViewModel.CurrentPlayingSongView == null)
        {
            StatusMessage = "No song is playing to transfer.";
            return;
        }

        IsTransferInProgress = true;
        StatusMessage = $"Sending session to {targetDevice.DeviceName}...";



        //var stream = await File.ReadAllBytesAsync(song.FilePath);

        //ParseChatService.GetSongMimeType(song, out var mimeType, out var fileExtension);

        //ParseFile songFile = new ParseFile($"{song.Title}.{song.FileFormat}", stream, mimeType);

        //await songFile.SaveAsync(ParseClient.Instance);

        await _sessionManager.InitiateSessionTransferAsync(targetDevice, null);

        IsTransferInProgress = false;
    }

    // This method is called when this device (Device B) receives a request
    private async void HandleIncomingTransferRequest(DimmerSharedSong request)
    {
        var songInfo = request;

        // Show a UI prompt to the user
        bool accept = await Shell.Current.DisplayAlert(
            "Session Transfer",
            $"Accept session for '{songInfo.Title}' ?",
            "Accept", "Decline"
        );

        if (!accept)
            return;

        StatusMessage = $"Downloading '{songInfo.Title}'...";

        // This is a simplified download. A real implementation would use a
        // dedicated download manager to handle progress, retries, etc.
        try
        {
            var httpClient = new HttpClient();
            var fileBytes = await httpClient.GetByteArrayAsync(songInfo.AudioFile.Url);

            // Save the file to a local temp path
            string tempPath = Path.Combine(FileSystem.CacheDirectory, songInfo.AudioFile.Name);
            await File.WriteAllBytesAsync(tempPath, fileBytes);

            StatusMessage = "Download complete! Starting playback...";

            // Now, tell the main ViewModel to play this downloaded song
            // You need a way to create a SongModelView from a temp file and metadata
            var newSong = new SongModelView
            {
                Title = songInfo.Title,
                ArtistName = songInfo.ArtistName,
                AlbumName = songInfo.AlbumName,
                FilePath = tempPath,
                // ... map other properties
            };

            await _mainViewModel.PlayTransferredSongAsync(newSong, songInfo.SharedPositionInSeconds ?? 0);
            // This is a crucial method you'll need in BaseViewModel
            await _mainViewModel.PlaySong(newSong);
            var sharedPosition = songInfo.SharedPositionInSeconds ?? 0;

            _mainViewModel.SeekTrackPosition(sharedPosition);

            // Let Device A know we got it.
            await _sessionManager.AcknowledgeTransferCompleteAsync(songInfo);

            StatusMessage = "Playback started!";
        }
        catch (Exception ex)
        {
            StatusMessage = "Error during transfer. Please try again.";
            _logger.LogError(ex, "Session transfer failed.");
        }
    }
    public void Dispose()
    {
        _disposables.Dispose();
        _sessionManager.StopListeners(); // Important
    }
    [RelayCommand]
    public async Task BackUpDeviceRealmFile()
    {
        // 1. Get the ACTIVE Realm instance to find the path
        // We don't use 'using' here because we don't want to close the UI's connection
        var realm = _mainViewModel.RealmFactory.GetRealmInstance();

        var sourcePath = realm.Config.DatabasePath;
        var tempPath = Path.Combine(FileSystem.CacheDirectory, "upload_temp.realm");

        // Ensure temp file is clean
        if (File.Exists(tempPath)) File.Delete(tempPath);

        try
        {
            IsBusy = true; // Optional: Show loading indicator

            // 2. THE BYPASS FIX:
            // Instead of realm.WriteCopy(), we manually copy the bytes.
            // FileShare.ReadWrite is the magic flag that lets us read a locked file.
            using (var sourceStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var destStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write))
            {
                await sourceStream.CopyToAsync(destStream);
            }

            // 3. Upload to Parse (Streamed)
            using (var uploadStream = new FileStream(tempPath, FileMode.Open, FileAccess.Read))
            {
                var parseFile = new ParseFile($"{CurrentUser?.ObjectId}_backup.realm.back", uploadStream);
                await parseFile.SaveAsync(ParseClient.Instance);

                var backupObj = new ParseObject("UserBackup");
                backupObj["user"] = CurrentUser;
                backupObj["backupFile"] = parseFile;
                backupObj["device"] = DeviceInfo.Name; // e.g. "Windows Desktop"
                backupObj["appVersion"] = AppInfo.VersionString;

                backupObj.ACL = new ParseACL(CurrentUser);
                await backupObj.SaveAsync();
            }

            _logger.LogInformation("Backup successful.");
            await Shell.Current.DisplayAlert("Success", "Backup uploaded successfully!", "OK");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Backup failed");
            await Shell.Current.DisplayAlert("Error", "Backup failed: " + ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
            // 4. Cleanup
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }
}