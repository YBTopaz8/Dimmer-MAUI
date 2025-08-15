
using DynamicData;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.DimmerLive.Interfaces;
public class ParseChatService : IChatService
{
    private readonly IAuthenticationService _authService;
    private readonly ILogger<ParseChatService> _logger;

    public ParseChatService(IAuthenticationService authService, ILogger<ParseChatService> logger)
    {
        _authService = authService;
        _logger = logger;
    }
    public IObservable<IChangeSet<ChatConversation, string>> Conversations => throw new NotImplementedException();

    public IObservable<IChangeSet<ChatMessage, string>> GetMessagesForConversation(ChatConversation conversation)
    {
        throw new NotImplementedException();
    }

    public Task<ChatConversation?> GetOrCreateConversationWithUserAsync(UserModelOnline otherUser)
    {
        throw new NotImplementedException();
    }

    public Task SendTextMessageAsync(ChatConversation conversation, string text)
    {
        throw new NotImplementedException();
    }

    public async Task ShareSongAsync(ChatConversation conversation, SongModelView song, double position)
    {
        var currentUser = _authService.CurrentUserValue;
        if (currentUser == null || conversation == null)
            return;

        var parameters = new Dictionary<string, object>
    {
        { "conversationId", conversation.ObjectId },
        { "title", song.Title },
        { "artist", song.ArtistName },
        { "album", song.AlbumName },
        { "position", position }
        // We don't send the whole song, just the metadata.
    };
        await ParseClient.Instance.CallCloudCodeFunctionAsync<string>("shareSongInChat", parameters);
    }
    public void StartListeners()
    {
        throw new NotImplementedException();
    }

    public void StopListeners()
    {
        throw new NotImplementedException();
    }
}
