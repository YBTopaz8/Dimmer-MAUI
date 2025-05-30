// --- START OF FILE BaseViewModelWin.cs ---
using CommunityToolkit.Mvvm.ComponentModel; // For ObservableObject
using CommunityToolkit.Mvvm.Input;
using Dimmer.Data.Models;
using Dimmer.Interfaces;
using Dimmer.Interfaces.Services;
using Dimmer.Utilities.Extensions;
using Microsoft.Extensions.Logging; // For ILogger
using Microsoft.Extensions.Logging.Abstractions; // For NullLogger
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading; // For SynchronizationContext
using System.Threading.Tasks;
using ZXing.Net.Maui.Controls; // For BarcodeGeneratorView
using Microsoft.Maui.ApplicationModel.DataTransfer; // For Share
using Microsoft.Maui.Storage; // For FilePicker, FilePickerFileType, PickOptions, ShareFile
// Assuming SkiaSharp and ZXing.SkiaSharp are correctly referenced for barcode scanning
using SkiaSharp;
using ZXing;

// Assuming Vanara.PInvoke.Shell32 and TaskbarList are for Windows-specific taskbar progress
using Vanara.PInvoke;
using Vanara.Windows.Shell;


namespace Dimmer.WinUI.ViewModel; // Assuming this is your WinUI ViewModel namespace

public partial class BaseViewModelWin : BaseViewModel // BaseViewModel is in Dimmer.ViewModel
{
    // Specific to BaseViewModelWin
    private readonly IFilePicker _filePicker;
    // TrayIconHelper removed as it wasn't used in the provided snippet and might be platform-specific UI concern

    [ObservableProperty]
    public string? barCodeInvitationValue; // Removed initial value that depended on ParseClient

    [ObservableProperty]
    public ObservableCollection<SongModelView>? displayedSongs; // This will be populated from _stateService.AllCurrentSongs

    [ObservableProperty]
    public CollectionView? songLyricsCV; // For scrolling lyrics

    // No need for _stateService and _mapper again if they are protected in BaseViewModel and accessible
    // private readonly IDimmerStateService _stateServiceWin; // Use _stateService from base
    // private readonly IMapper _mapperWin; // Use _mapper from base

    // Constructor corrected to match BaseViewModel's signature
    public BaseViewModelWin(
       IMapper mapper, // For BaseViewModel
                       // BaseAppFlow baseAppFlow, // This parameter is removed as BaseAppFlow is a service now, not passed like this
       IDimmerLiveStateService dimmerLiveStateService, // For BaseViewModel
       AlbumsMgtFlow albumsMgtFlow,       // For BaseViewModel
       PlayListMgtFlow playlistsMgtFlow,  // For BaseViewModel
       SongsMgtFlow songsMgtFlow,        // For BaseViewModel
       IDimmerStateService stateService,    // For BaseViewModel
       ISettingsService settingsService,   // For BaseViewModel
       SubscriptionManager subsManager,     // For BaseViewModel
       LyricsMgtFlow lyricsMgtFlow,       // For BaseViewModel
       IFolderMgtService folderMgtService,  // For BaseViewModel
       ILogger<BaseViewModel> baseLogger, // Logger for BaseViewModel
                                          // --- Dependencies specific to BaseViewModelWin ---
       IFilePicker filePicker,
       ILogger<BaseViewModelWin> winLogger // Specific logger for BaseViewModelWin
   ) : base(mapper, dimmerLiveStateService, albumsMgtFlow, playlistsMgtFlow, songsMgtFlow,
            stateService, settingsService, subsManager, lyricsMgtFlow, folderMgtService, baseLogger)
    {
        // _mapperWin = mapper; // Use _mapper from base
        // _stateServiceWin = stateService; // Use _stateService from base
        // _subs is managed by base class's _subsManager or local if needed

        this._filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
        // Use winLogger for BaseViewModelWin specific logging if different from baseLogger
        // For simplicity, assuming _logger from BaseViewModel is sufficient or BaseViewModelWin injects its own.
        // If BaseViewModelWin needs its own separate logger instance:
        // this._winLogger = winLogger ?? NullLogger<BaseViewModelWin>.Instance;

        // Initialize() is called by BaseViewModel constructor now.
        // InitializeWindowsSpecificFeatures(); // Call a new method for Win-specific init
    }

