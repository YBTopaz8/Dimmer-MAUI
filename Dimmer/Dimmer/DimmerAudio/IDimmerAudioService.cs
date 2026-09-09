using OwnaudioNET.Effects;
using OwnaudioNET.Effects.SmartMaster;

namespace Dimmer.DimmerAudio;

using OwnaudioNET.Effects;
using OwnaudioNET.Effects.SmartMaster;
using static Dimmer.DimmerAudio.OwnAudioService;

public interface IDimmerAudioService : IAsyncDisposable
{
    // ==========================================================
    // 1. REACTIVE STATE STREAMS (ViewModels subscribe to these)
    // ==========================================================
    IObservable<SongModelView?> CurrentSongObs { get; }
    IObservable<DimmerPlaybackState> PlaybackStateObs { get; }
    IObservable<double> PositionObs { get; }
    IObservable<double> DurationObs { get; }
    IObservable<double> VolumeObs { get; }
    IObservable<AudioOutputDevice?> CurrentDeviceObs { get; }
    IObservable<float[]> EqBandsObs { get; }

    IObservable<SongModelView> PlayEndedObs { get; }
    IObservable<double> SeekCompletedObs { get; }
    IObservable<Exception> ErrorObs { get; }
   

    // ==========================================================
    // 2. HARDWARE / NOTIFICATION TRIGGERS
    // ==========================================================
    IObservable<SongModelView> NextRequestedObs { get; }
    IObservable<SongModelView> PreviousRequestedObs { get; }
    IObservable<SongModelView> FavoriteRequestedObs { get; }

    void TriggerNext();
    void TriggerPrevious();
    void TriggerFavorite();

    // ==========================================================
    // 3. CORE PLAYBACK CONTROLS
    // ==========================================================
    SongModelView? CurrentTrackMetadata { get; }
    bool IsPlaying { get; }
    double CurrentPosition { get; }
    double Duration { get; }

    Task InitializeEngineAsync(string? outputDeviceId = null);
    Task InitializeAsync(SongModelView songModel, double pos);
    Task PlayAsync(double pos = -1);
    Task PauseAsync();
    Task SeekAsync(double positionSeconds);
    void Stop();
    Task SendNextSong(SongModelView nextSong);



    // ==========================================================
    // 4. AUDIO EFFECTS & TWEAKS
    // ==========================================================
    void SetPlaybackMode(PlaybackModeEnum mode, float customReverbMix = 0.15f, float customRoomSize = 0.35f);
    void SetPitchAndSpeed(float pitchSemitones, float tempoRatio);

    void EnableEqualizer(bool enable);
    void SetEqualizerPreset(EqualizerPreset preset);
    void ChangeEqBand(int bandIndex, float gainDb);

    void EnableReverb(bool enable, float roomSize = 0.35f, float mix = 0.15f);
    void EnableCompressor(bool enable, CompressorPreset preset = CompressorPreset.VocalGentle);
    void EnableSmartMaster(bool enable, SpeakerType targetSpeaker = SpeakerType.HiFi);

    void SetDjCrossfade(double balance = 0.5);

    // ==========================================================
    // 5. AMBIENCE (Background noise)
    // ==========================================================
    double AmbienceVolume { get; set; }
    Task InitializeAmbienceAsync(string filePath);
    void ToggleAmbience(bool isEnabled);

    // ==========================================================
    // 6. DEVICE MANAGEMENT
    // ==========================================================
    IEnumerable<AudioOutputDevice>? PlaybackDevices { get; }
    Task<List<AudioOutputDevice>?> GetAllAudioDevicesAsync();
    bool SetPreferredOutputDevice(AudioOutputDevice dev);
    void SetVolume(double volume);
    void MuteDevice(bool mute);
    double Volume { get; set; }
    AudioOutputDevice? GetCurrentAudioOutputDevice();
}