using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.DimmerLive.Models;

[ParseClassName("CloudScrobble")]
public class CloudScrobble : ParseObject
{
    [ParseFieldName("songTitleDurationKey")]
    public string SongTitleDurationKey { get => GetProperty<string>(); set => SetProperty(value); }

    [ParseFieldName("deviceId")]
    public string DeviceId { get => GetProperty<string>(); set => SetProperty(value); }

    [ParseFieldName("owner")]
    public ParseUser Owner { get => GetProperty<ParseUser>(); set => SetProperty(value); }
}