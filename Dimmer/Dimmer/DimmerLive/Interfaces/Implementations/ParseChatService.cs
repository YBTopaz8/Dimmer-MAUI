using DynamicData;

using Parse.LiveQuery;

using ReactiveUI;

using Syncfusion.Maui.Toolkit.NavigationDrawer;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.DimmerLive.Interfaces.Implementations;
public partial class ParseChatService : ObservableObject, IChatService, IDisposable
{
    private readonly BaseViewModel _baseVM;
    private readonly IAuthenticationService _authService;
    private readonly ILogger<ParseChatService> _logger;
    private readonly ParseLiveQueryClient _liveQueryClient;

    private readonly SourceCache<ChatMessage, string> _msgCache = new(c => c.ObjectId);
    private readonly SourceCache<ChatConversation, string> _conversationsCache = new(c => c.ObjectId);
    private Subscription<ChatConversation>? _conversationSubscription;
    private Subscription<ChatMessage>? _msgSub;

    // This is a powerful pattern. It's a dictionary that holds the message cache for each conversation.
    private readonly Dictionary<string, (SourceCache<ChatMessage, string> cache, IDisposable subscription)> _messageListeners = new();
    private readonly CompositeDisposable _disposables = new();

    public IObservable<IChangeSet<ChatConversation, string>> Conversations => _conversationsCache.Connect();
    public IObservable<IChangeSet<ChatMessage, string>> Messages => _msgCache.Connect();

    public ParseChatService(
        IAuthenticationService authService,
        ILogger<ParseChatService> logger,
        BaseViewModel baseVM,
        ParseLiveQueryClient liveQueryClient)
    {
        _baseVM = baseVM;
        _authService = authService;
        _logger = logger;
        _liveQueryClient = liveQueryClient;
        //_authService.CurrentUser
        //  .Subscribe(user =>
        //  {
        //      //if (user != null)
        //      //{
        //      //    StartListeners(user);
        //      //}
        //      //else
        //      //{
        //      //    StopListeners();
        //      //}
        //  })
        //  .DisposeWith(_disposables);
        StartListeners();
    }

    private readonly object _lock = new();
    public void StartListeners(UserModelOnline? currentUser = null)
    {
        lock (_lock)
        {
            if (_conversationSubscription != null)
                return; // Already running



            var query = ParseClient.Instance.GetQuery<ChatMessage>()
                .Include(nameof(ChatMessage.UserSenderId));
            var _messageSub = _liveQueryClient.Subscribe(query);


            _liveQueryClient.OnConnectionStateChanged
          .ObserveOn(RxApp.MainThreadScheduler) // Best practice: ensure UI updates are on the main thread
          .Subscribe(state =>
          {
              Debug.WriteLine($"[LiveQuery Status]: Connection state is now {state}");
              IsConnectedToMessagesLQ = state == LiveQueryConnectionState.Connected;
          });

            _liveQueryClient.OnError
            .Subscribe(ex =>
            {
                IsConnectedToMessagesLQ=false;
                Debug.WriteLine($"[LiveQuery Error]: {ex.Message}");
            });


            _liveQueryClient.OnDisconnected
                .Do(async info =>
                {
                    await _liveQueryClient.ReconnectAsync();
                    IsConnectedToMessagesLQ=false;
                    Debug.WriteLine($"Server disconnected.{info.Reason}");
                })
                .Subscribe();

            _liveQueryClient.OnSubscribed

                .ObserveOn(RxApp.TaskpoolScheduler)
                .Do(async e =>
                {
                    await SendTextMessageAsync("Hello 😄" + Username + "!");
                    Debug.WriteLine("Subscribed to: " + e.requestId);
                })
                .Subscribe();

            _messageSub.On(Subscription.Event.Enter, convo =>
            {
                _msgCache.AddOrUpdate(convo);
            });
            _messageSub.On(Subscription.Event.Create, convo =>
            {
                _msgCache.AddOrUpdate(convo);
            });
            _messageSub.On(Subscription.Event.Update, convo => _msgCache.AddOrUpdate(convo));
            //_messageSub.On(Subscription.Event.Leave, convo => _msgCache.Remove(convo));
            _messageSub.On(Subscription.Event.Delete, convo => _msgCache.Remove(convo));

            _disposables.Add(Disposable.Create(() => _conversationSubscription?.UnsubscribeNow()));
            _logger.LogInformation("ChatService listeners started.");
        }
    }

