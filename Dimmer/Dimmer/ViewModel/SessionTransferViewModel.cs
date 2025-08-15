using CommunityToolkit.Mvvm.Input;

using DynamicData;

using ReactiveUI;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.ViewModel;
public partial class SessionTransferViewModel : ObservableObject, IDisposable
{
    private readonly IPresenceService _presenceService;
    private readonly ISessionTransferService _sessionTransferService;
    private readonly BaseViewModel _mainViewModel; // To get current song state
    private readonly IDisposable _cleanup;

    private readonly ILogger<SessionTransferViewModel> _logger;
    // UI-bound properties

    private readonly ReadOnlyObservableCollection<UserDeviceSession> _otherDevices;
    public ReadOnlyObservableCollection<UserDeviceSession> OtherDevices => _otherDevices;


    [ObservableProperty]
    public partial string StatusMessage { get; set; }
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(TransferToDeviceCommand))] // This will re-evaluate CanExecute when IsTransferInProgress changes
    public partial bool IsTransferInProgress {get;set;}
    public SessionTransferViewModel(
        IPresenceService presenceService,
        ISessionTransferService sessionTransferService,
        ILogger<SessionTransferViewModel> logger,
        BaseViewModel mainViewModel)
    {
        _presenceService = presenceService;
        _sessionTransferService = sessionTransferService;
        _mainViewModel = mainViewModel;

        _logger = logger;

        // Bind the list of other devices to our UI property
        var deviceLoader = _presenceService.OtherActiveDevices
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out _otherDevices)
            .Subscribe();

        // Listen for incoming transfer requests
        var requestListener = _sessionTransferService.IncomingTransferRequests
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(HandleIncomingTransferRequest);

        _cleanup = new CompositeDisposable(deviceLoader, requestListener);
    }

    // Command bound to a button next to each device in the list
    [RelayCommand]
    private async Task TransferToDevice(UserDeviceSession targetDevice)
    {
        if (targetDevice == null || _mainViewModel.CurrentPlayingSongView == null)
        {
            StatusMessage = "No song is playing to transfer.";
            return;
        }

        IsTransferInProgress = true;
        StatusMessage = $"Uploading and sending session to {targetDevice.DeviceName}...";

        await _sessionTransferService.InitiateTransferAsync(
            targetDevice,
            _mainViewModel.CurrentPlayingSongView,
            _mainViewModel.CurrentTrackPositionSeconds
        );

        StatusMessage = "Session transfer initiated!";
        // You might want a timeout here
        await Task.Delay(2000); // Give user time to read message
        IsTransferInProgress = false;
    }

    // This method is called when this device (Device B) receives a request
    private async void HandleIncomingTransferRequest(SessionTransferRequest request)
    {
        var songInfo = request.SongToTransfer;

        // Show a UI prompt to the user
        bool accept = await Shell.Current.DisplayAlert(
            "Session Transfer",
            $"Accept session for '{songInfo.Title}' from {request.FromDeviceName}?",
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
            await _sessionTransferService.AcknowledgeTransferCompleteAsync(songInfo);

            StatusMessage = "Playback started!";
        }
        catch (Exception ex)
        {
            StatusMessage = "Error during transfer. Please try again.";
            _logger.LogError(ex, "Session transfer failed.");
        }
    }

    public void Dispose() => _cleanup.Dispose();
}