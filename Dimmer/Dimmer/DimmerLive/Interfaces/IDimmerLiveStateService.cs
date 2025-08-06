namespace Dimmer.DimmerLive.Interfaces;
public interface IDimmerLiveStateService
{
    void SaveUserLocally(UserModelView user);
    
    
    void TransferUserCurrentDevice(string userId, string originalDeviceId, string newDeviceId);

    void RequestSongFromDifferentDevice(string userId, string songId, string deviceId);

    Task FullySyncUser(string userEmail);
    void DeleteUserLocally(UserModel user);
    Task DeleteUserOnline(UserModelOnline user);
    Task<bool> LoginUserAsync(UserModel usr);
    Task SignUpUserAsync(UserModelView user);
    Task<bool> AttemptAutoLoginAsync();
    Task LogoutUser();
    Task ForgottenPassword();

    // --- NEW MESSAGING METHODS ---
    IObservable<IEnumerable<ChatConversation>> ObserveUserConversations();
    IObservable<IEnumerable<ChatMessage>> ObserveMessagesForConversation(string conversationId);
    Task<ChatConversation?> GetOrCreateConversationWithUserAsync(string userId);
    Task<ChatMessage?> SendTextMessageAsync(ChatConversation conversation, string text);
    Task<ChatMessage?> ShareSongInChatAsync(string conversationId, DimmerSharedSong songToShare);
    Task MarkConversationAsReadAsync(string conversationId);
    Task<DimmerSharedSong?> ShareSongOnline(SongModelView song, double positionInSeconds);
    Task<DimmerSharedSong?> FetchSharedSongByCodeAsync(string sharedSongCode);
    Task<bool> ActivateThisDeviceAndCheckForTransferAsync();
    Task<bool> PrepareSessionTransferAsync(SongModelView currentSong, double currentPositionSeconds);

    // --- LIVE QUERY STATUS ---
    IObservable<bool> IsLiveQueryConnected { get; }
    UserModelOnline? UserOnline { get; }
    UserModel UserLocalDB { get; }
    UserModelView UserLocalView { get; }
}

