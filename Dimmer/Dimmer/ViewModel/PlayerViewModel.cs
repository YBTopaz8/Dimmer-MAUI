using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dimmer.Interfaces;

// Note: Ensure you have a using statement for your SongModelView namespace if it's elsewhere
// using Dimmer.Models; 

namespace Dimmer.ViewModel;

public partial class PlayerViewModel : ObservableObject
{
    private readonly IDimmerAudioService _audioService;

    public PlayerViewModel(IDimmerAudioService audSer)
    {
        _audioService = audSer;

        // 1. Sync UI Progress Bar & Time Strings
        _audioService.PositionChanged += (s, pos) =>
        {
            CurrentPosition = pos;
            FormattedPosition = TimeSpan.FromSeconds(pos).ToString(@"mm\:ss");
        };

        // 2. Sync Song Metadata & dynamically load stems when song changes
        _audioService.CurrentSong.Subscribe(OnCurrentSongChanged);

        // 3. Load user's Ambience library on startup
        LoadAmbienceLibrary();
    }

    // ==========================================
    // 1. STANDARD PLAYBACK CONTROLS
    // ==========================================

    [ObservableProperty]
    public partial double CurrentPosition { get; set; }

    [ObservableProperty]
    public partial string FormattedPosition { get; set; } = "00:00";

    [ObservableProperty]
    public partial double Duration { get; set; }

    [ObservableProperty]
    public partial string FormattedDuration { get; set; } = "00:00";

    [ObservableProperty]
    public partial double MasterVolume { get; set; } = 1.0;
    partial void OnMasterVolumeChanged(double value) => _audioService.Volume = value;

    [ObservableProperty]
    public partial bool IsMuted { get; set; }
    partial void OnIsMutedChanged(bool value) => _audioService.IsMuted = value;

    [RelayCommand]
    private void PlayPause()
    {
        if (_audioService.IsPlaying)
            _audioService.Pause();
        else
            _audioService.Play(_audioService.CurrentPosition);
    }

    [RelayCommand]
    private void Seek(double position)
    {
        _audioService.Seek(position);
    }


    // ==========================================
    // 2. ADVANCED REAL-TIME DSP & EFFECTS
    // ==========================================

    // --- Speed, Pitch, & Reverse ---

    [ObservableProperty]
    public partial double PlaybackSpeed { get; set; } = 1.0;
    partial void OnPlaybackSpeedChanged(double value) => _audioService.PlaybackSpeed = value;

    [ObservableProperty]
    public partial double PitchShift { get; set; } = 0.0;
    partial void OnPitchShiftChanged(double value) => _audioService.PitchShift = value;

    [ObservableProperty]
    public partial bool IsReversed { get; set; }
    partial void OnIsReversedChanged(bool value) => _audioService.IsReversed = value;

    // --- Granular Reverb ---

    [ObservableProperty]
    public partial bool IsReverbEnabled { get; set; }
    partial void OnIsReverbEnabledChanged(bool value) => _audioService.EnableReverb = value;

    [ObservableProperty]
    public partial double ReverbMix { get; set; } = 0.4;
    partial void OnReverbMixChanged(double value) => _audioService.ReverbMix = value;

    [ObservableProperty]
    public partial float ReverbRoomSize { get; set; } = 0.8f;
    partial void OnReverbRoomSizeChanged(float value) => _audioService.ReverbRoomSize = value;

    [ObservableProperty]
    public partial float ReverbDamping { get; set; } = 0.4f;
    partial void OnReverbDampingChanged(float value) => _audioService.ReverbDamping = value;

    // --- Lo-Fi Suite ---

    [ObservableProperty]
    public partial bool IsLoFiEnabled { get; set; }
    partial void OnIsLoFiEnabledChanged(bool value) => _audioService.EnableLoFi = value;

    [ObservableProperty]
    public partial double LoFiCutoffFrequency { get; set; } = 2000.0;
    partial void OnLoFiCutoffFrequencyChanged(double value) => _audioService.LoFiCutoffFrequency = value;

    // --- Studio Mastering ---

    [ObservableProperty]
    public partial bool IsSmartMasterEnabled { get; set; }
    partial void OnIsSmartMasterEnabledChanged(bool value) => _audioService.EnableSmartMaster = value;


    // --- Quick DJ Presets ---

