using Parse.LiveQuery;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer_MAUI.ViewModels;

public partial class HomePageVM
{


    private Subscription<Message>? _messageSubscription; //  Subscription for messages



    [ObservableProperty]
    public partial ObservableCollection<FriendRequestDisplay>? PendingFriendRequests { get; set; } = new();

    [ObservableProperty]
    public partial ChatRoom? CurrentChatRoom { get; set; } 
    
    [ObservableProperty]
    public partial ObservableCollection<ChatMessageDisplay>? ChatMessages { get; set; } = new();

    private Subscription<UserActivity>? _friendRequestSubscription; //keep sub ref for later disposal
    private Subscription<UserActivity>? _chatMessageSubscription;
    private CancellationTokenSource _cts = new(); //to cancel async ops

    // --- Live Query Connection Management ---
    public async Task ConnectToLiveQueriesAsync()
    {
        if (LiveQueryClient == null || !LiveQueryClient.IsConnected)
        {
            
            try
            {
                LiveQueryClient = new ParseLiveQueryClient();
                LiveQueryClient.ConnectIfNeeded();
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
        if (LiveQueryClient != null)
        {
            try
            {
                // Unsubscribe from all subscriptions
                LiveQueryClient.RemoveAllSubscriptions();
                

                LiveQueryClient.Disconnect();
                Console.WriteLine("LiveQuery Client Disconnected");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error disconnecting from LiveQuery: {ex.Message}");
            }
            finally
            {
                LiveQueryClient = null;
            }
        }
        _cts.Cancel(); // Cancel any ongoing tasks
    }

    bool IsConnected;
    // --- Subscribe to Friend Requests ---
    private async Task SubscribeToFriendRequestsAsync()
    {
        if (LiveQueryClient == null)
            return;
        
        try
        {            
            CurrentUserOnline = await ParseClient.Instance.GetCurrentUser();
            var query = ParseClient.Instance.GetQuery("UserActivity")
                .WhereEqualTo("recipient", CurrentUserOnline)
                .WhereEqualTo("activityType", "FriendRequest");
            var subscription = LiveQueryClient!.Subscribe(query, "FriendRequestsSub");
            LiveQueryClient.ConnectIfNeeded();
            int retryDelaySeconds = 5;
            int maxRetries = 10;
            int batchSize = 3; // Number of events to release at a time
            TimeSpan throttleTime = TimeSpan.FromMilliseconds(0000);

            LiveQueryClient.OnConnected
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
                            LiveQueryClient.ConnectIfNeeded(); // revive app!

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

            LiveQueryClient.OnError
                .Do(ex =>
                {
                    Debug.WriteLine("LQ Error: " + ex.Message);
                    LiveQueryClient.ConnectIfNeeded();  // Ensure reconnection on errors
                })
                .OnErrorResumeNext(Observable.Empty<Exception>()) // Prevent breaking the stream
                .Subscribe();

            LiveQueryClient.OnDisconnected
                .Do(info => Debug.WriteLine(info.userInitiated
                    ? "User disconnected."
                    : "Server disconnected."))
                .Subscribe();

            LiveQueryClient.OnSubscribed
                .Do(e => Debug.WriteLine("Subscribed to: " + e.requestId))
                .Subscribe();


            LiveQueryClient.OnObjectEvent
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
        if (LiveQueryClient == null)
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

            var subscription = LiveQueryClient!.Subscribe(query, "ChatMessagesSub");

            LiveQueryClient.ConnectIfNeeded();
            int retryDelaySeconds = 5;
            int maxRetries = 10;
            int batchSize = 3; // Number of events to release at a time
            TimeSpan throttleTime = TimeSpan.FromMilliseconds(0000);

            LiveQueryClient.OnConnected
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
                            LiveQueryClient.ConnectIfNeeded(); // revive app!

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

            LiveQueryClient.OnError
                .Do(ex =>
                {
                    Debug.WriteLine("LQ Error: " + ex.Message);
                    LiveQueryClient.ConnectIfNeeded();  // Ensure reconnection on errors
                })
                .OnErrorResumeNext(Observable.Empty<Exception>()) // Prevent breaking the stream
                .Subscribe();

            LiveQueryClient.OnDisconnected
                .Do(info => Debug.WriteLine(info.userInitiated
                    ? "User disconnected."
                    : "Server disconnected."))
                .Subscribe();

            LiveQueryClient.OnSubscribed
                .Do(e => Debug.WriteLine("Subscribed to: " + e.requestId))
                .Subscribe();


            LiveQueryClient.OnObjectEvent
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



    // --- Start/Join a Chat (Now supports group chat) ---

    //This method now handles both creating and joining
    public async Task StartOrJoinChat(List<string> participantIds)
    {
        if (participantIds == null || participantIds.Count == 0)
        {
            Debug.WriteLine("Participant list is empty.");
            return;
        }

        try
        {
            //1. Fetch those users
            List<ParseUser> participants = new();

            //Fetch users
            foreach (var id in participantIds)
            {
                var q = ParseClient.Instance.GetQuery<ParseUser>().WhereEqualTo("objectId", id);
                var user = await q.FirstOrDefaultAsync(); // Use FirstOrDefaultAsync to handle nulls
                if (user != null)
                {
                    participants.Add(user);
                }
                else
                {
                    // Handle the case where a user ID is invalid.  Maybe log it.
                    Debug.WriteLine($"User with ID {id} not found.");
                    return; // Or continue, depending on your error handling.
                }
            }

            // Add Current User.
            ParseUser currentUser = await ParseClient.Instance.GetCurrentUser();
            participants.Add(currentUser);

            //2.  Check for existing chat room (IMPORTANT for preventing duplicates).
            CurrentChatRoom = await GetExistingChatRoom(participants);

            // 3. Create if it doesn't exist.
            if (CurrentChatRoom == null)
            {
                CurrentChatRoom = await CreateChatRoomAsync(participants);
            }

            // 4. Clear Existing Messages, for safety
            ChatMessages.Clear();

            // 5. Fetch any existing messages (optional, for history).  Could add pagination here.
            await LoadExistingMessages();

            // 6. Set up the LiveQuery subscription for new messages.
            await SubscribeToMessages();
            Debug.WriteLine($"Started/Joined chat. ChatRoomId: {CurrentChatRoom.ObjectId}");

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error starting/joining chat: {ex.Message}");
        }
    }

    private async Task LoadExistingMessages()
    {
        if (CurrentChatRoom == null)
            return;

        var query = ParseClient.Instance.GetQuery<Message>()
            .WhereEqualTo(nameof(Message.ChatRoom), CurrentChatRoom)
            .Include(nameof(Message.Sender))
            .OrderBy(nameof(Message.CreatedAt)); // Or a custom timestamp if you have one.

        var existingMessages = await query.FindAsync();

        foreach (var msg in existingMessages)
        {
            var msgDisplay = new ChatMessageDisplay
            {
                SenderUsername = msg.Sender.Username, // Assuming you included Sender
                Content = msg.Content,
                
                // ... other properties ...
            };
            ChatMessages.Add(msgDisplay); // Add to your ObservableCollection.
        }
    }
    // --- Live Query Subscription for Messages ---
    private async Task SubscribeToMessages()
    {
        if (CurrentChatRoom == null)
            return;


        LiveQueryClient.RemoveAllSubscriptions();
        // 1.  Unsubscribe from any previous subscription (important!).
        //_messageSubscription?.Unsubscribe();

        // 2.  Create the query.
        var query = ParseClient.Instance.GetQuery<Message>()
            .WhereEqualTo(nameof(Message.ChatRoom), CurrentChatRoom)
            .Include(nameof(Message.Sender)); // Include the Sender for display.

        // 3.  Subscribe.
        _messageSubscription = LiveQueryClient.Subscribe(query);

        // 4.  Handle events (Connected, Error, Disconnected, Subscribed are good practice).
        int retryDelaySeconds = 5;
        int maxRetries = 10;
        LiveQueryClient.OnConnected
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
                            Debug.WriteLine($"Retry attempt {tuple.attempt} after {retryDelaySeconds} seconds...");
                            // Explicit reconnect call before retry delay
                            LiveQueryClient.ConnectIfNeeded(); // revive app!

                            return Observable.Timer(TimeSpan.FromSeconds(retryDelaySeconds)).Select(_ => tuple.error); // Maintain compatible type
                        })
                )
                .Subscribe(
                    _ =>
                    {
                        Debug.WriteLine("Reconnected successfully.");
                    },
                    ex => Debug.WriteLine($"Failed to reconnect: {ex.Message}")
                );

        LiveQueryClient.OnError.Subscribe(ex => Debug.WriteLine($"LiveQuery Error: {ex.Message}"));
        LiveQueryClient.OnDisconnected.Subscribe(info => Debug.WriteLine($"LiveQuery Disconnected: {info}"));
        LiveQueryClient.OnSubscribed.Subscribe(e => Debug.WriteLine($"Subscribed to Messages for ChatRoom: {CurrentChatRoom.ObjectId}"));

        // 5.  MOST IMPORTANT: Handle the object events.
        LiveQueryClient.OnObjectEvent
             .Where(e => e.subscription == _messageSubscription)
             .Subscribe(e =>
             {
                 // Process different event types (Create, Update, Delete, etc.).
                 if (e.evt == Subscription.Event.Create)
                 {
                     ProcessMessageEvent(e);
                 }
                 // You could handle Update/Delete if you allow editing/deleting messages.
             });
    }


    private async Task<ChatRoom?> GetExistingChatRoom(List<ParseUser> participants)
    {
        // Efficiently check for an existing chat room with ALL the given participants.
        // Using Any or Contains will not work. We need to use ContainsAll in a smart way.

        var query = ParseClient.Instance.GetQuery<ChatRoom>()
            .WhereContainedIn("Participants", participants);  //All listed participants

        // Get all potential ChatRooms, and then filter in memory for an *exact* match.
        var potentialMatches = await query.FindAsync();

        foreach (var chatRoom in potentialMatches)
        {
            // Fetch the participants for this ChatRoom.
            var chatRoomParticipants = await chatRoom.GetRelation<ParseUser>("Participants").Query.FindAsync();

            //Compare. must have same counts and contains all.
            if (chatRoomParticipants.Count() == participants.Count &&
                chatRoomParticipants.All(crp => participants.Any(p => p.ObjectId == crp.ObjectId)))
            {
                return chatRoom; // Found an exact match.

            }
        }

        return null; // No exact match found.
    }
    private async Task<ChatRoom> CreateChatRoomAsync(List<ParseUser> participants)
    {
        var chatRoom = new ChatRoom();
        await chatRoom.SaveAsync(); // Save first to get an ObjectId

        // Add all participants to the relation.
        var relation = chatRoom.GetRelation<ParseUser>("Participants");
        foreach (var participant in participants)
        {
            relation.Add(participant);
        }
        await chatRoom.SaveAsync(); // Save again to update the relation

        //add this chatroom to other users,
        foreach (var p in participants)
        {
            var u = await ParseClient.Instance.GetQuery<ParseUser>().WhereEqualTo("objectId", p.ObjectId).FirstOrDefaultAsync();
            if (u != null)
            {
                u.GetRelation<ChatRoom>("ChatRooms").Add(chatRoom);
            }
            await u.SaveAsync();
        }


        return chatRoom;
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

    public async Task ListenForMessagesAsync(ChatRoom chatRoom, Action<Message> onMessageReceived)
    {
        try
        {
            _cts = new CancellationTokenSource();
            var query = ParseClient.Instance.GetQuery("Message")
                .WhereEqualTo("chatRoom", chatRoom)
                .OrderBy("createdAt"); // Order by creation time

            var subscription = LiveQueryClient!.Subscribe(query, "FriendRequestsSub");
            LiveQueryClient.ConnectIfNeeded();
            int retryDelaySeconds = 5;
            int maxRetries = 10;
            int batchSize = 3; // Number of events to release at a time
            TimeSpan throttleTime = TimeSpan.FromMilliseconds(0000);

            LiveQueryClient.OnConnected
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
                            LiveQueryClient.ConnectIfNeeded(); // revive app!

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

            LiveQueryClient.OnError
                .Do(ex =>
                {
                    Debug.WriteLine("LQ Error: " + ex.Message);
                    LiveQueryClient.ConnectIfNeeded();  // Ensure reconnection on errors
                })
                .OnErrorResumeNext(Observable.Empty<Exception>()) // Prevent breaking the stream
                .Subscribe();

            LiveQueryClient.OnDisconnected
                .Do(info => Debug.WriteLine(info.userInitiated
                    ? "User disconnected."
                    : "Server disconnected."))
                .Subscribe();

            LiveQueryClient.OnSubscribed
                .Do(e => Debug.WriteLine("Subscribed to: " + e.requestId))
                .Subscribe();


            LiveQueryClient.OnObjectEvent
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

    public async Task DisconnectAsync()
    {
        if (LiveQueryClient != null && _messageSubscription != null)
        {
            try
            {
                LiveQueryClient.RemoveAllSubscriptions();
                LiveQueryClient.Disconnect();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error disconnecting: {ex.Message}");
            }
            finally
            {
                _messageSubscription = null;
                LiveQueryClient = null;
            }
            _cts?.Cancel(); // Cancel any ongoing operations
        }
    }
}

public class LiveQueryManager : IDisposable
{
    private ParseLiveQueryClient LiveQueryClient { get; }
    private List<Subscription> Subscriptions { get; } = new List<Subscription>();
    private List<IDisposable> EventDisposables { get; } = new List<IDisposable>();

    public LiveQueryManager(ParseLiveQueryClient LiveQueryClient)
    {
        LiveQueryClient = LiveQueryClient;

        // Ensure the client is connected (you might handle this elsewhere, too)
        LiveQueryClient.ConnectIfNeeded();
        SubscribeToMultipleQueries();
        // Centralized error handling (optional, but good practice)
        var errorHandler = LiveQueryClient.OnError
            .Subscribe(ex =>
            {
                Debug.WriteLine($"LiveQuery Error: {ex.Message}");
                // Implement reconnection logic here, if desired.
                // LiveQueryClient.ConnectIfNeeded();
            });
        EventDisposables.Add(errorHandler);
    }

    public void SubscribeToMultipleQueries()
    {
        // 1. Chat Messages
        ParseQuery<ParseObject> chatQuery = ParseClient.Instance.GetQuery("ChatMessage");
        chatQuery = chatQuery.WhereEqualTo("roomId", "myRoomId"); // Example constraint
        Subscription chatSubscription = LiveQueryClient.Subscribe(chatQuery);
        Subscriptions.Add(chatSubscription);
        SubscribeToEvents(chatSubscription, "Chat");

        // 2. Game Scores
        ParseQuery<ParseObject> scoreQuery = ParseClient.Instance.GetQuery("GameScore");
        scoreQuery = scoreQuery.WhereGreaterThan("score", 1000);  // Example constraint
        Subscription scoreSubscription = LiveQueryClient.Subscribe(scoreQuery);
        Subscriptions.Add(scoreSubscription);
        SubscribeToEvents(scoreSubscription, "Score");

        // 3. User Status
        ParseQuery<ParseUser> userQuery = ParseClient.Instance.GetQuery<ParseUser>(); //Use ParseUser
        userQuery = userQuery.WhereEqualTo("online", true); // Example: Track online users
        Subscription userSubscription = LiveQueryClient.Subscribe(userQuery);
        Subscriptions.Add(userSubscription);
        SubscribeToEvents(userSubscription, "User");

        // You could add reconnection logic here, similar to your original example.
        // It's often better to centralize this, as shown in the constructor.
    }
    private void SubscribeToEvents(Subscription subscription, string subscriptionName)
    {
        var eventHandler = LiveQueryClient.OnObjectEvent
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
        LiveQueryClient.RemoveAllSubscriptions();
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

        // LiveQueryClient.Disconnect(); //Disconnect from the client. You may want to do that OUTSIDE of the manager.
    }
}
