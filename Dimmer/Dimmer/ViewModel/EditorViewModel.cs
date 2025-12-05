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

    public async Task MakeOneHourVersionCommand()
    {
        IsBusy = true;
        string input = CurrentPlayingSongView.FilePath;
        string output = Path.Combine(FileSystem.CacheDirectory, $"{CurrentPlayingSongView.Title}_1Hour.mp3");

        bool success = await _editorService.CreateOneHourLoopAsync(input, output);

        if (success) { /* Notify User / Add to Library */ }
        IsBusy = false;
    }
    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "Ready";

    [ObservableProperty]
    public partial double ProgressValue {get;set;}

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
        await RunEditorTask("Trimming Audio...", async (progress) =>
        {
            return await _editorService.TrimAudioAsync(SourceFilePath, outputPath,TimeSpan.FromSeconds(StartTime), TimeSpan.FromSeconds(EndTime));
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
}