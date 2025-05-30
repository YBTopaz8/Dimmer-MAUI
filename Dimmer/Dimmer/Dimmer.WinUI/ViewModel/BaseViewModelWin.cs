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
using Microsoft.Maui.Platform;

namespace Dimmer.WinUI.ViewModel; // Assuming this is your WinUI ViewModel namespace

public partial class BaseViewModelWin : BaseViewModel // BaseViewModel is in Dimmer.ViewModel
{
    private readonly IMapper mapper;
    private readonly IDimmerLiveStateService dimmerLiveStateService;
    private readonly AlbumsMgtFlow albumsMgtFlow;
    private readonly PlayListMgtFlow playlistsMgtFlow;
    private readonly SongsMgtFlow songsMgtFlow;
    private readonly IDimmerStateService stateService;
    private readonly ISettingsService settingsService;
    private readonly SubscriptionManager subsManager;
    private readonly LyricsMgtFlow lyricsMgtFlow;
    private readonly IFolderMgtService folderMgtService;
    private readonly ILogger<BaseViewModelWin> logger;


    public BaseViewModelWin(IMapper mapper, IDimmerLiveStateService dimmerLiveStateService, AlbumsMgtFlow albumsMgtFlow, PlayListMgtFlow playlistsMgtFlow, SongsMgtFlow songsMgtFlow, IDimmerStateService stateService, ISettingsService settingsService, SubscriptionManager subsManager, LyricsMgtFlow lyricsMgtFlow, IFolderMgtService folderMgtService, ILogger<BaseViewModelWin> logger) : base(mapper, dimmerLiveStateService, albumsMgtFlow, playlistsMgtFlow, songsMgtFlow, stateService, settingsService, subsManager, lyricsMgtFlow, folderMgtService, logger)
    {
        this.mapper=mapper;
        this.dimmerLiveStateService=dimmerLiveStateService;
        this.albumsMgtFlow=albumsMgtFlow;
        this.playlistsMgtFlow=playlistsMgtFlow;
        this.songsMgtFlow=songsMgtFlow;
        this.stateService=stateService;
        this.settingsService=settingsService;
        this.subsManager=subsManager;
        this.lyricsMgtFlow=lyricsMgtFlow;
        this.folderMgtService=folderMgtService;
        this.logger=logger;
    }

    [ObservableProperty]
    public partial int MediaBarGridRowPosition { get; set; }

    [RelayCommand]
    public void SwapMediaBarPosition()
    {
        if (MediaBarGridRowPosition==0)
        {
            MediaBarGridRowPosition = 1;
        }
        else
        {
            MediaBarGridRowPosition=0;
        }
    }
}