    // Call this from your UI after the ViewModel is constructed and MainPage is ready
    public bool InitializeWindowSpecificFeatures(Microsoft.UI.Xaml.Window window) // Pass the main window
    {
        IsSettingWindoOpened = false; // Default state

        // Example of how to get window handle for TaskbarList if needed for PlatUtils
        // This is highly platform-specific.
        // IntPtr windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(window);
        // PlatUtils.DimmerHandle = windowHandle; // Assuming PlatUtils needs this

        // Removed IsUserFirstTimeOpening logic here, should be handled by AppInitializerService
        // The decision to show/hide main view based on first time should be driven by state or navigation.

        // ResetDisplayedMasterList(); // This is now handled by subscribing to _stateService.AllCurrentSongs in BaseViewModel
        SubscribeToLyricIndexChangesForWinUI(); // WinUI specific lyric scrolling
        SubscribeToPositionForWinUITaskbar();    // WinUI specific taskbar progress

        // Simulate loading online data - replace with actual logic
        // For User Barcode, it should observe _stateService.CurrentUser
        _subsManager.Add(
            _stateService.CurrentUser
                .ObserveOn(SynchronizationContext.Current!)
                .Subscribe(user =>
                {
                    // Assuming UserOnline.ObjectId is what you want for barcode.
                    // This needs to be adapted based on how UserOnline is managed (e.g., via IDimmerLiveStateService)
                    // For now, let's assume a placeholder or local user ID.
                    BarCodeInvitationValue = user?.Id.ToString() ?? "NoUserLoggedIn";
                })
        );

        _logger.LogInformation("BaseViewModelWin: Windows-specific features initialized.");
        return true; // Indicate success
    }

    private void SubscribeToPositionForWinUITaskbar()
    {
        // This subscription is for updating the Windows Taskbar progress.
        // The actual CurrentPositionInSeconds and CurrentPositionPercentage properties
        // are updated by BaseViewModel's subscription to SongsMgtFlow.AudioEnginePositionObservable.
        _subsManager.Add(
            SongsMgtFlow.AudioEnginePositionObservable // Use the one from SongsMgtFlow via BaseViewModel
                .ObserveOn(SynchronizationContext.Current!) // Ensure UI thread for TaskbarList
                .Sample(TimeSpan.FromMilliseconds(500)) // Sample to reduce frequency of taskbar updates
                .Subscribe(pos =>
                {
                    if (!IsPlaying || CurrentTrackDurationSeconds <= 1) // Only update if playing and duration is valid
                    {
                        // Optionally set to indeterminate or clear progress when not playing
                        // TaskbarList.SetProgressState(PlatUtils.DimmerHandle, TBPFLAG.TBPF_NOPROGRESS);
                        return;
                    }
                    // Use properties from BaseViewModel that are already being updated
                    double percentage = CurrentTrackPositionPercentage; // This is already 0.0 to 1.0
                    ulong progressValue = (ulong)(percentage * 100);

                    // Ensure handle is valid before calling (PlatUtils.DimmerHandle needs to be set)
                    // if (PlatUtils.DimmerHandle != IntPtr.Zero)
                    // {
                    //     TaskbarList.SetProgressState(PlatUtils.DimmerHandle, TBPFLAG.TBPF_NORMAL);
                    //     TaskbarList.SetProgressValue(PlatUtils.DimmerHandle, progressValue, 100);
                    // }
                }, ex => _logger.LogError(ex, "Error in WinUI Taskbar Position subscription."))
        );
    }
    private void SubscribeToLyricIndexChangesForWinUI()
    {
        // BaseViewModel already subscribes to _stateService.CurrentLyric and updates ActiveCurrentLyricPhrase.
        // This subscription is specifically for the WinUI CollectionView scrolling.

        _subsManager.Add(
            Observable.FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs>(
                handler => this.PropertyChanged += handler, // Subscribe to PropertyChanged
                handler => this.PropertyChanged -= handler)
            .Where(evt => evt.EventArgs.PropertyName == nameof(this.ActiveCurrentLyricPhrase)) // Filter for the specific property
            .Select(_ => this.ActiveCurrentLyricPhrase) // Get the new value of the property
            .Where(activePhrase => activePhrase != null && SongLyricsCV != null)
            .ObserveOn(SynchronizationContext.Current!) // Ensure UI thread for UI operations
            .Subscribe(activePhrase =>
            {
                if (activePhrase == null || SongLyricsCV == null)
                    return; // Double check after ObserveOn
                _logger.LogTrace("BaseViewModelWin: Scrolling lyrics to: {LyricText}", activePhrase.Text);
                SongLyricsCV.ScrollTo(activePhrase, null, ScrollToPosition.Center, true);
            }, ex => _logger.LogError(ex, "Error in WinUI Lyric Scroll subscription."))
        );

        // ALSO, handle the initial value if ActiveCurrentLyricPhrase is already set
        // when this subscription is made, as FromEventPattern only fires on *changes*.
        if (this.ActiveCurrentLyricPhrase != null && SongLyricsCV != null)
        {
            // Ensure this is also on UI thread if called during initialization potentially off UI thread
            if (SynchronizationContext.Current != null)
            {
                SynchronizationContext.Current.Post(_ =>
                {
                    if (this.ActiveCurrentLyricPhrase != null && SongLyricsCV != null)
                    { // Re-check after post
                        SongLyricsCV.ScrollTo(this.ActiveCurrentLyricPhrase, null, ScrollToPosition.Center, true);
                    }
                }, null);
            }
            else
            { // Fallback if no sync context (e.g. test environment or very early init)
                SongLyricsCV.ScrollTo(this.ActiveCurrentLyricPhrase, null, ScrollToPosition.Center, true);
            }
        }
    }
    // ResetDisplayedMasterList is removed because DisplayedSongs should now be populated
    // by subscribing to _stateService.AllCurrentSongs in BaseViewModel.InitializeViewModelSubscriptions.

