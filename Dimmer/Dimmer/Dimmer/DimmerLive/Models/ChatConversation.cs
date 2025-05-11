using Parse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.DimmerLive.Models;

[ParseClassName("ChatConversation")]
public partial class ChatConversation : ParseObject
{
    // Use Relation for participants for better querying
    public ParseRelation<UserModelOnline> Participants => GetRelation<UserModelOnline>(nameof(Participants));

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

}