    [RelayCommand]
    private void ApplySlowedReverbPreset()
    {
        IsReversed = false;
        PitchShift = -2.0;       // Pitch down slightly
        PlaybackSpeed = 0.82;    // Slow down

        IsReverbEnabled = true;
        ReverbMix = 0.65;
        ReverbRoomSize = 0.9f;

        IsLoFiEnabled = false;
    }

    [RelayCommand]
    private void ApplyNightcorePreset()
    {
        IsReversed = false;
        PitchShift = 3.0;        // Pitch up
        PlaybackSpeed = 1.25;    // Speed up
        IsReverbEnabled = false;
        IsLoFiEnabled = false;
    }

    [RelayCommand]
    private void ApplyVinylChillPreset()
    {
        IsReversed = false;
        PitchShift = 0.0;
        PlaybackSpeed = 0.93;

        IsLoFiEnabled = true;
        LoFiCutoffFrequency = 1500.0; // Cut off high frequencies heavily

        IsReverbEnabled = true;
        ReverbMix = 0.35;
        ReverbRoomSize = 0.5f;
    }

    [RelayCommand]
    private void ResetAllEffects()
    {
        IsReversed = false;
        PlaybackSpeed = 1.0;
        PitchShift = 0.0;

        IsReverbEnabled = false;
        ReverbMix = 0.4;
        ReverbRoomSize = 0.8f;
        ReverbDamping = 0.4f;

        IsLoFiEnabled = false;
        LoFiCutoffFrequency = 2000.0;

        IsSmartMasterEnabled = false;
    }


    // ==========================================
    // 3. DYNAMIC MULTI-TRACK (STEMS)
    // ==========================================

    public ObservableCollection<StemTrackViewModel> ActiveStems { get; } = new();

