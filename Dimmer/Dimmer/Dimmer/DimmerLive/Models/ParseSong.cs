using Parse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.DimmerLive.Models;
[ParseClassName("Song")]
public class ParseSong : ParseObject
{
    [ParseFieldName("title")]
    public string Title
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }

    [ParseFieldName("artist")]
    public string Artist
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }

    [ParseFieldName("album")]
    public string Album
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }

    [ParseFieldName("durationSeconds")]
    public double? DurationSeconds
    {
        get => GetProperty<double?>();
        set => SetProperty(value);
    }

    [ParseFieldName("audioFile")]
    public ParseFile AudioFile
    {
        get => GetProperty<ParseFile>();
        set => SetProperty(value);
    }

    [ParseFieldName("coverArtFile")]
    public ParseFile CoverArtFile
    {
        get => GetProperty<ParseFile>();
        set => SetProperty(value);
    }

    [ParseFieldName("uploader")]
    public ParseUser Uploader
    {
        get => GetProperty<ParseUser>();
        set => SetProperty(value);
    }
}