using Hqub.Lastfm.Entities;

using Humanizer;

using static ATL.TagData;
using static Realms.ThreadSafeReference;

namespace Dimmer.DimmerLive.Models;

[ParseClassName("ChatConversation")]
public partial class ChatConversation : ParseObject
{
    // Use Relation for participants for better querying

    [ParseFieldName("participants")]
    public IList<UserModelOnline> participants
    {
        get => Get<IList<UserModelOnline>>(nameof(participants));
        set => Set(nameof(participants), value);
    }

    [ParseFieldName("lastMessage")]
    public ChatMessage LastMessage
    {
        get => GetProperty<ChatMessage>();
        set => SetProperty(value);
    }

    [ParseFieldName("lastMessageTimestamp")]
    public DateTime? LastMessageTimestamp // Use DateTime? (nullable)
    {
        get => GetProperty<DateTime?>();
        set => SetProperty(value);
    }

    [ParseFieldName("name")]
    public string Name
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }

    [ParseFieldName("isGroupChat")]
    public bool IsGroupChat
    {
        get => GetProperty<bool>();
        set => SetProperty(value);
    }
    [ParseFieldName("isDirectMessage")]
    public bool IsDirectMessage 
    {
        get => GetProperty<bool>();
        set => SetProperty(value);
    }
}

/// <summary>
/// Represents a friend request between two users. This object is typically temporary
/// and is deleted after the request is accepted or rejected.
/// </summary>
[ParseClassName("FriendRequest")]
public class FriendRequest : ParseObject
{
        /// <summary>
        
        /// </summary>
        [ParseFieldName("sender")]
        public UserModelOnline Sender
    {
        get => GetProperty<UserModelOnline>();
        set => SetProperty(value);
    }

        /// <summary>
        
        /// </summary>
        [ParseFieldName("recipient")]
        public UserModelOnline Recipient
    {
        get => GetProperty<UserModelOnline>();
        set => SetProperty(value);
    }

        /// <summary>
        
        
        /// </summary>
        [ParseFieldName("status")]
        public string Status
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }
}