    private void OnCurrentSongChanged(SongModelView? song)
    {
        if (song == null) return;

        Duration = song.DurationInSeconds;
        FormattedDuration = TimeSpan.FromSeconds(song.DurationInSeconds).ToString(@"mm\:ss");

        // Clear previous song stems
        _audioService.ClearAllStems();
        ActiveStems.Clear();

        // Dynamically find stems matching the song name in the same directory
        string songDir = Path.GetDirectoryName(song.FilePath) ?? "";
        string songName = Path.GetFileNameWithoutExtension(song.FilePath);

        string[] possibleStemTypes = { "Vocals", "Drums", "Bass", "Other", "Instrumental", "Guitar", "Piano" };

        foreach (var stemType in possibleStemTypes)
        {
            string stemPath = Path.Combine(songDir, $"{songName}_{stemType}.wav");

            // Fallback to MP3 check if WAV doesn't exist
            if (!File.Exists(stemPath))
                stemPath = Path.Combine(songDir, $"{songName}_{stemType}.mp3");

            if (File.Exists(stemPath))
            {
                var stemVm = new StemTrackViewModel
                {
                    StemId = $"{song.FilePath}_{stemType}",
                    Name = stemType,
                    FilePath = stemPath,
                    IsEnabled = true,
                    Volume = 1.0
                };

                // Subscribe to changes from the UI to update the Audio Service instantly
                stemVm.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(StemTrackViewModel.IsEnabled))
                    {
                        if (stemVm.IsEnabled)
                            _audioService.AddStemAsync(stemVm.StemId, stemVm.FilePath, stemVm.IsMuted ? 0 : stemVm.Volume);
                        else
                            _audioService.RemoveStem(stemVm.StemId);
                    }
                    else if (e.PropertyName == nameof(StemTrackViewModel.Volume) || e.PropertyName == nameof(StemTrackViewModel.IsMuted))
                    {
                        _audioService.SetStemVolume(stemVm.StemId, stemVm.IsMuted ? 0 : stemVm.Volume);
                    }
                };

                ActiveStems.Add(stemVm);

                // Automatically add and play it if it's enabled by default
                if (stemVm.IsEnabled)
                    _audioService.AddStemAsync(stemVm.StemId, stemVm.FilePath, stemVm.Volume);
            }
        }
    }


    // ==========================================
    // 4. DYNAMIC AMBIENCE LIBRARY
    // ==========================================

    public ObservableCollection<AmbienceSoundViewModel> AmbienceLibrary { get; } = new();

    private void LoadAmbienceLibrary()
    {
        // Load ambience files dynamically from user's Dimmer/Ambience folder
        string ambienceDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Dimmer", "Ambience");

        if (!Directory.Exists(ambienceDir))
        {
            Directory.CreateDirectory(ambienceDir);
            return; // No files yet
        }

        var files = Directory.GetFiles(ambienceDir, "*.*")
                             .Where(f => f.EndsWith(".mp3") || f.EndsWith(".wav") || f.EndsWith(".ogg") || f.EndsWith(".flac"));

        foreach (var file in files)
        {
            string name = Path.GetFileNameWithoutExtension(file);
            var ambVm = new AmbienceSoundViewModel
            {
                Id = $"Ambience_{name}",
                Name = name,
                FilePath = file,
                IconGlyph = GetIconForAmbience(name),
                IsActive = false,
                Volume = 0.5
            };

            // Bind UI toggles/sliders directly to the Audio Service
            ambVm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(AmbienceSoundViewModel.IsActive))
                {
                    if (ambVm.IsActive)
                        _audioService.AddStemAsync(ambVm.Id, ambVm.FilePath, ambVm.Volume);
                    else
                        _audioService.RemoveStem(ambVm.Id);
                }
                else if (e.PropertyName == nameof(AmbienceSoundViewModel.Volume))
                {
                    if (ambVm.IsActive)
                        _audioService.SetStemVolume(ambVm.Id, ambVm.Volume);
                }
            };

            AmbienceLibrary.Add(ambVm);
        }
    }

    private static string GetIconForAmbience(string name)
    {
        // Return Segoe MDL2 Assets icon glyphs based on filename for beautiful UI
        if (name.Contains("rain", StringComparison.OrdinalIgnoreCase)) return "\uE9C4";
        if (name.Contains("fire", StringComparison.OrdinalIgnoreCase)) return "\uEAA0";
        if (name.Contains("cafe", StringComparison.OrdinalIgnoreCase) || name.Contains("coffee", StringComparison.OrdinalIgnoreCase)) return "\uE734";
        if (name.Contains("wave", StringComparison.OrdinalIgnoreCase) || name.Contains("ocean", StringComparison.OrdinalIgnoreCase) || name.Contains("water", StringComparison.OrdinalIgnoreCase)) return "\uE9B9";
        if (name.Contains("wind", StringComparison.OrdinalIgnoreCase)) return "\uEBE6";
        if (name.Contains("night", StringComparison.OrdinalIgnoreCase) || name.Contains("cricket", StringComparison.OrdinalIgnoreCase)) return "\uE708";

        return "\uE8D6"; // Default Music Note icon
    }


    // ==========================================
    // 5. AI CHORD & KEY DETECTION
    // ==========================================

    [ObservableProperty]
    public partial string DetectedKey { get; set; } = "Unknown Key";

    [ObservableProperty]
    public partial string DetectedBpm { get; set; } = "-- BPM";

    [ObservableProperty]
    public partial string DetectedChords { get; set; } = "Click Analyze to detect chords";

    [ObservableProperty]
    public partial bool IsAnalyzing { get; set; }

    [RelayCommand]
    private async Task AnalyzeTrackAsync()
    {
        if (_audioService.CurrentTrackMetadata == null || string.IsNullOrEmpty(_audioService.CurrentTrackMetadata.FilePath))
            return;

        IsAnalyzing = true;
        DetectedChords = "Analyzing audio via OwnChordDetect AI...";

        // Process in background to keep UI perfectly responsive
        var result = await _audioService.AnalyzeTrackChordsAsync(_audioService.CurrentTrackMetadata.FilePath);

        DetectedKey = $"Key: {result.Key}";
        DetectedBpm = $"{result.Bpm} BPM";
        DetectedChords = string.IsNullOrWhiteSpace(result.Chords) ? "No chords detected." : result.Chords;

        IsAnalyzing = false;
    }
}

// ==========================================
// HELPER VIEWMODELS FOR LIST BINDINGS
// ==========================================

public partial class StemTrackViewModel : ObservableObject
{
    public required string StemId { get; init; }
    public required string Name { get; init; }
    public required string FilePath { get; init; }

    [ObservableProperty]
    public partial bool IsEnabled { get; set; }

    [ObservableProperty]
    public partial double Volume { get; set; }

    [ObservableProperty]
    public partial bool IsMuted { get; set; }
}

public partial class AmbienceSoundViewModel : ObservableObject
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string FilePath { get; init; }
    public required string IconGlyph { get; init; }

    [ObservableProperty]
    public partial bool IsActive { get; set; }

    [ObservableProperty]
    public partial double Volume { get; set; }
}