namespace Dimmer.DimmerLive.Models;
[ParseClassName("DeviceState")]
public class DeviceState : ParseObject
{

    
    [ParseFieldName("CurrentSong")]
    public ParseObject CurrentSong { get => GetProperty<ParseObject>(); set => SetProperty(value); }
    [ParseFieldName("CurrentSongTitle")]
    public string CurrentSongTitle { get => GetProperty<string>(); set => SetProperty(value); }

    [ParseFieldName("CurrentSongDuration")]
    public double CurrentSongDuration { get => GetProperty<double>(); set => SetProperty(value); }
    
    [ParseFieldName("CurrentSongArtistName")]
    public string CurrentSongArtistName { get => GetProperty<string>(); set => SetProperty(value); }
    [ParseFieldName("CurrentSongAlbumName")]
    public string CurrentSongAlbumName { get => GetProperty<string>(); set => SetProperty(value); }
    [ParseFieldName("CurrentSongFilePath")]
    public string CurrentSongFilePath { get => GetProperty<string>(); set => SetProperty(value); }
    [ParseFieldName("CurrentSongFileSize")]
    public long CurrentSongFileSize { get => GetProperty<long>(); set => SetProperty(value); }
    [ParseFieldName("CurrentSongFileFormat")]
    public string CurrentSongFileFormat { get => GetProperty<string>(); set => SetProperty(value); }
    [ParseFieldName("CurrentSongBitRate")]
    public int CurrentSongBitRate { get => GetProperty<int>(); set => SetProperty(value); }
    [ParseFieldName("CurrentSongReleaseYear")]
    public int? CurrentSongReleaseYear { get => GetProperty<int?>(); set => SetProperty(value); }
    [ParseFieldName("CurrentSongTrackNumber")]
    public int? CurrentSongTrackNumber { get => GetProperty<int?>(); set => SetProperty(value); }
    [ParseFieldName("CurrentSongDiscNumber")]
    public int? CurrentSongDiscNumber { get => GetProperty<int?>(); set => SetProperty(value); }
    [ParseFieldName("CurrentSongDiscTotal")]
    public int? CurrentSongDiscTotal { get => GetProperty<int?>(); set => SetProperty(value); }
    [ParseFieldName("CurrentSongLyricist")]
    public string CurrentSongLyricist { get => GetProperty<string>(); set => SetProperty(value); }
    [ParseFieldName("CurrentSongComposer")]
    public string CurrentSongComposer { get => GetProperty<string>(); set => SetProperty(value); }
    [ParseFieldName("CurrentSongConductor")]
    public string CurrentSongConductor { get => GetProperty<string>(); set => SetProperty(value); }
    /// <summary>
    /// A unique, persistent ID for this specific app instance (e.g., a GUID stored in app settings).
    /// </summary>
    [ParseFieldName("DeviceId")]
    public string DeviceId
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }

    /// <summary>
    /// A user-friendly name for this Device (e.g., "MCS's Desktop", "My Pixel Phone").
    /// </summary>
    [ParseFieldName("DeviceName")]
    public string DeviceName
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }

    /// <summary>
    /// The Current playback state. Stored as a string for simplicity (e.g., "Playing", "Paused", "Stopped").
    /// </summary>
    [ParseFieldName("playbackState")]
    public string PlaybackState
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }

    /// <summary>
    /// The Parse ObjectId of the song that is Currently playing or paused.
    /// </summary>
    [ParseFieldName("CurrentSongId")]
    public string CurrentSongId
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }

    /// <summary>
    /// The playback position of the Current song, in total seconds.
    /// </summary>
    [ParseFieldName("CurrentPosition")]
    public double CurrentPosition
    {
        get => GetProperty<double>();
        set => SetProperty(value);
    }

    /// <summary>
    /// The Current volume of the Device, from 0.0 to 1.0.
    /// </summary>
    [ParseFieldName("Volume")]
    public double Volume
    {
        get => GetProperty<double>();
        set => SetProperty(value);
    }

    /// <summary>
    /// The Current shuffle mode state.
    /// </summary>
    [ParseFieldName("IsShuffleActive")]
    public bool IsShuffleActive
    {
        get => GetProperty<bool>();
        set => SetProperty(value);
    }

    /// <summary>
    /// The Current repeat mode. Stored as a string (e.g., "None", "One", "All").
    /// </summary>
    [ParseFieldName("RepeatMode")]
    public string RepeatMode
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }

    /// <summary>
    /// A timestamp that is updated periodically to indicate the Device is online and active.
    /// </summary>
    [ParseFieldName("LastSeen")]
    public DateTime? LastSeen
    {
        get => GetProperty<DateTime?>();
        set => SetProperty(value);
    }
}


[ParseClassName("DeviceCommand")]
public class DeviceCommand : ParseObject
{
    /// <summary>
    /// The unique 'DeviceId' of the player that should execute this command.
    /// </summary>
    [ParseFieldName("TargetDeviceId")]
    public string TargetDeviceId
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }

    /// <summary>
    /// The unique 'DeviceId' of the remote that sent this command.
    /// </summary>
    [ParseFieldName("SourceDeviceId")]
    public string SourceDeviceId
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }

    /// <summary>
    /// The name of the action to be performed (e.g., "NEXT", "SEEK", "PLAY_SONG").
    /// </summary>
    [ParseFieldName("CommandName")]
    public string CommandName
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }

    /// <summary>
    /// A flexible dictionary to hold any data needed for the command.
    /// For SEEK, it could be { "position": 123.45 }.
    /// For PLAY_SONG, it could be { "songId": "abcdef1234" }.
    /// </summary>
    [ParseFieldName("Payload")]
    public IDictionary<string, object> Payload
    {
        get => GetProperty<IDictionary<string, object>>();
        set => SetProperty(value);
    }

    /// <summary>
    /// A flag that the target Device sets to 'true' after it has successfully
    /// processed the command. This prevents re-execution.
    /// </summary>
    [ParseFieldName("IsHandled")]
    public bool IsHandled
    {
        get => GetProperty<bool>();
        set => SetProperty(value);
    }

    /// <summary>
    /// The UTC timestamp of when the command was created.
    /// </summary>
    [ParseFieldName("Timestamp")]
    public DateTime? Timestamp
    {
        get => GetProperty<DateTime?>();
        set => SetProperty(value);
    }

}