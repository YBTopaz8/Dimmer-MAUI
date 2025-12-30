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
    SongModelView? CurrentTrackMetadata { get; }
    IEnumerable<AudioOutputDevice> PlaybackDevices { get; }

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

    /// <summary>
    /// Event raised when frequency data is available for visualization.
    /// The byte array contains FFT data values (typically 0-255).
    /// </summary>
    event EventHandler<byte[]?>? FrequencyDataAvailable;

    /// <summary>
    /// Gets or sets whether frequency visualization is enabled.
    /// </summary>
    bool IsFrequencyVisualizationEnabled { get; set; }



    ValueTask DisposeAsync();
    void Stop();
    List<AudioOutputDevice>? GetAllAudioDevices();
    bool SetPreferredOutputDevice(AudioOutputDevice dev);
    void Play(double pos);
    /// <summary>
    /// Initializes the player with the specified track metadata. Stops any current playback.
    /// </summary>
    /// <param name="metadata">The metadata of the track to load.</param>
    /// <returns>Task indicating completion.</returns>
    Task InitializeAsync(SongModelView songModel, double pos);
    Task SetDefaultAsync(AudioOutputDevice device);
    Task MuteDevice(bool mute);
    Task SetVolume(double volume);
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

    /// <summary>
    /// Gets or sets the volume of the ambience track (0.0 to 1.0), independent of main volume.
    /// </summary>
    double AmbienceVolume { get; set; }
    IObservable<SongModelView?> CurrentSong { get; }
}
