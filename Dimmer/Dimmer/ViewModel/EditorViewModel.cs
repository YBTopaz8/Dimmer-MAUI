using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Dimmer.Interfaces;
using Dimmer.Interfaces.Services.Interfaces.FileProcessing.FileProcessorUtils;

namespace Dimmer.ViewModel;

public partial class EditorViewModel : BaseViewModel
{
    private readonly IDimmerAudioEditorService _editorService;

    public EditorViewModel(IDimmerAudioEditorService editorService,IDimmerStateService dimmerStateService, MusicDataService musicDataService, IAppInitializerService appInitializerService, IDimmerAudioService audioServ, ISettingsService settingsService, ILyricsMetadataService lyricsMetadataService, SubscriptionManager subsManager, LyricsMgtFlow lyricsMgtFlow, ICoverArtService coverArtService, IFolderMgtService folderMgtService, IRepository<SongModel> _songRepo, IDuplicateFinderService duplicateFinderService, ILastfmService _lastfmService, IRepository<ArtistModel> artistRepo, IRepository<AlbumModel> albumModel, IRepository<GenreModel> genreModel, IDialogueService dialogueService, IRepository<PlaylistModel> PlaylistRepo, IRealmFactory RealmFact, IFolderMonitorService FolderServ, ILibraryScannerService LibScannerService, IRepository<DimmerPlayEvent> DimmerPlayEventRepo, BaseAppFlow BaseAppClass, ILogger<BaseViewModel> logger) : base(dimmerStateService, musicDataService, appInitializerService, audioServ, settingsService, lyricsMetadataService, subsManager, lyricsMgtFlow, coverArtService, folderMgtService, _songRepo, duplicateFinderService, _lastfmService, artistRepo, albumModel, genreModel, dialogueService, PlaylistRepo, RealmFact, FolderServ, LibScannerService, DimmerPlayEventRepo, BaseAppClass, logger)
    { 
        _editorService = editorService;
        InitializeAsync();
    }
    string oneHourOutputPath = string.Empty;
    [RelayCommand]
    public async Task MakeOneHourVersion()
    {
        oneHourOutputPath = string.Empty;
        IsBusy = true; 
        CanViewOneHourFolder = false;
        string input = CurrentPlayingSongView.FilePath;
        oneHourOutputPath = Path.Combine(FileSystem.CacheDirectory, $"{CurrentPlayingSongView.Title}_1Hour.mp3");

        bool success = await _editorService.CreateOneHourLoopAsync(input, oneHourOutputPath);

        if (success) {
        var successMessage = $"1-Hour version created at: {oneHourOutputPath}";
            StatusMessage = successMessage;
            CanViewOneHourFolder = true;
        }
        IsBusy = false;
    }
    [RelayCommand]
    public async Task OpenAndSelectedOneHourLoopedFile()
    {
        IsBusy = true;
        if (!string.IsNullOrWhiteSpace(oneHourOutputPath) && System.IO.File.Exists(oneHourOutputPath))
        {
            string argument = "/select, \"" + oneHourOutputPath + "\"";
            System.Diagnostics.Process.Start("explorer.exe", argument);
        }

        IsBusy = false;
    }

    

    [ObservableProperty]
    public partial bool IsBusy { get; set; }
    

    [ObservableProperty]
    public partial bool IsReverbEnabled { get; set; }
    

