using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer_MAUI.ViewModels;


public partial class HomePageVM
{
    private readonly CompositeDisposable _disposables = [];
    

    // Observables for events
    public IObservable<FriendRequestDisplay> FriendRequestReceived { get; private set; }
    public IObservable<(string ChatRoomId, ChatMessageDisplay Message)> ChatMessageReceived { get; private set; }
    public IObservable<(string MessageId, string NewContent)> ChatMessageEdited { get; private set; } // Add for edit
    public IObservable<string> ChatMessageDeleted { get; private set; }  // Add for delete

    public async Task InitializeAsync()
    {
        CurrentUserOnline = await ParseClient.Instance.GetCurrentUser();
        if (CurrentUserOnline == null)
        {
            throw new InvalidOperationException("User must be logged in to use ChatService.");
        }

        SubscribeToFriendRequests(); //initial subscriptions
        if (await CheckIfUserHasFriends())
        {
            SubscribeToChatMessages();
        }
    }

    private void SetupLiveQueryEventHandlers()
    {
        LiveQueryClient.OnConnected
            .Subscribe(_ => Debug.WriteLine("LiveQuery connected."))
            ; 

        LiveQueryClient.OnDisconnected
            .Subscribe(info => Debug.WriteLine($"LiveQuery Disconnected: {info}"));

        LiveQueryClient.OnError
            .Subscribe(ex => Debug.WriteLine($"LiveQuery Error: {ex.Message}"));

        // Centralized reconnection (important for robustness)
        //var reconnect = LiveQueryClient.OnDisconnected.Where(x => !x.userInitiated)
        //   .DelaySubscription(TimeSpan.FromSeconds(5))
        //   .SelectMany(_ => Observable.FromAsync(() => LiveQueryClient.ConnectAsync()))
        //   .Retry()
        //   .Subscribe();
        LiveQueryClient.OnDisconnected
                .Do(info => Debug.WriteLine(info.userInitiated
                    ? "User disconnected."
                    : "Server disconnected."))
                .Subscribe();
        //_disposables.Add(reconnect);
    }



    // --- Friend Request Methods ---

    public async Task SendFriendRequestAsync(string recipientUsername)
    {
        if (string.IsNullOrWhiteSpace(recipientUsername))
        {
            throw new ArgumentException("Recipient username cannot be empty.");
        }

        // 1. Find the recipient user.
        ParseQuery<ParseUser> recipientQuery = ParseClient.Instance.GetQuery<ParseUser>().WhereEqualTo("username", recipientUsername);
        ParseUser recipient = await recipientQuery.FirstOrDefaultAsync();
        if (recipient == null)
        {
            throw new ArgumentException($"User '{recipientUsername}' not found.");
        }
        // 2. Check existing request
        ParseQuery<FriendRequest> existingRequestQuery = ParseClient.Instance.GetQuery<FriendRequest>()
           .WhereEqualTo("sender", CurrentUserOnline)
           .WhereEqualTo("recipient", recipient)
           .WhereEqualTo("status", "pending");

        if (await existingRequestQuery.CountAsync() > 0)
        {
            throw new InvalidOperationException("A pending friend request already exists.");
        }

        // 3. Create the FriendRequest object.
        FriendRequest friendRequest = new FriendRequest
        {
            Sender = CurrentUserOnline,
            Receiver= recipient,
            Status = "pending"
        };
        await friendRequest.SaveAsync();

        //4. Create the activity
        UserActivity activity = new UserActivity
        {
            Sender = CurrentUserOnline,
            
            
            //FriendRequest = friendRequest,
        };
        await activity.SaveAsync();
    }
    public async Task<List<FriendRequestDisplay>> GetPendingFriendRequestsAsync()
    {
        ParseQuery<UserActivity> query = ParseClient.Instance.GetQuery<UserActivity>()
            .WhereEqualTo("recipient", CurrentUserOnline)
            
            .Include("sender"); // Include the sender

        IEnumerable<UserActivity> userActivities = await query.FindAsync();
        List<FriendRequestDisplay> friendRequests = new List<FriendRequestDisplay>();

        foreach (UserActivity activity in userActivities)
        {
            ParseUser sender = activity.Sender; // Access directly since it's included
            friendRequests.Add(new FriendRequestDisplay
            {
                RequestId = activity.ObjectId, // Use UserActivity's ObjectId
                SenderUsername = sender.Username,
                SenderId = sender.ObjectId
            });
        }

        return friendRequests;
    }

