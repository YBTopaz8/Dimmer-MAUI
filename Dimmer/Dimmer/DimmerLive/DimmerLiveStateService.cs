using Parse.LiveQuery;

namespace Dimmer.DimmerLive;
public class DimmerLiveStateService : IDimmerLiveStateService
{
    public IObservable<bool> IsLiveQueryConnected => throw new NotImplementedException();

    public UserModelOnline? UserOnline => throw new NotImplementedException();

    public UserModel UserLocalDB => throw new NotImplementedException();

    public UserModelView UserLocalView => throw new NotImplementedException();

    public Task<bool> ActivateThisDeviceAndCheckForTransferAsync()
    {
        throw new NotImplementedException();
    }

    public Task<bool> AttemptAutoLoginAsync()
    {
        throw new NotImplementedException();
    }

    public void DeleteUserLocally(UserModel user)
    {
        throw new NotImplementedException();
    }

    public Task DeleteUserOnline(UserModelOnline user)
    {
        throw new NotImplementedException();
    }

    public Task<DimmerSharedSong?> FetchSharedSongByCodeAsync(string sharedSongCode)
    {
        throw new NotImplementedException();
    }

    public Task ForgottenPassword()
    {
        throw new NotImplementedException();
    }

    public Task FullySyncUser(string userEmail)
    {
        throw new NotImplementedException();
    }

    public Task<ChatConversation?> GetOrCreateConversationWithUserAsync(string userId)
    {
        throw new NotImplementedException();
    }

    public Task<bool> LoginUserAsync(UserModel usr)
    {
        throw new NotImplementedException();
    }

    public Task LogoutUser()
    {
        throw new NotImplementedException();
    }

    public Task MarkConversationAsReadAsync(string conversationId)
    {
        throw new NotImplementedException();
    }

    public IObservable<IEnumerable<ChatMessage>> ObserveMessagesForConversation(string conversationId)
    {
        throw new NotImplementedException();
    }

    public IObservable<IEnumerable<ChatConversation>> ObserveUserConversations()
    {
        throw new NotImplementedException();
    }

    public Task<bool> PrepareSessionTransferAsync(SongModelView currentSong, double currentPositionSeconds)
    {
        throw new NotImplementedException();
    }

    public void RequestSongFromDifferentDevice(string userId, string songId, string deviceId)
    {
        throw new NotImplementedException();
    }

    public void SaveUserLocally(UserModelView user)
    {
        throw new NotImplementedException();
    }

    public Task<ChatMessage?> SendTextMessageAsync(ChatConversation conversation, string text)
    {
        throw new NotImplementedException();
    }

    public Task<ChatMessage?> ShareSongInChatAsync(string conversationId, DimmerSharedSong songToShare)
    {
        throw new NotImplementedException();
    }

    public Task<DimmerSharedSong?> ShareSongOnline(SongModelView song, double positionInSeconds)
    {
        throw new NotImplementedException();
    }

    public Task SignUpUserAsync(UserModelView user)
    {
        throw new NotImplementedException();
    }

    public void TransferUserCurrentDevice(string userId, string originalDeviceId, string newDeviceId)
    {
        throw new NotImplementedException();
    }


}
