using Parse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.ParseSection.Models;

[ParseClassName("ChatMessage")]
public partial class ChatMessage : ParseObject
{
    [ParseFieldName("conversation")]
    public ChatConversation Conversation
    {
        get => GetProperty<ChatConversation>();
        set => SetProperty(value);
    }

    [ParseFieldName("sender")]
    public ParseUser Sender
    {
        get => GetProperty<ParseUser>();
        set => SetProperty(value);
    }

    [ParseFieldName("text")]
    public string Text
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }

    [ParseFieldName("messageType")]
    public string MessageType
    {
        get => GetProperty<string>();
        set => SetProperty(value); // Use constants from MessageTypes class
    }

    [ParseFieldName("attachmentFile")]
    public ParseFile AttachmentFile
    {
        get => GetProperty<ParseFile>();
        set => SetProperty(value);
    }

    [ParseFieldName("location")]
    public ParseGeoPoint Location
    {
        get => GetProperty<ParseGeoPoint>();
        set => SetProperty(value);
    }

    [ParseFieldName("replyToMessage")]
    public ChatMessage ReplyToMessage // Pointer to the message being replied to
    {
        get => GetProperty<ChatMessage>();
        set => SetProperty(value);
    }

    [ParseFieldName("sharedSong")]
    public Song SharedSong // Pointer to a shared song
    {
        get => GetProperty<Song>();
        set => SetProperty(value);
    }

    [ParseFieldName("isDeleted")]
    public bool IsDeleted
    {
        get => GetProperty<bool>();
        set => SetProperty(value);
    }
}
