using Dimmer.DimmerLive.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.DimmerLive.Interfaces;
public interface IDimmerLiveStateService
{
    void SaveUserLocally(UserModelView user);
    
    
    void TransferUserCurrentDevice(string userId, string originalDeviceId, string newDeviceId);

    void RequestSongFromDifferentDevice(string userId, string songId, string deviceId);

    Task FullySyncUser(string userEmail);
    void DeleteUserLocally(UserModel user);
    Task DeleteUserOnline(UserModelOnline user);
    Task<bool> LoginUser(UserModel usr);
    Task SignUpUser(UserModelView user);
    Task<bool> AttemptAutoLoginAsync();
    Task LogoutUser();
    Task ForgottenPassword();


    // --- NEW MESSAGING METHODS ---
    IObservable<IEnumerable<ChatConversation>> ObserveUserConversations();
    IObservable<IEnumerable<ChatMessage>> ObserveMessagesForConversation(string conversationId);
    Task<ChatConversation?> GetOrCreateConversationWithUserAsync(UserModelOnline otherUser);
    Task<ChatMessage?> SendTextMessageAsync(string conversationId, string text);
    Task<ChatMessage?> ShareSongInChatAsync(string conversationId, DimmerSharedSong songToShare);
    Task MarkConversationAsReadAsync(string conversationId);
    Task ShareSongOnline(SongModelView song);

    // --- LIVE QUERY STATUS ---
    IObservable<bool> IsLiveQueryConnected { get; }
}

