using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Playback;

namespace Dimmer.Utilities.Events;
// Represents an error that occurred during media playback
public class MediaItemFailureEventArgs : EventArgs
{
    public SongModelView? FailedSong { get; }
    public MediaPlaybackItemErrorCode ErrorCode { get; }
    public Exception? ExtendedError { get; }

    public MediaItemFailureEventArgs(SongModelView? song, MediaPlaybackItemErrorCode code, Exception? extendedError)
    {        
        FailedSong = song;
        ErrorCode = code;
        ExtendedError = extendedError;
    }
}
