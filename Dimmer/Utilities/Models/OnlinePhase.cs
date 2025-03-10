using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer_MAUI.Utilities.Models;

class OnlinePhase
{
}
public partial class FriendRequestDisplay : ObservableObject
{
    [ObservableProperty]
    public partial string? RequestId { get; set; }
    [ObservableProperty]
    public partial string? SenderUsername { get; set; }
    [ObservableProperty]
    public partial string? SenderId { get; set; }
}
// For Displaying messages in chat UI
public partial class ChatMessageDisplay : ObservableObject
{
    [ObservableProperty]
    public partial string? MessageId { get; set; }
    [ObservableProperty]
    public partial string? SenderId { get; set; }
    [ObservableProperty]
    public partial string? SenderUsername { get; set; }
    [ObservableProperty]
    public partial string? Content { get; set; }
    [ObservableProperty]
    public partial DateTime CreatedAt { get; set; }
    [ObservableProperty]
    public partial bool IsEdited { get; set; }
    [ObservableProperty]
    public partial bool IsDeleted { get; set; }
}

[ParseClassName("FriendRequest")]
public partial class FriendRequest : ParseObject
{
    [ParseFieldName("sender")]
    public ParseUser Sender 
    {
        get => GetProperty<ParseUser>();
        set => SetProperty(value);
    }

    [ParseFieldName("receiver")]
    public ParseUser Receiver 
    {
        get => GetProperty<ParseUser>();
        set => SetProperty(value);
    }

    [ParseFieldName("status")]
    public string Status 
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }
}

[ParseClassName("Friendship")]
public partial class Friendship : ParseObject
{
    [ParseFieldName("User1")]
    public ParseUser User1 
    {
        get => GetProperty<ParseUser>();
        set => SetProperty(value);
    }

    [ParseFieldName("User2")]
    public ParseUser User2 
    {
        get => GetProperty<ParseUser>();
        set => SetProperty(value);
    }

    public string Status 
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }
}
[ParseClassName("ChatRoom")]
public partial class ChatRoom : ParseObject
{
    
    public ParseRelation<ParseUser> Participants
    {
        get => GetRelation<ParseUser>(nameof(Participants));
    }
    
    public string Type
    {
        get => GetProperty<string>(nameof(Type));
        set => SetProperty(value, nameof(Type));
    }
  

}

[ParseClassName("Message")]
public partial class Message : ParseObject
{
    [ParseFieldName("chatRoom")]
    public ChatRoom ChatRoom 
    {
        get => GetProperty<ChatRoom>();
        set => SetProperty(value);
    }

    [ParseFieldName("sender")]
    public ParseUser Sender 
    {
        get => GetProperty<ParseUser>();
        set => SetProperty(value);
    }

    [ParseFieldName("content")]
    public string Content 
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }
    
    [ParseFieldName("dateTimeOfMessage")]
    public DateTimeOffset DateTimeOfMessage
    {
        get => GetProperty<DateTimeOffset>();
        set => SetProperty(value);
    }

    [ParseFieldName("isEdited")]
    public bool IsEdited 
    {
        get => GetProperty<bool>();
        set => SetProperty(value);
    }

    [ParseFieldName("isDeleted")]
    public bool IsDeleted 
    {
        get => GetProperty<bool>();
        set => SetProperty(value);
    }

    [ParseFieldName("senderReaction")]
    public string SenderReaction 
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }

    [ParseFieldName("receiverReaction")]
    public string ReceiverReaction 
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }

    
}

[ParseClassName("SharedPlaylist")]
public partial class SharedPlaylist : ParseObject
{
    [ParseFieldName("name")]
    public string Name 
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }

    [ParseFieldName("owner")]
    public ParseUser Owner 
    {
        get => GetProperty<ParseUser>();
        set => SetProperty(value);
    }

    [ParseFieldName("sharedWith")]
    public List<ParseUser> SharedWith 
    {
        get => GetProperty<List<ParseUser>>();
        set => SetProperty(value);
    }

}

[ParseClassName("PlaylistTrack")] // For many-to-many relationship
public partial class PlaylistTrack : ParseObject
{
    [ParseFieldName("playlist")]
    public SharedPlaylist Playlist 
    {
        get => GetProperty<SharedPlaylist>();
        set => SetProperty(value);
    }

    [ParseFieldName("trackId")] // You'll need a Song/Track class
    public string TrackId 
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }
}

[ParseClassName("Achievement")]
public partial class Achievement : ParseObject
{
    [ParseFieldName("name")]
    public string Name 
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }

    [ParseFieldName("description")]
    public string Description 
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }

    [ParseFieldName("icon")] // ParseFile for the icon
    public ParseFile Icon 
    {
        get => GetProperty<ParseFile>();
        set => SetProperty(value);
    }
}
[ParseClassName("UserAchievement")]
public partial class UserAchievement : ParseObject
{
    [ParseFieldName("user")]
    public ParseUser User 
    {
        get => GetProperty<ParseUser>();
        set => SetProperty(value);
    }

    [ParseFieldName("achievement")]
    public Achievement Achievement 
    {
        get => GetProperty<Achievement>();
        set => SetProperty(value);
    }
}



[ParseClassName("UserActivity")]
public partial class UserActivity : ParseObject
{
    [ParseFieldName("activityType")]
    public string ActivityType
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }

    [ParseFieldName("sender")]
    public ParseUser Sender
    {
        get => GetProperty<ParseUser>();
        set => SetProperty(value);
    }


    [ParseFieldName("chatMessage")]
    public Message ChatMessage
    {
        get => GetProperty<Message>();
        set => SetProperty(value);
    }

    [ParseFieldName("achievement")]
    public UserAchievement Achievement 
    {
        get => GetProperty<UserAchievement>();
        set => SetProperty(value);
    }

    [ParseFieldName("sharedPlaylist")]
    public SharedPlaylist SharedPlaylist 
    {
        get => GetProperty<SharedPlaylist>();
        set => SetProperty(value);
    }

    [ParseFieldName("nowPlaying")]
    public SongModelView NowPlaying 
    {
        get => GetProperty<SongModelView>();
        set => SetProperty(value);
    }

    [ParseFieldName("isRead")]
    public bool IsRead 
    {
        get => GetProperty<bool>();
        set => SetProperty(value);
    }
    
   

}