    [ObservableProperty]
    public partial bool CanViewOneHourFolder { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "Ready";

    [ObservableProperty]
    public partial double PlaybackSpeed { get; set; } = 1.0;

    [ObservableProperty]
    public partial double ProgressValue {get;set;}

    [ObservableProperty]
    public partial double CutEndTime { get;set;}
    public string CutRangeText => $"{TimeSpan.FromSeconds(CutStartTime):mm\\:ss} - {TimeSpan.FromSeconds(CutEndTime):mm\\:ss}";
    [ObservableProperty]
    public partial double CutStartTime { get;set;}

    [ObservableProperty]
    public partial SongModelView SelectedSong {get;set;} // The song being edited

    [ObservableProperty]
    public partial string SourceFilePath {get;set;}

    // --- Trimming Properties ---
    [ObservableProperty]
    public partial double StartTime { get; set; } 

    [ObservableProperty]
    public partial double EndTime { get; set; } 

    [ObservableProperty]
    public partial double TotalDuration { get; set; } 

    // Formatted strings for UI
    public string DurationText => $"{StartTime:mm\\:ss} / {EndTime:mm\\:ss}";

    private async void InitializeAsync()
    {
        IsBusy = true;
        StatusMessage = "Checking FFmpeg dependencies...";
        bool loaded = await _editorService.EnsureFFmpegLoadedAsync();
        StatusMessage = loaded ? "Ready to Edit" : "FFmpeg failed to load.";
        IsBusy = false;
    }

    public void LoadSong(SongModelView song)
    {
        if (song == null || string.IsNullOrEmpty(song.FilePath)) return;

        SelectedSong = song;
        SourceFilePath = song.FilePath;

        // Setup default trim values (e.g., Full Length)
        TotalDuration = song.DurationInSeconds;
        EndTime = TotalDuration;

        StatusMessage = $"Loaded: {song.Title}";
    }
    IProgress<double> progress;
    [RelayCommand]
    private async Task TrimAudio()
    {
        if (string.IsNullOrEmpty(SourceFilePath)) return;
        if (EndTime <= StartTime)
        {
            StatusMessage = "Error: End time must be after Start time.";
            return;
        }
        var outputPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "DimmerTrimOutput");
        progress = new Progress<double>(val => ProgressValue = val / 100.0);
        await RunEditorTask("Trimming Audio...", async (progress) =>
        {
            return await _editorService.TrimAudioAsync(SourceFilePath, TimeSpan.FromSeconds(StartTime), TimeSpan.FromSeconds(EndTime), progress);
        });
    }

    [RelayCommand]
    private async Task CreateOneHourLoop()
    {
        if (string.IsNullOrEmpty(SourceFilePath)) return;

        await RunEditorTask("Generating 1-Hour Loop...", async (progress) =>
        {
            // Loop whatever is currently selected in the trim range (or full song)
            // But usually 1 hour loop implies full song. 
            // Let's loop the WHOLE song for simplicity of the feature.
            return await _editorService.CreateInfiniteLoopAsync(SourceFilePath, TimeSpan.FromHours(1), progress);
        });
    }

    private async Task RunEditorTask(string busyMsg, Func<IProgress<double>, Task<string>> action)
    {
        IsBusy = true;
        StatusMessage = busyMsg;
        ProgressValue = 0;

        var progressReporter = new Progress<double>(val => ProgressValue = val / 100.0);

        try
        {
            string resultPath = await action(progressReporter);

            if (!string.IsNullOrEmpty(resultPath))
            {
                StatusMessage = $"Success! Saved to: {Path.GetFileName(resultPath)}";
                // Optionally: Add to app database here
            }
            else
            {
                StatusMessage = "Operation Failed.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            ProgressValue = 0; // Reset or keep full to show completion
        }
    }


    [RelayCommand]
    public async Task CreateSlowedReverb()
    {
        // Preset for Lofi
        var options = new AudioEffectOptions
        {
            Speed = 0.85, // 15% slower
            EnableReverb = true,
            VolumeGain = 1.0
        };

        await RunEditorTask("Applying Slowed + Reverb...", async (p) =>
        {
            return await _editorService.ApplyAudioEffectsAsync(SourceFilePath, options, p);
        });
    }

    [RelayCommand]
    public async Task CreateNightcore()
    {
        // Preset for Nightcore
        var options = new AudioEffectOptions
        {
            Speed = 1.25, // 25% faster
            EnableReverb = false,
            VolumeGain = 1.0
        };

        await RunEditorTask("Applying Nightcore...", async (p) =>
        {
            return await _editorService.ApplyAudioEffectsAsync(SourceFilePath, options, p);
        });
    }


    [RelayCommand]
    public async Task RemoveSection()
    {
        if (string.IsNullOrEmpty(SourceFilePath)) return;

        // Validation
        if (CutEndTime <= CutStartTime)
        {
            StatusMessage = "Error: Cut End must be after Start.";
            return;
        }

        await RunEditorTask("Removing Section...", async (p) =>
        {
            return await _editorService.RemoveSectionAsync(
                SourceFilePath,
                TimeSpan.FromSeconds(CutStartTime),
                TimeSpan.FromSeconds(CutEndTime),
                p);
        });
    }

    [RelayCommand]
    public async Task Create8DAudio()
    {
        if (string.IsNullOrEmpty(SourceFilePath)) return;

        await RunEditorTask("Applying 8D Spatial Audio...", async (p) =>
        {
            return await _editorService.Apply8DAudioAsync(SourceFilePath, p);
        });
    }

}