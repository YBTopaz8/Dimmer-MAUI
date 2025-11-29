using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Dimmer.Data.Models.LyricsModels;


public class LrcLibLyrics
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("trackName")]
    public string TrackName { get; set; } = string.Empty;

    [JsonPropertyName("artistName")]
    public string ArtistName { get; set; } = string.Empty;

    [JsonPropertyName("albumName")]
    public string AlbumName { get; set; } = string.Empty;

    [JsonPropertyName("duration")]
    public double Duration { get; set; } // <--- CRITICAL FIX: int -> double

    [JsonPropertyName("instrumental")]
    public bool Instrumental { get; set; }

    [JsonPropertyName("plainLyrics")]
    public string? PlainLyrics { get; set; }

    [JsonPropertyName("syncedLyrics")]
    public string? SyncedLyrics { get; set; }
}

// Represents the body for a POST request to /publish
public class LrcLibPublishRequest
{
    // These should also use JsonPropertyName for consistency
    [JsonPropertyName("trackName")]
    public string TrackName { get; set; } = string.Empty;

    [JsonPropertyName("artistName")]
    public string ArtistName { get; set; } = string.Empty;

    [JsonPropertyName("albumName")]
    public string AlbumName { get; set; } = string.Empty;

    [JsonPropertyName("duration")]
    public int Duration { get; set; } // For publishing, int is fine as we control it

    [JsonPropertyName("plainLyrics")]
    public string PlainLyrics { get; set; } = string.Empty;

    [JsonPropertyName("syncedLyrics")]
    public string SyncedLyrics { get; set; } = string.Empty;
}

// Represents the response from /request-challenge
public class LrcLibChallengeResponse
{
    [JsonPropertyName("prefix")]
    public string Prefix { get; set; } = string.Empty;

    [JsonPropertyName("target")]
    public string Target { get; set; } = string.Empty;
}
