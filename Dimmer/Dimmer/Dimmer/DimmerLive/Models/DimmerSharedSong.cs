using Parse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.DimmerLive.Models;
[ParseClassName("DimmerSharedSong")]
public class DimmerSharedSong : ParseObject
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
    
    [ParseFieldName("sharedPositionInSeconds")]
    public double? SharedPositionInSeconds
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

    [ParseFieldName("unreadCounts")]
    public IDictionary<string, int> UnreadCounts
    {
        get => GetProperty<IDictionary<string, int>>();
        set => SetProperty(value);
    }

    // NEW: For group chat avatar (optional)
    [ParseFieldName("groupAvatar")]
    public ParseFile GroupAvatar
    {
        get => GetProperty<ParseFile>();
        set => SetProperty(value);
    }

    [ParseFieldName("audioMimeType")]
    public string AudioMimeType
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }

    // NEW: External source URL if the song is from a streaming service (optional)
    [ParseFieldName("externalUrl")]
    public string ExternalUrl
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }

    // NEW: Lyrics (if you want to store them with the shared song)
    [ParseFieldName("lyrics")]
    public string LyricsText
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }
}