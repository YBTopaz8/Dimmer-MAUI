using DynamicData;

namespace Dimmer.DimmerLive.Interfaces;
public interface IChatService
{
    Task<ChatConversation?> GetGeneralChatAsync();
    IObservable<IChangeSet<ChatConversation, string>> Conversations { get; }
    IObservable<IChangeSet<ChatMessage, string>> Messages { get; }
    string Username { get; }

    // Get a live stream of messages for a specific conversation

    // Create or find a 1-on-1 chat with another user
    Task<ChatConversation?> GetOrCreateConversationWithUserAsync(UserModelOnline otherUser);



    // The key feature: Share a song
    Task ShareSongAsync( SongModelView song, double position);

    //void StartListeners();
    void StopListeners();
    IObservable<IChangeSet<ChatMessage, string>> GetMessagesForConversation(ChatConversation conversation);
    Task SendTextMessageAsync(string text, string? receverObjectId = null, SongModelView? song = null);
    void StartListeners(UserModelOnline? currentUser = null);
}