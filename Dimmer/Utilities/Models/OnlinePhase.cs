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
    public ParseUser Sender { get => GetProperty<ParseUser>("sender"); set => SetProperty(value, "sender"); }

    [ParseFieldName("receiver")]
    public ParseUser Receiver { get => GetProperty<ParseUser>("receiver"); set => SetProperty(value, "receiver"); }

    [ParseFieldName("status")]
    public string Status { get => GetProperty<string>("status"); set => SetProperty(value, "status"); } // "pending", "accepted", "rejected"
}

[ParseClassName("Friendship")]
public partial class Friendship : ParseObject
{
    [ParseFieldName("User1")]
    public ParseUser User1 { get => GetProperty<ParseUser>("user1"); set => SetProperty(value, "user1"); }

    [ParseFieldName("User2")]
    public ParseUser User2 { get => GetProperty<ParseUser>("user2"); set => SetProperty(value, "user2"); }

    public string Status { get => GetProperty<string>(); set => SetProperty(value); } // "pending", "accepted"
}
[ParseClassName("ChatRoom")]
public partial class ChatRoom : ParseObject
{
    
    public ParseRelation<ParseUser> Participants
    {
        get => GetRelation<ParseUser>("Participants");
    }
    // Optional: Type of chat (direct or group)
    public string Type
    {
        get => GetProperty<string>(nameof(Type));
        set => SetProperty(value, nameof(Type));
    }

    // Optional: Name of the chat (for group chats, or auto-generated for direct)
    public string Name
    {
        get => GetProperty<string>(nameof(Name));
        set => SetProperty(value, nameof(Name));
    }
}

[ParseClassName("Message")]
public partial class Message : ParseObject
{
    [ParseFieldName("chatRoom")]
    public ChatRoom ChatRoom { get => GetProperty<ChatRoom>("chatRoom"); set => SetProperty(value, "chatRoom"); }

    [ParseFieldName("sender")]
    public ParseUser Sender { get => GetProperty<ParseUser>("sender"); set => SetProperty(value, "sender"); }

    [ParseFieldName("content")]
    public string Content { get => GetProperty<string>("content"); set => SetProperty(value, "content"); }

    [ParseFieldName("isEdited")]
    public bool IsEdited { get => GetProperty<bool>("isEdited"); set => SetProperty(value, "isEdited"); }

    [ParseFieldName("isDeleted")]
    public bool IsDeleted { get => GetProperty<bool>("isDeleted"); set => SetProperty(value, "isDeleted"); }
}

[ParseClassName("SharedPlaylist")]
public partial class SharedPlaylist : ParseObject
{
    [ParseFieldName("name")]
    public string Name { get => GetProperty<string>("name"); set => SetProperty(value, "name"); }

    [ParseFieldName("owner")]
    public ParseUser Owner { get => GetProperty<ParseUser>("owner"); set => SetProperty(value, "owner"); }

    [ParseFieldName("sharedWith")]
    public List<ParseUser> SharedWith { get => GetProperty<List<ParseUser>>("sharedWith"); set => SetProperty(value, "sharedWith"); }
}

[ParseClassName("PlaylistTrack")] // For many-to-many relationship
public partial class PlaylistTrack : ParseObject
{
    [ParseFieldName("playlist")]
    public SharedPlaylist Playlist { get => GetProperty<SharedPlaylist>("playlist"); set => SetProperty(value, "playlist"); }

    [ParseFieldName("trackId")] // You'll need a Song/Track class
    public string TrackId { get => GetProperty<string>("trackId"); set => SetProperty(value, "trackId"); }
}

[ParseClassName("Achievement")]
public partial class Achievement : ParseObject
{
    [ParseFieldName("name")]
    public string Name { get => GetProperty<string>("name"); set => SetProperty(value, "name"); }

    [ParseFieldName("description")]
    public string Description { get => GetProperty<string>("description"); set => SetProperty(value, "description"); }

    [ParseFieldName("icon")] // ParseFile for the icon
    public ParseFile Icon { get => GetProperty<ParseFile>("icon"); set => SetProperty(value, "icon"); }
}
[ParseClassName("UserAchievement")]
public partial class UserAchievement : ParseObject
{
    [ParseFieldName("user")]
    public ParseUser User { get => GetProperty<ParseUser>("user"); set => SetProperty(value, "user"); }

    [ParseFieldName("achievement")]
    public Achievement Achievement { get => GetProperty<Achievement>("achievement"); set => SetProperty(value, "achievement"); }
}



[ParseClassName("UserActivity")]
public partial class UserActivity : ParseObject
{
    [ParseFieldName("activityType")]
    public string ActivityType { get => GetProperty<string>("activityType"); set => SetProperty(value, "activityType"); }

    [ParseFieldName("sender")]
    public ParseUser Sender { get => GetProperty<ParseUser>("sender"); set => SetProperty(value, "sender"); }

    [ParseFieldName("recipient")]
    public ParseUser Recipient { get => GetProperty<ParseUser>("recipient"); set => SetProperty(value, "recipient"); }

    [ParseFieldName("chatMessage")]
    public Message ChatMessage { get => GetProperty<Message>("chatMessage"); set => SetProperty(value, "chatMessage"); }

    [ParseFieldName("achievement")]
    public UserAchievement Achievement { get => GetProperty<UserAchievement>("achievement"); set => SetProperty(value, "achievement"); }

    [ParseFieldName("sharedPlaylist")]
    public SharedPlaylist SharedPlaylist { get => GetProperty<SharedPlaylist>("sharedPlaylist"); set => SetProperty(value, "sharedPlaylist"); }

    [ParseFieldName("nowPlaying")]
    public SongModelView NowPlaying { get => GetProperty<SongModelView>("nowPlaying"); set => SetProperty(value, "nowPlaying"); }

    [ParseFieldName("isRead")]
    public bool IsRead { get => GetProperty<bool>("isRead"); set => SetProperty(value, "isRead"); }
    
    public Friendship? FriendRequest { get => GetProperty<Friendship>(); set => SetProperty(value); }

}