    public async Task AcceptFriendRequestAsync(string userActivityId)
    {
        // Fetch the UserActivity object
        ParseQuery<UserActivity> activityQuery = ParseClient.Instance.GetQuery<UserActivity>().WhereEqualTo("objectId", userActivityId);
        UserActivity userActivity = await activityQuery.FirstOrDefaultAsync();

        if (userActivity == null)
        {
            throw new ArgumentException($"UserActivity with ID '{userActivityId}' not found.");
        }

        // Fetch the FriendRequest object
        //var friendRequest = await userActivity.FriendRequest.FetchIfNeededAsync();

        //if (friendRequest.Status != "pending")
        //{
        //    throw new InvalidOperationException("Friend request is not pending.");
        //}

        //// Update the status to "accepted"
        //friendRequest.Status = "accepted";
        //await friendRequest.SaveAsync();

        //// Create the Friendship object
        //var friendship = new Friendship
        //{
        //    User1 = friendRequest.Sender,
        //    User2 = friendRequest.Receiver // CurrentUserOnline
        //};
        //await friendship.SaveAsync();
        SubscribeToChatMessages();
    }
    public async Task RejectFriendRequestAsync(string userActivityId)
    {
        // Fetch the UserActivity object
        ParseQuery<UserActivity> activityQuery = ParseClient.Instance.GetQuery<UserActivity>().WhereEqualTo("objectId", userActivityId);
        UserActivity userActivity = await activityQuery.FirstOrDefaultAsync();

        if (userActivity == null)
        {
            throw new ArgumentException($"UserActivity with ID '{userActivityId}' not found.");
        }

        // Fetch the FriendRequest object
        //var friendRequest = await userActivity.FriendRequest.FetchIfNeededAsync();

        // Update status (or delete, depending on your preference)
        //friendRequest.Status = "rejected";
        //await friendRequest.SaveAsync();
    }

    private void SubscribeToFriendRequests()
    {
        ParseQuery<UserActivity> query = ParseClient.Instance.GetQuery<UserActivity>()
            .WhereEqualTo("recipient", CurrentUserOnline)
            
            .Include("sender"); // Include sender for display

        Subscription<UserActivity> subscription = LiveQueryClient.Subscribe(query);
        SynchronizationContext s = (SynchronizationContext)SynchronizationContext.Current!;
        
        // Use Rx.NET to handle the events, convert to FriendRequestDisplay, and ensure UI thread safety.
        FriendRequestReceived = LiveQueryClient.OnObjectEvent
            .Where(e => e.subscription == subscription && e.evt == Subscription.Event.Create)
            .Select(e =>
            {
                // Correctly handle object casting and conversion
                if (e.objectData is UserActivity userActivity)
                {
                    // Assuming Sender is already included, no need for FetchIfNeededAsync here
                    return new FriendRequestDisplay
                    {
                        RequestId = userActivity.ObjectId, // Use the UserActivity's ObjectId
                        SenderUsername = userActivity.Sender.Username,
                        SenderId = userActivity.Sender.ObjectId
                    };
                }
                return null; // Or handle unexpected types appropriately
            })
            .Where(fr => fr != null) // Filter out null results from unexpected types
            .ObserveOn(s); // UI thread safety

        FriendRequestReceived.Subscribe();
    }
    public async Task<bool> CheckIfUserHasFriends()
    {
        if (CurrentUserOnline == null)
            return false;

        ParseQuery<Friendship> query1 = ParseClient.Instance.GetQuery<Friendship>().WhereEqualTo("user1", CurrentUserOnline);
        ParseQuery<Friendship> query2 = ParseClient.Instance.GetQuery<Friendship>().WhereEqualTo("user2", CurrentUserOnline);
        return await query1.CountAsync() > 0 || await query2.CountAsync() > 0;
    }

