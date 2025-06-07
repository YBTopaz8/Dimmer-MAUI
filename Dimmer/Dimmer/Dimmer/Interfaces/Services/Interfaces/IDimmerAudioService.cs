using Dimmer.Utilities.Events;

namespace Dimmer.Interfaces.Services.Interfaces;
public interface IDimmerAudioService
{

    void Play();

    void Pause();













    Task SeekAsync(double positionSeconds);






    Task InitializeAsync(SongModelView songModel, byte[]? SongCoverImage = null);




    void InitializePlaylist(IEnumerable<SongModelView> songModels);




    Task<List<AudioOutputDevice>> GetAvailableAudioOutputsAsync();










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
    event EventHandler<PlaybackEventArgs> PlayStarted;





    ValueTask DisposeAsync();
    void Stop();
    List<AudioOutputDevice>? GetAllAudioDevices();
    bool SetPreferredOutputDevice(AudioOutputDevice dev);
}

public class AudioOutputDevice
{






    public string? Id { get; set; }






    public string? Name { get; set; }
}