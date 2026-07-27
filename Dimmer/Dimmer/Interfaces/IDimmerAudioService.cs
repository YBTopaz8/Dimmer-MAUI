namespace Dimmer.Interfaces;

public interface IDimmerAudioService
{
    void Pause();
    void Seek(double positionSeconds);
    void InitializePlaylist(SongModelView songModelView, IEnumerable<SongModelView> songModels);

    bool IsPlaying { get; }
    double CurrentPosition { get; }
    double Duration { get; }
    double Volume { get; set; }

    // --- Granular Real-Time DSP & Effects ---
    double PlaybackSpeed { get; set; }        // Speed/Tempo multiplier
    double PitchShift { get; set; }           // Pitch in semitones (-12.0 to +12.0)
    bool IsReversed { get; set; }             // Real-time reverse audio toggle

    bool EnableReverb { get; set; }
    double ReverbMix { get; set; }            // Wet/Dry mix (0.0 to 1.0)
    float ReverbRoomSize { get; set; }        // Room size (0.0 to 1.0)
    float ReverbDamping { get; set; }         // High-frequency damping (0.0 to 1.0)

    bool EnableLoFi { get; set; }
    double LoFiCutoffFrequency { get; set; }  // Lowpass filter cutoff Hz (200.0 to 10000.0)

    bool EnableSmartMaster { get; set; }      // One-Knob Mastering Suite


    // --- Dynamic Multi-Track (Stem) Management ---
    Task AddStemAsync(string stemId, string filePath, double initialVolume = 1.0);
    void RemoveStem(string stemId);
    void SetStemVolume(string stemId, double volume);
    void ClearAllStems();

    // --- AI / Analysis ---
    /// <summary>
    /// Analyzes a file using OwnaudioNET's ChordDetect pipeline.
    /// Returns: (Musical Key, BPM, Formatted Chords String)
    /// </summary>
    Task<(string Key, int Bpm, string Chords)> AnalyzeTrackChordsAsync(string filePath);

    SongModelView? CurrentTrackMetadata { get; }
    IEnumerable<AudioOutputDevice>? PlaybackDevices { get; }

    event EventHandler<PlaybackEventArgs> IsPlayingChanged;
    event EventHandler<PlaybackEventArgs> PlayEnded;
    event EventHandler<PlaybackEventArgs> MediaKeyPreviousPressed;
    event EventHandler<PlaybackEventArgs> MediaKeyNextPressed;
    event EventHandler<double>? PositionChanged;
    event EventHandler<double>? DurationChanged;
    event EventHandler<double>? SeekCompleted;
    event EventHandler<double>? VolumeChanged;
    event EventHandler<(double newVol, bool isDeviceMuted, int devMavVol)>? DeviceVolumeChanged;
    event EventHandler<PlaybackEventArgs>? PlaybackStateChanged;
    event EventHandler<PlaybackEventArgs>? ErrorOccurred;

    ValueTask DisposeAsync();
    void Stop();
    List<AudioOutputDevice>? GetAllAudioDevices();
    void Play(double pos);

    Task InitializeAsync(SongModelView songModel, double pos);
    Task<bool> SetPreferredOutputDeviceAsync(AudioOutputDevice dev);
    Task MuteDevice(bool mute);
    Task SetVolume(double volume);
    double GetCurrentVolume();
    AudioOutputDevice? GetCurrentAudioOutputDevice();

    Task SendNextSong(SongModelView nextSong);
    Task SetDefaultAsync(AudioOutputDevice device);

    IObservable<SongModelView?> CurrentSong { get; }
    bool IsMuted { get; set; }
}
