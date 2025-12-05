using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.ViewModel.DimmerLiveVM;


public partial class DimmerCloudViewModel : ObservableObject, IDisposable
{
    // --- Sub-ViewModels (aggregated) ---
    public LoginViewModel LoginVM { get; }
    public ChatViewModel ChatVM { get; }

    public SocialViewModel SocialVM { get; }
    // We integrate Session Logic directly here to fix the specific logic issues

    // --- Services ---
    private readonly ILiveSessionManagerService _sessionManager;
    private readonly BaseViewModel _mainViewModel;
    private readonly ILogger<DimmerCloudViewModel> _logger;
    private readonly CompositeDisposable _disposables = new();

    // --- State Properties ---
    [ObservableProperty]
    public partial bool IsLoggedIn { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; }

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    // --- Device Management Collections ---
    private readonly ReadOnlyObservableCollection<UserDeviceSession> _otherDevices;
    public ReadOnlyObservableCollection<UserDeviceSession> OtherDevices => _otherDevices;

    public DimmerCloudViewModel(
        LoginViewModel loginVM,
        SocialViewModel socialVM,
        ILiveSessionManagerService sessionManager,
        BaseViewModel mainVM,
        IAuthenticationService authService,
        ILogger<DimmerCloudViewModel> logger)
    {
        LoginVM = loginVM;
        SocialVM = socialVM;
        _sessionManager = sessionManager;
        _mainViewModel = mainVM;
        _logger = logger;

        // 1. Monitor Login State
        authService.CurrentUser
            .ObserveOn(RxSchedulers.UI)
            .Subscribe(user => IsLoggedIn = user != null)
            .DisposeWith(_disposables);

        // 2. Bind Devices from Service
        _sessionManager.OtherAvailableDevices
            .ObserveOn(RxSchedulers.UI)
            .Bind(out _otherDevices)
            .Subscribe()
            .DisposeWith(_disposables);

        // 3. Listen for Incoming Transfers (The Fix from previous step)
        _sessionManager.IncomingTransferRequests
            .ObserveOn(RxSchedulers.UI)
            .Subscribe(HandleIncomingTransfer)
            .DisposeWith(_disposables);

        // Start Listeners
        if (authService.IsLoggedIn) _sessionManager.StartListeners();
    }

    // --- CORRECTED CLOUD BACKUP COMMAND ---
    [RelayCommand]
    public async Task CreateCloudBackup()
    {
        if (IsBusy) return;
        IsBusy = true;
        StatusMessage = "Archiving data to cloud...";

        try
        {
            // Assuming we added CreateBackupAsync to the Interface as discussed
            if (_sessionManager is ParseDeviceSessionService parseService)
            {
                var result = await parseService.CreateBackupAsync();
                StatusMessage = $"Success: {result}";
            }
            else
            {
                StatusMessage = "Backup service unavailable.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = "Backup failed.";
            _logger.LogError(ex, "Backup error");
        }
        finally
        {
            IsBusy = false;
        }
    }

    // --- CORRECTED SESSION TRANSFER COMMAND ---
    [RelayCommand]
    public async Task TransferSessionToDevice(UserDeviceSession targetDevice)
    {
        var currentSong = _mainViewModel.CurrentPlayingSongView;

        if (targetDevice == null) return;
        if (currentSong == null)
        {
            StatusMessage = "Nothing is playing to transfer.";
            return;
        }

        IsBusy = true;
        StatusMessage = $"Handing off to {targetDevice.DeviceName}...";

        try
        {
            // Map to DimmerPlayEventView (Metadata Only Transfer)
            var transferPayload = new DimmerPlayEventView
            {
                SongName = currentSong.Title,
                ArtistName = currentSong.ArtistName,
                AlbumName = currentSong.AlbumName,
                SongId = currentSong.Id,
                CoverImagePath = currentSong.CoverImagePath,
                PositionInSeconds = _mainViewModel.CurrentTrackPositionSeconds,
                IsFav = currentSong.IsFavorite
            };

            await _sessionManager.InitiateSessionTransferAsync(targetDevice, transferPayload);
            StatusMessage = "Transfer initiated.";
        }
        catch (Exception ex)
        {
            StatusMessage = "Transfer failed.";
            _logger.LogError(ex, "Transfer error");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async void HandleIncomingTransfer(DimmerSharedSong request)
    {
        // In WinUI we might use a ContentDialog, but for now we set status
        StatusMessage = $"Incoming Transfer: {request.Title}";

        // Auto-accept logic for demo purposes (In real app, show dialog)
        var localSong = _mainViewModel.SearchResults.FirstOrDefault(s => s.Id.ToString() == request.OriginalSongId || (s.Title == request.Title && s.ArtistName == request.ArtistName));

        if (localSong != null)
        {
            await _mainViewModel.PlaySong(localSong);
            if (request.SharedPositionInSeconds.HasValue)
            {
                _mainViewModel.SeekTrackPosition(request.SharedPositionInSeconds.Value);
            }
            await _sessionManager.AcknowledgeTransferCompleteAsync(request);
            StatusMessage = $"Resumed {request.Title} from remote.";
        }
        else
        {
            StatusMessage = $"Could not find '{request.Title}' locally.";
        }
    }

    public void Dispose()
    {
        _sessionManager.StopListeners();
        _disposables.Dispose();
    }
}