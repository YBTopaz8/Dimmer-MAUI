using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Models/QrCodePayload.cs
using System.Text.Json.Serialization; // For System.Text.Json attributes

namespace Dimmer.DimmerLive.Models;
public class QrCodeData
{
    [JsonPropertyName("eventType")]
    public string EventType { get; set; }

    [JsonPropertyName("eventId")]
    public string EventId { get; set; }

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } // Store as ISO 8601 string

    [JsonPropertyName("payload")]
    public Dictionary<string, object>? Payload { get; set; } // Flexible payload

    [JsonPropertyName("senderId")]
    public string? SenderId { get; set; }

    [JsonPropertyName("senderName")]
    public string? SenderName { get; set; }
}

// Define constants for event types for better code management
public static class QrEventTypes
{
    public const string AddUser = "ADD_USER";
    public const string ShareSong = "SHARE_SONG";
    public const string JoinConversation = "JOIN_CONVERSATION";
    public const string ShareAndAddUser = "SHARE_AND_ADD_USER"; // Example combined
    // Add more as needed
}