    [RelayCommand]
    public async Task ShareProfileQrCodeAsync(BarcodeGeneratorView? bCodeView) // Made parameter nullable
    {
        if (bCodeView == null)
        {
            _logger.LogWarning("ShareProfileQrCodeAsync: BarcodeGeneratorView is null.");
            return;
        }
        // UserOnline should be obtained from IDimmerLiveStateService or a user service
        // string? userIdToShare = DimmerLiveStateService.CurrentUserOnline?.ObjectId; // Example
        string? userIdToShare = _stateService.CurrentUser.FirstAsync().Wait()?.Id.ToString(); // Get from global state

        if (string.IsNullOrEmpty(userIdToShare))
        {
            _logger.LogWarning("ShareProfileQrCodeAsync: No user ID to share.");
            // Optionally show a message to the user to log in.
            return;
        }

        // Construct payload for QR code
        // var qrDataPayload = new { eventType = "AddUser", userId = userIdToShare, senderName = "YourAppName" };
        // string jsonPayload = System.Text.Json.JsonSerializer.Serialize(qrDataPayload);
        // bCodeView.Value = jsonPayload; // Set the value for the barcode generator

        _logger.LogInformation("ShareProfileQrCodeAsync: Preparing QR code for user ID: {UserId}", userIdToShare);

    }


    [ObservableProperty]
    private bool _isSettingWindoOpened; // Backing field

    public Task LoadOnlineDataAsync() // Renamed to be async
    {
        // This method should fetch data using IDimmerLiveStateService
        // For example, to update BarCodeInvitationValue:
        var currentUserFromState = _stateService.CurrentUser.FirstAsync().Wait(); // Blocking, consider alternatives
        if (currentUserFromState != null)
        {
            // Construct your QR payload here
            // var qrData = new { type = "userProfile", id = currentUserFromState.Id };
            // BarCodeInvitationValue = System.Text.Json.JsonSerializer.Serialize(qrData);
            BarCodeInvitationValue = currentUserFromState.Id.ToString(); // Simple ID for now
        }
        else
        {
            BarCodeInvitationValue = "NoUserLoggedIn";
        }
        _logger.LogInformation("LoadOnlineDataAsync executed. Barcode value set.");
        return Task.CompletedTask;
    }

    [RelayCommand]
    public async Task AddNewUserViaQrAsync() // Renamed to be async
    {
        _logger.LogInformation("AddNewUserViaQrAsync: Initiating QR scan.");
        try
        {
            var fileResult = await _filePicker.PickAsync(new PickOptions
            {
                PickerTitle = "Select a Profile QR Image",
                FileTypes = FilePickerFileType.Images
            });

            if (fileResult == null)
            {
                _logger.LogInformation("AddNewUserViaQrAsync: No file selected.");
                return;
            }

            using var stream = await fileResult.OpenReadAsync();
            // BarcodeReader requires a writable stream for some internal operations or specific image formats.
            // Copy to MemoryStream if OpenReadAsync gives a non-seekable/writable stream.
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            memoryStream.Position = 0; // Reset for reading

            using var skStream = new SKManagedStream(memoryStream);
            using var bitmap = SKBitmap.Decode(skStream);

            if (bitmap == null)
            {
                _logger.LogError("AddNewUserViaQrAsync: Could not decode image to SKBitmap.");
                return;
            }

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AddNewUserViaQrAsync: Error during QR code processing.");
        }
    }

    [RelayCommand] // Added RelayCommand if this is meant to be a command
    public void ShareSongOnline() // This name implies online sharing, but opens a local window.
    {
        _logger.LogInformation("ShareSongOnline: Opening DimmerSongWindow (local UI action).");
        // This logic is for opening a new MAUI window, specific to MAUI.
        // If it's truly "online sharing", it would interact with IDimmerLiveStateService.
        // For now, assuming it's a local UI window.

        // DimmerSongWindow newWin = new DimmerSongWindow(this); // Assuming this window takes BaseViewModelWin

        // MainThread.BeginInvokeOnMainThread(() =>
        // {
        //    if (Application.Current != null)
        //    {
        //        Application.Current.OpenWindow(newWin);
        //    }
        // });
        _logger.LogWarning("ShareSongOnline: Actual window opening logic needs to be implemented using MAUI's Window management.");
    }

    // Dispose method is inherited from BaseViewModel, no need to redefine unless
    // BaseViewModelWin has its own IDisposable resources not managed by _subsManager.
    // If you add new subscriptions in BaseViewModelWin directly to _subscriptions (from BaseAppFlow),
    // they will be disposed by BaseAppFlow's Dispose. If _subsManager is used, BaseViewModel.Dispose handles it.
}