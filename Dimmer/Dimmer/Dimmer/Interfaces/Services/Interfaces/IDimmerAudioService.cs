using Dimmer.Utilities.Events;

namespace Dimmer.Interfaces.Services.Interfaces;
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




    event EventHandler<PlaybackEventArgs> IsPlayingChanged;



    event EventHandler<PlaybackEventArgs> PlayEnded;



    event EventHandler<PlaybackEventArgs> MediaKeyPreviousPressed;



    event EventHandler<PlaybackEventArgs> MediaKeyNextPressed;



    event EventHandler<double>? PositionChanged;



    event EventHandler<double>? DurationChanged;



    event EventHandler<double>? SeekCompleted;



    event EventHandler<PlaybackEventArgs>? PlaybackStateChanged;



    event EventHandler<PlaybackEventArgs>? ErrorOccurred;





    ValueTask DisposeAsync();
    void Stop();
    List<AudioOutputDevice>? GetAllAudioDevices();
    bool SetPreferredOutputDevice(AudioOutputDevice dev);
    void Play(double pos);
    Task InitializeAsync(SongModelView songModel, double pos);
}
