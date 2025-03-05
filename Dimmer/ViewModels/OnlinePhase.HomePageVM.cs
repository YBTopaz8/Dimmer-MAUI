using Parse.LiveQuery;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer_MAUI.ViewModels;

public partial class HomePageVM
{
    [ObservableProperty]
    public partial ObservableCollection<FriendRequestDisplay>? PendingFriendRequests { get; set; } = new();

    [ObservableProperty]
    public partial ObservableCollection<ChatMessageDisplay>? ChatMessages { get; set; } = new();

    private ParseLiveQueryClient? _liveQueryClient; //keep LQ ref for later disposal
    private Subscription<UserActivity>? _friendRequestSubscription; //keep sub ref for later disposal
    private Subscription<UserActivity>? _chatMessageSubscription;
    private CancellationTokenSource _cts = new(); //to cancel async ops

    // --- Live Query Connection Management ---
    public async Task ConnectToLiveQueriesAsync()
    {
        if (_liveQueryClient == null || !_liveQueryClient.IsConnected)
        {
            try
            {
                _liveQueryClient = new ParseLiveQueryClient(new Uri("wss://YOUR_PARSE_SERVER"));
                _liveQueryClient.ConnectIfNeeded();
                Console.WriteLine("Live Query Client Connected");

                // Subscribe to Friend Requests and Chat Messages
                await SubscribeToFriendRequestsAsync();
                if (await CheckIfUserHasFriends()) //check for friends before listening
                {
                    await SubscribeToChatMessagesAsync();
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error connecting to LiveQuery: {ex.Message}");
                // Implement robust reconnection logic here
            }
        }
    }

    public void Disconnect()
    {
        if (_liveQueryClient != null)
        {
            try
            {
                // Unsubscribe from all subscriptions
                _liveQueryClient.RemoveAllSubscriptions();
                

                _liveQueryClient.Disconnect();
                Console.WriteLine("LiveQuery Client Disconnected");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error disconnecting from LiveQuery: {ex.Message}");
            }
            finally
            {
                _liveQueryClient = null;
            }
        }
        _cts.Cancel(); // Cancel any ongoing tasks
    }

    bool IsConnected;
    // --- Subscribe to Friend Requests ---
    private async Task SubscribeToFriendRequestsAsync()
    {
        if (_liveQueryClient == null)
            return;
        
        try
        {            
            CurrentUserOnline = await ParseClient.Instance.GetCurrentUser();
            var query = ParseClient.Instance.GetQuery("UserActivity")
                .WhereEqualTo("recipient", CurrentUserOnline)
                .WhereEqualTo("activityType", "FriendRequest");
            var subscription = LiveClient!.Subscribe(query, "FriendRequestsSub");
            LiveClient.ConnectIfNeeded();
            int retryDelaySeconds = 5;
            int maxRetries = 10;
            int batchSize = 3; // Number of events to release at a time
            TimeSpan throttleTime = TimeSpan.FromMilliseconds(0000);

            LiveClient.OnConnected
                .Do(_ => Debug.WriteLine("LiveQuery connected."))
                .RetryWhen(errors =>
                    errors
                        .Zip(Observable.Range(1, maxRetries), (error, attempt) => (error, attempt))
                        .SelectMany(tuple =>
                        {
                            if (tuple.attempt > maxRetries)
                            {
                                Debug.WriteLine($"Max retries reached. Error: {tuple.error.Message}");
                                return Observable.Throw<Exception>(tuple.error); // Explicit type here
                            }
                            IsConnected = false;
                            Debug.WriteLine($"Retry attempt {tuple.attempt} after {retryDelaySeconds} seconds...");

                            // Explicit reconnect call before retry delay
                            LiveClient.ConnectIfNeeded(); // revive app!

                            return Observable.Timer(TimeSpan.FromSeconds(retryDelaySeconds)).Select(_ => tuple.error); // Maintain compatible type
                        })
                )
                .Subscribe(
                    _ =>
                    {
                        IsConnected=true;
                        Debug.WriteLine("Reconnected successfully.");
                    },
                    ex => Debug.WriteLine($"Failed to reconnect: {ex.Message}")
                );

            LiveClient.OnError
                .Do(ex =>
                {
                    Debug.WriteLine("LQ Error: " + ex.Message);
                    LiveClient.ConnectIfNeeded();  // Ensure reconnection on errors
                })
                .OnErrorResumeNext(Observable.Empty<Exception>()) // Prevent breaking the stream
                .Subscribe();

            LiveClient.OnDisconnected
                .Do(info => Debug.WriteLine(info.userInitiated
                    ? "User disconnected."
                    : "Server disconnected."))
                .Subscribe();

            LiveClient.OnSubscribed
                .Do(e => Debug.WriteLine("Subscribed to: " + e.requestId))
                .Subscribe();


            LiveClient.OnObjectEvent
            .Where(e => e.subscription == subscription) // Filter relevant events
            .GroupBy(e => e.evt)
            .SelectMany(group =>
            {
                if (group.Key == Subscription.Event.Create)
                {
                    // Apply throttling only to CREATE events
                    return group.Throttle(throttleTime)
                                .Buffer(TimeSpan.FromSeconds(1), 3) // Further control
                                .SelectMany(batch => batch); // Flatten the batch
                }
                else
                {
                    //do something with group !
                    // Pass through other events without throttling
                    return group;
                }
            })
            .Subscribe(e =>
            {
                ProcessEvent(e);
            });

            
            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error subscribing to friend requests: {ex.Message}");
        }
    }

    private async void FetchAndAddFriendRequest(FriendRequest friendRequest)
    {
        try
        {
            // Fetch the sender's user data
            var sender = await friendRequest.Sender.FetchIfNeededAsync();
            var newRequest = new FriendRequestDisplay
            {
                RequestId = friendRequest.ObjectId,
                SenderUsername = sender.Username,
                SenderId = sender.ObjectId
            };
            PendingFriendRequests.Add(newRequest);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching sender data: {ex.Message}");
        }
    }

    // --- Subscribe to Chat Messages ---

    // --- Subscribe to Chat Messages ---
    private async Task SubscribeToChatMessagesAsync()
    {
        if (_liveQueryClient == null)
            return;

        try
        {
            var CurrentUserOnline = await ParseClient.Instance.GetCurrentUser();
            // Listen for UserActivity objects of type "ChatMessage"
            var query = ParseClient.Instance.GetQuery("UserActivity")
                .WhereEqualTo("recipient", CurrentUserOnline)
                .WhereEqualTo("activityType", "ChatMessage")
                .Include("chatMessage")  // Important: Include the chatMessage pointer
                .Include("chatMessage.sender"); // Include sender details

            var subscription = LiveClient!.Subscribe(query, "ChatMessagesSub");

            LiveClient.ConnectIfNeeded();
            int retryDelaySeconds = 5;
            int maxRetries = 10;
            int batchSize = 3; // Number of events to release at a time
            TimeSpan throttleTime = TimeSpan.FromMilliseconds(0000);

            LiveClient.OnConnected
                .Do(_ => Debug.WriteLine("LiveQuery connected."))
                .RetryWhen(errors =>
                    errors
                        .Zip(Observable.Range(1, maxRetries), (error, attempt) => (error, attempt))
                        .SelectMany(tuple =>
                        {
                            if (tuple.attempt > maxRetries)
                            {
                                Debug.WriteLine($"Max retries reached. Error: {tuple.error.Message}");
                                return Observable.Throw<Exception>(tuple.error); // Explicit type here
                            }
                            IsConnected = false;
                            Debug.WriteLine($"Retry attempt {tuple.attempt} after {retryDelaySeconds} seconds...");

                            // Explicit reconnect call before retry delay
                            LiveClient.ConnectIfNeeded(); // revive app!

                            return Observable.Timer(TimeSpan.FromSeconds(retryDelaySeconds)).Select(_ => tuple.error); // Maintain compatible type
                        })
                )
                .Subscribe(
                    _ =>
                    {
                        IsConnected=true;
                        Debug.WriteLine("Reconnected successfully.");
                    },
                    ex => Debug.WriteLine($"Failed to reconnect: {ex.Message}")
                );

            LiveClient.OnError
                .Do(ex =>
                {
                    Debug.WriteLine("LQ Error: " + ex.Message);
                    LiveClient.ConnectIfNeeded();  // Ensure reconnection on errors
                })
                .OnErrorResumeNext(Observable.Empty<Exception>()) // Prevent breaking the stream
                .Subscribe();

            LiveClient.OnDisconnected
                .Do(info => Debug.WriteLine(info.userInitiated
                    ? "User disconnected."
                    : "Server disconnected."))
                .Subscribe();

            LiveClient.OnSubscribed
                .Do(e => Debug.WriteLine("Subscribed to: " + e.requestId))
                .Subscribe();


            LiveClient.OnObjectEvent
            .Where(e => e.subscription == subscription) // Filter relevant events
            .GroupBy(e => e.evt)
            .SelectMany(group =>
            {
                if (group.Key == Subscription.Event.Create)
                {
                    // Apply throttling only to CREATE events
                    return group.Throttle(throttleTime)
                                .Buffer(TimeSpan.FromSeconds(1), 3) // Further control
                                .SelectMany(batch => batch); // Flatten the batch
                }
                else
                {
                    //do something with group !
                    // Pass through other events without throttling
                    return group;
                }
            })
            .Subscribe(e =>
            {
                ProcessEvent(e);
            });


        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error subscribing to chat messages: {ex.Message}");
        }
    }


    // --- Accept/Reject Friend Request (Commands) ---
    [RelayCommand]
    private async Task AcceptFriendRequest(string requestId)
    {
        try
        {
            
            // Remove the request from the UI list
            var requestToRemove = PendingFriendRequests.FirstOrDefault(r => r.RequestId == requestId);
            if (requestToRemove != null)
            {
                PendingFriendRequests.Remove(requestToRemove);
            }
            // Check if we now have friends and can start listening for chat messages
            if (await CheckIfUserHasFriends())
            {
                await SubscribeToChatMessagesAsync();
            }

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to accept friend request: {ex.Message}");
        }
    }
    void ProcessEvent((Subscription.Event evt, object objectDictionnary, Subscription subscription) e)
    {

    }
        [RelayCommand]
    private async Task RejectFriendRequest(string requestId)
    {
        try
        {
            // Remove the request from the UI list
            var requestToRemove = PendingFriendRequests.FirstOrDefault(r => r.RequestId == requestId);
            if (requestToRemove != null)
            {
                PendingFriendRequests.Remove(requestToRemove);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to reject friend request: {ex.Message}");
        }
    }
    [ObservableProperty]
    public partial ChatRoom? CurrentChatRoom { get; set; } // Store the ID of the active chat room

    // --- Send chat messages ---
    [RelayCommand]
    private async Task SendChatMessage(string messageText) //we bind directly to string now
    {
        if (string.IsNullOrWhiteSpace(messageText) || string.IsNullOrEmpty(CurrentChatRoom.ObjectId))
        {
            return; // Don't send empty messages
        }
        await SendMessageAsync(CurrentChatRoom, messageText);

    }

    public async Task<bool> CheckIfUserHasFriends()
    {
        try
        {
            ParseUser currentUser = await ParseClient.Instance.GetCurrentUser();
            if (currentUser == null)
            {
                return false;
            }

            var query1 = ParseClient.Instance.GetQuery("Friendship")
                .WhereEqualTo("user1", currentUser);
            int count1 = await query1.CountAsync();
            if (count1 > 0)
            {
                return true; // Early return if we find friends in the first query
            }

            var query2 = ParseClient.Instance.GetQuery("Friendship")
                .WhereEqualTo("user2", currentUser);
            int count2 = await query2.CountAsync();

            return count2 > 0; // Return true if count2 > 0, otherwise false
        }
        catch (Exception ex)
        {

            Console.WriteLine($"General Error: {ex.Message}");
            return false;
        }
    }

    // --- Example: Start a chat with a specific user ---
    public async Task StartChatWithUser(string otherUserId)
    {
        try
        {
            var q= ParseClient.Instance.GetQuery("_User").
                WhereEqualTo("objectId",otherUserId);
            var otherUser = await q.FirstOrDefaultAsync();
            // 1. Create/Get Chat Room
            CurrentChatRoom = await CreateChatRoomAsync(otherUser); //stores roomid
            //CurrentChatRoomId = await CreateChatRoomAsync(otherUser); //stores roomid

            if (!string.IsNullOrEmpty(CurrentChatRoom.ObjectId))
            {
                // 2. Clear existing messages (if any)
                ChatMessages.Clear();

                // 3.  (Optionally) Fetch existing messages from the chat room (using skip/limit for pagination)

                // 4. Subscribe to Live Queries (if not already subscribed)
                //await ConnectToLiveQueriesAsync(); //removed
                Console.WriteLine($"Started chat with user. ChatRoomId: {CurrentChatRoom.ObjectId}");
                // Navigate to your chat page, set up data binding, etc.
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error starting chat: {ex.Message}");
        }
    }

    public async Task<ChatRoom> CreateChatRoomAsync(ParseObject user2)
    {
        try
        {

            var query = ParseClient.Instance.GetQuery("ChatRoom")
                    .WhereEqualTo("participant1", CurrentUserOnline)
                    .WhereEqualTo("participant2", user2);

            var existingRoom = await query.FirstOrDefaultAsync();

            if (existingRoom != null)
                return (ChatRoom)existingRoom;

            var chatRoom = new ChatRoom
            {
                Participant1 = CurrentUserOnline,
                Participant2 = (ParseUser)user2
            };

            await chatRoom.SaveAsync();
            return chatRoom;
        }
        catch (Exception ex)
        {
            // Handle Parse-specific errors
            Console.WriteLine($"Error creating chat room: {ex.Message}");
            return null; // Or throw the exception, depending on your error handling strategy
        }
    }

    public async Task SendMessageAsync(ChatRoom chatRoom, string content)
    {
        try
        {
            ParseUser sender = CurrentUserOnline;
            // Authorization check (example)
            if (sender.ObjectId != chatRoom.Participant1.ObjectId && sender.ObjectId != chatRoom.Participant2.ObjectId)
            {
                Console.WriteLine("Unauthorized to send message in this chat room.");
                return;
            }
            var message = new Message
            {
                ChatRoom = chatRoom,
                Sender = sender,
                Content = content
            };

            await message.SaveAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending message: {ex.Message}");
        }
    }

    public async Task ListenForMessagesAsync(ChatRoom chatRoom, Action<Message> onMessageReceived)
    {
        try
        {
            _cts = new CancellationTokenSource();
            var query = ParseClient.Instance.GetQuery("Message")
                .WhereEqualTo("chatRoom", chatRoom)
                .OrderBy("createdAt"); // Order by creation time

            var subscription = LiveClient!.Subscribe(query, "FriendRequestsSub");
            LiveClient.ConnectIfNeeded();
            int retryDelaySeconds = 5;
            int maxRetries = 10;
            int batchSize = 3; // Number of events to release at a time
            TimeSpan throttleTime = TimeSpan.FromMilliseconds(0000);

            LiveClient.OnConnected
                .Do(_ => Debug.WriteLine("LiveQuery connected."))
                .RetryWhen(errors =>
                    errors
                        .Zip(Observable.Range(1, maxRetries), (error, attempt) => (error, attempt))
                        .SelectMany(tuple =>
                        {
                            if (tuple.attempt > maxRetries)
                            {
                                Debug.WriteLine($"Max retries reached. Error: {tuple.error.Message}");
                                return Observable.Throw<Exception>(tuple.error); // Explicit type here
                            }
                            IsConnected = false;
                            Debug.WriteLine($"Retry attempt {tuple.attempt} after {retryDelaySeconds} seconds...");

                            // Explicit reconnect call before retry delay
                            LiveClient.ConnectIfNeeded(); // revive app!

                            return Observable.Timer(TimeSpan.FromSeconds(retryDelaySeconds)).Select(_ => tuple.error); // Maintain compatible type
                        })
                )
                .Subscribe(
                    _ =>
                    {
                        IsConnected=true;
                        Debug.WriteLine("Reconnected successfully.");
                    },
                    ex => Debug.WriteLine($"Failed to reconnect: {ex.Message}")
                );

            LiveClient.OnError
                .Do(ex =>
                {
                    Debug.WriteLine("LQ Error: " + ex.Message);
                    LiveClient.ConnectIfNeeded();  // Ensure reconnection on errors
                })
                .OnErrorResumeNext(Observable.Empty<Exception>()) // Prevent breaking the stream
                .Subscribe();

            LiveClient.OnDisconnected
                .Do(info => Debug.WriteLine(info.userInitiated
                    ? "User disconnected."
                    : "Server disconnected."))
                .Subscribe();

            LiveClient.OnSubscribed
                .Do(e => Debug.WriteLine("Subscribed to: " + e.requestId))
                .Subscribe();


            LiveClient.OnObjectEvent
            .Where(e => e.subscription == subscription) // Filter relevant events
            .GroupBy(e => e.evt)
            .SelectMany(group =>
            {
                if (group.Key == Subscription.Event.Create)
                {
                    // Apply throttling only to CREATE events
                    return group.Throttle(throttleTime)
                                .Buffer(TimeSpan.FromSeconds(1), 3) // Further control
                                .SelectMany(batch => batch); // Flatten the batch
                }
                else
                {
                    //do something with group !
                    // Pass through other events without throttling
                    return group;
                }
            })
            .Subscribe(e =>
            {
                ProcessEvent(e);
            });

            Console.WriteLine("Connected to Live Query.");

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error connecting to Live Query: {ex.Message}");
            // Implement reconnection logic here
        }
    }

    private Subscription<Message>? _messageSubscription;
    public async Task DisconnectAsync()
    {
        if (_liveQueryClient != null && _messageSubscription != null)
        {
            try
            {
                LiveClient.RemoveAllSubscriptions();
                LiveClient.Disconnect();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error disconnecting: {ex.Message}");
            }
            finally
            {
                _messageSubscription = null;
                _liveQueryClient = null;
            }
            _cts?.Cancel(); // Cancel any ongoing operations
        }
    }
}

public class LiveQueryManager : IDisposable
{
    private ParseLiveQueryClient LiveClient { get; }
    private List<Subscription> Subscriptions { get; } = new List<Subscription>();
    private List<IDisposable> EventDisposables { get; } = new List<IDisposable>();

    public LiveQueryManager(ParseLiveQueryClient liveClient)
    {
        LiveClient = liveClient;

        // Ensure the client is connected (you might handle this elsewhere, too)
        LiveClient.ConnectIfNeeded();

        // Centralized error handling (optional, but good practice)
        var errorHandler = LiveClient.OnError
            .Subscribe(ex =>
            {
                Debug.WriteLine($"LiveQuery Error: {ex.Message}");
                // Implement reconnection logic here, if desired.
                // LiveClient.ConnectIfNeeded();
            });
        EventDisposables.Add(errorHandler);
    }

    public void SubscribeToMultipleQueries()
    {
        // 1. Chat Messages
        ParseQuery<ParseObject> chatQuery = ParseClient.Instance.GetQuery("ChatMessage");
        chatQuery = chatQuery.WhereEqualTo("roomId", "myRoomId"); // Example constraint
        Subscription chatSubscription = LiveClient.Subscribe(chatQuery);
        Subscriptions.Add(chatSubscription);
        SubscribeToEvents(chatSubscription, "Chat");

        // 2. Game Scores
        ParseQuery<ParseObject> scoreQuery = ParseClient.Instance.GetQuery("GameScore");
        scoreQuery = scoreQuery.WhereGreaterThan("score", 1000);  // Example constraint
        Subscription scoreSubscription = LiveClient.Subscribe(scoreQuery);
        Subscriptions.Add(scoreSubscription);
        SubscribeToEvents(scoreSubscription, "Score");

        // 3. User Status
        ParseQuery<ParseUser> userQuery = ParseClient.Instance.GetQuery<ParseUser>(); //Use ParseUser
        userQuery = userQuery.WhereEqualTo("online", true); // Example: Track online users
        Subscription userSubscription = LiveClient.Subscribe(userQuery);
        Subscriptions.Add(userSubscription);
        SubscribeToEvents(userSubscription, "User");

        // You could add reconnection logic here, similar to your original example.
        // It's often better to centralize this, as shown in the constructor.
    }
    private void SubscribeToEvents(Subscription subscription, string subscriptionName)
    {
        var eventHandler = LiveClient.OnObjectEvent
            .Where(e => e.subscription == subscription)
            .Subscribe(e =>
            {
                // Process events based on subscription type (e.g., chat, score, user)
                ProcessEvent(e, subscriptionName);
            });
        EventDisposables.Add(eventHandler);
    }
    private void ProcessEvent((Subscription.Event evt, object objectDictionnary, Subscription subscription) e, string subscriptionName)
    {
        // You can switch on e.evt (Create, Update, Delete, Enter, Leave)
        // and also use the subscriptionName to differentiate the data.

        Debug.WriteLine($"[{subscriptionName}] Event: {e.evt}, Object: {e.objectDictionnary}");

        // Example:
        switch (subscriptionName)
        {
            case "Chat":
                // Handle chat message events
                if (e.evt == Subscription.Event.Create)
                {
                    //var chatMessage = ObjectMapper.MapFromDictionary<ChatMessage>(e.objectDictionnary as Dictionary<string, object>);
                    // Add chatMessage to UI, etc.
                }
                break;
            case "Score":
                // Handle game score events
                if (e.evt == Subscription.Event.Update)
                {
                    //var gameScore = ObjectMapper.MapFromDictionary<GameScore>(e.objectDictionnary as Dictionary<string, object>);
                    //Update game score
                }
                break;
            case "User":
                // Handle user status events
                if (e.evt == Subscription.Event.Enter || e.evt==Subscription.Event.Update)
                {
                    var userStatus = ObjectMapper.MapFromDictionary<ParseUser>(e.objectDictionnary as Dictionary<string, object>);
                    //...
                }
                break;
        }
    }

    public void Dispose()
    {
        LiveClient.RemoveAllSubscriptions();
        // Unsubscribe from all subscriptions and dispose of event handlers.
        foreach (var subscription in Subscriptions)
        {

        }

        foreach (var disposable in EventDisposables)
        {
            disposable.Dispose(); // Release Rx.NET subscriptions
        }
        Subscriptions.Clear();
        EventDisposables.Clear();

        // LiveClient.Disconnect(); //Disconnect from the client. You may want to do that OUTSIDE of the manager.
    }
}
