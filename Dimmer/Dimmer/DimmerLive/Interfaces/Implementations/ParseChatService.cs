using DynamicData;

using Parse.LiveQuery;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.DimmerLive.Interfaces.Implementations;
public partial class ParseChatService : IChatService, IDisposable
{
    private readonly IAuthenticationService _authService;
    private readonly ILogger<ParseChatService> _logger;
    private readonly ParseLiveQueryClient _liveQueryClient;

    private readonly SourceCache<ChatConversation, string> _conversationsCache = new(c => c.ObjectId);
    private Subscription<ChatConversation>? _conversationSubscription;

    // This is a powerful pattern. It's a dictionary that holds the message cache for each conversation.
    private readonly Dictionary<string, (SourceCache<ChatMessage, string> cache, IDisposable subscription)> _messageListeners = new();
    private readonly CompositeDisposable _disposables = new();

    public IObservable<IChangeSet<ChatConversation, string>> Conversations => _conversationsCache.Connect();

    public ParseChatService(
        IAuthenticationService authService,
        ILogger<ParseChatService> logger,
        ParseLiveQueryClient liveQueryClient)
    {
        _authService = authService;
        _logger = logger;
        _liveQueryClient = liveQueryClient;
    }

    public void StartListeners()
    {
        var currentUser = _authService.CurrentUserValue;
        if (currentUser == null)
            return;

        StopListeners(); // Ensure no lingering listeners

        // Live Query for conversations the current user is a participant in
        var conversationQuery = new ParseQuery<ChatConversation>(ParseClient.Instance)
            .WhereEqualTo("Participants", currentUser)
            .Include(nameof(ChatConversation.LastMessage)); // Include last message data for previews

        _conversationSubscription = _liveQueryClient.Subscribe(conversationQuery);
        _conversationSubscription.On(Subscription.Event.Enter, convo =>
        {
            _conversationsCache.AddOrUpdate(convo);
        });
        _conversationSubscription.On(Subscription.Event.Create, convo => _conversationsCache.AddOrUpdate(convo));
        _conversationSubscription.On(Subscription.Event.Update, convo => _conversationsCache.AddOrUpdate(convo));
        _conversationSubscription.On(Subscription.Event.Leave, convo => _conversationsCache.Remove(convo));
        _conversationSubscription.On(Subscription.Event.Delete, convo => _conversationsCache.Remove(convo));

        _disposables.Add(Disposable.Create(() => _conversationSubscription?.UnsubscribeNow()));
        _logger.LogInformation("ChatService listeners started.");
    }

    public async Task<ChatConversation?> GetOrCreateConversationWithUserAsync(UserModelOnline otherUser)
    {
        var currentUser = _authService.CurrentUserValue;
        if (currentUser == null || otherUser == null || currentUser.ObjectId == otherUser.ObjectId)
        {
            _logger.LogWarning("Cannot create conversation, invalid user data.");
            return null;
        }

        try
        {
            // Use Cloud Code to handle this complex logic atomically
            var parameters = new Dictionary<string, object> { { "otherUserId", otherUser.ObjectId } };
            var result = await ParseClient.Instance.CallCloudCodeFunctionAsync<ChatConversation>("getOrCreateDirectConversation", parameters);
            _conversationsCache.AddOrUpdate(result); // Add to our live cache
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get or create conversation with {Username}", otherUser.Username);
            return null;
        }
    }

    public IObservable<IChangeSet<ChatMessage, string>> GetMessagesForConversation(ChatConversation conversation)
    {
        if (conversation == null)
            return Observable.Empty<IChangeSet<ChatMessage, string>>();

        // If we already have a listener for this conversation, return its observable
        if (_messageListeners.TryGetValue(conversation.ObjectId, out var listener))
        {
            return listener.cache.Connect();
        }

        _logger.LogInformation("Creating new message listener for conversation {ConversationId}", conversation.ObjectId);

        // Create a new cache and subscription for this conversation's messages
        var messageCache = new SourceCache<ChatMessage, string>(m => m.ObjectId);

        var messageQuery = new ParseQuery<ChatMessage>(ParseClient.Instance)
            .WhereEqualTo("conversation", conversation)
            .Include(nameof(ChatMessage.Sender)); // Important for displaying sender info

        var messageSubscription = _liveQueryClient.Subscribe(messageQuery);

        // Fetch initial messages
        messageQuery.FindAsync().ContinueWith(t => {
            if (t.IsCompletedSuccessfully)
            {
                messageCache.AddOrUpdate(t.Result);
            }
        });

        // Wire up live query events to the cache
        messageSubscription.On(Subscription.Event.Enter, msg =>
        {
            messageCache.AddOrUpdate(msg);
        });
        messageSubscription.On(Subscription.Event.Create, msg =>
        {
            messageCache.AddOrUpdate(msg);
        });
        messageSubscription.On(Subscription.Event.Update, msg => messageCache.AddOrUpdate(msg));
        messageSubscription.On(Subscription.Event.Delete, msg => messageCache.Remove(msg));

        var subscriptionDisposable = Disposable.Create(() => messageSubscription.UnsubscribeNow());
        _messageListeners[conversation.ObjectId] = (messageCache, subscriptionDisposable);

        return messageCache.Connect();
    }

    public async Task SendTextMessageAsync(ChatConversation conversation, string text)
    {
        if (conversation == null || string.IsNullOrWhiteSpace(text) || _authService.CurrentUserValue == null)
            return;

        var message = new ChatMessage
        {
            Conversation = conversation,
            Sender = _authService.CurrentUserValue,
            Text = text,
            MessageType = "Text"
        };

        // ACLs are best handled by a beforeSave trigger in Cloud Code
        await message.SaveAsync();
    }

    public Task ShareSongAsync(ChatConversation conversation, SongModelView song, double position)
    {
        // This is a prime candidate for a Cloud Code function
        var parameters = new Dictionary<string, object>
        {
            { "conversationId", conversation.ObjectId },
            { "title", song.Title },
            { "artist", song.ArtistName },
            { "album", song.AlbumName },
            { "position", position }
        };

        return ParseClient.Instance.CallCloudCodeFunctionAsync<string>("shareSongInChat", parameters);
    }

    public void StopListeners()
    {
        _conversationSubscription?.UnsubscribeNow();
        _conversationSubscription = null;

        foreach (var listener in _messageListeners.Values)
        {
            listener.subscription.Dispose();
        }
        _messageListeners.Clear();
        _logger.LogInformation("ChatService listeners stopped.");
    }

    public void Dispose()
    {
        StopListeners();
        _disposables.Dispose();
        _conversationsCache.Dispose();
        foreach (var listener in _messageListeners.Values)
        {
            listener.cache.Dispose();
        }
    }

    public async Task<ChatConversation?> GetGeneralChatAsync()
    {
        try
        {
            // The cloud function handles finding or creating the one and only general chat.
            // We pass no parameters because it's a globally known object.
            var generalChat = await ParseClient.Instance.CallCloudCodeFunctionAsync<ChatConversation>("getOrCreateGeneralChat", new Dictionary<string, object>());
            _conversationsCache.AddOrUpdate(generalChat); // Add it to our local cache
            return generalChat;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get the General Chat conversation.");
            return null;
        }
    }
}