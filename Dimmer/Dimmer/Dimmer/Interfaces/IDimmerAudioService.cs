using Dimmer.Utilities.Events;

namespace Dimmer.Interfaces;
public interface IDimmerAudioService
{

        public static IDimmerAudioService Current;

        void Initialize(SongModelView? media = null, byte[]? ImageBytes = null);


        ///<Summary>
        /// Pauses the currently initialized song.
        ///</Summary>
        void Play(bool IsFromPreviousOrNext = false);

        ///<Summary>
        /// Pauses the currently initialized song.
        ///</Summary>  
        void Pause();

        ///<Summary>
        /// Resumes the currently initialized song.
        ///</Summary>   
        void Resume(double positionInSeconds);

        ///<Summary>
        /// Set the current playback position (in seconds).
        ///</Summary>    
        void SetCurrentTime(double value);

        void SetCurrentMedia(SongModelView media);


    ///<Summary>
    /// Gets a value indicating whether the currently loaded audio file is playing.
    ///</Summary>
    bool IsPlaying { get; }

        ///<Summary>
        /// Gets the current position of audio playback in seconds.
        ///</Summary>
        double CurrentPosition { get; }

        ///<Summary>
        /// Gets the length of audio in seconds.
        ///</Summary>
        double Duration { get; }

        ///<Summary>
        /// Gets or sets the playback volume 0 to 1 where 0 is no-sound and 1 is full volume.
        ///</Summary>
        double Volume { get; set; }

        ///<Summary>
        /// Gets or sets the balance left/right: -1 is 100% left : 0% right, 1 is 100% right : 0% left, 0 is equal volume left/right.
        ///</Summary>

        //double Balance { get; set; }

        event EventHandler<PlaybackEventArgs> IsPlayingChanged;
        event EventHandler<PlaybackEventArgs> PlayEnded;
        event EventHandler PlayPrevious;
        event EventHandler PlayNext;

        event EventHandler<long> IsSeekedFromNotificationBar;
    event EventHandler? PlayStopAndShowWindow;
}