    public async Task<ChatConversation?> GetOrCreateConversationWithUserAsync(UserModelOnline otherUser)
    {
        var currentUser = _authService.CurrentUserValue;
        if (currentUser == null || otherUser == null) // Removed: || currentUser.ObjectId == otherUser.ObjectId
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
    [ObservableProperty]
    public partial bool IsConnectedToMessagesLQ { get; set; }

    private async Task LoadInitialMessagesAsync(SourceCache<ChatMessage, string> cache, ChatConversation conversation)
    {
        try
        {
            var messageQuery = new ParseQuery<ChatMessage>(ParseClient.Instance)
                .WhereEqualTo("conversation.objectId", conversation.ObjectId)
                ;
            var initialMessages = await messageQuery.FindAsync();
            cache.Edit(updater => updater.AddOrUpdate(initialMessages));

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch initial messages for conversation {ConversationId}", conversation.ObjectId);
        }
    }


    public IObservable<IChangeSet<ChatMessage, string>> GetMessagesForConversation(ChatConversation conversation)
    {
        if (conversation == null)
            return Observable.Empty<IChangeSet<ChatMessage, string>>();
        lock (_lock)
        {

            // If we already have a listener for this conversation, return its observable
            if (_messageListeners.TryGetValue(conversation.ObjectId, out var listener))
            {
                return listener.cache.Connect();
            }

            _logger.LogInformation("Creating new message listener for conversation {ConversationId}", conversation.ObjectId);

            // Create a new cache and subscription for this conversation's messages

            var messageQuery = new ParseQuery<ChatMessage>(ParseClient.Instance)
                .WhereEqualTo("conversationId", conversation.ObjectId)
                .Include($"{nameof(ChatMessage.SharedSong)}.uploader"); // Include nested pointers


            var messageSubscription = _liveQueryClient.Subscribe(messageQuery);
            IsConnectedToMessagesLQ= messageSubscription.IsConnected;


            var messageCache = new SourceCache<ChatMessage, string>(m => m.ObjectId);

            LoadInitialMessagesAsync(messageCache, conversation)
          .FireAndForget(ex => _logger.LogError(ex, "Initial message load failed for {ConvoId}", conversation.ObjectId));



            // Wire up live query events to the cache
            messageSubscription.On(Subscription.Event.Enter, msg =>
            {
                // connected so let ui know

                _logger.LogInformation("New message in conversation {ConversationId}: {MessageText}", conversation.ObjectId, msg.Text);
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
    }

    public string Username
    { get; set; }
      
    public async Task SendTextMessageAsync(string text, string? receverObjectId = null,SongModelView? song=null)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;
      Username=  DeviceInfo.Current.Platform +" "+ DeviceInfo.VersionString +" "+  DeviceInfo.Manufacturer;

        receverObjectId = receverObjectId ?? Username;
        try
        {
            
            var message = new ChatMessage
            {

                //Sender = _authService.CurrentUserValue,
                Text = text,
                MessageType = "Text",
                
            };

            if(Username is null)
            {
Username = "Unknown User "+Guid.NewGuid();
            }
            message["UserName"]=Username;
            message["Username"]=Username;
            message["senderId"] = receverObjectId; // For Cloud Code use
            
            message["UserSenderId"] = receverObjectId; // For Cloud Code use
            if (song is not null && !string.IsNullOrEmpty(song.FilePath))
            {
                return;
           

            }
            // ACLs are best handled by a beforeSave trigger in Cloud Code
            await message.SaveAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }


    public static void GetSongMimeType(SongModelView song, out string mimeType, out string fileExtension)
    {
        // Basic implementation based on file extension
        fileExtension = Path.GetExtension(song.FilePath)?.ToLowerInvariant() ?? ".mp3";
        switch (fileExtension)
        {
            case ".mp3":
                mimeType = "audio/mpeg";
                break;
            case ".wav":
                mimeType = "audio/wav";
                break;
            case ".flac":
                mimeType = "audio/flac";
                break;
            case ".aac":
                mimeType = "audio/aac";
                break;
            default:
                mimeType = "application/octet-stream"; // Fallback
                break;
        }
    }

    public async Task ShareSongAsync( SongModelView song, double position)
    {

        // save songFile Parse
        
        var stream = await File.ReadAllBytesAsync(song.FilePath);

        GetSongMimeType(song, out var mimeType, out var fileExtension);

        ParseFile songFile = new ParseFile($"{song.Title}.{song.FileFormat}", stream, mimeType);

        await songFile.SaveAsync(ParseClient.Instance);

        // Create the DimmerSharedSong object
        DimmerSharedSong newSong = new()
        {
            Title = song.Title,
            ArtistName = song.ArtistName,
            AlbumName = song.AlbumName,
            DurationSeconds = song.DurationInSeconds,
            GenreName = song.GenreName,
            IsFavorite = song.IsFavorite,
            SharedPositionInSeconds = position,
            

            
        };

        newSong.AudioFile = songFile;
        newSong.Uploader = _authService.CurrentUserValue;
        newSong.AudioFileUrl =songFile.Url; // For Cloud Code use
        newSong.AudioFileName =songFile.Name; // For Cloud Code use
        newSong.AudioFileMimeType =songFile.MimeType; // For Cloud Code use
        return;
        await newSong.SaveAsync();

        ChatMessage newMsg = new()
        {
            //Sender = _authService.CurrentUserValue,
            Text = $"Shared a song: {song.Title} by {song.ArtistName}",
            MessageType = "SongShare",
            SharedSong = newSong
        };
        newMsg["songId"] = newSong.ObjectId; 
        newMsg.SongId= newSong.ObjectId;


       await  newMsg.SaveAsync();

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
        _conversationsCache.Clear();
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