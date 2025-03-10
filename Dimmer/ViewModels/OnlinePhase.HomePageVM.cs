﻿using MongoDB.Bson;
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
    public partial ObservableCollection<UserActivity>? ChatMessages { get; set; } = new();
    
    [ObservableProperty]
    public partial ObservableCollection<ParseUser>? AllUsersAvailable { get; set; } = new();

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
                //SubscribeToFriendRequests();                
                SubscribeToChatMessages2();

                await StartOrJoinChat(new List<string>() { CurrentUserOnline.ObjectId });
                


                AllUsersAvailable.Add(CurrentUserOnline);

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
    public void SubscribeToFriendRequests2()
    {
        if (LiveQueryClient == null)
            return;
        
        try
        {

            var query = ParseClient.Instance.GetQuery<UserActivity>()
                .WhereEqualTo("recipient", CurrentUserOnline);
                
            var subscription = LiveQueryClient!.Subscribe(query, "FriendRequestsSub");
            LiveQueryClient.ConnectIfNeeded();
            int retryDelaySeconds = 5;
            int maxRetries = 10;
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
            .Subscribe(async e =>
            {
               await ProcessEvent(e);
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
    public void SubscribeToChatMessages2()
    {
        if (LiveQueryClient == null)
            return;

        try
        {
            // Listen for UserActivity objects of type "ChatMessage"
            var query = ParseClient.Instance.GetQuery<UserActivity>()                
                             
                .Include("chatMessage")  // Important: Include the chatMessage pointer
                .Include("chatMessage.Sender") // Include sender details
                .Include("chatMessage.Content") // Include sender details
                .Include("nowPlaying");
                


            var subscription = LiveQueryClient!.Subscribe(query, "ChatMessagesSub");

            LiveQueryClient.ConnectIfNeeded();
            int retryDelaySeconds = 5;
            int maxRetries = 10;
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
            .Subscribe(async e =>
            {
                await ProcessEvent(e);
            });

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error subscribing to activity messages: {ex.Message}");
        }
    }



    // --- Start/Join a Chat (Now supports group activity) ---

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
            List<ParseUser> participants = [];

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
            participants.Add(CurrentUserOnline);

            //2.  Check for existing activity room (IMPORTANT for preventing duplicates).
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
            
            Debug.WriteLine($"Started/Joined activity. ChatRoomId: {CurrentChatRoom.ObjectId}");

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error starting/joining activity: {ex.Message}");
        }
    }

    private async Task LoadExistingMessages()
    {
        if (CurrentChatRoom == null)
            return;

        var query = ParseClient.Instance.GetQuery<Message>()
            .WhereEqualTo(nameof(Message.ChatRoom), CurrentChatRoom)
            .Include(nameof(Message.Sender))
            .OrderBy(nameof(Message.CreatedAt)); 

        var existingMessages = await query.FindAsync();
        ChatMessages??=new();   
        foreach (var msg in ChatMessages)
        {
            await Task.WhenAll(msg.Sender.FetchIfNeededAsync(),
                msg.ChatMessage.FetchIfNeededAsync());

            ChatMessages.Add( msg); // Add to your ObservableCollection.
        }
    }
    // --- Live Query Subscription for Messages
    [ObservableProperty]
    public partial CollectionView? UserChatColView { get; set; }

    private async Task<ChatRoom?> GetExistingChatRoom(List<ParseUser> participants)
    {
        // Efficiently check for an existing activity room with ALL the given participants.
        // Using Any or Contains will not work. We need to use ContainsAll in a smart way.

        var query = ParseClient.Instance.GetQuery<ChatRoom>()
                     .WhereMatchesQuery("Participants", ParseClient.Instance.GetQuery<ParseUser>().WhereEqualTo("objectId", CurrentUserOnline.ObjectId));
        ;

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
    private async Task<ChatRoom> CreateChatRoomAsync2(List<ParseUser> participants)
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
            // Check if we now have friends and can start listening for activity messages
            if (await CheckIfUserHasFriends())
            {
                //SubscribeToChatMessages();
            }

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to accept friend request: {ex.Message}");
        }
    }
    async Task ProcessEvent((Subscription.Event evt, object objectDictionnary, Subscription subscription) e)
    {
        if (ChatMessages is null)
        {
            ChatMessages =new();
        }

        var objData = e.objectDictionnary as Dictionary<string, object>;
        if (objData is null)
            return;

        UserActivity activity;

        switch (e.evt)
        {
            case Subscription.Event.Enter:
                Debug.WriteLine("Entered");
                break;

            case Subscription.Event.Leave:
                Debug.WriteLine("Left");
                break;

            case Subscription.Event.Create:

                try
                {

                    // 2. Create a *new* UserActivity object with just the objectId.
                    activity = new UserActivity()
                    {
                        ObjectId = objData["objectId"].ToString(),                        
                        ActivityType = objData["activityType"].ToString()!,
                    };
                    ParseUser emptyUsr = (ParseUser)objData["sender"];
                    // 3. Fetch the related objects if they exist.

                    if (objData.ContainsKey("chatMessage"))
                    {
                        // Create a *new* Message object with just the objectId.
                        Message uMsg = (Message)objData["chatMessage"];



                        var qa = ParseClient.Instance.GetQuery<Message>()
                            .WhereEqualTo("objectId", uMsg.ObjectId);
                        uMsg = await qa.FirstOrDefaultAsync();


                        var q1 = ParseClient.Instance.GetQuery<ParseUser>()
                            .WhereEqualTo("objectId", emptyUsr.ObjectId);
                        uMsg.Sender = await q1.FirstOrDefaultAsync();
                        activity.Sender= uMsg.Sender;
                        activity.ChatMessage=uMsg;

                        if (uMsg.ChatRoom is not null)
                        { 
                            var q2 = ParseClient.Instance.GetQuery<ChatRoom>()
                                .WhereEqualTo("objectId", uMsg.ChatRoom.ObjectId);
                            var resChatRoom = await q2.FirstOrDefaultAsync();
                            uMsg.ChatRoom = resChatRoom;
                        }
                        

                    }
                    
                        // Add the fully populated UserActivity to your list.
                        ChatMessages.Add(activity);

                    
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing UserActivity: {ex.Message}");
                }

                break;
            case Subscription.Event.Update:
                activity = ObjectMapper.MapFromDictionary<UserActivity>(objData);
                var obj = ChatMessages.FirstOrDefault(x => x.ObjectId == activity.ObjectId);

                if (obj != null)
                {
                    ChatMessages[ChatMessages.IndexOf(obj)] = activity;
                }
                break;

            case Subscription.Event.Delete:
                activity = ObjectMapper.MapFromDictionary<UserActivity>(objData);
                var objToDelete = ChatMessages.FirstOrDefault(x => x.ObjectId == activity.ObjectId);

                if (objToDelete != null)
                {
                    ChatMessages.Remove(objToDelete);
                }
                if (ChatMessages.Count>1)
                {
                    //for some interesting reasons, if you call this when messages.count <1 it will crash/disconnect LQ subscription. (or maybe send it to another thread?)
                    MainThread.BeginInvokeOnMainThread(() => UserChatColView?.ScrollTo(ChatMessages.LastOrDefault(), null, ScrollToPosition.End, true));
                }
                break;

            default:
                Debug.WriteLine("Unhandled event type.");
                break;
        }

        Debug.WriteLine($"Processed {e.evt} on object {objData?.GetType()}");
    }

    public async Task<bool> CheckIfUserHasFriends2()
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

    public async Task SendMessageAsync(string messageContent, PlayType evtType= PlayType.logEvent, SongModelView? song=null)
    {
        if (string.IsNullOrWhiteSpace(messageContent) || CurrentChatRoom == null)
            return;

        
        try
        {
            var message = new Message
            {
                Sender = CurrentUserOnline,                
                Content = messageContent,
                ChatRoom = CurrentChatRoom
            };
            await message.SaveAsync();

            await UserActivityLogger.LogUserActivity(
                CurrentUserOnline!,  
                evtType, nowPlaying:song,
                chatMessage: message, chatRoomm: CurrentChatRoom, CurrentUserOnline: CurrentUserOnline);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending message: {ex.Message}");
        }
    }

    public async Task SetChatRoom(ChatRoomOptions roomOption)
    {
        
        var user = new List<string>
        {
            CurrentUserOnline.ObjectId
        };

        CurrentChatRoom = await StartOrJoinChatAsync(user);
        Message newMessage = new();
        
        newMessage.Content = $"{CurrentUserOnline.Username} Started Dimmer at {DateTime.Now.ToLocalTime()} On {DeviceInfo.Platform}";
        await newMessage.SaveAsync();

        await UserActivityLogger.LogUserActivity(CurrentUserOnline,
            activityType: 0, chatMessage: newMessage,
            chatRoomm: CurrentChatRoom, CurrentUserOnline: CurrentUserOnline);
        switch (roomOption)
        {
            
            case ChatRoomOptions.PersonalRoom:
                break;
            case ChatRoomOptions.GroupChatRoom:
                break;
            case ChatRoomOptions.UpdatesRoom:
                break;
            case ChatRoomOptions.StatsRoom:
                break;
            default:
                break;
        }
    }



}


public enum ChatRoomOptions
{
    PersonalRoom,
    GroupChatRoom,
    UpdatesRoom,
    StatsRoom

}