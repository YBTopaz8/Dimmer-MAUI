using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.DimmerLive.Models;


[ParseClassName("DeviceSyncState")]
public class DeviceSyncState : ParseObject
{
    [ParseFieldName("deviceId")]
    public string DeviceId { get => GetProperty<string>(); set => SetProperty(value); }

    [ParseFieldName("deviceName")]
    public string DeviceName { get => GetProperty<string>(); set => SetProperty(value); }

    [ParseFieldName("owner")]
    public ParseUser Owner { get => GetProperty<ParseUser>(); set => SetProperty(value); }

    [ParseFieldName("stateFile")]
    public ParseFile StateFile { get => GetProperty<ParseFile>(); set => SetProperty(value); }
}
