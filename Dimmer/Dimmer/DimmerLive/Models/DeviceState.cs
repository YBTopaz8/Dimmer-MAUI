namespace Dimmer.DimmerLive.Models;

[ParseClassName("DeviceState")]
public class DeviceState : ParseObject
{
    [ParseFieldName("deviceId")] public string DeviceId { get => GetProperty<string>(); set => SetProperty(value); }
    [ParseFieldName("deviceName")] public string DeviceName { get => GetProperty<string>(); set => SetProperty(value); }
    [ParseFieldName("currentSongKey")] public string CurrentSongKey { get => GetProperty<string>(); set => SetProperty(value); }
    [ParseFieldName("position")] public double Position { get => GetProperty<double>(); set => SetProperty(value); }
    [ParseFieldName("isPlaying")] public bool IsPlaying { get => GetProperty<bool>(); set => SetProperty(value); }
    [ParseFieldName("lastSeen")] public DateTime LastSeen { get => GetProperty<DateTime>(); set => SetProperty(value); }
    [ParseFieldName("owner")] public ParseUser Owner { get => GetProperty<ParseUser>(); set => SetProperty(value); }
}

