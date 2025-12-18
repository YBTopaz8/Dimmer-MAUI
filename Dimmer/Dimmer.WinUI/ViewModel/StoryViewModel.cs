using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.WinUI.ViewModel;

public partial class StoryViewModel : ObservableObject
{
    public BaseViewModel BaseVM { get; private set; }

    private readonly IDimmerAudioEditorService _editorService;
    private readonly IDimmerAudioService _audioService; // Your existing playback service

    // The full lyrics list
    public ObservableCollection<LyricPhraseModelView> Lyrics { get; } = new();

    // The user's selection
    [ObservableProperty]
    public partial LyricPhraseModelView StartLine { get; set; }

    [ObservableProperty]
    public partial LyricPhraseModelView EndLine {get;set;}

    [ObservableProperty]
    public partial bool IsBusy {get;set;}

    [ObservableProperty]
    public partial string StatusMessage {get;set;}

    public StoryViewModel(IDimmerAudioEditorService editor, IDimmerAudioService audio,
        BaseViewModel vm)
    {
        BaseVM = vm;
        _editorService = editor;
        _audioService = audio;
    }

    [RelayCommand]
    public async Task PreviewSelection()
    {
        if (StartLine == null || EndLine == null) return;

        double start = StartLine.TimeStampMs / 1000.0;
        double end = EndLine.TimeStampMs / 1000.0 + (EndLine.DurationMs / 1000.0); // Play until end of last line

        // Use your AudioService to play the range
        // You might need to add a method PlayRange(start, end) to your AudioService
        // Or just seek and pause manually later.
        await _audioService.InitializeAsync(BaseVM.SelectedSong, start);

        // Optional: Set a timer to stop playback after (end - start) seconds
    }

    [RelayCommand]
    public async Task ExportStory(string imagePath)
    {
        if (StartLine == null || EndLine == null)
        {
            StatusMessage = "Please select lyrics range.";
            return;
        }

        IsBusy = true;
        StatusMessage = "Processing Audio...";

        // 1. Calculate Times
        TimeSpan start = TimeSpan.FromMilliseconds(StartLine.TimeStampMs);
        // Add duration of the last line to capture it fully
        TimeSpan end = TimeSpan.FromMilliseconds(EndLine.TimeStampMs + EndLine.DurationMs);

        // 2. Trim the Audio first (Temp file)
        var progress = new Progress<double>();
        string tempAudio = await _editorService.TrimAudioAsync(
            BaseVM.SelectedSong.FilePath, start, end, progress);

        StatusMessage = "Rendering Video...";

        // 3. Merge Image + Trimmed Audio
        string videoPath = await _editorService.CreateStoryVideoAsync(
            imagePath, tempAudio, progress);

        // 4. Cleanup Temp Audio
        if (File.Exists(tempAudio)) File.Delete(tempAudio);

        StatusMessage = $"Saved to: {videoPath}";

        // 5. Open Explorer to file
        System.Diagnostics.Process.Start("explorer.exe", $"/select, \"{videoPath}\"");

        IsBusy = false;
    }
}
