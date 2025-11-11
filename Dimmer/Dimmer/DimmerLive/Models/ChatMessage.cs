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


    [ParseFieldName("Text")]
    public string Text
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }

    [ParseFieldName("SongId")]
    public string SongId
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }

    [ParseFieldName("MessageType")]
    public string MessageType
    {
        get => GetProperty<string>();
        set => SetProperty(value); // Use constants from MessageTypes class
    }

    [ParseFieldName("AttachmentFile")]
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

    [ParseFieldName("SharedSong")]
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
    public IList<UserModelOnline> ReadBy
    {
        get => GetProperty<IList<UserModelOnline>>();
        set => SetProperty(value); // Or use AddUnique for managing this array
    }
    [ParseFieldName("reactions")]
    public IDictionary<string, IList<string>> Reactions // Key: emoji, Value: List of User ObjectIds
    {
        get => GetProperty<IDictionary<string, IList<string>>>();
        set => SetProperty(value);
    }
    [ParseFieldName("UserDevicePlatform")]
    public string UserDevicePlatform
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }
    [ParseFieldName("UserDeviceIdiom")]
    public string UserDeviceIdiom
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }
    [ParseFieldName("UserDeviceVersion")]
    public string UserDeviceVersion
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }
    [ParseFieldName("UserName")]
    public string UserName
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }
    [ParseFieldName("UserSenderId")]
    public string UserSenderId
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }
   
    public string TargetDeviceSessionId { get; internal set; }
}
