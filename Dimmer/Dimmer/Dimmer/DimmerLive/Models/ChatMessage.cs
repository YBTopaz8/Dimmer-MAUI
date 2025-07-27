namespace Dimmer.DimmerLive.Models;

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
    public DimmerSharedSong SharedSong // Pointer to a shared song
    {
        get => GetProperty<DimmerSharedSong>();
        set => SetProperty(value);
    }

    [ParseFieldName("isDeleted")]
    public bool IsDeleted
    {
        get => GetProperty<bool>();
        set => SetProperty(value);
    }

    [ParseFieldName("readBy")]
    public IList<ParseUser> ReadBy
    {
        get => GetProperty<IList<ParseUser>>();
        set => SetProperty(value); // Or use AddUnique for managing this array
    }
    [ParseFieldName("reactions")]
    public IDictionary<string, IList<string>> Reactions // Key: emoji, Value: List of User ObjectIds
    {
        get => GetProperty<IDictionary<string, IList<string>>>();
        set => SetProperty(value);
    }
}
