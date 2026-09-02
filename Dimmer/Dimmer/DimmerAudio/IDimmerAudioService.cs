using OwnaudioNET.Effects;
using OwnaudioNET.Effects.SmartMaster;

namespace Dimmer.Interfaces;
public  interface IDimmerAudioService
{


    Task PauseAsync();


    Task SeekAsync(double positionSeconds);


    void InitializePlaylist(SongModelView songModelView, IEnumerable<SongModelView> songModels);

    DimmerPlaybackState CurrentPlaybackState { get; set; }
    bool IsPlaying { get; }

    double CurrentPosition { get; }

    double Duration { get; }

    double Volume { get; set; }
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
    Task<List<AudioOutputDevice>?> GetAllAudioDevicesAsync();
    bool SetPreferredOutputDevice(AudioOutputDevice dev);
    Task PlayAsync(double pos);
    /// <summary>
    /// Initializes the player with the specified track metadata. Stops any current playback.
    /// </summary>
    /// <param name="metadata">The metadata of the track to load.</param>
    /// <returns>Task indicating completion.</returns>
    Task InitializeAsync(SongModelView songModel, double pos);
    Task SetDefaultAsync(AudioOutputDevice device);
    void MuteDevice(bool mute);
    void SetVolume(double volume);
    double GetCurrentVolume();
    AudioOutputDevice? GetCurrentAudioOutputDevice();

    // --- AMBIENCE / BACKGROUND AUDIO ---
    /// <summary>
    /// Loads a background audio file (rain, wind, etc.) and prepares it for looping.
    /// </summary>
    Task InitializeAmbienceAsync(string filePath);

    /// <summary>
    /// Toggles whether the ambience track should play when the main music plays.
    /// </summary>
    void ToggleAmbience(bool isEnabled);
    Task SendNextSong(SongModelView nextSong);

    Task InitializeEngineAsync(string? outputDeviceId = null);
    void SetPitchAndSpeed(float pitchSemitones, float tempoRatio);
    void EnableEqualizer(bool enable);
    void SetEqualizerPreset(EqualizerPreset preset);
    void SetEqualizerBand(int bandIndex, float gainDb);
    void EnableReverb(bool enable, float roomSize = 0.6F, float mix = 0.25F);
    void EnableCompressor(bool enable, CompressorPreset preset = CompressorPreset.VocalGentle);
    void EnableSmartMaster(bool enable, SpeakerType targetSpeaker = SpeakerType.HiFi);
    void ChangeEqBand(int bandIndex, float gainDb);
    void SetDjCrossfade(double balance = 0.5);

    /// <summary>
    /// Gets or sets the volume of the ambience track (0.0 to 1.0), independent of main volume.
    /// </summary>
    double AmbienceVolume { get; set; }
    IObservable<SongModelView?> CurrentSong { get; }
    IObservable<float[]> EqBands { get; }
}
