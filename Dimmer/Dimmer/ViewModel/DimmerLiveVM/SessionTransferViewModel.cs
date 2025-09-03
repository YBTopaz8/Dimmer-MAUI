using CommunityToolkit.Mvvm.Input;

using Dimmer.DimmerLive.Interfaces.Implementations;
using Dimmer.DimmerSearch.Interfaces;

using DynamicData;

using Microsoft.Maui;

using ReactiveUI;

using Syncfusion.Maui.Toolkit.NavigationDrawer;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.ViewModel;
public partial class SessionTransferViewModel : ObservableObject, IDisposable
{
    private readonly ILiveSessionManagerService _sessionManager;
    private  BaseViewModel _mainViewModel; // To get current song state
    private readonly ILogger<SessionTransferViewModel> _logger;
    private readonly CompositeDisposable _disposables = new();

    private readonly ReadOnlyObservableCollection<UserDeviceSession> _otherDevices;
    public ReadOnlyObservableCollection<UserDeviceSession> OtherDevices => _otherDevices;

    [ObservableProperty]
    public partial string StatusMessage { get; set; }

    [ObservableProperty]
    public partial bool IsTransferInProgress { get; set; }


    public SessionTransferViewModel(
        ILiveSessionManagerService sessionManager,
        ILogger<SessionTransferViewModel> logger,
        BaseViewModel mainViewModel)
    {
        _sessionManager = sessionManager;
        _logger = logger;
        _mainViewModel = mainViewModel;

        // Bind the list of other devices to our UI property
        _sessionManager.OtherAvailableDevices
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out _otherDevices)
            .Subscribe()
            .DisposeWith(_disposables);

        // Listen for incoming transfer requests from the service's observable
        _sessionManager.IncomingTransferRequests
            .ObserveOn(RxApp.MainThreadScheduler)
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

    public async Task TransferToDevice(UserDeviceSession targetDevice,SongModelView song)
    {
        return;
        if (targetDevice == null || _mainViewModel.CurrentPlayingSongView == null)
        {
            StatusMessage = "No song is playing to transfer.";
            return;
        }

        IsTransferInProgress = true;
        StatusMessage = $"Sending session to {targetDevice.DeviceName}...";
 
      

        var stream = await File.ReadAllBytesAsync(song.FilePath);

        ParseChatService.GetSongMimeType(song, out var mimeType, out var fileExtension);

        ParseFile songFile = new ParseFile($"{song.Title}.{song.FileFormat}", stream, mimeType);

        await songFile.SaveAsync(ParseClient.Instance);

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
}