    // --- Chat Methods ---
    private void SubscribeToChatMessages()
    {
        ParseQuery<UserActivity> query = ParseClient.Instance.GetQuery<UserActivity>()          
            
            .Include("chatMessage")
            .Include("chatMessage.sender")
            .Include("chatMessage.chatRoom"); // Include ChatRoom

        Subscription<UserActivity> subscription = LiveQueryClient.Subscribe(query);

        ChatMessageReceived = LiveQueryClient.OnObjectEvent
             .Where(e => e.subscription == subscription && e.evt == Subscription.Event.Create)
             .Select(e =>
             {
                 if (e.objectData is UserActivity userActivity)
                 {

                     Message chatMessage = userActivity.ChatMessage; // No need for MapFromDictionary
                     string chatRoomId = chatMessage.ChatRoom.ObjectId;  // Access ChatRoom
                     return (chatRoomId, new ChatMessageDisplay
                     {
                         MessageId = chatMessage.ObjectId, // Use the Message's ObjectId
                         SenderUsername = chatMessage.Sender.Username,
                         Content = chatMessage.Content,
                         IsEdited = chatMessage.IsEdited,
                         //IsMyMessage = chatMessage.Sender.ObjectId == CurrentUserOnline.ObjectId
                     });
                 }
                 return (null,null); // Or handle unexpected type appropriately
             })
            .Where(t => t.Item2 != null)  //filter out null
            .ObserveOn(SynchronizationContext.Current!);  // Back to the UI t hread.

        // Subscribe and manage the subscription.
        ChatMessageReceived.Subscribe();

        ChatMessageEdited = LiveQueryClient.OnObjectEvent
          .Where(e => e.subscription == subscription && e.evt == Subscription.Event.Update)
          .Select(e =>
          {
              if (e.objectData is UserActivity userActivity)
              {
                  Message chatMessage = userActivity.ChatMessage;
                  return (chatMessage.ObjectId, chatMessage.Content);
              }
              return (null, null); // Or handle unexpected types appropriately
          })
          .Where(t => t.ObjectId != null)  //filter out null
          .ObserveOn(SynchronizationContext.Current);
        ChatMessageEdited.Subscribe();

        ChatMessageDeleted = LiveQueryClient.OnObjectEvent
            .Where(e => e.subscription == subscription && e.evt == Subscription.Event.Delete)
            .Select(e =>
            {
                if (e.objectData is UserActivity userActivity)
                {

                    return userActivity.ChatMessage.ObjectId; // Return the deleted message's ID
                }
                return null; // Or handle unexpected type
            })
            .Where(id => id != null)
            .ObserveOn(SynchronizationContext.Current);

        ChatMessageDeleted.Subscribe();

    }
    public async Task<ChatRoom> StartOrJoinChatAsync(List<string> participantIds)
    {
        // Fetch participants (including current user).
        List<ParseUser> participants = await FetchParticipantsAsync(participantIds);
        if (participants.Count <= 0)
            return null;

        // Check for existing chat room.
        ChatRoom? existingRoom = await GetExistingChatRoomAsync(participants);
        if (existingRoom != null)
            return existingRoom;

        // Create new chat room.
        return await CreateChatRoomAsync(participants);
    }
    private async Task<List<ParseUser>> FetchParticipantsAsync(List<string> participantIds)
    {
        List<ParseUser> participants = new List<ParseUser>();
        foreach (string id in participantIds)
        {
            ParseUser user = await ParseClient.Instance.GetQuery<ParseUser>().GetAsync(id);
            if (user != null)
            {
                participants.Add(user);
            }
            else
            {
                Debug.WriteLine($"User with ID {id} not found.");
                return null; // Indicate failure.
            }
        }
        participants.Add(CurrentUserOnline); // Add current user.
        return participants;
    }

