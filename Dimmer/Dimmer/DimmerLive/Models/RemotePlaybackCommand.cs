using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.DimmerLive.Models;


[ParseClassName("RemotePlaybackCommand")]
public class RemotePlaybackCommand : ParseObject
{
    [ParseFieldName("targetDeviceId")]
    public string TargetDeviceId { get => GetProperty<string>(); set => SetProperty(value); } // "ALL" or specific ID

    [ParseFieldName("senderDeviceId")]
    public string SenderDeviceId { get => GetProperty<string>(); set => SetProperty(value); }

    [ParseFieldName("commandType")]
    public string CommandType { get => GetProperty<string>(); set => SetProperty(value); } // "PLAY", "PAUSE", "SEEK", "PLAY_SONG"

    [ParseFieldName("payload")]
    public string Payload { get => GetProperty<string>(); set => SetProperty(value); } // e.g., "songId", "15000" (ms)

    [ParseFieldName("owner")]
    public ParseUser Owner { get => GetProperty<ParseUser>(); set => SetProperty(value); }
}

