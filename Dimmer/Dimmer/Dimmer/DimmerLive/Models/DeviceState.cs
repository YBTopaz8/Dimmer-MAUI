using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.DimmerLive.Models;
[ParseClassName("DeviceState")]
public class DeviceState : ParseObject
{

    
    [ParseFieldName("currentSong")]
    public ParseObject CurrentSong { get => GetProperty<ParseObject>(); set => SetProperty(value); }
    [ParseFieldName("currentSongTitle")]
    public string CurrentSongTitle { get => GetProperty<string>(); set => SetProperty(value); }

    [ParseFieldName("currentSongDuration")]
    public double CurrentSongDuration { get => GetProperty<double>(); set => SetProperty(value); }
    
    [ParseFieldName("currentSongArtistName")]
    public string CurrentSongArtistName { get => GetProperty<string>(); set => SetProperty(value); }
    [ParseFieldName("currentSongAlbumName")]
    public string CurrentSongAlbumName { get => GetProperty<string>(); set => SetProperty(value); }
    [ParseFieldName("currentSongFilePath")]
    public string CurrentSongFilePath { get => GetProperty<string>(); set => SetProperty(value); }
    [ParseFieldName("currentSongFileSize")]
    public long CurrentSongFileSize { get => GetProperty<long>(); set => SetProperty(value); }
    [ParseFieldName("currentSongFileFormat")]
    public string CurrentSongFileFormat { get => GetProperty<string>(); set => SetProperty(value); }
    [ParseFieldName("currentSongBitRate")]
    public int CurrentSongBitRate { get => GetProperty<int>(); set => SetProperty(value); }
    [ParseFieldName("currentSongReleaseYear")]
    public int? CurrentSongReleaseYear { get => GetProperty<int?>(); set => SetProperty(value); }
    [ParseFieldName("currentSongTrackNumber")]
    public int? CurrentSongTrackNumber { get => GetProperty<int?>(); set => SetProperty(value); }
    [ParseFieldName("currentSongDiscNumber")]
    public int? CurrentSongDiscNumber { get => GetProperty<int?>(); set => SetProperty(value); }
    [ParseFieldName("currentSongDiscTotal")]
    public int? CurrentSongDiscTotal { get => GetProperty<int?>(); set => SetProperty(value); }
    [ParseFieldName("currentSongLyricist")]
    public string CurrentSongLyricist { get => GetProperty<string>(); set => SetProperty(value); }
    [ParseFieldName("currentSongComposer")]
    public string CurrentSongComposer { get => GetProperty<string>(); set => SetProperty(value); }
    [ParseFieldName("currentSongConductor")]
    public string CurrentSongConductor { get => GetProperty<string>(); set => SetProperty(value); }
    /// <summary>
    /// A unique, persistent ID for this specific app instance (e.g., a GUID stored in app settings).
    /// </summary>
    [ParseFieldName("deviceId")]
    public string DeviceId
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }

    /// <summary>
    /// A user-friendly name for this device (e.g., "MCS's Desktop", "My Pixel Phone").
    /// </summary>
    [ParseFieldName("deviceName")]
    public string DeviceName
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }

    /// <summary>
    /// The current playback state. Stored as a string for simplicity (e.g., "Playing", "Paused", "Stopped").
    /// </summary>
    [ParseFieldName("playbackState")]
    public string PlaybackState
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }

    /// <summary>
    /// The Parse ObjectId of the song that is currently playing or paused.
    /// </summary>
    [ParseFieldName("currentSongId")]
    public string CurrentSongId
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }

    /// <summary>
    /// The playback position of the current song, in total seconds.
    /// </summary>
    [ParseFieldName("currentPosition")]
    public double CurrentPosition
    {
        get => GetProperty<double>();
        set => SetProperty(value);
    }

    /// <summary>
    /// The current volume of the device, from 0.0 to 1.0.
    /// </summary>
    [ParseFieldName("volume")]
    public double Volume
    {
        get => GetProperty<double>();
        set => SetProperty(value);
    }

    /// <summary>
    /// The current shuffle mode state.
    /// </summary>
    [ParseFieldName("isShuffleActive")]
    public bool IsShuffleActive
    {
        get => GetProperty<bool>();
        set => SetProperty(value);
    }

    /// <summary>
    /// The current repeat mode. Stored as a string (e.g., "None", "One", "All").
    /// </summary>
    [ParseFieldName("repeatMode")]
    public string RepeatMode
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }

    /// <summary>
    /// A timestamp that is updated periodically to indicate the device is online and active.
    /// </summary>
    [ParseFieldName("lastSeen")]
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
    /// The unique 'deviceId' of the player that should execute this command.
    /// </summary>
    [ParseFieldName("targetDeviceId")]
    public string TargetDeviceId
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }

    /// <summary>
    /// The unique 'deviceId' of the remote that sent this command.
    /// </summary>
    [ParseFieldName("sourceDeviceId")]
    public string SourceDeviceId
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }

    /// <summary>
    /// The name of the action to be performed (e.g., "NEXT", "SEEK", "PLAY_SONG").
    /// </summary>
    [ParseFieldName("commandName")]
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
    [ParseFieldName("payload")]
    public IDictionary<string, object> Payload
    {
        get => GetProperty<IDictionary<string, object>>();
        set => SetProperty(value);
    }

    /// <summary>
    /// A flag that the target device sets to 'true' after it has successfully
    /// processed the command. This prevents re-execution.
    /// </summary>
    [ParseFieldName("isHandled")]
    public bool IsHandled
    {
        get => GetProperty<bool>();
        set => SetProperty(value);
    }

    /// <summary>
    /// The UTC timestamp of when the command was created.
    /// </summary>
    [ParseFieldName("timestamp")]
    public DateTime? Timestamp
    {
        get => GetProperty<DateTime?>();
        set => SetProperty(value);
    }

}