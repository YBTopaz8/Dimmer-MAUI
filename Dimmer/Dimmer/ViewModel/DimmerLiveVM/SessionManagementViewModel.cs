

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

    public UserModelOnline? CurrentUser => LoginViewModel.CurrentUserOnline;

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
    [RelayCommand]
    public async Task TransferToDevice(UserDeviceSession targetDevice)
    {
        // 1. Validate we have a song
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
            var localSong = _mainViewModel.SearchResults.FirstOrDefault(s => s.Id.ToString() == request.OriginalSongId)
                            ?? _mainViewModel.SearchResults.FirstOrDefault(s => s.Title == request.Title && s.ArtistName == request.ArtistName);

            if (localSong != null)
            {
                StatusMessage = "Song found locally. Playing...";

                // 2. Play the LOCAL file
                await _mainViewModel.PlaySong(localSong);

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
                await Shell.Current.DisplayAlert("Backup", result, "OK");
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
}