    private async Task<ChatRoom?> GetExistingChatRoomAsync(List<ParseUser> participants)
    {
        ParseQuery<ChatRoom> query = ParseClient.Instance.GetQuery<ChatRoom>()
                     .WhereMatchesQuery("Participants", ParseClient.Instance.GetQuery<ParseUser>().WhereEqualTo("objectId", CurrentUserOnline.ObjectId));
        ;

        // Get all potential ChatRooms, and then filter in memory for an *exact* match.
        IEnumerable<ChatRoom> potentialMatches = await query.FindAsync();

        foreach (ChatRoom chatRoom in potentialMatches)
        {
            // Fetch the participants for this ChatRoom.
            IEnumerable<ParseUser> chatRoomParticipants = await chatRoom.GetRelation<ParseUser>("Participants").Query.FindAsync();

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
        ChatRoom chatRoom = new ChatRoom();
        
        await chatRoom.SaveAsync();

        ParseRelation<ParseUser> relation = chatRoom.Participants;
        foreach (ParseUser participant in participants)
        {
            relation.Add(participant);
        }
        await chatRoom.SaveAsync();

        return chatRoom;
    }
    public async Task<List<ChatMessageDisplay>> GetChatHistoryAsync(string chatRoomId)
    {
        ChatRoom chatRoom = new ChatRoom { ObjectId = chatRoomId }; // Create a ChatRoom object with just the ID

        ParseQuery<Message> query = ParseClient.Instance.GetQuery<Message>()
            .WhereEqualTo(nameof(Message.ChatRoom), chatRoom)
            .Include(nameof(Message.Sender))
            .OrderBy(nameof(Message.CreatedAt));

        IEnumerable<Message> messages = await query.FindAsync();

        return [.. messages.Select(msg => new ChatMessageDisplay
        {
            MessageId = msg.ObjectId,
            SenderUsername = msg.Sender.Username,
            Content = msg.Content,
            IsEdited = msg.IsEdited,
            //IsMyMessage = msg.Sender.ObjectId == CurrentUserOnline.ObjectId
        })];
    }

    public async Task SendMessageAsync(string chatRoomId, string content)
    {
        ChatRoom chatRoom = new ChatRoom { ObjectId = chatRoomId }; // Create a ChatRoom object with just the ID.
        Message message = new Message
        {
            ChatRoom = chatRoom,
            Sender = CurrentUserOnline,
            Content = content,
            IsEdited = false
        };
        await message.SaveAsync();

        // Create UserActivity for the message
        UserActivity activity = new UserActivity
        {
            Sender = CurrentUserOnline,
            
            ActivityType = "ChatMessage",
            ChatMessage = message
        };
        await activity.SaveAsync();

        // Update UserActivity recipients based on ChatRoom participants
        await UpdateMessageRecipientsAsync(activity, chatRoom);

    }

    private async Task UpdateMessageRecipientsAsync(UserActivity activity, ChatRoom chatRoom)
    {
        // Fetch the participants of the chat room (excluding the sender).
        IEnumerable<ParseUser> participants = await chatRoom.Participants.Query
            .WhereNotEqualTo("objectId", CurrentUserOnline.ObjectId)
            .FindAsync();

        // Create UserActivity entries for each recipient.
        foreach (ParseUser recipient in participants)
        {
            // You could optimize this by batching the save operations.
            UserActivity newActivity = new UserActivity
            {
                Sender = CurrentUserOnline,
                
                ActivityType = "ChatMessage",
                ChatMessage = activity.ChatMessage
            };
            await newActivity.SaveAsync();
        }

        // Optionally, you could delete the initial UserActivity that was sent to self, or use it
        //  for a sent confirmation.
        //await activity.DeleteAsync(); 
    }

    public async Task EditMessageAsync(string messageId, string newContent)
    {
        ParseQuery<Message> query = ParseClient.Instance.GetQuery<Message>().WhereEqualTo("objectId", messageId);
        Message message = await query.FirstOrDefaultAsync();
        if (message == null)
        {
            throw new ArgumentException($"Message with ID '{messageId}' not found.");
        }

        if (message.Sender.ObjectId != CurrentUserOnline.ObjectId)
        {
            throw new InvalidOperationException("Only the sender can edit a message.");
        }

        message.Content = newContent;
        message.IsEdited = true;
        await message.SaveAsync();
    }

    public async Task DeleteMessageAsync(string messageId)
    {
        ParseQuery<Message> query = ParseClient.Instance.GetQuery<Message>().WhereEqualTo("objectId", messageId);
        Message message = await query.FirstOrDefaultAsync();

        if (message == null)
        {
            throw new ArgumentException($"Message with ID '{messageId}' not found.");
        }

        if (message.Sender.ObjectId != CurrentUserOnline.ObjectId)
        {
            throw new InvalidOperationException("Only the sender can delete a message.");
        }
        //get all user activity that includes this message
        ParseQuery<UserActivity> activityQuery = ParseClient.Instance.GetQuery<UserActivity>()
            .WhereEqualTo("chatMessage", message);
        IEnumerable<UserActivity> activities = await activityQuery.FindAsync();
        foreach (UserActivity activity in activities)
        {
            await activity.DeleteAsync();
        }
        await message.DeleteAsync(); // Delete the message itself.
    }

    public void Dispose()
    {
        _disposables.Dispose();
        LiveQueryClient.Disconnect();
    }
}